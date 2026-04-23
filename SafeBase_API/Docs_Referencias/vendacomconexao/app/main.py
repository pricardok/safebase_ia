# backend/app/main.py
from fastapi import FastAPI, HTTPException, Depends, Request, status, APIRouter
from fastapi.middleware.cors import CORSMiddleware
from fastapi.openapi.utils import get_openapi
from pydantic import BaseModel
from absl import logging as absl_logging
import google.generativeai as genai
import os
import time
import random
import json
import traceback
from dotenv import load_dotenv
from typing import Optional, List, Dict, Any
import logging
from datetime import datetime
import requests
import openai
from openai import AzureOpenAI

# Importações do sistema de prompts
from app.services.prompt_service import prompt_service
from app.prompts.registry import get_all_prompts, get_prompts_by_module
from app.services.audit_service import audit_service, AuditAction
from app.auth_integrations import validate_integration_api_key
from app.dependencies import check_module_permission, chamar_ia_otimizado
from app.services.planos_service import planos_service

# Importações de modelos e autenticação
from app.models import (
    SimuladorRequest, FeedbackRequest, ObjecaoRequest, DetectorRequest,
    ConexaoPerguntasRequest, ConexaoAnaliseRequest, ConexaoDialogoRequest,
    ScriptsOtimizarRequest, ScriptsAnaliseRequest, ScriptsVariacoesRequest, UserLoginResponse,
    AnaliseConversaRequest, ProbabilidadeConversaoRequest,
    UserLogin, UserRegister, TokenResponse, UserResponse,
    ContextoAlinhamentoRequest, ContextoAlinhamentoResponse,
    PredicaoObjecoesRequest, PredicaoObjecoesResponse,
    MudancaEmocionalRequest, MudancaEmocionalResponse,
    SimulacaoCreate, SimulacaoResponse, ScriptExemploResponse, OnboardingResponse,
    UserBehavioralProfileResponse, UserHistorySummaryResponse,
    PromptVersaoCreate, PromptVersaoResponse, PromptVersaoUpdate
)
from app.database import (
    get_db_connection,
    get_all_profiles, get_all_modules, create_api_key, deactivate_api_key,
    get_active_api_keys, assign_user_profile, get_user_by_username, create_user, # Funções de usuário e RBAC
    save_simulation, get_user_simulations, user_has_simulations, # Funções de histórico
    get_example_scripts, create_example_simulation, # Funções de onboarding
    get_user_simulations_secure, validate_simulation_ownership,
    get_prompt_versoes, create_prompt_versao, update_prompt_versao_status,
    get_active_prompt_versoes_for_testing
)
from app.auth_jwt import create_access_token, verify_password, get_password_hash, ACCESS_TOKEN_EXPIRE_MINUTES
from app.middleware import auth_middleware

# Novos imports para serviços melhorados
from app.services.rbac_service import rbac_service
from app.services.history_service import history_service
from app.services.database_context import db_context
from app.services.signup_service import signup_service
from app.utils.parsing_utils import parsing_utils

# Orquestração
from app.services.ia_orchestrator import ia_orchestrator
from app.services.key_manager import key_manager

from app.models_planos import (
    PlanoPublicoResponse, UpgradePlanoRequest, PlanoEfetivoResponse, EstatisticasUsoResponse, SignupRequest, UserWithTenantAndPlano,
    DescontoClienteCreate, DescontoClienteResponse
)

# Funçoes auxiliares e sistema Mock
from app.services.utils_service import ( 
    parsear_resposta_contexto, 
    parsear_resposta_predicao_objecoes,
    parsear_resposta_mudanca_emocional,
    parsear_analise_textual,
    analisar_probabilidade_conversao,
    parsear_respostas_ia,
    gerar_resposta_mock_simulador,
    gerar_feedback_mock,
    quebrar_objecao_mock,
    detector_analisar_mock,
    gerar_mock_contexto,
    gerar_mock_predicao_objecoes,
    gerar_mock_mudanca_emocional
)



