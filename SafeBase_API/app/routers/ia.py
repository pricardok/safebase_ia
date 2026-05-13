import hashlib
import json

from fastapi import APIRouter, Depends, HTTPException, status
from sqlalchemy.orm import Session

from app.core.dependencies import require_api_key
from app.db.models import ChaveIA, ProvedorIA
from app.db.session import get_db
from app.schemas.ia import IAKeyCreateRequest, IAKeyCreateResponse, IAQueryRequest, IAQueryResponse
from app.services.crypto_manager import crypto_manager
from app.services.ia_service import IAService

router = APIRouter()

ia_service = IAService()

@router.post("/ia/query", response_model=IAQueryResponse)
async def query_ia(payload: IAQueryRequest, auth=Depends(require_api_key)):
    result = ia_service.query_insights(payload.dict())
    return {"query": payload.query, "result": result}


@router.post("/ia/keys", response_model=IAKeyCreateResponse, status_code=status.HTTP_201_CREATED)
def create_ia_key(
    payload: IAKeyCreateRequest,
    db: Session = Depends(get_db),
    auth=Depends(require_api_key),
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
    )
