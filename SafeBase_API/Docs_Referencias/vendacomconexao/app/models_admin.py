# app/models_admin.py
from pydantic import BaseModel, Field
from typing import Optional, Literal
from datetime import datetime


class GlobalDiscountBase(BaseModel):
    codigo: str = Field(..., description="Código único do cupom (ex: VERAO20).")
    tipo: Literal['PERCENTUAL', 'FIXO'] = Field(..., description="Tipo de desconto.")
    valor: float = Field(..., description="Valor do desconto (ex: 20 para 20% ou 50.00 para R$50).")
    max_usos: int = Field(0, description="Número máximo de usos (0 para ilimitado).")
    expira_em: Optional[datetime] = Field(None, description="Data de expiração do cupom.")
    ativo: bool = Field(True, description="Status de ativação do cupom.")


class GlobalDiscountCreate(GlobalDiscountBase):
    pass


class GlobalDiscountUpdate(GlobalDiscountBase):
    pass


class GlobalDiscountResponse(GlobalDiscountBase):
    id: int
    usos_atuais: int
    criado_em: datetime

    class Config:
        from_attributes = True


class ApplyDiscountToClientRequest(BaseModel):
    codigo_cupom: str = Field(..., description="O código do cupom global a ser aplicado.")
    expira_em: Optional[datetime] = Field(None, description="Data de expiração específica para este cliente. Se omitido, usa a data do cupom global.")


class DescontoDetalhadoResponse(BaseModel):
    desconto_id: int
    desconto_tipo: str
    desconto_valor: float
    desconto_expira_em: Optional[datetime]
    desconto_ativo: bool

    class Config:
        from_attributes = True