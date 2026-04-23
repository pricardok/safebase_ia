# backend/app/models_email.py
from pydantic import BaseModel, Field, EmailStr
from typing import Optional, List, Dict, Any, Literal
from datetime import datetime


class EmailTemplateType(str, Literal):
    SYSTEM = "SYSTEM"
    CAMPAIGN = "CAMPAIGN"

class EmailTemplateBase(BaseModel):
    chave: str = Field(..., description="Chave única para identificação programática (ex: WELCOME_USER).")
    nome: str = Field(..., description="Nome amigável para o painel.")
    assunto: str = Field(..., description="Assunto padrão do e-mail.")
    html_content: str = Field(..., description="Conteúdo HTML do template.")
    text_content: Optional[str] = Field(None, description="Conteúdo em texto puro (fallback).")
    variaveis_disponiveis: List[str] = Field([], description="Lista de strings de variáveis que o template aceita.")
    tipo: EmailTemplateType = Field(..., description="Tipo de template: SYSTEM (essencial) ou CAMPAIGN (marketing).")
    ativo: bool = Field(True, description="Status de ativação.")

class EmailTemplateCreate(EmailTemplateBase):
    pass

class EmailTemplateUpdate(BaseModel):
    nome: Optional[str] = None
    assunto: Optional[str] = None
    html_content: Optional[str] = None
    text_content: Optional[str] = None
    variaveis_disponiveis: Optional[List[str]] = None
    ativo: Optional[bool] = None
    # 'chave' e 'tipo' não são atualizáveis por design, especialmente para 'SYSTEM'

class EmailTemplateResponse(EmailTemplateBase):
    id: int
    created_at: datetime
    updated_at: datetime

    class Config:
        from_attributes = True

class PasswordResetRequest(BaseModel):
    email: EmailStr

class PasswordResetResponse(BaseModel):
    success: bool
    message: str
    temporary_password: Optional[str] = None # Apenas para ambiente de desenvolvimento