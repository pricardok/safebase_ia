from fastapi import Request, Depends, HTTPException, status, Security
from fastapi.security import APIKeyHeader, APIKeyQuery
import os
import hashlib
import logging

logger = logging.getLogger(__name__)

API_KEY_INTEGRACOES = os.getenv("API_KEY_INTEGRACOES", "RPf8d8CWL7eEf7JIa64G86Q7NEpq5c2k")

def _mask_key(api_key: str) -> str:
    """Mascara uma chave de API para exibição segura em logs."""
    if not api_key or len(api_key) < 8:
        return "key_invalida"
    return f"{api_key[:4]}...{api_key[-4:]}"

api_key_query = APIKeyQuery(name="validation_code", auto_error=False)
api_key_header = APIKeyHeader(name="X-API-KEY-INTEGRACOES", auto_error=False)

async def validate_integration_api_key(
    key_from_query: str = Security(api_key_query),
    key_from_header: str = Security(api_key_header),
):
    """
    Valida a API Key para o módulo de integrações.
    Aceita a chave via query parameter 'validation_code' ou header 'X-API-KEY-INTEGRACOES'.
    """
    if key_from_query == API_KEY_INTEGRACOES:
        logger.info("Autenticação de integração via Query Parameter bem-sucedida.")
        return
    if key_from_header == API_KEY_INTEGRACOES:
        logger.info("Autenticação de integração via Header bem-sucedida.")
        return

    # Log de segurança aprimorado com mascaramento
    provided_key = key_from_header or key_from_query
    log_message = f"Falha na autenticação de integração. Chave fornecida: {_mask_key(provided_key)}" if provided_key else "Falha na autenticação de integração. Nenhuma chave fornecida."
    logger.warning(log_message)

    raise HTTPException(
        status_code=status.HTTP_401_UNAUTHORIZED,
        detail="API Key de integração inválida ou não fornecida."
    )