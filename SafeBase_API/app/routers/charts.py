import json
from datetime import datetime, timedelta

from fastapi import APIRouter, Depends, HTTPException, Request, status
from typing import List
from sqlalchemy.orm import Session

from app.core.dependencies import require_jwt
from app.db import models
from app.db.session import get_db
from app.schemas.charts import ChartShareDetailResponse, ChartShareRequest, ChartShareResponse
from app.schemas.favorite_shares import (
    ChartFavoriteShareDetailResponse,
    ChartFavoriteShareRequest,
    ChartFavoriteShareResponse,
)
from app.schemas.favorites import ChartFavoriteCreate, ChartFavoriteResponse, ChartFavoriteUpdate

router = APIRouter(prefix="/charts", tags=["Charts"])


@router.post("/share", response_model=ChartShareResponse, dependencies=[Depends(require_jwt)])
async def share_chart(payload: ChartShareRequest, request: Request, db: Session = Depends(get_db)):
    username = request.state.auth.get("payload", {}).get("sub")
    user = db.query(models.User).filter(models.User.username == username).first()
    if not user:
        raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail="User not found")

    categoria_id = None
    if payload.categoria_codigo:
        categoria = db.query(models.ChatCategoria).filter(models.ChatCategoria.codigo == payload.categoria_codigo).first()
        if not categoria:
            raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail="Categoria nao encontrada")
        categoria_id = categoria.id

    expires_at = None
    if payload.expires_in_hours:
        expires_at = datetime.utcnow() + timedelta(hours=payload.expires_in_hours)

    import secrets
    share_token = secrets.token_urlsafe(24)

    record = models.ChartShare(
        user_id=user.id,
        categoria_id=categoria_id,
        titulo=payload.titulo,
        chart_payload=json.dumps(payload.chart.dict(), ensure_ascii=False),
        share_token=share_token,
        expires_at=expires_at,
    )
    db.add(record)
    db.commit()
    db.refresh(record)

    return ChartShareResponse(
        share_url=f"/charts/share/{record.share_token}",
        token=record.share_token,
        expires_at=record.expires_at,
    )


@router.get("/share/{token}", response_model=ChartShareDetailResponse)
async def get_shared_chart(token: str, db: Session = Depends(get_db)):
    record = db.query(models.ChartShare).filter(models.ChartShare.share_token == token).first()
    if not record:
        raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail="Share token nao encontrado")
    if record.expires_at and record.expires_at < datetime.utcnow():
        raise HTTPException(status_code=status.HTTP_410_GONE, detail="Share token expirado")

    categoria_codigo = None
    if record.categoria_id:
        categoria = db.query(models.ChatCategoria).filter(models.ChatCategoria.id == record.categoria_id).first()
        categoria_codigo = categoria.codigo if categoria else None

    return ChartShareDetailResponse(
        titulo=record.titulo,
        categoria_codigo=categoria_codigo,
        chart=json.loads(record.chart_payload),
        expires_at=record.expires_at,
    )


@router.get("/favorites", response_model=List[ChartFavoriteResponse], dependencies=[Depends(require_jwt)])
async def list_favorites(request: Request, db: Session = Depends(get_db)):
    username = request.state.auth.get("payload", {}).get("sub")
    user = db.query(models.User).filter(models.User.username == username).first()
    if not user:
        raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail="User not found")

    favorites = db.query(models.ChartFavorite).filter(models.ChartFavorite.user_id == user.id).all()
    categoria_ids = {fav.categoria_id for fav in favorites if fav.categoria_id}
    categorias_map = {}
    if categoria_ids:
        categorias = db.query(models.ChatCategoria).filter(models.ChatCategoria.id.in_(categoria_ids)).all()
        categorias_map = {categoria.id: categoria.codigo for categoria in categorias}

    return [
        ChartFavoriteResponse(
            id=fav.id,
            titulo=fav.titulo,
            categoria_codigo=categorias_map.get(fav.categoria_id),
            chart=json.loads(fav.chart_payload),
            criado_em=fav.criado_em,
        )
        for fav in favorites
    ]


@router.post("/favorites", response_model=ChartFavoriteResponse, status_code=status.HTTP_201_CREATED, dependencies=[Depends(require_jwt)])
async def create_favorite(payload: ChartFavoriteCreate, request: Request, db: Session = Depends(get_db)):
    username = request.state.auth.get("payload", {}).get("sub")
    user = db.query(models.User).filter(models.User.username == username).first()
    if not user:
        raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail="User not found")

    categoria_id = None
    if payload.categoria_codigo:
        categoria = db.query(models.ChatCategoria).filter(models.ChatCategoria.codigo == payload.categoria_codigo).first()
        if not categoria:
            raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail="Categoria nao encontrada")
        categoria_id = categoria.id

    favorite = models.ChartFavorite(
        user_id=user.id,
        categoria_id=categoria_id,
        titulo=payload.titulo,
        chart_payload=json.dumps(payload.chart.dict(), ensure_ascii=False),
    )
    db.add(favorite)
    db.commit()
    db.refresh(favorite)

    return ChartFavoriteResponse(
        id=favorite.id,
        titulo=favorite.titulo,
        categoria_codigo=payload.categoria_codigo,
        chart=payload.chart.dict(),
        criado_em=favorite.criado_em,
    )


