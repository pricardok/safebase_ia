# backend/app/auth_jwt.py
from datetime import datetime, timedelta
from jose import JWTError, jwt
from passlib.context import CryptContext
from fastapi import HTTPException, status
from pydantic import BaseModel
import os
from dotenv import load_dotenv
import hashlib

load_dotenv()

# Configurações
SECRET_KEY = os.getenv("JWT_SECRET_KEY", "f19a33ae27b735ba880d0c9d5a2c17c5886f75efaed0b37075a8b0ffefed89c2")
ALGORITHM = "HS256"
ACCESS_TOKEN_EXPIRE_MINUTES = 60 * 24 * 7  # 7 dias

# Contexto para hash de senhas com fallback
try:
    pwd_context = CryptContext(schemes=["bcrypt"], deprecated="auto")
    BCrypt_AVAILABLE = True
except Exception as e:
    print(f"⚠️  BCrypt não disponível: {e}")
    pwd_context = None
    BCrypt_AVAILABLE = False

# Modelos
class Token(BaseModel):
    access_token: str
    token_type: str

class TokenData(BaseModel):
    username: str = None

class User(BaseModel):
    username: str
    email: str = None
    full_name: str = None
    disabled: bool = None

class UserInDB(User):
    hashed_password: str

# Funções de utilidade com fallback
def verify_password(plain_password, hashed_password):
    """Verifica senha com fallback para SHA256 se bcrypt falhar"""
    if BCrypt_AVAILABLE and pwd_context:
        try:
            return pwd_context.verify(plain_password, hashed_password)
        except Exception as e:
            print(f"⚠️  Erro bcrypt, usando fallback: {e}")
    
    # Fallback para SHA256
    return hashlib.sha256(plain_password.encode()).hexdigest() == hashed_password

def get_password_hash(password):
    """Gera hash da senha com fallback"""
    if BCrypt_AVAILABLE and pwd_context:
        try:
            return pwd_context.hash(password)
        except Exception as e:
            print(f"⚠️  Erro bcrypt hash, usando fallback: {e}")
    
    # Fallback para SHA256
    return hashlib.sha256(password.encode()).hexdigest()

def create_access_token(data: dict, expires_delta: timedelta = None):
    to_encode = data.copy()
    if expires_delta:
        expire = datetime.utcnow() + expires_delta
    else:
        expire = datetime.utcnow() + timedelta(minutes=15)
    to_encode.update({"exp": expire})
    encoded_jwt = jwt.encode(to_encode, SECRET_KEY, algorithm=ALGORITHM)
    return encoded_jwt

def verify_token(token: str):
    try:
        payload = jwt.decode(token, SECRET_KEY, algorithms=[ALGORITHM])
        username: str = payload.get("sub")
        if username is None:
            return None
        return payload # Retorna o payload completo (dicionário)
    except JWTError:
        return None