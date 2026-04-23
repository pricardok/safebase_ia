# backend/app/auth.py
from fastapi import HTTPException, Security, Depends
from fastapi.security import APIKeyHeader
from starlette.status import HTTP_403_FORBIDDEN
import os
from dotenv import load_dotenv

load_dotenv()

# Configuração
API_KEY_NAME = "X-API-Key"
API_KEY = os.getenv("API_KEY")

# Header para API Key
api_key_header = APIKeyHeader(name=API_KEY_NAME, auto_error=False)

async def get_api_key(api_key: str = Security(api_key_header)):
    """
    Obtém e valida a API Key fornecida no header
    """
    # Se não há API_KEY configurada, permite acesso (modo desenvolvimento)
    if not API_KEY:
        return True
        
    if not api_key:
        raise HTTPException(
            status_code=HTTP_403_FORBIDDEN,
            detail="API Key não fornecida. Use o header 'X-API-Key'."
        )
    
    if api_key != API_KEY:
        raise HTTPException(
            status_code=HTTP_403_FORBIDDEN,
            detail="API Key inválida ou expirada"
        )
    
    return True

# Função callable para usar como dependência
async def api_key_auth(api_key: str = Security(api_key_header)):
    return await get_api_key(api_key)