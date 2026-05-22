from datetime import datetime, timedelta
from typing import Any, Dict, List, Optional

from sqlalchemy import or_
from sqlalchemy.orm import Session

from app.core.config import settings
from app.core.security import create_access_token, generate_refresh_token, hash_token, verify_password
from app.db import models


class AuthService:
    def authenticate(self, db: Session, username: str, password: str) -> Optional[Dict[str, Any]]:
        user = (
            db.query(models.User)
            .filter(
                or_(models.User.username == username, models.User.email == username),
                models.User.is_active == True,
            )
            .first()
        )
        if not user or not verify_password(password, user.hashed_password):
            return None

        roles, permissions = self._get_user_roles_permissions(db, user.id)
        return {
            "id": user.id,
            "username": user.username,
            "email": user.email,
            "full_name": user.full_name,
            "is_active": user.is_active,
            "roles": roles,
            "permissions": permissions,
        }

    def create_access_token(self, subject: str, roles: List[str], permissions: List[str]) -> str:
        return create_access_token(subject, roles=roles, permissions=permissions)

    def issue_refresh_token(self, db: Session, user_id: int) -> str:
        token = generate_refresh_token()
        token_hash = hash_token(token)
        expires_at = datetime.utcnow() + timedelta(days=settings.refresh_token_expire_days)
        record = models.RefreshToken(
            user_id=user_id,
            token_hash=token_hash,
            expires_at=expires_at,
        )
        db.add(record)
        db.commit()
        return token

    def refresh_access_token(self, db: Session, refresh_token: str) -> Optional[str]:
        token_hash = hash_token(refresh_token)
        record = (
            db.query(models.RefreshToken)
            .filter(
                models.RefreshToken.token_hash == token_hash,
                models.RefreshToken.revoked_at.is_(None),
            )
            .first()
        )
        if not record or record.expires_at < datetime.utcnow():
            return None

        user = db.query(models.User).filter(models.User.id == record.user_id).first()
        if not user:
            return None

        roles, permissions = self._get_user_roles_permissions(db, user.id)
        return create_access_token(user.username, roles=roles, permissions=permissions)

    def revoke_refresh_token(self, db: Session, refresh_token: str) -> bool:
        token_hash = hash_token(refresh_token)
        record = (
            db.query(models.RefreshToken)
            .filter(
                models.RefreshToken.token_hash == token_hash,
                models.RefreshToken.revoked_at.is_(None),
            )
            .first()
        )
        if not record:
            return False

        record.revoked_at = datetime.utcnow()
        db.commit()
        return True

    def revoke_all_refresh_tokens(self, db: Session, user_id: int) -> int:
        updated = (
            db.query(models.RefreshToken)
            .filter(models.RefreshToken.user_id == user_id, models.RefreshToken.revoked_at.is_(None))
            .update({models.RefreshToken.revoked_at: datetime.utcnow()})
        )
        db.commit()
        return updated

    def get_user_with_permissions(self, db: Session, username: str) -> Optional[Dict[str, Any]]:
        user = db.query(models.User).filter(models.User.username == username).first()
        if not user:
            return None
        roles, permissions = self._get_user_roles_permissions(db, user.id)
        categorias = self._get_user_categories(db, user.id)
        return {
            "id": user.id,
            "username": user.username,
            "email": user.email,
            "full_name": user.full_name,
            "is_active": user.is_active,
            "roles": roles,
            "permissions": permissions,
            "categorias": categorias,
        }

    def _get_user_categories(self, db: Session, user_id: int) -> List[str]:
        rows = (
            db.query(models.ChatCategoria.codigo)
            .join(models.UserCategoria, models.UserCategoria.categoria_id == models.ChatCategoria.id)
            .filter(models.UserCategoria.user_id == user_id, models.ChatCategoria.ativo == True)
            .all()
        )
        return sorted({row[0] for row in rows})

    def _get_user_roles_permissions(self, db: Session, user_id: int) -> tuple[List[str], List[str]]:
        role_rows = (
            db.query(models.Role.name)
            .join(models.UserRole, models.UserRole.role_id == models.Role.id)
            .filter(models.UserRole.user_id == user_id, models.Role.is_active == True)
            .all()
        )
        roles = sorted({row[0] for row in role_rows})

        perm_rows = (
            db.query(models.Permission.code)
            .join(models.RolePermission, models.RolePermission.permission_id == models.Permission.id)
            .join(models.Role, models.Role.id == models.RolePermission.role_id)
            .join(models.UserRole, models.UserRole.role_id == models.Role.id)
            .filter(models.UserRole.user_id == user_id, models.Role.is_active == True)
            .all()
        )
        permissions = sorted({row[0] for row in perm_rows})
        return roles, permissions
