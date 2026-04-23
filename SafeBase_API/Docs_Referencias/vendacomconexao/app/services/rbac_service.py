#backend/app/services/rbac_service.py
"""
Serviço dedicado para gestão de autorização RBAC 
"""

import logging
import time
from typing import Dict, Any, Optional, Set, List
from functools import lru_cache
from fastapi import Request, HTTPException, status
import hashlib
from app.database import (
    get_user_profile, get_profile_permissions, get_api_key_profile,
    get_all_modules, get_all_profiles, get_profile_by_id,
    update_profile_name, clear_profile_permissions, add_permission_to_profile,
    get_module_by_name, get_profile_permissions as db_get_profile_permissions
)
from .audit_service import audit_service

# Mapeamento de módulos para permissões granulares. Esta é a "tradução"
# que garante a retrocompatibilidade.
MODULE_PERMISSION_MAP = {
    "simulador": ["simulador:acessar"],
    "objecoes": ["objecoes:acessar"],
    "detector": ["detector:acessar"],
    "conexao": ["conexao:acessar"],
    "scripts": ["scripts:acessar"],
    "analise": ["analise:acessar"],
    "historico": ["historico:acessar"],
    "admin": [
        "admin:acessar",
        "clientes:read",
        "clientes:write",
        "planos:read",
        "planos:write",
        "usuarios:read",
        "usuarios:write",
        "apikeys:read",
        "apikeys:write",
        "ia:read",
        "ia:write",
        "auditoria:read",
        "webhooks:read",
        "webhooks:reprocess",
        "configuracoes:read",
        "configuracoes:write",
        "testes_ab:read",
        "testes_ab:write"
    ]
}

logger = logging.getLogger(__name__)

