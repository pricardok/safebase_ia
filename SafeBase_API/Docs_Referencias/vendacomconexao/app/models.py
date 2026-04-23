# backend/app/models.py
from pydantic import BaseModel, Field, field_validator, EmailStr
from typing import Optional, List, Dict, Any, Union, Literal
from datetime import datetime
from enum import Enum
import json
import traceback
 
# ========== IA ==========

class ProviderUpdate(BaseModel):
    ativo: Optional[bool] = None
    ordem_prioridade: Optional[int] = None
    config_modelo: Optional[Dict] = None

class KeyCreate(BaseModel):
    provedor_id: int
    chave_real: str
    ordem_prioridade: Optional[int] = Field(1, description="Prioridade da chave individual (menor número = maior prioridade)")
    descricao: Optional[str] = None

class KeyResponse(BaseModel):
    id: int
    provedor_nome: str
    chave_mascarada: str
    descricao: Optional[str]
    ativa: bool
    falhas_consecutivas: int
    ultima_sucesso: Optional[datetime]
    ultima_falha: Optional[datetime]
    total_requisicoes: int
    ordem_prioridade: int
    total_erros: int
    created_at: datetime
    operacional: bool

class GlobalKeyStatusResponse(BaseModel):
    """Schema para o status de uma chave na lista de roteamento global."""
    id: int
    provider_name: str
    provider_priority: int
    key_priority: int # Nova prioridade da chave individual
    chave_mascarada: str
    descricao: Optional[str]
    ativa: bool
    falhas_consecutivas: int
    ultima_sucesso: Optional[datetime] = None
    ultima_falha: Optional[datetime] = None
    total_requisicoes: int
    total_erros: int

# ========== SCHEMAS PARA PRODUTO_DESCRICAO ==========

class SimuladorRequest(BaseModel):
    """Schema para requisições do simulador de conversa"""
    produto_descricao: str = Field(..., description="Descrição do produto/serviço sendo vendido")
    perfil_cliente: str = Field(..., description="Perfil do cliente (frio, morno, quente)")
    mensagem_usuario: str = Field(..., description="Mensagem enviada pelo vendedor")
    historico: Optional[str] = Field("", description="Histórico da conversa em formato texto")
    incluir_copiloto: bool = Field(False, description="Se deve incluir a análise do Co-piloto Cognitivo na resposta")
    session_id: Optional[str] = Field(None, description="ID único para agrupar a sessão de conversa")

class PlaybookPassoResponse(BaseModel):
    acao: str
    sugestao: str

class PlaybookRecomendadoResponse(BaseModel):
    nome: str
    passos: List[PlaybookPassoResponse]

class CopilotoResponse(BaseModel):
    """Schema para as análises consolidadas do Co-piloto Cognitivo."""
    contexto_alinhamento: Optional['ContextoAlinhamentoResponse'] = None
    predicao_objecoes: Optional['PredicaoObjecoesResponse'] = None
    mudanca_emocional: Optional['MudancaEmocionalResponse'] = None
    playbook_recomendado: Optional[PlaybookRecomendadoResponse] = None

class SimuladorResponse(BaseModel):
    """Schema para respostas do simulador de conversa"""
    resposta: str = Field(..., description="Resposta do cliente simulado")
    perfil: str = Field(..., description="Perfil do cliente utilizado")
    modo: str = Field(..., description="Modo de operação (gemini, mock, mock-fallback)")
    session_id: str = Field(..., description="ID da sessão de conversa")
    copiloto: Optional[CopilotoResponse] = Field(None, description="Análises do Co-piloto Cognitivo (se solicitado)")

class FeedbackRequest(BaseModel):
    """Schema para requisições de feedback"""
    produto_descricao: str = Field(..., description="Descrição do produto/serviço sendo vendido")
    mensagem_usuario: str = Field(..., description="Mensagem a ser analisada")
    session_id: Optional[str] = Field(None, description="ID único para agrupar a sessão de conversa")

class FeedbackResponse(BaseModel):
    """Schema para respostas de feedback"""
    feedback: str = Field(..., description="Feedback da IA sobre a mensagem")
    modo: str = Field(..., description="Modo de operação (gemini, mock, mock-fallback)")

# ========== SCHEMAS PARA QUEBRA DE OBJEÇÕES ==========

class ObjecaoRequest(BaseModel):
    """Schema para requisições de quebra de objeções"""
    produto_descricao: str = Field(..., description="Descrição do produto/serviço sendo vendido")
    objecao: str = Field(..., description="Objeção do cliente")
    session_id: Optional[str] = Field(None, description="ID único para agrupar a sessão de conversa")

