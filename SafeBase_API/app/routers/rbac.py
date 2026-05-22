from typing import Dict, List

from fastapi import APIRouter, Depends, HTTPException, status
from sqlalchemy.orm import Session

from app.core.dependencies import require_permissions
from app.db import models
from app.db.session import get_db
from app.schemas.rbac import (
    PermissionCreate,
    PermissionResponse,
    RoleCreate,
    RolePermissionsAssign,
    RoleResponse,
    RoleUpdate,
    UserRolesAssign,
)

router = APIRouter(prefix="/rbac", tags=["RBAC"])


def _role_permissions_map(db: Session) -> Dict[int, List[str]]:
    rows = (
        db.query(models.RolePermission.role_id, models.Permission.code)
        .join(models.Permission, models.Permission.id == models.RolePermission.permission_id)
        .all()
    )
    mapping: Dict[int, List[str]] = {}
    for role_id, code in rows:
        mapping.setdefault(role_id, []).append(code)
    return mapping


@router.get("/roles", response_model=List[RoleResponse], dependencies=[Depends(require_permissions(["rbac.read"]))])
async def list_roles(db: Session = Depends(get_db)):
    roles = db.query(models.Role).order_by(models.Role.name).all()
    permissions_map = _role_permissions_map(db)
    return [
        RoleResponse(
            id=role.id,
            name=role.name,
            description=role.description,
            is_active=role.is_active,
            created_at=role.created_at,
            permissions=sorted(permissions_map.get(role.id, [])),
        )
        for role in roles
    ]


@router.post("/roles", response_model=RoleResponse, status_code=status.HTTP_201_CREATED, dependencies=[Depends(require_permissions(["rbac.manage"]))])
async def create_role(payload: RoleCreate, db: Session = Depends(get_db)):
    exists = db.query(models.Role).filter(models.Role.name == payload.name).first()
    if exists:
        raise HTTPException(status_code=status.HTTP_409_CONFLICT, detail="Role already exists")
    role = models.Role(
        name=payload.name,
        description=payload.description,
        is_active=payload.is_active,
    )
    db.add(role)
    db.commit()
    db.refresh(role)
    return RoleResponse(
        id=role.id,
        name=role.name,
        description=role.description,
        is_active=role.is_active,
        created_at=role.created_at,
        permissions=[],
    )


@router.patch("/roles/{role_id}", response_model=RoleResponse, dependencies=[Depends(require_permissions(["rbac.manage"]))])
async def update_role(role_id: int, payload: RoleUpdate, db: Session = Depends(get_db)):
    role = db.query(models.Role).filter(models.Role.id == role_id).first()
    if not role:
        raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail="Role not found")
    if payload.description is not None:
        role.description = payload.description
    if payload.is_active is not None:
        role.is_active = payload.is_active
    db.commit()
    db.refresh(role)
    permissions_map = _role_permissions_map(db)
    return RoleResponse(
        id=role.id,
        name=role.name,
        description=role.description,
        is_active=role.is_active,
        created_at=role.created_at,
        permissions=sorted(permissions_map.get(role.id, [])),
    )


@router.delete("/roles/{role_id}", status_code=status.HTTP_204_NO_CONTENT, dependencies=[Depends(require_permissions(["rbac.manage"]))])
async def delete_role(role_id: int, db: Session = Depends(get_db)):
    role = db.query(models.Role).filter(models.Role.id == role_id).first()
    if not role:
        raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail="Role not found")
    if role.name.lower() == "admin":
        raise HTTPException(status_code=status.HTTP_400_BAD_REQUEST, detail="Cannot delete admin role")
    db.delete(role)
    db.commit()
    return None


@router.get("/permissions", response_model=List[PermissionResponse], dependencies=[Depends(require_permissions(["rbac.read"]))])
async def list_permissions(db: Session = Depends(get_db)):
    permissions = db.query(models.Permission).order_by(models.Permission.code).all()
    return [
        PermissionResponse(
            id=permission.id,
            code=permission.code,
            description=permission.description,
            created_at=permission.created_at,
        )
        for permission in permissions
    ]


@router.post("/permissions", response_model=PermissionResponse, status_code=status.HTTP_201_CREATED, dependencies=[Depends(require_permissions(["rbac.manage"]))])
async def create_permission(payload: PermissionCreate, db: Session = Depends(get_db)):
    exists = db.query(models.Permission).filter(models.Permission.code == payload.code).first()
    if exists:
        raise HTTPException(status_code=status.HTTP_409_CONFLICT, detail="Permission already exists")
    permission = models.Permission(code=payload.code, description=payload.description)
    db.add(permission)
    db.commit()
    db.refresh(permission)
    return PermissionResponse(
        id=permission.id,
        code=permission.code,
        description=permission.description,
        created_at=permission.created_at,
    )


