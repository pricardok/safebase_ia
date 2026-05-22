from fastapi import APIRouter, Depends, HTTPException, status
from sqlalchemy.orm import Session

from app.core.dependencies import require_auth
from app.db.session import get_db
from app.db import models
from typing import Optional, Set
from app.schemas.chat import (
    ChatConversationCreate,
    ChatConversationDetail,
    ChatConversationListResponse,
    ChatConversationPinRequest,
    ChatConversationRenameRequest,
    ChatConversationSettingsRequest,
    ChatConversationSummary,
    ChatMessageListResponse,
    ChatConversationTitleResponse,
    ChatMessageResponse,
    ChatMessageSendRequest,
    ChatMessageSendResponse,
)
from app.services.chat_orchestrator import ChatOrchestrator
from app.services.chat_service import ChatService

router = APIRouter(tags=["Chat"])
chat_service = ChatService()
chat_orchestrator = ChatOrchestrator()


def _resolve_user_id(auth: dict) -> str:
    if auth.get("type") == "jwt":
        return auth.get("payload", {}).get("sub", "jwt:unknown")
    if auth.get("type") == "api_key":
        record = auth.get("record", {})
        name = record.get("name") or "unknown"
        return f"api_key:{name}"
    return "unknown"


def _get_user_record(db: Session, auth: dict) -> Optional[models.User]:
    if auth.get("type") != "jwt":
        return None
    username = auth.get("payload", {}).get("sub")
    if not username:
        return None
    return db.query(models.User).filter(models.User.username == username).first()


def _get_user_category_codes(db: Session, user_id: int) -> Set[str]:
    rows = (
        db.query(models.ChatCategoria.codigo)
        .join(models.UserCategoria, models.UserCategoria.categoria_id == models.ChatCategoria.id)
        .filter(models.UserCategoria.user_id == user_id, models.ChatCategoria.ativo == True)
        .all()
    )
    return {row[0] for row in rows}


def _resolve_category(db: Session, categoria_codigo: str) -> Optional[models.ChatCategoria]:
    return (
        db.query(models.ChatCategoria)
        .filter(models.ChatCategoria.codigo == categoria_codigo, models.ChatCategoria.ativo == True)
        .first()
    )


@router.get("/chat/conversas", response_model=ChatConversationListResponse)
def list_conversations(db: Session = Depends(get_db), auth=Depends(require_auth)):
    user_id = _resolve_user_id(auth)
    items = chat_service.list_conversations(db, user_id)

    categoria_ids = {conversation.categoria_id for conversation, _ in items if conversation.categoria_id}
    categorias_map = {}
    if categoria_ids:
        categorias = db.query(models.ChatCategoria).filter(models.ChatCategoria.id.in_(categoria_ids)).all()
        categorias_map = {categoria.id: categoria.codigo for categoria in categorias}

    summaries = []
    for conversation, last_message in items:
        preview = last_message.conteudo[:120] if last_message else None
        summaries.append(
            ChatConversationSummary(
                id=conversation.id,
                titulo=conversation.titulo,
                categoria_codigo=categorias_map.get(conversation.categoria_id),
                pinned=conversation.pinned,
                criado_em=conversation.criado_em,
                atualizado_em=conversation.atualizado_em,
                last_message_preview=preview,
            )
        )

    return ChatConversationListResponse(conversas=summaries)


@router.post("/chat/conversas", response_model=ChatConversationDetail, status_code=status.HTTP_201_CREATED)
def create_conversation(
    payload: ChatConversationCreate,
    db: Session = Depends(get_db),
    auth=Depends(require_auth),
):
    user_id = _resolve_user_id(auth)
    categoria = _resolve_category(db, payload.categoria_codigo)
    if not categoria:
        raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail="Categoria nao encontrada")

    user_record = _get_user_record(db, auth)
    if user_record:
        allowed = _get_user_category_codes(db, user_record.id)
        if payload.categoria_codigo not in allowed:
            raise HTTPException(status_code=status.HTTP_403_FORBIDDEN, detail="Categoria nao autorizada")

    conversation = chat_service.create_conversation(db, user_id, payload.titulo, categoria.id)
    return ChatConversationDetail(
        id=conversation.id,
        titulo=conversation.titulo,
        categoria_codigo=payload.categoria_codigo,
        pinned=conversation.pinned,
        temperature=float(conversation.temperature) if conversation.temperature is not None else None,
        max_tokens=conversation.max_tokens,
        criado_em=conversation.criado_em,
        atualizado_em=conversation.atualizado_em,
        mensagens=[],
    )


