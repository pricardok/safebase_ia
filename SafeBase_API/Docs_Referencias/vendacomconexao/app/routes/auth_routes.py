from fastapi import APIRouter, Request, HTTPException, status
from datetime import timedelta
from fastapi.responses import JSONResponse
import secrets # Importa a biblioteca para geração de valores aleatórios seguros
from pydantic import BaseModel # Adicionado: Importação de BaseModel
import logging, os
import traceback

from app.models import UserLogin, TokenResponse, UserRegister, UserResponse, UserLoginResponse
from app.models_planos import UserWithTenantAndPlano, PlanoEfetivoResponse, SignupRequest
from app.database import (
    get_user_by_username, create_user, get_user_profile, get_profile_permissions, 
    update_user, get_user_by_email, get_user_by_login_identifier, get_user_details_by_id,
    get_cliente_by_id, get_plano_do_cliente
)
from app.auth_jwt import create_access_token, verify_password, get_password_hash, ACCESS_TOKEN_EXPIRE_MINUTES
from app.services.signup_service import signup_service

logger = logging.getLogger(__name__)
router = APIRouter(
    prefix="/auth",
    tags=["Autenticação"]
)

@router.post("/login", response_model=UserLoginResponse, include_in_schema=False)
async def login(user_data: UserLogin):
    try:
        user = get_user_by_login_identifier(user_data.login_identifier)
        if not user or not verify_password(user_data.password, user["hashed_password"]):
            raise HTTPException(
                status_code=status.HTTP_401_UNAUTHORIZED,
                detail="Usuário ou senha incorretos",
                headers={"WWW-Authenticate": "Bearer"},
            )

        # Verifica se o usuário precisa trocar a senha
        if user.get("must_change_password"):
            logger.info(f"Usuário '{user['username']}' precisa alterar a senha no primeiro login.")
            # Gera um token temporário, válido apenas para a troca de senha
            temp_token_expires = timedelta(minutes=15)
            temp_token = create_access_token(
                data={"sub": user["username"], "scope": "force_password_change"}, expires_delta=temp_token_expires
            )
            # include whatsapp IA flag in the temporary response as well
            whatsapp_ia_tmp = False
            try:
                if user.get('cliente_id'):
                    cliente_tmp = get_cliente_by_id(user.get('cliente_id'))
                    whatsapp_ia_tmp = bool(cliente_tmp.get('whatsapp_webhooks_enabled')) if cliente_tmp else False
            except Exception:
                logger.exception('Erro ao buscar cliente para verificar whatsapp_webhooks_enabled (temp)')

            return JSONResponse(status_code=210, content={"status": "password_change_required", "temp_token": temp_token, "whatsapp_ia": whatsapp_ia_tmp})

        if not user.get("is_active", True):
            raise HTTPException(status_code=400, detail="Sua conta está desativada.")

        # GERAÇÃO DO SAL DE SESSÃO: Invalida tokens antigos
        session_salt = secrets.token_hex(16)
        update_user(user_id=user["id"], session_salt=session_salt)

        access_token_expires = timedelta(minutes=ACCESS_TOKEN_EXPIRE_MINUTES)
        access_token = create_access_token(
            # Inclui o sal no token
            data={"sub": user["username"], "salt": session_salt}, expires_delta=access_token_expires
        )

        # Usa a nova função para obter todos os detalhes, incluindo plano e cliente
        user_details = get_user_details_by_id(user["id"])

        # Determine whatsapp_webhooks_enabled from the client's record (if any)
        whatsapp_enabled = False
        cliente_id_for_user = user_details.get('cliente_id') if user_details else user.get('cliente_id')
        try:
            if cliente_id_for_user:
                cliente = get_cliente_by_id(cliente_id_for_user)
                whatsapp_enabled = bool(cliente.get('whatsapp_webhooks_enabled')) if cliente else False
        except Exception:
            logger.exception('Erro ao buscar cliente para verificar whatsapp_webhooks_enabled')

        # add hybrid flag into returned user details (backwards compatible)
        if isinstance(user_details, dict):
            # expose a single boolean 'whatsapp_ia' for frontend (true/false)
            user_details['whatsapp_ia'] = bool(whatsapp_enabled)
            # also duplicate inside nested 'cliente' object when present for frontend compatibility
            try:
                if isinstance(user_details.get('cliente'), dict):
                    user_details['cliente']['whatsapp_ia'] = bool(whatsapp_enabled)
            except Exception:
                logger.exception('Erro ao duplicar whatsapp_ia dentro de user["cliente"]')

        return {
            "access_token": access_token,
            "token_type": "bearer",
            "user": user_details
        }
    except HTTPException:
        raise
    except Exception as e:
        logger.error(f"Erro interno durante login para {user_data.login_identifier}: {str(e)}")
        raise HTTPException(status_code=500, detail="Erro interno do servidor durante o login.")

