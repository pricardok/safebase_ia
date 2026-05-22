from typing import List

from fastapi import APIRouter, Depends, HTTPException, status
from sqlalchemy.orm import Session

from app.core.dependencies import require_permissions
from app.db import models
from app.db.session import get_db
from app.schemas.category import CategoryCreate, CategoryResponse, CategoryUpdate, UserCategoryAssign
from app.schemas.external_sources import ExternalSourceCreate, ExternalSourceResponse, ExternalSourceUpdate
from app.schemas.user import UserAdminResponse

router = APIRouter(prefix="/admin", tags=["Admin"])


@router.get("/categories", response_model=List[CategoryResponse], dependencies=[Depends(require_permissions(["categories.read"]))])
async def list_categories(db: Session = Depends(get_db)):
    categorias = db.query(models.ChatCategoria).order_by(models.ChatCategoria.nome).all()
    return [
        CategoryResponse(
            id=cat.id,
            codigo=cat.codigo,
            nome=cat.nome,
            descricao=cat.descricao,
            ativo=cat.ativo,
            criado_em=cat.criado_em,
        )
        for cat in categorias
    ]


@router.get("/users", response_model=List[UserAdminResponse], dependencies=[Depends(require_permissions(["users.read"]))])
async def list_users(db: Session = Depends(get_db)):
    users = db.query(models.User).order_by(models.User.username).all()

    role_rows = (
        db.query(models.UserRole.user_id, models.Role.name)
        .join(models.Role, models.Role.id == models.UserRole.role_id)
        .all()
    )
    roles_map: dict[int, list[str]] = {}
    for user_id, role_name in role_rows:
        roles_map.setdefault(user_id, []).append(role_name)

    category_rows = (
        db.query(models.UserCategoria.user_id, models.ChatCategoria.codigo)
        .join(models.ChatCategoria, models.ChatCategoria.id == models.UserCategoria.categoria_id)
        .all()
    )
    categories_map: dict[int, list[str]] = {}
    for user_id, codigo in category_rows:
        categories_map.setdefault(user_id, []).append(codigo)

    return [
        UserAdminResponse(
            id=user.id,
            username=user.username,
            email=user.email,
            full_name=user.full_name,
            is_active=user.is_active,
            roles=sorted(set(roles_map.get(user.id, []))),
            categorias=sorted(set(categories_map.get(user.id, []))),
        )
        for user in users
    ]


@router.post("/categories", response_model=CategoryResponse, status_code=status.HTTP_201_CREATED, dependencies=[Depends(require_permissions(["categories.manage"]))])
async def create_category(payload: CategoryCreate, db: Session = Depends(get_db)):
    exists = db.query(models.ChatCategoria).filter(models.ChatCategoria.codigo == payload.codigo).first()
    if exists:
        raise HTTPException(status_code=status.HTTP_409_CONFLICT, detail="Categoria ja existe")

    categoria = models.ChatCategoria(
        codigo=payload.codigo,
        nome=payload.nome,
        descricao=payload.descricao,
        ativo=payload.ativo,
    )
    db.add(categoria)
    db.commit()
    db.refresh(categoria)

    return CategoryResponse(
        id=categoria.id,
        codigo=categoria.codigo,
        nome=categoria.nome,
        descricao=categoria.descricao,
        ativo=categoria.ativo,
        criado_em=categoria.criado_em,
    )


@router.patch("/categories/{category_id}", response_model=CategoryResponse, dependencies=[Depends(require_permissions(["categories.manage"]))])
async def update_category(category_id: int, payload: CategoryUpdate, db: Session = Depends(get_db)):
    categoria = db.query(models.ChatCategoria).filter(models.ChatCategoria.id == category_id).first()
    if not categoria:
        raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail="Categoria nao encontrada")

    if payload.nome is not None:
        categoria.nome = payload.nome
    if payload.descricao is not None:
        categoria.descricao = payload.descricao
    if payload.ativo is not None:
        categoria.ativo = payload.ativo

    db.commit()
    db.refresh(categoria)

    return CategoryResponse(
        id=categoria.id,
        codigo=categoria.codigo,
        nome=categoria.nome,
        descricao=categoria.descricao,
        ativo=categoria.ativo,
        criado_em=categoria.criado_em,
    )