class ObjecaoResponse(BaseModel):
    """Schema para respostas de quebra de objeções - 5 abordagens"""
    resposta_empatica: str = Field(..., description="Resposta com abordagem empática")
    resposta_valor: str = Field(..., description="Resposta com abordagem de valor")
    resposta_prova_social: str = Field(..., description="Resposta com prova social")
    resposta_urgencia: str = Field(..., description="Resposta criando senso de urgência")
    resposta_autoridade: str = Field(..., description="Resposta com autoridade e dados")
    modo: str = Field(..., description="Modo de operação (gemini, mock, mock-fallback)")

# ========== SCHEMAS PARA DETECTOR DE VENDEDOR ==========

class DetectorRequest(BaseModel):
    """Schema para requisições do detector de vendedor chato"""
    produto_descricao: str = Field(..., description="Descrição do produto/serviço sendo vendido")
    mensagem: str = Field(..., description="Mensagem a ser analisada")
    incluir_copiloto: bool = Field(False, description="Se deve incluir a análise do Co-piloto Cognitivo na resposta")
    session_id: Optional[str] = Field(None, description="ID único para agrupar a sessão de conversa")

class DetectorResponse(BaseModel):
    """Schema para respostas do detector de vendedor chato - Análise estruturada"""
    classificacao: str = Field(..., description="Classificação do tom: AGRESSIVO, POUCA_EMPATIA, EQUILIBRADO, CONSULTIVO, EMPÁTICO")
    motivo: str = Field(..., description="Explicação detalhada da classificação")
    sugestao: str = Field(..., description="Sugestão de melhoria")
    pontuacao_empatia: int = Field(..., description="Pontuação de empatia de 0 a 100")
    nivel_pressao: str = Field(..., description="Nível de pressão: BAIXO, MÉDIO, ALTO")
    indicadores_problema: List[str] = Field(..., description="Lista de indicadores de problema na mensagem")
    exemplo_corrigido: str = Field(..., description="Exemplo de mensagem corrigida")
    copiloto: Optional[CopilotoResponse] = Field(None, description="Análises do Co-piloto Cognitivo (se solicitado)")
    modo: str = Field(..., description="Modo de operação (gemini, mock, mock-fallback)")

# ========== SCHEMAS PARA MÓDULO DE CONEXÃO ==========

class ConexaoPerguntasRequest(BaseModel):
    """Schema para gerar perguntas personalizadas"""
    produto_descricao: str = Field(..., description="Descrição do produto/serviço")
    contexto_cliente: str = Field(..., description="Contexto ou perfil do cliente")
    session_id: Optional[str] = Field(None, description="ID único para agrupar a sessão de conversa")

class ConexaoPerguntasResponse(BaseModel):
    """Schema para respostas de perguntas personalizadas"""
    perguntas: List[str] = Field(..., description="Lista de perguntas personalizadas")
    dicas: str = Field(..., description="Dicas de aplicação")
    modo: str = Field(..., description="Modo de operação")

class ConexaoAnaliseRequest(BaseModel):
    """Schema para análise de respostas do vendedor"""
    produto_descricao: str = Field(..., description="Descrição do produto/serviço")
    pergunta: str = Field(..., description="Pergunta feita ao cliente")
    resposta_vendedor: str = Field(..., description="Resposta do vendedor")
    session_id: Optional[str] = Field(None, description="ID único para agrupar a sessão de conversa")

class ConexaoAnaliseResponse(BaseModel):
    """Schema para análise de respostas"""
    pontuacao: int = Field(..., description="Pontuação de 0-100")
    feedback: str = Field(..., description="Feedback detalhado")
    sugestoes: List[str] = Field(..., description="Sugestões de melhoria")
    modo: str = Field(..., description="Modo de operação")

class ConexaoDialogoRequest(BaseModel):
    """Schema para simulação de diálogo"""
    produto_descricao: str = Field(..., description="Descrição do produto/serviço")
    cenario: str = Field(..., description="Cenário da conversa")
    abordagem: str = Field(..., description="Abordagem do vendedor")
    session_id: Optional[str] = Field(None, description="ID único para agrupar a sessão de conversa")

class ConexaoDialogoResponse(BaseModel):
    """Schema para simulação de diálogo"""
    dialogo: List[Dict[str, str]] = Field(..., description="Diálogo simulado")
    analise: str = Field(..., description="Análise do diálogo")
    modo: str = Field(..., description="Modo de operação")

class RoteiroAbordagemRequest(BaseModel):
    """Schema para geração de roteiro de abordagem"""
    produto_descricao: str = Field(..., description="Descrição do produto/serviço")
    tipo_pessoa: Literal["pessoa fria", "pessoa conhecida", "pessoa de remarketing", "veio de anúncio"] = Field(..., description="Tipo de pessoa")
    nome_cliente: Optional[str] = Field(None, description="Nome do cliente (opcional)")
    session_id: Optional[str] = Field(None, description="ID único para agrupar a sessão de conversa")

