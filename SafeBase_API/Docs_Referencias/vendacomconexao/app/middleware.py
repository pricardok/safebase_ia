# backend/app/middleware.py
from fastapi import HTTPException, Request
from starlette.status import HTTP_403_FORBIDDEN, HTTP_401_UNAUTHORIZED, HTTP_429_TOO_MANY_REQUESTS
from starlette.responses import JSONResponse
import os
import logging
import time, uuid
import hashlib, json # Import json for request.json() handling
from .auth_jwt import verify_token
from .database import get_user_by_username # log_api_request will be removed
from .services.rbac_service import rbac_service, mask_api_key
from .config import API_LOGGING_ENABLED

logger = logging.getLogger(__name__)

API_KEY = os.getenv("API_KEY")

# Rate limiting
_request_counts = {}

class RateLimiter:
    def __init__(self, max_requests: int = 100, window_seconds: int = 60):
        self.max_requests = max_requests
        self.window_seconds = window_seconds
    
    def is_rate_limited(self, identifier: str) -> bool:
        now = time.time()
        window_start = now - self.window_seconds
        
        # Limpa requisições antigas
        self._clean_old_requests(now)
        
        # Conta requisições nesta janela
        window_requests = [
            req_time for req_time in _request_counts.get(identifier, [])
            if req_time > window_start
        ]
        
        _request_counts[identifier] = window_requests
        
        if len(window_requests) >= self.max_requests:
            return True
        
        window_requests.append(now)
        return False
    
    def _clean_old_requests(self, current_time: float):
        """Remove requisições antigas do cache global"""
        global _request_counts
        window_start = current_time - self.window_seconds
        
        for identifier in list(_request_counts.keys()):
            _request_counts[identifier] = [
                req_time for req_time in _request_counts[identifier]
                if req_time > window_start
            ]
            
            # Remove identificadores vazios
            if not _request_counts[identifier]:
                del _request_counts[identifier]

# Configurações de rate limiting por tipo de autenticação
rate_limiters = {
    "jwt": RateLimiter(max_requests=200, window_seconds=60),    # Usuários humanos
    "api_key": RateLimiter(max_requests=1000, window_seconds=60), # Integrações
    "super_access": RateLimiter(max_requests=5000, window_seconds=60) # Super acesso
}

async def auth_middleware(request: Request, call_next):
    start_time = time.time()
    response = None
    status_code = 500  # Padrão em caso de erro não tratado
    user_id = None

    error_details = {}

    try:
        response = await _process_request(request, call_next)
        status_code = response.status_code
    except HTTPException as e:
        status_code = e.status_code
        error_details['error_type'] = 'HTTPException'
        error_details['error_detail'] = e.detail
        # Em vez de apenas relançar, criamos uma resposta de erro para que o fluxo continue
        response = JSONResponse(status_code=e.status_code, content={"detail": e.detail})
    except Exception as e:
        # Captura exceções não tratadas (que viram erros 500)
        status_code = 500
        error_details['error_type'] = type(e).__name__
        error_details['error_detail'] = str(e)
        # Adiciona um traceback simplificado para depuração
        import traceback
        error_details['traceback'] = traceback.format_exc().splitlines()[-3:]
        response = JSONResponse(status_code=500, content={"detail": "Erro interno do servidor."})

    finally:
        # Bloco de logging que sempre executa
        if API_LOGGING_ENABLED:
            # Inicializa os dados de log com valores padrão seguros
            user_id = None
            username = None
            cliente_id = None
            razao_social = None
            plano = None
            auth_type = getattr(request.state, 'auth_method', None)

            # Tenta obter os dados do usuário de forma segura
            user_obj = getattr(request.state, 'current_user_obj', None)
            # CORREÇÃO: Garante que user_obj não é None antes de tentar acessá-lo.
            if user_obj and isinstance(user_obj, dict):
                user_id = user_obj.get('id')
                username = user_obj.get('username')
                cliente_id = user_obj.get('cliente_id')

                # OTIMIZAÇÃO: Evita novas chamadas ao banco no middleware.
                # Usa os dados que já foram carregados no objeto 'user' durante a autenticação.
                cliente_info = user_obj.get('cliente')
                if cliente_info and isinstance(cliente_info, dict):
                    razao_social = cliente_info.get('razao_social')

                plano_info = user_obj.get('plano_atual')
                if plano_info and isinstance(plano_info, dict):
                    plano = plano_info.get('nome')
                elif user_obj.get('plano_nome'): # Fallback para o nome do plano direto no user_obj
                    plano = user_obj.get('plano_nome')

            # Preenche o campo 'details' apenas se houver um erro
            details_to_log = None
            if status_code >= 400:
                details_to_log = error_details.copy() # Usar uma cópia para evitar modificar o original
                details_to_log['query_params'] = dict(request.query_params)
                details_to_log['request_body'] = "Corpo da requisição não é logado para evitar bloqueio."

            # Usar o logger padrão, que será capturado pelo DatabaseLogHandler assíncrono
            log_data = {
                "path": request.url.path,
                "method": request.method,
                "status_code": status_code,
                "user_id": user_id,
                "username": username,
                "cliente_id": cliente_id,
                "razao_social": razao_social,
                "auth_type": auth_type,
                "plano": plano,
                "details": details_to_log,
                "ip_address": request.client.host if request.client else "unknown",
            }
            log_level = logging.INFO if status_code < 400 else logging.ERROR
            logger.log(log_level, "API Request Log", extra=log_data)

    return response