@router.delete("/favorites/{favorite_id}", status_code=status.HTTP_204_NO_CONTENT, dependencies=[Depends(require_jwt)])
async def delete_favorite(favorite_id: int, request: Request, db: Session = Depends(get_db)):
    username = request.state.auth.get("payload", {}).get("sub")
    user = db.query(models.User).filter(models.User.username == username).first()
    if not user:
        raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail="User not found")

    favorite = (
        db.query(models.ChartFavorite)
        .filter(models.ChartFavorite.id == favorite_id, models.ChartFavorite.user_id == user.id)
        .first()
    )
    if not favorite:
        raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail="Favorito nao encontrado")

    db.delete(favorite)
    db.commit()
    return None


@router.patch("/favorites/{favorite_id}", response_model=ChartFavoriteResponse, dependencies=[Depends(require_jwt)])
async def update_favorite(
    favorite_id: int,
    payload: ChartFavoriteUpdate,
    request: Request,
    db: Session = Depends(get_db),
):
    username = request.state.auth.get("payload", {}).get("sub")
    user = db.query(models.User).filter(models.User.username == username).first()
    if not user:
        raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail="User not found")

    favorite = (
        db.query(models.ChartFavorite)
        .filter(models.ChartFavorite.id == favorite_id, models.ChartFavorite.user_id == user.id)
        .first()
    )
    if not favorite:
        raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail="Favorito nao encontrado")

    if payload.titulo is not None:
        favorite.titulo = payload.titulo

    db.commit()
    db.refresh(favorite)

    categoria_codigo = None
    if favorite.categoria_id:
        categoria = db.query(models.ChatCategoria).filter(models.ChatCategoria.id == favorite.categoria_id).first()
        categoria_codigo = categoria.codigo if categoria else None

    return ChartFavoriteResponse(
        id=favorite.id,
        titulo=favorite.titulo,
        categoria_codigo=categoria_codigo,
        chart=json.loads(favorite.chart_payload),
        criado_em=favorite.criado_em,
    )


@router.post("/favorites/{favorite_id}/share", response_model=ChartFavoriteShareResponse, dependencies=[Depends(require_jwt)])
async def share_favorite(
    favorite_id: int,
    payload: ChartFavoriteShareRequest,
    request: Request,
    db: Session = Depends(get_db),
):
    username = request.state.auth.get("payload", {}).get("sub")
    user = db.query(models.User).filter(models.User.username == username).first()
    if not user:
        raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail="User not found")

    favorite = (
        db.query(models.ChartFavorite)
        .filter(models.ChartFavorite.id == favorite_id, models.ChartFavorite.user_id == user.id)
        .first()
    )
    if not favorite:
        raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail="Favorito nao encontrado")

    expires_at = None
    if payload.expires_in_hours:
        expires_at = datetime.utcnow() + timedelta(hours=payload.expires_in_hours)

    import secrets

    share_token = secrets.token_urlsafe(24)
    record = models.ChartFavoriteShare(
        favorite_id=favorite.id,
        share_token=share_token,
        expires_at=expires_at,
    )
    db.add(record)
    db.commit()
    db.refresh(record)

    return ChartFavoriteShareResponse(
        share_url=f"/charts/favorites/share/{record.share_token}",
        token=record.share_token,
        expires_at=record.expires_at,
    )


@router.get("/favorites/share/{token}", response_model=ChartFavoriteShareDetailResponse)
async def get_shared_favorite(token: str, db: Session = Depends(get_db)):
    record = db.query(models.ChartFavoriteShare).filter(models.ChartFavoriteShare.share_token == token).first()
    if not record:
        raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail="Share token nao encontrado")
    if record.expires_at and record.expires_at < datetime.utcnow():
        raise HTTPException(status_code=status.HTTP_410_GONE, detail="Share token expirado")

    favorite = db.query(models.ChartFavorite).filter(models.ChartFavorite.id == record.favorite_id).first()
    if not favorite:
        raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail="Favorito nao encontrado")

    categoria_codigo = None
    if favorite.categoria_id:
        categoria = db.query(models.ChatCategoria).filter(models.ChatCategoria.id == favorite.categoria_id).first()
        categoria_codigo = categoria.codigo if categoria else None

    return ChartFavoriteShareDetailResponse(
        titulo=favorite.titulo,
        categoria_codigo=categoria_codigo,
        chart=json.loads(favorite.chart_payload),
        expires_at=record.expires_at,
    )
