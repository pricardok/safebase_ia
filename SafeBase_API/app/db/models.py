from sqlalchemy import Boolean, Column, DateTime, ForeignKey, Integer, Numeric, String, Text, UniqueConstraint
from sqlalchemy.orm import relationship
from datetime import datetime

from app.db.base import Base


class User(Base):
    __tablename__ = "users"

    id = Column(Integer, primary_key=True, index=True)
    username = Column(String(128), unique=True, index=True, nullable=False)
    full_name = Column(String(256), nullable=True)
    email = Column(String(256), unique=True, index=True, nullable=False)
    hashed_password = Column(String(256), nullable=False)
    is_active = Column(Boolean, default=True)
    created_at = Column(DateTime, default=datetime.utcnow)

    roles = relationship("UserRole", back_populates="user", cascade="all, delete-orphan")


class ApiKey(Base):
    __tablename__ = "api_keys"

    id = Column(Integer, primary_key=True, index=True)
    name = Column(String(128), nullable=False)
    key = Column(String(256), unique=True, nullable=False)
    scopes = Column(Text, nullable=True)
    is_active = Column(Boolean, default=True)
    created_at = Column(DateTime, default=datetime.utcnow)


class Role(Base):
    __tablename__ = "roles"

    id = Column(Integer, primary_key=True, index=True)
    name = Column(String(128), unique=True, nullable=False, index=True)
    description = Column(String(512), nullable=True)
    is_active = Column(Boolean, default=True)
    created_at = Column(DateTime, default=datetime.utcnow)

    users = relationship("UserRole", back_populates="role", cascade="all, delete-orphan")
    permissions = relationship("RolePermission", back_populates="role", cascade="all, delete-orphan")


class Permission(Base):
    __tablename__ = "permissions"

    id = Column(Integer, primary_key=True, index=True)
    code = Column(String(128), unique=True, nullable=False, index=True)
    description = Column(String(512), nullable=True)
    created_at = Column(DateTime, default=datetime.utcnow)

    roles = relationship("RolePermission", back_populates="permission", cascade="all, delete-orphan")


class UserRole(Base):
    __tablename__ = "user_roles"
    __table_args__ = (UniqueConstraint("user_id", "role_id", name="uq_user_roles"),)

    id = Column(Integer, primary_key=True, index=True)
    user_id = Column(Integer, ForeignKey("users.id"), nullable=False)
    role_id = Column(Integer, ForeignKey("roles.id"), nullable=False)
    assigned_at = Column(DateTime, default=datetime.utcnow)

    user = relationship("User", back_populates="roles")
    role = relationship("Role", back_populates="users")


class RolePermission(Base):
    __tablename__ = "role_permissions"
    __table_args__ = (UniqueConstraint("role_id", "permission_id", name="uq_role_permissions"),)

    id = Column(Integer, primary_key=True, index=True)
    role_id = Column(Integer, ForeignKey("roles.id"), nullable=False)
    permission_id = Column(Integer, ForeignKey("permissions.id"), nullable=False)
    granted_at = Column(DateTime, default=datetime.utcnow)

    role = relationship("Role", back_populates="permissions")
    permission = relationship("Permission", back_populates="roles")


class Agent(Base):
    __tablename__ = "agents"

    id = Column(Integer, primary_key=True, index=True)
    agent_id = Column(String(128), unique=True, index=True, nullable=False)
    nome_host = Column(String(256), nullable=True)
    nome_instancia = Column(String(256), nullable=True)
    versao = Column(String(128), nullable=True)
    ultimo_heartbeat = Column(DateTime, nullable=True)
    status = Column(String(64), nullable=True)
    metadados = Column(Text, nullable=True)
    created_at = Column(DateTime, default=datetime.utcnow)


class AgentPayload(Base):
    __tablename__ = "agent_payloads"

    id = Column(Integer, primary_key=True, index=True)
    agent_id = Column(String(128), ForeignKey("agents.agent_id"), nullable=False)
    tipo_payload = Column(String(128), nullable=False)
    dados_payload = Column(Text, nullable=True)
    metadata_json = Column(Text, nullable=True)
    coletado_em = Column(DateTime, nullable=True)
    recebido_em = Column(DateTime, default=datetime.utcnow)
    payload_normalizado = Column(Boolean, default=False)
    normalizado_em = Column(DateTime, nullable=True)
    categoria_id = Column(Integer, ForeignKey("payload_categorias.id"), nullable=True)