async def _process_request(request: Request, call_next):
    # Permitir requisições OPTIONS (CORS preflight) SEM autenticação
    if request.method == "OPTIONS":
        logger.info(f"Permitindo requisição OPTIONS (CORS): {request.url.path}")
        return await call_next(request)
    
    # Rotas públicas que não precisam de autenticação
    public_routes = [
        "/", "/health", "/docs", "/openapi.json", "/redoc",
        "/auth/login", "/auth/signup", "/planos"
    ]
    
    # Rotas que aceitam apenas API Key (Swagger)
    api_key_only_routes = ["/docs", "/openapi.json", "/redoc"]
    
    # Rotas de email que usam API Key específica
    email_routes = [
        "/auth/reset-password", "/email/status", "/email/test"
    ]
    
    # Rotas de webhook que usam API Key específica
    webhook_routes = [
        "/hooks/gateway-events",
        # Webhook público do Waha (WhatsApp)
        "/integracoes/whatsapp/webhook",
        "/integracoes/whatsapp/webhook/ws"  # WebSocket para webhooks em tempo real
    ]
    
    # Rotas com autenticação própria e independente
    independent_auth_routes = [
        "/integracoes/pagamentos",
        "/auth/force-password-change" 
        "/integracoes/pagamentos"
    ]    
    
    # Verifica se é uma rota pública
    if request.url.path in public_routes:
        return await call_next(request)

    # CORREÇÃO: Webhooks e rotas independentes são checados PRIMEIRO.
    # Isso delega a autenticação para a própria rota, sem bloqueio do middleware.
    if request.url.path in independent_auth_routes or request.url.path in webhook_routes:
        logger.info(f"Requisição para rota com autenticação delegada: {request.url.path}. Prosseguindo...")
        return await call_next(request)

    # Aplica rate limiting baseado no IP para rotas não públicas (exceto webhooks)
    client_ip = request.client.host if request.client else "unknown"
    ip_limiter = RateLimiter(max_requests=50, window_seconds=60)
    
    if ip_limiter.is_rate_limited(f"ip:{client_ip}"):
        logger.warning(f"Rate limit excedido para IP: {client_ip}")
        return JSONResponse(
            status_code=HTTP_429_TOO_MANY_REQUESTS,
            content={"detail": "Muitas requisições. Tente novamente em alguns instantes."}
        )
    
    
    # Se é uma rota que só aceita API Key (como Swagger)
    if request.url.path in api_key_only_routes:
        if not api_key or api_key != API_KEY:
            logger.warning(f"Acesso negado ao Swagger sem API Key válida - IP: {client_ip}")
            raise HTTPException(
                status_code=HTTP_403_FORBIDDEN,
                detail="Acesso ao Swagger requer API Key válida"
            )
        
        # Rate limiting para Swagger
        if rate_limiters["api_key"].is_rate_limited(f"swagger:{client_ip}"):
            return JSONResponse(
                status_code=HTTP_429_TOO_MANY_REQUESTS,
                content={"detail": "Rate limit excedido para Swagger"}
            )
            
        return await call_next(request)
    
    authorization = request.headers.get("Authorization")
    # Aceita chave via header ou via query params (ex.: ?x-api-key=...)
    api_key = request.headers.get("X-API-Key") or request.query_params.get("x-api-key") or request.query_params.get("api_key") or request.query_params.get("apiKey")

    # Rotas de email - API Key específica (NOVA FUNCIONALIDADE)
    if request.url.path in email_routes:
        from app.auth_email import validate_email_api_key
        try:
            # Valida API Key específica para email (SEM AWAIT - função síncrona)
            validate_email_api_key(api_key)
            
            # Rate limiting específico para rotas de email
            email_identifier = f"email:{client_ip}"
            if rate_limiters["api_key"].is_rate_limited(email_identifier):
                logger.warning(f"Rate limit excedido para rotas de email: {client_ip}")
                return JSONResponse(
                    status_code=HTTP_429_TOO_MANY_REQUESTS,
                    content={"detail": "Rate limit excedido para operações de email"}
                )
            
            # Configurar request state para rotas de email
            request.state.auth_method = "email_api_key"
            request.state.is_super_access = False
            request.state.permissions = {"email": {"pode_acessar": True}}
            
            logger.info(f"Acesso autorizado para rota de email: {request.url.path} - IP: {client_ip}")
            return await call_next(request)
            
        except HTTPException as e:
            # Re-lançar a exceção de autenticação
            logger.warning(f"Autenticação de email falhou: {e.detail} - IP: {client_ip}")
            raise
        except Exception as email_auth_error:
            logger.error(f"Erro na autenticação de email: {email_auth_error}")
            return JSONResponse(
                status_code=HTTP_401_UNAUTHORIZED,
                content={"detail": "Falha na autenticação para operações de email"}
            )
    
    # Autenticação JWT (Aplicação Web) - FUNCIONALIDADE ORIGINAL (CORRIGIDA)
    if authorization and authorization.startswith("Bearer "):
        token = authorization.replace("Bearer ", "")
        
        try:
            token_data = verify_token(token)
            
            # CORREÇÃO: Verificar se token_data é um dict e extrair username
            if token_data:
                # CORRIGIDO: Agora verify_token sempre retorna um dicionário (payload).
                # Acessamos o username através da chave "sub".
                username = token_data.get("sub")

                if not username:
                    logger.warning(f"Token JWT sem username: {request.url.path}")
                    return JSONResponse(
                        status_code=HTTP_401_UNAUTHORIZED,
                        content={"detail": "Token inválido"}
                    )
                
                user = get_user_by_username(username)
                if not user:
                    logger.warning(f"Usuário JWT não encontrado: {username}")
                    return JSONResponse(
                        status_code=HTTP_401_UNAUTHORIZED,
                        content={"detail": "Usuário não encontrado"}
                    )
                
                # Verifica o escopo do token ANTES de checar o sal de sessão
                token_scope = token_data.get("scope")

                # Se for um token de escopo especial (como para troca de senha),
                # não verificamos o sal de sessão.
                if token_scope == "force_password_change":
                    logger.info(f"Token de escopo especial '{token_scope}' validado para o usuário '{username}'.")
                else:
                    # Se for um token de acesso normal, AÍ SIM verificamos o sal.
                    token_salt = token_data.get("salt")
                    db_salt = user.get("session_salt")
                    if token_salt != db_salt:
                        logger.warning(f"Token com sal de sessão inválido para o usuário '{username}'. Possível sessão antiga ou roubada.")
                        return JSONResponse(
                            status_code=HTTP_401_UNAUTHORIZED,
                            content={"detail": "Sessão inválida. Por favor, faça login novamente."}
                        )
                
                # Rate limiting para JWT
                user_identifier = f"jwt:{user['id']}"
                if rate_limiters["jwt"].is_rate_limited(user_identifier):
                    logger.warning(f"Rate limit excedido para usuário JWT: {user['username']}")
                    return JSONResponse(
                        status_code=HTTP_429_TOO_MANY_REQUESTS,
                        content={"detail": "Muitas requisições. Tente novamente em alguns instantes."}
                    )
                
                # Obtem permissões via RBAC Service
                permissions = rbac_service.get_user_permissions(user["id"])
                
                # Configurar request state
                request.state.user = token_data
                request.state.current_user_id = user["id"]
                request.state.current_user_obj = user
                request.state.auth_method = "jwt"
                request.state.cliente_id = user.get("cliente_id")
                request.state.permissions = permissions
                logger.info(f"Acesso autorizado via JWT: {username}")
                return await call_next(request)
            else:
                logger.warning(f"Token JWT inválido: {request.url.path} - IP: {client_ip}")
                return JSONResponse(
                    status_code=HTTP_401_UNAUTHORIZED,
                    content={"detail": "Token inválido ou expirado"}
                )
                
        except Exception as jwt_error:
            logger.error(f"Erro ao verificar JWT: {jwt_error}")
            return JSONResponse(
                status_code=HTTP_401_UNAUTHORIZED,
                content={"detail": "Token inválido"}
            )
    
    # Autenticação API Key (Integrações) - FUNCIONALIDADE ORIGINAL
    elif api_key:
        # Rate limiting para API Key
        api_key_identifier = f"api_key:{hashlib.md5(api_key.encode()).hexdigest()}"
        if rate_limiters["api_key"].is_rate_limited(api_key_identifier):
            logger.warning(f"Rate limit excedido para API Key: {mask_api_key(api_key)}")
            return JSONResponse(
                status_code=HTTP_429_TOO_MANY_REQUESTS,
                content={"detail": "Rate limit excedido para API Key"}
            )
        
        # Se a chave for igual à API_KEY do .env
        if api_key == API_KEY:
            # Rate limiting mais generoso para super acesso
            super_identifier = f"super:{client_ip}"
            if rate_limiters["super_access"].is_rate_limited(super_identifier):
                logger.warning(f"Rate limit excedido para SUPER ACESSO - IP: {client_ip}")
                return JSONResponse(
                    status_code=HTTP_429_TOO_MANY_REQUESTS,
                    content={"detail": "Rate limit excedido para super acesso"}
                )
            
            request.state.current_profile = {
                "id": 0,
                "nome": "super_admin", 
                "descricao": "Acesso total via API Key master"
            }
            request.state.auth_method = "api_key"
            request.state.is_super_access = True
            request.state.permissions = {"*": {"pode_acessar": True}}
            
            logger.info(f"Acesso SUPER ADMIN via API Key: {request.url.path} - IP: {client_ip}")
            return await call_next(request)
        
        # API Key normal - valida no banco
        try:
            from app.database import get_api_key_profile
            profile = get_api_key_profile(api_key)
            
            if profile and profile.get("ativa"):
                permissions = rbac_service.get_api_key_permissions(api_key)
                
                request.state.current_profile = profile
                request.state.auth_method = "api_key"
                request.state.is_super_access = False
                request.state.permissions = permissions
                
                logger.info(f"Acesso autorizado via API Key: {profile['nome']} - Módulos: {list(permissions.keys())}")
                return await call_next(request)
            else:
                logger.warning(f"API Key inativa ou não encontrada: {mask_api_key(api_key)}")
                
        except Exception as e:
            logger.error(f"Erro ao validar API Key: {e}")
        
        logger.warning(f"API Key inválida: {request.url.path} - Key: {mask_api_key(api_key)} - IP: {client_ip}")
        return JSONResponse(
            status_code=HTTP_403_FORBIDDEN,
            content={"detail": "API Key inválida ou expirada"}
        )
    
    # Nenhum método de autenticação fornecido
    else:
        logger.warning(f"Tentativa de acesso sem autenticação: {request.url.path} - IP: {client_ip}")
        return JSONResponse(
            status_code=HTTP_401_UNAUTHORIZED,
            content={"detail": "Token JWT ou API Key necessários"}
        )

