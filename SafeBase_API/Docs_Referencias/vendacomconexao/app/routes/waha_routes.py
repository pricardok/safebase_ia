from fastapi import APIRouter, Request, Header, HTTPException
from typing import Optional
import logging

from app.services.waha_service import waha_service
from app.services.waha_outbound_service import waha_outbound_service
import os

logger = logging.getLogger(__name__)

router = APIRouter()


@router.post("/integracoes/whatsapp/webhook", include_in_schema=False, tags=["Integrações"])
async def waha_webhook(request: Request, x_waha_signature: Optional[str] = Header(None)):
    """Endpoint público para receber webhooks do Waha.
    Recomendações:
    - Configure `WAHA_WEBHOOK_SECRET` no .env com o segredo do Waha (se aplicável)
    - Configure `WAHA_SIGNATURE_HEADER` para o nome do header que a Waha usa (padrão: `X-Waha-Signature`)
    """
    try:
        raw_body = await request.body()
        try:
            payload = await request.json()
        except Exception:
            # fallback para texto simples
            payload = {"raw_body": raw_body.decode('utf-8', errors='replace')}

        headers = dict(request.headers)
        # Debug: log incoming headers to help diagnose signature or header name differences
        logger.debug("Incoming webhook headers: %s", {k: (v if not k.lower().startswith('x-api-key') else v[:8] + '...') for k, v in headers.items()})
        # Inclui explicitamente a assinatura caso o header tenha nome diferente
        if x_waha_signature:
            headers['X-Waha-Signature'] = x_waha_signature

        event_id = await waha_service.handle_webhook(payload=payload, headers=headers, raw_body=raw_body)

        if not event_id:
            # Retornamos 200 para webhooks (evita retries) mas sinalizamos internamente
            return {"status": "received", "event_id": None}

        return {"status": "processed", "event_id": event_id}

    except Exception as e:
        logger.exception("Erro no endpoint Waha webhook: %s", e)
        # Para webhooks externos é comum retornar 200 mesmo em falhas internas, mas aqui devolvemos 500 para facilitar debug inicial
        raise HTTPException(status_code=500, detail="Erro interno no processamento do webhook")


@router.get("/integracoes/whatsapp/webhook", include_in_schema=False, tags=["Integrações"])
async def waha_webhook_health_check():
    """Health check para o webhook Waha (usado pelo provedor para verificar se a URL está viva)."""
    return {"status": "ok", "message": "Webhook endpoint is active"}


@router.post("/integracoes/whatsapp/send_test", include_in_schema=False, tags=["Integrações"])
async def waha_send_test(request: Request, x_api_key: Optional[str] = Header(None)):
    """Endpoint interno para testar envio outbound via Waha worker.
    Protegido por `API_KEY` (cabeçalho `X-API-Key`). Aceita JSON: {"to": "55119...","text":"mensagem"}.
    """
    # Simple auth: require internal API key
    if x_api_key != os.getenv('API_KEY'):
        raise HTTPException(status_code=403, detail="Forbidden")

    body = await request.json()
    to = body.get('to')
    text = body.get('text')
    if not to or not text:
        raise HTTPException(status_code=400, detail="'to' and 'text' required")

    logger.info('Manual outbound test to=%s', to)
    try:
        status_code, resp_text = await waha_outbound_service.send_text(to, text, metadata={'event': 'manual_test'})
        return {"status_code": status_code, "response_body": resp_text}
    except Exception as e:
        logger.exception('Erro ao executar send_test via Waha: %s', e)
        raise HTTPException(status_code=500, detail=str(e))