class PayloadCategoria(Base):
    __tablename__ = "payload_categorias"

    id = Column(Integer, primary_key=True, index=True)
    nome = Column(String(128), unique=True, nullable=False)
    descricao = Column(String(512), nullable=True)
    ativo = Column(Boolean, default=True)


class AlertaSafe(Base):
    __tablename__ = "alertas_safe"

    id = Column(Integer, primary_key=True, index=True)
    payload_agente_id = Column(Integer, ForeignKey("agent_payloads.id"), nullable=False)
    agent_id = Column(String(128), nullable=True)
    tipo_alerta = Column(String(256), nullable=True)
    gravidade = Column(String(128), nullable=True)
    mensagem = Column(Text, nullable=True)
    criado_em = Column(DateTime, default=datetime.utcnow)


class DadosEsperasSafe(Base):
    __tablename__ = "dados_esperas_safe"

    id = Column(Integer, primary_key=True, index=True)
    payload_agente_id = Column(Integer, ForeignKey("agent_payloads.id"), nullable=False)
    agent_id = Column(String(128), nullable=True)
    tipo_espera = Column(String(256), nullable=True)
    tempo_espera_ms = Column(Integer, nullable=True)
    tempo_recurso_ms = Column(Integer, nullable=True)
    tempo_sinal_ms = Column(Integer, nullable=True)
    contagem_tarefas = Column(Integer, nullable=True)


class DadosJobsSafe(Base):
    __tablename__ = "dados_jobs_safe"

    id = Column(Integer, primary_key=True, index=True)
    payload_agente_id = Column(Integer, ForeignKey("agent_payloads.id"), nullable=False)
    agent_id = Column(String(128), nullable=True)
    nome_job = Column(String(512), nullable=True)
    status = Column(String(128), nullable=True)
    iniciado_em = Column(DateTime, nullable=True)
    finalizado_em = Column(DateTime, nullable=True)
    duracao_ms = Column(Integer, nullable=True)
    mensagem_erro = Column(Text, nullable=True)


class DadosLoginsSafe(Base):
    __tablename__ = "dados_logins_safe"

    id = Column(Integer, primary_key=True, index=True)
    payload_agente_id = Column(Integer, ForeignKey("agent_payloads.id"), nullable=False)
    agent_id = Column(String(128), nullable=True)
    nome_servidor = Column(String(256), nullable=True)
    nome_instancia = Column(String(256), nullable=True)
    servidor_completo = Column(String(256), nullable=True)
    login_name = Column(String(256), nullable=True)
    login_type = Column(String(128), nullable=True)
    is_disabled = Column(Boolean, nullable=True)
    create_date = Column(DateTime, nullable=True)
    modify_date = Column(DateTime, nullable=True)
    server_roles = Column(Text, nullable=True)
    server_permissions = Column(Text, nullable=True)


class DadosBackupSafe(Base):
    __tablename__ = "dados_backup_safe"

    id = Column(Integer, primary_key=True, index=True)
    payload_agente_id = Column(Integer, ForeignKey("agent_payloads.id"), nullable=False)
    agent_id = Column(String(128), nullable=True)
    nome_servidor = Column(String(256), nullable=True)
    nome_instancia = Column(String(256), nullable=True)
    servidor_completo = Column(String(256), nullable=True)
    servidor = Column(String(256), nullable=True)
    banco = Column(String(256), nullable=True)
    full_backup_status = Column(String(64), nullable=True)
    diff_backup_status = Column(String(64), nullable=True)
    log_backup_status = Column(String(64), nullable=True)
    tipo_recuperacao = Column(String(64), nullable=True)
    ultimo_full = Column(Integer, nullable=True)
    data_full = Column(DateTime, nullable=True)
    tamanho_full_mb = Column(String(64), nullable=True)
    ultimo_diff = Column(Integer, nullable=True)
    data_diff = Column(DateTime, nullable=True)
    ultimo_full_diff = Column(Integer, nullable=True)
    tamanho_diff_mb = Column(String(64), nullable=True)
    ultimo_log_min = Column(Integer, nullable=True)
    data_log = Column(DateTime, nullable=True)
    tamanho_log_mb = Column(String(64), nullable=True)
    full_backup_alarm = Column(Integer, nullable=True)
    diff_backup_alarm = Column(Integer, nullable=True)
    log_backup_alarm = Column(Integer, nullable=True)


