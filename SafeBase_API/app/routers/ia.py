import hashlib
import json

from fastapi import APIRouter, Depends, HTTPException, status
from sqlalchemy.orm import Session

from app.core.dependencies import require_auth
from app.db import models
from app.db.models import ChaveIA, ProvedorIA
from app.db.session import get_db
from app.schemas.charts import ChartPayload
from app.schemas.ia import IAKeyCreateRequest, IAKeyCreateResponse, IAQueryRequest, IAQueryResponse
from app.services.crypto_manager import crypto_manager
from app.services.ia_service import IAService

router = APIRouter(tags=["IA"])

ia_service = IAService()


def _get_user_record(db: Session, auth: dict):
    if auth.get("type") != "jwt":
        return None
    username = auth.get("payload", {}).get("sub")
    if not username:
        return None
    return db.query(models.User).filter(models.User.username == username).first()


def _get_user_category_codes(db: Session, user_id: int):
    rows = (
        db.query(models.ChatCategoria.codigo)
        .join(models.UserCategoria, models.UserCategoria.categoria_id == models.ChatCategoria.id)
        .filter(models.UserCategoria.user_id == user_id, models.ChatCategoria.ativo == True)
        .all()
    )
    return {row[0] for row in rows}


def _resolve_category(db: Session, categoria_codigo: str):
    return (
        db.query(models.ChatCategoria)
        .filter(models.ChatCategoria.codigo == categoria_codigo, models.ChatCategoria.ativo == True)
        .first()
    )

@router.post("/ia/query", response_model=IAQueryResponse)
async def query_ia(payload: IAQueryRequest, db: Session = Depends(get_db), auth=Depends(require_auth)):
    if auth.get("type") == "jwt":
        if not payload.categoria_codigo:
            raise HTTPException(
                status_code=status.HTTP_422_UNPROCESSABLE_ENTITY,
                detail="categoria_codigo obrigatoria",
            )
        categoria = _resolve_category(db, payload.categoria_codigo)
        if not categoria:
            raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail="Categoria nao encontrada")

        user_record = _get_user_record(db, auth)
        if user_record:
            allowed = _get_user_category_codes(db, user_record.id)
            if payload.categoria_codigo not in allowed:
                raise HTTPException(status_code=status.HTTP_403_FORBIDDEN, detail="Categoria nao autorizada")

    result = ia_service.query_insights(payload.dict())

    mode = (payload.mode or "rapido").lower()
    if mode == "grafico":
        chart = ChartPayload(
            type="line",
            title=f"Evolução - {payload.categoria_codigo or 'categoria'}",
            labels=["Jan", "Fev", "Mar", "Abr"],
            datasets=[{"label": "Quantidade", "data": [120, 135, 148, 162]}],
        )
        return IAQueryResponse(
            query=payload.query,
            response="Aqui está o gráfico solicitado.",
            chart=chart,
            insights="Aumento de 35% no período.",
            result=result,
        )

    return IAQueryResponse(
        query=payload.query,
        response=result.get("resposta") if isinstance(result, dict) else None,
        result=result if isinstance(result, dict) else {"raw": result},
    )


@router.post("/ia/keys", response_model=IAKeyCreateResponse, status_code=status.HTTP_201_CREATED)
def create_ia_key(
    payload: IAKeyCreateRequest,
    db: Session = Depends(get_db),
    auth=Depends(require_auth),
):
    if not payload.provider_id and not payload.provider_name:
        raise HTTPException(
            status_code=status.HTTP_422_UNPROCESSABLE_ENTITY,
            detail="provider_id ou provider_name sao obrigatorios",
        )

    query = db.query(ProvedorIA)
    if payload.provider_id:
        provider = query.filter(ProvedorIA.id == payload.provider_id).first()
    else:
        provider = query.filter(ProvedorIA.nome == payload.provider_name).first()

    if not provider:
        raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail="Provedor nao encontrado")

    hash_chave = hashlib.sha256(payload.api_key.encode("utf-8")).hexdigest()
    metadata_text = json.dumps(payload.metadados or {}, ensure_ascii=False)

    encrypted_key = crypto_manager.encrypt_key(payload.api_key)
    key_record = ChaveIA(
        provedor_id=provider.id,
        hash_chave=hash_chave,
        chave_criptografada=encrypted_key,
        descricao=payload.descricao,
        prioridade=payload.prioridade or 1,
        ativo=True if payload.ativa is None else payload.ativa,
        metadados=metadata_text,
    )
    db.add(key_record)
    db.commit()
    db.refresh(key_record)

    return IAKeyCreateResponse(
        id=key_record.id,
        provider_id=provider.id,
        provider_name=provider.nome,
        hash_chave=key_record.hash_chave,
        descricao=key_record.descricao,
        ativa=key_record.ativo,
        prioridade=key_record.prioridade,
    )
