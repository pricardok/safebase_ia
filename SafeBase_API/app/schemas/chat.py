from datetime import datetime
from typing import List, Optional

from pydantic import BaseModel


class ChatConversationCreate(BaseModel):
    titulo: Optional[str] = None


class ChatConversationSummary(BaseModel):
    id: int
    titulo: Optional[str]
    preview: Optional[str]
    atualizado_em: datetime


class ChatMessageResponse(BaseModel):
    id: int
    papel: str
    conteudo: str
    criado_em: datetime


class ChatConversationDetail(BaseModel):
    id: int
    titulo: Optional[str]
    criado_em: datetime
    atualizado_em: datetime
    mensagens: List[ChatMessageResponse]


class ChatConversationListResponse(BaseModel):
    conversas: List[ChatConversationSummary]


class ChatMessageSendRequest(BaseModel):
    conteudo: str


class ChatMessageSendResponse(BaseModel):
    conversa_id: int
    user_message: ChatMessageResponse
    assistant_message: ChatMessageResponse