class InsightIA(Base):
    __tablename__ = "insights_ia"

    id = Column(Integer, primary_key=True, index=True)
    fonte_dados = Column(String(256), nullable=True)
    tipo_insight = Column(String(256), nullable=True)
    resumo = Column(Text, nullable=True)
    detalhes = Column(Text, nullable=True)
    confianca = Column(Numeric(5, 4), nullable=True)
    gerado_em = Column(DateTime, default=datetime.utcnow)
    objetos_relacionados = Column(Text, nullable=True)


class ProvedorIA(Base):
    __tablename__ = "provedores_ia"

    id = Column(Integer, primary_key=True, index=True)
    nome = Column(String(256), nullable=False)
    descricao = Column(Text, nullable=True)
    prioridade = Column(Integer, nullable=False)
    configuracao = Column(Text, nullable=True)
    ativo = Column(Boolean, default=True)
    criado_em = Column(DateTime, default=datetime.utcnow)
    atualizado_em = Column(DateTime, nullable=True)


class ChaveIA(Base):
    __tablename__ = "chaves_ia"

    id = Column(Integer, primary_key=True, index=True)
    provedor_id = Column(Integer, ForeignKey("provedores_ia.id"), nullable=False)
    hash_chave = Column(String(512), nullable=True)
    chave_criptografada = Column(Text, nullable=True)
    descricao = Column(String(512), nullable=True)
    prioridade = Column(Integer, default=1, nullable=False)
    ativo = Column(Boolean, default=True)
    falhas_consecutivas = Column(Integer, default=0, nullable=False)
    total_requisicoes = Column(Integer, default=0, nullable=False)
    total_erros = Column(Integer, default=0, nullable=False)
    ultima_sucesso = Column(DateTime, nullable=True)
    ultima_falha = Column(DateTime, nullable=True)
    metadados = Column(Text, nullable=True)
    criado_em = Column(DateTime, default=datetime.utcnow)
    atualizado_em = Column(DateTime, nullable=True)


class ChatConversation(Base):
    __tablename__ = "chat_conversas"

    id = Column(Integer, primary_key=True, index=True)
    titulo = Column(String(256), nullable=True)
    usuario_id = Column(String(128), index=True, nullable=False)
    categoria_id = Column(Integer, ForeignKey("chat_categorias.id"), nullable=True)
    pinned = Column(Boolean, default=False)
    temperature = Column(Numeric(4, 2), default=0.7)
    max_tokens = Column(Integer, default=2000)
    criado_em = Column(DateTime, default=datetime.utcnow)
    atualizado_em = Column(DateTime, default=datetime.utcnow, onupdate=datetime.utcnow)


class ChatMessage(Base):
    __tablename__ = "chat_mensagens"

    id = Column(Integer, primary_key=True, index=True)
    conversa_id = Column(Integer, ForeignKey("chat_conversas.id"), nullable=False, index=True)
    papel = Column(String(16), nullable=False)
    conteudo = Column(Text, nullable=False)
    metadados = Column(Text, nullable=True)
    criado_em = Column(DateTime, default=datetime.utcnow)


class ChatContext(Base):
    __tablename__ = "chat_contextos"

    id = Column(Integer, primary_key=True, index=True)
    conversa_id = Column(Integer, ForeignKey("chat_conversas.id"), nullable=False, index=True)
    tipo = Column(String(64), nullable=False)
    conteudo = Column(Text, nullable=False)
    criado_em = Column(DateTime, default=datetime.utcnow)


class ChatCategoria(Base):
    __tablename__ = "chat_categorias"

    id = Column(Integer, primary_key=True, index=True)
    codigo = Column(String(128), unique=True, nullable=False, index=True)
    nome = Column(String(256), nullable=False)
    descricao = Column(String(512), nullable=True)
    ativo = Column(Boolean, default=True)
    criado_em = Column(DateTime, default=datetime.utcnow)


