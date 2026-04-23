import hashlib
import secrets
import logging
from typing import Optional

logger = logging.getLogger(__name__)

class KeyManager:
    @staticmethod
    def hash_key(chave: str) -> str:
        """Gera hash seguro para chave de API"""
        return hashlib.sha256(chave.encode()).hexdigest()
    
    @staticmethod
    def mask_key(chave: str) -> str:
        """Mascara chave para exibição segura"""
        if len(chave) <= 8:
            return "***"
        return chave[:4] + "..." + chave[-4:]
    
    @staticmethod
    def validate_key_format(provedor: str, chave: str) -> bool:
        """Valida formato básico da chave conforme provedor"""
        if not chave or len(chave) < 10:
            return False
        
        if provedor == 'gemini' and chave.startswith('AIza'):
            return True
        elif provedor == 'openai' and chave.startswith('sk-'):
            return True
        elif provedor == 'mistral' and len(chave) == 32:
            return True
        elif provedor == 'huggingface' and chave.startswith('hf_'):
            return True
        elif provedor == 'azure':
            return True  
        
        return True  
    
    @staticmethod
    def generate_key_description(provedor: str, chave: str) -> str:
        """Gera descrição automática para chave"""
        base_descriptions = {
            'gemini': 'Chave Gemini',
            'openai': 'Chave OpenAI', 
            'azure': 'Chave Azure OpenAI',
            'mistral': 'Chave Mistral',
            'huggingface': 'Token Hugging Face'
        }
        return f"{base_descriptions.get(provedor, 'Chave')} - {KeyManager.mask_key(chave)}"

# Instância global
key_manager = KeyManager()

