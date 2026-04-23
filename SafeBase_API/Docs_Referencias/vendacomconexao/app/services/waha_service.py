# app\services\waha_service.py
import logging
import os
import hmac
import hashlib
import uuid
import asyncio
from datetime import datetime
from typing import Dict, Any, Optional
from types import SimpleNamespace
import re
import time
import httpx
from app.services.waha_session import get_session, set_session, update_session_key, clear_session, get_session_key
import json

from app.services.prompt_service import prompt_service
from app.services.history_service import history_service
from app.dependencies import chamar_ia_otimizado
from app.services.waha_outbound_service import waha_outbound_service
from app.database.core import get_db_connection

from app.database.webhooks_db import log_webhook_event
from app.database.history import save_simulation
from app.database.users import get_or_create_system_user

logger = logging.getLogger(__name__)


class WahaService:
    def __init__(self):
        # Nome do gateway usado no log
        self.gateway_name = os.getenv("WAHA_GATEWAY_NAME", "waha")
        # Segredo para validar assinatura de webhook (se aplicável)
        self.webhook_secret = os.getenv("WAHA_WEBHOOK_SECRET")
        # Nome do header que carrega a assinatura
        self.signature_header = os.getenv("WAHA_SIGNATURE_HEADER", "X-Waha-Signature")
        # API Key para fazer requisições ao WAHA
        self.waha_api_key = os.getenv("WAHA_API_KEY")
        # URL base do WAHA para resolver LIDs 
        self.waha_base_url = os.getenv("WAHA_LID_RESOLVE_URL", "https://waha01.vendacomconexao.com")
        
        # Usuário sistema para gravações de simulações (não nulo)
        try:
            sys_username = os.getenv('WEBHOOK_SYSTEM_USERNAME', 'webhook_bot')
            self.system_user_id = get_or_create_system_user(sys_username)
        except Exception:
            logger.exception("Não foi possível obter/criar usuário de sistema para webhooks")
            self.system_user_id = None
        
        # Cache para mapear @lid IDs para números reais
        # Estrutura: {"143172869029943@lid": "555182824693", ...}
        # Preenchido por:
        # 1. Eventos 'unread_count' que contêm data[0].name com número real
        # 2. Chamadas à API do WAHA (GET /api/default/lids/{lid}) que retorna pn (phone number)
        # 3. Consultas anteriores ao banco de dados
        self._lid_to_real_phone_cache = {}

    def _validate_signature(self, payload_bytes: bytes, signature_header_value: Optional[str], headers: Optional[Dict[str, Any]] = None) -> bool:
        """Valida HMAC-SHA256 (ou similar) se `WAHA_WEBHOOK_SECRET` estiver configurado.
        Se não houver segredo configurado, assume-se válido (para testes locais).
        """
        if not self.webhook_secret:
            logger.debug("Sem WAHA_WEBHOOK_SECRET configurado — pulando validação de assinatura")
            return True

        # If explicit signature header is not provided, allow an API key header
        if not signature_header_value:
            logger.debug("Assinatura de webhook ausente; verificando X-API-Key se presente")
            api_key_header = None
            if headers:
                api_key_header = headers.get('X-API-Key') or headers.get('x-api-key')
            logger.debug("X-API-Key header present: %s", bool(api_key_header))
            # If WAHA_WEBHOOK_SECRET equals API key header, accept
            if api_key_header and hmac.compare_digest(api_key_header, self.webhook_secret):
                logger.debug("Webhooks autenticado por X-API-Key igual a WAHA_WEBHOOK_SECRET")
                return True
            # Otherwise check registered API keys in DB (if present)
            if api_key_header:
                try:
                    from app.database.rbac import get_api_key_profile
                    profile = get_api_key_profile(api_key_header)
                    if profile and profile.get('ativa'):
                        logger.debug("Webhooks autenticado por X-API-Key registrado no backend")
                        return True
                except Exception:
                    logger.debug("Erro ao validar API Key via DB para webhook; prosseguindo")
            logger.warning("Assinatura de webhook ausente e X-API-Key inválida/ausente")
            return False

        try:
            # Suporta formatos: raw hexdigest ou 'sha256=...' similares
            provided = signature_header_value
            if provided.startswith("sha256="):
                provided = provided.split("=", 1)[1]

            computed = hmac.new(self.webhook_secret.encode('utf-8'), payload_bytes, hashlib.sha256).hexdigest()

            is_valid = hmac.compare_digest(computed, provided)
            if not is_valid:
                logger.warning("Assinatura de webhook inválida: computed=%s provided=%s", computed, provided)
                # Extra debugging: show header presence (X-Hub-Signature-256, X-Waha-Signature)
                try:
                    sig_header_values = [
                        headers.get('X-Waha-Signature'),
                        headers.get('x-waha-signature'),
                        headers.get('X-Hub-Signature-256'),
                        headers.get('x-hub-signature-256')
                    ] if headers else []
                    logger.debug("Assinatura presentes nos headers: %s", [v for v in sig_header_values if v])
                except Exception:
                    pass
            return is_valid
        except Exception as e:
            logger.error("Erro ao validar assinatura do webhook: %s", e)
            return False

    def _find_event_by_message_id(self, message_id: str) -> Optional[str]:
        """Procura um evento já registrado que contenha o `message_id` no payload.
        Retorna o id do evento se encontrado, caso contrário None.
        """
        if not message_id:
            return None

        try:
            with get_db_connection() as conn:
                with conn.cursor() as cur:
                    # Tenta corresponder em diferentes caminhos comuns do JSON
                    cur.execute("""
                        SELECT id FROM webhook_events
                        WHERE ( (payload::json->> 'id') = %s
                                OR (payload::json->> 'message_id') = %s
                                OR (payload::json->'message'->> 'id') = %s )
                        LIMIT 1
                    """, (message_id, message_id, message_id))
                    row = cur.fetchone()
                    if row:
                        return str(row[0])
        except Exception as e:
            logger.debug("Erro ao procurar message_id existente: %s", e)

        return None

    async def handle_webhook(self, payload: Dict[str, Any], headers: Dict[str, Any], raw_body: bytes = None) -> Optional[str]:
        """Processa o webhook recebido.
        - valida assinatura (quando configurada)
        - loga o payload em `webhook_events`
        - tenta persistir um registro simples em `simulacoes` (usuario_id = NULL) para disponibilizar no histórico
        Retorna o id do evento de webhook (UUID) ou None em caso de falha crítica.
        """
        try:
            # Validação de assinatura
            sig_val = None
            if headers:
                # support both default and common webhook signature header names
                sig_val = headers.get(self.signature_header) or headers.get(self.signature_header.lower())
                if not sig_val:
                    # Waha might send X-Hub-Signature-256 (common in other implementations)
                    sig_val = headers.get('X-Hub-Signature-256') or headers.get('x-hub-signature-256')

            body_bytes = raw_body if raw_body is not None else (str(payload).encode('utf-8'))

            if not self._validate_signature(body_bytes, sig_val, headers):
                # Registra tentativa com tipo de evento 'invalid_signature' usando
                # um status genérico aceito pelo banco ('failed') para evitar
                # violação de constraints na tabela `webhook_events`.
                log_webhook_event(gateway=self.gateway_name, event_type="invalid_signature", payload=payload, status="failed")
                return None

            # Extrai message_id para deduplicação antes de logar o evento
            message_id = payload.get('id') or payload.get('message_id') or (payload.get('message') and payload.get('message').get('id')) or None
            # Se ainda não temos message_id, tente extrair de payload.data[0]._data.id (formato Waha)
            if not message_id and isinstance(payload.get('data'), list) and len(payload.get('data')) > 0:
                try:
                    first = payload.get('data')[0]
                    if isinstance(first, dict):
                        mid = None
                        if first.get('_data') and isinstance(first.get('_data'), dict):
                            mid = first.get('_data').get('id')
                        mid = mid or first.get('id')
                        # mid pode ser um objeto com sub-chaves
                        if isinstance(mid, dict):
                            message_id = mid.get('id') or mid.get('_serialized') or str(mid)
                        else:
                            message_id = mid or message_id
                except Exception:
                    pass
            # Também tente extrair message_id de payload['payload'] (formato WEBJS/Waha)
            if not message_id and isinstance(payload.get('payload'), dict):
                try:
                    pl = payload.get('payload')
                    mid = pl.get('id') or (pl.get('_data') and pl.get('_data').get('id'))
                    if isinstance(mid, dict):
                        message_id = mid.get('id') or mid.get('_serialized') or str(mid)
                    else:
                        message_id = mid or message_id
                except Exception:
                    pass
                # Ignore Waha events originating from the local session (outbound acks)
                try:
                    # common shape: payload.data[0]._data.id.fromMe or payload.data[0]._data.fromMe
                    if isinstance(payload.get('data'), list) and len(payload.get('data')) > 0:
                        first = payload.get('data')[0]
                        if isinstance(first, dict):
                            from_me = None
                            if first.get('_data') and isinstance(first.get('_data'), dict):
                                id_obj = first.get('_data').get('id')
                                if isinstance(id_obj, dict):
                                    from_me = id_obj.get('fromMe')
                                from_me = from_me or first.get('_data').get('fromMe')
                            if from_me:
                                logger.info('Ignorando evento Waha vindo de nós mesmos (fromMe); não processando.')
                                # mark as received to keep audit but avoid attempting processing
                                log_webhook_event(gateway=self.gateway_name, event_type=payload.get('event') or event_type, payload=payload, status='received', processing_log='ignored_fromMe')
                                return None
                except Exception:
                    pass

            # Se já foi processado, retornamos o evento existente (idempotência)
            existing = self._find_event_by_message_id(message_id) if message_id else None
            if existing:
                logger.info("Webhook duplicado detectado para message_id=%s. Pulando processamento.", message_id)
                return existing

            # Loga o evento bruto
            event_type = payload.get('event') or payload.get('type') or 'message_received'
            event_id = log_webhook_event(gateway=self.gateway_name, event_type=event_type, payload=payload, status="received", external_message_id=message_id)

            # Se é evento unread_count, cacheia o mapeamento @lid -> número real
            # Isso permite que eventos 'message' posteriores resolvam o @lid
            self._cache_lid_phone_mapping(payload)

            # Normaliza mensagem para o formato interno 'conversa'
            try:
                # Tenta extrair informações comuns — adaptar conforme o formato do Waha
                # Padrão: tentar top-level primeiro, depois data[0]
                from_number = (
                    payload.get('from')
                    or payload.get('sender')
                    or payload.get('telefone')
                    or payload.get('msisdn')
                    or (payload.get('payload') and (payload.get('payload').get('from') or payload.get('payload').get('msisdn') or payload.get('payload').get('sender')))
                    or (payload.get('message') and (payload.get('message').get('from') or payload.get('message').get('sender') or payload.get('message').get('msisdn')))
                    or (payload.get('_data') and payload.get('_data').get('from'))
                )
                
                # Se não encontrou no top-level, tenta em data[0]
                if not from_number and isinstance(payload.get('data'), list) and len(payload.get('data', [])) > 0:
                    first = payload.get('data')[0]
                    if isinstance(first, dict):
                        from_number = (
                            first.get('from')
                            or first.get('sender')
                            or (first.get('_data') and first.get('_data').get('from'))
                            or (first.get('id') and (first.get('id').get('_serialized') if isinstance(first.get('id'), dict) else None))  # Tenta em data[0].id._serialized (eventos de status)
                        )
                
                to_number = (
                    payload.get('to')
                    or payload.get('recipient')
                    or (payload.get('payload') and payload.get('payload').get('to'))
                    or (payload.get('message') and payload.get('message').get('to'))
                )
                
                # Se não encontrou no top-level, tenta em data[0]
                if not to_number and isinstance(payload.get('data'), list) and len(payload.get('data', [])) > 0:
                    first = payload.get('data')[0]
                    if isinstance(first, dict):
                        to_number = (
                            first.get('to')
                            or (first.get('_data') and first.get('_data').get('to'))
                        )
                
                # Extrai notifyName (nome do contato) que pode conter o número real
                # Primeiro tenta campos de nome
                notify_name = (
                    payload.get('notifyName')
                    or (payload.get('payload') and payload.get('payload').get('notifyName'))
                    or (payload.get('message') and payload.get('message').get('notifyName'))
                    or (payload.get('_data') and payload.get('_data').get('notifyName'))
                )
                
                # Se notifyName é um valor inválido (tipo '.', vazio, ou muito curto), descartar
                if isinstance(notify_name, str) and (not notify_name.strip() or len(notify_name.strip()) <= 1 or notify_name.strip() == '.'):
                    notify_name = None
                
                # Se ainda não encontrou um notify_name válido, tenta em data[0]
                if not notify_name and isinstance(payload.get('data'), list) and len(payload.get('data', [])) > 0:
                    first = payload.get('data')[0]
                    if isinstance(first, dict):
                        # Tenta vários lugares em data[0]
                        name_from_data = (
                            first.get('name')  # WAHA usa 'name' em unread_count
                            or first.get('notifyName')  # Ou notifyName
                            or (first.get('_data') and first.get('_data').get('name'))  # Ou em _data.name
                        )
                        # Valida o nome antes de usar
                        if isinstance(name_from_data, str) and name_from_data.strip() and len(name_from_data.strip()) > 1 and name_from_data.strip() != '.':
                            notify_name = name_from_data
                            logger.debug('Extracted notify_name from data[0]: %s', notify_name)
                
                # Tenta buscar em lugares alternativos se notifyName ainda for inválido
                if not notify_name or notify_name == '.':
                    # Tenta extrair de payload['payload']['_data']['notifyName'] se disponível
                    if isinstance(payload.get('payload'), dict):
                        payload_data = payload['payload']
                        if isinstance(payload_data.get('_data'), dict):
                            alt_name = payload_data['_data'].get('notifyName')
                            if alt_name and alt_name != '.':
                                notify_name = alt_name
                                logger.debug('Found notifyName in payload._data: %s', notify_name)
                    
                    # Se ainda não encontrou, tenta em payload['data'][0]['lastMessage']['_data']['notifyName']
                    if (not notify_name or notify_name == '.') and isinstance(payload.get('data'), list) and len(payload.get('data', [])) > 0:
                        first = payload['data'][0]
                        if isinstance(first, dict) and isinstance(first.get('lastMessage'), dict):
                            last_msg = first['lastMessage']
                            if isinstance(last_msg.get('_data'), dict):
                                alt_name = last_msg['_data'].get('notifyName')
                                if alt_name and alt_name != '.':
                                    notify_name = alt_name
                                    logger.debug('Found notifyName in data[0].lastMessage._data: %s', notify_name)
                
                logger.debug('Final notify_name for processing: %s', notify_name)
                logger.debug('Extracted phone raw from webhook: from=%s to=%s notifyName=%s', from_number, to_number, notify_name)
                
                # Resolve o número real se for um ID Meta Business (passa payload para fallback)
                # Usa versão async com retry para aguardar eventos unread_count que chegam após message
                from_number = await self._resolve_phone_number_async(from_number, notify_name, payload)
                logger.debug('Resolved from_number: %s', from_number)
                
                # Se não conseguiu extrair from_number, é um evento que não é mensagem (ex: unread_count, status)
                # Ignora o processamento
                if not from_number:
                    logger.debug('Ignorando webhook sem from_number: event_type=%s payload_keys=%s', event_type, list(payload.keys()))
                    # Marca como processado mesmo assim para evitar retry
                    try:
                        log_webhook_event(gateway=self.gateway_name, event_type=event_type, payload=payload, status="processed", event_id=event_id, external_message_id=message_id, processing_log='skipped_no_from_number')
                    except Exception:
                        pass
                    return str(event_id)
                
                # garante que message_id esteja definido para ser salvo nas métricas
                message_id = message_id or str(uuid.uuid4())
                # Extrai texto de várias formas, incluindo os formatos Waha/WEBJS:
                # - payload.message
                # - payload.data[0]._data.body
                # - payload._data.body
                # - payload.payload.body (WEBJS wrapper)
                text = ''
                # Caso comum: payload.message is a dict with text/body
                if isinstance(payload.get('message'), dict):
                    text = payload['message'].get('text') or payload['message'].get('body') or ''
                # WEBJS/Waha often wraps the real payload in `payload` with `body`/_data
                if not text and isinstance(payload.get('payload'), dict):
                    try:
                        pl = payload.get('payload')
                        text = pl.get('text') or pl.get('body') or ((pl.get('_data') and pl.get('_data').get('body')) if isinstance(pl.get('_data'), dict) else '') or ''
                    except Exception:
                        text = ''
                # Waha often sends an array in `data` / `payload.data`
                if not text and isinstance(payload.get('data'), list) and len(payload.get('data')) > 0:
                    try:
                        first = payload.get('data')[0]
                        if isinstance(first, dict):
                            # Prefer nested _data.body
                            text = (first.get('_data') and (first.get('_data').get('body'))) or first.get('body') or ''
                    except Exception:
                        text = ''
                # Top-level fallbacks
                text = (text or (payload.get('_data') and payload.get('_data').get('body')) or payload.get('text') or payload.get('body') or '')
                # Se ainda não encontramos texto, log de debug para investigação
                if not text:
                    logger.debug('Nenhum texto extraído do webhook Waha; keys=%s sample=%s', list(payload.keys()), json.dumps({k: payload.get(k) for k in list(payload.keys())[:3]}, default=str) )
                timestamp = payload.get('timestamp') or payload.get('ts') or datetime.utcnow().isoformat()

                conversa = {
                    "canal": "whatsapp",
                    "mensagens": [
                        {
                            "tipo": "cliente",
                            "texto": text,
                            "from": from_number,
                            "to": to_number,
                            "message_id": message_id,
                            "timestamp": timestamp
                        }
                    ]
                }

                metricas = {"raw_event_id": str(event_id), "received_at": datetime.utcnow().isoformat()}

                # Agendamos o processamento pesado em background (não bloquear resposta HTTP)
                # Fazemos uma lookup rápida para mapear número -> user/cliente e registramos essa informação
                try:
                    lookup = self._lookup_phone(from_number)
                except Exception as lk_e:
                    logger.warning("Erro rápido ao procurar telefone antes do background: %s", lk_e)
                    lookup = {"type": None}

                # Prepara processing_log com o resultado do match para auditoria
                match_info = None
                try:
                    if lookup and lookup.get('type') == 'user' and lookup.get('user_id'):
                        match_info = f"matched_user:{lookup.get('user_id')}"
                    elif lookup and lookup.get('type') == 'cliente' and lookup.get('cliente_id'):
                        match_info = f"matched_cliente:{lookup.get('cliente_id')}"
                    else:
                        match_info = "matched:none"
                except Exception:
                    match_info = "matched:unknown"

                try:
                    log_webhook_event(gateway=self.gateway_name, event_type=event_type, payload=payload, status="received", event_id=event_id, external_message_id=message_id, processing_log=match_info)
                except Exception:
                    # Não falhar a requisição por erro de log
                    logger.exception("Falha ao atualizar webhook como received")

                # Agendar task de background, repassando lookup para evitar nova consulta
                try:
                    asyncio.create_task(self._background_process(event_id, event_type, payload, conversa, metricas, from_number, text, message_id, lookup))
                except Exception as bg_e:
                    logger.exception("Não foi possível agendar processamento em background: %s", bg_e)

                # Responde rapidamente para o provedor
                return str(event_id)

            except Exception as inner:
                logger.error("Erro ao processar payload do webhook: %s", inner)
                if event_id:
                    # Use the standardized 'processing_error' status to comply with DB check constraints
                    log_webhook_event(gateway=self.gateway_name, event_type="processing_error", payload=payload, status="processing_error", processing_log=str(inner), event_id=event_id)
                return None

        except Exception as e:
            logger.exception("Falha geral ao manipular webhook Waha: %s", e)
            return None

    def _is_meta_business_id(self, phone: Optional[str]) -> bool:
        """Verifica se o número é um ID Meta Business (formato: 123456789@lid)."""
        if not phone or not isinstance(phone, str):
            return False
        return '@lid' in phone.lower()

    async def _resolve_lid_via_waha_api(self, lid_id: Optional[str]) -> Optional[str]:
        """Resolve um @lid ID fazendo requisição à API do WAHA.
        
        Endpoint: GET {WAHA_LID_RESOLVE_URL}/api/default/lids/{lid_id}
        Response: {"lid":"143172869029943@lid","pn":"555182824693@c.us"}
        
        Extrai o campo 'pn', remove o '@c.us' e retorna o número normalizado.
        
        Args:
            lid_id: ID @lid (ex: "143172869029943@lid")
            
        Returns:
            Número normalizado (ex: "555182824693") ou None se falhar
        """
        if not lid_id or not self._is_meta_business_id(lid_id):
            return None
        
        # Verifica cache primeiro
        if lid_id in self._lid_to_real_phone_cache:
            logger.debug('Found %s in cache (WAHA API)', lid_id)
            return self._lid_to_real_phone_cache[lid_id]
        
        # Não temos WAHA API key ou URL configurados
        if not self.waha_api_key or not self.waha_base_url:
            logger.debug('WAHA API resolution not configured (missing WAHA_API_KEY or WAHA_LID_RESOLVE_URL)')
            return None
        
        try:
            # Constrói a URL
            url = f"{self.waha_base_url}/api/default/lids/{lid_id}"
            
            # Faz a requisição HTTP
            async with httpx.AsyncClient(timeout=5.0) as client:
                response = await client.get(
                    url,
                    headers={
                        "X-Api-Key": self.waha_api_key,
                        "Accept": "application/json"
                    }
                )
                
                if response.status_code != 200:
                    logger.warning('WAHA API resolve failed: status=%d for %s', response.status_code, lid_id)
                    return None
                
                data = response.json()
                pn = data.get('pn')  # ex: "555182824693@c.us"
                
                if not pn:
                    logger.warning('WAHA API response missing "pn" field: %s', data)
                    return None
                
                # Remove "@c.us" suffix se existir
                phone = pn.replace('@c.us', '').strip()
                
                # Normaliza (remove tudo que não for dígito)
                phone_normalized = re.sub(r'\D', '', phone)
                
                if not phone_normalized:
                    logger.warning('Failed to extract phone from WAHA API pn=%s', pn)
                    return None
                
                # Garante código de país BR se necessário
                if len(phone_normalized) in (10, 11) and not phone_normalized.startswith('55'):
                    phone_normalized = '55' + phone_normalized
                
                # Cacheia o resultado
                self._lid_to_real_phone_cache[lid_id] = phone_normalized
                logger.info('Resolved @lid via WAHA API: %s -> %s', lid_id, phone_normalized)
                
                return phone_normalized
                
        except httpx.TimeoutException:
            logger.warning('WAHA API timeout resolving %s', lid_id)
            return None
        except httpx.RequestError as e:
            logger.warning('WAHA API request error resolving %s: %s', lid_id, e)
            return None
        except Exception as e:
            logger.exception('Error resolving @lid via WAHA API: %s', e)
            return None

    def _cache_lid_phone_mapping(self, payload: Dict[str, Any]) -> None:
        """Extrai e cacheia mapeamento @lid -> número real a partir de evento 'unread_count'.
        
        Evento 'unread_count' contém:
            data[0].id._serialized = "143172869029943@lid"
            data[0].name = "+55 51 8282-4693"  (número real)
            
        Armmazena esse mapeamento para que eventos 'message' posteriores possam
        resolver o @lid para o número real.
        """
        event_type = payload.get('event')
        if event_type != 'unread_count':
            return
        
        try:
            data_list = payload.get('data')
            if not isinstance(data_list, list) or len(data_list) == 0:
                return
            
            first_item = data_list[0]
            if not isinstance(first_item, dict):
                return
            
            # Extrai @lid ID
            lid_id = None
            id_obj = first_item.get('id')
            if isinstance(id_obj, dict):
                # Prefere _serialized que já contém formato completo
                lid_id = id_obj.get('_serialized') or id_obj.get('user')
                if lid_id and not lid_id.endswith('@lid'):
                    lid_id = f"{lid_id}@lid"
            
            # Extrai número real do campo 'name'
            real_phone_formatted = first_item.get('name')
            
            if lid_id and real_phone_formatted:
                # Normaliza o número
                real_phone = self._extract_real_phone_from_notify_name(real_phone_formatted)
                if real_phone and len(real_phone) >= 10:
                    self._lid_to_real_phone_cache[lid_id] = real_phone
                    logger.debug('Cached LID mapping: %s -> %s', lid_id, real_phone)
        except Exception as e:
            logger.debug('Erro ao cachear LID->phone mapping: %s', e)

    def _get_cached_phone_for_lid(self, lid_id: Optional[str]) -> Optional[str]:
        """Procura número real em cache para um @lid ID.
        
        Estratégia:
        1. Verifica cache em memória (preenchido por unread_count events)
        2. Se não encontrado, procura no histórico de webhooks recebidos
        3. Busca por eventos recentes com o mesmo @lid que contenham um número válido em notifyName/name
        
        Returns:
            Número normalizado (ex: '555182824693') se encontrado, None caso contrário.
        """
        if not lid_id or not self._is_meta_business_id(lid_id):
            return None
        
        # Stage 1: Verifica cache em memória
        cached = self._lid_to_real_phone_cache.get(lid_id)
        if cached:
            logger.debug('Found cached phone for %s: %s', lid_id, cached)
            return cached
        
        # Stage 2: Procura no histórico de webhooks recebidos
        # Busca por eventos recentes que contenham esse @lid e um número real em notifyName
        try:
            with get_db_connection() as conn:
                with conn.cursor() as cur:
                    # Busca eventos do webhook dos últimos 24 horas que contenham esse @lid
                    # e verifique se contêm um número válido no payload
                    cur.execute("""
                        SELECT payload FROM webhook_events
                        WHERE (
                            gateway = %s
                            AND (payload::text ILIKE %s OR payload::text ILIKE %s)
                            AND created_at > NOW() - INTERVAL '24 hours'
                        )
                        ORDER BY created_at DESC
                        LIMIT 50
                    """, (self.gateway_name, f'%{lid_id}%', f'%{lid_id.replace("@lid", "")}%'))
                    
                    rows = cur.fetchall()
                    for row in rows:
                        if not row or not row[0]:
                            continue
                        try:
                            payload = json.loads(row[0]) if isinstance(row[0], str) else row[0]
                            # Procura por notifyName ou name fields que contenham números
                            candidates = []
                            
                            # Tenta vários caminhos no payload
                            if isinstance(payload.get('data'), list) and len(payload.get('data')) > 0:
                                first = payload.get('data')[0]
                                if isinstance(first, dict):
                                    candidates.extend([
                                        first.get('name'),
                                        first.get('notifyName'),
                                        (first.get('_data', {}).get('name') if isinstance(first.get('_data'), dict) else None),
                                        (first.get('_data', {}).get('notifyName') if isinstance(first.get('_data'), dict) else None),
                                    ])
                            
                            candidates.extend([
                                payload.get('notifyName'),
                                payload.get('name'),
                            ])
                            
                            # Tenta extrair um número válido
                            for candidate in candidates:
                                if candidate and isinstance(candidate, str) and any(c.isdigit() for c in candidate):
                                    phone = self._extract_real_phone_from_notify_name(candidate)
                                    if phone and len(phone) >= 10:
                                        # Encontrou! Cacheia para uso futuro
                                        self._lid_to_real_phone_cache[lid_id] = phone
                                        logger.info('Resolved @lid via DB webhook history: %s -> %s', lid_id, phone)
                                        return phone
                        except Exception as e:
                            logger.debug('Erro ao processar webhook histórico para %s: %s', lid_id, e)
                            continue
        except Exception as e:
            logger.debug('Erro ao procurar @lid em webhook history: %s', e)
        
        return None

    def _extract_real_phone_from_notify_name(self, notify_name: Optional[str]) -> Optional[str]:
        """Extrai o número real do notifyName (ex: '+55 51 8282-4693' -> '555182824693').
        
        Remove espaços, símbolos especiais, e garante apenas dígitos.
        Se resultar em 10-11 dígitos sem código de país, adiciona 55 (Brasil).
        """
        if not notify_name or not isinstance(notify_name, str):
            return None
        
        # Valida se é um nome válido (não apenas "." ou valor muito curto)
        cleaned_name = notify_name.strip()
        if len(cleaned_name) <= 1 or cleaned_name == '.':
            return None
        
        # Remove espaços, símbolos e deixa apenas dígitos
        # Remove caracteres comuns: ( ) - . espaço, incluindo o sinal de + para preservar
        # Primeiro vamos extrair todos os dígitos e o sinal de +
        cleaned = cleaned_name
        # Mantém apenas dígitos e o sinal de +
        cleaned = re.sub(r'[^\d\+]', '', cleaned)
        
        # Se começa com +, remove-o para processamento
        if cleaned.startswith('+'):
            cleaned = cleaned[1:]
        
        # Remove tudo que não for dígito após extrair o código do país
        cleaned = re.sub(r'[^\d]', '', cleaned)
        
        if not cleaned or not cleaned.isdigit():
            return None
        
        # Se tem 10-11 dígitos e não começa com 55, assume número BR e adiciona código país
        if len(cleaned) in (10, 11) and not cleaned.startswith('55'):
            cleaned = '55' + cleaned
        
        return cleaned if len(cleaned) >= 10 else None

    def _resolve_phone_number(self, from_number: Optional[str], notify_name: Optional[str], payload: Dict[str, Any] = None) -> Optional[str]:
        """Resolve o número real do WhatsApp lidando com IDs @lid.
        
        Estratégia:
        1. Se não é @lid, retorna o número original
        2. Se é @lid, tenta extrair do notifyName (campo que contém número formatado)
        3. Se ainda não encontrou, tenta em payload['data'][*]['name'] (eventos de unread_count)
        4. Se ainda não encontrou, tenta em payload['data'][*]['_data']['name'] (dentro da estrutura WEBJS)
        5. Se ainda não encontrou, procura no cache de eventos unread_count anteriores
        6. Se ainda não encontrou, mantém o @lid original para evitar None
        
        Args:
            from_number: Número extraído do campo 'from' (pode ser @lid)
            notify_name: Número/nome do contato (pode conter telefone formatado)
            payload: Payload completo do webhook para fallback
            
        Returns:
            Número normalizado (apenas dígitos) ou @lid original se não conseguir extrair
        """
        # Log detalhado para debug
        logger.debug('DEBUG _resolve_phone_number: from_number=%s, notify_name=%s, is_lid=%s, cache_keys=%s',
                    from_number, notify_name, self._is_meta_business_id(from_number),
                    list(self._lid_to_real_phone_cache.keys())[:5] if self._lid_to_real_phone_cache else [])
        
        # Se não é @lid, retorna como está
        if not self._is_meta_business_id(from_number):
            return from_number
        
        logger.debug('Resolvendo @lid: from_number=%s notify_name=%s', from_number, notify_name)
        
        # STAGE 1: Verifica cache preenchido por eventos unread_count anteriores
        cached_phone = self._get_cached_phone_for_lid(from_number)
        if cached_phone:
            logger.info('Resolvido @lid via cache: %s -> %s', from_number, cached_phone)
            return cached_phone
        
        # STAGE 2: Tenta extrair do notifyName (comum em todas as mensagens)
        # IMPORTANTE: Em eventos 'message', o notifyName pode vir como '.' mas ainda assim
        # devemos tentar extrair se houver algum conteúdo
        if notify_name and isinstance(notify_name, str):
            real_phone = self._extract_real_phone_from_notify_name(notify_name)
            if real_phone and len(real_phone) >= 10:
                logger.info('Resolvido @lid via notifyName: %s -> %s', from_number, real_phone)
                # Armazena no cache para uso futuro
                self._lid_to_real_phone_cache[from_number] = real_phone
                return real_phone
        
        # STAGE 3: Tenta em payload['data'][*] - múltiplas localizações
        if payload and isinstance(payload.get('data'), list) and len(payload.get('data')) > 0:
            for idx, item in enumerate(payload.get('data')):
                if not isinstance(item, dict):
                    continue
                
                # Tenta vários campos em data[idx]
                # Em eventos 'unread_count', o número real está em 'name'
                # Em eventos 'message', pode estar em 'name' ou '_data.name'
                locations = [
                    ('name', item.get('name')),  # Unread count usa 'name'
                    ('_data.name', item.get('_data', {}).get('name') if isinstance(item.get('_data'), dict) else None),
                    ('notifyName', item.get('notifyName')),
                    ('_data.notifyName', item.get('_data', {}).get('notifyName') if isinstance(item.get('_data'), dict) else None),
                    ('lastMessage._data.from', item.get('lastMessage', {}).get('_data', {}).get('from') if isinstance(item.get('lastMessage'), dict) else None),
                ]
                
                for loc_name, field_value in locations:
                    if field_value and isinstance(field_value, str):
                        # Verifica se parece ser um número de telefone
                        # Pode ser formato com +, espaços, ou apenas dígitos
                        if any(char.isdigit() for char in field_value):
                            real_phone = self._extract_real_phone_from_notify_name(field_value)
                            if real_phone and len(real_phone) >= 10:
                                logger.info('Resolvido @lid via payload.data[%d].%s: %s -> %s', 
                                          idx, loc_name, from_number, real_phone)
                                # Armazena no cache para uso futuro
                                self._lid_to_real_phone_cache[from_number] = real_phone
                                return real_phone
        
        # STAGE 4: Tenta em payload['payload'] para eventos WEBJS
        if payload and isinstance(payload.get('payload'), dict):
            payload_data = payload['payload']
            # Verifica em notifyName ou campos similares
            notify_candidates = [
                payload_data.get('notifyName'),
                payload_data.get('name'),
                (payload_data.get('_data', {}).get('notifyName') if isinstance(payload_data.get('_data'), dict) else None),
                (payload_data.get('_data', {}).get('name') if isinstance(payload_data.get('_data'), dict) else None),
            ]
            
            for candidate in notify_candidates:
                if candidate and isinstance(candidate, str):
                    real_phone = self._extract_real_phone_from_notify_name(candidate)
                    if real_phone and len(real_phone) >= 10:
                        logger.info('Resolvido @lid via payload.payload: %s -> %s', from_number, real_phone)
                        self._lid_to_real_phone_cache[from_number] = real_phone
                        return real_phone
        
        # STAGE 5: Se não conseguiu extrair número real, retorna @lid original
        # Isso evita None e permite que o lookup tente usar o @lid como fallback
        logger.info('Nao foi possivel resolver @lid, mantendo original: %s', from_number)
        return from_number

    def _clean_phone_number(self, phone_str: str) -> str:
        """Limpa número de telefone removendo tudo exceto dígitos.
        
        Args:
            phone_str: String com número de telefone (pode conter +, espaços, hífens, etc.)
        
        Returns:
            String apenas com dígitos
        """
        if not phone_str:
            return ""
        
        # Remove tudo que não é dígito
        digits = re.sub(r'\D', '', phone_str)
        return digits

    async def _resolve_phone_number_async(self, from_number: Optional[str], notify_name: Optional[str], payload: Dict[str, Any] = None) -> Optional[str]:
        """Versão async de _resolve_phone_number com múltiplas estratégias de resolução.
        
        Quando recebe um @lid, tenta resolver usando várias estratégias:
        1. WAHA API (/api/default/lids/{lid}) - NOVO, mais direto
        2. Cache em memória (preenchido por unread_count events)
        3. Aguarda eventos posteriores para popular cache
        4. Busca em histórico de webhooks
        
        Estratégia completa:
        1. Se é @lid, tenta resolver via API WAHA (primeira e melhor opção)
        2. Se falhar, executa sync resolution que tenta várias fontes
        3. Se ainda é @lid, aguarda 800ms para unread_count event
        4. Tenta novamente
        5. Se ainda não resolvido, aguarda mais 200ms e tenta última vez
        """
        # STAGE 0: Se é @lid, tenta resolver via API do WAHA primeiro
        if self._is_meta_business_id(from_number):
            logger.debug('ASYNC_RESOLVE: Attempting to resolve @lid via WAHA API: %s', from_number)
            waha_resolved = await self._resolve_lid_via_waha_api(from_number)
            if waha_resolved and not self._is_meta_business_id(waha_resolved):
                logger.info('ASYNC_RESOLVE resolved via WAHA API on first attempt: %s -> %s', from_number, waha_resolved)
                return waha_resolved
        
        # STAGE 1: Primeira tentativa de resolução local
        result = self._resolve_phone_number(from_number, notify_name, payload)
        logger.debug('ASYNC_RESOLVE attempt 1 (sync): input=%s result=%s is_lid=%s', 
                    from_number, result, self._is_meta_business_id(result) if result else None)
        
        # Se foi resolvido ou não é @lid, retorna imediatamente
        if result and not self._is_meta_business_id(result):
            logger.info('ASYNC_RESOLVE resolved on attempt 1: %s -> %s', from_number, result)
            return result
        
        # STAGE 2: Se ainda é @lid após primeira tentativa, aguarda cache popular via unread_count
        if result and self._is_meta_business_id(result):
            logger.info('ASYNC_RESOLVE unresolved on attempt 1: %s. Waiting 800ms for unread_count event...', result)
            await asyncio.sleep(0.8)  # Aguarda 800ms (unread_count normalmente chega ~400ms depois de message)
            
            # Segunda tentativa após aguardar
            result_retry = self._resolve_phone_number(from_number, notify_name, payload)
            logger.debug('ASYNC_RESOLVE attempt 2 (after wait): result=%s is_lid=%s', 
                        result_retry, self._is_meta_business_id(result_retry) if result_retry else None)
            
            if result_retry and not self._is_meta_business_id(result_retry):
                logger.info('ASYNC_RESOLVE resolved on attempt 2 (via cache): %s -> %s', from_number, result_retry)
                return result_retry
            
            # STAGE 3: Se ainda não resolvido, aguarda mais um pouco e tenta terceira vez
            if result_retry and self._is_meta_business_id(result_retry):
                logger.debug('ASYNC_RESOLVE still unresolved on attempt 2, waiting 300ms more...')
                await asyncio.sleep(0.3)
                
                result_retry2 = self._resolve_phone_number(from_number, notify_name, payload)
                logger.debug('ASYNC_RESOLVE attempt 3 (final): result=%s is_lid=%s',
                            result_retry2, self._is_meta_business_id(result_retry2) if result_retry2 else None)
                
                if result_retry2 and not self._is_meta_business_id(result_retry2):
                    logger.info('ASYNC_RESOLVE resolved on attempt 3 (via cache): %s -> %s', from_number, result_retry2)
                    return result_retry2
                else:
                    logger.info('ASYNC_RESOLVE could not resolve after 3 attempts, maintaining @lid: %s', result_retry2)
                    return result_retry2
            
            return result_retry
        
        return result

    def _normalize_phone(self, phone: Optional[str]) -> Optional[str]:
        if not phone:
            return None
        # Remove tudo que não for dígito
        digits = re.sub(r"\D", "", str(phone))
        if not digits:
            return None
        # Remove prefixos internacionais comuns como 00
        if digits.startswith('00'):
            digits = digits[2:]
        # Remove zeros à esquerda residuais
        digits = digits.lstrip('0')
        # Normalização mínima: se não tem código de país e tem 10-11 dígitos, assume BR (+55)
        if len(digits) in (10, 11) and not digits.startswith('55'):
            return '55' + digits
        # Se já tem código de país (ex.: 55...), retorna tal qual
        return digits

    def _generate_phone_candidates(self, norm: str):
        """Gera candidatos de telefone para matching.
        Ex.: se norm='554891212945' (faltar o 9 do celular BR), retorna ['554891212945','5548991212945']
        """
        candidates = [norm]
        try:
            if norm and norm.startswith('55') and len(norm) == 12:
                candidate = norm[:4] + '9' + norm[4:]
                if candidate not in candidates:
                    candidates.append(candidate)
        except Exception:
            pass
        return candidates

    def _lookup_phone(self, raw_phone: Optional[str]) -> Dict[str, Any]:
        """Procura primeiro em `users.telefone`, depois em `clientes.telefone`.
        
        Estratégia:
        1. Tenta normalizar e procurar exato
        2. Se é @lid e não encontrou, tenta buscar nos logs recentes
        3. Retorna dict: {"type": "user"|"cliente"|None, ...}
        """
        norm = self._normalize_phone(raw_phone)
        
        # Se é um @lid, tenta resolver primeiro
        if raw_phone and self._is_meta_business_id(raw_phone):
            # Tenta lookup via histórico/cache
            resolved = self._get_cached_phone_for_lid(raw_phone)
            if resolved:
                norm = resolved
                logger.info('_lookup_phone: Resolved @lid %s to %s via cache', raw_phone, resolved)
            else:
                logger.info('_lookup_phone: Could not resolve @lid %s, using numeric part for partial match', raw_phone)
                # Extrai parte numérica do @lid para busca parcial
                lid_numeric = re.sub(r'\D', '', raw_phone)
                if lid_numeric:
                    norm = lid_numeric
        
        if not norm:
            return {"type": None}

        candidates = self._generate_phone_candidates(norm)
        logger.debug('_lookup_phone: raw=%s norm=%s candidates=%s', raw_phone, norm, candidates)

        try:
            with get_db_connection() as conn:
                # Try users for each candidate
                for cand in candidates:
                    try:
                        with conn.cursor() as cur:
                            cur.execute("""
                                SELECT id, cliente_id, telefone
                                FROM users
                                WHERE regexp_replace(coalesce(telefone, ''), '\\D', '', 'g') = %s
                                LIMIT 1
                            """, (cand,))
                            row = cur.fetchone()
                            if row:
                                cliente_id = row[1]
                                whatsapp_webhooks_enabled = None
                                webhooks_enabled = None
                                if cliente_id:
                                    try:
                                        with conn.cursor() as c2:
                                            c2.execute("""
                                                SELECT whatsapp_webhooks_enabled
                                                FROM clientes WHERE id = %s LIMIT 1
                                            """, (cliente_id,))
                                            crow = c2.fetchone()
                                            if crow:
                                                whatsapp_webhooks_enabled = crow[0]
                                                webhooks_enabled = bool(crow[0]) if crow[0] is not None else None
                                            else:
                                                whatsapp_webhooks_enabled = None
                                                webhooks_enabled = None
                                    except Exception:
                                        webhooks_enabled = None
                                        whatsapp_webhooks_enabled = None
                                logger.info('Phone lookup matched user by candidate=%s for raw=%s', cand, raw_phone)
                                return {"type": "user", "user_id": row[0], "cliente_id": cliente_id, "telefone": row[2], "webhooks_enabled": webhooks_enabled, "whatsapp_webhooks_enabled": whatsapp_webhooks_enabled}
                    except Exception:
                        logger.exception('Erro ao buscar usuário por telefone candidate=%s', cand)

                # Try clientes for each candidate
                for cand in candidates:
                    try:
                        with conn.cursor() as cur:
                            cur.execute("""
                                SELECT id, razao_social, telefone, whatsapp_webhooks_enabled
                                FROM clientes
                                WHERE regexp_replace(coalesce(telefone, ''), '\\D', '', 'g') = %s
                                LIMIT 1
                            """, (cand,))
                            crow = cur.fetchone()
                            if crow:
                                logger.info('Phone lookup matched cliente by candidate=%s for raw=%s', cand, raw_phone)
                                return {"type": "cliente", "cliente_id": crow[0], "razao_social": crow[1], "telefone": crow[2], "whatsapp_webhooks_enabled": bool(crow[3]), "webhooks_enabled": bool(crow[3])}
                    except Exception:
                        logger.exception('Erro ao buscar cliente por telefone candidate=%s', cand)
        except Exception as e:
            logger.warning("Erro ao consultar DB para telefone %s: %s", raw_phone, e)

        return {"type": None}

    async def _route_message(self, from_number: Optional[str], text: str, lookup: Dict[str, Any], conversa: Dict[str, Any], event_id: Optional[str] = None, event_type: Optional[str] = None, message_id: Optional[str] = None):
        """Roteia e processa a mensagem inbound usando os serviços internos.
        Heurística de roteamento por palavra-chave (pode ser substituída por um intent classifier).
        """
        if not text:
            return
        # Prevent rapid-duplicate processing: if similar message from same number
        # was processed recently (30s), skip to avoid echo/duplicate replies.
        try:
            # Allow disabling dedupe via env for troubleshooting (set WAHA_DISABLE_DEDUPE=true)
            if os.getenv('WAHA_DISABLE_DEDUPE', 'false').lower() == 'true':
                logger.warning('WAHA_DISABLE_DEDUPE=true — pulando verificação de duplicate window')
            else:
                try:
                    with get_db_connection() as conn:
                        with conn.cursor() as cur:
                            # Prefer strict matching by external_message_id when available
                            if message_id:
                                cur.execute("""
                                    SELECT id, payload::text, status, processing_log, external_message_id
                                    FROM webhook_events
                                    WHERE gateway = %s AND external_message_id = %s
                                      AND received_at > NOW() - INTERVAL '30 seconds'
                                    ORDER BY received_at DESC
                                    LIMIT 1
                                """, (self.gateway_name, message_id))
                            else:
                                # Fallback: match by a short snippet of the text and same from_number context
                                cur.execute("""
                                    SELECT id, payload::text, status, processing_log, external_message_id
                                    FROM webhook_events
                                    WHERE gateway = %s AND payload::text ILIKE %s
                                      AND received_at > NOW() - INTERVAL '30 seconds'
                                    ORDER BY received_at DESC
                                    LIMIT 1
                                """, (self.gateway_name, f"%{text[:120]}%"))
                            dup = cur.fetchone()
                            if dup:
                                dup_id, dup_payload_text, dup_status, dup_processing_log, dup_external = dup[0], dup[1], dup[2], dup[3], dup[4]
                                dup_payload_text = dup_payload_text or ''
                                # Ignore matches that look like acks/fromMe
                                lower_payload = dup_payload_text.lower()
                                if 'fromme' in lower_payload or '"fromme"' in lower_payload or '"me"' in lower_payload and 'id' in lower_payload:
                                    logger.debug('Encontrado evento recente (%s) mas parece ser ack/fromMe — ignorando dedupe', dup_id)
                                else:
                                    # Only skip when previous event was processed or is being processed
                                    if dup_status in ('processed', 'processing', 'received'):
                                        try:
                                            snippet = dup_payload_text[:240]
                                            logger.info('Mensagem similar encontrada recentemente (30s) — pulando processamento para evitar duplicado: %s matched_event=%s status=%s snippet=%s', text[:80], str(dup_id), dup_status, snippet)
                                        except Exception:
                                            logger.info('Mensagem similar encontrada recentemente (30s) — pulando processamento para evitar duplicado: %s matched_event=%s', text[:80], str(dup_id))
                                        return
                except Exception:
                    # non-fatal — proceed if DB check fails
                    logger.exception('Erro ao checar duplicados no DB; prosseguindo com roteamento')
        except Exception:
            # non-fatal — proceed if DB check fails
            pass
        logger.info('Routing message from=%s text=%s event_id=%s message_id=%s lookup=%s', from_number, (text[:80] + '...') if len(text) > 80 else text, event_id, message_id, {k: lookup.get(k) for k in ['type','cliente_id','user_id','webhooks_enabled']})


        # Heurística simples de intenção + suporte a menu/seleção por número
        low = (text or '').strip().lower()
        # If the sender corresponds to a user/client in our DB but the client's account
        # does not have the WhatsApp module enabled, send a one-time informational
        # message and do not proceed further. Only apply to known numbers.
        try:
            if lookup and lookup.get('type') in ('user', 'cliente'):
                try:
                    if lookup.get('type') == 'cliente':
                        # For clientes, prefer explicit whatsapp flag, then generic webhooks flag
                        has_whatsapp = lookup.get('whatsapp_webhooks_enabled') if lookup.get('whatsapp_webhooks_enabled') is not None else lookup.get('webhooks_enabled')
                        if has_whatsapp is False:
                            msg = "Parece que sua conta não tem o módulo de WhatsApp habilitado. Acesse https://site.vendacomconexao.com/ para adquirir o módulo."
                            await waha_outbound_service.send_text(from_number, msg, metadata={'event': 'module_not_enabled'})
                            return
                    else:
                        # lookup type == 'user'
                        # If user is linked to a cliente, check the cliente flags if present
                        if lookup.get('cliente_id'):
                            client_flag = None
                            if 'whatsapp_webhooks_enabled' in lookup and lookup.get('whatsapp_webhooks_enabled') is not None:
                                client_flag = lookup.get('whatsapp_webhooks_enabled')
                            elif 'webhooks_enabled' in lookup and lookup.get('webhooks_enabled') is not None:
                                client_flag = lookup.get('webhooks_enabled')
                            if client_flag is False:
                                msg = "Parece que sua conta não tem o módulo de WhatsApp habilitado. Acesse https://site.vendacomconexao.com/ para adquirir o módulo."
                                await waha_outbound_service.send_text(from_number, msg, metadata={'event': 'module_not_enabled'})
                                return
                        else:
                            # User exists but not linked to a cliente: inform and suggest support/site
                            msg = "Seu usuário não está vinculado a um cliente cadastrado. Se deseja usar o módulo de WhatsApp, acesse https://site.vendacomconexao.com/ ou contate nosso suporte."
                            await waha_outbound_service.send_text(from_number, msg, metadata={'event': 'module_no_cliente'})
                            return
                except Exception:
                    logger.exception('Erro ao checar permissões de módulo; prosseguindo normalmente')
        except Exception:
            logger.exception('Erro ao checar permissões de módulo; prosseguindo normalmente')

        # Saudações curtas (tratamento próprio para evitar respostas genéricas/promocionais)
        greetings = ('oi', 'ola', 'olá', 'bom dia', 'boa tarde', 'boa noite', 'buenos dias', 'oie', 'e ai', 'e aí', 'opa', 'ei', 'eae')

        # Only respond to numbers that are known in our database (user or cliente),
        # unless `WAHA_ALLOW_UNKNOWN_REPLY` is enabled for debugging.
        is_known = bool(lookup and lookup.get('type') in ('user', 'cliente'))
        allow_unknown = os.getenv('WAHA_ALLOW_UNKNOWN_REPLY', 'false').lower() == 'true'
        if not is_known and not allow_unknown:
            # Allow showing the menu and starting known flows (like simulador) even to unknown numbers
            # Also allow continuing an already-started session (awaiting_menu or expecting)
            try:
                if session_id:
                    s_active = await get_session(session_id)
                else:
                    s_active = {}
            except Exception:
                s_active = {}
            logger.info('Unknown lookup early-check from=%s low=%s s_active=%s', from_number, low, {k: s_active.get(k) for k in ['awaiting_menu','expecting']})

            if (low not in ('menu', 'opcoes', 'opções') and 'simul' not in low and low not in ('1','2','3','4') and 'frio' not in low and 'morno' not in low and 'quente' not in low and not s_active.get('awaiting_menu') and not s_active.get('expecting')):
                # Do not respond to other messages from unknown numbers
                return

        # Compute stable session id early so greeting logic can inspect session state
        norm_phone = self._normalize_phone(from_number)
        session_id = None
        if norm_phone:
            try:
                session_id = str(uuid.uuid5(uuid.NAMESPACE_URL, norm_phone))
            except Exception:
                session_id = None
        try:
            # If this looks like a greeting and there is no active simulador/chat flow, respond with concise personalized welcome
            # Use a regex to capture common short greetings and avoid accidental matches
            greeting_pattern = re.compile(rf"^(?:{'|'.join(re.escape(g) for g in greetings)})([!,. ]|$)", re.I)
            if greeting_pattern.search(low):
                # If there's an ongoing session expecting sim flow, skip greeting
                if session_id:
                    s_check = await get_session(session_id)
                    if s_check and s_check.get('expecting'):
                        pass
                    else:
                        try:
                            from app.database.users import get_user_by_id
                            user = None
                            if lookup and lookup.get('type') == 'user' and lookup.get('user_id'):
                                user = get_user_by_id(lookup.get('user_id'))
                            first = (user.get('full_name').split()[0] if user and user.get('full_name') else None) if user else None
                        except Exception:
                            first = None

                        name_part = f"{first}, " if first else ""
                        welcome = f"Olá, {name_part}posso te ajudar a destravar suas vendas. Responda 'menu' para opções."
                        await waha_outbound_service.send_text(from_number, welcome, metadata={'event': 'greeting'})
                        return
        except Exception:
            logger.exception('Erro ao processar saudacao personalizada; prosseguindo normalmente')
        # Compute a stable session id early to support menu flows
        norm_phone_tmp = self._normalize_phone(from_number)
        session_id_tmp = None
        if norm_phone_tmp:
            try:
                session_id_tmp = str(uuid.uuid5(uuid.NAMESPACE_URL, norm_phone_tmp))
            except Exception:
                session_id_tmp = None
        # Support explicit menu/restart commands to show menu and reset flow
        if low in ('menu', 'opcoes', 'opções', 'opções', 'opções', 'reiniciar', 'reset', 'sair', 'cancelar') or low == 'opções':
            try:
                menu_text = (
                    "Olá! Escolha uma opção:\n"
                    "1 - Quebrar Objeção\n"
                    "2 - Gerar Script de Abordagem\n"
                    "3 - Responder Pergunta do Cliente\n"
                    "4 - Consultor de Vendas (aconselhamento prático)\n"
                    "5 - Melhorar mensagem antes de enviar\n\n"
                    "Responda com o número (ex.: 1) ou escreva o que deseja."
                )
                status_code, resp_text = await waha_outbound_service.send_text(from_number, menu_text, metadata={'event': 'menu'})
                logger.info('Menu outbound attempt result=%s body_len=%s for %s', status_code, len(resp_text) if resp_text else 0, from_number)
                # Reset session and set awaiting menu selection
                if session_id_tmp:
                    logger.debug('Resetting session and setting awaiting_menu in session_id=%s', session_id_tmp)
                    await set_session(session_id_tmp, {'awaiting_menu': True, 'menu_ts': time.time(), 'flow': None, 'expecting': None})
            except Exception:
                logger.exception('Erro ao enviar menu via Waha')
            return

        # If we are awaiting a menu selection, handle numeric choices quickly
        try:
            if session_id_tmp:
                s = await get_session(session_id_tmp)
                if s and s.get('awaiting_menu'):
                    # allow either numeric choices or keywords contained in the user's message
                    if any(low == n for n in ('1','2','3','4','5')) or any(k in low for k in ('objec','obje','script','scriptar','script','responder','pergunta','consultor','consultoria','consultor de vendas','melhorar','mensagem')):
                        logger.info('Processing awaiting_menu selection from=%s low=%s', from_number, low)
                        # Clear awaiting_menu and set flow
                        await update_session_key(session_id_tmp, 'awaiting_menu', False)
                        # Option 1: Ask product first, then ask for the objection
                        if low == '1' or 'objec' in low:
                            await update_session_key(session_id_tmp, 'flow', 'objecoes')
                            await update_session_key(session_id_tmp, 'expecting', 'objecao_product')
                            await waha_outbound_service.send_text(from_number, "Qual seu produto ou serviço? Ex: Curso online de marketing digital com certificado, suporte 24/7 e garantia de 30 dias...", metadata={'event':'menu_objecoes_product'})
                            return
                        if low == '2' or 'script' in low or 'gerar' in low:
                            await update_session_key(session_id_tmp, 'flow', 'scripts')
                            await update_session_key(session_id_tmp, 'expecting', 'script_product')
                            await waha_outbound_service.send_text(from_number, "Qual seu produto ou serviço? Ex: Curso online de marketing digital com certificado, suporte 24/7 e garantia de 30 dias...", metadata={'event':'menu_script_product'})
                            return
                        # Option 3: Ask product first, then the question to respond
                        if low == '3' or 'responder' in low or 'pergunta' in low:
                            await update_session_key(session_id_tmp, 'flow', 'responder')
                            await update_session_key(session_id_tmp, 'expecting', 'responder_product')
                            await waha_outbound_service.send_text(from_number, "Qual seu produto ou serviço? Ex: Curso online de marketing digital com certificado, suporte 24/7 e garantia de 30 dias...", metadata={'event':'menu_responder_product'})
                            return
                        if low == '4' or 'consult' in low:
                            await update_session_key(session_id_tmp, 'flow', 'consultor')
                            await update_session_key(session_id_tmp, 'expecting', 'consultor_topic')
                            await waha_outbound_service.send_text(from_number, "Sobre qual tópico de vendas você quer aconselhamento? (ex.: abordagem, preço, follow-up)", metadata={'event':'menu_consultor'})
                            return
                        # Option 5: Improve message before sending
                        if low == '5' or 'melhor' in low or 'mensagem' in low:
                            await update_session_key(session_id_tmp, 'flow', 'melhorar')
                            await update_session_key(session_id_tmp, 'expecting', 'melhorar_input')
                            await waha_outbound_service.send_text(from_number, "Cole aqui a mensagem que deseja melhorar (ex.: 'Olá, tenho um curso com garantia de 30 dias...')", metadata={'event':'menu_melhorar'})
                            return
        except Exception:
            logger.exception('Erro ao processar seleção de menu na sessão')

        module = 'simulador'
        # Numeric shortcuts (1/2/3/4)
        if low == '1' or 'simulador' in low:
            module = 'simulador'
        elif low == '2' or 'objec' in low or 'objeção' in low or 'objecoes' in low:
            module = 'objecoes'
        elif low == '3' or any(k in low for k in ['detector', 'chato', 'empatia', 'ton']):
            module = 'detector'
        elif low == '4' or any(k in low for k in ['conexao', 'pergunta', 'dialogo']):
            module = 'conexao'

        # Prepara um request-like mínimo para permitir render_with_user_context
        fake_request = SimpleNamespace()
        fake_request.state = SimpleNamespace()
        if lookup.get('type') == 'user':
            fake_request.state.current_user_id = lookup.get('user_id')
        else:
            fake_request.state.current_user_id = None

        # session_id was already computed earlier; log it for visibility
        logger.info('Computed session_id=%s from raw from_number=%s normalized=%s', session_id, from_number, norm_phone)

        # Check if there's an active conversational flow in session state
        try:
            if session_id:
                s = await get_session(session_id)
                expecting = s.get('expecting') if s else None
                flow = s.get('flow') if s else None
                if expecting:
                    logger.info('Session %s expecting=%s flow=%s — handling as flow continuation', session_id, expecting, flow)
                    # SIMULADOR flow continuation
                    if expecting == 'simulador_details':
                        logger.info('Handling simulador_details for session=%s from=%s text=%s', session_id, from_number, text)
                        # parse product and temperature
                        temp_m = re.search(r"\b(frio|morno|quente)\b", low)
                        temp = temp_m.group(1) if temp_m else None
                        # product is text minus temp words
                        product = re.sub(r"\b(frio|morno|quente)\b", "", text, flags=re.I).strip()

                        # If either piece is missing, try to recover from stored session partials
                        stored_product = s.get('simulador_product') if s else None
                        stored_temp = s.get('simulador_profile') if s else None
                        if not product and stored_product:
                            logger.debug('Using stored simulador_product from session for session=%s', session_id)
                            product = stored_product
                        if not temp and stored_temp:
                            logger.debug('Using stored simulador_profile from session for session=%s', session_id)
                            temp = stored_temp

                        # If product present but temp missing, persist product and ask for temp
                        if product and not temp:
                            await update_session_key(session_id, 'simulador_product', product)
                            await waha_outbound_service.send_text(from_number, "Com qual tipo de cliente deseja iniciar o treinamento? Frio, Morno ou Quente", metadata={'event':'sim_menu_followup'})
                            # keep expecting
                            await update_session_key(session_id, 'expecting', 'simulador_details')
                            return

                        # If temp present but product missing, persist temp and ask for product
                        if temp and not product:
                            await update_session_key(session_id, 'simulador_profile', temp)
                            await waha_outbound_service.send_text(from_number, "Qual seu produto ou serviço? *", metadata={'event':'sim_menu_followup'})
                            # keep expecting
                            await update_session_key(session_id, 'expecting', 'simulador_details')
                            return

                        # If neither found at all, ask for product (default first prompt)
                        if not product:
                            await waha_outbound_service.send_text(from_number, "Qual seu produto ou serviço? *", metadata={'event':'sim_menu_followup'})
                            return
                        # We have both product and temp — start interactive simulation chat
                        try:
                            # clear expecting and set chat state
                            await update_session_key(session_id, 'expecting', 'simulador_chat')
                            await update_session_key(session_id, 'flow', 'simulador')
                            await update_session_key(session_id, 'simulador_product', product)
                            await update_session_key(session_id, 'simulador_profile', temp)

                            # Send concise intro + start chat marker
                            intro = (
                                "Simule Conversas que Vendem\nDomine a abordagem certa para cada tipo de cliente.\n"
                                "Pratique em um ambiente realista e receba feedback instantâneo.\n\n"
                                f"💬 Chat de Simulação\nChat de simulação para Cliente {temp.capitalize()} iniciado"
                            )
                            await waha_outbound_service.send_text(from_number, intro, metadata={'event':'simulador_started'})

                            # Generate initial client utterance to start the chat
                            try:
                                logger.info('Generating initial client reply via IA for product=%s profile=%s', product, temp)
                                start_prompt = await prompt_service.obter_prompt_simulador_cliente(fake_request, produto_descricao=product, perfil_cliente=temp, mensagem_usuario='iniciar')
                                human_wrapper = ("INSTRUÇÕES: Seja o CLIENTE no diálogo. Responda curto e realista, adequado ao perfil.\n\n")
                                client_reply = await chamar_ia_otimizado(human_wrapper + start_prompt)
                                logger.info('IA client reply: %s', client_reply)
                                # Send client reply prefixed
                                await waha_outbound_service.send_text(from_number, f"CLIENTE: {client_reply}", metadata={'event':'simulador_client_start'})
                                # Save initial pair in history
                                try:
                                    conversa_data = {'mensagens': [{'tipo':'cliente','texto': client_reply}]}
                                    metricas = {'source': 'waha', 'session_id': session_id}
                                    await history_service.save_simulation_secure(usuario_id=lookup.get('user_id') if lookup.get('type')=='user' else None, modulo='simulador', produto_descricao=product, perfil_cliente=temp, conversa=conversa_data, metricas=metricas, session_id=session_id)
                                except Exception:
                                    logger.exception('Erro ao salvar inicio de simulacao via whatsapp')
                            except Exception:
                                logger.exception('Erro ao gerar primeira resposta do cliente no simulador')

                        except Exception:
                            logger.exception('Erro ao iniciar conversa de simulador no fluxo de menu')
                            await waha_outbound_service.send_text(from_number, "Desculpe, não consegui iniciar a simulação agora. Pode tentar novamente?", metadata={'event':'simulador_error'})
                        return

                    # NEW: intermediate step — ask product before objection/question flows
                    if expecting == 'objecao_product':
                        try:
                            product = text.strip()
                            if not product:
                                await waha_outbound_service.send_text(from_number, "Qual seu produto ou serviço? Ex: Curso online de marketing digital com certificado, suporte 24/7 e garantia de 30 dias...", metadata={'event':'menu_objecoes_product'})
                                return
                            await update_session_key(session_id, 'objecoes_product', product)
                            await update_session_key(session_id, 'expecting', 'objecao_detail')
                            await waha_outbound_service.send_text(from_number, "Qual objeção mais te desafia? Ex: 'tá caro', 'vou pensar'", metadata={'event':'menu_objecoes'})
                            return
                        except Exception:
                            logger.exception('Erro no passo objecao_product')
                            await waha_outbound_service.send_text(from_number, "Não entendi. Qual seu produto ou serviço?", metadata={'event':'menu_objecoes_product_error'})
                            return

                    if expecting == 'responder_product':
                        try:
                            product = text.strip()
                            if not product:
                                await waha_outbound_service.send_text(from_number, "Qual seu produto ou serviço? Ex: Curso online de marketing digital com certificado, suporte 24/7 e garantia de 30 dias...", metadata={'event':'menu_responder_product'})
                                return
                            await update_session_key(session_id, 'responder_product', product)
                            await update_session_key(session_id, 'expecting', 'responder_question')
                            await waha_outbound_service.send_text(from_number, "Copia e cola aqui a pergunta do cliente que você quer responder:", metadata={'event':'menu_responder'})
                            return
                        except Exception:
                            logger.exception('Erro no passo responder_product')
                            await waha_outbound_service.send_text(from_number, "Não entendi. Qual seu produto ou serviço?", metadata={'event':'menu_responder_product_error'})
                            return

                    if expecting == 'melhorar_input':
                        try:
                            await update_session_key(session_id, 'expecting', 'melhorar_confirm')
                            await update_session_key(session_id, 'melhorar_original', text)
                            human_wrapper = ("INSTRUÇÕES CRÍTICAS:\n"
                                "- Melhore a mensagem mantendo 100% de veracidade.\n"
                                "- Torne mais CLARA, CURTA e PERSUASIVA (mais conexão com o cliente).\n"
                                "- NÃO invente detalhes ou fatos que não existem.\n"
                                "- Gere 2 variações numeradas: 1️⃣ 2️⃣\n"
                                "- Foco em empatia, reconhecimento da dor/necessidade do cliente.\n"
                                "- Máximo 2-3 linhas por variação.\n\n")
                            respostas = await chamar_ia_otimizado(human_wrapper + text)
                            await update_session_key(session_id, 'melhorar_result', respostas)
                            await waha_outbound_service.send_text(from_number, f"Aqui estão as variações:\n{respostas}", metadata={'event':'melhorar_result'})
                            await waha_outbound_service.send_text(from_number, "Responda '1' ou '2' para enviar essa variação, ou 'menu' para voltar.", metadata={'event':'melhorar_next'})
                            return
                        except Exception:
                            logger.exception('Erro no fluxo melhorar_input')
                            await waha_outbound_service.send_text(from_number, "Não consegui melhorar sua mensagem agora. Pode tentar novamente?", metadata={'event':'melhorar_error'})
                            return

                    if expecting == 'melhorar_confirm':
                        try:
                            lowt = (text or '').strip().lower()
                            if lowt in ('1', '2', 'enviar', 'sim'):
                                # pick the chosen variation (1 or 2, or fallback to first)
                                res = (await get_session(session_id)).get('melhorar_result')
                                chosen = None
                                if res:
                                    parts = [p.strip() for p in re.split(r"\n(?=\d️⃣|\d\))", res) if p.strip()]
                                    if lowt == '2' and len(parts) > 1:
                                        chosen = parts[1]
                                    else:
                                        chosen = parts[0] if parts else res.strip()
                                if chosen:
                                    await waha_outbound_service.send_text(from_number, chosen, metadata={'event':'melhorar_send'})
                                    try:
                                        await history_service.save_simulation_secure(usuario_id=lookup.get('user_id') if lookup.get('type')=='user' else None, modulo='melhorar', produto_descricao='whatsapp', perfil_cliente=None, conversa={'mensagens':[{'tipo':'vendedor','texto':chosen}]}, metricas={'source':'waha'}, session_id=session_id)
                                    except Exception:
                                        logger.exception('Erro ao salvar envio de mensagem melhorada')
                                    # ask about menu
                                    await waha_outbound_service.send_text(from_number, "Quer voltar ao menu? Responda 'menu' para ver opções.", metadata={'event':'post_result_menu_prompt'})
                                    await update_session_key(session_id, 'awaiting_menu', True)
                                    await update_session_key(session_id, 'expecting', None)
                                    return
                                else:
                                    await waha_outbound_service.send_text(from_number, "Não encontrei variações para enviar. Pode tentar novamente?", metadata={'event':'melhorar_send_error'})
                                    await update_session_key(session_id, 'expecting', None)
                                    return
                            elif lowt == 'menu':
                                await waha_outbound_service.send_text(from_number, "Ok — para ver as opções, digite 'menu'.", metadata={'event':'melhorar_back_menu'})
                                await update_session_key(session_id, 'awaiting_menu', True)
                                await update_session_key(session_id, 'expecting', None)
                                return
                            else:
                                await waha_outbound_service.send_text(from_number, "Responda '1' ou '2' para escolher a variação, ou 'menu' para voltar.", metadata={'event':'melhorar_confirm_help'})
                                return
                        except Exception:
                            logger.exception('Erro no passo melhorar_confirm')
                            await waha_outbound_service.send_text(from_number, "Erro ao processar confirmação — pode tentar novamente?", metadata={'event':'melhorar_confirm_error'})
                            return

                    # OBJECOES flow continuation
                    if expecting == 'objecao_detail':
                        try:
                            prod = s.get('objecoes_product') if s and 'objecoes_product' in s else 'produto'
                            prompt = await prompt_service.obter_prompt_quebrar_objecao(fake_request, produto_descricao=prod, objecao=text)
                            human_wrapper = ("INSTRUÇÕES CRÍTICAS:\n"
                                "- Gere 5 abordagens distintas, numeradas de 1 a 5, seguindo esta ordem e rótulos:\n"
                                "  1. [EMPATIA]\n"
                                "  2. [VALOR]\n"
                                "  3. [PROVA_SOCIAL]\n"
                                "  4. [URGENCIA]\n"
                                "  5. [AUTORIDADE]\n"
                                "- Cada linha deve começar com 'N. [RÓTULO]: ' (ex: '1. [EMPATIA]: ...').\n"
                                "- Cada item deve corresponder ao rótulo indicado (não misture rótulos entre itens).\n"
                                "- Seja humano, empático e direto. NÃO invente detalhes sobre o produto.\n"
                                "- Use APENAS as informações que o usuário forneceu. Máximo 2-4 linhas por abordagem.\n\n")
                            respostas = await chamar_ia_otimizado(human_wrapper + prompt)
                            await waha_outbound_service.send_text(from_number, respostas, metadata={'event':'objecoes_result'})
                            await update_session_key(session_id, 'expecting', None)
                            try:
                                await history_service.save_simulation_secure(usuario_id=lookup.get('user_id') if lookup.get('type')=='user' else None, modulo='objecoes', produto_descricao='whatsapp', perfil_cliente=None, conversa={'mensagens':[{'tipo':'cliente','texto':text}], 'respostas': respostas}, metricas={'source':'waha'}, session_id=session_id)
                            except Exception:
                                logger.exception('Erro ao salvar simulação objecoes via whatsapp')
                            # Pergunta se deseja voltar ao menu
                            await waha_outbound_service.send_text(from_number, "Quer voltar ao menu? Responda 'menu' para ver opções.", metadata={'event':'post_result_menu_prompt'})
                            await update_session_key(session_id, 'awaiting_menu', True)
                            return
                        except Exception:
                            logger.exception('Erro ao processar quebra de objeção no fluxo')
                            await waha_outbound_service.send_text(from_number, "Não consegui gerar abordagens agora, você pode descrever a objeção novamente?", metadata={'event':'objecoes_error'})
                            return

                    if expecting == 'script_product':
                        try:
                            product = text.strip()
                            if not product:
                                await waha_outbound_service.send_text(from_number, "Qual seu produto ou serviço? Ex: Curso online de marketing digital com certificado, suporte 24/7 e garantia de 30 dias...", metadata={'event':'menu_script_product'})
                                return
                            await update_session_key(session_id, 'script_product', product)
                            await update_session_key(session_id, 'expecting', 'script_initial')
                            await waha_outbound_service.send_text(from_number, "Cole ou digite seu roteiro de conversa inicial (opcional). Ex: 'Olá, tudo bem? Estou aqui para ajudar você a encontrar o curso ideal.' — responda 'pular' se não tiver.", metadata={'event':'menu_script_initial'})
                            return
                        except Exception:
                            logger.exception('Erro no passo script_product')
                            await waha_outbound_service.send_text(from_number, "Não entendi. Qual seu produto ou serviço?", metadata={'event':'menu_script_product_error'})
                            return

                    if expecting in ('script_context', 'script_initial'):
                        try:
                            # Determine product and script base (script may be optional)
                            script_input = text.strip()
                            prod = s.get('script_product') if s and 'script_product' in s else None
                            # If there's no stored product and user provided text, treat it as product
                            if not prod and script_input and script_input.lower() != 'pular':
                                prod = script_input
                                script_base = script_input
                            else:
                                # If user explicitly provided an initial script after product, use it
                                if script_input and script_input.lower() != 'pular':
                                    script_base = script_input
                                else:
                                    # no initial script: fallback to product (stored or generic)
                                    script_base = prod or 'produto'
                                    prod = prod or 'produto'

                            await update_session_key(session_id, 'expecting', None)
                            await update_session_key(session_id, 'flow', 'scripts')
                            # Use enhanced prompt to generate 3 variations with personalization guidance
                            prompt = await prompt_service.obter_prompt_gerar_variacoes(fake_request, produto_descricao=prod, script_base=script_base, canal='whatsapp', numero_variacoes=3)
                            human_wrapper = ("INSTRUÇÕES CRÍTICAS - GERAR SCRIPTS NATURAIS (NÃO PAREÇA VENDEDOR):\n"
                                "- NÃO diga: 'Tenho um produto', 'Estou oferecendo', 'Quero vender', 'Vem conhecer'.\n"
                                "- DIGA: Reconheça o problema implícito do cliente, crie empatia, mostre que existe solução.\n"
                                "- Use LINGUAGEM DE DESCOBERTA: 'Muita gente tem esse desafio...', 'Descobri que...', 'Comigo ficou diferente'.\n"
                                "- Cada variação deve fazer o cliente ACHAR QUE PRECISA (não pareça empurrando).\n"
                                "- Variações numeradas: 1️⃣ 2️⃣ 3️⃣\n"
                                "- Foco: Empatia → Reconhecimento do problema → Sugestão implícita (sem vender).\n"
                                "- Evite: Adjetivos sobre o produto ('melhor', 'inovador'), benefícios diretos ('você ganhará X').\n"
                                "- Máximo 2-3 linhas por variação. Conversa natural, não pitch.\n"
                                "- NÃO invente detalhes do produto. Use APENAS o que o usuário forneceu.\n\n")
                            respostas = await chamar_ia_otimizado(human_wrapper + prompt)
                            await waha_outbound_service.send_text(from_number, respostas, metadata={'event':'scripts_result'})
                            try:
                                await history_service.save_simulation_secure(usuario_id=lookup.get('user_id') if lookup.get('type')=='user' else None, modulo='scripts', produto_descricao=prod, perfil_cliente=None, conversa={'mensagens':[{'tipo':'cliente','texto':script_base}], 'respostas': respostas}, metricas={'source':'waha'}, session_id=session_id)
                            except Exception:
                                logger.exception('Erro ao salvar geracao de script via whatsapp')
                            # Pergunta se deseja voltar ao menu
                            await waha_outbound_service.send_text(from_number, "Quer voltar ao menu? Responda 'menu' para ver opções.", metadata={'event':'post_result_menu_prompt'})
                            await update_session_key(session_id, 'awaiting_menu', True)
                            return
                        except Exception:
                            logger.exception('Erro no fluxo scripts')
                            await waha_outbound_service.send_text(from_number, "Não consegui gerar scripts agora — pode me dizer mais sobre o produto?", metadata={'event':'scripts_error'})
                            return

                    if expecting == 'responder_question':
                        try:
                            question = text.strip()
                            await update_session_key(session_id, 'expecting', None)
                            prod = s.get('responder_product') if s and 'responder_product' in s else 'produto'
                            prompt = await prompt_service.obter_prompt_responder_pergunta(fake_request, produto_descricao=prod, pergunta=question)
                            human_wrapper = ("INSTRUÇÕES CRÍTICAS:\n"
                                "- Gere 3 respostas diferentes: curta, média e persuasiva.\n"
                                "- Cada resposta deve ser numerada (1️⃣ 2️⃣ 3️⃣) e independente.\n"
                                "- NÃO invente detalhes sobre o produto ou fatos não mencionados.\n"
                                "- Use APENAS o contexto que o usuário forneceu.\n"
                                "- Foco em empatia e mostrar que você entende a pergunta do cliente.\n"
                                "- Máximo 2-3 linhas por resposta. Ágil e direto.\n\n")
                            respostas = await chamar_ia_otimizado(human_wrapper + prompt)
                            await waha_outbound_service.send_text(from_number, respostas, metadata={'event':'responder_result'})
                            try:
                                await history_service.save_simulation_secure(usuario_id=lookup.get('user_id') if lookup.get('type')=='user' else None, modulo='responder', produto_descricao='whatsapp', perfil_cliente=None, conversa={'mensagens':[{'tipo':'cliente','texto':question}], 'respostas': respostas}, metricas={'source':'waha'}, session_id=session_id)
                            except Exception:
                                logger.exception('Erro ao salvar responder pedido via whatsapp')
                            # Pergunta se deseja voltar ao menu
                            await waha_outbound_service.send_text(from_number, "Quer voltar ao menu? Responda 'menu' para ver opções.", metadata={'event':'post_result_menu_prompt'})
                            await update_session_key(session_id, 'awaiting_menu', True)
                            return
                        except Exception:
                            logger.exception('Erro no fluxo responder')
                            await waha_outbound_service.send_text(from_number, "Não consegui gerar respostas agora — pode colar a pergunta novamente?", metadata={'event':'responder_error'})
                            return

                    if expecting == 'consultor_topic':
                        try:
                            topic = text.strip()
                            await update_session_key(session_id, 'expecting', None)
                            prompt = await prompt_service.obter_prompt_consultor_vendas(fake_request, topico=topic, contexto='')
                            human_wrapper = ("INSTRUÇÕES: Seja prático, realista e forneça 2-3 passos acionáveis curtos e uma sugestão de texto para o vendedor. Se for fora de escopo, recuse com uma frase curta.\n\n")
                            resposta = await chamar_ia_otimizado(human_wrapper + prompt)
                            await waha_outbound_service.send_text(from_number, resposta, metadata={'event':'consultor_result'})
                            try:
                                await history_service.save_simulation_secure(usuario_id=lookup.get('user_id') if lookup.get('type')=='user' else None, modulo='consultor', produto_descricao='whatsapp', perfil_cliente=None, conversa={'mensagens':[{'tipo':'cliente','texto':topic}], 'respostas': resposta}, metricas={'source':'waha'}, session_id=session_id)
                            except Exception:
                                logger.exception('Erro ao salvar consultor via whatsapp')
                            # Pergunta se deseja voltar ao menu
                            await waha_outbound_service.send_text(from_number, "Quer voltar ao menu? Responda 'menu' para ver opções.", metadata={'event':'post_result_menu_prompt'})
                            await update_session_key(session_id, 'awaiting_menu', True)
                            return
                        except Exception:
                            logger.exception('Erro no fluxo consultor')
                            await waha_outbound_service.send_text(from_number, "Não consegui processar seu pedido — pode reformular?", metadata={'event':'consultor_error'})
                            return

                    # DETECTOR flow continuation
                    if expecting == 'detector_example':
                        try:
                            prompt = await prompt_service.obter_prompt_detector_analisar(fake_request, produto_descricao='produto', mensagem=text)
                            analise = await chamar_ia_otimizado(prompt)
                            await waha_outbound_service.send_text(from_number, analise, metadata={'event':'detector_result'})
                            await update_session_key(session_id, 'expecting', None)
                            # Pergunta se deseja voltar ao menu
                            await waha_outbound_service.send_text(from_number, "Quer voltar ao menu? Responda 'menu' para ver opções.", metadata={'event':'post_result_menu_prompt'})
                            await update_session_key(session_id, 'awaiting_menu', True)
                            return
                        except Exception:
                            logger.exception('Erro no fluxo detector')
                            await waha_outbound_service.send_text(from_number, "Não consegui analisar agora — pode enviar outra mensagem de exemplo?", metadata={'event':'detector_error'})
                            return

                    # CONEXAO flow continuation
                    if expecting == 'conexao_context':
                        try:
                            prompt = await prompt_service.obter_prompt_gerar_perguntas(fake_request, produto_descricao=text, contexto_cliente='')
                            perguntas = await chamar_ia_otimizado(prompt)
                            await waha_outbound_service.send_text(from_number, perguntas, metadata={'event':'conexao_result'})
                            await update_session_key(session_id, 'expecting', None)
                            # Pergunta se deseja voltar ao menu
                            await waha_outbound_service.send_text(from_number, "Quer voltar ao menu? Responda 'menu' para ver opções.", metadata={'event':'post_result_menu_prompt'})
                            await update_session_key(session_id, 'awaiting_menu', True)
                            return
                        except Exception:
                            logger.exception('Erro no fluxo conexao')
                            await waha_outbound_service.send_text(from_number, "Não consegui gerar perguntas agora — pode me dizer mais sobre o produto?", metadata={'event':'conexao_error'})
                            return

                    # SIMULADOR chat continuation
                    if expecting == 'simulador_chat':
                        try:
                            # Seller message received: generate client reply + feedback
                            product = s.get('simulador_product') if s else None
                            profile = s.get('simulador_profile') if s else None
                            # Treat incoming text as seller utterance
                            seller_text = text
                            # 1) Get client reply
                            client_prompt = await prompt_service.obter_prompt_simulador_cliente(fake_request, produto_descricao=product or 'produto', perfil_cliente=profile or 'morno', mensagem_usuario=seller_text)
                            human_wrapper_client = ("INSTRUÇÕES: Seja o CLIENTE. Responda breve e realista conforme o perfil.\n\n")
                            client_reply = await chamar_ia_otimizado(human_wrapper_client + client_prompt)
                            # send client reply prefixed
                            await waha_outbound_service.send_text(from_number, f"CLIENTE: {client_reply}", metadata={'event':'simulador_client_reply'})

                            # 2) Get feedback on seller message
                            feedback_prompt = await prompt_service.obter_prompt_simulador_feedback(fake_request, produto_descricao=product or 'produto', mensagem_usuario=seller_text)
                            human_wrapper_feedback = ("INSTRUÇÕES: Gere um feedback curto e prático (1-3 linhas) com sugestões de melhoria.\n\n")
                            feedback_text = await chamar_ia_otimizado(human_wrapper_feedback + feedback_prompt)
                            # send feedback prefixed nicely
                            await waha_outbound_service.send_text(from_number, f"📝 Feedback da IA:\n{feedback_text}", metadata={'event':'simulador_feedback'})

                            # Save to history the pair
                            try:
                                conversa_data = {'mensagens': [{'tipo':'vendedor','texto': seller_text}, {'tipo':'cliente','texto': client_reply}], 'feedback': feedback_text}
                                metricas = {'source': 'waha', 'session_id': session_id}
                                await history_service.save_simulation_secure(usuario_id=lookup.get('user_id') if lookup.get('type')=='user' else None, modulo='simulador', produto_descricao=product, perfil_cliente=profile, conversa=conversa_data, metricas=metricas, session_id=session_id)
                            except Exception:
                                logger.exception('Erro ao salvar interacao de simulador no histórico')

                            return
                        except Exception:
                            logger.exception('Erro no fluxo de chat do simulador')
                            await waha_outbound_service.send_text(from_number, "Desculpe, não consegui processar sua mensagem agora. Pode tentar novamente?", metadata={'event':'simulador_chat_error'})
                            return
        except Exception:
            logger.exception('Erro ao verificar estado de sessão para continuação de fluxo')
        try:
            if module == 'simulador':
                # If user explicitly asked to start a simulation but didn't provide product/temp,
                # start a guided sim flow to collect details
                if 'simul' in low and not re.search(r"\b(frio|morno|quente|produto|preço|valor|hamburg)\b", low, re.I):
                    logger.info('Detected guided simulador start for from=%s low=%s session_id=%s', from_number, low, session_id)
                    if session_id:
                        await set_session(session_id, {'flow':'simulador', 'expecting': 'simulador_details'})
                        logger.info('Session after set: %s', await get_session(session_id))
                prompt = await prompt_service.obter_prompt_whatsapp_conversar(
                    fake_request,
                    produto_descricao='produto',
                    mensagem_usuario=text,
                    historico=''
                )

                try:
                    logger.debug('Prompt enviado ao IA (snippet): %s', (prompt[:800] + '...') if len(prompt) > 800 else prompt)
                    # Empacotar instruções "human-like" sem alterar templates
                    human_wrapper = ("INSTRUÇÕES: Seja humano, breve (1-3 frases), empático e consultivo. Use o primeiro nome se aplicável. Evite mencionar 'nossos serviços', 'WhatsApp' como serviço, ou oferecer agendamentos/demonstrações.\n\n")
                    resposta = await chamar_ia_otimizado(human_wrapper + prompt)
                    # If IA simply echoed the user's message, retry once with an explicit instruction
                    def _normalize(s: str) -> str:
                        import re
                        return re.sub(r"\W+", "", (s or '').strip().lower())

                    if _normalize(resposta) == _normalize(text):
                        logger.warning('IA retornou eco da mensagem do usuário; tentando nova chamada com reforço de instrução')
                        retry_prompt = prompt + "\n\nIMPORTANTE: NÃO REPITA A MENSAGEM DO CLIENTE. Responda com valor, faça uma pergunta de seguimento ou ofereça um próximo passo." 
                        try:
                            resposta_retry = await chamar_ia_otimizado(retry_prompt)
                            # If retry produced better result, use it
                            if _normalize(resposta_retry) != _normalize(text):
                                resposta = resposta_retry
                            else:
                                logger.warning('IA ainda ecoou após retry; aplicando fallback consultivo')
                                resposta = f"Olá! Posso ajudar com isso. Pode me dizer mais sobre '{text}'?"
                        except Exception:
                            logger.exception('Retry para IA falhou; aplicando fallback consultivo')
                            resposta = f"Olá! Posso ajudar com isso. Pode me dizer mais sobre '{text}'?"
                except Exception:
                    # fallback simples: usa texto de eco
                    logger.exception('Chamada à IA falhou; usando fallback consultivo')
                    resposta = "Olá! Posso ajudar — conte-me rapidamente o que você precisa e eu já te oriento ou posso simular uma conversa para você." 

                # Pós-processamento: evitar que a resposta sugira agendamento/demonstração
                try:
                    schedule_pattern = re.compile(r"\b(agend(ar|amento)|demonstraç|posso agendar|mostrar.*vendas|ver.*vendas)\b", re.I)
                    if resposta and schedule_pattern.search(resposta):
                        logger.warning('IA sugeriu agendamento/demonstração — aplicando pós-processamento para substituir por simulação curta')
                        try:
                            sim_prompt = await prompt_service.obter_prompt_simular_dialogo(fake_request, produto_descricao='produto', cenario=text, abordagem='venda_curta')
                            sim_text = await chamar_ia_otimizado(sim_prompt)
                            if sim_text and sim_text.strip():
                                resposta = f"Prefiro não agendar demonstrações. Aqui vai uma simulação curta de venda:\n{sim_text.strip()}"
                            else:
                                resposta = "Prefiro não agendar demonstrações. Posso simular uma venda curta — qual é o principal diferencial do seu produto?"
                        except Exception:
                            logger.exception('Erro ao gerar simulação curta como substituição a sugestão de agendamento/demonstração')
                            resposta = "Prefiro não agendar demonstrações. Posso simular uma venda curta: qual é o diferencial do seu hambúrguer?"
                except Exception:
                    logger.exception('Erro no pós-processamento de resposta da IA')

                # Remove potenciais trechos promocionais sobre serviços/automatização
                try:
                    marketing_pattern = re.compile(r"\b(whatsapp\s*business|nossos serv(i|í)cos|servi[cç]os de whatsapp|automatizar suas mensagens|mensagens de marketing|enviar.*mensagens.*marketing|podemos te ajudar|podemos ajudar a automatizar)\b", re.I)
                    if resposta and marketing_pattern.search(resposta):
                        logger.warning('IA gerou texto promocional — removendo e substituindo por resposta consultiva')
                        resposta = "Entendi. Me conte: qual é o principal diferencial do seu produto?"
                except Exception:
                    logger.exception('Erro ao aplicar filtro promocional na resposta da IA')

                # Saudação personalizada: se for um usuário conhecido sem histórico, comece com o primeiro nome
                try:
                    if lookup and lookup.get('type') == 'user' and lookup.get('user_id'):
                        user_id = lookup.get('user_id')
                        # Verifica se há histórico suficiente (memória) para este módulo
                        resumo = await history_service.get_user_summary(user_id, 'whatsapp')
                        if not resumo or resumo.lower().startswith('sem histórico'):
                            try:
                                from app.database.users import get_user_by_id
                                user = get_user_by_id(user_id)
                                first = None
                                if user:
                                    first = (user.get('full_name') or '').strip().split()[0] if user.get('full_name') else None
                                    if not first and user.get('username'):
                                        first = user.get('username').split('_')[0]
                                if first:
                                    # If response doesn't already include greeting with name, prepend it
                                    if not re.search(rf"(^|\W)(oi|olá|ola)[, ]+{re.escape(first)}", resposta, re.I):
                                        logger.info('Adicionando saudacao personalizada com primeiro nome=%s para user_id=%s', first, user_id)
                                        resposta = f"Olá, {first}! {resposta.strip()}"
                            except Exception:
                                logger.exception('Erro ao recuperar primeiro nome do usuário para saudação')
                except Exception:
                    logger.exception('Erro ao aplicar saudacao personalizada')

                # Salva no histórico com usuário se houver, usando o módulo 'whatsapp'
                try:
                    conversa_data = {
                        'mensagens': [
                            {'tipo': 'cliente', 'texto': text},
                            {'tipo': 'vendedor', 'texto': resposta}
                        ]
                    }
                    metricas = {'source': 'waha', 'session_id': session_id}
                    await history_service.save_simulation_secure(
                        usuario_id=lookup.get('user_id') if lookup.get('type') == 'user' else None,
                        modulo='whatsapp',
                        produto_descricao='whatsapp',
                        perfil_cliente='quente',
                        conversa=conversa_data,
                        metricas=metricas,
                        feedback_ia=None,
                        session_id=session_id
                    )

                except Exception as e:
                    logger.warning('Erro ao salvar simulação whatsapp via Waha: %s', e)

                # Envia resposta para o número via Waha, se permitido
                try:
                    allow_send = False
                    if lookup and lookup.get('type') == 'cliente':
                        allow_send = bool(lookup.get('webhooks_enabled'))
                    elif lookup and lookup.get('type') == 'user':
                        # user may reference cliente_id -> webhooks_enabled field fetched in lookup
                        allow_send = bool(lookup.get('webhooks_enabled'))
                    # Default: do not send if unknown lookup
                    outbound_sent = False
                    outbound_status = None
                    rc_text = None
                    rc = None
                    # Debug: possibly force outbound for testing
                    if os.getenv('WAHA_OUTBOUND_ALWAYS_SEND', 'false').lower() == 'true':
                        logger.warning('WAHA_OUTBOUND_ALWAYS_SEND enabled; forcing outbound for %s', from_number)
                        allow_send = True
                    # Log allow_send and lookup info for debugging outbound behavior
                    logger.info('Waha outbound decision allow_send=%s lookup=%s from_number=%s', allow_send, {k: lookup.get(k) for k in ['type','cliente_id','user_id','webhooks_enabled']}, from_number)
                    if allow_send and from_number:
                        # Some payloads include 'whatsapp:+5511...' form; outbound service normalizes
                        metadata = {'event': 'whatsapp_agent_response', 'session_id': session_id}
                        # Build outbound payload for auditing (matches Waha /api/sendText body)
                        num = from_number
                        if num and num.startswith('whatsapp:'):
                            num = num[len('whatsapp:'):]
                        chat_id = num if '@' in num else f"{num}@c.us"
                        outbound_payload = {"session": os.getenv('WAHA_SESSION', 'default'), "chatId": chat_id, "text": resposta, "metadata": metadata}

                        # Mark outbound attempt in DB for auditing (store payload)
                        try:
                            if event_id:
                                # Use 'received' here to avoid DB check constraint violations
                                log_webhook_event(gateway=self.gateway_name, event_type=event_type, payload=conversa, status="received", event_id=event_id, external_message_id=message_id, processing_log=json.dumps({"outbound_attempt":"attempting","outbound_payload":outbound_payload}))
                        except Exception:
                            logger.exception('Falha ao marcar outbound_attempting no webhook_event')

                        logger.info('Attempting outbound send via WAHA_SEND_URL=%s', os.getenv('WAHA_SEND_URL') or os.getenv('WAHA_BASE_URL'))
                        rc, rc_text = await waha_outbound_service.send_text(from_number, resposta, metadata=metadata)
                        outbound_sent = bool(rc and int(rc) >= 200 and int(rc) < 300)
                        outbound_status = rc
                        logger.debug('Waha outbound send result: %s body_len=%s for %s', rc, len(rc_text) if rc_text else 0, from_number)
                        logger.info('Outbound result rc=%s for %s (event_id=%s)', rc, from_number, event_id)
                    else:
                        logger.debug('Outbound not allowed for %s (allow_send=%s)', from_number, allow_send)

                    # Respect WAHA_OUTBOUND_ALWAYS_SEND but do not override an explicit client disable
                    try:
                        force = os.getenv('WAHA_OUTBOUND_ALWAYS_SEND', 'false').lower() == 'true'
                        if force:
                            disabled = False
                            try:
                                if lookup and lookup.get('type') in ('cliente', 'user'):
                                    flag = lookup.get('whatsapp_webhooks_enabled') if 'whatsapp_webhooks_enabled' in lookup else lookup.get('webhooks_enabled')
                                    if flag is False:
                                        disabled = True
                            except Exception:
                                disabled = False
                            if force and not disabled and not allow_send:
                                logger.warning('WAHA_OUTBOUND_ALWAYS_SEND enabled; forcing outbound for %s', from_number)
                                allow_send = True
                            elif force and disabled:
                                logger.info('WAHA_OUTBOUND_ALWAYS_SEND ignored because client explicitly disables WhatsApp for %s', from_number)
                    except Exception:
                        logger.exception('Erro ao aplicar WAHA_OUTBOUND_ALWAYS_SEND guard')
                except Exception as e:
                    logger.exception('Erro ao enviar resposta via Waha: %s', e)
                    outbound_sent = False
                    outbound_status = None
                # Optional: update webhook event with outbound send result
                try:
                    if event_id:
                        try:
                            resp_body = rc_text or ''
                            full_response = resp_body if len(resp_body) <= 10000 else resp_body[:10000] + '... (truncated)'
                            response_info = {
                                "outbound_sent": outbound_sent,
                                "status": outbound_status,
                                "outbound_payload": outbound_payload if 'outbound_payload' in locals() else None,
                                "outbound_response": {"status_code": rc, "body": full_response, "truncated": len(resp_body) > 10000}
                            }
                            log_webhook_event(gateway=self.gateway_name, event_type=event_type, payload=conversa, status="processed", event_id=event_id, external_message_id=message_id, processing_log=json.dumps(response_info, ensure_ascii=False))
                        except Exception:
                            logger.exception('Falha ao atualizar webhook com resultado de envio')
                except Exception:
                    logger.exception('Falha ao atualizar webhook com resultado de envio')

            elif module == 'objecoes':
                prompt = await prompt_service.obter_prompt_quebrar_objecao(
                    fake_request,
                    produto_descricao='whatsapp',
                    objecao=text
                )
                try:
                    respostas_texto = await chamar_ia_otimizado(prompt)
                except Exception:
                    respostas_texto = 'Não consegui gerar abordagens no momento.'

                try:
                    await history_service.save_simulation_secure(
                        usuario_id=lookup.get('user_id') if lookup.get('type') == 'user' else None,
                        modulo='objecoes',
                        produto_descricao='whatsapp',
                        perfil_cliente=None,
                        conversa={'mensagens':[{'tipo':'cliente','texto':text}], 'respostas': respostas_texto},
                        metricas={'source':'waha'},
                        feedback_ia=respostas_texto,
                        session_id=session_id
                    )
                except Exception as e:
                    logger.warning('Erro ao salvar simulação objecoes via Waha: %s', e)

            else:
                # Para outros módulos, salva apenas o inbound por enquanto
                logger.debug('Módulo %s não suportado para processamento automático ainda.', module)

        except Exception as e:
            logger.exception('Erro no roteamento de mensagem Waha: %s', e)

    async def _background_process(self, event_id: str, event_type: str, payload: Dict[str, Any], conversa: Dict[str, Any], metricas: Dict[str, Any], from_number: Optional[str], text: str, message_id: Optional[str], lookup: Optional[Dict[str, Any]] = None):
        """Executa o salvamento da simulação e o roteamento de forma assíncrona.
        Usa `asyncio.to_thread` para chamadas síncronas ao banco quando necessário.
        Atualiza o status do webhook no final (processed / processing_error).
        """
        try:
            logger.info('Background processing start event_id=%s from=%s message_id=%s', event_id, from_number, message_id)
            # Se não recebido do caller, faz lookup (sync) em thread
            if lookup is None:
                lookup = await asyncio.to_thread(self._lookup_phone, from_number)
            logger.info('Background lookup result for %s: %s', from_number, {k: lookup.get(k) for k in ['type','cliente_id','user_id','webhooks_enabled']})

            # Checa permissão do cliente
            override_send = os.getenv('WAHA_OUTBOUND_ALWAYS_SEND', 'false').lower() == 'true'
            if lookup and lookup.get('type') is None:
                # Log unknown inbound numbers into DB for monitoring/operations
                try:
                    from app.database.webhooks_db import log_unknown_whatsapp_contact
                    norm = self._normalize_phone(from_number)
                    # Try to extract sender's display name (notifyName) from various payload shapes
                    remetente = None
                    try:
                        if isinstance(payload, dict):
                            # common Waha/WEBJS shapes
                            pl = payload.get('payload') or payload.get('message')
                            if isinstance(pl, dict):
                                remetente = pl.get('notifyName') or (pl.get('_data') and pl.get('_data').get('notifyName'))
                            # payload.data[0]._data.notifyName pattern
                            if not remetente and isinstance(payload.get('data'), list) and len(payload.get('data')) > 0:
                                first = payload.get('data')[0]
                                if isinstance(first, dict):
                                    remetente = (first.get('_data') and first.get('_data').get('notifyName')) or first.get('notifyName')
                    except Exception:
                        remetente = None

                    await asyncio.to_thread(log_unknown_whatsapp_contact, norm, from_number, event_id, remetente)
                    logger.info('Unknown number logged: %s remetente=%s', norm, remetente)
                except Exception:
                    logger.exception('Erro ao registrar unknown_whatsapp_contact')

            if (not override_send) and ((lookup and lookup.get('type') == 'cliente' and not bool(lookup.get('webhooks_enabled'))) or (lookup and lookup.get('type') == 'user' and not bool(lookup.get('webhooks_enabled')))):
                logger.info("Cliente %s não habilitado para webhooks — background abort.", lookup.get('cliente_id'))
                # Inform the sender that the WhatsApp module is not enabled for their account
                try:
                    msg = "Parece que sua conta não tem o módulo de WhatsApp habilitado. Acesse https://site.vendacomconexao.com/ para adquirir o módulo."
                    await waha_outbound_service.send_text(from_number, msg, metadata={'event': 'module_not_enabled'})
                except Exception:
                    logger.exception('Erro ao enviar mensagem de acesso ao módulo para %s', from_number)
                try:
                    # DB may not accept custom 'webhook_disabled' status in all environments; use 'received' and mark in processing_log
                    log_webhook_event(gateway=self.gateway_name, event_type=event_type, payload=payload, status="received", event_id=event_id, external_message_id=message_id, processing_log='webhook_disabled')
                except Exception:
                    logger.exception("Falha ao atualizar webhook como webhook_disabled (fallback to received)")
                return
            # Salva simulação (sync) em thread pool para não bloquear o loop
            try:
                # Decide qual usuario_id usar: usuário real se existir, senão usuário sistema
                user_to_use = None
                if lookup and lookup.get('type') == 'user' and lookup.get('user_id'):
                    user_to_use = lookup.get('user_id')
                else:
                    user_to_use = self.system_user_id

                if not user_to_use:
                    logger.warning("Nenhum usuário disponível para salvar simulação (nenhum usuário encontrado e system_user_id não configurado)")
                else:
                    # Compute a stable UUID session_id from phone to avoid writing raw JID into UUID column
                    norm_phone_bg = self._normalize_phone(from_number)
                    session_id_bg = None
                    if norm_phone_bg:
                        try:
                            session_id_bg = str(uuid.uuid5(uuid.NAMESPACE_URL, norm_phone_bg))
                        except Exception:
                            session_id_bg = None
                    logger.info('Background save_simulation session_id=%s from=%s normalized=%s', session_id_bg, from_number, norm_phone_bg)
                    await asyncio.to_thread(
                        save_simulation,
                        user_to_use,
                        "whatsapp",
                        "waha_inbound",
                        None,
                        conversa,
                        metricas,
                        None,
                        False,
                        session_id_bg,
                    )
            except Exception as e:
                logger.warning("Não foi possível salvar simulação de webhook (raw) em background: %s", e)

            # Roteia a mensagem (pode chamar IA) — isso já é async
            try:
                await self._route_message(from_number, text, lookup, conversa, event_id=event_id, event_type=event_type, message_id=message_id)
            except Exception as route_err:
                logger.exception("Erro ao rotear mensagem em background: %s", route_err)

            # Se chegou aqui, marca como processed
            try:
                log_webhook_event(gateway=self.gateway_name, event_type=event_type, payload=payload, status="processed", event_id=event_id, external_message_id=message_id)
            except Exception:
                logger.exception("Falha ao marcar webhook como processed")

        except Exception as e:
            logger.exception("Erro no processamento background do webhook: %s", e)
            try:
                log_webhook_event(gateway=self.gateway_name, event_type=event_type, payload=payload, status="processing_error", processing_log=str(e), event_id=event_id, external_message_id=message_id)
            except Exception:
                logger.exception("Falha ao registrar processing_error no webhook")

# Instância global (colocada no final do arquivo para garantir que todos os métodos
# façam parte da classe `WahaService` antes da instanciação)
waha_service = WahaService()