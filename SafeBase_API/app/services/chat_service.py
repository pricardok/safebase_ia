import json
from datetime import datetime
from typing import List, Optional, Tuple

from sqlalchemy.orm import Session

from app.db.models import ChatConversation, ChatMessage


class ChatService:
    def list_conversations(self, db: Session, user_id: str, limit: int = 50) -> List[Tuple[ChatConversation, Optional[ChatMessage]]]:
        conversations = (
            db.query(ChatConversation)
            .filter(ChatConversation.usuario_id == user_id)
            .order_by(ChatConversation.atualizado_em.desc())
            .limit(limit)
            .all()
        )

        result = []
        for conversation in conversations:
            last_message = (
                db.query(ChatMessage)
                .filter(ChatMessage.conversa_id == conversation.id)
                .order_by(ChatMessage.criado_em.desc())
                .first()
            )
            result.append((conversation, last_message))

        return result

    def create_conversation(self, db: Session, user_id: str, title: Optional[str]) -> ChatConversation:
        conversation = ChatConversation(
            titulo=title or "Nova conversa",
            usuario_id=user_id,
        )
        db.add(conversation)
        db.commit()
        db.refresh(conversation)
        return conversation

    def get_conversation(self, db: Session, conversation_id: int, user_id: str) -> Optional[ChatConversation]:
        return (
            db.query(ChatConversation)
            .filter(ChatConversation.id == conversation_id, ChatConversation.usuario_id == user_id)
            .first()
        )

    def get_messages(self, db: Session, conversation_id: int) -> List[ChatMessage]:
        return (
            db.query(ChatMessage)
            .filter(ChatMessage.conversa_id == conversation_id)
            .order_by(ChatMessage.criado_em.asc())
            .all()
        )

    def get_recent_messages(self, db: Session, conversation_id: int, limit: int = 10) -> List[ChatMessage]:
        messages = (
            db.query(ChatMessage)
            .filter(ChatMessage.conversa_id == conversation_id)
            .order_by(ChatMessage.criado_em.desc())
            .limit(limit)
            .all()
        )
        return list(reversed(messages))

    def add_message(
        self,
        db: Session,
        conversation: ChatConversation,
        role: str,
        content: str,
        metadata: Optional[dict] = None,
    ) -> ChatMessage:
        message = ChatMessage(
            conversa_id=conversation.id,
            papel=role,
            conteudo=content,
            metadados=json.dumps(metadata or {}, ensure_ascii=False),
        )
        db.add(message)

        if role == "user" and (not conversation.titulo or conversation.titulo == "Nova conversa"):
            conversation.titulo = content[:60].strip() or "Nova conversa"

        conversation.atualizado_em = datetime.utcnow()
        db.commit()
        db.refresh(message)
        return message
