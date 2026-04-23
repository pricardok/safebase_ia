# app/services/signup_service.py
import logging
from fastapi import HTTPException, status 
from pydantic import BaseModel 
from typing import Optional
from app.database import (
    create_user, get_user_by_username, get_user_by_email, assign_user_profile, get_profile_by_name,
    create_cliente, get_plano_inicial_padrao
)
from app.models_planos import SignupRequest
 
logger = logging.getLogger(__name__)

class SignupService:
    async def register_new_cliente_and_user(self, signup_data: SignupRequest, plano_id_override: Optional[int] = None):
        """
        Orquestra o processo de signup:
        1. Valida dados.
        2. Encontra o plano de trial.
        3. Cria o cliente (tenant).
        4. Cria o usuário administrador.
        4. Cria o usuário administrador para o cliente.
        """
        # 1. Validação
        if get_user_by_username(signup_data.username):
            raise HTTPException(
                status_code=status.HTTP_400_BAD_REQUEST,
                detail="Nome de usuário já está em uso."
            )
        if get_user_by_email(signup_data.email_contato):
            raise HTTPException(
                status_code=status.HTTP_400_BAD_REQUEST,
                detail="O e-mail fornecido já está cadastrado."
            )

        # 2. Determina o plano a ser usado
        plano_id_final = plano_id_override
        if not plano_id_final:
            plano_padrao = get_plano_inicial_padrao()
            if not plano_padrao:
                logger.error("Nenhum plano inicial padrão (pago e ativo) foi encontrado no banco de dados.")
                raise HTTPException(
                    status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
                    detail="Não foi possível iniciar o processo de cadastro. Nenhum plano padrão configurado."
                )
            plano_id_final = plano_padrao['id']

        try:
            # 3. Criar o cliente
            cliente_id = create_cliente(
                razao_social=signup_data.razao_social,
                nome_fantasia=signup_data.nome_fantasia,
                documento=signup_data.documento,
                tipo_pessoa=signup_data.tipo_pessoa,
                email_contato=signup_data.email_contato,
                telefone=signup_data.telefone,
                plano_id=plano_id_final
            )
            logger.info(f"Cliente criado com ID: {cliente_id}.")

            # 4. Criar o usuário associado ao cliente
            user_id = create_user(
                username=signup_data.username,
                email=signup_data.email_contato, # Usando o email de contato como email do usuário admin
                password=signup_data.password,
                full_name=signup_data.full_name,
                cliente_id=cliente_id,
                must_change_password=True # Marca que a senha precisa ser trocada
            )
            logger.info(f"Usuário criado com ID: {user_id}.")

            # 5. Atribuir perfil padrão 'vendedor' ao novo usuário
            vendedor_profile = get_profile_by_name('vendedor')
            if vendedor_profile:
                assign_user_profile(user_id, vendedor_profile['id'])
                logger.info(f"Perfil 'vendedor' atribuído com sucesso ao usuário ID {user_id}.")
            else:
                logger.warning(f"Perfil 'vendedor' não encontrado. O usuário ID {user_id} foi criado sem perfil.")
                # Opcional: Lançar uma exceção se o perfil for obrigatório
                # raise HTTPException(status_code=500, detail="Configuração de perfil padrão 'vendedor' não encontrada.")

            # 6. Enviar email de boas-vindas com a senha temporária
            try:
                # Importação local para quebrar o ciclo de dependência na inicialização.
                # Isso garante que o email_service seja carregado apenas quando necessário.
                from app.services.email_service import email_service
                
                await email_service.send_welcome_email(
                    to_email=signup_data.email_contato,
                    full_name=signup_data.full_name,
                    temporary_password=signup_data.password # A senha original é a temporária
                )
            except Exception as email_error:
                logger.error(f"Falha ao enviar email de boas-vindas para {signup_data.email_contato}: {email_error}")

            return {"cliente_id": cliente_id, "user_id": user_id}

        except Exception as e:
            logger.error(f"Falha crítica no processo de signup para {signup_data.razao_social}: {e}")
            raise HTTPException(status_code=status.HTTP_500_INTERNAL_SERVER_ERROR, detail="Ocorreu um erro inesperado durante o cadastro.")

signup_service = SignupService()