class RoteiroAbordagemResponse(BaseModel):
    roteiro_ia: str = Field(..., description="Roteiro gerado em formato de texto")
    roteiro_estruturado: Dict[str, Any] = Field(..., description="Roteiro estruturado em JSON por etapas")
    modo: str = Field(..., description="Modo de operação (gemini, mock, mock-fallback)")
    session_id: Optional[str] = Field(None, description="ID da sessão de conversa")

# ========== SCHEMAS PARA SCRIPTS PRONTOS ==========

class ScriptsOtimizarRequest(BaseModel):
    """Schema para otimização de scripts"""
    produto_descricao: str = Field(..., description="Descrição do produto/serviço")
    script_original: str = Field(..., description="Script original")
    canal: str = Field(..., description="Canal de comunicação")
    objetivo: str = Field(..., description="Objetivo do script")
    session_id: Optional[str] = Field(None, description="ID único para agrupar a sessão de conversa")

class ScriptsOtimizarResponse(BaseModel):
    """Schema para scripts otimizados"""
    script_otimizado: str = Field(..., description="Script otimizado")
    melhorias: List[str] = Field(..., description="Lista de melhorias aplicadas")
    session_id: Optional[str] = Field(None, description="ID da sessão de conversa")
    modo: str = Field(..., description="Modo de operação")

class ScriptsOtimizarResponseCompleto(BaseModel):
    """Schema para resposta completa de otimização de scripts"""
    script_otimizado: str = Field(..., description="Script otimizado")
    melhorias: List[str] = Field(..., description="Lista de melhorias aplicadas")
    analise_eficacia: Dict[str, Union[str, int, List[str]]] = Field(
        default_factory=dict, 
        description="Análise de eficácia do script original."
    )
    variacoes: List[Dict[str, str]] = Field(
        default_factory=list, 
        description="Variações do script geradas pela IA."
    )

class ScriptsAnaliseRequest(BaseModel):
    """Schema para análise de eficácia"""
    produto_descricao: str = Field(..., description="Descrição do produto/serviço")
    script: str = Field(..., description="Script a ser analisado")
    canal: str = Field(..., description="Canal de comunicação")
    session_id: Optional[str] = Field(None, description="ID único para agrupar a sessão de conversa")

class ScriptsAnaliseResponse(BaseModel):
    """Schema para análise de eficácia"""
    pontuacao: int = Field(..., description="Pontuação de 0-100")
    pontos_fortes: List[str] = Field(..., description="Pontos fortes do script")
    pontos_fracos: List[str] = Field(..., description="Pontos fracos do script")
    sugestoes: List[str] = Field(..., description="Sugestões de melhoria")
    session_id: Optional[str] = Field(None, description="ID da sessão de conversa")
    modo: str = Field(..., description="Modo de operação")

class ScriptsVariacoesRequest(BaseModel):
    """Schema para geração de variações"""
    produto_descricao: str = Field(..., description="Descrição do produto/serviço")
    script_base: str = Field(..., description="Script base")
    canal: str = Field(..., description="Canal de comunicação")
    numero_variacoes: int = Field(3, description="Número de variações")
    session_id: Optional[str] = Field(None, description="ID único para agrupar a sessão de conversa")

class ScriptsVariacoesResponse(BaseModel):
    """Schema para variações de scripts"""
    variacoes: List[Dict[str, str]] = Field(..., description="Variações geradas")
    session_id: Optional[str] = Field(None, description="ID da sessão de conversa")
    modo: str = Field(..., description="Modo de operação")

# ========== SCHEMAS PARA HISTÓRICO E ONBOARDING ==========

class SimulacaoCreate(BaseModel):
    modulo: str = Field(..., description="Módulo onde a simulação foi realizada")
    produto_descricao: str = Field(..., description="Descrição do produto/serviço")
    perfil_cliente: Optional[str] = Field(None, description="Perfil do cliente (frio, morno, quente)")
    conversa: Optional[Dict[str, Any]] = Field(None, description="Histórico completo da conversa")
    metricas: Optional[Dict[str, Any]] = Field(None, description="Métricas da simulação")
    feedback_ia: Optional[str] = Field(None, description="Feedback da IA sobre a simulação")
    is_exemplo: bool = Field(False, description="Se é um exemplo de onboarding")
    session_id: Optional[str] = Field(None, description="ID da sessão de conversa")

