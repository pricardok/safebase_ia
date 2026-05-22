-- Chart favorites (SQL Server)

IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[chart_favorites]') AND type IN (N'U'))
BEGIN
    CREATE TABLE [dbo].[chart_favorites] (
        [id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [user_id] INT NOT NULL,
        [categoria_id] INT NULL,
        [titulo] NVARCHAR(256) NULL,
        [chart_payload] NVARCHAR(MAX) NOT NULL,
        [criado_em] DATETIME2 NOT NULL CONSTRAINT DF_chart_favorites_criado_em DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT FK_chart_favorites_users FOREIGN KEY ([user_id]) REFERENCES [dbo].[users]([id]) ON DELETE CASCADE,
        CONSTRAINT FK_chart_favorites_categorias FOREIGN KEY ([categoria_id]) REFERENCES [dbo].[chat_categorias]([id]) ON DELETE SET NULL
    );
END