@router.get("/chat/conversas/{conversation_id}", response_model=ChatConversationDetail)
def get_conversation(
    conversation_id: int,
    db: Session = Depends(get_db),
    auth=Depends(require_auth),
):
    user_id = _resolve_user_id(auth)
    conversation = chat_service.get_conversation(db, conversation_id, user_id)
    if not conversation:
        raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail="Conversa nao encontrada")

    categoria_codigo = None
    if conversation.categoria_id:
        categoria = db.query(models.ChatCategoria).filter(models.ChatCategoria.id == conversation.categoria_id).first()
        categoria_codigo = categoria.codigo if categoria else None

    messages = chat_service.get_messages(db, conversation_id)
    return ChatConversationDetail(
        id=conversation.id,
        titulo=conversation.titulo,
        categoria_codigo=categoria_codigo,
        pinned=conversation.pinned,
        temperature=float(conversation.temperature) if conversation.temperature is not None else None,
        max_tokens=conversation.max_tokens,
        criado_em=conversation.criado_em,
        atualizado_em=conversation.atualizado_em,
        mensagens=[
            ChatMessageResponse(
                id=msg.id,
                papel=msg.papel,
                conteudo=msg.conteudo,
                criado_em=msg.criado_em,
            )
            for msg in messages
        ],
    )


@router.get("/chat/conversas/{conversation_id}/mensagens", response_model=ChatMessageListResponse)
def list_messages(
    conversation_id: int,
    limit: int = 50,
    offset: int = 0,
    db: Session = Depends(get_db),
    auth=Depends(require_auth),
):
    user_id = _resolve_user_id(auth)
    conversation = chat_service.get_conversation(db, conversation_id, user_id)
    if not conversation:
        raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail="Conversa nao encontrada")

    total, messages = chat_service.get_messages_paginated(db, conversation_id, limit, offset)
    return ChatMessageListResponse(
        total=total,
        messages=[
            ChatMessageResponse(
                id=msg.id,
                papel=msg.papel,
                conteudo=msg.conteudo,
                criado_em=msg.criado_em,
            )
            for msg in messages
        ],
    )


@router.post("/chat/conversas/{conversation_id}/mensagens", response_model=ChatMessageSendResponse)
def send_message(
    conversation_id: int,
    payload: ChatMessageSendRequest,
    db: Session = Depends(get_db),
    auth=Depends(require_auth),
):
    user_id = _resolve_user_id(auth)
    user_message, assistant_message = chat_orchestrator.handle_user_message(
        db, conversation_id, user_id, payload.conteudo
    )
    if not user_message or not assistant_message:
        raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail="Conversa nao encontrada")

    return ChatMessageSendResponse(
        conversa_id=conversation_id,
        user_message=ChatMessageResponse(
            id=user_message.id,
            papel=user_message.papel,
            conteudo=user_message.conteudo,
            criado_em=user_message.criado_em,
        ),
        assistant_message=ChatMessageResponse(
            id=assistant_message.id,
            papel=assistant_message.papel,
            conteudo=assistant_message.conteudo,
            criado_em=assistant_message.criado_em,
        ),
    )


