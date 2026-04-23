# backend/app/services/crypto_manager.py
import os
import base64
from cryptography.fernet import Fernet
from cryptography.hazmat.primitives import hashes
from cryptography.hazmat.primitives.kdf.pbkdf2 import PBKDF2HMAC
import logging

logger = logging.getLogger(__name__)

class CryptoManager:
    def __init__(self):
        # Usa uma chave derivada de uma senha mestre do ambiente
        self.master_key = os.getenv("CRYPTO_MASTER_KEY", "default-master-key-for-dev")
        self.fernet = self._create_fernet()
    
    def _create_fernet(self):
        """Cria instância Fernet a partir da master key"""
        try:
            # Deriva uma chave de 32 bytes adequada para Fernet
            kdf = PBKDF2HMAC(
                algorithm=hashes.SHA256(),
                length=32,
                salt=b'vendamais_salt_',
                iterations=100000,
            )
            key = base64.urlsafe_b64encode(kdf.derive(self.master_key.encode()))
            return Fernet(key)
        except Exception as e:
            logger.error(f"Erro ao criar Fernet: {e}")
            # Fallback para desenvolvimento
            return Fernet(Fernet.generate_key())
    
    def encrypt_key(self, chave_real: str) -> dict:
        """Criptografa uma chave real"""
        try:
            encrypted = self.fernet.encrypt(chave_real.encode())
            return {
                'encrypted': base64.urlsafe_b64encode(encrypted).decode(),
                'iv': 'fixed_iv_for_simplicity'  
            }
        except Exception as e:
            logger.error(f"Erro ao criptografar chave: {e}")
            raise
    
    def decrypt_key(self, encrypted_data: str) -> str:
        """Descriptografa uma chave real"""
        try:
            encrypted_bytes = base64.urlsafe_b64decode(encrypted_data.encode())
            decrypted = self.fernet.decrypt(encrypted_bytes)
            return decrypted.decode()
        except Exception as e:
            logger.error(f"Erro ao descriptografar chave: {e}")
            raise

# Instância global
crypto_manager = CryptoManager()