class SimulacaoResponse(BaseModel):
    id: int = Field(..., description="ID da simulação")
    modulo: str = Field(..., description="Módulo onde a simulação foi realizada")
    produto_descricao: str = Field(..., description="Descrição do produto/serviço")
    perfil_cliente: Optional[str] = Field(None, description="Perfil do cliente")
    conversa: Optional[Dict[str, Any]] = Field(None, description="Histórico da conversa")
    metricas: Optional[Dict[str, Any]] = Field(None, description="Métricas da simulação")
    feedback_ia: Optional[str] = Field(None, description="Feedback da IA")
    data_criacao: datetime = Field(..., description="Data de criação")
    is_exemplo: bool = Field(..., description="Se é um exemplo")
    session_id: Optional[str] = Field(None, description="ID da sessão de conversa")

class ScriptExemploResponse(BaseModel):
    id: str = Field(..., description="ID do script")
    modulo: str = Field(..., description="Módulo do script")
    canal: str = Field(..., description="Canal do script")
    script: str = Field(..., description="Conteúdo do script")
    data_criacao: datetime = Field(..., description="Data de criação")

class OnboardingResponse(BaseModel):
    simulacoes_exemplo: List[SimulacaoResponse] = Field(..., description="Simulações de exemplo")
    scripts_exemplo: List[ScriptExemploResponse] = Field(..., description="Scripts de exemplo")

class SessaoConversaResponse(BaseModel):
    """Schema para uma sessão de conversa agrupada."""
    session_id: Optional[str] = Field(..., description="ID da sessão de conversa")
    produto_descricao: str = Field(..., description="Descrição do produto principal da sessão")
    data_inicio: datetime = Field(..., description="Data de início da sessão")
    modulos_utilizados: List[str] = Field(..., description="Módulos utilizados na sessão")
    interacoes: List[SimulacaoResponse] = Field(..., description="Lista de interações (simulações) na sessão")
    total_interacoes: int = Field(..., description="Total de interações na sessão")

# ========== SCHEMAS GERAIS E UTILITÁRIOS ==========

class ProdutoRequest(BaseModel):
    """Schema para requisições que precisam apenas da descrição do produto"""
    produto_descricao: str = Field(..., description="Descrição do produto/serviço sendo vendido")

class HealthCheck(BaseModel):
    """Schema para resposta de health check"""
    status: str = Field(..., description="Status da API")
    message: str = Field(..., description="Mensagem de status")
    timestamp: datetime = Field(..., description="Timestamp da verificação")
    version: str = Field(..., description="Versão da API")

class ErrorResponse(BaseModel):
    """Schema para respostas de erro"""
    error: str = Field(..., description="Mensagem de erro")
    details: Optional[str] = Field(None, description="Detalhes do erro")
    error_code: Optional[str] = Field(None, description="Código do erro")

# ========== SCHEMAS PARA FUTURAS EXPANSÕES ==========

class ConversaHistorico(BaseModel):
    """Schema para histórico de conversas"""
    id: Optional[str] = Field(None, description="ID da conversa")
    produto_descricao: str = Field(..., description="Produto/serviço utilizado")
    perfil_cliente: str = Field(..., description="Perfil do cliente")
    mensagens: List[Dict[str, Any]] = Field(..., description="Lista de mensagens da conversa")
    pontuacao: Optional[float] = Field(None, description="Pontuação final da conversa")
    criado_em: datetime = Field(..., description="Data de criação")

class AnaliseDesempenho(BaseModel):
    """Schema para análise de desempenho do usuário"""
    usuario_id: str = Field(..., description="ID do usuário")
    modulo: str = Field(..., description="Módulo utilizado")
    produto_descricao: str = Field(..., description="Produto/serviço utilizado")
    metricas: Dict[str, Any] = Field(..., description="Métricas de desempenho")
    data_analise: datetime = Field(..., description="Data da análise")

# ========== SCHEMAS PARA CONTEXTGUARDIAN ==========

class ContextoAlinhamentoRequest(BaseModel):
    produto_descricao: str = Field(..., description="Descrição do produto/serviço")
    conversa: List[Dict[str, str]] = Field(..., description="Histórico da conversa")
    historico_contexto: Optional[List[str]] = Field([], description="Histórico de contextos mencionados")
    session_id: Optional[str] = Field(None, description="ID único para agrupar a sessão de conversa")

class ContextoAlinhamentoResponse(BaseModel):
    alinhamento_contextual: float = Field(..., description="Nível de alinhamento contextual (0.0-1.0)")
    ruptura_detectada: bool = Field(..., description="Se foi detectada ruptura de contexto")
    sugestao_transicao: str = Field(..., description="Sugestão de transição para realinhar")
    topicos_nao_abordados: List[str] = Field(..., description="Tópicos não abordados pelo vendedor")
    nivel_urgencia: str = Field(..., description="Nível de urgência: BAIXO, MEDIO, ALTO")
    modo: str = Field(..., description="Modo de operação")