@router.get("/me", response_model=UserWithTenantAndPlano, include_in_schema=False)
async def get_current_user(request: Request):
    if not hasattr(request.state, 'user'):
        raise HTTPException(status_code=status.HTTP_401_UNAUTHORIZED, detail="Não autenticado")

    # CORRIGIDO: Acessar o username a partir do dicionário 'user' no estado da requisição
    username = request.state.user.get("sub")
    if not username:
        raise HTTPException(status_code=status.HTTP_401_UNAUTHORIZED, detail="Token inválido, 'sub' não encontrado.")
    user = get_user_by_username(username)
    if not user:
        raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail="Usuário não encontrado")

    profile = get_user_profile(user["id"])
    permissions = get_profile_permissions(profile["id"]) if profile else {}
    auth_method = getattr(request.state, 'auth_method', 'unknown')
    cliente_id = getattr(request.state, 'cliente_id', None)

    accessible_modules = [mod for mod, perm in permissions.items() if perm.get("pode_acessar", False)]

    plano_atual = None
    tenant_status = None
    trial_expires_at = None
    cliente_razao_social = None
    cliente_nome_fantasia = None

    if cliente_id:
        # Busca dados do cliente e do plano
        cliente_data = get_cliente_by_id(cliente_id)
        plano_atual_data = get_plano_do_cliente(cliente_id)
        
        if cliente_data:
            cliente_razao_social = cliente_data.get("razao_social")
            cliente_nome_fantasia = cliente_data.get("nome_fantasia")

        if plano_atual_data:
            plano_atual = PlanoEfetivoResponse(**plano_atual_data)

    # Hybrid flag: whether whatsapp webhooks are enabled for this tenant (conservative default = False)
    whatsapp_webhooks_enabled = False
    cliente_obj = None
    whatsapp_modules = {}
    try:
        if cliente_id and cliente_data:
            whatsapp_webhooks_enabled = bool(cliente_data.get('whatsapp_webhooks_enabled'))
            # build a cliente object to include in response (keeps compatibility with login.user.cliente)
            cliente_obj = {
                "id": cliente_id,
                "razao_social": cliente_data.get('razao_social'),
                "nome_fantasia": cliente_data.get('nome_fantasia'),
                "whatsapp_ia": bool(whatsapp_webhooks_enabled)
            }
    except Exception:
        logger.exception('Erro ao verificar whatsapp_webhooks_enabled no /auth/me')


    return {
        "id": user["id"],
        "username": user["username"],
        "email": user["email"],
        "full_name": user["full_name"],
        "telefone": user.get("telefone"),
        "is_active": user["is_active"],
        "cliente_id": cliente_id,
        "cliente_razao_social": cliente_razao_social,
        "cliente_nome_fantasia": cliente_nome_fantasia,
        "cliente": cliente_obj,
        "perfil": profile["nome"] if profile else "sem_perfil",
        "permissoes": accessible_modules,
        "plano_atual": plano_atual,
        "tenant_status": tenant_status,
        "trial_expires_at": trial_expires_at,
        "auth_method": auth_method,
        "is_super_access": getattr(request.state, 'is_super_access', False),
        "whatsapp_ia": bool(whatsapp_webhooks_enabled)
    }

@router.post("/signup", response_model=dict, status_code=status.HTTP_201_CREATED)
async def signup_new_cliente(signup_data: SignupRequest):
    try:
        result = await signup_service.register_new_cliente_and_user(signup_data)
        return {"message": "Cliente e usuário criados com sucesso!", "data": result}
    except HTTPException:
        raise
    except Exception as e:
        logger.error(f"Erro na rota de signup: {e}")
        raise HTTPException(status_code=500, detail="Erro ao processar o cadastro.")

@router.post("/register", response_model=UserResponse, include_in_schema=False)
async def register(user_data: UserRegister):
    if get_user_by_username(user_data.username):
        raise HTTPException(status_code=status.HTTP_400_BAD_REQUEST, detail="Usuário já existe")
    
    try:
        user_id = create_user(
            username=user_data.username,
            email=user_data.email,
            password=user_data.password,
            full_name=user_data.full_name
        )
        user = get_user_by_username(user_data.username)
        return user
    except Exception as e:
        logger.error(f"Erro ao criar usuário: {e}")
        raise HTTPException(status_code=status.HTTP_500_INTERNAL_SERVER_ERROR, detail="Erro ao criar usuário")

