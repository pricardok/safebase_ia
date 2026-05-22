-- Refresh tokens (SQL Server)

IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[auth_refresh_tokens]') AND type IN (N'U'))
BEGIN
    CREATE TABLE [dbo].[auth_refresh_tokens] (
        [id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [user_id] INT NOT NULL,
        [token_hash] NVARCHAR(128) NOT NULL UNIQUE,
        [expires_at] DATETIME2 NOT NULL,
        [revoked_at] DATETIME2 NULL,
        [created_at] DATETIME2 NOT NULL CONSTRAINT DF_auth_refresh_tokens_created_at DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT FK_auth_refresh_tokens_users FOREIGN KEY ([user_id]) REFERENCES [dbo].[users]([id]) ON DELETE CASCADE
    );
END
