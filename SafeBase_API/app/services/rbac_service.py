from typing import Dict

from sqlalchemy.orm import Session

from app.db import models

DEFAULT_PERMISSIONS: Dict[str, str] = {
    "rbac.read": "Leitura da configuração de RBAC",
    "rbac.manage": "Gerenciamento completo de RBAC",
    "users.read": "Leitura de usuários",
    "users.manage": "Gerenciamento completo de usuários",
    "categories.read": "Leitura de categorias",
    "categories.manage": "Gerenciamento completo de categorias",
    "external_sources.read": "Leitura de fontes externas",
    "external_sources.manage": "Gerenciamento completo de fontes externas",
}

DEFAULT_ROLES: Dict[str, Dict[str, object]] = {
    "admin": {
        "description": "Administrador do sistema",
        "permissions": list(DEFAULT_PERMISSIONS.keys()),
    }
}


def seed_rbac(db: Session) -> None:
    permissions_by_code: Dict[str, models.Permission] = {}

    for code, description in DEFAULT_PERMISSIONS.items():
        permission = db.query(models.Permission).filter(models.Permission.code == code).first()
        if not permission:
            permission = models.Permission(code=code, description=description)
            db.add(permission)
            db.flush()
        permissions_by_code[code] = permission

    for role_name, role_data in DEFAULT_ROLES.items():
        role = db.query(models.Role).filter(models.Role.name == role_name).first()
        if not role:
            role = models.Role(
                name=role_name,
                description=role_data.get("description"),
                is_active=True,
            )
            db.add(role)
            db.flush()

        for code in role_data.get("permissions", []):
            permission = permissions_by_code.get(code)
            if not permission:
                continue
            exists = (
                db.query(models.RolePermission)
                .filter(
                    models.RolePermission.role_id == role.id,
                    models.RolePermission.permission_id == permission.id,
                )
                .first()
            )
            if not exists:
                db.add(
                    models.RolePermission(
                        role_id=role.id,
                        permission_id=permission.id,
                    )
                )

    db.commit()
