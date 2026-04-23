-- Time DBA
USE [msdb]
	
if not exists (
select NULL
from msdb.dbo.sysoperators
where name = 'MonitoramentoDB' )
begin 
	EXEC [msdb].[dbo].[sp_add_operator]
			@name = N'MonitoramentoDB',
			@enabled = 1,
			@pager_days = 0,
			@email_address = N'monitoramentodb@safeweb.com.br'	-- To put more Emails: 'EMail1@provedor.com;EMail2@provedor.com'	

end

GO
USE master;
GO
sp_configure 'clr enabled'
GO
sp_configure 'clr enabled', 1
GO
RECONFIGURE
GO
sp_configure 'clr enabled'
GO
CREATE DATABASE [SafeBase]
COLLATE Latin1_General_CI_AS
WITH TRUSTWORTHY ON, DB_CHAINING ON;
GO
USE [SafeBase]
GO
sp_configure 'show advanced options', 1;  
GO  
RECONFIGURE;  
GO  
sp_configure 'Ole Automation Procedures', 1;  
GO  
RECONFIGURE;  
GO

-- Habilita Dependências na Instancia 

USE master;
GO
sp_configure 'clr enabled'
GO
sp_configure 'clr enabled', 1
GO
RECONFIGURE
GO
sp_configure 'clr enabled'
GO
USE [SafeBase]
GO
sp_configure 'show advanced options', 1;  
GO  
RECONFIGURE;  
GO  
sp_configure 'Ole Automation Procedures', 1;  
GO  
RECONFIGURE; 
GO
sp_configure 'show advanced options', 1;
GO
 
RECONFIGURE
GO
 
sp_configure 'Database Mail XPs', 1;
GO
 
RECONFIGURE
GO

ALTER DATABASE [SafeBase] SET TRUSTWORTHY ON;

ALTER AUTHORIZATION ON DATABASE::[SafeBase] TO [sa]


-- Ajusta User

USE [SafeBase]
GO

DECLARE 
    @Comando VARCHAR(MAX)

SELECT
    @Comando = 'EXEC dbo.sp_changedbowner ' + QUOTENAME(B.name)
FROM
    sys.databases                       A
    JOIN sys.server_principals          B	ON	B.sid = A.owner_sid
WHERE
    A.name = DB_NAME()

EXEC dbo.sp_changedbowner [sa]

--PRINT @Comando
EXEC(@Comando)

-- LIMPA TABELAS

TRUNCATE TABLE 	[dbo].Alerta
TRUNCATE TABLE 	[dbo].AlertaAlwaysOn
TRUNCATE TABLE	[dbo].AlertaStatusDatabases
TRUNCATE TABLE	[dbo].BaseDados
TRUNCATE TABLE	[dbo].BaseJobs
TRUNCATE TABLE	[dbo].CheckAlerta
TRUNCATE TABLE	[dbo].CheckAlertaSemClear
TRUNCATE TABLE	[dbo].CheckAlteracaoJobs
TRUNCATE TABLE	[dbo].CheckArquivosDados
TRUNCATE TABLE	[dbo].CheckArquivosLog
TRUNCATE TABLE	[dbo].CheckBackupsExecutados
TRUNCATE TABLE	[dbo].CheckConexaoAberta
TRUNCATE TABLE	[dbo].CheckConexaoAberta_Email
TRUNCATE TABLE	[dbo].CheckContadores
TRUNCATE TABLE	[dbo].CheckContadoresEmail
TRUNCATE TABLE	[dbo].CheckDatabaseGrowth
TRUNCATE TABLE	[dbo].CheckDatabaseGrowthEmail
TRUNCATE TABLE	[dbo].CheckDatabasesSemBackup
TRUNCATE TABLE	[dbo].CheckDBControllerQueries
TRUNCATE TABLE	[dbo].CheckDBControllerQueriesGeral
TRUNCATE TABLE	[dbo].CheckEspacoDisco
TRUNCATE TABLE	[dbo].CheckFragmentacaoIndices
TRUNCATE TABLE	[dbo].CheckInitDBQueries
TRUNCATE TABLE	[dbo].CheckInitDBQueriesGeral
TRUNCATE TABLE	[dbo].CheckJobDemorados
TRUNCATE TABLE	[dbo].CheckJobsFailed
TRUNCATE TABLE	[dbo].CheckJobsRunning
TRUNCATE TABLE	[dbo].CheckQueries
TRUNCATE TABLE	[dbo].CheckQueriesGeral
TRUNCATE TABLE	[dbo].CheckQueriesRunning
TRUNCATE TABLE	[dbo].CheckSQLServerErrorLog
TRUNCATE TABLE	[dbo].CheckSQLServerLoginFailed
TRUNCATE TABLE	[dbo].CheckSQLServerLoginFailedEmail
TRUNCATE TABLE	[dbo].CheckTableGrowth
TRUNCATE TABLE	[dbo].CheckTableGrowthEmail
TRUNCATE TABLE	[dbo].CheckUtilizacaoArquivoReads
TRUNCATE TABLE	[dbo].CheckUtilizacaoArquivoWrites
TRUNCATE TABLE	[dbo].CheckWaitsStats
TRUNCATE TABLE	[dbo].Contador
TRUNCATE TABLE	[dbo].ContadorRegistro
TRUNCATE TABLE	[dbo].GrupoDeMailLista
TRUNCATE TABLE	[dbo].HistoricoErrosDB
TRUNCATE TABLE	[dbo].HistoricoFragmentacaoIndice
TRUNCATE TABLE	[dbo].HistoricoSuspectPages
TRUNCATE TABLE	[dbo].HistoricoTamanhoTabela
TRUNCATE TABLE	[dbo].HistoricoUtilizacaoArquivo
TRUNCATE TABLE	[dbo].HistoricoWaitsStats
TRUNCATE TABLE	[dbo].HitoricoFragmentacaoIndice
TRUNCATE TABLE	[dbo].HorariosGrupoPeriodo
TRUNCATE TABLE	[dbo].HorariosJobs
TRUNCATE TABLE	[dbo].JobsDB
TRUNCATE TABLE	[dbo].LogEmail
TRUNCATE TABLE	[dbo].LogErro
TRUNCATE TABLE	[dbo].PasswordAudit
TRUNCATE TABLE	[dbo].ResultadoEspacodisco
TRUNCATE TABLE	[dbo].ResultadoProc
TRUNCATE TABLE	[dbo].ResultadoProcBlock
TRUNCATE TABLE	[dbo].ResultadoTraceLog
TRUNCATE TABLE	[dbo].ServerAudi
TRUNCATE TABLE	[dbo].Servidor
TRUNCATE TABLE	[dbo].SQLTraceLog
TRUNCATE TABLE	[dbo].Tabela
TRUNCATE TABLE	[dbo].Testedb
