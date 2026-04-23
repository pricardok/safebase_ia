# backend/app/services/email_service.py
import os
import logging
from datetime import datetime
from abc import ABC, abstractmethod
from typing import List, Dict, Any, Optional
import httpx
from app.models import (
    EmailRequest, EmailResponse, EmailRecipient, EmailContent,
    EmailConfig, EmailProviderEnum, AdminEmailStatusResponse
)
from app.database import get_email_template_by_chave
import re


logger = logging.getLogger(__name__)

class BaseEmailProvider(ABC):
    """Interface base para provedores de email"""
    
    @abstractmethod
    async def send_email(self, to: List[EmailRecipient], content: EmailContent) -> EmailResponse:
        pass
    
    @abstractmethod
    def get_provider_name(self) -> str:
        pass
    
    @abstractmethod
    def is_configured(self) -> bool:
        pass

class BrevoEmailProvider(BaseEmailProvider):
    """Provedor de email usando Brevo (Sendinblue)"""
    
    def __init__(self, config: EmailConfig):
        self.config = config
        self.base_url = "https://api.brevo.com/v3/smtp/email"
        self.headers = {
            "accept": "application/json",
            "api-key": config.api_key,
            "content-type": "application/json"
        }
    
    def get_provider_name(self) -> str:
        return EmailProviderEnum.BREVO.value
    
    def is_configured(self) -> bool:
        return bool(self.config.api_key and self.config.sender_email)
    
    async def send_email(self, to: List[EmailRecipient], content: EmailContent) -> EmailResponse:
        try:
            email_data = {
                "sender": {
                    "name": self.config.sender_name,
                    "email": self.config.sender_email
                },
                "to": [{"email": recipient.email, "name": recipient.name} for recipient in to],
                "subject": content.subject,
                "htmlContent": content.html_content
            }
            
            # Adicionar conteúdo em texto puro se disponível
            if content.text_content:
                email_data["textContent"] = content.text_content
            
            async with httpx.AsyncClient() as client:
                response = await client.post(
                    self.base_url,
                    headers=self.headers,
                    json=email_data,
                    timeout=30.0
                )
                
                if response.status_code == 201:
                    response_data = response.json()
                    email_id = response_data.get('messageId')
                    
                    logger.info(f"Email enviado com sucesso via {self.get_provider_name()} - ID: {email_id}")
                    return EmailResponse(
                        success=True,
                        message="Email enviado com sucesso",
                        provider_used=self.get_provider_name(),
                        email_id=email_id
                    )
                else:
                    error_msg = f"Erro ao enviar email: {response.status_code} - {response.text}"
                    logger.error(error_msg)
                    return EmailResponse(
                        success=False,
                        message=error_msg,
                        provider_used=self.get_provider_name()
                    )
                    
        except Exception as e:
            error_msg = f"Erro no provedor {self.get_provider_name()}: {str(e)}"
            logger.error(error_msg)
            return EmailResponse(
                success=False,
                message=error_msg,
                provider_used=self.get_provider_name()
            )

class MockEmailProvider(BaseEmailProvider):
    """Provedor mock para desenvolvimento e testes"""
    
    def __init__(self, config: EmailConfig):
        self.config = config
    
    def get_provider_name(self) -> str:
        return EmailProviderEnum.MOCK.value
    
    def is_configured(self) -> bool:
        return True
    
    async def send_email(self, to: List[EmailRecipient], content: EmailContent) -> EmailResponse:
        logger.info(f"[MOCK] Simulando envio de email para {len(to)} destinatário(s)")
        logger.info(f"[MOCK] Assunto: {content.subject}")
        logger.info(f"[MOCK] De: {self.config.sender_name} <{self.config.sender_email}>")
        
        for recipient in to:
            logger.info(f"[MOCK] Para: {recipient.name} <{recipient.email}>")
        
        # Simular delay de rede
        import asyncio
        await asyncio.sleep(0.5)
        
        return EmailResponse(
            success=True,
            message="Email enviado com sucesso (MOCK)",
            provider_used=self.get_provider_name(),
            email_id="mock_email_id_12345"
        )

