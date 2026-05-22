-- User settings (SQL Server)

IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[user_settings]') AND type IN (N'U'))
BEGIN
    CREATE TABLE [dbo].[user_settings] (
        [id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [user_id] INT NOT NULL UNIQUE,
        [default_categoria_codigo] NVARCHAR(128) NULL,
        [default_mode] NVARCHAR(32) NULL,
        [default_temperature] DECIMAL(4,2) NULL,
        [default_max_tokens] INT NULL,
        [criado_em] DATETIME2 NOT NULL CONSTRAINT DF_user_settings_criado_em DEFAULT (SYSUTCDATETIME()),
        [atualizado_em] DATETIME2 NOT NULL CONSTRAINT DF_user_settings_atualizado_em DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT FK_user_settings_users FOREIGN KEY ([user_id]) REFERENCES [dbo].[users]([id]) ON DELETE CASCADE
    );
END
