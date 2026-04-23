# app/services/webhook_service.py
import logging
import uuid
from typing import Dict, Any
from fastapi import HTTPException, status
from datetime import datetime, timedelta

from app.models_hooks import KiwifyWebhookPayload
from app.database.webhooks_db import get_gateway_product_mapping, log_webhook_event
from app.database import (
    get_user_by_email, get_plano_by_id, atribuir_plano_cliente, 
    get_plano_inicial_padrao, create_cliente, create_user,
    get_profile_by_name, assign_user_profile,get_cliente_by_email,
    update_cliente_status, get_user_by_cliente_id, update_user_status
)

from app.services.signup_service import signup_service
from app.models_planos import SignupRequest
from app.services.security import generate_temporary_password

logger = logging.getLogger(__name__)

class WebhookService:
    async def process_kiwify_event(self, payload: Dict[str, Any]) -> str:
        """
        Processa um evento de webhook da Kiwify.
        """
        event_id = None
        try:
            # 2. Valida o payload com Pydantic
            kiwify_data = KiwifyWebhookPayload.parse_obj(payload)
            event_type = kiwify_data.get_event_type()
            
            # 1. Loga o evento recebido no banco de dados para auditoria (APÓS validação)
            event_id = log_webhook_event("kiwify", event_type, payload)

            processing_log = f"Evento '{event_type}' recebido. "

            # 3. Roteia o evento para o handler apropriado
            if event_type == "order_approved":
                log_message = await self._handle_order_approved(kiwify_data)
                processing_log += log_message
            elif event_type == "order_refunded":
                log_message = await self._handle_order_refunded(kiwify_data)
                processing_log += log_message
            elif event_type == "subscription_canceled":
                log_message = await self._handle_subscription_canceled(kiwify_data)
                processing_log += log_message
            elif event_type == "subscription_late":
                log_message = await self._handle_subscription_late(kiwify_data)
                processing_log += log_message
            elif event_type == "subscription_renewed":
                log_message = await self._handle_subscription_renewed(kiwify_data)
                processing_log += log_message
            elif event_type == "subscription_activated":
                log_message = await self._handle_subscription_activated(kiwify_data)
                processing_log += log_message
            elif event_type == "chargeback":
                log_message = await self._handle_chargeback(kiwify_data)
                processing_log += log_message
            # Eventos informativos que não alteram o estado do cliente
            elif event_type == "billet_created":
                log_message = await self._handle_billet_created(kiwify_data)
                processing_log += log_message
            elif event_type == "pix_created":
                log_message = await self._handle_pix_created(kiwify_data)
                processing_log += log_message
            elif event_type == "order_rejected":
                log_message = await self._handle_order_rejected(kiwify_data)
                processing_log += log_message
            else:
                processing_log += "Tipo de evento não suportado para processamento."
                logger.info(f"Webhook da Kiwify com evento não suportado recebido: {event_type}")

            # 4. Atualiza o log do evento no banco com o resultado
            log_webhook_event(event_id, status="processed", processing_log=processing_log)
            return processing_log

        except Exception as e:
            error_message = f"Falha ao processar webhook da Kiwify: {e}"
            logger.error(error_message, exc_info=True)
            if event_id:
                # CORREÇÃO: Garante que a causa da falha seja registrada no log do banco.
                log_webhook_event(event_id, status="failed", processing_log=error_message)
            # Não relança a exceção para garantir que a rota sempre retorne 200 OK para a Kiwify.
            # A falha já está registrada no banco para análise e reprocessamento.
            return error_message

    async def _deactivate_client_access(self, cliente_id: str, reason: str) -> str:
        """
        Desativa o acesso de um cliente e de todos os seus usuários.
        """
        success = update_cliente_status(cliente_id, "inativo")
        if success:
            users = get_user_by_cliente_id(cliente_id)
            for user in users:
                update_user_status(user['id'], False)
            logger.info(f"Acesso revogado para cliente {cliente_id} devido a {reason}.")
            return f"Acesso revogado para cliente {cliente_id} devido a {reason}."
        else:
            logger.error(f"Falha ao revogar acesso para cliente {cliente_id} devido a {reason}.")
            return f"Falha ao revogar acesso para cliente {cliente_id} devido a {reason}."

    async def _handle_order_approved(self, data: KiwifyWebhookPayload) -> str:
        """
        Lógica para quando uma ordem de compra é aprovada. 
        """
        customer_email = data.get_customer_email()
        customer_name = data.get_customer_name()
        # Usa plano padrão
        plano_padrao = get_plano_inicial_padrao()
        if not plano_padrao:
            logger.error("Nenhum plano inicial padrão encontrado no sistema.")
            raise Exception("Nenhum plano padrão configurado no sistema.")

        plano_id = plano_padrao['id']
        plano_nome = plano_padrao['nome']

        # LÓGICA PRINCIPAL: Verifica primeiro se o CLIENTE existe pelo email
        cliente = get_cliente_by_email(customer_email)

        if cliente:
            # Cliente existente: Reativa o cliente e seus usuários, e atualiza o plano.
            cliente_id = cliente['id']
            update_cliente_status(cliente_id, "ativo", expiry_date=None) # Limpa a data de expiração
            
            # MELHORIA: Garante que todos os usuários associados sejam reativados
            associated_users = get_user_by_cliente_id(cliente_id)
            for user in associated_users:
                update_user_status(user['id'], True)
            
            logger.info(f"Reativando {len(associated_users)} usuário(s) para o cliente {cliente_id}.")
            
            atribuir_plano_cliente(cliente_id, plano_id)
            
            log_message = f"Cliente existente '{customer_email}' (ID: {cliente_id}) reativado com sucesso. Plano atualizado para '{plano_nome}'."
            logger.info(log_message)
            return log_message
        else:
            # Novo usuário: Cria cliente e usuário
            logger.info(f"Novo cliente via webhook Kiwify: {customer_email}. Criando com plano padrão.")

            # Extrai dados do payload de forma segura
            documento_raw = data.Customer.CPF if data.Customer else "0"
            documento = "".join(filter(str.isdigit, documento_raw or "0"))
            documento = documento if len(documento) == 11 else "00000000000"
            telefone = data.get_customer_phone()

            # Usa email REAL para username
            username_base = customer_email.split('@')[0].replace('.', '_') if customer_email != "sem-email@exemplo.com" else "cliente"
            username = f"{username_base}_{str(uuid.uuid4())[:4]}"

            # USA DADOS REAIS do Kiwify
            signup_data = SignupRequest(
                razao_social=customer_name,
                nome_fantasia=customer_name,
                documento=documento,
                tipo_pessoa='F',
                email_contato=customer_email,
                telefone=telefone,
                full_name=customer_name,
                username=username,
                password=generate_temporary_password(),
                plano_id=plano_id
            )

            # Log dos dados que serão salvos
            logger.info(f"Dados Kiwify que serão salvos: Email: {customer_email}, Nome: {customer_name}, CPF: {documento}, Telefone: {telefone}")

            # Chamada correta do signup_service
            result = await signup_service.register_new_cliente_and_user(signup_data)
            
            logger.info(f"Cliente criado via Kiwify - Cliente ID: {result['cliente_id']}, User ID: {result['user_id']}")
            return f"Novo cliente e usuário '{customer_email}' criados com sucesso com o plano padrão '{plano_nome}'."

    async def _handle_order_refunded(self, data: KiwifyWebhookPayload) -> str:
        """
        REEMBOLSO: Revogar acesso imediatamente
        """
        customer_email = data.get_customer_email()
        cliente = get_cliente_by_email(customer_email)
        
        if not cliente:
            return f"Cliente com email {customer_email} não encontrado para processar reembolso."
        
        cliente_id = cliente.get("id")
        if not cliente_id:
            # Este caso não deve ocorrer se o cliente foi encontrado, mas é uma salvaguarda.
            return f"Cliente encontrado para {customer_email}, mas sem ID válido."
        
        # Revoga acesso imediatamente usando a nova função auxiliar
        return await self._deactivate_client_access(cliente_id, "reembolso")

    async def _handle_subscription_canceled(self, data: KiwifyWebhookPayload) -> str:
        """
        ASSINATURA CANCELADA: Manter acesso até o fim do período
        """
        customer_email = data.get_customer_email()
        cliente = get_cliente_by_email(customer_email)
        
        if not cliente:
            return f"Cliente com email {customer_email} não encontrado para processar cancelamento."
        
        cliente_id = cliente.get("id")
        if not cliente_id:
            return f"Cliente encontrado para {customer_email}, mas sem ID válido."
        
        # LÓGICA CORRIGIDA: Usa o campo 'access_until' da Kiwify, que é mais preciso.
        if data.Subscription and data.Subscription.customer_access and data.Subscription.customer_access.access_until:
            try:
                access_until = data.Subscription.customer_access.access_until
                success = update_cliente_status(cliente_id, "cancelado", access_until)
                if success:
                    log_msg = f"Assinatura cancelada para cliente {cliente_id}. Acesso mantido até {access_until.strftime('%Y-%m-%d %H:%M:%S')}."
                    logger.info(log_msg)
                    # Desativa usuários apenas se a data de acesso já passou
                    if access_until < datetime.now(access_until.tzinfo):
                        await self._deactivate_client_access(cliente_id, "período de acesso expirado após cancelamento")
                    return log_msg
                else:
                    return f"Falha ao atualizar status de cancelamento para cliente {cliente_id}."
            except Exception as e:
                logger.error(f"Erro ao processar data de expiração: {e}")
                success = update_cliente_status(cliente_id, "cancelado")
                return f"Assinatura cancelada para cliente {cliente_id}. Data de expiração não definida."
        else:
            # Fallback caso 'access_until' não venha no payload
            success = update_cliente_status(cliente_id, "cancelado")
            return f"Assinatura cancelada para cliente {cliente_id}. Acesso mantido até fim do período."

    async def _handle_subscription_late(self, data: KiwifyWebhookPayload) -> str:
        """
        PAGAMENTO EM ATRASO: Modo restrito
        """
        customer_email = data.get_customer_email()
        cliente = get_cliente_by_email(customer_email)
        
        if not cliente:
            return f"Cliente com email {customer_email} não encontrado para marcar como atrasado."
        
        cliente_id = cliente.get("id")
        if not cliente_id:
            return f"Cliente com email {customer_email} encontrado, mas sem ID válido."
        
        success = update_cliente_status(cliente_id, "atrasado")
        
        if success:
            logger.info(f"Cliente {cliente_id} em modo restrito devido a pagamento em atraso.")
            return f"Cliente {cliente_id} em modo restrito devido a pagamento em atraso."
        else:
            logger.error(f"Falha ao definir cliente {cliente_id} como atrasado")
            return f"Falha ao definir cliente {cliente_id} como atrasado"

    async def _handle_subscription_renewed(self, data: KiwifyWebhookPayload) -> str:
        """
        ASSINATURA RENOVADA: Restaurar acesso completo
        """
        customer_email = data.get_customer_email()
        cliente = get_cliente_by_email(customer_email)
        
        if not cliente:
            return f"Cliente com email {customer_email} não encontrado para processar renovação."
        
        cliente_id = cliente.get("id")
        if not cliente_id:
            return f"Cliente com email {customer_email} encontrado, mas sem ID válido."
        
        # Restaura o acesso, limpando a data de expiração
        success = update_cliente_status(cliente_id, "ativo", expiry_date=None)
        
        if success:
            # Reativar usuários se necessário
            users = get_user_by_cliente_id(cliente_id)
            for user in users:
                update_user_status(user['id'], True)
            
            logger.info(f"Acesso restaurado para cliente {cliente_id} devido a renovação.")
            return f"Acesso restaurado para cliente {cliente_id} devido a renovação."
        else:
            logger.error(f"Falha ao restaurar acesso para cliente {cliente_id}")
            return f"Falha ao restaurar acesso para cliente {cliente_id}"

    async def _handle_subscription_activated(self, data: KiwifyWebhookPayload) -> str:
        """
        ASSINATURA ATIVADA: Similar ao order_approved, mas para assinaturas
        """
        customer_email = data.get_customer_email()
        cliente = get_cliente_by_email(customer_email)
        
        if not cliente:
            # Se não existe usuário, cria como order_approved
            return await self._handle_order_approved(data)
        else:
            cliente_id = cliente.get("id")
            if cliente_id:
                success = update_cliente_status(cliente_id, "ativo", expiry_date=None)
                if success:
                    return f"Assinatura ativada para cliente existente {cliente_id}."
            
            return f"Assinatura ativada para usuário existente {customer_email}."

    async def _handle_billet_created(self, data: KiwifyWebhookPayload) -> str:
        """
        BOLETO GERADO: Apenas logar - aguardar pagamento
        """
        customer_email = data.get_customer_email()
        order_id = data.order_id
        
        logger.info(f"Boleto gerado para pedido {order_id}, cliente {customer_email}. Aguardando pagamento.")
        return f"Boleto gerado para pedido {order_id}. Aguardando pagamento."

    async def _handle_pix_created(self, data: KiwifyWebhookPayload) -> str:
        """
        PIX GERADO: Apenas logar - aguardar pagamento
        """
        customer_email = data.get_customer_email()
        order_id = data.order_id
        
        logger.info(f"PIX gerado para pedido {order_id}, cliente {customer_email}. Aguardando pagamento.")
        return f"PIX gerado para pedido {order_id}. Aguardando pagamento."

    async def _handle_order_rejected(self, data: KiwifyWebhookPayload) -> str:
        """
        COMPRA RECUSADA: Logar motivo da recusa
        """
        customer_email = data.get_customer_email()
        order_id = data.order_id
        rejection_reason = data.card_rejection_reason or "Motivo não especificado"
        
        logger.warning(f"Compra recusada para pedido {order_id}, cliente {customer_email}. Motivo: {rejection_reason}")
        return f"Compra recusada para pedido {order_id}. Motivo: {rejection_reason}"

    async def _handle_chargeback(self, data: KiwifyWebhookPayload) -> str:
        """
        CHARGEBACK: Similar ao reembolso - revogar acesso
        """
        customer_email = data.get_customer_email()
        cliente = get_cliente_by_email(customer_email)
        
        if not cliente:
            return f"Cliente com email {customer_email} não encontrado para processar chargeback."
        
        cliente_id = cliente.get("id")
        if not cliente_id:
            return f"Cliente com email {customer_email} encontrado, mas sem ID válido."
        
        # Revoga acesso imediatamente usando a nova função auxiliar
        return await self._deactivate_client_access(cliente_id, "chargeback")

# Instância global do serviço
webhook_service = WebhookService()