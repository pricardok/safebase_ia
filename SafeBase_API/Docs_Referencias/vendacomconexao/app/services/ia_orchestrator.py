# backend/app/services/ia_orchestrator.py
import hashlib
import logging
import time
from typing import Dict, List, Optional, Tuple
from functools import lru_cache
import json
from datetime import datetime, timedelta

import psycopg2
from app.database import get_db_connection

# Importações necessárias para os provedores de IA
import google.generativeai as genai
import httpx
from openai import AzureOpenAI, OpenAI

logger = logging.getLogger(__name__)

class IAOrchestrator:
    def __init__(self):
        self._cache_ttl = 30 
        self._last_cache_refresh = 0
        self._active_providers_cache = None
        self._active_keys_cache = {}
        self._cache_expiry_times = {} 
        # Cache para a lista global de chaves com TTL de 5 segundos
        self._globally_sorted_keys_cache = None
        self._globally_sorted_keys_cache_time = 0
        self._performance_metrics = {
            'total_requests': 0,
            'cache_hits': 0,
            'provider_failures': 0,
            'avg_response_time': 0.0
        }
    
    def _refresh_cache_if_needed(self):
        """Refresh cache se TTL expirou"""
        current_time = time.time()
        
        # Limpa caches expirados
        self._clean_expired_cache_entries(current_time)
        
        if current_time - self._last_cache_refresh > self._cache_ttl:
            self._active_providers_cache = None
            self._active_keys_cache = {}
            self._last_cache_refresh = current_time
    
    def _clean_expired_cache_entries(self, current_time: float):
        """Limpa entradas de cache expiradas individualmente"""
        # Limpa cache de chaves ativas
        expired_keys = [
            key for key, expiry in self._cache_expiry_times.items()
            if current_time > expiry
        ]
        for key in expired_keys:
            self._active_keys_cache.pop(key, None)
            self._cache_expiry_times.pop(key, None)
    
    def _set_cache_expiry(self, cache_key: str, ttl: int = None):
        """Define tempo de expiração para uma entrada de cache específica"""
        if ttl is None:
            ttl = self._cache_ttl
        self._cache_expiry_times[cache_key] = time.time() + ttl
    
    def _get_system_config(self, chave: str) -> Optional[str]:
        """Obtém configuração do sistema do banco usando gerenciador de contexto"""
        try:
            with get_db_connection() as conn:
                with conn.cursor() as cursor:
                    cursor.execute(
                        "SELECT valor FROM configuracoes_sistema WHERE chave = %s",
                        (chave,)
                    )
                    result = cursor.fetchone()
                    return result[0] if result else None
        except Exception as e:
            logger.error(f"Erro ao obter configuração {chave}: {e}")
            return None
    
    @lru_cache(maxsize=32)
    def _get_cached_config(self, chave: str) -> Optional[str]:
        """Cache para configurações do sistema"""
        return self._get_system_config(chave)
    
    def get_active_providers(self) -> List[Dict]:
        """Retorna provedores ativos ordenados por prioridade"""
        self._refresh_cache_if_needed()
        
        if self._active_providers_cache is not None:
            return self._active_providers_cache
        
        try:
            with get_db_connection() as conn:
                with conn.cursor() as cursor:
                    cursor.execute("""
                        SELECT id, nome, descricao, ordem_prioridade, config_modelo, updated_at
                        FROM ia_provedores 
                        WHERE ativo = true 
                        ORDER BY ordem_prioridade ASC
                    """)
                    providers = []
                    for row in cursor.fetchall():
                        config_modelo_data = row[4] # O índice estava incorreto
                        # Garante que o config_modelo seja um dicionário
                        if isinstance(config_modelo_data, str):
                            try:
                                config_modelo_data = json.loads(config_modelo_data)
                            except json.JSONDecodeError:
                                config_modelo_data = {}
                        providers.append({
                            'id': row[0],
                            'nome': row[1],
                            'descricao': row[2],
                            'ordem_prioridade': row[3],
                            'config_modelo': config_modelo_data,
                            'updated_at': row[5]
                        })
                    
                    self._active_providers_cache = providers
                    return providers
        except Exception as e:
            logger.error(f"Erro ao obter provedores ativos: {e}")
            return []
    
    def get_active_keys_for_provider(self, provedor_id: int) -> List[Dict]:
        """Retorna chaves ativas para um provedor - COM CACHE INDIVIDUAL TTL"""
        self._refresh_cache_if_needed()
        
        cache_key = f"provider_{provedor_id}"
        
        # Verifica se cache existe e não expirou
        if (cache_key in self._active_keys_cache and 
            cache_key in self._cache_expiry_times and
            time.time() < self._cache_expiry_times[cache_key]):
            self._performance_metrics['cache_hits'] += 1
            return self._active_keys_cache[cache_key]
        
        try:
            with get_db_connection() as conn:
                with conn.cursor() as cursor:
                    cursor.execute("""
                        SELECT id, chave_hash, chave_mascarada, descricao, ativa, falhas_consecutivas, ordem_prioridade,
                               ultima_falha, ultima_sucesso, total_requisicoes, total_erros
                        FROM ia_chaves 
                        WHERE provedor_id = %s AND ativa = true
                        ORDER BY 
                            falhas_consecutivas ASC,
                            ultima_sucesso DESC NULLS LAST,
                            created_at ASC
                    """, (provedor_id,))
                    
                    keys = []
                    for row in cursor.fetchall():
                        keys.append({
                            'id': row[0],
                            'chave_hash': row[1],
                            'chave_mascarada': row[2],
                            'descricao': row[3],
                            'ativa': row[4],
                            'falhas_consecutivas': row[5],
                            'ordem_prioridade': row[6], # Nova coluna
                            'ultima_falha': row[7],
                            'ultima_sucesso': row[8],
                            'total_requisicoes': row[9],
                            'total_erros': row[10]
                        })
                    
                    # Atualiza cache com TTL individual
                    self._active_keys_cache[cache_key] = keys
                    self._set_cache_expiry(cache_key, ttl=15)  # TTL menor para chaves
                    
                    return keys
        except Exception as e:
            logger.error(f"Erro ao obter chaves para provedor {provedor_id}: {e}")
            return []

    def get_globally_sorted_keys(self) -> List[Dict]:
        """
        Busca todas as chaves de todos os provedores ativos e as retorna em uma
        lista única, ordenada por um critério de saúde global.
        """
        # Otimização: Cache com TTL de 5 segundos para alta carga
        current_time = time.time()
        if self._globally_sorted_keys_cache and (current_time - self._globally_sorted_keys_cache_time < 5):
            logger.debug("Usando cache global de chaves ordenadas.")
            self._performance_metrics['cache_hits'] += 1
            return self._globally_sorted_keys_cache

        all_keys = []
        active_providers = self.get_active_providers()

        for provider in active_providers:
            provider_id = provider['id']
            provider_name = provider['nome']
            ordem_prioridade = provider['ordem_prioridade']
            config_modelo = provider.get('config_modelo')

            # Isso garante que outras partes do sistema que usam o mesmo cache não sejam afetadas
            # pela renomeação da chave 'ordem_prioridade' para 'key_priority'.
            keys_for_provider = [key.copy() for key in self.get_active_keys_for_provider(provider_id)]
            
            for key in keys_for_provider:
                # Adiciona informações do provedor a cada chave
                key['provider_name'] = provider_name
                key['key_priority'] = key.get('ordem_prioridade', 99) # Usa .get() para segurança
                key['provider_priority'] = ordem_prioridade
                # Garante que provider_config sempre exista para evitar KeyError
                if config_modelo:
                    key['provider_config'] = config_modelo
                else:
                    # Adiciona um fallback seguro se a configuração não estiver no banco
                    key['provider_config'] = {"modelos": ["default-model"]}
                all_keys.append(key)

        # Ordena a lista global de chaves
        # Critérios: Menos falhas, maior prioridade do provedor, último sucesso mais recente
        all_keys.sort(key=lambda k: (
            k['ativa'] is False, # Chaves inativas sempre por último
            k['key_priority'],
            k['falhas_consecutivas'],
            k['provider_priority'],
            -(k['ultima_sucesso'].timestamp() if k['ultima_sucesso'] else 0)
        ))

        # Adiciona health_score e status para o frontend
        keys_with_health_score = []
        for key in all_keys:
            # Cálculo do Health Score (0-100)
            # 50% do peso para falhas, 30% para prioridade, 20% para status ativo
            health_score = 0
            if key['ativa']:
                health_score += 20 
                # Penaliza por falhas (até 50 pontos)
                health_score += max(0, 50 - (key['falhas_consecutivas'] * 10))
            # Bônus por prioridade (até 30 pontos)
            health_score += max(0, 30 - (key['key_priority'] * 5))

            # Determina o status
            status_text = 'inativo'
            if key['ativa']:
                status_text = 'ativo'
            if key['falhas_consecutivas'] >= 5: # Assumindo 5 como limite
                status_text = 'expirado'

            # CORREÇÃO: Monta o dicionário de resposta com todos os campos
            # exigidos pelo `GlobalKeyStatusResponse` e também os campos que o frontend já usa.
            keys_with_health_score.append({
                # Campos para o frontend (conforme documentação)
                "id": key['id'],
                "provedor_nome": key['provider_name'],
                "descricao": key['descricao'],
                "prioridade": key['key_priority'],
                "status": status_text,
                "health_score": int(health_score),
                "last_failure": key['ultima_falha'].isoformat() if key['ultima_falha'] else None,

                # Campos adicionais para o response_model do backend
                "provider_name": key['provider_name'], # Duplicado, mas necessário para o modelo
                "provider_priority": key['provider_priority'],
                "key_priority": key['key_priority'],
                "chave_mascarada": key.get('chave_mascarada', 'N/A'),
                "provider_config": key.get('provider_config'), # ADICIONADO: Garante que a configuração seja passada adiante.
                "ativa": key['ativa'],
                "falhas_consecutivas": key['falhas_consecutivas'],
                "total_requisicoes": key.get('total_requisicoes', 0),
                "total_erros": key.get('total_erros', 0),
            })

        logger.debug(f"Total de chaves globais ordenadas: {len(keys_with_health_score)}")
        
        # Atualiza o cache
        self._globally_sorted_keys_cache = keys_with_health_score
        self._globally_sorted_keys_cache_time = current_time
        return keys_with_health_score
    
    def rotate_key(self, provedor_nome: str) -> Optional[Tuple[int, str]]:
        """Retorna próxima chave ativa para o provedor (round-robin)"""
        try:
            with get_db_connection() as conn:
                with conn.cursor() as cursor:
                    # Primeiro obtém o provedor
                    cursor.execute(
                        "SELECT id FROM ia_provedores WHERE nome = %s AND ativo = true",
                        (provedor_nome,)
                    )
                    provider_result = cursor.fetchone()
                    if not provider_result:
                        return None
                    
                    provedor_id = provider_result[0]
                    active_keys = self.get_active_keys_for_provider(provedor_id)
                    
                    if not active_keys:
                        return None
                    
                    # Ordena por: menos falhas, mais recente sucesso, mais antiga
                    sorted_keys = sorted(active_keys, 
                        key=lambda k: (
                            k['falhas_consecutivas'],
                            -k['total_requisicoes'] if k['ultima_sucesso'] else 999999,
                            k['ultima_sucesso'] or datetime.min
                        )
                    )
                    
                    # Pega a melhor chave disponível
                    best_key = sorted_keys[0]
                    return provedor_id, best_key['chave_hash']
                    
        except Exception as e:
            logger.error(f"Erro no rotate_key para {provedor_nome}: {e}")
            return None
    
    def mark_key_failure(self, chave_id: int, erro: str = ""):
        """Marca falha para uma chave - INVALIDA CACHE RELEVANTE"""
        try:
            # Obtém o limite de falhas dinamicamente da configuração do sistema
            max_failures_str = self._get_cached_config('MAX_FAILURES_BEFORE_DISABLE')
            max_failures = int(max_failures_str) if max_failures_str and str(max_failures_str).isdigit() else 3

            with get_db_connection() as conn:
                with conn.cursor() as cursor:
                    cursor.execute("""
                        UPDATE ia_chaves 
                        SET falhas_consecutivas = falhas_consecutivas + 1,
                            total_erros = total_erros + 1,
                            ultima_falha = NOW(),
                            -- Usa o valor dinâmico para desativar a chave
                            ativa = CASE
                                WHEN (falhas_consecutivas + 1) >= %s THEN false
                                ELSE ativa
                            END
                        WHERE id = %s
                    """, (max_failures, chave_id))
                    conn.commit()
                    
                    # Limpa cache de forma mais inteligente
                    self._invalidate_relevant_caches(chave_id)
                    
                    logger.warning(f"Chave {chave_id} marcada com falha: {erro}")
                    
        except Exception as e:
            logger.error(f"Erro ao marcar falha da chave {chave_id}: {e}")
    
    def mark_key_success(self, chave_id: int):
        """Marca sucesso para uma chave - INVALIDA CACHE RELEVANTE"""
        try:
            with get_db_connection() as conn:
                with conn.cursor() as cursor:
                    cursor.execute("""
                        UPDATE ia_chaves 
                        SET falhas_consecutivas = 0,
                            total_requisicoes = total_requisicoes + 1,
                            ultima_sucesso = NOW()
                        WHERE id = %s
                    """, (chave_id,))
                    conn.commit()
                    
                    # Limpa cache de forma mais inteligente
                    self._invalidate_relevant_caches(chave_id)
                    
                    logger.info(f"Chave {chave_id} marcada com sucesso")
                    
        except Exception as e:
            logger.error(f"Erro ao marcar sucesso da chave {chave_id}: {e}")
    
    def _invalidate_relevant_caches(self, chave_id: int):
        """Invalida apenas caches relevantes para a chave modificada"""
        try:
            with get_db_connection() as conn:
                with conn.cursor() as cursor:
                    cursor.execute(
                        "SELECT provedor_id FROM ia_chaves WHERE id = %s",
                        (chave_id,)
                    )
                    result = cursor.fetchone()
                    if result:
                        provedor_id = result[0]
                        cache_key = f"provider_{provedor_id}"
                        self._active_keys_cache.pop(cache_key, None)
                        self._cache_expiry_times.pop(cache_key, None)
                        logger.info(f"Cache invalidado para provedor ID {provedor_id} devido a mudança na chave {chave_id}.")
        except Exception as e:
            logger.warning(f"Erro ao invalidar cache para chave {chave_id}: {e}")
            # Fallback: limpa tudo
            self._active_keys_cache = {}
            self._cache_expiry_times = {}
    
    def should_use_mock(self) -> bool:
        """Determina se deve usar modo mock"""
        try:
            mock_config = self._get_cached_config('USE_MOCK')
            if mock_config and mock_config.lower() == 'true':
                return True
            
            # Verifica se todos os provedores estão inoperantes
            active_providers = self.get_active_providers()
            if not active_providers:
                return True
            
            for provider in active_providers:
                active_keys = self.get_active_keys_for_provider(provider['id'])
                if active_keys:
                    # Tem pelo menos uma chave ativa para este provedor
                    return False
            
            # Nenhum provedor tem chaves ativas
            return True
        except Exception as e:
            logger.error(f"Erro ao verificar modo mock: {e}")
            # Em caso de erro, usa mock por segurança
            return True
    
    def get_system_status(self) -> Dict:
        """Retorna status completo do sistema de IA - COM MÉTRICAS DE PERFORMANCE"""
        try:
            status = {
                'mock_ativo': self.should_use_mock(),
                'provedores': [], # Renomeado de 'provedores_ativos' para 'provedores'
                'estatisticas': {
                    'total_provedores': 0,
                    'provedores_operacionais': 0,
                    'total_chaves': 0,
                    'chaves_ativas': 0
                },
                'performance_metrics': self._performance_metrics
            }
            
            all_providers = self.get_all_providers() # Nova função para buscar todos
            status['estatisticas']['total_provedores'] = len(all_providers)
            
            for provider in all_providers:
                provider_status = {
                    'id': provider['id'],
                    'nome': provider['nome'],
                    'prioridade': provider['ordem_prioridade'],
                    'chaves': [],
                    'ativo': provider['ativo'] # Adiciona o status do provedor
                }
                
                all_keys = self.get_all_keys_for_provider(provider['id']) # Nova função para buscar todas as chaves
                status['estatisticas']['total_chaves'] += len(all_keys)
                
                operational_keys = 0
                for key in all_keys:
                    key_status = {
                        'id': key['id'],
                        'mascarada': key['chave_mascarada'],
                        'descricao': key['descricao'],
                        'falhas_consecutivas': key['falhas_consecutivas'],
                        'ultima_sucesso': key['ultima_sucesso'],
                        'total_requisicoes': key['total_requisicoes'],
                        'ativa': key['ativa'], # Adiciona o status da chave
                        'operacional': key['ativa'] and key['falhas_consecutivas'] < 5 # Lógica de operacionalidade
                    }
                    provider_status['chaves'].append(key_status)
                    
                    if key_status['operacional']:
                        operational_keys += 1
                        status['estatisticas']['chaves_ativas'] += 1
                
                provider_status['operacional'] = provider['ativo'] and operational_keys > 0
                status['provedores'].append(provider_status)
                
                if provider_status['operacional']:
                    status['estatisticas']['provedores_operacionais'] += 1
            
            # Calcula cache hit ratio
            total_requests = self._performance_metrics['total_requests']
            cache_hits = self._performance_metrics['cache_hits']
            if total_requests > 0:
                status['performance_metrics']['cache_hit_ratio'] = round(cache_hits / total_requests * 100, 2)
            else:
                status['performance_metrics']['cache_hit_ratio'] = 0.0
            
            return status
        except Exception as e:
            logger.error(f"Erro ao obter status do sistema: {e}")
            return {
                'mock_ativo': True, # Renomeado de 'provedores_ativos' para 'provedores'
                'provedores': [],
                'estatisticas': {
                    'total_provedores': 0,
                    'provedores_operacionais': 0,
                    'total_chaves': 0,
                    'chaves_ativas': 0
                },
                'erro': str(e)
            }

    def get_performance_summary(self) -> Dict:
        """
        Calcula e retorna um resumo das métricas de performance, agrupadas por provedor.
        """
        summary = {
            "mode": "production" if not self.should_use_mock() else "mock",
            "total_cache_hits": self._performance_metrics.get('cache_hits', 0),
            "total_requests": self._performance_metrics.get('total_requests', 0),
            "metrics_by_provider": []
        }

        all_providers = self.get_all_providers()
        for provider in all_providers:
            provider_id = provider['id']
            keys_for_provider = self.get_all_keys_for_provider(provider_id)

            total_requests = sum(k.get('total_requisicoes', 0) for k in keys_for_provider)
            total_erros = sum(k.get('total_erros', 0) for k in keys_for_provider)

            success_rate = 0.0
            if total_requests > 0:
                success_rate = (total_requests - total_erros) / total_requests

            # Simulação de cache hit rate e latência por provedor (a ser melhorado no futuro)
            # Por enquanto, usamos as métricas globais como uma aproximação.
            global_total_req = self._performance_metrics.get('total_requests', 1)
            cache_hit_rate = self._performance_metrics.get('cache_hits', 0) / global_total_req if global_total_req > 0 else 0.0
            avg_latency_ms = self._performance_metrics.get('avg_response_time', 0.0) * 1000

            summary["metrics_by_provider"].append({
                "provider": provider['nome'],
                "total_requests": total_requests,
                "success_rate": round(success_rate, 2),
                "cache_hit_rate": round(cache_hit_rate, 2),
                "average_latency_ms": int(avg_latency_ms),
                "latency_p95_ms": int(avg_latency_ms * 1.5) # Simulação de p95
            })

        return summary

    def get_all_providers(self) -> List[Dict]:
        """Retorna TODOS os provedores (ativos e inativos) ordenados por prioridade."""
        try:
            with get_db_connection() as conn:
                with conn.cursor(cursor_factory=psycopg2.extras.DictCursor) as cursor:
                    cursor.execute("""
                        SELECT id, nome, descricao, ordem_prioridade, ativo, config_modelo, updated_at
                        FROM ia_provedores 
                        ORDER BY ordem_prioridade ASC
                    """)
                    providers = [dict(row) for row in cursor.fetchall()]
                    return providers
        except Exception as e:
            logger.error(f"Erro ao obter todos os provedores: {e}")
            return []

    def get_all_keys_for_provider(self, provedor_id: int) -> List[Dict]:
        """Retorna TODAS as chaves (ativas e inativas) para um provedor."""
        try:
            with get_db_connection() as conn:
                with conn.cursor(cursor_factory=psycopg2.extras.DictCursor) as cursor:
                    cursor.execute("""
                        SELECT id, chave_mascarada, descricao, ativa, 
                               falhas_consecutivas, ultima_sucesso, total_requisicoes, total_erros
                        FROM ia_chaves 
                        WHERE provedor_id = %s
                        ORDER BY ordem_prioridade, created_at DESC
                    """, (provedor_id,))
                    keys = [dict(row) for row in cursor.fetchall()]
                    return keys
        except Exception as e:
            logger.error(f"Erro ao obter todas as chaves para o provedor {provedor_id}: {e}")
            return []

    def get_real_key_for_use(self, chave_id: int) -> Optional[str]:
        """Obtém a chave REAL descriptografada para uso"""
        try:
            with get_db_connection() as conn:
                with conn.cursor() as cursor:
                    cursor.execute("""
                        SELECT chave_real_criptografada, iv
                        FROM ia_chaves_seguras 
                        WHERE chave_id = %s
                    """, (chave_id,))
                    
                    result = cursor.fetchone()
                    if result:
                        from app.services.crypto_manager import crypto_manager
                        encrypted_key = result[0]
                        return crypto_manager.decrypt_key(encrypted_key)
                    
                    return None
                    
        except Exception as e:
            logger.error(f"Erro ao obter chave real {chave_id}: {e}")
            return None

    def get_provider_key_for_use(self, provedor_nome: str) -> Optional[Tuple[int, str]]:
        """Obtém chave REAL para uso (ID da chave + chave real)"""
        try:
            with get_db_connection() as conn:
                with conn.cursor() as cursor:
                    # Obtém provedor
                    cursor.execute(
                        "SELECT id FROM ia_provedores WHERE nome = %s AND ativo = true",
                        (provedor_nome,)
                    )
                    provider_result = cursor.fetchone()
                    if not provider_result:
                        return None
                    
                    provedor_id = provider_result[0]
                    
                    # Busca chaves ativas ordenadas por performance
                    cursor.execute("""
                        SELECT id, falhas_consecutivas
                        FROM ia_chaves 
                        WHERE provedor_id = %s AND ativa = true
                        ORDER BY 
                            falhas_consecutivas ASC,
                            ultima_sucesso DESC NULLS LAST
                        LIMIT 1
                    """, (provedor_id,))
                    
                    key_result = cursor.fetchone()
                    if key_result:
                        chave_id = key_result[0]
                        chave_real = self.get_real_key_for_use(chave_id)
                        
                        if chave_real:
                            return chave_id, chave_real
                    logger.warning(f"Nenhuma chave operacional encontrada para o provedor {provedor_nome}.")
                    
                    return None
                    
        except Exception as e:
            logger.error(f"Erro ao obter chave para {provedor_nome}: {e}")
            return None

    def record_request_metrics(self, response_time: float, cache_hit: bool = False):
        """Registra métricas de performance"""
        self._performance_metrics['total_requests'] += 1
        if cache_hit:
            self._performance_metrics['cache_hits'] += 1
        
        # Atualiza média móvel do tempo de resposta
        current_avg = self._performance_metrics['avg_response_time']
        total_reqs = self._performance_metrics['total_requests']
        
        if total_reqs == 1:
            self._performance_metrics['avg_response_time'] = response_time
        else:
            # Média móvel exponencial
            alpha = 0.1  # Fator de suavização
            self._performance_metrics['avg_response_time'] = (
                alpha * response_time + (1 - alpha) * current_avg
            )

    def reset_performance_metrics(self) -> bool:
        """Reseta as métricas de performance em memória e no banco de dados."""
        try:
            # Reseta métricas em memória
            self._performance_metrics = {
                'total_requests': 0, 'cache_hits': 0, 'provider_failures': 0, 'avg_response_time': 0.0
            }
            # Reseta métricas no banco de dados
            with get_db_connection() as conn:
                with conn.cursor() as cursor:
                    cursor.execute("UPDATE ia_chaves SET falhas_consecutivas = 0, total_requisicoes = 0, total_erros = 0, ultima_falha = NULL, ultima_sucesso = NULL")
                    conn.commit()
            logger.info("Métricas de performance da IA resetadas com sucesso.")
            return True
        except Exception as e:
            logger.error(f"Erro ao resetar métricas de performance: {e}")
            return False

    def update_provider(self, provedor_id: int, updates: Dict) -> bool:
        """Atualiza configurações de um provedor IA"""
        try:
            with get_db_connection() as conn:
                with conn.cursor() as cursor:
                    # Constrói a query dinamicamente baseada nos campos fornecidos
                    set_clauses = []
                    params = []
                    
                    if 'ativo' in updates:
                        set_clauses.append("ativo = %s")
                        params.append(updates['ativo'])
                    
                    if 'ordem_prioridade' in updates:
                        set_clauses.append("ordem_prioridade = %s")
                        params.append(updates['ordem_prioridade'])
                    
                    if 'config_modelo' in updates:
                        set_clauses.append("config_modelo = %s")
                        params.append(json.dumps(updates['config_modelo']))
                    
                    if not set_clauses:
                        logger.warning(f"Nenhum campo válido para atualizar no provedor {provedor_id}")
                        return False
                    
                    set_clause = ", ".join(set_clauses)
                    params.append(provedor_id)
                    
                    cursor.execute(f"""
                        UPDATE ia_provedores 
                        SET {set_clause}, updated_at = CURRENT_TIMESTAMP
                        WHERE id = %s
                        RETURNING id
                    """, params)
                    
                    result = cursor.fetchone()
                    conn.commit()
                    
                    if result:
                        # Limpa cache relevante
                        self._active_providers_cache = None
                        logger.info(f"Provedor {provedor_id} atualizado: {updates}")
                        return True
                    
                    return False
                    
        except Exception as e:
            logger.error(f"Erro ao atualizar provedor {provedor_id}: {e}")
            return False

    def create_provider(self, nome: str, ordem_prioridade: int, ativo: bool, config_modelo: Dict) -> Optional[int]:
        """Cria um novo provedor de IA."""
        try:
            with get_db_connection() as conn:
                with conn.cursor() as cursor:
                    cursor.execute("""
                        INSERT INTO ia_provedores (nome, ordem_prioridade, ativo, config_modelo, descricao)
                        VALUES (%s, %s, %s, %s, %s)
                        ON CONFLICT (nome) DO NOTHING
                        RETURNING id
                    """, (nome, ordem_prioridade, ativo, json.dumps(config_modelo), f"Provedor {nome}"))
                    
                    result = cursor.fetchone()
                    conn.commit()

                    if result:
                        self._active_providers_cache = None # Invalida o cache de provedores
                        logger.info(f"Provedor '{nome}' criado com sucesso.")
                        return result[0]
                    return None # Conflito de nome, nada foi criado
        except Exception as e:
            logger.error(f"Erro ao criar provedor '{nome}': {e}")
            return None

    def add_key(self, provedor_id: int, chave_real: str, descricao: str = None, ordem_prioridade: int = 1) -> Optional[int]:
        try:
            from app.services.key_manager import key_manager
            from app.services.crypto_manager import crypto_manager
            
            # Busca o nome do provedor para a validação, que é a causa do erro
            provider_name = None
            with get_db_connection() as conn:
                with conn.cursor() as cursor:
                    cursor.execute("SELECT nome FROM ia_provedores WHERE id = %s", (provedor_id,))
                    result = cursor.fetchone()
                    if result:
                        provider_name = result[0]
            
            if not provider_name:
                logger.error(f"Provedor com ID {provedor_id} não encontrado para adicionar chave.")
                return None

            # Valida formato da chave
            if not key_manager.validate_key_format(provider_name, chave_real):
                logger.error(f"Formato de chave inválido para provedor {provider_name} (ID: {provedor_id})")
                return None
            
            # Gera hash e versão mascarada
            chave_hash = key_manager.hash_key(chave_real)
            chave_mascarada = key_manager.mask_key(chave_real)
            
            # Criptografa chave real
            chave_criptografada = crypto_manager.encrypt_key(chave_real)
            
            with get_db_connection() as conn:
                with conn.cursor() as cursor:
                    # Insere metadados da chave
                    cursor.execute("""
                        INSERT INTO ia_chaves 
                        (provedor_id, chave_hash, chave_mascarada, descricao, ativa, ordem_prioridade)
                        VALUES (%s, %s, %s, %s, true, %s)
                        RETURNING id
                    """, (provedor_id, chave_hash, chave_mascarada, descricao, ordem_prioridade))
                    
                    chave_id = cursor.fetchone()[0]
                    
                    # Insere chave real criptografada
                    cursor.execute("""
                        INSERT INTO ia_chaves_seguras 
                        (chave_id, chave_real_criptografada, iv)
                        VALUES (%s, %s, %s)
                    """, (chave_id, chave_criptografada['encrypted'], chave_criptografada['iv']))
                    
                    conn.commit()
                    
                    # Limpa caches relevantes
                    cache_key = f"provider_{provedor_id}"
                    self._active_keys_cache.pop(cache_key, None)
                    self._cache_expiry_times.pop(cache_key, None)
                    
                    logger.info(f"✅ Nova chave adicionada para provedor {provedor_id}: {chave_mascarada}")
                    return chave_id
                    
        except Exception as e:
            logger.error(f"Erro ao adicionar chave para provedor {provedor_id}: {e}")
            return None

    def toggle_key(self, chave_id: int) -> bool:
        """Ativa/desativa uma chave IA específica"""
        try:
            with get_db_connection() as conn:
                with conn.cursor() as cursor:
                    # Primeiro obtém o estado atual
                    cursor.execute("""
                        SELECT ativa, provedor_id FROM ia_chaves WHERE id = %s
                    """, (chave_id,))
                    
                    result = cursor.fetchone()
                    if not result:
                        logger.error(f"Chave {chave_id} não encontrada")
                        return False
                    
                    atual_ativa, provedor_id = result
                    nova_ativa = not atual_ativa
                    
                    # Atualiza o estado
                    cursor.execute("""
                        UPDATE ia_chaves 
                        SET ativa = %s, updated_at = CURRENT_TIMESTAMP
                        WHERE id = %s
                        RETURNING id
                    """, (nova_ativa, chave_id))
                    
                    result = cursor.fetchone()
                    conn.commit()
                    
                    if result:
                        # Limpa cache relevante
                        cache_key = f"provider_{provedor_id}"
                        self._active_keys_cache.pop(cache_key, None)
                        self._cache_expiry_times.pop(cache_key, None)
                        
                        status = "ativada" if nova_ativa else "desativada"
                        logger.info(f"Chave {chave_id} {status}")
                        return True
                    
                    return False
                    
        except Exception as e:
            logger.error(f"Erro ao alternar chave {chave_id}: {e}")
            return False

    def delete_provider(self, provedor_id: int) -> bool:
        """Exclui um provedor e todas as suas chaves associadas."""
        try:
            with get_db_connection() as conn:
                with conn.cursor() as cursor:
                    # A tabela ia_chaves_seguras tem ON DELETE CASCADE, então não precisamos nos preocupar com ela.
                    # 1. Excluir chaves associadas ao provedor
                    cursor.execute("DELETE FROM ia_chaves WHERE provedor_id = %s", (provedor_id,))
                    # 2. Excluir o provedor
                    cursor.execute("DELETE FROM ia_provedores WHERE id = %s", (provedor_id,))
                    conn.commit()
                    
                    if cursor.rowcount > 0:
                        self._active_providers_cache = None # Invalida o cache de provedores
                        logger.info(f"Provedor {provedor_id} e suas chaves foram excluídos com sucesso.")
                        return True
                    return False # Provedor não encontrado
        except Exception as e:
            logger.error(f"Erro ao excluir o provedor {provedor_id}: {e}")
            return False

    def get_detailed_keys(self) -> List[Dict]:
        """Obtém informações detalhadas de todas as chaves IA"""
        try:
            with get_db_connection() as conn:
                with conn.cursor() as cursor:
                    cursor.execute("""
                        SELECT 
                            c.id,
                            p.nome as provedor_nome,
                            c.chave_mascarada,
                            c.descricao,
                            c.ativa,
                            c.falhas_consecutivas,
                            c.ultima_sucesso,
                            c.ultima_falha,
                            c.total_requisicoes,
                            c.total_erros,
                            c.created_at,
                            c.ordem_prioridade
                        FROM ia_chaves c
                        JOIN ia_provedores p ON c.provedor_id = p.id
                        ORDER BY p.nome, c.created_at DESC
                    """)
                    
                    chaves = []
                    for row in cursor.fetchall():
                        chaves.append({
                            'id': row[0],
                            'provedor_nome': row[1],
                            'chave_mascarada': row[2],
                            'descricao': row[3],
                            'ativa': row[4],
                            'falhas_consecutivas': row[5],
                            'ultima_sucesso': row[6],
                            'ultima_falha': row[7],
                            'total_requisicoes': row[8],
                            'total_erros': row[9],
                            'created_at': row[10],
                            'ordem_prioridade': row[11],
                            'operacional': row[5] == 0 and row[4]  
                        })
                    
                    return chaves
        except Exception as e:
            logger.error(f"Erro ao obter chaves detalhadas: {e}")
            return []

    async def call_provider(self, provider_name: str, api_key: str, prompt: str, config_modelo: Optional[Dict] = None) -> Optional[str]:
        """
        Chama o provedor de IA específico com a chave fornecida.
        Esta função centraliza a lógica de comunicação com cada API de IA.
        """
        resultado = None
        
        # Determina o modelo a ser usado a partir da configuração do banco
        model_name = "default-model" # Fallback
        if config_modelo and isinstance(config_modelo, dict) and "modelos" in config_modelo and config_modelo["modelos"]:
            model_name = config_modelo["modelos"][0] # Usa o primeiro modelo da lista
        else:
            logger.warning(f"Nenhuma configuração de modelo encontrada para {provider_name}. Usando fallback.")

        try:
            if provider_name == 'gemini':
                genai.configure(api_key=api_key)
                model = genai.GenerativeModel(model_name)
                response = model.generate_content(
                    prompt,
                    generation_config={
                        "temperature": 0.7,
                        "top_p": 0.8,
                        "max_output_tokens": 1024,
                    }
                )
                resultado = response.text.strip()

            elif provider_name == 'mistral':
                headers = {
                    "Authorization": f"Bearer {api_key}",
                    "Content-Type": "application/json"
                }
                payload = {
                    "model": model_name,
                    "messages": [{"role": "user", "content": prompt}],
                    "temperature": 0.7,
                    "max_tokens": 1024,
                    "top_p": 0.8
                }
                async with httpx.AsyncClient() as client:
                    response = await client.post(
                        "https://api.mistral.ai/v1/chat/completions",
                        headers=headers,
                        json=payload,
                        timeout=30
                    )
                    response.raise_for_status() # Lança exceção para códigos de erro HTTP
                    result = response.json()
                    resultado = result["choices"][0]["message"]["content"].strip()

            elif provider_name == 'openai':
                client = OpenAI(api_key=api_key)
                response = client.chat.completions.create(
                    model=model_name,
                    messages=[{"role": "user", "content": prompt}],
                    temperature=0.7,
                    max_tokens=1024
                )
                resultado = response.choices[0].message.content.strip()

            # Adicione aqui a lógica para outros provedores como 'azure' e 'huggingface' se necessário

            else:
                logger.error(f"Provedor '{provider_name}' não implementado em call_provider.")
                raise NotImplementedError(f"Provedor '{provider_name}' não implementado.")

            if not resultado:
                raise ValueError("A resposta do provedor de IA estava vazia.")

            return resultado

        except Exception as e:
            logger.error(f"Falha ao chamar o provedor '{provider_name}': {e}")
            # A exceção será capturada pela função chamar_ia_otimizado em dependencies.py
            raise

# Instância global do orquestrador
logger.info("Instância do IAOrchestrator criada.")
ia_orchestrator = IAOrchestrator()
