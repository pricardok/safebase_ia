-- Exemplo de envio em blocos para o endpoint /ingest/agent-data
-- Ajuste @batchSize conforme MAX_INGESTION_BATCH_SIZE configurado no backend.

DECLARE @url NVARCHAR(4000) = 'http://192.168.11.215:8000/ingest/agent-data';
DECLARE @apiKey NVARCHAR(200) = 'Hx7z9Q2wR4mP6tY8uVbN3cL1sZ0kF5hE';
DECLARE @agentId NVARCHAR(200) = @@SERVERNAME;
DECLARE @batchSize INT = 100;
DECLARE @chunkIndex INT = 1;
DECLARE @total INT;
DECLARE @startRow INT = 1;
DECLARE @endRow INT;
DECLARE @loginData NVARCHAR(MAX);
DECLARE @payload NVARCHAR(MAX);
DECLARE @response XML;

IF OBJECT_ID('tempdb..#Logins') IS NOT NULL
    DROP TABLE #Logins;

SELECT
    ROW_NUMBER() OVER (ORDER BY p.name) AS rn,
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
INTO #Logins
FROM sys.server_principals p
WHERE p.type_desc IN (
    'SQL_LOGIN',
    'WINDOWS_LOGIN',
    'WINDOWS_GROUP',
    'ASYMMETRIC_KEY_LOGIN',
    'CERTIFICATE_MAPPED_LOGIN'
)
ORDER BY p.name;

SELECT @total = COUNT(*) FROM #Logins;

WHILE @startRow <= @total
BEGIN
    SET @endRow = CASE WHEN @startRow + @batchSize - 1 > @total THEN @total ELSE @startRow + @batchSize - 1 END;

    SELECT @loginData = (
        SELECT
            login_name,
            login_type,
            is_disabled,
            create_date,
            modify_date,
            server_roles,
            server_permissions
        FROM #Logins
        WHERE rn BETWEEN @startRow AND @endRow
        ORDER BY rn
        FOR JSON PATH
    );

    SELECT @payload = (
        SELECT
            @agentId AS agent_id,
            CONVERT(NVARCHAR(30), SYSUTCDATETIME(), 127) AS [timestamp],
            'InstanceLogins' AS payload_type,
            JSON_QUERY(@loginData) AS payload_data,
            JSON_QUERY(N'{"source":"sys.server_principals","category":"server_logins"}') AS metadata
        FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
    );

    SELECT @response = safebase.dbo.fncResolveHttpRequest(
        'POST',
        @url,
        @payload,
        N'<Headers>
            <Header Name="Content-Type">application/json; charset=utf-8</Header>
            <Header Name="X-API-Key">' + @apiKey + N'</Header>
        </Headers>',
        30000,
        1,
        0
    );

    DECLARE @status NVARCHAR(MAX);
    SET @status = @response.value('(/Response/StatusDescription)[1]', 'nvarchar(max)');

    PRINT CONCAT('Sent chunk ', @chunkIndex, ' (rows ', @startRow, ' to ', @endRow, '): ', @status);

    SET @startRow += @batchSize;
    SET @chunkIndex += 1;
END;

DROP TABLE #Logins;
