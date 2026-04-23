-- Create database if it does not exist
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'SafeBaseAPI')
BEGIN
    CREATE DATABASE [SafeBaseAPI];
END;
GO

USE [SafeBaseAPI];
GO

-- Users table for JWT authentication
IF OBJECT_ID('dbo.users', 'U') IS NULL
BEGIN
CREATE TABLE dbo.users (
    id INT IDENTITY(1,1) PRIMARY KEY,
    username NVARCHAR(128) NOT NULL UNIQUE,
    email NVARCHAR(256) NOT NULL UNIQUE,
    full_name NVARCHAR(256) NULL,
    hashed_password NVARCHAR(512) NOT NULL,
    is_active BIT NOT NULL DEFAULT(1),
    created_at DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);
END;
GO

-- API keys table for service authentication
IF OBJECT_ID('dbo.api_keys', 'U') IS NULL
BEGIN
CREATE TABLE dbo.api_keys (
    id INT IDENTITY(1,1) PRIMARY KEY,
    name NVARCHAR(128) NOT NULL,
    [key] NVARCHAR(512) NOT NULL UNIQUE,
    scopes NVARCHAR(MAX) NULL,
    is_active BIT NOT NULL DEFAULT(1),
    created_at DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);
END;
GO

-- Agents known to the system
IF OBJECT_ID('dbo.agents', 'U') IS NULL
BEGIN
CREATE TABLE dbo.agents (
    id INT IDENTITY(1,1) PRIMARY KEY,
    agent_id NVARCHAR(128) NOT NULL UNIQUE,
    nome_host NVARCHAR(256) NULL,
    nome_instancia NVARCHAR(256) NULL,
    versao NVARCHAR(128) NULL,
    ultimo_heartbeat DATETIME2 NULL,
    status NVARCHAR(64) NULL,
    metadados NVARCHAR(MAX) NULL,
    created_at DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);
END;
GO

-- Payloads enviados pelos agentes
IF OBJECT_ID('dbo.agent_payloads', 'U') IS NULL
BEGIN
CREATE TABLE dbo.agent_payloads (
    id INT IDENTITY(1,1) PRIMARY KEY,
    agent_id NVARCHAR(128) NOT NULL,
    tipo_payload NVARCHAR(128) NOT NULL,
    dados_payload NVARCHAR(MAX) NULL,
    coletado_em DATETIME2 NULL,
    recebido_em DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_AgentPayloads_Agent FOREIGN KEY (agent_id) REFERENCES dbo.agents(agent_id)
);
END;
GO

-- Jobs coletados pelo agente
IF OBJECT_ID('dbo.dados_jobs_safe', 'U') IS NULL
BEGIN
CREATE TABLE dbo.dados_jobs_safe (
    id INT IDENTITY(1,1) PRIMARY KEY,
    payload_agente_id INT NOT NULL,
    nome_job NVARCHAR(256) NULL,
    status NVARCHAR(64) NULL,
    iniciado_em DATETIME2 NULL,
    finalizado_em DATETIME2 NULL,
    duracao_ms INT NULL,
    mensagem_erro NVARCHAR(MAX) NULL,
    CONSTRAINT FK_DadosJobsSafe_Payload FOREIGN KEY (payload_agente_id) REFERENCES dbo.agent_payloads(id)
);
END;
GO

-- Esperas coletadas pelo agente
IF OBJECT_ID('dbo.dados_esperas_safe', 'U') IS NULL
BEGIN
CREATE TABLE dbo.dados_esperas_safe (
    id INT IDENTITY(1,1) PRIMARY KEY,
    payload_agente_id INT NOT NULL,
    tipo_espera NVARCHAR(128) NULL,
    tempo_espera_ms INT NULL,
    tempo_recurso_ms INT NULL,
    tempo_sinal_ms INT NULL,
    contagem_tarefas INT NULL,
    CONSTRAINT FK_DadosEsperasSafe_Payload FOREIGN KEY (payload_agente_id) REFERENCES dbo.agent_payloads(id)
);
END;
GO

-- Alertas coletados pelo agente
IF OBJECT_ID('dbo.alertas_safe', 'U') IS NULL
BEGIN
CREATE TABLE dbo.alertas_safe (
    id INT IDENTITY(1,1) PRIMARY KEY,
    payload_agente_id INT NOT NULL,
    tipo_alerta NVARCHAR(128) NULL,
    gravidade NVARCHAR(64) NULL,
    mensagem NVARCHAR(MAX) NULL,
    criado_em DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_AlertasSafe_Payload FOREIGN KEY (payload_agente_id) REFERENCES dbo.agent_payloads(id)
);
END;
GO

-- Provedores de IA disponíveis
IF OBJECT_ID('dbo.provedores_ia', 'U') IS NULL
BEGIN
CREATE TABLE dbo.provedores_ia (
    id INT IDENTITY(1,1) PRIMARY KEY,
    nome NVARCHAR(128) NOT NULL,
    descricao NVARCHAR(MAX) NULL,
    prioridade INT NOT NULL DEFAULT 0,
    configuracao NVARCHAR(MAX) NULL,
    ativo BIT NOT NULL DEFAULT(1),
    criado_em DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    atualizado_em DATETIME2 NULL
);
END;
GO

-- Chaves de IA armazenadas de forma segura
IF OBJECT_ID('dbo.chaves_ia', 'U') IS NULL
BEGIN
CREATE TABLE dbo.chaves_ia (
    id INT IDENTITY(1,1) PRIMARY KEY,
    provedor_id INT NOT NULL,
    hash_chave NVARCHAR(256) NULL,
    chave_criptografada NVARCHAR(MAX) NULL,
    descricao NVARCHAR(256) NULL,
    ativo BIT NOT NULL DEFAULT(1),
    metadados NVARCHAR(MAX) NULL,
    criado_em DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    atualizado_em DATETIME2 NULL,
    CONSTRAINT FK_ChavesIA_Provedor FOREIGN KEY (provedor_id) REFERENCES dbo.provedores_ia(id)
);
END;
GO

-- Insights gerados pelo módulo de IA
IF OBJECT_ID('dbo.insights_ia', 'U') IS NULL
BEGIN
CREATE TABLE dbo.insights_ia (
    id INT IDENTITY(1,1) PRIMARY KEY,
    fonte_dados NVARCHAR(128) NULL,
    tipo_insight NVARCHAR(128) NULL,
    resumo NVARCHAR(MAX) NULL,
    detalhes NVARCHAR(MAX) NULL,
    confianca DECIMAL(5,4) NULL,
    gerado_em DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    objetos_relacionados NVARCHAR(MAX) NULL
);
END;
GO

-- API logs para auditoria
IF OBJECT_ID('dbo.api_logs', 'U') IS NULL
BEGIN
CREATE TABLE dbo.api_logs (
    id INT IDENTITY(1,1) PRIMARY KEY,
    created_at DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    path NVARCHAR(512) NULL,
    method NVARCHAR(32) NULL,
    status_code INT NULL,
    username NVARCHAR(128) NULL,
    auth_type NVARCHAR(32) NULL,
    client_ip NVARCHAR(64) NULL,
    request_body NVARCHAR(MAX) NULL,
    response_body NVARCHAR(MAX) NULL,
    details NVARCHAR(MAX) NULL
);
END;
GO
