# backend/app/routes/email_routes.py
from fastapi import APIRouter, HTTPException, Request, Depends, Header
import logging
from app.models import (
    EmailRequest, EmailResponse, EmailRecipient, EmailContent, PasswordResetRequest, PasswordResetResponse
)
from app.services.email_service import email_service
from app.services.security import generate_temporary_password, get_password_hash
from app.database import get_user_by_email, update_user_password
from app.auth_email import validate_email_api_key  # Nova importação

logger = logging.getLogger(__name__)
 
# Criar router para rotas de email
email_router = APIRouter(prefix="", tags=["Email"])

@email_router.post("/auth/reset-password", response_model=PasswordResetResponse)
async def reset_password(
    request: PasswordResetRequest, 
    api_key: str = Depends(validate_email_api_key)):
    """
    Solicita reset de senha para um email - Protegido por API Key específica
    """
    try:
        # Buscar usuário por email
        user = get_user_by_email(request.email)
        if not user:
            # Por segurança, sempre retornar sucesso mesmo se email não existir
            logger.info(f"Tentativa de reset de senha para email não cadastrado: {request.email}")
            return PasswordResetResponse(
                success=True,
                message="Se o email existir em nosso sistema, você receberá instruções para resetar sua senha."
            )
        
        # Gerar senha temporária
        temp_password = generate_temporary_password()
        
        # Hash da nova senha
        hashed_password = get_password_hash(temp_password)
        
        # Atualizar senha no banco
        update_user_password(user["id"], hashed_password)
        
        # Preparar contexto para o template
        context = {
            "full_name": user.get('full_name', user['username']),
            "username": user['username'],
            "temp_password": temp_password
        }

        # Enviar email usando o template do banco
        email_response = await email_service.send_email_from_template(
            template_chave='PASSWORD_RESET',
            recipient_email=request.email,
            recipient_name=user.get('full_name', user['username']),
            context=context
        )
        
        if email_response.success:
            logger.info(f"Email de reset de senha enviado para {request.email} via {email_response.provider_used}")
            
            # Em desenvolvimento, podemos retornar a senha temporária
            import os
            if os.getenv("ENVIRONMENT") == "development":
                return PasswordResetResponse(
                    success=True,
                    message="Senha temporária gerada e enviada por email",
                    temporary_password=temp_password
                )
            
            return PasswordResetResponse(
                success=True,
                message="Senha temporária gerada e enviada por email"
            )
        else:
            logger.error(f"Falha ao enviar email para {request.email}: {email_response.message}")
            # Reverter a alteração da senha se o email falhou
            update_user_password(user["id"], user["hashed_password"])
            raise HTTPException(
                status_code=500,
                detail="Erro ao enviar email de redefinição de senha"
            )
            
    except HTTPException:
        raise
    except Exception as e:
        logger.error(f"Erro no reset de senha: {str(e)}")
        raise HTTPException(
            status_code=500,
            detail="Erro interno no processo de redefinição de senha"
        )

@email_router.get("/email/status")
async def get_email_status(api_key: str = Depends(validate_email_api_key)):
    """Retorna o status do serviço de email - Protegido por API Key específica"""
    return email_service.get_status()

@email_router.post("/email/test", response_model=EmailResponse)
async def test_email_service(
    email_request: EmailRequest,
    api_key: str = Depends(validate_email_api_key)):
    """Endpoint para testar o serviço de email - Protegido por API Key específica"""
    return await email_service.send_email(email_request)

@email_router.post("/email/send_email", response_model=EmailResponse, include_in_schema=False)
async def send_email(
    email_request: EmailRequest,
    api_key: str = Depends(validate_email_api_key)):
    """Endpoint genérico para enviar um email."""
    try:
        return await email_service.send_email(email_request)
    except Exception as e:
        logger.error(f"Erro ao enviar email via endpoint /email/send_email: {e}")
        raise HTTPException(status_code=500, detail="Erro interno ao enviar email.")