# ========== SCHEMAS PARA OBJECTIONPREDICTOR ==========

class PredicaoObjecoesRequest(BaseModel):
    produto_descricao: str = Field(..., description="Descrição do produto/serviço")
    conversa: List[Dict[str, str]] = Field(..., description="Histórico da conversa")
    nicho: str = Field(..., description="Nicho do cliente")
    perfil_cliente: str = Field(..., description="Perfil do cliente")
    session_id: Optional[str] = Field(None, description="ID único para agrupar a sessão de conversa")

class PredicaoObjecoesResponse(BaseModel):
    objecoes_provaveis: List[Dict[str, Any]] = Field(..., description="Lista de objeções previstas")
    sinais_detectados: List[str] = Field(..., description="Sinais comportamentais detectados")
    abordagem_preventiva: str = Field(..., description="Abordagem preventiva sugerida")
    nivel_risco: str = Field(..., description="Nível de risco: BAIXO, MEDIO, ALTO")
    modo: str = Field(..., description="Modo de operação")


# ========== SCHEMAS PARA EMOTIONSHIFT ==========

class MudancaEmocionalRequest(BaseModel):
    produto_descricao: str = Field(..., description="Descrição do produto/serviço")
    conversa: List[Dict[str, str]] = Field(..., description="Histórico da conversa")
    metricas_base: Optional[Dict[str, Any]] = Field({}, description="Métricas base para comparação")
    session_id: Optional[str] = Field(None, description="ID único para agrupar a sessão de conversa")

class MudancaEmocionalResponse(BaseModel):
    mudanca_detectada: bool = Field(..., description="Se foi detectada mudança emocional")
    ponto_virada: str = Field(..., description="Ponto onde a mudança ocorreu")
    direcao_mudanca: str = Field(..., description="Direção da mudança: POSITIVO_PARA_NEGATIVO, NEGATIVO_PARA_POSITIVO")
    emocao_antes: str = Field(..., description="Emoção antes da mudança")
    emocao_depois: str = Field(..., description="Emoção depois da mudança")
    fator_critico: str = Field(..., description="Fator que causou a mudança")
    sugestao_ajuste_imediato: str = Field(..., description="Sugestão de ajuste imediato")
    estrategia_recuperacao: str = Field(..., description="Estratégia de recuperação")
    alerta_risco: str = Field(..., description="Nível de alerta: BAIXO, MEDIO, ALTO")
    probabilidade_recuperacao: float = Field(..., description="Probabilidade de recuperação (0.0-1.0)")
    modo: str = Field(..., description="Modo de operação")

# ========== NOVOS SCHEMAS PARA A/B TESTING ==========

class PromptVersaoCreate(BaseModel):
    """[FASE 1] Schema para criação de versão de prompt"""
    nome: str = Field(..., description="Nome do prompt")
    versao: str = Field(..., description="Versão do prompt (ex: v1.0, v2.0)")
    template: str = Field(..., description="Template do prompt")
    modulo: Optional[str] = Field(None, description="Módulo do prompt")
    descricao: Optional[str] = Field(None, description="Descrição da versão")
    parametros: Optional[Dict[str, Any]] = Field(None, description="Parâmetros do prompt")
    peso_teste: float = Field(1.0, description="Peso para A/B testing (0.0-1.0)")

class PromptVersaoResponse(BaseModel):
    """[FASE 1] Schema para resposta de versão de prompt"""
    id: int = Field(..., description="ID da versão")
    nome: str = Field(..., description="Nome do prompt")
    versao: str = Field(..., description="Versão do prompt")
    template: str = Field(..., description="Template do prompt")
    ativa: bool = Field(..., description="Se a versão está ativa")
    peso_teste: float = Field(..., description="Peso para A/B testing")
    modulo: Optional[str] = Field(None, description="Módulo do prompt")
    descricao: Optional[str] = Field(None, description="Descrição da versão")
    parametros: Optional[Dict[str, Any]] = Field(None, description="Parâmetros do prompt")
    criado_em: datetime = Field(..., description="Data de criação")
    atualizado_em: datetime = Field(..., description="Data de atualização")

class PromptVersaoUpdate(BaseModel):
    """[FASE 1] Schema para atualização de versão de prompt"""
    ativa: Optional[bool] = Field(None, description="Status ativo/inativo")
    peso_teste: Optional[float] = Field(None, description="Peso para A/B testing")
    descricao: Optional[str] = Field(None, description="Descrição da versão")

class UserBehavioralProfileResponse(BaseModel):
    """[FASE 1] Schema para resposta do perfil comportamental do usuário"""
    estilo: str = Field(..., description="Estilo predominante: AGRESSIVO, EMPÁTICO, TÉCNICO, CONSULTIVO, NEUTRO")
    lacunas: List[str] = Field(..., description="Lacunas identificadas")
    total_simulacoes: int = Field(..., description="Total de simulações analisadas")
    ultima_atualizacao: datetime = Field(..., description="Data da última atualização")

