from datetime import datetime
from typing import List, Optional

from pydantic import BaseModel


class ChatConversationCreate(BaseModel):
    titulo: Optional[str] = None
    categoria_codigo: str


class ChatConversationRenameRequest(BaseModel):
    titulo: str


class ChatConversationPinRequest(BaseModel):
    pinned: bool


class ChatConversationSettingsRequest(BaseModel):
    temperature: Optional[float] = None
    max_tokens: Optional[int] = None


class ChatConversationSummary(BaseModel):
    id: int
    titulo: Optional[str]
    categoria_codigo: Optional[str] = None
    pinned: bool
    criado_em: datetime
    atualizado_em: datetime
    last_message_preview: Optional[str]


class ChatMessageResponse(BaseModel):
    id: int
    papel: str
    conteudo: str
    criado_em: datetime


class ChatConversationDetail(BaseModel):
    id: int
    titulo: Optional[str]
    categoria_codigo: Optional[str] = None
    pinned: bool
    temperature: Optional[float] = None
    max_tokens: Optional[int] = None
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


class ChatMessageListResponse(BaseModel):
    total: int
    messages: List[ChatMessageResponse]


class ChatConversationTitleResponse(BaseModel):
    titulo: str
