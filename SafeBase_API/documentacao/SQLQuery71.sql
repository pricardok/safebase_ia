/*
DECLARE @url nvarchar(4000) = 'http://127.0.0.1:8000/ingest/agent-data';
DECLARE @apiKey nvarchar(200) = 'Hx7z9Q2wR4mP6tY8uVbN3cL1sZ0kF5hE';

SELECT safebase.dbo.fncResolveHttpRequest(
    'POST',
    @url,
    (SELECT [Server],[Job_Name],[Status],[Dt_Execucao],[Run_Duration],[SQL_Message]
     FOR JSON PATH, WITHOUT_ARRAY_WRAPPER),
    '<Headers>
        <Header Name="Content-Type">application/json; charset=utf-8</Header>
        <Header Name="X-API-Key">' + @apiKey + '</Header>
    </Headers>',
    30000,
    1,
    0
)
FROM [SafeBase].[dbo].[CheckJobsFailed]
WHERE [Status] = 'Failed';



DECLARE @url NVARCHAR(4000) = 'http://127.0.0.1:8000/ingest/agent-data';
DECLARE @apiKey NVARCHAR(200) = 'Hx7z9Q2wR4mP6tY8uVbN3cL1sZ0kF5hE';
DECLARE @agentId NVARCHAR(200) = @@SERVERNAME;
DECLARE @payload NVARCHAR(MAX);

SELECT @payload = (
    SELECT
        p.name AS login_name,
        p.type_desc AS login_type,
        p.is_disabled,
        p.create_date,
        p.modify_date,
        COALESCE(
            STUFF((
                SELECT ', ' + r.name
                FROM sys.server_role_members rm
                JOIN sys.server_principals r
                    ON rm.role_principal_id = r.principal_id
                WHERE rm.member_principal_id = p.principal_id
                FOR XML PATH(''), TYPE
            ).value('.', 'nvarchar(max)'), 1, 2, ''),
            '') AS server_roles,
        COALESCE(
            STUFF((
                SELECT ', ' + sp.permission_name
                    + CASE WHEN sp.state_desc = 'GRANT_WITH_GRANT_OPTION' THEN ' (WITH GRANT)' ELSE '' END
                FROM sys.server_permissions sp
                WHERE sp.grantee_principal_id = p.principal_id
                FOR XML PATH(''), TYPE
            ).value('.', 'nvarchar(max)'), 1, 2, ''),
            '') AS server_permissions
    FROM sys.server_principals p
    WHERE p.type_desc IN (
        'SQL_LOGIN',
        'WINDOWS_LOGIN',
        'WINDOWS_GROUP',
        'ASYMMETRIC_KEY_LOGIN',
        'CERTIFICATE_MAPPED_LOGIN'
    )
    ORDER BY p.name
    FOR JSON PATH
);

SELECT safebase.dbo.fncResolveHttpRequest(
    'POST',
    @url,
    N'{
        "agent_id": "' + @agentId + N'",
        "timestamp": "' + CONVERT(NVARCHAR(30), SYSUTCDATETIME(), 127) + N'",
        "payload_type": "InstanceLogins",
        "payload_data": ' + @payload + N',
        "metadata": {"source":"sys.server_principals","category":"server_logins"}
    }',
    N'<Headers>
        <Header Name="Content-Type">application/json; charset=utf-8</Header>
        <Header Name="X-API-Key">' + @apiKey + N'</Header>
    </Headers>',
    30000,
    1,
    0
);
*/

DECLARE @url NVARCHAR(4000) = 'http://127.0.0.1:8000/ingest/agent-data';
DECLARE @apiKey NVARCHAR(200) = 'Hx7z9Q2wR4mP6tY8uVbN3cL1sZ0kF5hE';
DECLARE @agentId NVARCHAR(200) = @@SERVERNAME;
DECLARE @loginData NVARCHAR(MAX);

SELECT @loginData = (
    SELECT
        p.name AS login_name,
        p.type_desc AS login_type,
        p.is_disabled,
        p.create_date,
        p.modify_date,
        COALESCE(
            STUFF((
                SELECT ', ' + r.name
                FROM sys.server_role_members rm
                JOIN sys.server_principals r
                    ON rm.role_principal_id = r.principal_id
                WHERE rm.member_principal_id = p.principal_id
                FOR XML PATH(''), TYPE
            ).value('.', 'nvarchar(max)'), 1, 2, ''),
            '') AS server_roles,
        COALESCE(
            STUFF((
                SELECT ', ' + sp.permission_name
                    + CASE WHEN sp.state_desc = 'GRANT_WITH_GRANT_OPTION' THEN ' (WITH GRANT)' ELSE '' END
                FROM sys.server_permissions sp
                WHERE sp.grantee_principal_id = p.principal_id
                FOR XML PATH(''), TYPE
            ).value('.', 'nvarchar(max)'), 1, 2, ''),
            '') AS server_permissions
    FROM sys.server_principals p
    WHERE p.type_desc IN (
        'SQL_LOGIN',
        'WINDOWS_LOGIN',
        'WINDOWS_GROUP',
        'ASYMMETRIC_KEY_LOGIN',
        'CERTIFICATE_MAPPED_LOGIN'
    )
    ORDER BY p.name
    FOR JSON PATH
);

SELECT dbo.fncResolveHttpRequest(
    'POST',
    @url,
    N'{
        "agent_id": "' + @agentId + N'",
        "timestamp": "' + CONVERT(NVARCHAR(30), SYSUTCDATETIME(), 127) + N'",
        "payload_type": "InstanceLogins",
        "payload_data": {"logins": ' + @loginData + N'},
        "metadata": {"source":"sys.server_principals","category":"server_logins"}
    }',
    N'<Headers>
        <Header Name="Content-Type">application/json; charset=utf-8</Header>
        <Header Name="X-API-Key">' + @apiKey + N'</Header>
    </Headers>',
    30000,
    1,
    0
);