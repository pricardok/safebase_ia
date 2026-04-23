# backend/app/auth_email.py
import os
import logging
from fastapi import HTTPException, Header
from dotenv import load_dotenv

load_dotenv()

logger = logging.getLogger(__name__)

def validate_email_api_key(x_api_key: str = Header(...)):
    """
    Valida a API Key específica para rotas de email
    """
    expected_api_key = os.getenv("API_KEY_MAIL_SEGURO")
    
    # Se não houver API Key configurada, permitir em desenvolvimento
    if not expected_api_key:
        logger.warning("API_KEY_MAIL_SEGURO não configurada - permitindo acesso em desenvolvimento")
        return "dev_key"
    
    if x_api_key != expected_api_key:
        logger.warning(f"Tentativa de acesso com API Key inválida: {x_api_key[:8]}...")
        raise HTTPException(
            status_code=401,
            detail="API Key inválida para operações de email"
        )
    
    return x_api_key