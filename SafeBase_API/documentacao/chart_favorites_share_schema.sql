-- Chart favorites share (SQL Server)

IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[chart_favorite_shares]') AND type IN (N'U'))
BEGIN
    CREATE TABLE [dbo].[chart_favorite_shares] (
        [id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [favorite_id] INT NOT NULL,
        [share_token] NVARCHAR(128) NOT NULL UNIQUE,
        [expires_at] DATETIME2 NULL,
        [criado_em] DATETIME2 NOT NULL CONSTRAINT DF_chart_favorite_shares_criado_em DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT FK_chart_favorite_shares_favorite FOREIGN KEY ([favorite_id]) REFERENCES [dbo].[chart_favorites]([id]) ON DELETE CASCADE
    );
END
