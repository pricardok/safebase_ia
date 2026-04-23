# app/models_hooks.py
from pydantic import BaseModel, Field, EmailStr
from typing import Optional, List, Any
from datetime import datetime
import json


class KiwifyProduct(BaseModel):
    product_id: str = Field(..., alias="product_id")
    product_name: str = Field(..., alias="product_name")

class KiwifyCustomer(BaseModel):
    full_name: str = Field(..., alias="full_name")
    first_name: str = Field(..., alias="first_name")
    email: EmailStr
    mobile: Optional[str] = None
    CPF: Optional[str] = None
    ip: Optional[str] = None
    street: Optional[str] = None
    number: Optional[str] = None
    complement: Optional[str] = None
    neighborhood: Optional[str] = None
    city: Optional[str] = None
    state: Optional[str] = None
    zipcode: Optional[str] = None

class KiwifyCommissionedStore(BaseModel):
    id: str
    type: str
    custom_name: str
    email: EmailStr
    value: str

class KiwifySubscriptionCharge(BaseModel):
    charge_date: datetime

class KiwifyCommissions(BaseModel):
    # Mapeando campos do payload Kiwify (inglês) para nomes internos (português)
    valor_cobranca: float = Field(alias="charge_amount")
    product_base_price: float
    product_base_price_currency: str
    kiwify_fee: float
    kiwify_fee_currency: str
    lojas_comissionadas: List[KiwifyCommissionedStore] = Field(alias="commissioned_stores")
    minha_comissao: float = Field(alias="my_commission")
    currency: str
    funds_status: str
    estimated_deposit_date: Optional[datetime]
    deposit_date: Optional[datetime]

class KiwifyTrackingParameters(BaseModel):
    src: Optional[str] = None
    sck: Optional[str] = None
    utm_source: Optional[str] = None
    utm_medium: Optional[str] = None
    utm_campaign: Optional[str] = None
    utm_content: Optional[str] = None
    utm_term: Optional[str] = None
    s1: Optional[str] = None
    s2: Optional[str] = None
    s3: Optional[str] = None

class KiwifySubscriptionPlan(BaseModel):
    id: str
    name: str
    frequency: str
    qty_charges: int

class KiwifySubscriptionChargesDetail(BaseModel):
    completed: List[Any]
    future: List[KiwifySubscriptionCharge]

class KiwifyCustomerAccess(BaseModel):
    has_access: bool
    active_period: bool
    access_until: Optional[datetime] = None

class KiwifySubscription(BaseModel):
    data_de_início: Optional[datetime] = Field(None, alias="start_date")
    próximo_pagamento: Optional[datetime] = Field(None, alias="next_payment")
    status: str
    customer_access: KiwifyCustomerAccess
    plano: KiwifySubscriptionPlan = Field(alias="plan")
    acusações: Optional[KiwifySubscriptionChargesDetail] = Field(None, alias="charges")

class KiwifyWebhookPayload(BaseModel):
    """
    Schema principal para o payload do webhook da Kiwify.
    """
    #  CAMPOS PRINCIPAIS (completos)
    order_id: str
    order_ref: Optional[str] = None
    order_status: Optional[str] = None
    product_type: Optional[str] = None
    payment_method: Optional[str] = None
    store_id: Optional[str] = None
    payment_merchant_id: Optional[str] = None # Corrigido para string
    installments: Optional[int] = None
    card_type: Optional[str] = None
    card_last4digits: Optional[str] = None
    card_rejection_reason: Optional[str] = None
    boleto_URL: Optional[str] = None
    boleto_barcode: Optional[str] = None
    boleto_expiry_date: Optional[str] = None
    pix_code: Optional[str] = None
    pix_expiration: Optional[str] = None
    sale_type: Optional[str] = None
    created_at: Optional[str] = None
    updated_at: Optional[str] = None
    approved_date: Optional[str] = None
    refunded_at: Optional[str] = None
    webhook_event_type: str
    subscription_id: Optional[str] = None
    access_url: Optional[str] = None
    
    Product: Optional[KiwifyProduct] = None
    Customer: Optional[KiwifyCustomer] = None  # Customer em inglês
    Commissions: Optional[KiwifyCommissions] = None # Tornando opcional para maior flexibilidade
    TrackingParameters: Optional[KiwifyTrackingParameters] = None
    Subscription: Optional[KiwifySubscription] # Subscription é opcional e em inglês

    class Config:
        populate_by_name = True
        #allow_population_by_field_name = True
        validate_by_name = True
        arbitrary_types_allowed = True
        json_encoders = {
            datetime: lambda v: v.isoformat() if v else None,
        }

    def get_customer_name(self) -> str:
        """Retorna o nome do cliente com fallback seguro."""
        customer = self.Customer
        if customer and (customer.full_name or customer.first_name):
            return customer.full_name or customer.first_name
        return "Novo Cliente"

    def get_customer_email(self) -> str:
        """Retorna o email do cliente com fallback seguro."""
        customer = self.Customer
        if not customer or not customer.email:
            raise ValueError("E-mail do cliente não encontrado no payload do webhook.")
        return customer.email

    def get_customer_phone(self) -> str:
        """Retorna o telefone do cliente, com fallback."""
        customer = self.Customer
        return customer.mobile or "N/A" if customer else "N/A"

    def get_product_id(self) -> Optional[str]:
        """Retorna o ID do produto."""
        return self.Product.product_id if self.Product else None

    def get_event_type(self) -> str:
        """Retorna o tipo de evento do webhook."""
        return self.webhook_event_type
