# /app/routes/hooks_routes.py
from fastapi import APIRouter, Request, Depends, HTTPException, status, Body
import logging
from typing import Dict, Any
import hmac
import hashlib
import os

from app.services.webhook_service import webhook_service

logger = logging.getLogger(__name__)

def _validate_kiwify_signature_old_nao_usar(payload: bytes, signature: str) -> bool:
    """Valida a assinatura do webhook da Kiwify usando HMAC-SHA1."""
    secret = os.getenv("KIWIFY_WEBHOOK_SECRET")
    if not secret:
        logger.error("A variável de ambiente KIWIFY_WEBHOOK_SECRET não está configurada.")
        return False
    
    calculated_signature = hmac.new(secret.encode('utf-8'), payload, hashlib.sha1).hexdigest()
    return hmac.compare_digest(calculated_signature, signature)

def _validate_kiwify_signature(payload: bytes, signature: str) -> bool:
    """Valida a assinatura do webhook da Kiwify usando HMAC-SHA1."""
    secret = os.getenv("KIWIFY_WEBHOOK_SECRET")
    if not secret:
        logger.error("A variável de ambiente KIWIFY_WEBHOOK_SECRET não está configurada.")
        return False
    
    # ✅ CORREÇÃO: Usar decode() para string e garantir encoding UTF-8
    payload_str = payload.decode('utf-8')
    calculated_signature = hmac.new(
        secret.encode('utf-8'), 
        payload_str.encode('utf-8'),  # ✅ ENCODE DA STRING
        hashlib.sha1
    ).hexdigest()
    
    logger.info(f"Signature recebida: {signature}")
    logger.info(f"Signature calculada: {calculated_signature}")
    logger.info(f"Payload: {payload_str}")
    
    return hmac.compare_digest(calculated_signature, signature)

router = APIRouter(
    prefix="/hooks",
    tags=["Webhooks"],
    # A dependência de autenticação foi removida para usar a validação de signature da Kiwify.
    include_in_schema=False # Oculta do Swagger para evitar confusão com a nova rota /integracoes
)

@router.post(
    "/gateway-events",
    summary="[LEGADO] Webhook para Eventos de Gateways de Pagamento",
    description="Recebe e processa eventos de webhooks da Kiwify com validação de signature HMAC-SHA1.",
    status_code=status.HTTP_200_OK
)
async def handle_gateway_webhook(request: Request):
    """
    Endpoint específico para webhooks da Kiwify. Valida a assinatura antes de processar.
    Retorna 200 OK mesmo em caso de erro de processamento para evitar retries do gateway.
    """
    body_bytes = await request.body()
    signature = request.query_params.get("signature")

    if not signature:
        logger.warning("Requisição de webhook da Kiwify recebida sem 'signature'.")
        raise HTTPException(status_code=status.HTTP_400_BAD_REQUEST, detail="Signature de validação ausente.")

    if not _validate_kiwify_signature(body_bytes, signature):
        logger.warning("Requisição de webhook da Kiwify com assinatura inválida.")
        raise HTTPException(status_code=status.HTTP_400_BAD_REQUEST, detail="Assinatura inválida.")

    try:
        payload = await request.json()
        processing_result = await webhook_service.process_kiwify_event(payload)
        return {"status": "success", "message": "Webhook processado com sucesso.", "details": processing_result}
    except Exception:
        # O erro já foi logado e registrado no banco pelo webhook_service.
        # Apenas garantimos o retorno 200 OK para a Kiwify.
        logger.critical("Falha crítica no processamento do webhook, mas retornando 200 OK para o gateway.")
        # Retorna 200 OK para o gateway não tentar reenviar, mesmo que o processamento interno falhe.
        return {"status": "error", "message": "Webhook recebido, mas falhou ao processar internamente."}