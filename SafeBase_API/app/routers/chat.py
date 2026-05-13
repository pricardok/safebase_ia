from fastapi import APIRouter, Depends, HTTPException, status
from sqlalchemy.orm import Session

from app.core.dependencies import require_api_key
from app.db.session import get_db
from app.schemas.chat import (
    ChatConversationCreate,
    ChatConversationDetail,
    ChatConversationListResponse,
    ChatConversationSummary,
    ChatMessageResponse,
    ChatMessageSendRequest,
    ChatMessageSendResponse,
)
from app.services.chat_orchestrator import ChatOrchestrator
from app.services.chat_service import ChatService

router = APIRouter()
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


@router.get("/chat/conversas", response_model=ChatConversationListResponse)
def list_conversations(db: Session = Depends(get_db), auth=Depends(require_api_key)):
    user_id = _resolve_user_id(auth)
    items = chat_service.list_conversations(db, user_id)

    summaries = []
    for conversation, last_message in items:
        preview = last_message.conteudo[:120] if last_message else None
        summaries.append(
            ChatConversationSummary(
                id=conversation.id,
                titulo=conversation.titulo,
                preview=preview,
                atualizado_em=conversation.atualizado_em,
            )
        )

    return ChatConversationListResponse(conversas=summaries)


@router.post("/chat/conversas", response_model=ChatConversationDetail, status_code=status.HTTP_201_CREATED)
def create_conversation(
    payload: ChatConversationCreate,
    db: Session = Depends(get_db),
    auth=Depends(require_api_key),
):
    user_id = _resolve_user_id(auth)
    conversation = chat_service.create_conversation(db, user_id, payload.titulo)
    return ChatConversationDetail(
        id=conversation.id,
        titulo=conversation.titulo,
        criado_em=conversation.criado_em,
        atualizado_em=conversation.atualizado_em,
        mensagens=[],
    )


@router.get("/chat/conversas/{conversation_id}", response_model=ChatConversationDetail)
def get_conversation(
    conversation_id: int,
    db: Session = Depends(get_db),
    auth=Depends(require_api_key),
):
    user_id = _resolve_user_id(auth)
    conversation = chat_service.get_conversation(db, conversation_id, user_id)
    if not conversation:
        raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail="Conversa nao encontrada")

    messages = chat_service.get_messages(db, conversation_id)
    return ChatConversationDetail(
        id=conversation.id,
        titulo=conversation.titulo,
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


@router.post("/chat/conversas/{conversation_id}/mensagens", response_model=ChatMessageSendResponse)
def send_message(
    conversation_id: int,
    payload: ChatMessageSendRequest,
    db: Session = Depends(get_db),
    auth=Depends(require_api_key),
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