@router.put("/profile", response_model=dict, include_in_schema=False)
async def update_user_profile(update_data: dict, request: Request):
    try:
        if not hasattr(request.state, 'user'):
            raise HTTPException(status_code=status.HTTP_401_UNAUTHORIZED, detail="Não autenticado")
        
        # CORRIGIDO: Acessar o username a partir do dicionário 'user' no estado da requisição
        username = request.state.user.get("sub")
        if not username:
            raise HTTPException(status_code=status.HTTP_401_UNAUTHORIZED, detail="Token inválido, 'sub' não encontrado.")
        current_user = get_user_by_username(username)
        if not current_user:
            raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail="Usuário não encontrado")
        
        user_id = current_user["id"]
        
        allowed_fields = ["username", "email", "full_name", "password", "current_password", "telefone"]
        update_fields = {field: value for field, value in update_data.items() if field in allowed_fields and value is not None}
        
        if not update_fields:
            raise HTTPException(status_code=400, detail="Nenhum campo válido para atualização")
        
        if "password" in update_fields:
            if "current_password" not in update_fields:
                raise HTTPException(status_code=400, detail="Para alterar a senha, é necessário informar a senha atual")
            
            if not verify_password(update_fields["current_password"], current_user["hashed_password"]):
                raise HTTPException(status_code=400, detail="Senha atual incorreta.")
            
            del update_fields["current_password"]
            update_fields["hashed_password"] = get_password_hash(update_fields["password"])
            del update_fields["password"]
        
        if "username" in update_fields:
            existing_user = get_user_by_username(update_fields["username"])
            if existing_user and existing_user["id"] != user_id:
                raise HTTPException(status_code=400, detail="Username já está em uso")
        
        updated = update_user(user_id, **update_fields)

        if updated:
            updated_user = get_user_by_username(update_fields.get("username", current_user["username"]))
            return {
                "message": "Perfil atualizado com sucesso",
                "user": {
                    "id": updated_user["id"],
                    "username": updated_user["username"],
                    "email": updated_user["email"],
                    "full_name": updated_user["full_name"],
                    "telefone": updated_user.get("telefone"),
                    "is_active": updated_user["is_active"]
                }
            }
        else:
            raise HTTPException(status_code=500, detail="Erro ao atualizar perfil no banco de dados")
        
    except HTTPException:
        raise
    except Exception as e:
        logger.error(f"Erro ao atualizar perfil: {e}")
        logger.error(f"Stack trace: {traceback.format_exc()}")
        raise HTTPException(status_code=500, detail="Erro interno ao atualizar perfil")

@router.post("/debug-hash", include_in_schema=False)
async def debug_hash(request: dict):
    password = request.get("password", "teste123")
    try:
        hashed = get_password_hash(password)
        verified = verify_password(password, hashed)
        return {"password": password, "hashed": hashed, "verified": verified, "hash_length": len(hashed)}
    except Exception as e:
        return {"error": str(e)}

class ForcePasswordChangeRequest(BaseModel):
    new_password: str

@router.post("/force-password-change", status_code=status.HTTP_200_OK, include_in_schema=False)
async def force_password_change(
    request_data: ForcePasswordChangeRequest,
    request: Request):
    """
    Endpoint para forçar a troca de senha no primeiro login.
    Requer um token temporário com o escopo 'force_password_change'.
    """
    if not hasattr(request.state, 'user') or request.state.user.get("scope") != "force_password_change":
        raise HTTPException(status_code=status.HTTP_403_FORBIDDEN, detail="Token inválido para esta operação.")

    # CORRIGIDO: Acessar o username a partir do dicionário 'user' no estado da requisição
    username = request.state.user.get("sub")
    if not username:
        raise HTTPException(status_code=status.HTTP_401_UNAUTHORIZED, detail="Token inválido, 'sub' não encontrado.")
    user = get_user_by_username(username)
    if not user:
        raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail="Usuário não encontrado.")

    try:
        # Atualiza a senha e remove a flag de troca obrigatória
        update_user(
            user_id=user["id"],
            hashed_password=get_password_hash(request_data.new_password),
            must_change_password=False
        )
        logger.info(f"Usuário '{user['username']}' alterou a senha com sucesso após o primeiro login.")
        return {"message": "Senha alterada com sucesso. Por favor, faça login novamente."}
    except Exception as e:
        logger.error(f"Erro ao forçar a troca de senha para o usuário {user['username']}: {e}")
        raise HTTPException(status_code=500, detail="Erro interno ao alterar a senha.")

