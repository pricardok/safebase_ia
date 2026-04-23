# backend/app/models_planos.py
from pydantic import BaseModel, Field, validator
from typing import Optional, List, Dict, Any
from datetime import datetime
from enum import Enum

class TipoPlanoEnum(str, Enum):
    TRIAL = "trial"
    PAGO = "pago"
    FREE = "free"

class TipoDescontoEnum(str, Enum):
    PERCENTUAL = "percentual"
    FIXO = "fixo"

class PlanoBase(BaseModel):
    """Modelo base para plano"""
    nome: str = Field(..., description="Nome do plano")
    descricao: Optional[str] = Field(None, description="Descrição do plano")
    tipo: TipoPlanoEnum = Field(TipoPlanoEnum.PAGO, description="Tipo do plano")
    preco_mensal: float = Field(0, description="Preço mensal do plano")
    max_clientes: int = Field(100, description="Número máximo de clientes")
    max_usuarios: int = Field(3, description="Número máximo de usuários")
    features: Dict[str, Any] = Field({}, description="Features do plano")
    ordem_exibicao: int = Field(1, description="Ordem de exibição")

class PlanoCreate(PlanoBase):
    """Modelo para criação de plano"""
    pass

class PlanoResponse(PlanoBase):
    """Modelo para resposta de plano"""
    id: int = Field(..., description="ID do plano")
    ativo: bool = Field(..., description="Se o plano está ativo")
    created_at: datetime = Field(..., description="Data de criação")
    updated_at: datetime = Field(..., description="Data de atualização")

    class Config:
        from_attributes = True

class PlanoPublicoResponse(BaseModel):
    """Modelo para resposta de plano na listagem pública (sem campos de auditoria)"""
    id: int
    nome: str
    descricao: Optional[str]
    preco_mensal: float
    max_clientes: int
    max_usuarios: int
    features: Dict[str, Any]
    ordem_exibicao: int

class DescontoClienteBase(BaseModel):
    """Modelo base para desconto de cliente"""
    plano_id: int = Field(..., description="ID do plano")
    tipo: TipoDescontoEnum = Field(..., description="Tipo de desconto")
    valor: float = Field(..., description="Valor do desconto")
    expira_em: Optional[datetime] = Field(None, description="Data de expiração do desconto")
    descricao: Optional[str] = Field(None, description="Descrição do desconto")

    @validator('valor')
    def validate_valor(cls, v, values):
        tipo = values.get('tipo')
        if tipo == TipoDescontoEnum.PERCENTUAL and (v < 0 or v > 100):
            raise ValueError('Desconto percentual deve estar entre 0 e 100')
        elif tipo == TipoDescontoEnum.FIXO and v < 0:
            raise ValueError('Desconto fixo não pode ser negativo')
        return v

class DescontoClienteCreate(DescontoClienteBase):
    """Modelo para criação de desconto"""
    cliente_id: str = Field(..., description="ID do cliente")

class DescontoClienteResponse(DescontoClienteBase):
    """Modelo para resposta de desconto"""
    id: int = Field(..., description="ID do desconto")
    cliente_id: str = Field(..., description="ID do cliente")
    ativo: bool = Field(..., description="Se o desconto está ativo")
    created_at: datetime = Field(..., description="Data de criação")
    updated_at: datetime = Field(..., description="Data de atualização")

    class Config:
        from_attributes = True

class PlanoEfetivoResponse(BaseModel):
    """Modelo para resposta do plano efetivo (com desconto aplicado)"""
    id: int = Field(..., description="ID do plano")
    nome: str = Field(..., description="Nome do plano")
    descricao: Optional[str] = Field(None, description="Descrição do plano")
    tipo: str = Field(..., description="Tipo do plano")
    preco_base: float = Field(..., description="Preço base do plano")
    preco_final: float = Field(..., description="Preço final com desconto")
    max_clientes: int = Field(..., description="Número máximo de clientes")
    max_usuarios: int = Field(..., description="Número máximo de usuários")
    features: Dict[str, Any] = Field(..., description="Features do plano")
    desconto: Optional[Dict[str, Any]] = Field(None, description="Desconto aplicado")
    trial_expires_at: Optional[datetime] = Field(None, description="Data de expiração do trial")
    em_trial: bool = Field(..., description="Se está em período de trial")
    dias_restantes_trial: int = Field(0, description="Dias restantes de trial")
    status_cliente: str = Field(..., description="Status do cliente")

    class Config:
        from_attributes = True