class UserHistorySummaryResponse(BaseModel):
    """[FASE 1] Schema para resposta do resumo do histórico do usuário"""
    resumo: str = Field(..., description="Resumo estruturado do histórico")
    modulo: str = Field(..., description="Módulo analisado")
    timestamp: datetime = Field(..., description="Timestamp da análise")

# ========== ENUMS PARA VALIDAÇÃO ==========

class PerfilClienteEnum(str, Enum):
    """Enumeração de perfis de cliente válidos"""
    FRIO = "frio"
    MORNO = "morno"
    QUENTE = "quente"

class CanalEnum(str, Enum):
    """Enumeração de canais de comunicação"""
    INSTAGRAM = "instagram"
    WHATSAPP = "whatsapp"
    TELEFONE = "telefone"
    PRESENCIAL = "presencial"
    EMAIL = "email"

class ModoOperacaoEnum(str, Enum):
    """Enumeração de modos de operação"""
    GEMINI = "gemini"
    MOCK = "mock"
    MOCK_FALLBACK = "mock-fallback"

class NivelProbabilidadeEnum(str, Enum):
    """Enumeração de níveis de probabilidade"""
    BAIXO = "baixo"
    MEDIO = "medio"
    ALTO = "alto"
    MUITO_ALTO = "muito_alto"

class TendenciaEnum(str, Enum):
    """Enumeração de tendências"""
    POSITIVA = "positiva"
    NEGATIVA = "negativa"
    ESTAVEL = "estavel"

class EstiloComportamentalEnum(str, Enum):
    """[FASE 1] Enumeração de estilos comportamentais"""
    AGRESSIVO = "AGRESSIVO"
    EMPÁTICO = "EMPÁTICO"
    TÉCNICO = "TÉCNICO"
    CONSULTIVO = "CONSULTIVO"
    NEUTRO = "NEUTRO"

# ========== SCHEMAS PARA ANÁLISE PREDITIVA ==========

class AnaliseConversaRequest(BaseModel):
    """Schema para análise completa de conversa"""
    produto_descricao: str = Field(..., description="Descrição do produto/serviço")
    conversa: List[Dict[str, str]] = Field(..., description="Histórico completo da conversa")
    perfil_cliente: str = Field(..., description="Perfil do cliente")
    session_id: Optional[str] = Field(None, description="ID único para agrupar a sessão de conversa")

class AnaliseConversaResponse(BaseModel):
    """Schema para resposta de análise de conversa"""
    analise: Dict[str, Any] = Field(..., description="Análise completa da conversa")
    modo: str = Field(..., description="Modo de operação")

class ProbabilidadeConversaoRequest(BaseModel):
    """Schema para cálculo de probabilidade de conversão"""
    produto_descricao: str = Field(..., description="Descrição do produto/serviço")
    mensagem_vendedor: str = Field(..., description="Última mensagem do vendedor")
    resposta_cliente: str = Field(..., description="Resposta do cliente")
    perfil_cliente: str = Field(..., description="Perfil do cliente")
    historico: List[Dict[str, str]] = Field([], description="Histórico anterior")
    session_id: Optional[str] = Field(None, description="ID único para agrupar a sessão de conversa")

class ProbabilidadeConversaoResponse(BaseModel):
    """Schema para resposta de probabilidade de conversão"""
    probabilidade: int = Field(..., description="Probabilidade de conversão (0-100)")
    nivel: str = Field(..., description="Nível de probabilidade")
    metricas: Dict[str, Any] = Field(..., description="Métricas detalhadas")
    sugestoes_imediata: List[str] = Field(..., description="Sugestões imediatas")
    modo: str = Field(..., description="Modo de operação")

# ======== logins =========== 

class UserLogin(BaseModel):
    login_identifier: str = Field(..., description="Nome de usuário ou e-mail")
    password: str = Field(..., description="Senha")

class UserRegister(BaseModel):
    username: str = Field(..., description="Nome de usuário")
    email: str = Field(..., description="Email do usuário")
    password: str = Field(..., description="Senha")
    full_name: str = Field(None, description="Nome completo")

class UserResponse(BaseModel):
    id: int = Field(..., description="ID do usuário")
    username: str = Field(..., description="Nome de usuário")
    email: str = Field(..., description="Email")
    full_name: str = Field(None, description="Nome completo")
    is_active: bool = Field(..., description="Usuário ativo")
    telefone: Optional[str] = Field(None, description="Telefone do usuário")