@router.delete("/permissions/{permission_code}", status_code=status.HTTP_204_NO_CONTENT, dependencies=[Depends(require_permissions(["rbac.manage"]))])
async def delete_permission(permission_code: str, db: Session = Depends(get_db)):
    permission = db.query(models.Permission).filter(models.Permission.code == permission_code).first()
    if not permission:
        raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail="Permission not found")
    db.delete(permission)
    db.commit()
    return None


@router.post("/roles/{role_id}/permissions", status_code=status.HTTP_204_NO_CONTENT, dependencies=[Depends(require_permissions(["rbac.manage"]))])
async def add_permissions_to_role(role_id: int, payload: RolePermissionsAssign, db: Session = Depends(get_db)):
    role = db.query(models.Role).filter(models.Role.id == role_id).first()
    if not role:
        raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail="Role not found")

    permissions = (
        db.query(models.Permission)
        .filter(models.Permission.code.in_(payload.permission_codes))
        .all()
    )
    permissions_by_code = {permission.code: permission for permission in permissions}
    missing = [code for code in payload.permission_codes if code not in permissions_by_code]
    if missing:
        raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail=f"Permissions not found: {', '.join(missing)}")

    for permission in permissions:
        exists = (
            db.query(models.RolePermission)
            .filter(
                models.RolePermission.role_id == role_id,
                models.RolePermission.permission_id == permission.id,
            )
            .first()
        )
        if not exists:
            db.add(models.RolePermission(role_id=role_id, permission_id=permission.id))

    db.commit()
    return None


@router.delete("/roles/{role_id}/permissions/{permission_code}", status_code=status.HTTP_204_NO_CONTENT, dependencies=[Depends(require_permissions(["rbac.manage"]))])
async def remove_permission_from_role(role_id: int, permission_code: str, db: Session = Depends(get_db)):
    permission = db.query(models.Permission).filter(models.Permission.code == permission_code).first()
    if not permission:
        raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail="Permission not found")
    link = (
        db.query(models.RolePermission)
        .filter(
            models.RolePermission.role_id == role_id,
            models.RolePermission.permission_id == permission.id,
        )
        .first()
    )
    if not link:
        raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail="Role permission link not found")
    db.delete(link)
    db.commit()
    return None


@router.get("/users/{user_id}/roles", response_model=List[str], dependencies=[Depends(require_permissions(["rbac.read"]))])
async def list_user_roles(user_id: int, db: Session = Depends(get_db)):
    rows = (
        db.query(models.Role.name)
        .join(models.UserRole, models.UserRole.role_id == models.Role.id)
        .filter(models.UserRole.user_id == user_id)
        .all()
    )
    return sorted({row[0] for row in rows})


@router.post("/users/{user_id}/roles", status_code=status.HTTP_204_NO_CONTENT, dependencies=[Depends(require_permissions(["rbac.manage"]))])
async def add_roles_to_user(user_id: int, payload: UserRolesAssign, db: Session = Depends(get_db)):
    user = db.query(models.User).filter(models.User.id == user_id).first()
    if not user:
        raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail="User not found")

    roles = db.query(models.Role).filter(models.Role.name.in_(payload.role_names)).all()
    roles_by_name = {role.name: role for role in roles}
    missing = [name for name in payload.role_names if name not in roles_by_name]
    if missing:
        raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail=f"Roles not found: {', '.join(missing)}")

    for role in roles:
        exists = (
            db.query(models.UserRole)
            .filter(models.UserRole.user_id == user_id, models.UserRole.role_id == role.id)
            .first()
        )
        if not exists:
            db.add(models.UserRole(user_id=user_id, role_id=role.id))

    db.commit()
    return None


@router.delete("/users/{user_id}/roles/{role_name}", status_code=status.HTTP_204_NO_CONTENT, dependencies=[Depends(require_permissions(["rbac.manage"]))])
async def remove_role_from_user(user_id: int, role_name: str, db: Session = Depends(get_db)):
    role = db.query(models.Role).filter(models.Role.name == role_name).first()
    if not role:
        raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail="Role not found")
    link = (
        db.query(models.UserRole)
        .filter(models.UserRole.user_id == user_id, models.UserRole.role_id == role.id)
        .first()
    )
    if not link:
        raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail="User role link not found")
    db.delete(link)
    db.commit()
    return None


@router.get("/users/{user_id}/permissions", response_model=List[str], dependencies=[Depends(require_permissions(["rbac.read"]))])
async def list_user_permissions(user_id: int, db: Session = Depends(get_db)):
    rows = (
        db.query(models.Permission.code)
        .join(models.RolePermission, models.RolePermission.permission_id == models.Permission.id)
        .join(models.Role, models.Role.id == models.RolePermission.role_id)
        .join(models.UserRole, models.UserRole.role_id == models.Role.id)
        .filter(models.UserRole.user_id == user_id)
        .all()
    )
    return sorted({row[0] for row in rows})
