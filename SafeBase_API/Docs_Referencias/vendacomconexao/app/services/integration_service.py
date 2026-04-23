import logging
import uuid
from typing import Dict, Any
from fastapi import HTTPException, status 

from app.models import KiwifyWebhookPayload
from app.services.security import generate_temporary_password # Importa a função correta
from app.database.integrations_db import log_integration_event
from app.services.signup_service import signup_service
from app.models_planos import SignupRequest

logger = logging.getLogger(__name__)

class IntegrationService:

    async def process_payment_webhook(self, payload: Dict[str, Any]) -> Dict[str, Any]:
        """
        Processa um webhook de pagamento genérico. Por enquanto, focado em Kiwify.
        """
        log_id = log_integration_event("kiwify", payload.get("webhook_event_type", "unknown"), payload)

        try:
            # Valida o payload com o modelo Pydantic
            kiwify_data = KiwifyWebhookPayload.parse_obj(payload)
            event_type = kiwify_data.get_event_type()

            if event_type == "order_approved":
                return await self._handle_order_approved(kiwify_data)
            
            # Outros eventos podem ser tratados aqui no futuro
            elif event_type in ["order_refunded", "subscription_cancelled"]:
                logger.info(f"Evento '{event_type}' recebido para {kiwify_data.get_customer_email()}. Nenhuma ação configurada.")
                return {"status": "success", "message": f"Evento '{event_type}' recebido e logado."}
            
            else:
                logger.warning(f"Tipo de evento não suportado recebido: {event_type}")
                return {"status": "ignored", "message": f"Tipo de evento '{event_type}' não é processado."}

        except Exception as e:
            logger.error(f"Falha ao processar webhook de pagamento (Log ID: {log_id}): {e}", exc_info=True)
            raise HTTPException(
                status_code=status.HTTP_400_BAD_REQUEST,
                detail=f"Erro ao processar payload: {str(e)}"
            )

    async def _handle_order_approved(self, data: KiwifyWebhookPayload) -> Dict[str, Any]:
        """
        Mapeia os dados do webhook e chama o serviço de signup existente.
        """
        logger.info(f"Processando 'order_approved' para o cliente: {data.get_customer_email()}")

        # Lógica para tratar o documento recebido (CPF da Kiwify)
        documento = "".join(filter(str.isdigit, data.Customer.CPF or ""))
        tipo_pessoa = 'F' # Assumimos que o documento da Kiwify é sempre um CPF (Pessoa Física)
        if len(documento) != 11:
            documento = "00000000000" # Fallback para um CPF genérico se o recebido for inválido

        # Mapeamento dos dados do Kiwify para o SignupRequest
        signup_data = SignupRequest(
            razao_social=data.Customer.full_name,
            nome_fantasia=data.Customer.full_name,
            documento=documento,
            tipo_pessoa=tipo_pessoa,
            email_contato=data.Customer.email,
            telefone=data.Customer.mobile or "N/A",
            full_name=data.Customer.full_name,
            username=data.Customer.email.split('@')[0].replace('.', '_') + f"_{str(uuid.uuid4())[:4]}",
            password=generate_temporary_password() # CORRIGIDO: Gera uma senha forte e amigável
        )

        # Chama o serviço de signup existente, que contém toda a lógica de criação
        result = await signup_service.register_new_cliente_and_user(signup_data)
        logger.info(f"Cliente e usuário criados via integração: {result}")
        
        return {"status": "success", "message": "Cliente e usuário criados com sucesso.", "details": result}

integration_service = IntegrationService()