from app.services.email_service import email_service
from app.routes.email_routes import email_router
from app.routes.simulador_routes import router as simulador_router
from app.routes.admin_routes_portal import router as admin_portal_router
from app.routes.admin_routes import router as admin_router
from app.routes.objecoes_routes import router as objecoes_router
from app.routes.detector_routes import router as detector_router
from app.routes.conexao_routes import router as conexao_router
from app.routes.scripts_routes import router as scripts_router
from app.routes.analise_routes import router as analise_router
from app.routes.auth_routes import router as auth_router
from app.routes.planos_routes import router as planos_router
from app.routes.historico_routes import router as historico_router
from app.routes.prompts_routes import router as prompts_router
from app.routes import hooks_routes
from app.routes.waha_routes import router as waha_router

from app.services.logging_service import setup_database_logging

# Configura logging
logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)
load_dotenv()

# ========== METADADOS DAS TAGS ==========
tags_metadata = [
    {
        "name": "Geral",
        "description": "Endpoints gerais do sistema - Health check e status"
    },
    {
        "name": "Administração", 
        "description": "Gestão de usuários, perfis, API Keys e configurações do sistema"
    },
    {
        "name": "Admin - IA",
        "description": "Gestão do sistema de orquestração IA - provedores, chaves e métricas"
    },
    {
        "name": "Analise",
        "description": "Análises preditivas e emocionais - ContextGuardian, EmotionShift, ObjectionPredictor"
    },
    {
        "name": "Simulador", 
        "description": "Simulador de conversas com clientes IA + feedback em tempo real"
    },
    {
        "name": "Objecoes",
        "description": "Sistema de quebra de objeções com 5 abordagens"
    },
    {
        "name": "Detector",
        "description": "Detector de vendedor chato - análise de tom e empatia"
    },
    {
        "name": "Conexão",
        "description": "Módulo de conexão avançada - perguntas, análise e diálogos"
    },
    {
        "name": "Scripts", 
        "description": "Otimização, análise e variações de scripts de vendas"
    },
    {
        "name": "Histórico",
        "description": "Histórico de simulações e dados de onboarding"
    },
    {
        "name": "Prompts",
        "description": "Gestão e renderização de prompts do sistema"
    },
    {
        "name": "Integrações",
        "description": "Endpoints para receber notificações de sistemas externos (webhooks).",
        "externalDocs": {
            "description": "Requer autenticação via X-API-KEY-INTEGRACOES ou ?api_key=",
        },
    }
]

# ========== CONFIGURAÇÃO ==========
app = FastAPI(
    title="Venda+ API", 
    version="1.4.0",
    description="Venda+ Assistente de Vendas",
    docs_url="/docs",
    redoc_url="/redoc",
    openapi_tags=tags_metadata
)
# Importar rotas de IA
from app.routes.email_routes import email_router

# ========== CONFIGURAÇÃO DO OPENAPI ==========
def custom_openapi():
    if app.openapi_schema:
        return app.openapi_schema
        
    openapi_schema = get_openapi(
        title=app.title,
        version=app.version,
        description=app.description,
        routes=app.routes,
    )
    
    # Adiciona security scheme para API Key
    openapi_schema["components"]["securitySchemes"] = {
        "ApiKeyAuth": {
            "type": "apiKey",
            "in": "header",
            "name": "X-API-Key",
            "description": "Insira a API Key:"
        }
    }
    
    # Adiciona segurança globalmente a todos os endpoints
    if "security" not in openapi_schema:
        openapi_schema["security"] = []
    openapi_schema["security"].append({"ApiKeyAuth": []})
    
    app.openapi_schema = openapi_schema
    return app.openapi_schema

app.openapi = custom_openapi

# ========== CONFIGURAÇÃO DO SISTEMA IA ==========
USE_MOCK = os.getenv("USE_MOCK", "false").lower() == "true"

