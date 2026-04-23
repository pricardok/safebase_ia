using System;
using System.Collections.Generic;
using System.Text;

namespace InitDB.Client
{
    class stpCargaAlteracaoDB
    {
        public static string Query()
        {
            // Criar diretorios caso nao existam 
            string checkBackupBase = ExecuteSql.ExecuteQuery("SELECT Ds_Caminho FROM [dbo].AlertaParametro (NOLOCK) WHERE Nm_Alerta = 'CheckList'");
            string checkBackupFim = ExecuteSql.ExecuteQuery("SELECT Ds_Caminho_Log FROM AlertaParametro (NOLOCK) where Nm_Alerta = 'Alteracao database'");

            string LocalFile = checkBackupBase + checkBackupFim;
            //string dir = @"C:\Data\Logs";
            Core.ExecuteCheckDir();
            
            return
            // @"insert into [dbo].[Testedb] ([Nome],[DateTest]) values ('Teste da ferramenta DB - stpCargaAlteracaoDB',GETDATE())";
            @"
              SET NOCOUNT ON
 
              USE [master]
              IF NOT EXISTS (SELECT * FROM sys.server_audits 
                  WHERE name = N'Audit-db')
              BEGIN
	          USE [master]
	            CREATE SERVER AUDIT [Audit-db]
		                TO FILE 
		                (  FILEPATH = N'" + LocalFile + @"\'
		                  ,MAXSIZE = 50 MB
						  ,MAX_FILES = 10
		                  ,RESERVE_DISK_SPACE = OFF
		                )
		                WITH
		                (  QUEUE_DELAY = 1000
		                  ,ON_FAILURE = CONTINUE
		      )
		
	          ALTER SERVER AUDIT [Audit-db] WITH (STATE = ON) 
	
	          USE [master]
	          CREATE SERVER AUDIT SPECIFICATION [ServerAuditSpecification01]
		                FOR SERVER AUDIT [Audit-db]
		                ADD (DATABASE_CHANGE_GROUP),
		                ADD (DATABASE_OBJECT_CHANGE_GROUP),
		                ADD (SCHEMA_OBJECT_CHANGE_GROUP),
		                ADD (SERVER_OBJECT_CHANGE_GROUP)
		                WITH (STATE = ON)
		      END

              USE [SafeBase]

              INSERT INTO [dbo].[ServerAudi] ([DataEvento] ,[serverInstanceName],[DatabaseName],[ActionId],[Session],[statement])
              SELECT CAST(event_time AS date) AS event_time,server_instance_name,database_name,action_id,server_principal_name,statement--,* 
              FROM Sys.fn_get_audit_file('" + LocalFile + @"\*.sqlaudit',default,default)
                 WHERE ([statement] like '%MODIFY NAME%' OR [statement] like '%DROP%DATABASE%' OR [statement] like '%CREATE%' )
				 AND CAST(event_time AS date) = CONVERT (date, GETDATE()-1)

              IF (@@ROWCOUNT = 0)
              BEGIN
                INSERT INTO [dbo].[ServerAudi] ([DataEvento] ,[serverInstanceName],[DatabaseName],[ActionId],[Session],[statement])
                SELECT GETDATE()-1,'Sem registro de Alteraçoes nas bases', NULL, NULL, NULL, NULL
              END

			  USE [master]  
			  ALTER SERVER AUDIT [Audit-db]  
			  WITH (STATE = OFF);  

			  use [SafeBase]
			  DECLARE @CaminhoFIleAudit VARCHAR(1000)

			  DECLARE DeleteFIleAudit Cursor For 
				select CaminhoCompleto from dbo.fncListarDiretorio ('" + LocalFile + @"\', '*') where CaminhoCompleto like '%sqlaudit'
	
	          Open DeleteFIleAudit
	          FETCH NEXT FROM DeleteFIleAudit INTO @CaminhoFIleAudit 

	          WHILE(@@FETCH_STATUS = 0)
	          BEGIN
				exec dbo.stpDeleteFile @CaminhoFIleAudit
		        FETCH NEXT FROM DeleteFIleAudit INTO @CaminhoFIleAudit 
	          END
	          CLOSE DeleteFIleAudit
	          DEALLOCATE DeleteFIleAudit
            
              USE [master]  
			  ALTER SERVER AUDIT [Audit-db]  
			  WITH (STATE = ON);  

               ";

        }
    }
}
