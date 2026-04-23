using System;
using System.Collections.Generic;
using System.Text;
using SafeBase_Installer.Core;

namespace SafeBase_Installer
{
    class TableDelete
    {
        public static string Query(string use)
        {
            return
            @"
            USE " + use + @"
            SET ANSI_NULLS ON
	
	        IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID('[dbo].[Alerta]')) 
	        DROP TABLE [dbo].[Alerta]
		        -- Dropped Table: Alerta

	        IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID('[dbo].[AlertaAlwaysOn]')) 
	        DROP TABLE [dbo].[AlertaAlwaysOn]
		        -- Dropped Table: AlertaAlwaysOn

	        IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID('[dbo].[AlertaEnvio]')) 
	        DROP TABLE [dbo].[AlertaEnvio]
		        -- Dropped Table: AlertaEnvio

	        IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID('[dbo].[AlertaStatusDatabases]')) 
	        DROP TABLE [dbo].[AlertaStatusDatabases]
		        -- Dropped Table: AlertaStatusDatabases

	        IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID('[dbo].[BaseDados]')) 
	        DROP TABLE [dbo].[BaseDados]
		        -- Dropped Table: BaseDados

	        IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID('[dbo].[BaseJobs]')) 
	        DROP TABLE [dbo].[BaseJobs]
		        -- Dropped Table: BaseJobs

	        IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID('[dbo].[CheckAlerta]')) 
	        DROP TABLE [dbo].[CheckAlerta]
		        -- Dropped Table: CheckAlerta

	        IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID('[dbo].[CheckAlertaSemClear]')) 
	        DROP TABLE [dbo].[CheckAlertaSemClear]
		        -- Dropped Table: CheckAlertaSemClear

	        IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID('[dbo].[CheckAlteracaoJobs]')) 
	        DROP TABLE [dbo].[CheckAlteracaoJobs]
		        -- Dropped Table: CheckAlteracaoJobs

	        IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID('[dbo].[CheckArquivosDados]')) 
	        DROP TABLE [dbo].[CheckArquivosDados]
		        -- Dropped Table: CheckArquivosDados

	        IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID('[dbo].[CheckArquivosLog]')) 
	        DROP TABLE [dbo].[CheckArquivosLog]
		        -- Dropped Table: CheckArquivosLog

	        IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID('[dbo].[CheckBackupsExecutados]')) 
	        DROP TABLE [dbo].[CheckBackupsExecutados]
		        -- Dropped Table: CheckBackupsExecutados

	        IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID('[dbo].[CheckConexaoAberta]')) 
	        DROP TABLE [dbo].[CheckConexaoAberta]
		        -- Dropped Table: CheckConexaoAberta

	        IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID('[dbo].[CheckConexaoAberta_Email]')) 
	        DROP TABLE [dbo].[CheckConexaoAberta_Email]
		        -- Dropped Table: CheckConexaoAberta_Email

	        IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID('[dbo].[CheckContadores]')) 
	        DROP TABLE [dbo].[CheckContadores]
		        -- Dropped Table: CheckContadores

	        IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID('[dbo].[CheckContadoresEmail]')) 
	        DROP TABLE [dbo].[CheckContadoresEmail]
		        -- Dropped Table: CheckContadoresEmail

	        IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID('[dbo].[CheckDatabaseGrowth]')) 
	        DROP TABLE [dbo].[CheckDatabaseGrowth]
		        -- Dropped Table: CheckDatabaseGrowth

	        IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID('[dbo].[CheckDatabaseGrowthEmail]')) 
	        DROP TABLE [dbo].[CheckDatabaseGrowthEmail]
		        -- Dropped Table: CheckDatabaseGrowthEmail

	        IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID('[dbo].[CheckDatabasesHistoricoBackup]')) 
	        DROP TABLE [dbo].[CheckDatabasesHistoricoBackup]
		        -- Dropped Table: CheckDatabasesHistoricoBackup

	        IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID('[dbo].[CheckDatabasesSemBackup]')) 
	        DROP TABLE [dbo].[CheckDatabasesSemBackup]
		        -- Dropped Table: CheckDatabasesSemBackup

	        IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID('[dbo].[CheckDBControllerQueries]')) 
	        DROP TABLE [dbo].[CheckDBControllerQueries]
		        -- Dropped Table: CheckDBControllerQueries

	        IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID('[dbo].[CheckDBControllerQueriesGeral]')) 
	        DROP TABLE [dbo].[CheckDBControllerQueriesGeral]
		        -- Dropped Table: CheckDBControllerQueriesGeral

	        IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID('[dbo].[CheckEspacoDisco]')) 
	        DROP TABLE [dbo].[CheckEspacoDisco]
		        -- Dropped Table: CheckEspacoDisco

	        IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID('[dbo].[CheckFragmentacaoIndices]')) 
	        DROP TABLE [dbo].[CheckFragmentacaoIndices]
		        -- Dropped Table: CheckFragmentacaoIndices

	        IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID('[dbo].[CheckInitDBQueries]')) 
	        DROP TABLE [dbo].[CheckInitDBQueries]
		        -- Dropped Table: CheckInitDBQueries

	        IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID('[dbo].[CheckInitDBQueriesGeral]')) 
	        DROP TABLE [dbo].[CheckInitDBQueriesGeral]
		        -- Dropped Table: CheckInitDBQueriesGeral

	        IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID('[dbo].[CheckJobDemorados]')) 
	        DROP TABLE [dbo].[CheckJobDemorados]
		        -- Dropped Table: CheckJobDemorados

	        IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID('[dbo].[CheckJobsFailed]')) 
	        DROP TABLE [dbo].[CheckJobsFailed]
		        -- Dropped Table: CheckJobsFailed

	        IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID('[dbo].[CheckJobsRunning]')) 
	        DROP TABLE [dbo].[CheckJobsRunning]
		        -- Dropped Table: CheckJobsRunning

	        IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID('[dbo].[CheckQueries]')) 
	        DROP TABLE [dbo].[CheckQueries]
		        -- Dropped Table: CheckQueries

	        IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID('[dbo].[CheckQueriesGeral]')) 
	        DROP TABLE [dbo].[CheckQueriesGeral]
		        -- Dropped Table: CheckQueriesGeral

	        IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID('[dbo].[CheckQueriesRunning]')) 
	        DROP TABLE [dbo].[CheckQueriesRunning]
		        -- Dropped Table: CheckQueriesRunning

	        IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID('[dbo].[CheckSQLServerErrorLog]')) 
	        DROP TABLE [dbo].[CheckSQLServerErrorLog]
		        -- Dropped Table: CheckSQLServerErrorLog

	        IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID('[dbo].[CheckSQLServerLoginFailed]')) 
	        DROP TABLE [dbo].[CheckSQLServerLoginFailed]
		        -- Dropped Table: CheckSQLServerLoginFailed

	        IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID('[dbo].[CheckSQLServerLoginFailedEmail]')) 
	        DROP TABLE [dbo].[CheckSQLServerLoginFailedEmail]
		        -- Dropped Table: CheckSQLServerLoginFailedEmail

	        IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID('[dbo].[CheckTableGrowth]')) 
	        DROP TABLE [dbo].[CheckTableGrowth]
		        -- Dropped Table: CheckTableGrowth

	        IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID('[dbo].[CheckTableGrowthEmail]')) 
	        DROP TABLE [dbo].[CheckTableGrowthEmail]
		        -- Dropped Table: CheckTableGrowthEmail

	        IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID('[dbo].[CheckUtilizacaoArquivoReads]')) 
	        DROP TABLE [dbo].[CheckUtilizacaoArquivoReads]
		        -- Dropped Table: CheckUtilizacaoArquivoReads

	        IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID('[dbo].[CheckUtilizacaoArquivoWrites]')) 
	        DROP TABLE [dbo].[CheckUtilizacaoArquivoWrites]
		        -- Dropped Table: CheckUtilizacaoArquivoWrites

	        IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID('[dbo].[CheckWaitsStats]')) 
	        DROP TABLE [dbo].[CheckWaitsStats]
		        -- Dropped Table: CheckWaitsStats

	        IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID('[dbo].[Config]')) 
	        DROP TABLE [dbo].[Config]
		        -- Dropped Table: Config

	        IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID('[dbo].[ConfigDB]')) 
	        DROP TABLE [dbo].[ConfigDB]
		        -- Dropped Table: ConfigDB

	        IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID('[dbo].[Contador]')) 
	        DROP TABLE [dbo].[Contador]
		        -- Dropped Table: Contador

	        IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID('[dbo].[ContadorRegistro]')) 
	        DROP TABLE [dbo].[ContadorRegistro]
		        -- Dropped Table: ContadorRegistro

	        IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID('[dbo].[GrupoDeMailLista]')) 
	        DROP TABLE [dbo].[GrupoDeMailLista]
		        -- Dropped Table: GrupoDeMailLista

	        IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID('[dbo].[GrupoDeMail]')) 
	        DROP TABLE [dbo].[GrupoDeMail]
		        -- Dropped Table: GrupoDeMail

	        IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID('[dbo].[HisIndexDefragStatus]')) 
	        DROP TABLE [dbo].[HisIndexDefragStatus]
		        -- Dropped Table: HisIndexDefragStatus

	        IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID('[dbo].[HistIndexDefragExclusion]')) 
	        DROP TABLE [dbo].[HistIndexDefragExclusion]
		        -- Dropped Table: HistIndexDefragExclusion

	        IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID('[dbo].[HistIndexDefragLog]')) 
	        DROP TABLE [dbo].[HistIndexDefragLog]
		        -- Dropped Table: HistIndexDefragLog

	        IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID('[dbo].[HistIndexDefragTablesToExclude]')) 
	        DROP TABLE [dbo].[HistIndexDefragTablesToExclude]
		        -- Dropped Table: HistIndexDefragTablesToExclude

	        IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID('[dbo].[HistoricoDBCC]')) 
	        DROP TABLE [dbo].[HistoricoDBCC]
		        -- Dropped Table: HistoricoDBCC

	        IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID('[dbo].[HistoricoErrosDB]')) 
	        DROP TABLE [dbo].[HistoricoErrosDB]
		        -- Dropped Table: HistoricoErrosDB

	        IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID('[dbo].[HistoricoFragmentacaoIndice]')) 
	        DROP TABLE [dbo].[HistoricoFragmentacaoIndice]
		        -- Dropped Table: HistoricoFragmentacaoIndice

	        IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID('[dbo].[HistoricoQueue]')) 
	        DROP TABLE [dbo].[HistoricoQueue]
		        -- Dropped Table: HistoricoQueue

	        IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID('[dbo].[HistoricoSuspectPages]')) 
	        DROP TABLE [dbo].[HistoricoSuspectPages]
		        -- Dropped Table: HistoricoSuspectPages

	        IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID('[dbo].[HistoricoTamanhoTabela]')) 
	        DROP TABLE [dbo].[HistoricoTamanhoTabela]
		        -- Dropped Table: HistoricoTamanhoTabela

	        IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID('[dbo].[HistoricoUtilizacaoArquivo]')) 
	        DROP TABLE [dbo].[HistoricoUtilizacaoArquivo]
		        -- Dropped Table: HistoricoUtilizacaoArquivo

	        IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID('[dbo].[HistoricoWaitsStats]')) 
	        DROP TABLE [dbo].[HistoricoWaitsStats]
		        -- Dropped Table: HistoricoWaitsStats

	        IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID('[dbo].[HitoricoFragmentacaoIndice]')) 
	        DROP TABLE [dbo].[HitoricoFragmentacaoIndice]
		        -- Dropped Table: HitoricoFragmentacaoIndice

	        IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID('[dbo].[HorariosGrupoPeriodo]')) 
	        DROP TABLE [dbo].[HorariosGrupoPeriodo]
		        -- Dropped Table: HorariosGrupoPeriodo

	        IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID('[dbo].[HorariosJobs]')) 
	        DROP TABLE [dbo].[HorariosJobs]
		        -- Dropped Table: HorariosJobs

	        IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID('[dbo].[JobsDB]')) 
	        DROP TABLE [dbo].[JobsDB]
		        -- Dropped Table: JobsDB

	        IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID('[dbo].[LayoutHtmlCss]')) 
            BEGIN
	            DROP TABLE [dbo].[LayoutHtmlCss]
		            -- Dropped Table: LayoutHtmlCss
            END

	        IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID('[dbo].[LogEmail]')) 
	        DROP TABLE [dbo].[LogEmail]
		        -- Dropped Table: LogEmail

	        IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID('[dbo].[LogErro]')) 
	        DROP TABLE [dbo].[LogErro]
		        -- Dropped Table: LogErro

	        IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID('[dbo].[LogQueue]')) 
	        DROP TABLE [dbo].[LogQueue]
		        -- Dropped Table: LogQueue

	        IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID('[dbo].[PasswordAudit]')) 
	        DROP TABLE [dbo].[PasswordAudit]
		        -- Dropped Table: PasswordAudit

	        IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID('[dbo].[Periodo]')) 
	        DROP TABLE [dbo].[Periodo]
		        -- Dropped Table: Periodo

	        IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID('[dbo].[PeriodoSemana]')) 
	        DROP TABLE [dbo].[PeriodoSemana]
		        -- Dropped Table: PeriodoSemana

	        IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID('[dbo].[ResultadoEspacodisco]')) 
	        DROP TABLE [dbo].[ResultadoEspacodisco]
		        -- Dropped Table: ResultadoEspacodisco

	        IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID('[dbo].[ResultadoProc]')) 
	        DROP TABLE [dbo].[ResultadoProc]
		        -- Dropped Table: ResultadoProc

	        IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID('[dbo].[ResultadoProcBlock]')) 
	        DROP TABLE [dbo].[ResultadoProcBlock]
		        -- Dropped Table: ResultadoProcBlock

	        IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID('[dbo].[ResultadoTraceLog]')) 
	        DROP TABLE [dbo].[ResultadoTraceLog]
		        -- Dropped Table: ResultadoTraceLog

	        IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID('[dbo].[ServerAudi]')) 
	        DROP TABLE [dbo].[ServerAudi]
		        -- Dropped Table: ServerAudi

	        IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID('[dbo].[Servidor]')) 
	        DROP TABLE [dbo].[Servidor]
		        -- Dropped Table: Servidor

	        IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID('[dbo].[SQLTraceLog]')) 
	        DROP TABLE [dbo].[SQLTraceLog]
		        -- Dropped Table: SQLTraceLog

	        IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID('[dbo].[Tabela]')) 
	        DROP TABLE [dbo].[Tabela]
		        -- Dropped Table: Tabela

	        IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID('[dbo].[Testedb]')) 
	        DROP TABLE [dbo].[Testedb]
		        -- Dropped Table: Testedb

	        IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID('[dbo].[MailAssinatura]')) 
	        DROP TABLE [dbo].[MailAssinatura]
		        -- -- Dropped Table: MailAssinatura

	        IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID('[dbo].[AlertaMsgToken]')) 
	        DROP TABLE [dbo].[AlertaMsgToken]
		        -- -- Dropped Table: AlertaMsgToken

	        IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID('[dbo].[AlertaParametroMenssage]')) 
	        DROP TABLE [dbo].[AlertaParametroMenssage]
		        -- -- Dropped Table: AlertaParametroMenssage
	
	        IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID('[dbo].[AlertaParametro]')) 
	        DROP TABLE [dbo].[AlertaParametro]
		        -- -- Dropped Table: AlertaParametro

			IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID('[dbo].[HistoricoAlwaysOn]')) 
	        DROP TABLE [dbo].[HistoricoAlwaysOn]
		        -- Dropped Table: HistoricoAlwaysOn

			IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID('[dbo].[HistoricoAuditLogins]')) 
	        DROP TABLE [dbo].[HistoricoAuditLogins]
		        -- Dropped Table: HistoricoAuditLogins

			IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID('[dbo].[HistoricoVersionamentoDB]')) 
	        DROP TABLE [dbo].[HistoricoVersionamentoDB]
		        -- Dropped Table: HistoricoVersionamentoDB

			IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID('[dbo].[HistoricoUsuariosAD]')) 
	        DROP TABLE [dbo].[HistoricoUsuariosAD]
		        -- Dropped Table: HistoricoUsuariosAD

			IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID('[dbo].[HistoricoAlteracaoObjetos]')) 
	        DROP TABLE [dbo].[HistoricoAlteracaoObjetos]
		        -- Dropped Table: HistoricoAlteracaoObjetos

			IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID('[job].[Job]')) 
	        DROP TABLE [job].[Job]
		        -- Dropped Table: Job

			IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID('[job].[JobAgendamento]')) 
	        DROP TABLE [job].[JobAgendamento]
		        -- Dropped Table: JobAgendamento

			IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID('[job].[JobHistorico]')) 
	        DROP TABLE [job].[JobHistorico]
		        -- Dropped Table: JobHistorico

			IF  EXISTS (SELECT SCHEMA_NAME FROM INFORMATION_SCHEMA.SCHEMATA WHERE SCHEMA_NAME = 'job')
			DROP SCHEMA job
				-- Dropped Schema: job
            GO

            ";

        }
    }
}