class EmailService:
    """Serviço genérico de email com suporte a múltiplos provedores"""
    
    def __init__(self):
        self.providers: Dict[str, BaseEmailProvider] = {}
        self.default_provider: Optional[str] = None
        self._initialized = False
        self._last_test_result: Optional[str] = None # 'sucesso', 'falha'
        self._last_test_timestamp: Optional[datetime] = None
        self._last_error: Optional[str] = None
    
    def initialize(self):
        """Inicializa o serviço de email com os provedores configurados"""
        if self._initialized:
            return
            
        try:
            api_key = os.getenv("YOUR_API_V3_KEY")
            sender_email = os.getenv("EMAIL_ENVIO_SISTEMA")
            
            if not api_key or not sender_email:
                self._last_error = "Variáveis de ambiente de email não configuradas"
                logger.warning(self._last_error)
                
                # Configurar provedor mock como fallback
                mock_config = EmailConfig(
                    api_key="mock_key",
                    sender_email="noreply@vendacomconexao.com",
                    sender_name="Sistema Venda com Conexão",
                    provider=EmailProviderEnum.MOCK
                )
                mock_provider = MockEmailProvider(mock_config)
                self.providers[EmailProviderEnum.MOCK.value] = mock_provider
                self.default_provider = EmailProviderEnum.MOCK.value
                
            else:
                # Configurar provedor Brevo
                brevo_config = EmailConfig(
                    api_key=api_key,
                    sender_email=sender_email,
                    sender_name="Sistema Venda com Conexão",
                    provider=EmailProviderEnum.BREVO
                )
                brevo_provider = BrevoEmailProvider(brevo_config)
                self.providers[EmailProviderEnum.BREVO.value] = brevo_provider
                self.default_provider = EmailProviderEnum.BREVO.value
                
                logger.info("Serviço de email inicializado com provedor Brevo")
            
            self._initialized = True
            
        except Exception as e:
            self._last_error = f"Erro na inicialização do serviço de email: {str(e)}"
            logger.error(self._last_error)
    
    async def send_welcome_email(self, to_email: str, full_name: str, temporary_password: str):
        """Envia um e-mail de boas-vindas usando o template 'WELCOME_USER' do banco."""
        logger.info(f"Preparando e-mail de boas-vindas para {to_email}")
        
        context = {
            "full_name": full_name,
            "temporary_password": temporary_password,
            "to_email": to_email
        }
        
        return await self.send_email_from_template(
            template_chave='WELCOME_USER',
            recipient_email=to_email,
            recipient_name=full_name,
            context=context
        )

    async def send_test_email(self, recipient_email: str) -> EmailResponse:
        """
        Envia um e-mail de teste para um destinatário especificado e registra o resultado.
        """
        logger.info(f"Enviando e-mail de teste para {recipient_email}")

        context = {
            "test_date": datetime.now().strftime("%d/%m/%Y %H:%M:%S")
        }

        response = await self.send_email_from_template(
            template_chave='TEST_EMAIL',
            recipient_email=recipient_email,
            recipient_name="Usuário de Teste",
            context=context
        )
        
        self._last_test_result = 'sucesso' if response.success else 'falha'
        self._last_test_timestamp = datetime.now()
        
        return response

    async def send_email(self, request: EmailRequest) -> EmailResponse:
        """
        Envia um e-mail usando o provedor padrão. Esta é a função de envio de baixo nível.
        """
        if not self._initialized:
            self.initialize()
        
        if not self.providers:
            error_msg = "Nenhum provedor de email configurado ou inicializado."
            return EmailResponse(success=False, message=error_msg)

        provider_name = request.provider.value if request.provider else self.default_provider
        if provider_name not in self.providers:
            error_msg = f"Provedor {provider_name} não encontrado. Disponíveis: {list(self.providers.keys())}"
            return EmailResponse(success=False, message=error_msg)
        
        provider = self.providers[provider_name]
        
        if not provider.is_configured():
            error_msg = f"Provedor {provider_name} não está configurado corretamente"
            return EmailResponse(success=False, message=error_msg)
        
        return await provider.send_email(request.to, request.content)

    def _render_template(self, template_content: str, context: Dict[str, Any]) -> str:
        """Substitui variáveis no formato {{variavel}} pelo valor no contexto."""
        for key, value in context.items():
            template_content = re.sub(r'{{\s*' + re.escape(key) + r'\s*}}', str(value), template_content)
        return template_content

    async def send_email_from_template(self, template_chave: str, recipient_email: str, recipient_name: str, context: Dict[str, Any]) -> EmailResponse:
        """
        Busca um template no banco, renderiza com o contexto e envia o e-mail.
        """
        template = get_email_template_by_chave(template_chave)
        if not template:
            msg = f"Template de e-mail com chave '{template_chave}' não encontrado ou inativo."
            logger.error(msg)
            return EmailResponse(success=False, message=msg)

        # Renderiza o assunto e o conteúdo
        subject = self._render_template(template['assunto'], context)
        html_content = self._render_template(template['html_content'], context)
        text_content = self._render_template(template['text_content'], context) if template.get('text_content') else None

        email_request = EmailRequest(
            to=[EmailRecipient(email=recipient_email, name=recipient_name)],
            content=EmailContent(subject=subject, html_content=html_content, text_content=text_content)
        )
        return await self.send_email(email_request)

    async def send_email_old(self, request: EmailRequest) -> EmailResponse:
        """Envia email usando o provedor especificado ou o padrão"""
        if not self._initialized:
            self.initialize()
        
        if not self.providers:
            error_msg = "Nenhum provedor de email configurado ou inicializado."
            # Atualiza o status do último teste mesmo que não haja provedores
            self._last_test_result = 'falha'
            self._last_test_timestamp = datetime.now()
            return EmailResponse(success=False, message=error_msg)

        # Se o provedor padrão é mock e não há outro configurado,
        # o status geral deve refletir isso.
        if self.default_provider == EmailProviderEnum.MOCK.value and len(self.providers) == 1:
            logger.warning("Serviço de e-mail operando em modo MOCK. Nenhuma configuração real de e-mail encontrada.")
        
        provider_name = request.provider.value if request.provider else self.default_provider
        if provider_name not in self.providers:
            error_msg = f"Provedor {provider_name} não encontrado. Disponíveis: {list(self.providers.keys())}"
            return EmailResponse(success=False, message=error_msg)
        
        provider = self.providers[provider_name]
        
        if not provider.is_configured():
            error_msg = f"Provedor {provider_name} não está configurado corretamente"
            return EmailResponse(success=False, message=error_msg)
        
        response = await provider.send_email(request.to, request.content)
        return response
    
    def get_admin_status(self) -> Dict[str, Any]:
        """ 
        Retorna o status do serviço de email formatado para o painel de administração.
        """
        if not self._initialized:
            self.initialize()

        service_status: Literal['operacional', 'erro', 'desconhecido'] = 'desconhecido'
        if self.default_provider and self.default_provider != EmailProviderEnum.MOCK.value:
            service_status = 'operacional'
        elif self.default_provider == EmailProviderEnum.MOCK.value:
            service_status = 'operacional' # Mock é operacional para testes
        if self._last_error:
            service_status = 'erro'

        return {
            "service_status": service_status,
            "last_test_result": self._last_test_result,
            "last_test_timestamp": self._last_test_timestamp.isoformat() if self._last_test_timestamp else None,
            "provider_name": self.default_provider or "Nenhum"
        }
    
    def get_available_providers(self) -> List[str]:
        """Retorna lista de provedores disponíveis"""
        return list(self.providers.keys())

# Instância global do serviço de email
email_service = EmailService()