from typing import List

from fastapi import APIRouter, Depends, HTTPException, Request, status
from sqlalchemy.orm import Session

from app.core.dependencies import require_jwt, require_permissions
from app.core.security import get_password_hash
from app.db import models
from app.db.session import get_db
from app.schemas.settings import UserSettingsResponse, UserSettingsUpdate
from app.schemas.user import UserCreate, UserResponse

router = APIRouter(prefix="/users", tags=["Users"])


@router.post("", response_model=UserResponse, status_code=status.HTTP_201_CREATED, dependencies=[Depends(require_permissions(["users.manage"]))])
async def create_user(payload: UserCreate, db: Session = Depends(get_db)):
    username_exists = db.query(models.User).filter(models.User.username == payload.username).first()
    if username_exists:
        raise HTTPException(status_code=status.HTTP_409_CONFLICT, detail="Username already exists")

    email_exists = db.query(models.User).filter(models.User.email == payload.email).first()
    if email_exists:
        raise HTTPException(status_code=status.HTTP_409_CONFLICT, detail="Email already exists")

    user = models.User(
        username=payload.username,
        email=payload.email,
        full_name=payload.full_name,
        hashed_password=get_password_hash(payload.password),
        is_active=True,
    )
    db.add(user)
    db.flush()

    role_names = payload.role_names or []
    if role_names:
        roles = db.query(models.Role).filter(models.Role.name.in_(role_names)).all()
        roles_by_name = {role.name: role for role in roles}
        missing = [name for name in role_names if name not in roles_by_name]
        if missing:
            raise HTTPException(
                status_code=status.HTTP_404_NOT_FOUND,
                detail=f"Roles not found: {', '.join(missing)}",
            )
        for role in roles:
            db.add(models.UserRole(user_id=user.id, role_id=role.id))

    db.commit()
    db.refresh(user)

    assigned_roles = [role.name for role in db.query(models.Role).join(models.UserRole).filter(models.UserRole.user_id == user.id).all()]

    return UserResponse(
        id=user.id,
        username=user.username,
        email=user.email,
        full_name=user.full_name,
        is_active=user.is_active,
        roles=sorted(set(assigned_roles)),
    )


@router.get("/{user_id}", response_model=UserResponse, dependencies=[Depends(require_permissions(["users.read"]))])
async def get_user(user_id: int, db: Session = Depends(get_db)):
    user = db.query(models.User).filter(models.User.id == user_id).first()
    if not user:
        raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail="User not found")

    assigned_roles = [role.name for role in db.query(models.Role).join(models.UserRole).filter(models.UserRole.user_id == user.id).all()]

    return UserResponse(
        id=user.id,
        username=user.username,
        email=user.email,
        full_name=user.full_name,
        is_active=user.is_active,
        roles=sorted(set(assigned_roles)),
    )


@router.get("/me/settings", response_model=UserSettingsResponse, dependencies=[Depends(require_jwt)])
async def get_my_settings(request: Request, db: Session = Depends(get_db)):
    username = request.state.auth.get("payload", {}).get("sub")
    user = db.query(models.User).filter(models.User.username == username).first()
    if not user:
        raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail="User not found")

    settings = db.query(models.UserSettings).filter(models.UserSettings.user_id == user.id).first()
    if not settings:
        return UserSettingsResponse()

    return UserSettingsResponse(
        default_categoria_codigo=settings.default_categoria_codigo,
        default_mode=settings.default_mode,
        default_temperature=float(settings.default_temperature) if settings.default_temperature is not None else None,
        default_max_tokens=settings.default_max_tokens,
    )


@router.patch("/me/settings", response_model=UserSettingsResponse, dependencies=[Depends(require_jwt)])
async def update_my_settings(payload: UserSettingsUpdate, request: Request, db: Session = Depends(get_db)):
    username = request.state.auth.get("payload", {}).get("sub")
    user = db.query(models.User).filter(models.User.username == username).first()
    if not user:
        raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail="User not found")

    settings = db.query(models.UserSettings).filter(models.UserSettings.user_id == user.id).first()
    if not settings:
        settings = models.UserSettings(user_id=user.id)
        db.add(settings)

    if payload.default_categoria_codigo is not None:
        settings.default_categoria_codigo = payload.default_categoria_codigo
    if payload.default_mode is not None:
        settings.default_mode = payload.default_mode
    if payload.default_temperature is not None:
        settings.default_temperature = payload.default_temperature
    if payload.default_max_tokens is not None:
        settings.default_max_tokens = payload.default_max_tokens

    db.commit()
    db.refresh(settings)

    return UserSettingsResponse(
        default_categoria_codigo=settings.default_categoria_codigo,
        default_mode=settings.default_mode,
        default_temperature=float(settings.default_temperature) if settings.default_temperature is not None else None,
        default_max_tokens=settings.default_max_tokens,
    )
