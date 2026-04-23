from fastapi import Request
from starlette.middleware.base import BaseHTTPMiddleware
from starlette.responses import Response

from app.core.config import settings
from app.core.security import decode_access_token
from app.services.apikey_service import ApiKeyService


class AuthMiddleware(BaseHTTPMiddleware):
    def __init__(self, app, api_key_service: ApiKeyService):
        super().__init__(app)
        self.api_key_service = api_key_service

    async def dispatch(self, request: Request, call_next):
        auth_state = {"type": "anonymous"}
        authorization = request.headers.get("Authorization", "")
        api_key_header = request.headers.get(settings.api_key_header_name)

        if authorization.startswith("Bearer "):
            token = authorization.split(" ", 1)[1]
            try:
                payload = decode_access_token(token)
                auth_state = {"type": "jwt", "payload": payload}
            except ValueError:
                auth_state = {"type": "invalid"}
        elif api_key_header:
            record = self.api_key_service.validate_key(api_key_header)
            if record:
                auth_state = {"type": "api_key", "record": record}
            elif api_key_header == settings.default_api_key and settings.default_api_key:
                auth_state = {"type": "api_key", "record": {"name": "default", "key": api_key_header, "scopes": ["default"], "is_active": True}}
            else:
                auth_state = {"type": "invalid"}

        request.state.auth = auth_state
        response = await call_next(request)
        return response
