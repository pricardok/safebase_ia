IF OBJECT_ID(N'dbo.stpAuditSpec_GetSpecDetails', N'P') IS NOT NULL
    DROP PROCEDURE dbo.stpAuditSpec_GetSpecDetails;
GO

CREATE PROCEDURE dbo.stpAuditSpec_GetSpecDetails
    @AuditSpecName SYSNAME = N'AuditSpec_UGA_FA',
    @ObjectName SYSNAME = NULL,
    @AuditActionName NVARCHAR(128) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        sas.name AS AuditSpecificationName,
        sa.name AS AuditName,
        sa.audit_guid AS AuditGuid,
        sa.audit_destination AS AuditDestination,
        sa.queue_delay AS AuditQueueDelay,
        sa.is_state_enabled AS AuditEnabled,
        sas.is_state_enabled AS AuditSpecificationEnabled,
        sas.create_date AS AuditSpecificationCreateDate,
        sas.modify_date AS AuditSpecificationModifyDate,
        ssd.audit_action_id AS AuditActionId,
        ssd.audit_action_name AS AuditActionName,
        ssd.class_type_desc AS ClassTypeDesc,
        ssd.object_schema_name AS ObjectSchemaName,
        ssd.object_name AS ObjectName,
        ssd.database_name AS DatabaseName,
        ssd.server_principal_name AS ServerPrincipalName,
        ssd.principal_name AS PrincipalName,
        ssd.statement AS StatementText,
        ssd.is_group AS IsGroup,
        ssd.is_subclass AS IsSubclass
    FROM sys.server_audit_specifications AS sas
    JOIN sys.server_audit_specification_details AS ssd
        ON sas.server_specification_id = ssd.server_specification_id
    LEFT JOIN sys.server_audits AS sa
        ON sas.audit_guid = sa.audit_guid
    WHERE sas.name = @AuditSpecName
      AND (@ObjectName IS NULL OR ssd.object_name = @ObjectName)
      AND (@AuditActionName IS NULL OR ssd.audit_action_name = @AuditActionName)
    ORDER BY ssd.audit_action_name, ssd.object_schema_name, ssd.object_name;
END
GO
