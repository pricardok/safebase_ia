from fastapi import APIRouter, Depends, Request, status, Body
import logging
from typing import Dict, Any

from app.auth_integrations import validate_integration_api_key
from app.services.integration_service import integration_service

logger = logging.getLogger(__name__)

router = APIRouter(
    prefix="/integracoes",
    tags=["Integrações"],
    dependencies=[Depends(validate_integration_api_key)]
)

@router.post(
    "/pagamentos",
    summary="Webhook para Notificações de Pagamento",
    description="Recebe e processa notificações de gateways de pagamento (ex: Kiwify).",
    status_code=status.HTTP_200_OK
)
async def handle_payment_webhook(payload: Dict[str, Any] = Body(...)):
    """
    Endpoint para receber webhooks de pagamento. A autenticação é feita via
    `X-API-KEY-INTEGRACOES` no header ou `api_key` na query.
    """
    result = await integration_service.process_payment_webhook(payload)
    return result