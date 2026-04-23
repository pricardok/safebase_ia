# backend/app/services/security.py

import secrets
import string
from typing import Optional
from passlib.context import CryptContext
import hashlib

# Configura contexto de hash
try:
    pwd_context = CryptContext(schemes=["bcrypt"], deprecated="auto")
    BCrypt_AVAILABLE = True
except Exception as e:
    print(f"BCrypt não disponível: {e}")
    pwd_context = None
    BCrypt_AVAILABLE = False

def generate_api_key(length: int = 32) -> str:
    """
    Gera uma API Key segura
    """
    alphabet = string.ascii_letters + string.digits
    return ''.join(secrets.choice(alphabet) for _ in range(length))

def validate_api_key_format(api_key: str) -> bool:
    """
    Valida o formato básico de uma API Key
    """
    if len(api_key) < 16:
        return False
    
    # Verifica se contém apenas caracteres alfanuméricos
    return all(c.isalnum() for c in api_key)

def mask_api_key(api_key: str) -> str:
    """
    Mascara a API Key para logging (mostra apenas primeiros e últimos caracteres)
    """
    if len(api_key) <= 8:
        return "***"
    return api_key[:4] + "***" + api_key[-4:]

def get_password_hash(password: str):
    """Gera hash da senha com fallback (reutilizada para API Keys)"""
    if BCrypt_AVAILABLE and pwd_context:
        try:
            return pwd_context.hash(password)
        except Exception as e:
            print(f"Erro bcrypt hash, usando fallback: {e}")
    
    # Fallback para SHA256
    return hashlib.sha256(password.encode()).hexdigest()

def verify_password(plain_password: str, hashed_password: str):
    """Verifica senha com fallback (reutilizada para API Keys)"""
    if BCrypt_AVAILABLE and pwd_context:
        try:
            return pwd_context.verify(plain_password, hashed_password)
        except Exception as e:
            print(f"Erro bcrypt, usando fallback: {e}")
    
    # Fallback para SHA256
    return hashlib.sha256(plain_password.encode()).hexdigest() == hashed_password

def generate_temporary_password(length: int = 12) -> str:
    """Gera uma senha temporária segura"""
    alphabet = string.ascii_letters + string.digits + "!@#$%"
    return ''.join(secrets.choice(alphabet) for _ in range(length))

    