@router.patch("/chat/conversas/{conversation_id}/rename", response_model=ChatConversationDetail)
def rename_conversation(
    conversation_id: int,
    payload: ChatConversationRenameRequest,
    db: Session = Depends(get_db),
    auth=Depends(require_auth),
):
    user_id = _resolve_user_id(auth)
    conversation = chat_service.get_conversation(db, conversation_id, user_id)
    if not conversation:
        raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail="Conversa nao encontrada")

    conversation = chat_service.rename_conversation(db, conversation, payload.titulo)
    categoria_codigo = None
    if conversation.categoria_id:
        categoria = db.query(models.ChatCategoria).filter(models.ChatCategoria.id == conversation.categoria_id).first()
        categoria_codigo = categoria.codigo if categoria else None
    return ChatConversationDetail(
        id=conversation.id,
        titulo=conversation.titulo,
        categoria_codigo=categoria_codigo,
        pinned=conversation.pinned,
        temperature=float(conversation.temperature) if conversation.temperature is not None else None,
        max_tokens=conversation.max_tokens,
        criado_em=conversation.criado_em,
        atualizado_em=conversation.atualizado_em,
        mensagens=[],
    )


@router.patch("/chat/conversas/{conversation_id}/pin", response_model=ChatConversationDetail)
def pin_conversation(
    conversation_id: int,
    payload: ChatConversationPinRequest,
    db: Session = Depends(get_db),
    auth=Depends(require_auth),
):
    user_id = _resolve_user_id(auth)
    conversation = chat_service.get_conversation(db, conversation_id, user_id)
    if not conversation:
        raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail="Conversa nao encontrada")

    conversation = chat_service.pin_conversation(db, conversation, payload.pinned)
    categoria_codigo = None
    if conversation.categoria_id:
        categoria = db.query(models.ChatCategoria).filter(models.ChatCategoria.id == conversation.categoria_id).first()
        categoria_codigo = categoria.codigo if categoria else None
    return ChatConversationDetail(
        id=conversation.id,
        titulo=conversation.titulo,
        categoria_codigo=categoria_codigo,
        pinned=conversation.pinned,
        temperature=float(conversation.temperature) if conversation.temperature is not None else None,
        max_tokens=conversation.max_tokens,
        criado_em=conversation.criado_em,
        atualizado_em=conversation.atualizado_em,
        mensagens=[],
    )


@router.patch("/chat/conversas/{conversation_id}/settings", response_model=ChatConversationDetail)
def update_settings(
    conversation_id: int,
    payload: ChatConversationSettingsRequest,
    db: Session = Depends(get_db),
    auth=Depends(require_auth),
):
    user_id = _resolve_user_id(auth)
    conversation = chat_service.get_conversation(db, conversation_id, user_id)
    if not conversation:
        raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail="Conversa nao encontrada")

    conversation = chat_service.update_settings(db, conversation, payload.temperature, payload.max_tokens)
    categoria_codigo = None
    if conversation.categoria_id:
        categoria = db.query(models.ChatCategoria).filter(models.ChatCategoria.id == conversation.categoria_id).first()
        categoria_codigo = categoria.codigo if categoria else None
    return ChatConversationDetail(
        id=conversation.id,
        titulo=conversation.titulo,
        categoria_codigo=categoria_codigo,
        pinned=conversation.pinned,
        temperature=float(conversation.temperature) if conversation.temperature is not None else None,
        max_tokens=conversation.max_tokens,
        criado_em=conversation.criado_em,
        atualizado_em=conversation.atualizado_em,
        mensagens=[],
    )


@router.post("/chat/conversas/{conversation_id}/generate-title", response_model=ChatConversationTitleResponse)
def generate_title(
    conversation_id: int,
    db: Session = Depends(get_db),
    auth=Depends(require_auth),
):
    user_id = _resolve_user_id(auth)
    conversation = chat_service.get_conversation(db, conversation_id, user_id)
    if not conversation:
        raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail="Conversa nao encontrada")

    title = chat_service.generate_title(db, conversation)
    conversation = chat_service.rename_conversation(db, conversation, title)
    return ChatConversationTitleResponse(titulo=conversation.titulo)


@router.delete("/chat/conversas/{conversation_id}", status_code=status.HTTP_204_NO_CONTENT)
def delete_conversation(
    conversation_id: int,
    db: Session = Depends(get_db),
    auth=Depends(require_auth),
):
    user_id = _resolve_user_id(auth)
    deleted = chat_service.delete_conversation(db, conversation_id, user_id)
    if not deleted:
        raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail="Conversa nao encontrada")
    return None
