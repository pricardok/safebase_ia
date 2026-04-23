import os
import logging
import json
from typing import Optional, Tuple
import httpx

logger = logging.getLogger(__name__)


class WahaOutboundService:
    def __init__(self):
        # Prefer explicit WAHA_SEND_URL. Otherwise, try WAHA_BASE_URL + default path
        self.send_url = os.getenv('WAHA_SEND_URL')
        base = os.getenv('WAHA_BASE_URL') or os.getenv('WAHA_WORKER_API_URL')
        if not self.send_url and base:
            # Common default path — configurable in env
            self.send_url = base.rstrip('/') + '/api/send'

        self.api_key = os.getenv('WAHA_API_KEY')
        self.timeout = float(os.getenv('WAHA_OUTBOUND_TIMEOUT', '5'))

    async def send_text(self, to: str, text: str, metadata: Optional[dict] = None) -> Tuple[Optional[int], Optional[str]]:
        """Sends a text message to the Waha worker via HTTP API.

        - `to`: target phone number (prefer E.164, with or without whatsapp: prefix)
        - `text`: textual message body
        - `metadata`: optional dict to include in the payload for tracing
        Returns HTTP status code on success, or None on error.
        """
        if not self.send_url:
            logger.debug('WAHA send URL not configured; skipping outbound send')
            return None

        # Normalize to Waha expected `whatsapp:+country...` if not provided
        if not to.startswith('whatsapp:'):
            to = 'whatsapp:' + to

        headers = {"Content-Type": "application/json"}
        if self.api_key:
            headers['X-API-Key'] = self.api_key
        # WAHA's HTTP API expects different endpoints for different message types.
        # For text messages use POST /api/sendText with body: { session, chatId, text }
        # Normalize 'to' into WAHA chatId format (e.g. 551191234567@c.us)
        num = to
        if num.startswith('whatsapp:'):
            num = num[len('whatsapp:'):]

        # If already contains '@', assume it's a full chatId
        if '@' in num:
            chat_id = num
        else:
            chat_id = f"{num}@c.us"

        payload = {
            "session": os.getenv('WAHA_SESSION', 'default'),
            "chatId": chat_id,
            "text": text
        }
        if metadata:
            payload['metadata'] = metadata

        try:
            async with httpx.AsyncClient(timeout=self.timeout) as client:
                if os.getenv('WAHA_OUTBOUND_DEBUG') == 'true':
                    logger.debug('WAHA outbound payload: %s', payload)
                logger.info('WAHA outbound send to %s (api_key_present=%s)', self.send_url, bool(self.api_key))
                # Choose an appropriate endpoint for text
                send_url = self.send_url
                if send_url.endswith('/api/send'):
                    send_url = send_url[:-len('/api/send')] + '/api/sendText'
                elif not send_url.endswith('/api/sendText'):
                    send_url = send_url.rstrip('/') + '/api/sendText'

                r = await client.post(send_url, headers=headers, json=payload)
                if r.status_code >= 200 and r.status_code < 300:
                    logger.info('Mensagem enviada para %s via Waha (%s): %s', to, self.send_url, r.status_code)
                    logger.debug('WAHA outbound response body: %s', r.text)
                    return r.status_code, r.text
                else:
                    logger.warning('Falha ao enviar mensagem via Waha: %s %s', r.status_code, r.text)
                    return r.status_code, r.text
        except Exception as e:
            logger.exception('Erro ao enviar mensagem via Waha: %s', e)
            return None, str(e)


# Instância global
waha_outbound_service = WahaOutboundService()
