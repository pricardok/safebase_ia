-- SafeBase RBAC schema + seed (SQL Server)
-- Cria tabelas de roles, permissions, user_roles e role_permissions

IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[roles]') AND type IN (N'U'))
BEGIN
    CREATE TABLE [dbo].[roles] (
        [id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [name] NVARCHAR(128) NOT NULL UNIQUE,
        [description] NVARCHAR(512) NULL,
        [is_active] BIT NOT NULL CONSTRAINT DF_roles_is_active DEFAULT (1),
        [created_at] DATETIME2 NOT NULL CONSTRAINT DF_roles_created_at DEFAULT (SYSUTCDATETIME())
    );
END

IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[permissions]') AND type IN (N'U'))
BEGIN
    CREATE TABLE [dbo].[permissions] (
        [id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [code] NVARCHAR(128) NOT NULL UNIQUE,
        [description] NVARCHAR(512) NULL,
        [created_at] DATETIME2 NOT NULL CONSTRAINT DF_permissions_created_at DEFAULT (SYSUTCDATETIME())
    );
END

IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[user_roles]') AND type IN (N'U'))
BEGIN
    CREATE TABLE [dbo].[user_roles] (
        [id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [user_id] INT NOT NULL,
        [role_id] INT NOT NULL,
        [assigned_at] DATETIME2 NOT NULL CONSTRAINT DF_user_roles_assigned_at DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT UQ_user_roles UNIQUE ([user_id], [role_id]),
        CONSTRAINT FK_user_roles_users FOREIGN KEY ([user_id]) REFERENCES [dbo].[users]([id]) ON DELETE CASCADE,
        CONSTRAINT FK_user_roles_roles FOREIGN KEY ([role_id]) REFERENCES [dbo].[roles]([id]) ON DELETE CASCADE
    );
END

IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[role_permissions]') AND type IN (N'U'))
BEGIN
    CREATE TABLE [dbo].[role_permissions] (
        [id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [role_id] INT NOT NULL,
        [permission_id] INT NOT NULL,
        [granted_at] DATETIME2 NOT NULL CONSTRAINT DF_role_permissions_granted_at DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT UQ_role_permissions UNIQUE ([role_id], [permission_id]),
        CONSTRAINT FK_role_permissions_roles FOREIGN KEY ([role_id]) REFERENCES [dbo].[roles]([id]) ON DELETE CASCADE,
        CONSTRAINT FK_role_permissions_permissions FOREIGN KEY ([permission_id]) REFERENCES [dbo].[permissions]([id]) ON DELETE CASCADE
    );
END

-- Seed inicial (permissões + role admin)
IF NOT EXISTS (SELECT 1 FROM [dbo].[permissions] WHERE [code] = 'rbac.read')
    INSERT INTO [dbo].[permissions] ([code], [description]) VALUES ('rbac.read', 'Leitura da configuração de RBAC');

IF NOT EXISTS (SELECT 1 FROM [dbo].[permissions] WHERE [code] = 'rbac.manage')
    INSERT INTO [dbo].[permissions] ([code], [description]) VALUES ('rbac.manage', 'Gerenciamento completo de RBAC');

IF NOT EXISTS (SELECT 1 FROM [dbo].[permissions] WHERE [code] = 'users.read')
    INSERT INTO [dbo].[permissions] ([code], [description]) VALUES ('users.read', 'Leitura de usuários');

IF NOT EXISTS (SELECT 1 FROM [dbo].[permissions] WHERE [code] = 'users.manage')
    INSERT INTO [dbo].[permissions] ([code], [description]) VALUES ('users.manage', 'Gerenciamento completo de usuários');

IF NOT EXISTS (SELECT 1 FROM [dbo].[permissions] WHERE [code] = 'categories.read')
    INSERT INTO [dbo].[permissions] ([code], [description]) VALUES ('categories.read', 'Leitura de categorias');

IF NOT EXISTS (SELECT 1 FROM [dbo].[permissions] WHERE [code] = 'categories.manage')
    INSERT INTO [dbo].[permissions] ([code], [description]) VALUES ('categories.manage', 'Gerenciamento completo de categorias');

IF NOT EXISTS (SELECT 1 FROM [dbo].[permissions] WHERE [code] = 'external_sources.read')
    INSERT INTO [dbo].[permissions] ([code], [description]) VALUES ('external_sources.read', 'Leitura de fontes externas');

IF NOT EXISTS (SELECT 1 FROM [dbo].[permissions] WHERE [code] = 'external_sources.manage')
    INSERT INTO [dbo].[permissions] ([code], [description]) VALUES ('external_sources.manage', 'Gerenciamento completo de fontes externas');

IF NOT EXISTS (SELECT 1 FROM [dbo].[roles] WHERE [name] = 'admin')
    INSERT INTO [dbo].[roles] ([name], [description], [is_active]) VALUES ('admin', 'Administrador do sistema', 1);

DECLARE @adminRoleId INT;
SELECT @adminRoleId = [id] FROM [dbo].[roles] WHERE [name] = 'admin';

INSERT INTO [dbo].[role_permissions] ([role_id], [permission_id])
SELECT @adminRoleId, p.[id]
FROM [dbo].[permissions] p
WHERE p.[code] IN ('rbac.read', 'rbac.manage', 'users.read', 'users.manage', 'categories.read', 'categories.manage', 'external_sources.read', 'external_sources.manage')
  AND NOT EXISTS (
      SELECT 1 FROM [dbo].[role_permissions] rp
      WHERE rp.[role_id] = @adminRoleId AND rp.[permission_id] = p.[id]
  );
