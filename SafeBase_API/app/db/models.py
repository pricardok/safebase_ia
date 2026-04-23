from sqlalchemy import Boolean, Column, DateTime, ForeignKey, Integer, String, Text
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


class ApiKey(Base):
    __tablename__ = "api_keys"

    id = Column(Integer, primary_key=True, index=True)
    name = Column(String(128), nullable=False)
    key = Column(String(256), unique=True, nullable=False)
    scopes = Column(Text, nullable=True)
    is_active = Column(Boolean, default=True)
    created_at = Column(DateTime, default=datetime.utcnow)


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
