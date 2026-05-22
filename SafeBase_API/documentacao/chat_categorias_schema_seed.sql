-- Chat categorias + vinculos de usuários (SQL Server)

IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[chat_categorias]') AND type IN (N'U'))
BEGIN
    CREATE TABLE [dbo].[chat_categorias] (
        [id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [codigo] NVARCHAR(128) NOT NULL UNIQUE,
        [nome] NVARCHAR(256) NOT NULL,
        [descricao] NVARCHAR(512) NULL,
        [ativo] BIT NOT NULL CONSTRAINT DF_chat_categorias_ativo DEFAULT (1),
        [criado_em] DATETIME2 NOT NULL CONSTRAINT DF_chat_categorias_criado_em DEFAULT (SYSUTCDATETIME())
    );
END

IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[user_categorias]') AND type IN (N'U'))
BEGIN
    CREATE TABLE [dbo].[user_categorias] (
        [id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [user_id] INT NOT NULL,
        [categoria_id] INT NOT NULL,
        [assigned_at] DATETIME2 NOT NULL CONSTRAINT DF_user_categorias_assigned_at DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT UQ_user_categorias UNIQUE ([user_id], [categoria_id]),
        CONSTRAINT FK_user_categorias_users FOREIGN KEY ([user_id]) REFERENCES [dbo].[users]([id]) ON DELETE CASCADE,
        CONSTRAINT FK_user_categorias_categorias FOREIGN KEY ([categoria_id]) REFERENCES [dbo].[chat_categorias]([id]) ON DELETE CASCADE
    );
END

IF COL_LENGTH('dbo.chat_conversas', 'categoria_id') IS NULL
BEGIN
    ALTER TABLE dbo.chat_conversas
        ADD categoria_id int NULL;

    ALTER TABLE dbo.chat_conversas
        ADD CONSTRAINT FK_chat_conversas_categorias
        FOREIGN KEY (categoria_id) REFERENCES dbo.chat_categorias(id);
END

-- Seeds iniciais
IF NOT EXISTS (SELECT 1 FROM dbo.chat_categorias WHERE codigo = 'emprestimo_consignado')
    INSERT INTO dbo.chat_categorias (codigo, nome, descricao, ativo)
    VALUES ('emprestimo_consignado', 'Empréstimo Consignado', 'Crédito com desconto em folha para servidores, aposentados e pensionistas.', 1);

IF NOT EXISTS (SELECT 1 FROM dbo.chat_categorias WHERE codigo = 'saque_aniversario_fgts')
    INSERT INTO dbo.chat_categorias (codigo, nome, descricao, ativo)
    VALUES ('saque_aniversario_fgts', 'Saque-Aniversário FGTS', 'Antecipação do saque aniversário do FGTS.', 1);

IF NOT EXISTS (SELECT 1 FROM dbo.chat_categorias WHERE codigo = 'credito_trabalhador_clt')
    INSERT INTO dbo.chat_categorias (codigo, nome, descricao, ativo)
    VALUES ('credito_trabalhador_clt', 'Crédito do Trabalhador (Consignado CLT)', 'Empréstimo para funcionários de empresas conveniadas.', 1);

IF NOT EXISTS (SELECT 1 FROM dbo.chat_categorias WHERE codigo = 'loas_bpc')
    INSERT INTO dbo.chat_categorias (codigo, nome, descricao, ativo)
    VALUES ('loas_bpc', 'LOAS/BPC', 'Consignados voltados ao amparo assistencial.', 1);

IF NOT EXISTS (SELECT 1 FROM dbo.chat_categorias WHERE codigo = 'seguros')
    INSERT INTO dbo.chat_categorias (codigo, nome, descricao, ativo)
    VALUES ('seguros', 'Seguros', 'Coberturas com assistências e benefícios.', 1);

IF NOT EXISTS (SELECT 1 FROM dbo.chat_categorias WHERE codigo = 'dba')
    INSERT INTO dbo.chat_categorias (codigo, nome, descricao, ativo)
    VALUES ('dba', 'DBA', 'Administração de banco de dados.', 1);

IF NOT EXISTS (SELECT 1 FROM dbo.chat_categorias WHERE codigo = 'bi')
    INSERT INTO dbo.chat_categorias (codigo, nome, descricao, ativo)
    VALUES ('bi', 'BI', 'Área de Business Intelligence.', 1);
