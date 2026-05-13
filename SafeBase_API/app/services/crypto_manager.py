import base64
import logging
import os

from cryptography.fernet import Fernet
from cryptography.hazmat.primitives import hashes
from cryptography.hazmat.primitives.kdf.pbkdf2 import PBKDF2HMAC

logger = logging.getLogger(__name__)


class CryptoManager:
    def __init__(self) -> None:
        self.master_key = os.getenv("CRYPTO_MASTER_KEY", "default-master-key-for-dev")
        self.fernet = self._create_fernet()

    def _create_fernet(self) -> Fernet:
        try:
            kdf = PBKDF2HMAC(
                algorithm=hashes.SHA256(),
                length=32,
                salt=b"safebase_salt",
                iterations=100000,
            )
            key = base64.urlsafe_b64encode(kdf.derive(self.master_key.encode("utf-8")))
            return Fernet(key)
        except Exception as exc:
            logger.error("Erro ao criar Fernet: %s", exc)
            return Fernet(Fernet.generate_key())

    def encrypt_key(self, plain_text: str) -> str:
        encrypted = self.fernet.encrypt(plain_text.encode("utf-8"))
        return base64.urlsafe_b64encode(encrypted).decode("utf-8")

    def decrypt_key(self, encrypted_text: str) -> str:
        encrypted_bytes = base64.urlsafe_b64decode(encrypted_text.encode("utf-8"))
        decrypted = self.fernet.decrypt(encrypted_bytes)
        return decrypted.decode("utf-8")


crypto_manager = CryptoManager()
