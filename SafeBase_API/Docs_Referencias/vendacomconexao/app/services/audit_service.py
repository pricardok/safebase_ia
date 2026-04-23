# backend/app/services/audit_service.py
"""
Serviço de auditoria para logging de operações sensíveis
"""

import logging
import time
import json
from typing import Dict, Any, List, Optional
from fastapi import Request
from enum import Enum

logger = logging.getLogger(__name__)

class AuditAction(Enum):
    """Ações que devem ser auditadas"""
    SUPER_ACCESS = "super_access"
    MODULE_ACCESS = "module_access"
    DATA_ACCESS = "data_access"
    USER_LOGIN = "user_login"
    API_KEY_USAGE = "api_key_usage"
    ADMIN_OPERATION = "admin_operation"
    RATE_LIMIT_EXCEEDED = "rate_limit_exceeded"

class AuditService:
    def __init__(self):
        self._audit_logs = []
        self._max_logs = 10000  # Mantém apenas os últimos 10k logs
    
    def log_operation(
        self,
        action: AuditAction,
        request: Request,
        details: Dict[str, Any],
        user_id: Optional[int] = None,
        success: bool = True
    ):
        """Registra uma operação para auditoria"""
        try:
            log_entry = {
                "timestamp": time.time(),
                "action": action.value,
                "user_id": user_id,
                "ip_address": request.client.host if request.client else "unknown",
                "user_agent": request.headers.get("user-agent", "unknown"),
                "method": request.method,
                "path": str(request.url.path),
                "success": success,
                "details": details
            }
            
            # Adiciona ao log em memória
            self._audit_logs.append(log_entry)
            
            # Mantém apenas os logs mais recentes
            if len(self._audit_logs) > self._max_logs:
                self._audit_logs = self._audit_logs[-self._max_logs:]
            
            # Log estruturado para sistemas externos
            logger.info(
                f"AUDIT: {action.value} - "
                f"User: {user_id} - "
                f"IP: {log_entry['ip_address']} - "
                f"Path: {request.url.path} - "
                f"Success: {success} - "
                f"Details: {json.dumps(details)}"
            )
            
        except Exception as e:
            logger.error(f"Erro ao registrar log de auditoria: {e}")
    
    def get_audit_logs(
        self, 
        action: Optional[AuditAction] = None,
        user_id: Optional[int] = None,
        limit: int = 100
    ) -> List[Dict[str, Any]]:
        """Obtém logs de auditoria para análise"""
        filtered_logs = self._audit_logs.copy()
        
        if action:
            filtered_logs = [log for log in filtered_logs if log["action"] == action.value]
        
        if user_id:
            filtered_logs = [log for log in filtered_logs if log["user_id"] == user_id]
        
        return sorted(filtered_logs, key=lambda x: x["timestamp"], reverse=True)[:limit]
    
    def log_super_access(self, request: Request, module_name: str):
        """Log específico para super acesso"""
        self.log_operation(
            action=AuditAction.SUPER_ACCESS,
            request=request,
            details={
                "module": module_name,
                "access_type": "super_admin"
            },
            user_id=getattr(request.state, 'current_user_id', None),
            success=True
        )
    
    def log_module_access(self, request: Request, module_name: str, granted: bool):
        """Log de acesso a módulos"""
        self.log_operation(
            action=AuditAction.MODULE_ACCESS,
            request=request,
            details={
                "module": module_name,
                "granted": granted,
                "auth_method": getattr(request.state, 'auth_method', 'unknown'),
                "permissions": list(getattr(request.state, 'permissions', {}).keys())
            },
            user_id=getattr(request.state, 'current_user_id', None),
            success=granted
        )
    
    def log_data_access(self, request: Request, resource_type: str, resource_id: Any, ownership_valid: bool):
        """Log de acesso a dados"""
        self.log_operation(
            action=AuditAction.DATA_ACCESS,
            request=request,
            details={
                "resource_type": resource_type,
                "resource_id": resource_id,
                "ownership_valid": ownership_valid
            },
            user_id=getattr(request.state, 'current_user_id', None),
            success=ownership_valid
        )

# Instância global
audit_service = AuditService()