class EstatisticasUsoResponse(BaseModel):
    """Modelo para estatísticas de uso do cliente"""
    clientes: int = Field(..., description="Total de clientes")
    usuarios: int = Field(..., description="Total de usuários")
    limites: Dict[str, int] = Field(..., description="Limites do plano")
    percentual_clientes: float = Field(0, description="Percentual de uso de clientes")
    percentual_usuarios: float = Field(0, description="Percentual de uso de usuários")

    class Config:
        from_attributes = True

class SignupRequest(BaseModel):
    """Modelo para requisição de signup público"""
    razao_social: str = Field(..., description="Razão social da empresa")
    nome_fantasia: Optional[str] = Field(None, description="Nome fantasia (se aplicável)")
    documento: str = Field(..., description="CPF (11 dígitos) ou CNPJ (14 dígitos) da entidade")
    tipo_pessoa: str = Field(..., description="Tipo de pessoa: 'F' para Física, 'J' para Jurídica")
    email_contato: str = Field(..., description="Email de contato principal")
    telefone: str = Field(..., description="Telefone de contato")
    username: str = Field(..., description="Nome de usuário desejado")
    password: str = Field(..., description="Senha do usuário")
    full_name: str = Field(..., description="Nome completo do usuário")

    @validator('tipo_pessoa')
    def validate_tipo_pessoa(cls, v):
        if v.upper() not in ['F', 'J']:
            raise ValueError("Tipo de pessoa deve ser 'F' ou 'J'")
        return v.upper()

    @validator('documento')
    def validate_documento(cls, v, values):
        tipo_pessoa = values.get('tipo_pessoa', '').upper()
        doc_clean = "".join(filter(str.isdigit, v))
        if tipo_pessoa == 'J' and len(doc_clean) != 14:
            raise ValueError('CNPJ (tipo J) deve ter 14 dígitos numéricos')
        if tipo_pessoa == 'F' and len(doc_clean) != 11:
            raise ValueError('CPF (tipo F) deve ter 11 dígitos numéricos')
        return doc_clean

class UpgradePlanoRequest(BaseModel):
    """Modelo para requisição de upgrade de plano"""
    plano_id: int = Field(..., description="ID do plano para upgrade")

class UserWithTenantAndPlano(BaseModel):
    """Modelo extendido de usuário com informações de tenant e plano"""
    id: int = Field(..., description="ID do usuário")
    username: str = Field(..., description="Nome de usuário")
    email: str = Field(..., description="Email")
    full_name: str = Field(None, description="Nome completo")
    telefone: Optional[str] = Field(None, description="Telefone do usuário")
    is_active: bool = Field(..., description="Usuário ativo")
    perfil: str = Field(..., description="Perfil do usuário")
    permissoes: List[str] = Field(..., description="Permissões do usuário")
    auth_method: str = Field(..., description="Método de autenticação")
    is_super_access: bool = Field(..., description="Se é super acesso")
    cliente_id: Optional[str] = Field(None, description="ID do cliente")
    cliente_razao_social: Optional[str] = Field(None, description="Razão social do cliente")
    cliente_nome_fantasia: Optional[str] = Field(None, description="Nome fantasia do cliente")
    tenant_status: Optional[str] = Field(None, description="Status do tenant")
    trial_expires_at: Optional[datetime] = Field(None, description="Data de expiração do trial")
    plano_atual: Optional[PlanoEfetivoResponse] = Field(None, description="Plano atual do tenant")

    class Config:
        from_attributes = True

class PlanoUpdateRequest(BaseModel):
    """Modelo para atualização de um plano. Todos os campos são opcionais."""
    nome: Optional[str] = None
    descricao: Optional[str] = None
    preco_mensal: Optional[float] = None
    max_clientes: Optional[int] = None
    max_usuarios: Optional[int] = None
    features: Optional[Dict[str, Any]] = None
    ordem_exibicao: Optional[int] = None
    ativo: Optional[bool] = None