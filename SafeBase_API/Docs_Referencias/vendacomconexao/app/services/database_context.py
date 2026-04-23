"""
Contexto de banco para garantir isolamento de dados por usuário
"""

from typing import Optional
from fastapi import Request, HTTPException, status
import logging

logger = logging.getLogger(__name__)

class DatabaseContext:
    def __init__(self):
        pass
    
    def get_current_user_id(self, request: Request) -> int:
        """Obtém o ID do usuário atual de forma segura"""
        if not hasattr(request.state, 'current_user_id'):
            raise HTTPException(
                status_code=status.HTTP_401_UNAUTHORIZED,
                detail="Usuário não autenticado"
            )
        
        return request.state.current_user_id
    
    def validate_data_ownership(self, request: Request, resource_user_id: int) -> bool:
        """Valida se o usuário atual é dono do recurso"""
        current_user_id = self.get_current_user_id(request)
        
        if current_user_id != resource_user_id:
            logger.warning(
                f"Tentativa de acesso a dados de outro usuário: "
                f"Current: {current_user_id}, Resource: {resource_user_id}"
            )
            return False
        
        return True
    
    def should_save_history(self, request: Request) -> bool:
        """Determina se deve salvar no histórico baseado no método de auth"""
        return getattr(request.state, 'auth_method', None) == 'jwt'

# Instância global
db_context = DatabaseContext()