class RBACService:
    def __init__(self):
        self._permissions_cache = {}
        self._cache_ttl = 300  # 5 minutos
        self._super_access_logs = []
    
    def _get_cache_key(self, identifier: str, type: str) -> str:
        """Gera chave de cache consistente"""
        return f"{type}:{hashlib.md5(identifier.encode()).hexdigest()}"
    
    @lru_cache(maxsize=256)
    def get_user_permissions(self, user_id: int) -> Dict[str, Any]:
        """Obtém permissões do usuário com cache inteligente"""
        from app.database import get_user_profile, get_profile_permissions
        
        try:
            profile = get_user_profile(user_id)
            if not profile:
                logger.warning(f"Perfil não encontrado para usuário: {user_id}")
                return {}
            
            permissions = get_profile_permissions(profile["id"])
            logger.debug(f"Permissões carregadas para usuário {user_id}: {list(permissions.keys())}")
            return permissions
            
        except Exception as e:
            logger.error(f"Erro ao obter permissões do usuário {user_id}: {e}")
            return {}
    
    @lru_cache(maxsize=256)
    def get_api_key_permissions(self, api_key_hash: str) -> Dict[str, Any]:
        """Obtém permissões da API Key com cache e validação"""
        from app.database import get_api_key_profile, get_profile_permissions
        
        try:
            profile = get_api_key_profile(api_key_hash)
            if not profile:
                logger.warning(f"Perfil não encontrado para API Key: {mask_api_key(api_key_hash)}")
                return {}
            
            if not profile.get("ativa", True):
                logger.warning(f"API Key inativa: {mask_api_key(api_key_hash)}")
                return {}
            
            permissions = get_profile_permissions(profile["id"])
            logger.debug(f"Permissões carregadas para API Key: {list(permissions.keys())}")
            return permissions
            
        except Exception as e:
            logger.error(f"Erro ao obter permissões da API Key: {e}")
            return {}

    def get_all_possible_permissions(self) -> List[str]:
        """Retorna uma lista plana de todas as strings de permissão possíveis no sistema."""
        permissions = set()
        for module_permissions in MODULE_PERMISSION_MAP.values():
            for permission in module_permissions:
                permissions.add(permission)
        return sorted(list(permissions))

    def update_profile_with_permissions(self, perfil_id: int, nome: Optional[str], permissoes: Optional[List[str]]) -> Optional[Dict[str, Any]]:
        """
        Atualiza um perfil, traduzindo uma lista de strings de permissão para o modelo de módulos,
        garantindo retrocompatibilidade.
        """
        profile = get_profile_by_id(perfil_id)
        if not profile:
            return None

        # 1. Atualiza o nome, se fornecido e diferente
        if nome is not None and nome.strip() and nome.strip() != profile['nome']:
            update_profile_name(perfil_id, nome.strip())

        # 2. Atualiza as permissões (módulos), se fornecidas
        if permissoes is not None:
            # Limpa as permissões de módulo antigas do perfil
            clear_profile_permissions(perfil_id)

            # Converte a lista de permissões granulares em um conjunto de módulos a serem ativados
            modules_to_enable = set()
            for perm_string in permissoes:
                for module_name, module_perms_list in MODULE_PERMISSION_MAP.items():
                    if perm_string in module_perms_list:
                        modules_to_enable.add(module_name)
                        break
            
            # Adiciona as novas permissões (baseadas em módulos) ao perfil
            for module_name in modules_to_enable:
                module = get_module_by_name(module_name)
                if module:
                    add_permission_to_profile(perfil_id, module['id'])

        # Limpa os caches para garantir que as novas permissões sejam carregadas na próxima requisição
        self.get_user_permissions.cache_clear()
        self.get_api_key_permissions.cache_clear()
        get_profile_permissions.cache_clear()

        # Retorna o perfil atualizado, formatado para o frontend
        updated_profile_data = get_profile_by_id(perfil_id)
        permissions_data = db_get_profile_permissions(perfil_id)
        
        # Converte as permissões de módulo de volta para a lista de strings granulares para a resposta
        granted_permissions = set()
        for module_name, perms_info in permissions_data.items():
            if perms_info.get("pode_acessar"):
                granted_permissions.update(MODULE_PERMISSION_MAP.get(module_name, []))

        updated_profile_data["permissoes"] = sorted(list(granted_permissions))

        return updated_profile_data
    
    def check_module_access(self, request: Request, module_name: str) -> bool:
        """Verifica acesso ao módulo de forma centralizada e segura"""
        if not hasattr(request.state, 'permissions'):
            logger.error("Tentativa de acesso sem permissões configuradas")
            raise HTTPException(
                status_code=status.HTTP_403_FORBIDDEN,
                detail="Permissões não disponíveis"
            )
        
        # Super acesso tem permissão total com auditoria
        if getattr(request.state, 'is_super_access', False):
            self._log_super_access(request, module_name)
            return True
        
        permissions = request.state.permissions
        
        # Verifica acesso específico ao módulo
        if module_name in permissions and permissions[module_name].get("pode_acessar", False):
            return True
        
        # Verifica acesso wildcard
        if "*" in permissions and permissions["*"].get("pode_acessar", False):
            return True
        
        # Log detalhado de acesso negado
        auth_method = getattr(request.state, 'auth_method', 'unknown')
        user_info = self._get_user_info_for_logging(request)
        
        logger.warning(
            f"ACESSO NEGADO: {auth_method}::{user_info} -> "
            f"Módulo: {module_name} - "
            f"IP: {request.client.host if request.client else 'unknown'} - "
            f"Path: {request.url.path}"
        )
        
        raise HTTPException(
            status_code=status.HTTP_403_FORBIDDEN,
            detail=f"Seu perfil não tem acesso ao módulo '{module_name}'"
        )
    
    def validate_endpoint_access(self, request: Request, endpoint_path: str) -> bool:
        """Valida acesso a endpoint específico baseado no módulo"""
        # Extrai o módulo do path do endpoint
        module_name = self._extract_module_from_path(endpoint_path)
        return self.check_module_access(request, module_name)
    
    def _extract_module_from_path(self, path: str) -> str:
        """Extrai nome do módulo do path do endpoint"""
        if not path or path == "/":
            return "root"
        
        # Remove leading slash e split
        parts = path.lstrip('/').split('/')
        if not parts:
            return "root"
        
        # Primeira parte do path é geralmente o módulo
        module = parts[0]
        
        # Mapeamento de endpoints para módulos
        module_mapping = {
            'simulador': 'simulador',
            'objecoes': 'objecoes', 
            'detector': 'detector',
            'conexao': 'conexao',
            'scripts': 'scripts',
            'analise': 'analise',
            'historico': 'historico',
            'admin': 'admin',
            'auth': 'auth',
            'prompts': 'prompts'
        }
        
        return module_mapping.get(module, module)
    
    def _log_super_access(self, request: Request, module_name: str):
        """Log de uso do super acesso para auditoria com detalhes completos"""
        log_entry = {
            "timestamp": time.time(),
            "module": module_name,
            "path": str(request.url.path),
            "method": request.method,
            "ip": request.client.host if request.client else "unknown",
            "user_agent": request.headers.get("user-agent", "unknown")
        }
        
        # Mantém apenas os últimos 1000 logs
        self._super_access_logs = self._super_access_logs[-999:] + [log_entry]
        
        logger.info(
            f"SUPER_ACCESS_AUDIT: {log_entry['timestamp']} - "
            f"Module: {module_name} - "
            f"Path: {request.url.path} - "
            f"IP: {log_entry['ip']} - "
            f"Method: {request.method}"
        )
    
    def _get_user_info_for_logging_OLD(self, request: Request) -> str:
        """Obtém informações do usuário para logging (sem dados sensíveis)"""
        if hasattr(request.state, 'user'):
            # CORREÇÃO: O payload do token (request.state.user) é um dicionário.
            # Acessamos o 'sub' (subject/username) usando a sintaxe de dicionário.
            return f"user:{request.state.user['sub']}"
        elif hasattr(request.state, 'current_profile'):
            return f"api_key:{request.state.current_profile.get('nome', 'unknown')}"
        else:
            return "unknown"

    def _get_user_info_for_logging(self, request: Request) -> str:
        """Obtém informações do usuário para logging (sem dados sensíveis)"""
        if hasattr(request.state, 'user'):
            # CORREÇÃO: O payload do token (request.state.user) é um dicionário.
            # Acessamos o 'sub' (subject/username) usando a sintaxe de dicionário.
            return f"user:{request.state.user.get('sub', 'unknown_user')}"
        elif hasattr(request.state, 'current_profile'):
            return f"api_key:{request.state.current_profile.get('nome', 'unknown')}"
        else:
            return "unknown"
    
    def get_super_access_logs(self, limit: int = 50) -> list:
        """Obtém logs de super acesso para auditoria (apenas admin)"""
        return self._super_access_logs[-limit:]
    
    def clear_permissions_cache(self, user_id: Optional[int] = None, api_key_hash: Optional[str] = None):
        """Limpa cache de permissões para forçar recarregamento"""
        if user_id:
            self.get_user_permissions.cache_clear()
        if api_key_hash:
            self.get_api_key_permissions.cache_clear()
        
        logger.info("Cache de permissões limpo")

def mask_api_key(api_key: str) -> str:
    """Mascara API Key para logging"""
    if len(api_key) <= 8:
        return "***"
    return api_key[:4] + "***" + api_key[-4:]

# Instância global
rbac_service = RBACService()