@router.get("/users/{user_id}/categories", response_model=List[str], dependencies=[Depends(require_permissions(["categories.read"]))])
async def list_user_categories(user_id: int, db: Session = Depends(get_db)):
    rows = (
        db.query(models.ChatCategoria.codigo)
        .join(models.UserCategoria, models.UserCategoria.categoria_id == models.ChatCategoria.id)
        .filter(models.UserCategoria.user_id == user_id)
        .all()
    )
    return sorted({row[0] for row in rows})


@router.post("/users/{user_id}/categories", response_model=List[str], dependencies=[Depends(require_permissions(["categories.manage"]))])
async def set_user_categories(user_id: int, payload: UserCategoryAssign, db: Session = Depends(get_db)):
    user = db.query(models.User).filter(models.User.id == user_id).first()
    if not user:
        raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail="Usuario nao encontrado")

    categorias = (
        db.query(models.ChatCategoria)
        .filter(models.ChatCategoria.codigo.in_(payload.categoria_codigos))
        .all()
    )
    categorias_by_code = {cat.codigo: cat for cat in categorias}
    missing = [code for code in payload.categoria_codigos if code not in categorias_by_code]
    if missing:
        raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail=f"Categorias nao encontradas: {', '.join(missing)}")

    db.query(models.UserCategoria).filter(models.UserCategoria.user_id == user_id).delete()
    for categoria in categorias:
        db.add(models.UserCategoria(user_id=user_id, categoria_id=categoria.id))

    db.commit()

    return sorted({cat.codigo for cat in categorias})


@router.get("/external-sources", response_model=List[ExternalSourceResponse], dependencies=[Depends(require_permissions(["external_sources.read"]))])
async def list_external_sources(db: Session = Depends(get_db)):
    sources = db.query(models.ExternalDataSource).order_by(models.ExternalDataSource.nome).all()
    return [
        ExternalSourceResponse(
            id=source.id,
            nome=source.nome,
            tipo=source.tipo,
            configuracao=source.configuracao,
            ativo=source.ativo,
            criado_em=source.criado_em,
            atualizado_em=source.atualizado_em,
        )
        for source in sources
    ]


@router.post("/external-sources", response_model=ExternalSourceResponse, status_code=status.HTTP_201_CREATED, dependencies=[Depends(require_permissions(["external_sources.manage"]))])
async def create_external_source(payload: ExternalSourceCreate, db: Session = Depends(get_db)):
    source = models.ExternalDataSource(
        nome=payload.nome,
        tipo=payload.tipo,
        configuracao=payload.configuracao,
        ativo=payload.ativo,
    )
    db.add(source)
    db.commit()
    db.refresh(source)

    return ExternalSourceResponse(
        id=source.id,
        nome=source.nome,
        tipo=source.tipo,
        configuracao=source.configuracao,
        ativo=source.ativo,
        criado_em=source.criado_em,
        atualizado_em=source.atualizado_em,
    )


@router.patch("/external-sources/{source_id}", response_model=ExternalSourceResponse, dependencies=[Depends(require_permissions(["external_sources.manage"]))])
async def update_external_source(source_id: int, payload: ExternalSourceUpdate, db: Session = Depends(get_db)):
    source = db.query(models.ExternalDataSource).filter(models.ExternalDataSource.id == source_id).first()
    if not source:
        raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail="Fonte externa nao encontrada")

    if payload.nome is not None:
        source.nome = payload.nome
    if payload.tipo is not None:
        source.tipo = payload.tipo
    if payload.configuracao is not None:
        source.configuracao = payload.configuracao
    if payload.ativo is not None:
        source.ativo = payload.ativo

    db.commit()
    db.refresh(source)

    return ExternalSourceResponse(
        id=source.id,
        nome=source.nome,
        tipo=source.tipo,
        configuracao=source.configuracao,
        ativo=source.ativo,
        criado_em=source.criado_em,
        atualizado_em=source.atualizado_em,
    )