def check_module_permission_OLD(request: Request, module_name: str):
    """Verifica se o usuário atual tem permissão para acessar um módulo"""
    if not hasattr(request.state, 'permissions'):
        raise HTTPException(
            status_code=status.HTTP_403_FORBIDDEN,
            detail="Permissões não disponíveis"
        )
    
    # Super acesso tem permissão total
    if getattr(request.state, 'is_super_access', False):
        return True
    
    permissions = request.state.permissions
    
    # Verifica se o módulo está nas permissões
    if module_name in permissions and permissions[module_name]["pode_acessar"]:
        return True
    
    # Verifica acesso wildcard
    if "*" in permissions and permissions["*"]["pode_acessar"]:
        return True
    
    raise HTTPException(
        status_code=status.HTTP_403_FORBIDDEN,
        detail=f"Seu perfil não tem acesso ao módulo '{module_name}'"
    )

def get_active_api_keys_OLD():
    """Obtém todas as API Keys ativas (função auxiliar para middleware)"""
    with get_db_connection() as conn:
        with conn.cursor() as cur:
            cur.execute("""
                SELECT ak.id, ak.chave_hash, p.nome as perfil, ak.descricao, 
                       ak.criado_em, ak.expira_em, ak.limites_uso
                FROM api_keys ak
                JOIN perfis p ON ak.perfil_id = p.id
                WHERE ak.ativa = true
                ORDER BY ak.criado_em DESC
            """)
            keys = []
            for row in cur.fetchall():
                keys.append({
                    "id": row[0],
                    "chave_hash": row[1],
                    "perfil": row[2],
                    "descricao": row[3],
                    "criado_em": row[4],
                    "expira_em": row[5],
                    "limites_uso": row[6]
                })
            return keys