# ========== CONFIGURAÇÃO CORS ==========
app.add_middleware(
    CORSMiddleware,
    #allow_origins=["http://localhost:3000", "http://127.0.0.1:3000","http://3.145.90.222:3000"],
    allow_origins=["*"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
    expose_headers=["*"],
    max_age=600,
)

# ========== EVENTOS DA APLICAÇÃO ==========
@app.on_event("startup")
async def startup_event():
    # Variável global para manter a referência ao listener de log
    global db_log_listener

    # Importa a configuração que decide se o banco deve ser inicializado.
    from app.config import AMBIENTE_ENV, API_LOGGING_ENABLED
    
    # Inicializa o sistema de logging do absl
    absl_logging.set_verbosity(absl_logging.INFO)

    # Inicia o logging para o banco de dados se estiver habilitado, independentemente do modo de criação de ambiente.
    if API_LOGGING_ENABLED:
        db_log_listener = setup_database_logging()
        logger.info("Serviço de logging para o banco de dados ativado.")

# ========== REGISTRO DO MIDDLEWARE ==========
@app.middleware("http")
async def authentication_middleware(request: Request, call_next):
    return await auth_middleware(request, call_next)
    
# Importa as configurações globais após a inicialização dos serviços
from app.config import USE_MOCK_FORCADO, PROVIDER_NOME

# ========== ROTAS PÚBLICAS (OCULTAS DO SWAGGER) ==========
@app.get("/", include_in_schema=False, tags=["Geral"])
async def root():
    API_KEY = os.getenv("API_KEY")
    status_ia = ia_orchestrator.get_system_status()
    modo = "MOCK" if status_ia['mock_ativo'] else "ORCHESTRATOR"
    auth_status = "PROTEGIDO" if API_KEY else "DESPROTEGIDO"
    
    return {
        "message": f"Venda+ API v4.0.0 ({modo})", 
        "status": "online",
        "autenticacao": auth_status,
        "timestamp": datetime.now().isoformat(),
        "sistema_ia": {
            "provedores_ativos": status_ia['estatisticas']['provedores_operacionais'],
            "chaves_operacionais": status_ia['estatisticas']['chaves_ativas'],
            "modo_mock": status_ia['mock_ativo']
        },
        "features": [
            "orquestracao-ia-automatica", 
            "analise-preditiva", 
            "metricas-tempo-real",
            "multi-provedores",
            "gestao-segura-chaves"
        ],
        "documentation": "/docs"
    }

@app.get("/health", include_in_schema=False, tags=["Geral"])
async def health_check():
    # Verifica a conexão com o banco de dados
    db_status = "connected"
    try:
        with get_db_connection() as conn:
            with conn.cursor() as cur:
                cur.execute("SELECT 1")
                cur.fetchone()
    except Exception as e:
        logger.error(f"Health check falhou na conexão com o banco: {e}")
        db_status = "error"
        raise HTTPException(status_code=503, detail={"database_status": db_status, "error": str(e)})

    # Verifica o status do sistema de IA
    status_ia = ia_orchestrator.get_system_status()
    ia_healthy = status_ia['estatisticas']['provedores_operacionais'] > 0 or status_ia['mock_ativo']
    
    return {
        "status": "healthy",
        "version": "4.0.0",
        "timestamp": datetime.now().isoformat(),
        "database": db_status,
        "sistema_ia": {
            "status": "operational" if ia_healthy else "degraded",
            "modo": "mock" if status_ia['mock_ativo'] else "orchestrator",
            "provedores_operacionais": status_ia['estatisticas']['provedores_operacionais'],
            "chaves_ativas": status_ia['estatisticas']['chaves_ativas'],
            "performance": status_ia.get('performance_metrics', {})
        },
        "authentication": "api_key_required"
    }

app.include_router(email_router)
app.include_router(simulador_router)
app.include_router(admin_portal_router) # Adicionado ANTES do admin_router legado
app.include_router(admin_router)
app.include_router(objecoes_router)
app.include_router(detector_router)
app.include_router(conexao_router)
app.include_router(scripts_router)
app.include_router(analise_router)
app.include_router(auth_router)
app.include_router(planos_router)
app.include_router(historico_router)
app.include_router(prompts_router)
app.include_router(hooks_routes.router)
app.include_router(waha_router)

# Importa e registra o novo router de integrações
from app.routes.integrations_routes import router as integrations_router
app.include_router(integrations_router)

if __name__ == "__main__":
    import uvicorn
    uvicorn.run(app, host="0.0.0.0", port=8000)