class UserLoginData(UserResponse):
    """Dados do usuário retornados no login, incluindo info do cliente e plano."""
    cliente: Optional[Dict[str, Any]] = Field(None, description="Informações básicas do cliente (tenant)")
    plano_atual: Optional[Dict[str, Any]] = Field(None, description="Plano atual do cliente")

class TokenResponse(BaseModel):
    access_token: str = Field(..., description="Token JWT")
    token_type: str = Field(..., description="Tipo do token")
    user: UserResponse = Field(..., description="Dados do usuário")

class UserLoginResponse(BaseModel):
    """Schema completo para a resposta de login."""
    access_token: str = Field(..., description="Token JWT")
    token_type: str = Field(..., description="Tipo do token")
    user: UserLoginData = Field(..., description="Dados completos do usuário, cliente e plano")

# ========== SCHEMAS PARA INTEGRAÇÕES (EX: KIWIFY) ==========

class _KiwifyProduct(BaseModel):
    product_id: str = Field(..., alias="product_id")
    product_name: str = Field(..., alias="product_name")

class _KiwifyCustomer(BaseModel):
    full_name: str = Field(..., alias="full_name")
    first_name: str = Field(..., alias="first_name")
    email: str = Field(..., alias="email")
    mobile: Optional[str] = Field(None, alias="mobile")
    CPF: Optional[str] = Field(None, alias="CPF")

class KiwifyWebhookPayload(BaseModel):
    """
    Schema para o payload de webhook da Kiwify, focando nos eventos de pedido.
    """
    order_id: str = Field(..., alias="order_id")
    order_status: str = Field(..., alias="order_status")
    webhook_event_type: str = Field(..., alias="webhook_event_type")
    Product: _KiwifyProduct
    Customer: _KiwifyCustomer

    def get_customer_email(self) -> str:
        return self.Customer.email

    def get_customer_name(self) -> str:
        return self.Customer.full_name

    def get_event_type(self) -> str:
        return self.webhook_event_type

class PlanoChangeRequest(BaseModel):
    """Schema para a requisição de mudança de plano pelo usuário."""
    plano_id: int = Field(..., description="O ID do novo plano desejado.")

class ProfileUpdateRequest(BaseModel):
    """Schema para a requisição de atualização de um perfil de permissões."""
    nome: Optional[str] = Field(None, description="O novo nome do perfil.")
    permissoes: Optional[List[str]] = Field(None, description="A lista completa de novas permissões para o perfil.")

    @field_validator('nome')
    def nome_must_not_be_empty(cls, v):
        if v is not None and not v.strip():
            raise ValueError('O nome do perfil não pode ser vazio.')
        return v

# ========== FUNÇÕES ÚTEIS ==========

def validar_perfil_cliente(perfil: str) -> bool:
    """Valida se o perfil do cliente é suportado"""
    return perfil.lower() in [perfil.value for perfil in PerfilClienteEnum]

def validar_canal(canal: str) -> bool:
    """Valida se o canal é suportado"""
    return canal.lower() in [canal.value for canal in CanalEnum]

def obter_perfis_suportados() -> List[str]:
    """Retorna lista de perfis suportados"""
    return [perfil.value for perfil in PerfilClienteEnum]

def obter_canais_suportados() -> List[str]:
    """Retorna lista de canais suportados"""
    return [canal.value for canal in CanalEnum]
    
def calcular_nivel_probabilidade(probabilidade: int) -> str:
    """Calcula o nível baseado na probabilidade"""
    if probabilidade >= 80:
        return "muito_alto"
    elif probabilidade >= 60:
        return "alto"
    elif probabilidade >= 40:
        return "medio"
    else:
        return "baixo"

def obter_cor_probabilidade(probabilidade: int) -> str:
    """Retorna cor CSS baseada na probabilidade"""
    if probabilidade >= 80:
        return "#10B981"  # Verde
    elif probabilidade >= 60:
        return "#F59E0B"  # Amarelo
    elif probabilidade >= 40:
        return "#EF4444"  # Vermelho
    else:
        return "#6B7280"  # Cinza

# ========== NOVAS FUNÇÕES ÚTEIS ==========

def validar_estilo_comportamental(estilo: str) -> bool:
    """[FASE 1] Valida se o estilo comportamental é suportado"""
    return estilo.upper() in [estilo.value for estilo in EstiloComportamentalEnum]

def obter_estilos_comportamentais() -> List[str]:
    """[FASE 1] Retorna lista de estilos comportamentais suportados"""
    return [estilo.value for estilo in EstiloComportamentalEnum]

def calcular_peso_teste(peso: float) -> float:
    """[FASE 1] Garante que o peso de teste está entre 0.0 e 1.0"""
    return max(0.0, min(1.0, peso))

