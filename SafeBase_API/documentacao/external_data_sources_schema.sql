-- External data sources (SQL Server)

IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[external_data_sources]') AND type IN (N'U'))
BEGIN
    CREATE TABLE [dbo].[external_data_sources] (
        [id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [nome] NVARCHAR(128) NOT NULL,
        [tipo] NVARCHAR(64) NOT NULL,
        [configuracao] NVARCHAR(MAX) NULL,
        [ativo] BIT NOT NULL CONSTRAINT DF_external_data_sources_ativo DEFAULT (1),
        [criado_em] DATETIME2 NOT NULL CONSTRAINT DF_external_data_sources_criado_em DEFAULT (SYSUTCDATETIME()),
        [atualizado_em] DATETIME2 NOT NULL CONSTRAINT DF_external_data_sources_atualizado_em DEFAULT (SYSUTCDATETIME())
    );
END
