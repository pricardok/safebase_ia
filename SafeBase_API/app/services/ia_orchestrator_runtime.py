import json
import logging
import time
from datetime import datetime
from typing import Any, Dict, Optional, Tuple

import httpx

from app.core.config import settings
from app.db.models import ChaveIA, ProvedorIA
from app.db.session import SessionLocal
from app.services.crypto_manager import crypto_manager


logger = logging.getLogger(__name__)


class IAOrchestratorRuntime:
    def __init__(self) -> None:
        self._provider_rr_index = 0
        self._key_rr_index: Dict[int, int] = {}

    def generate_reply(self, user_message: str, context: Dict[str, Any]) -> str:
        prompt = self._build_prompt(user_message, context)
        result, _provider_name, _key_id = self._call_with_failover(prompt)
        return result or "Sem resposta disponivel no momento."

    def generate_insight(self, query: str, context: Dict[str, Any]) -> Dict[str, Any]:
        prompt = self._build_prompt(query, context)
        result, provider_name, key_id = self._call_with_failover(prompt)
        return {
            "insight": result or "Sem resposta disponivel no momento.",
            "metadata": {
                "provider": provider_name,
                "key_id": key_id,
                "query": query,
            },
        }

    def _build_prompt(self, user_message: str, context: Dict[str, Any]) -> str:
        environment = json.dumps(context.get("environment", {}), ensure_ascii=False)
        notes = json.dumps(context.get("knowledge_notes", []), ensure_ascii=False)
        return (
            "Voce e um DBA junior assistindo o time de operacoes. "
            "Responda com diagnostico, impacto e acoes sugeridas. "
            "Sempre cite a fonte dos dados quando possivel.\n\n"
            f"Contexto do ambiente: {environment}\n"
            f"Notas relevantes: {notes}\n\n"
            f"Pergunta: {user_message}"
        )

    def _call_with_failover(self, prompt: str) -> Tuple[Optional[str], Optional[str], Optional[int]]:
        db = SessionLocal()
        try:
            providers = (
                db.query(ProvedorIA)
                .filter(ProvedorIA.ativo == True)
                .order_by(ProvedorIA.prioridade.asc())
                .all()
            )

            if not providers:
                logger.warning("Nenhum provedor ativo encontrado.")
                return None, None, None

            provider_count = len(providers)
            start_index = self._provider_rr_index % provider_count

            if settings.ia_debug_logs_enabled:
                logger.info("IA debug: providers=%s start_index=%s", provider_count, start_index)

            for offset in range(provider_count):
                provider_index = (start_index + offset) % provider_count
                provider = providers[provider_index]
                provider_name = (provider.nome or "").strip().lower()

                keys = (
                    db.query(ChaveIA)
                    .filter(ChaveIA.provedor_id == provider.id, ChaveIA.ativo == True)
                    .order_by(
                        ChaveIA.prioridade.asc(),
                        ChaveIA.falhas_consecutivas.asc(),
                        ChaveIA.ultima_sucesso.desc(),
                    )
                    .all()
                )

                if not keys:
                    logger.warning("Provedor %s sem chaves ativas.", provider.nome)
                    continue

                if settings.ia_debug_logs_enabled:
                    logger.info(
                        "IA debug: tentando provedor=%s (%s) com %s chaves",
                        provider.nome,
                        provider.id,
                        len(keys),
                    )

                response, key_id = self._try_provider_keys(db, provider, provider_name, keys, prompt)
                if response:
                    self._provider_rr_index = (provider_index + 1) % provider_count
                    if settings.ia_debug_logs_enabled:
                        logger.info(
                            "IA debug: sucesso provedor=%s key_id=%s next_provider_index=%s",
                            provider.nome,
                            key_id,
                            self._provider_rr_index,
                        )
                    db.commit()
                    return response, provider_name, key_id

                if settings.ia_debug_logs_enabled:
                    logger.info("IA debug: falha no provedor=%s, tentando proximo", provider.nome)

            return None, None, None
        finally:
            db.close()

    def _try_provider_keys(
        self,
        db,
        provider: ProvedorIA,
        provider_name: str,
        keys: list,
        prompt: str,
    ) -> Tuple[Optional[str], Optional[int]]:
        key_count = len(keys)
        start_index = self._key_rr_index.get(provider.id, 0) % key_count

        for offset in range(key_count):
            key_index = (start_index + offset) % key_count
            key = keys[key_index]
            api_key = self._resolve_api_key(key)
            if not api_key:
                logger.warning("Chave %s vazia para provedor %s", key.id, provider.nome)
                continue

            try:
                model_name = self._resolve_model_name(provider)
                if settings.ia_debug_logs_enabled:
                    logger.info(
                        "IA debug: provedor=%s key_id=%s model=%s index=%s/%s",
                        provider.nome,
                        key.id,
                        model_name,
                        key_index,
                        key_count - 1,
                    )
                response = self._call_provider(provider_name, api_key, prompt, model_name, provider)
                self._record_key_success(db, key)
                self._key_rr_index[provider.id] = (key_index + 1) % key_count
                return response, key.id
            except Exception as exc:
                logger.warning("Falha no provedor %s com chave %s: %s", provider.nome, key.id, exc)
                self._record_key_failure(db, key)
                continue

        db.commit()
        return None, None

    def _resolve_api_key(self, key: ChaveIA) -> Optional[str]:
        if not key.chave_criptografada:
            return None
        try:
            return crypto_manager.decrypt_key(key.chave_criptografada)
        except Exception as exc:
            logger.warning("Falha ao descriptografar chave %s: %s", key.id, exc)
            return key.chave_criptografada

    def _record_key_success(self, db, key: ChaveIA) -> None:
        key.total_requisicoes += 1
        key.falhas_consecutivas = 0
        key.ultima_sucesso = datetime.utcnow()
        db.add(key)
        if settings.ia_debug_logs_enabled:
            logger.info("IA debug: key_id=%s success recorded", key.id)

    def _record_key_failure(self, db, key: ChaveIA) -> None:
        key.total_requisicoes += 1
        key.total_erros += 1
        key.falhas_consecutivas += 1
        key.ultima_falha = datetime.utcnow()
        if key.falhas_consecutivas >= settings.ia_key_failure_threshold:
            key.ativo = False
            if settings.ia_debug_logs_enabled:
                logger.info(
                    "IA debug: key_id=%s disabled after %s failures",
                    key.id,
                    key.falhas_consecutivas,
                )
        db.add(key)
        if settings.ia_debug_logs_enabled:
            logger.info("IA debug: key_id=%s failure recorded", key.id)

    def _resolve_model_name(self, provider: ProvedorIA) -> str:
        config = self._load_provider_config(provider)
        models = config.get("modelos") if isinstance(config, dict) else None
        if models and isinstance(models, list) and models:
            return models[0]
        return "default-model"

    def _load_provider_config(self, provider: ProvedorIA) -> Dict[str, Any]:
        if not provider.configuracao:
            return {}
        try:
            return json.loads(provider.configuracao)
        except json.JSONDecodeError:
            return {}

    def _call_provider(
        self,
        provider_name: str,
        api_key: str,
        prompt: str,
        model_name: str,
        provider: ProvedorIA,
    ) -> str:
        config = self._load_provider_config(provider)
        start_time = time.monotonic()

        if provider_name == "openai":
            endpoint = config.get("endpoint", "https://api.openai.com/v1/chat/completions")
            payload = {
                "model": model_name,
                "messages": [{"role": "user", "content": prompt}],
                "temperature": 0.7,
                "max_tokens": 1024,
            }
            headers = {"Authorization": f"Bearer {api_key}"}
            result = self._post_chat_completion(endpoint, headers, payload)
            if settings.ia_debug_logs_enabled:
                logger.info("IA debug: openai ok in %.0fms", (time.monotonic() - start_time) * 1000)
            return result

        if provider_name == "gemini":
            endpoint = config.get(
                "endpoint",
                f"https://generativelanguage.googleapis.com/v1beta/models/{model_name}:generateContent",
            )
            payload = {
                "contents": [{"parts": [{"text": prompt}]}],
                "generationConfig": {"temperature": 0.7, "maxOutputTokens": 1024},
            }
            result = self._post_gemini(endpoint, api_key, payload)
            if settings.ia_debug_logs_enabled:
                logger.info("IA debug: gemini ok in %.0fms", (time.monotonic() - start_time) * 1000)
            return result

        if provider_name == "mistral":
            endpoint = config.get("endpoint", "https://api.mistral.ai/v1/chat/completions")
            payload = {
                "model": model_name,
                "messages": [{"role": "user", "content": prompt}],
                "temperature": 0.7,
                "max_tokens": 1024,
            }
            headers = {"Authorization": f"Bearer {api_key}"}
            result = self._post_chat_completion(endpoint, headers, payload)
            if settings.ia_debug_logs_enabled:
                logger.info("IA debug: mistral ok in %.0fms", (time.monotonic() - start_time) * 1000)
            return result

        if provider_name == "azure_openai":
            endpoint = config.get("endpoint")
            deployment = config.get("deployment")
            api_version = config.get("api_version", "2024-02-15-preview")
            if not endpoint or not deployment:
                raise ValueError("Azure OpenAI requer endpoint e deployment no configuracao.")
            url = f"{endpoint}/openai/deployments/{deployment}/chat/completions?api-version={api_version}"
            payload = {
                "messages": [{"role": "user", "content": prompt}],
                "temperature": 0.7,
                "max_tokens": 1024,
            }
            headers = {"api-key": api_key}
            result = self._post_chat_completion(url, headers, payload)
            if settings.ia_debug_logs_enabled:
                logger.info("IA debug: azure_openai ok in %.0fms", (time.monotonic() - start_time) * 1000)
            return result

        raise ValueError(f"Provedor nao suportado: {provider_name}")

    def _post_chat_completion(self, url: str, headers: Dict[str, str], payload: Dict[str, Any]) -> str:
        with httpx.Client(timeout=30) as client:
            response = client.post(url, headers=headers, json=payload)
            if settings.ia_debug_logs_enabled:
                logger.info("IA debug: http status=%s url=%s", response.status_code, url)
            response.raise_for_status()
            data = response.json()
            return data["choices"][0]["message"]["content"].strip()

    def _post_gemini(self, url: str, api_key: str, payload: Dict[str, Any]) -> str:
        with httpx.Client(timeout=30) as client:
            response = client.post(f"{url}?key={api_key}", json=payload)
            if settings.ia_debug_logs_enabled:
                logger.info("IA debug: http status=%s url=%s", response.status_code, url)
            response.raise_for_status()
            data = response.json()
            return data["candidates"][0]["content"]["parts"][0]["text"].strip()