# Atualiza as referências antecipadas após todas as classes terem sido definidas
CopilotoResponse.model_rebuild()
SimuladorResponse.model_rebuild()
DetectorResponse.model_rebuild()
PlaybookRecomendadoResponse.model_rebuild()

# ========== SCHEMAS PARA SERVIÇO DE E-MAIL (CONSOLIDADO) ==========

class EmailProviderEnum(str, Enum):
    BREVO = "brevo"
    SENDGRID = "sendgrid"
    MOCK = "mock"

class EmailRecipient(BaseModel):
    email: EmailStr = Field(..., description="Email do destinatário")
    name: str = Field(..., description="Nome do destinatário")

class EmailContent(BaseModel):
    subject: str = Field(..., description="Assunto do email")
    html_content: str = Field(..., description="Conteúdo HTML do email")
    text_content: Optional[str] = Field(None, description="Conteúdo em texto puro (fallback)")

class EmailRequest(BaseModel):
    to: List[EmailRecipient] = Field(..., description="Lista de destinatários")
    content: EmailContent = Field(..., description="Conteúdo do email")
    provider: Optional[EmailProviderEnum] = Field(None, description="Provedor específico (opcional)")

class EmailResponse(BaseModel):
    success: bool = Field(..., description="Indica se o email foi enviado com sucesso")
    message: str = Field(..., description="Mensagem de status")
    provider_used: Optional[str] = Field(None, description="Provedor utilizado")
    email_id: Optional[str] = Field(None, description="ID do email no provedor (se disponível)")

class EmailConfig(BaseModel):
    api_key: str = Field(..., description="API Key do provedor")
    sender_email: str = Field(..., description="Email do remetente")
    sender_name: str = Field("Sistema VendaMais", description="Nome do remetente")
    provider: EmailProviderEnum = Field(..., description="Provedor de email")

class AdminEmailStatusResponse(BaseModel):
    service_status: Literal['operacional', 'erro', 'desconhecido']
    last_test_result: Optional[Literal['sucesso', 'falha']] = None
    last_test_timestamp: Optional[datetime] = None
    provider_name: str

class TestEmailRequest(BaseModel):
    recipient_email: EmailStr

class PasswordResetRequest(BaseModel):
    email: EmailStr

class PasswordResetResponse(BaseModel):
    success: bool
    message: str
    temporary_password: Optional[str] = None

class EmailTemplateType(str, Enum):
    SYSTEM = "SYSTEM"
    CAMPAIGN = "CAMPAIGN"

class EmailTemplateBase(BaseModel):
    chave: str
    nome: str
    assunto: str
    html_content: str
    text_content: Optional[str] = None
    variaveis_disponiveis: List[str] = []
    tipo: EmailTemplateType
    ativo: bool = True

class EmailTemplateCreate(EmailTemplateBase):
    pass

class EmailTemplateUpdate(BaseModel):
    nome: Optional[str] = None
    assunto: Optional[str] = None
    html_content: Optional[str] = None
    text_content: Optional[str] = None
    variaveis_disponiveis: Optional[List[str]] = None
    ativo: Optional[bool] = None

class EmailTemplateResponse(EmailTemplateBase):
    id: int
    created_at: datetime
    updated_at: datetime

    class Config:
        from_attributes = True

# ========== SCHEMAS PARA TEMPLATES DE E-MAIL (CONSOLIDADO) ==========

class EmailTemplateType(str, Enum):
    SYSTEM = "SYSTEM"
    CAMPAIGN = "CAMPAIGN"

class EmailTemplateBase(BaseModel):
    chave: str = Field(..., description="Chave única para identificação programática (ex: WELCOME_USER).")
    nome: str = Field(..., description="Nome amigável para o painel.")
    assunto: str = Field(..., description="Assunto padrão do e-mail.")
    html_content: str = Field(..., description="Conteúdo HTML do template.")
    text_content: Optional[str] = Field(None, description="Conteúdo em texto puro (fallback).")
    variaveis_disponiveis: List[str] = Field([], description="Lista de strings de variáveis que o template aceita.")
    tipo: EmailTemplateType = Field(..., description="Tipo de template: SYSTEM (essencial) ou CAMPAIGN (marketing).")
    ativo: bool = Field(True, description="Status de ativação.")

class EmailTemplateCreate(EmailTemplateBase):
    pass

class EmailTemplateUpdate(BaseModel):
    nome: Optional[str] = None
    assunto: Optional[str] = None
    html_content: Optional[str] = None
    text_content: Optional[str] = None
    variaveis_disponiveis: Optional[List[str]] = None
    ativo: Optional[bool] = None

class EmailTemplateResponse(EmailTemplateBase):
    id: int
    created_at: datetime
    updated_at: datetime

    class Config:
        from_attributes = True