class UserCategoria(Base):
    __tablename__ = "user_categorias"
    __table_args__ = (UniqueConstraint("user_id", "categoria_id", name="uq_user_categorias"),)

    id = Column(Integer, primary_key=True, index=True)
    user_id = Column(Integer, ForeignKey("users.id"), nullable=False)
    categoria_id = Column(Integer, ForeignKey("chat_categorias.id"), nullable=False)
    assigned_at = Column(DateTime, default=datetime.utcnow)


class UserSettings(Base):
    __tablename__ = "user_settings"

    id = Column(Integer, primary_key=True, index=True)
    user_id = Column(Integer, ForeignKey("users.id"), nullable=False, unique=True)
    default_categoria_codigo = Column(String(128), nullable=True)
    default_mode = Column(String(32), nullable=True)
    default_temperature = Column(Numeric(4, 2), nullable=True)
    default_max_tokens = Column(Integer, nullable=True)
    criado_em = Column(DateTime, default=datetime.utcnow)
    atualizado_em = Column(DateTime, default=datetime.utcnow, onupdate=datetime.utcnow)


class ChartShare(Base):
    __tablename__ = "chart_shares"

    id = Column(Integer, primary_key=True, index=True)
    user_id = Column(Integer, ForeignKey("users.id"), nullable=False)
    categoria_id = Column(Integer, ForeignKey("chat_categorias.id"), nullable=True)
    titulo = Column(String(256), nullable=True)
    chart_payload = Column(Text, nullable=False)
    share_token = Column(String(128), unique=True, nullable=False, index=True)
    expires_at = Column(DateTime, nullable=True)
    criado_em = Column(DateTime, default=datetime.utcnow)


class ChartFavorite(Base):
    __tablename__ = "chart_favorites"

    id = Column(Integer, primary_key=True, index=True)
    user_id = Column(Integer, ForeignKey("users.id"), nullable=False)
    categoria_id = Column(Integer, ForeignKey("chat_categorias.id"), nullable=True)
    titulo = Column(String(256), nullable=True)
    chart_payload = Column(Text, nullable=False)
    criado_em = Column(DateTime, default=datetime.utcnow)


class ChartFavoriteShare(Base):
    __tablename__ = "chart_favorite_shares"

    id = Column(Integer, primary_key=True, index=True)
    favorite_id = Column(Integer, ForeignKey("chart_favorites.id"), nullable=False)
    share_token = Column(String(128), unique=True, nullable=False, index=True)
    expires_at = Column(DateTime, nullable=True)
    criado_em = Column(DateTime, default=datetime.utcnow)


class ExternalDataSource(Base):
    __tablename__ = "external_data_sources"

    id = Column(Integer, primary_key=True, index=True)
    nome = Column(String(128), nullable=False)
    tipo = Column(String(64), nullable=False)
    configuracao = Column(Text, nullable=True)
    ativo = Column(Boolean, default=True)
    criado_em = Column(DateTime, default=datetime.utcnow)
    atualizado_em = Column(DateTime, default=datetime.utcnow, onupdate=datetime.utcnow)


class KnowledgeMd(Base):
    __tablename__ = "conhecimento_md"

    id = Column(Integer, primary_key=True, index=True)
    titulo = Column(String(256), nullable=False)
    caminho_arquivo = Column(String(512), nullable=False)
    hash_conteudo = Column(String(128), nullable=False)
    origem = Column(String(32), nullable=False, default="manual")
    criado_em = Column(DateTime, default=datetime.utcnow)
    atualizado_em = Column(DateTime, default=datetime.utcnow, onupdate=datetime.utcnow)


class RefreshToken(Base):
    __tablename__ = "auth_refresh_tokens"

    id = Column(Integer, primary_key=True, index=True)
    user_id = Column(Integer, ForeignKey("users.id"), nullable=False, index=True)
    token_hash = Column(String(128), unique=True, nullable=False, index=True)
    expires_at = Column(DateTime, nullable=False)
    revoked_at = Column(DateTime, nullable=True)
    created_at = Column(DateTime, default=datetime.utcnow)
