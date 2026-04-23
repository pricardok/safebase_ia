using System;
using System.Collections.Generic;
using System.Text;

namespace InitDB.Client
{
    class stpEnviaCheckList
    {
        public static string Query()
        {
            return
            // @"insert into [dbo].[Testedb] ([Nome],[DateTest]) values ('Teste da ferramenta DB',GETDATE())";
            @"
            SET NOCOUNT ON

	        -- Declara as variaveis
	        DECLARE @Tamanho_Minimo_Alerta_log INT, @AlertaLogHeader VARCHAR(MAX), @AlertaLogTable VARCHAR(MAX), @EmptyBodyEmail VARCHAR(MAX),
			        @importance AS VARCHAR(6), @Subject VARCHAR(500), @Fl_Tipo TINYINT, @Log_Full_Parametro TINYINT,
			        @ResultadoWhoisactiveHeader VARCHAR(MAX), @ResultadoWhoisactiveTable VARCHAR(MAX), @EmailDestination VARCHAR(200), 
			        @BuscaParametro VARCHAR(80), @TextRel1 VARCHAR(600), @TextRel2 VARCHAR(100), @TextRel3 VARCHAR(100), @TextRel4 VARCHAR(100),
			        @TextRel5 VARCHAR(100), @TextRel6 VARCHAR(100), @TextRel7 VARCHAR(100),@TextRel8 VARCHAR(100), @TextRel9 VARCHAR(100),
			        @TextRel10 VARCHAR(100), @TextRel11 VARCHAR(100), @TextRel12 VARCHAR(100), @TextRel13 VARCHAR(100), @TextRel14 VARCHAR(100),
			        @TextRel15 VARCHAR(100), @TextRel16 VARCHAR(100), @TextRel17 VARCHAR(100), @TextRel18 VARCHAR(100), @TextRel19 VARCHAR(100),
			        @TextRel20 VARCHAR(100), @TextRel21 VARCHAR(100), @TextRel22 VARCHAR(100), @TextRel23 VARCHAR(100), @TextRel24 VARCHAR(100),
			        @TextRel25 VARCHAR(100), @TextRel26 VARCHAR(100), @TextRel27 VARCHAR(100), @NomeRel VARCHAR(300),@MntMsg VARCHAR(200), @TLMsg VARCHAR(200), 
			        @SendMail VARCHAR(200),  @ProfileDBMail VARCHAR(50), @BodyFormatMail VARCHAR(20), @CaminhoPath VARCHAR(50), @CaminhoFim VARCHAR(50), 
			        @Ass VARCHAR(4000), @HTML VARCHAR(MAX), @Query VARCHAR(MAX), @Empresa VARCHAR (50), @Ds_Email_Assunto VARCHAR (50), 
                    @Ds_Email_Texto VARCHAR (600), @Ds_Menssageiro_01 VARCHAR (30),@Ds_Menssageiro_02 VARCHAR (30), @Ds_Menssageiro_03 VARCHAR (30)
                    --@EmailBody VARCHAR(MAX)

	        -- Recupera Parametros principais
	        DECLARE @Id_AlertaParametro INT = (SELECT Id_AlertaParametro FROM [dbo].AlertaParametro (NOLOCK) WHERE Nm_Alerta = 'CheckList' AND Ativo = 1)
            DECLARE @Ds_Caminho_Base VARCHAR(100) = (SELECT Ds_Caminho FROM [dbo].AlertaParametro (NOLOCK) WHERE Nm_Alerta = 'CheckList')
            DECLARE @Telegram INT = (select Id_AlertaParametro from AlertaParametro WHERE Nm_Alerta = 'Envia Telegram')
            DECLARE @Teams INT = (select Id_AlertaParametro from AlertaParametro WHERE Nm_Alerta = 'Envia Teams')

	        -- Email, Parametro, Id Telegram, Caminho dos reports, Profile DB Mail, Body Format Mail 
	        SELECT @NomeRel = A.Nm_Alerta, 
		           @EmailDestination = A.Ds_Email, 
		           @TLMsg = Ds_MSG,
				   @Ds_Menssageiro_01 = A.Ds_Menssageiro_01,
				   @Ds_Menssageiro_02 = A.Ds_Menssageiro_02,
                   @Ds_Menssageiro_03 = A.Ds_Menssageiro_03,
		           @CaminhoPath = A.Ds_Caminho_Log, 
		           @ProfileDBMail = A.Ds_ProfileDBMail, 
		           @BodyFormatMail = A.Ds_BodyFormatMail,
		           @importance = A.Ds_TipoMail,
		           @Empresa = A.Nm_Empresa,
                   @Ds_Email_Assunto = B.SubjectSolution,
                   @Ds_Email_Texto = B.MailTextSolution,
                   @Ass = C.Assinatura
	        FROM [dbo].[AlertaParametro] A
            INNER JOIN [dbo].[AlertaParametroMenssage] B ON A.Id_AlertaParametro = B.IdAlertaParametro
			INNER JOIN [dbo].[MailAssinatura] C ON C.Id = A.IdMailAssinatura
	        WHERE [Id_AlertaParametro] = @Id_AlertaParametro
            AND B.NomeMsg IS NULL

            DECLARE @CanalTelegram VARCHAR(100) = (SELECT A.canal FROM [dbo].[AlertaMsgToken] A
                      INNER JOIN [dbo].AlertaParametro B ON A.Id = B.Ds_Menssageiro_01 where b.Ds_Menssageiro_01 = @Ds_Menssageiro_01 AND B.Id_AlertaParametro = @Telegram AND B.Ativo = 1) 

            -- Variaveis do check list
            DECLARE @Check_Disponibilidade VARCHAR (90) = (select MailTextSolution from [dbo].[AlertaParametroMenssage] WHERE NomeMsg = 'Check_Disponibilidade')
            DECLARE @Check_CrescimentoBases VARCHAR (90) = (select MailTextSolution from [dbo].[AlertaParametroMenssage] WHERE NomeMsg = 'Check_CrescimentoBases')
            DECLARE @Check_CrescimentoTabelas VARCHAR (90) = (select MailTextSolution from [dbo].[AlertaParametroMenssage] WHERE NomeMsg = 'Check_CrescimentoTabelas')
            DECLARE @Check_AlteracaoInstancia VARCHAR (90) = (select MailTextSolution from [dbo].[AlertaParametroMenssage] WHERE NomeMsg = 'Check_AlteracaoInstancia')
            DECLARE @Check_InfoArquivoDados VARCHAR (90) = (select MailTextSolution from [dbo].[AlertaParametroMenssage] WHERE NomeMsg = 'Check_InfoArquivoDados')
            DECLARE @Check_InfoArquivolog VARCHAR (90) = (select MailTextSolution from [dbo].[AlertaParametroMenssage] WHERE NomeMsg = 'Check_InfoArquivolog')
            DECLARE @Check_UtilizacaoArquivosWrites VARCHAR (90) = (select MailTextSolution from [dbo].[AlertaParametroMenssage] WHERE NomeMsg = 'Check_UtilizacaoArquivosWrites')
            DECLARE @Check_UtilizacaoArquivosReads VARCHAR (90) = (select MailTextSolution from [dbo].[AlertaParametroMenssage] WHERE NomeMsg = 'Check_UtilizacaoArquivosReads')
            DECLARE @Check_SemBKP VARCHAR (90) = (select MailTextSolution from [dbo].[AlertaParametroMenssage] WHERE NomeMsg = 'Check_SemBKP')
            DECLARE @Check_BKP_DB VARCHAR (90) = (select MailTextSolution from [dbo].[AlertaParametroMenssage] WHERE NomeMsg = 'Check_BKP_DB')
            DECLARE @Check_FragIndice VARCHAR (90) = (select MailTextSolution from [dbo].[AlertaParametroMenssage] WHERE NomeMsg = 'Check_FragIndice')
            DECLARE @Check_QueryDemorada VARCHAR (90) = (select MailTextSolution from [dbo].[AlertaParametroMenssage] WHERE NomeMsg = 'Check_QueryDemorada')
            DECLARE @Check_QueryExec VARCHAR (90) = (select MailTextSolution from [dbo].[AlertaParametroMenssage] WHERE NomeMsg = 'Check_QueryExec')
            DECLARE @Check_QtdQueryExec VARCHAR (90) = (select MailTextSolution from [dbo].[AlertaParametroMenssage] WHERE NomeMsg = 'Check_QtdQueryExec')
            DECLARE @Check_ErroLog VARCHAR (90) = (select MailTextSolution from [dbo].[AlertaParametroMenssage] WHERE NomeMsg = 'Check_ErroLog')
            DECLARE @Check_JOB_EXEC VARCHAR (90) = (select MailTextSolution from [dbo].[AlertaParametroMenssage] WHERE NomeMsg = 'Check_JOB_EXEC')
            DECLARE @Check_JOB_Alterados VARCHAR (90) = (select MailTextSolution from [dbo].[AlertaParametroMenssage] WHERE NomeMsg = 'Check_JOB_Alterados')
            DECLARE @Check_JOB_Falharam VARCHAR (90) = (select MailTextSolution from [dbo].[AlertaParametroMenssage] WHERE NomeMsg = 'Check_JOB_Falharam')
            DECLARE @Check_JOB_Demorados VARCHAR (90) = (select MailTextSolution from [dbo].[AlertaParametroMenssage] WHERE NomeMsg = 'Check_JOB_Demorados')
            DECLARE @Check_Media_Contadores VARCHAR (90) = (select MailTextSolution from [dbo].[AlertaParametroMenssage] WHERE NomeMsg = 'Check_Media_Contadores')
            DECLARE @Check_ConexoesUsuario VARCHAR (90) = (select MailTextSolution from [dbo].[AlertaParametroMenssage] WHERE NomeMsg = 'Check_ConexoesUsuario')
            DECLARE @Check_WaitsStats VARCHAR (90) = (select MailTextSolution from [dbo].[AlertaParametroMenssage] WHERE NomeMsg = 'Check_WaitsStats')
            DECLARE @Check_AlertasSemSolucao VARCHAR (90) = (select MailTextSolution from [dbo].[AlertaParametroMenssage] WHERE NomeMsg = 'Check_AlertasSemSolucao')
            DECLARE @Check_AlertasDiaAnterior VARCHAR (90) = (select MailTextSolution from [dbo].[AlertaParametroMenssage] WHERE NomeMsg = 'Check_AlertasDiaAnterior')
            DECLARE @Check_LoginFailed VARCHAR (90) = (select MailTextSolution from [dbo].[AlertaParametroMenssage] WHERE NomeMsg = 'Check_LoginFailed')
            DECLARE @Check_EspacoDisco VARCHAR (90) = (select MailTextSolution from [dbo].[AlertaParametroMenssage] WHERE NomeMsg = 'Check_EspacoDisco')
            DECLARE @Check_HistBKP VARCHAR (90) = (select MailTextSolution from [dbo].[AlertaParametroMenssage] WHERE NomeMsg = 'Check_HistBKP')


	
	        /***********************************************************************************************************************************
	        -- Cargas de envio
	        ***********************************************************************************************************************************/

            -- Disponibilidade MSSQL
	        IF(OBJECT_ID('tempdb..##DisponibilidadeSQL') IS NOT NULL)
		        DROP TABLE ##DisponibilidadeSQL;
	        CREATE TABLE ##DisponibilidadeSQL
	        (
	         [Disponibilidade MSSQL] NVARCHAR(200)
	        );

	        INSERT INTO ##DisponibilidadeSQL
	        SELECT CASE
			           WHEN(RTRIM(CONVERT(CHAR(17), DATEDIFF(SECOND, CONVERT(DATETIME, [Create_Date]), GETDATE())/86400)) = 0)
				           OR (RTRIM(CONVERT(CHAR(17), DATEDIFF(SECOND, CONVERT(DATETIME, [Create_Date]), GETDATE())/86400)) > 365)
			           THEN   ' <p style=color:red;>'  -- '< bgcolor='+'#009900'+'>' --' <bgcolor=yellow>' -- 
                       ELSE ''
                   END + RTRIM(CONVERT(CHAR(17), DATEDIFF(SECOND, CONVERT(DATETIME, [Create_Date]), GETDATE()) / 86400)) + ' Dia(s) ' + RIGHT('00' + RTRIM(CONVERT(CHAR(7), DATEDIFF(SECOND, CONVERT(DATETIME, [Create_Date]), GETDATE()) % 86400 / 3600)), 2) + ' Hora(s) ' + RIGHT('00' + RTRIM(CONVERT(CHAR(7), DATEDIFF(SECOND, CONVERT(DATETIME, [Create_Date]), GETDATE()) % 86400 % 3600 / 60)), 2) + ' Minuto(s) ' AS DisponibilidadeSQL
            FROM[sys].[databases]
            WHERE[Database_Id] = 2;

            --Check Espaco Disco
            IF(OBJECT_ID('tempdb..##CheckEspacoDisco') IS NOT NULL)
                DROP TABLE ##CheckEspacoDisco;
	        CREATE TABLE ##CheckEspacoDisco
	            ([Drive] NVARCHAR(50),
                 [TotalSize_GB] NVARCHAR(50),
	             [SpaceUsed_GB] NVARCHAR(50),
	             [FreeSpace_GB] NVARCHAR(50),
	             [SpaceUsed_Percent] NVARCHAR(50)
	        );

	        INSERT INTO ##CheckEspacoDisco
			SELECT [Drive] [DriveName],
		           ISNULL(CAST([Size (MB)] AS VARCHAR), '-') AS[TotalSize_GB],
		           ISNULL(CAST([Used (MB)] AS VARCHAR), '-') AS[SpaceUsed_GB],
		           ISNULL(CAST([Free (MB)] AS VARCHAR), '-') AS[FreeSpace_GB],
		           ISNULL(CAST([Used (%)] AS VARCHAR), '-') AS[SpaceUsed_Percent]
            FROM[dbo].[CheckEspacoDisco];

	        -- Check Arquivos Dados
            IF(OBJECT_ID('tempdb..##CheckArquivosDados') IS NOT NULL)
                DROP TABLE ##CheckArquivosDados;
	        CREATE TABLE ##CheckArquivosDados
	            ([NomeDatabase]       NVARCHAR(50),
	            [LogicalName] NVARCHAR(50),
	            [TotalReservado] NVARCHAR(50),
	            [TotalUtilizado] NVARCHAR(50),
	            [EspacoLivre(MB)] NVARCHAR(50),
	            [EspacoLivre(%)] NVARCHAR(50),
	            [MaxSize] NVARCHAR(50),
	            [Growth] NVARCHAR(50)
	        );
	        
            INSERT INTO ##CheckArquivosDados
            SELECT TOP 5 [Nm_Database]
                AS NomeDatabase,
                   ISNULL([Logical_Name], '-') AS[LogicalName],
		           ISNULL(CAST([Total_Reservado] AS VARCHAR), '-') AS [TotalReservado],
		           ISNULL(CAST([Total_Utilizado] AS VARCHAR), '-') AS [TotalUtilizado],
		           ISNULL(CAST([Espaco_Livre_MB] AS VARCHAR), '-') AS [EspacoLivreMB],
		           ISNULL(CAST([Espaco_Livre_Perc] AS VARCHAR), '-') AS [EspacoLivre%],
		           ISNULL(CAST([MaxSize] AS VARCHAR), '-') AS [MAXSIZE],
		           ISNULL(CAST([Growth] AS VARCHAR), '-') AS [Growth]
            FROM[dbo].[CheckArquivosDados]
                ORDER BY CAST(REPLACE([Total_Reservado], '-', 0) AS NUMERIC(15, 2)) DESC,
			         CAST(REPLACE([Total_Utilizado], '-', 0) AS NUMERIC(15, 2)) DESC;
	
	        -- Check Arquivos Logs
            IF(OBJECT_ID('tempdb..##CheckArquivosLog') IS NOT NULL)
                DROP TABLE ##CheckArquivosLog;
	        CREATE TABLE ##CheckArquivosLog
	            ([NomeDatabase]       NVARCHAR(50),
	            [LogicalName] NVARCHAR(50),
	            [TotalReservado] NVARCHAR(50),
	            [TotalUtilizado] NVARCHAR(50),
	            [EspacoLivre(MB)] NVARCHAR(50),
	            [EspacoLivre(%)] NVARCHAR(50),
	            [MaxSize] NVARCHAR(50),
	            [Growth] NVARCHAR(50)
	        );
	        
            INSERT INTO ##CheckArquivosLog
            SELECT TOP 5 [Nm_Database],
			        ISNULL([Logical_Name], '-') AS[LogicalName],
			        ISNULL(CAST([Total_Reservado] AS VARCHAR), '-') AS[TotalReservado],
			        ISNULL(CAST([Total_Utilizado] AS VARCHAR), '-') AS[TotalUtilizado],
			        ISNULL(CAST([Espaco_Livre_MB] AS VARCHAR), '-') AS[EspacoLivre(MB)],
			        ISNULL(CAST([Espaco_Livre_Perc] AS VARCHAR), '-') AS[EspacoLivre(%)],
			        ISNULL(CAST([MaxSize] AS VARCHAR), '-') AS[MAXSIZE],
			        ISNULL(CAST([Growth] AS VARCHAR), '-') AS[Growth]
            FROM[dbo].[CheckArquivosLog]
                ORDER BY CAST(REPLACE([Total_Reservado], '-', 0) AS NUMERIC(15, 2)) DESC,
	                 CAST(REPLACE([Total_Utilizado], '-', 0) AS NUMERIC(15, 2)) DESC;

	        -- Check Database Growth
            IF(OBJECT_ID('tempdb..##CheckDatabaseGrowth') IS NOT NULL)
		        DROP TABLE ##CheckDatabaseGrowth;
	        
            SELECT*
            INTO ##CheckDatabaseGrowth
	        FROM
            (
                SELECT TOP 30 [Nm_Servidor] AS NomeServidor,
                    [Nm_Database] AS NomeDatabase,
                    ISNULL(CAST([Tamanho_Atual] AS VARCHAR), '-') AS[TamanhoAtual],
			        ISNULL(CAST([Cresc_1_dia] AS VARCHAR), '-') AS[Cresc_1_dia],
			        ISNULL(CAST([Cresc_15_dia] AS VARCHAR), '-') AS[Cresc_15_dia],
			        ISNULL(CAST([Cresc_30_dia] AS VARCHAR), '-') AS[Cresc_30_dia],
			        ISNULL(CAST([Cresc_60_dia] AS VARCHAR), '-') AS[Cresc_60_dia]
                FROM[dbo].[CheckDatabaseGrowth]
                WHERE[Nm_Servidor] IS NOT NULL		-- REGISTROS NORMAIS
                UNION
                SELECT[Nm_Servidor] AS NomeServidor,
			        [Nm_Database] AS NomeDatabase,
			        ISNULL(CAST([Tamanho_Atual] AS VARCHAR), '-') AS[TamanhoAtual],
			        ISNULL(CAST([Cresc_1_dia] AS VARCHAR), '-') AS[Cresc_1_dia],
			        ISNULL(CAST([Cresc_15_dia] AS VARCHAR), '-') AS[Cresc_15_dia],
			        ISNULL(CAST([Cresc_30_dia] AS VARCHAR), '-') AS[Cresc_30_dia],
			        ISNULL(CAST([Cresc_60_dia] AS VARCHAR), '-') AS[Cresc_60_dia]
                FROM[dbo].[CheckDatabaseGrowth]
                WHERE[Nm_Servidor] IS NULL			-- TOTAL GERAL
	        ) AS T;

	        -- Check Database Growth Tabela
            IF(OBJECT_ID('tempdb..##CheckTableGrowthTb') IS NOT NULL)
		        DROP TABLE ##CheckTableGrowthTb;
	       
            SELECT*
            INTO ##CheckTableGrowthTb
	        FROM
            (
                SELECT TOP 30 [Nm_Servidor] AS NomeServidor,
                    [Nm_Database] AS NomeDatabase,
                    ISNULL([Nm_Tabela], '-') AS[NomeTabela],
			        ISNULL(CAST([Tamanho_Atual] AS VARCHAR), '-') AS[TamanhoAtual],
			        ISNULL(CAST([Cresc_1_dia] AS VARCHAR), '-') AS[Cresc_1_dia],
			        ISNULL(CAST([Cresc_15_dia] AS VARCHAR), '-') AS[Cresc_15_dia],
			        ISNULL(CAST([Cresc_30_dia] AS VARCHAR), '-') AS[Cresc_30_dia],
			        ISNULL(CAST([Cresc_60_dia] AS VARCHAR), '-') AS[Cresc_60_dia]
                FROM[dbo].[CheckTableGrowthEmail]
                WHERE[Nm_Servidor] IS NOT NULL		-- REGISTROS NORMAIS
                UNION ALL
                SELECT[Nm_Servidor] AS NomeServidor,
                    [Nm_Database] AS NomeDatabase,
			        ISNULL([Nm_Tabela], '-') AS[NomeTabela],
			        ISNULL(CAST([Tamanho_Atual] AS VARCHAR), '-') AS[TamanhoAtual],
			        ISNULL(CAST([Cresc_1_dia] AS VARCHAR), '-') AS[Cresc_1_dia],
			        ISNULL(CAST([Cresc_15_dia] AS VARCHAR), '-') AS[Cresc_15_dia],
			        ISNULL(CAST([Cresc_30_dia] AS VARCHAR), '-') AS[Cresc_30_dia],
			        ISNULL(CAST([Cresc_60_dia] AS VARCHAR), '-') AS[Cresc_60_dia]
                FROM[dbo].[CheckTableGrowthEmail]
                WHERE[Nm_Servidor] IS NULL			-- TOTAL GERAL
	        ) AS TB;
	
			-- Alteracoes DB top 5 
            IF(OBJECT_ID('tempdb..##ServerAudi') IS NOT NULL)
				DROP TABLE ##ServerAudi;
	        CREATE TABLE ##ServerAudi
    	        ([DataEvento]       NVARCHAR(50),
	            [serverInstanceName] NVARCHAR(50),
	            [DatabaseName] NVARCHAR(50),
	            [ActionId] NVARCHAR(50),
			    [Session] NVARCHAR(50),
	            [statement] VARCHAR(MAX)
	        );
            
            BEGIN
			 INSERT INTO ##ServerAudi ([DataEvento] ,[serverInstanceName],[DatabaseName],[ActionId],[Session],[statement])
			 select top 10 [DataEvento] ,[serverInstanceName],[DatabaseName],[ActionId],[Session],[statement] from [dbo].[ServerAudi] 
					where CAST(DataEvento AS DATE) = CONVERT(VARCHAR(10),GETDATE()-1,112)

		     IF (@@ROWCOUNT = 0)
              BEGIN
                INSERT INTO ##ServerAudi ([DataEvento] ,[serverInstanceName],[DatabaseName],[ActionId],[Session],[statement])
                SELECT GETDATE(),'Sem registro de Alteraçoes nas bases', NULL, NULL, NULL, NULL
              END
            END
	        
            -- Check Utilizacao Arquivo Writes
            IF(OBJECT_ID('tempdb..##CheckUtilizacaoArquivoWrites') IS NOT NULL)
		        DROP TABLE ##CheckUtilizacaoArquivoWrites;

            SELECT *
            INTO ##CheckUtilizacaoArquivoWrites
	        FROM
            (
                SELECT TOP 10 Nm_Database AS NomeDatabase,
                    ISNULL(CAST(file_id AS VARCHAR), '-') AS file_id,
                    ISNULL(CAST(io_stall_write_ms AS VARCHAR), '-') AS io_stall_write_ms,
                    ISNULL(CAST(num_of_writes AS VARCHAR), '-') AS num_of_writes,
                    ISNULL(CAST([avg_write_stall_ms] AS VARCHAR), '-') AS[avg_write_stall_ms]
                FROM[dbo].[CheckUtilizacaoArquivoWrites]
	        ) AS TBW

	        -- Check Utilizacao Arquivo Reads
            IF(OBJECT_ID('tempdb..##CheckUtilizacaoArquivoReads') IS NOT NULL)
		        DROP TABLE ##CheckUtilizacaoArquivoReads;

            SELECT*
            INTO ##CheckUtilizacaoArquivoReads
	        FROM
            (
                SELECT TOP 10 Nm_Database AS NomeDatabase,
                    ISNULL(CAST(file_id AS VARCHAR), '-') AS file_id,
                    ISNULL(CAST(io_stall_read_ms AS VARCHAR), '-') AS io_stall_read_ms,
                    ISNULL(CAST(num_of_reads AS VARCHAR), '-') AS num_of_reads,
                    ISNULL(CAST([avg_read_stall_ms] AS VARCHAR), '-') AS[avg_read_stall_ms]
                FROM[dbo].[CheckUtilizacaoArquivoReads]
	        ) AS TBR;


	        -- Check Historio de Backup
			IF(OBJECT_ID('tempdb..##Hist_DB_Backup') IS NOT NULL)
                DROP TABLE ##Hist_DB_Backup;

            SELECT*
            INTO ##Hist_DB_Backup
	        FROM
            (

                            SELECT TOP 20
									ISNULL([Servidor], '-')		AS[Servidor],
									ISNULL([Banco], '-')		AS[Banco],
									ISNULL([UltimoFull], '-')		AS[UltimoFull],
									[DataFull],
									ISNULL([TamanhoFull_MB], '-')	AS[TamanhoFull_MB],
									ISNULL([UltimoDiff], '-')		AS[UltimoDiff],
									[DataDiff],
									ISNULL([UltimoFullDiff], '-')	AS[UltimoFullDiff],
									ISNULL([TamanhoDiff_MB], '-')	AS[TamanhoDiff_MB],
									ISNULL([UltimoLog_Min], '-')	AS[UltimoLog_Min],
									[DataLog],
									ISNULL([TamanhoLog_MB], '-')	AS[TamanhoLog_MB]
                            FROM [dbo].[CheckDatabasesHistoricoBackup] WHERE [Banco] NOT IN ('master','model','msdb','tempdb')
                            ORDER BY [Banco]	
	        ) AS TBBA

            -- Check Databases Sem Backup
            IF(OBJECT_ID('tempdb..##CheckDatabasesSemBackup') IS NOT NULL)
            DROP TABLE ##CheckDatabasesSemBackup;

            SELECT*
            INTO ##CheckDatabasesSemBackup
	        FROM
            (

                            SELECT TOP 10
                                    CASE
                                        WHEN    [Nm_Database] <> 'Sem registro de Databases Sem Backup nas últimas 16 horas.'
                                            THEN '<p style=color:red;>'+ [Nm_Database] +'</p>'
                                            ELSE '' 
                                    END --+ [Nm_Database]
                                    AS [NomeDatabase]
                            FROM [dbo].[CheckDatabasesSemBackup] WHERE Nm_Database NOT IN ('master','model','msdb','tempdb')
                            ORDER BY[Nm_Database]	
	        ) AS TBB

	        -- Check Backup Realizados
            IF(OBJECT_ID('tempdb..##CheckBackupsExecutados') IS NOT NULL)
		        DROP TABLE ##CheckBackupsExecutados;

            SELECT*
            INTO ##CheckBackupsExecutados
	        FROM
            (
                SELECT TOP 10 [Database_Name] AS[DatabaseName],
                              ISNULL(CONVERT(VARCHAR, [Backup_Start_Date], 120), '-') AS[BackupStartDate],
					          ISNULL(CAST([Tempo_Min] AS VARCHAR), '-') AS[TempoMin],
					          ISNULL(CAST([Recovery_Model] AS VARCHAR), '-') AS[RecoveryModel],
					          ISNULL(CASE[Type]
                                         WHEN 'D'
                                         THEN 'FULL'
                                         WHEN 'I'
                                         THEN 'Diferencial'
                                         WHEN 'L'
                                         THEN 'Log'
                                     END, '-') AS[Tipo],
					          ISNULL(CAST([Tamanho_MB] AS VARCHAR), '-') AS[TamanhoMB]
                FROM[dbo].[CheckBackupsExecutados]
                ORDER BY CAST(ABS(REPLACE([Tamanho_MB], '-', 0)) AS NUMERIC(15, 2)) DESC
	        ) AS TBKP;

	        -- Queries em Execução
            IF(OBJECT_ID('tempdb..##CheckQueriesRunning') IS NOT NULL)
		        DROP TABLE ##CheckQueriesRunning;

            SELECT*
            INTO ##CheckQueriesRunning
	        FROM
            (
                SELECT TOP 5 ISNULL([dd hh:mm:ss.mss], '-') AS[DD HH:MM:SS.MSS],
					         [database_name] AS[DatabaseName],
					         ISNULL([login_name], '-') AS[LoginName],
					         ISNULL([host_name], '-') AS[HostName],
					         ISNULL(CONVERT(VARCHAR(20), [Start_time], 120), '-') AS[StartTime],
					         ISNULL([status], '-') AS[Status],
					         ISNULL(CAST([session_id] AS VARCHAR), '-') AS[SessionID],
					         ISNULL(CAST([blocking_session_id] AS VARCHAR), '-') AS[BlockingSessionID],
					         ISNULL([wait_info], '-') AS[WaitInfo],
					         ISNULL(CAST([open_tran_count] AS VARCHAR), '-') AS[OpenTranCount],
					         ISNULL(CAST([CPU] AS VARCHAR), '-') AS[CPU],
					         ISNULL(CAST([reads] AS VARCHAR), '-') AS[Reads],
					         ISNULL(CAST([writes] AS VARCHAR), '-') AS[Writes],
					         ISNULL(SUBSTRING(CAST([sql_command] AS VARCHAR), 1, 150), '-') AS[SQLCommand]
                FROM[dbo].[CheckQueriesRunning]
                ORDER BY[start_time]
	        ) AS TBKP;

	        -- Check Jobs Running
            IF(OBJECT_ID('tempdb..##CheckJobsRunning') IS NOT NULL)
		        DROP TABLE ##CheckJobsRunning;

            SELECT*
            INTO ##CheckJobsRunning
	        FROM
            (
                            SELECT TOP 10

                                    [Nm_JOB] AS NomeJob,
                                    ISNULL(CONVERT(VARCHAR(16), [Dt_Inicio],120), '-')	AS[DataInicio], 
							        ISNULL(Qt_Duracao, '-')                             AS[QtdDuracao], 
							        ISNULL([Nm_Step], '-')                              AS[NomeStep]
                            FROM[dbo].[CheckJobsRunning]
                ORDER BY[Dt_Inicio]
	        ) AS Job;

	        -- Check Jobs Alterados
            IF(OBJECT_ID('tempdb..##CheckAlteracaoJobs') IS NOT NULL)
		        DROP TABLE ##CheckAlteracaoJobs;

            SELECT*
            INTO ##CheckAlteracaoJobs
	        FROM
            (
                SELECT TOP 10 [Nm_Job] AS[NomeJob],
                              ISNULL(CASE[Fl_Habilitado]
                                         WHEN 1

                                         THEN 'SIM'

                                         WHEN 0

                                         THEN 'NÃO'

                                     END, '-') AS[Habilitado],
					          ISNULL(CONVERT(VARCHAR, [Dt_Criacao], 120), '-') AS[DataCriacao],
					          ISNULL(CONVERT(VARCHAR, [Dt_Modificacao], 120), '-') AS[DataModificacao],
					          ISNULL(CAST([Nr_Versao] AS VARCHAR), '-') AS[NumeroVersao]
                FROM[dbo].[CheckAlteracaoJobs]
                ORDER BY[Dt_Modificacao] DESC
	        ) AS JobA;

	        -- Jobs que Falharam
            IF(OBJECT_ID('tempdb..##CheckJobsFailed') IS NOT NULL)
		        DROP TABLE ##CheckJobsFailed;
	        
            SELECT*
            INTO ##CheckJobsFailed
	        FROM
            (
                            SELECT TOP 10
                                    [Job_Name] AS[NomeJob],
                                    ISNULL([Status], '-')                               AS[Status], 
							        ISNULL(CONVERT(VARCHAR, [Dt_Execucao], 120), '-')	AS[DataExecucao], 
							        ISNULL([Run_Duration], '-')                         AS[RunDuration], 
							        ISNULL([SQL_Message], '-')                          AS[SQLMessage]
                            FROM[dbo].[CheckJobsFailed]
                ORDER BY[Dt_Execucao] DESC
	        ) AS JobF;

	        -- Jobs Demorados
            IF(OBJECT_ID('tempdb..##CheckJobDemorados') IS NOT NULL)
		        DROP TABLE ##CheckJobDemorados;

            SELECT*
            INTO ##CheckJobDemorados
	        FROM
            (
                SELECT TOP 10 [Job_Name] [NomeJob],
                              ISNULL([Status], '-') AS[Status],
				              ISNULL(CONVERT(VARCHAR, [Dt_Execucao], 120), '-') AS[DataExecucao],
				              ISNULL([Run_Duration], '-') AS[RunDuration],
				              ISNULL([SQL_Message], '-') AS[SQLMessage]
                FROM[dbo].[CheckJobDemorados]
                    ORDER BY[Run_Duration] DESC
	        ) AS JobD;

	        -- Queries Demoradas Dia Anterior(07:00 - 23:00)
            IF(OBJECT_ID('tempdb..##CheckDBControllerQueries') IS NOT NULL)
		        DROP TABLE ##CheckDBControllerQueries;

            SELECT*
            INTO ##CheckDBControllerQueries
	        FROM
            (
            SELECT[dbo].[fncRetiraCaractereInvalidoXML]([PrefixoQuery]) AS [PrefixoQuery],
                                    ISNULL(CAST([QTD] AS VARCHAR), '-')						AS[QTD],
							        ISNULL(CAST([Total] AS VARCHAR), '-')						AS[Total],
							        ISNULL(CAST([Media] AS VARCHAR), '-')						AS[Media],
							        ISNULL(CAST([Menor] AS VARCHAR), '-')						AS[Menor],
							        ISNULL(CAST([Maior] AS VARCHAR), '-')						AS[Maior],
							        ISNULL(CAST([Writes] AS VARCHAR), '-')						AS[Writes],
							        ISNULL(CAST([CPU] AS VARCHAR), '-')						AS[CPU],
							        ISNULL(CAST([Reads] AS VARCHAR), '-')						AS[Reads],
							        [Ordem]
                FROM[dbo].[CheckDBControllerQueries]
	        ) AS Qu;

	        -- Quantidade de Queries Demoradas dos Últimos 10 Dias(07:00 - 23:00)
            IF(OBJECT_ID('tempdb..##CheckDBControllerQueriesGeral') IS NOT NULL)
		        DROP TABLE ##CheckDBControllerQueriesGeral;

            SELECT*
            INTO ##CheckDBControllerQueriesGeral
	        FROM
            (
                SELECT[Data],
                       ISNULL(CAST([QTD] AS VARCHAR), '-') AS[Quantidade]
                FROM[dbo].[CheckDBControllerQueriesGeral]
	        ) AS Qum;

	        -- Média Contadores Dia Anterior(07:00 - 23:00)
            IF(OBJECT_ID('tempdb..##CheckContadores') IS NOT NULL)
		        DROP TABLE ##CheckContadores;

            SELECT*
            INTO ##CheckContadores
	        FROM
            (
                             SELECT[Hora],
                                    [BatchRequests],
                                    [CPU],
                                    [Page_Life_Expectancy] AS[PageLifeExpectancy],
                                    [User_Connection] AS[UserConnection],
                                    [Qtd_Queries_Lentas] AS[QtdQueriesLentas],
                                    [Reads_Queries_Lentas] AS [Reads_Queries_Lentas]
                            FROM [dbo].[CheckContadoresEmail]
            ) AS Qum;

	        -- Conexões Abertas por Usuários
            IF(OBJECT_ID('tempdb..##CheckConexaoAberta') IS NOT NULL)
		        DROP TABLE ##CheckConexaoAberta;

            SELECT*
            INTO ##CheckConexaoAberta
	        FROM
            (
                SELECT --Nr_Ordem AS Ordem,
                       ISNULL([login_name], '-') AS[LoginName],
			           CAST([session_count] AS VARCHAR) AS[SessionCount]
                FROM[dbo].[CheckConexaoAberta_Email]
	        ) AS CA;


	        -- Fragmentação dos Índices - Top 10
	        IF(OBJECT_ID('tempdb..##CheckFragmentacaoIndices') IS NOT NULL)
		        DROP TABLE ##CheckFragmentacaoIndices;
	        
            SELECT*
            INTO ##CheckFragmentacaoIndices
	        FROM
            (
                SELECT TOP 10 ISNULL(CONVERT(VARCHAR, [Dt_Referencia], 120), '-') AS[DataReferencia],
					          [Nm_Database]
                AS NomeDatabase,
                              ISNULL([Nm_Tabela], '-') AS[NomeTabela],
					          ISNULL([Nm_Indice], '-') AS[NomeIndice],
					          ISNULL(CAST([Avg_Fragmentation_In_Percent] AS VARCHAR), '-') AS[AvgFragmentationInPercent],
					          ISNULL(CAST([Page_Count] AS VARCHAR), '-') AS[PageCount],
					          ISNULL(CAST([Fill_Factor] AS VARCHAR), '-') AS[FillFactor],
					          ISNULL(CASE[Fl_Compressao]
                                         WHEN 0
                                         THEN 'Sem Compressão'
                                         WHEN 1
                                         THEN 'Compressão de Linha'
                                         WHEN 2
                                         THEN 'Compressao de Página'
                                     END, '-') AS[Compressao]
                FROM[dbo].[CheckFragmentacaoIndices]
                ORDER BY CAST(REPLACE([Avg_Fragmentation_In_Percent], '-', 0) AS NUMERIC(15, 2)) DESC
	        ) AS IND;

	        -- Waits Stats Dia Anterior 07:00 - 23:00 - Top 10
	        IF(OBJECT_ID('tempdb..##CheckWaitsStats') IS NOT NULL)
		        DROP TABLE ##CheckWaitsStats;
	        
            SELECT*
            INTO ##CheckWaitsStats
	        FROM
            (
                SELECT TOP 10 [WaitType],
                              ISNULL(CONVERT(VARCHAR, [Max_Log], 120), '-') AS[MaxLog],
					          ISNULL(CAST([DIf_Wait_S] AS VARCHAR), '-') AS[DIf_Wait_S],
					          ISNULL(CAST([DIf_Resource_S] AS VARCHAR), '-') AS[DIf_Resource_S],
					          ISNULL(CAST([DIf_Signal_S] AS VARCHAR), '-') AS[DIf_Signal_S],
					          ISNULL(CAST([DIf_WaitCount] AS VARCHAR), '-') AS[DIf_WaitCount],
					          ISNULL(CAST([Last_Percentage] AS VARCHAR), '-') AS[LastPercentage]
                FROM[dbo].[CheckWaitsStats]
                ORDER BY CAST(REPLACE([DIf_Wait_S], '-', 0) AS NUMERIC(15, 2)) DESC
	        ) AS WT;
		
	        -- Alertas Sem Solução
            IF(OBJECT_ID('tempdb..##CheckAlertaSemClear') IS NOT NULL)
		        DROP TABLE ##CheckAlertaSemClear;
	        
            SELECT*
            INTO ##CheckAlertaSemClear
	        FROM
            (
                            SELECT[Nm_Alerta] AS NomeAlerta,
                                    ISNULL([Ds_Mensagem], '-') AS[Mensagem],
							        ISNULL(CONVERT(VARCHAR, [Dt_Alerta], 120), '-') AS[DataAlerta],
							        ISNULL([Run_Duration], '-') AS[Duration]
                            FROM[dbo].[CheckAlertaSemClear]
	        ) AS AlertS;

	        -- Alertas do Dia Anterior TOP 40 
	        IF(OBJECT_ID('tempdb..##CheckAlerta') IS NOT NULL)
		        DROP TABLE ##CheckAlerta;
	        
            SELECT*
            INTO ##CheckAlerta
	        FROM
            (
                SELECT TOP 40 [Nm_Alerta] AS NomeAlerta,
                              ISNULL([Ds_Mensagem], '-') AS[Mensagem],
					          ISNULL(CONVERT(VARCHAR, [Dt_Alerta], 120), '-') AS[DataAlerta],
					          ISNULL([Run_Duration], '-') AS[Duration]
                FROM[dbo].[CheckAlerta]
                ORDER BY[Dt_Alerta] DESC
	        ) AS AlertSS;

	        -- Login Failed - SQL Server - TOP 10 
	        IF(OBJECT_ID('tempdb..##CheckSQLServerLoginFailed') IS NOT NULL)
		        DROP TABLE ##CheckSQLServerLoginFailed;
	        
            SELECT*
            INTO ##CheckSQLServerLoginFailed
	        FROM
            (
                SELECT TOP 10 

                           ISNULL(CAST([Nr_Ordem] AS VARCHAR), '-') AS[Ordem],
					          [Text],
					          ISNULL(CAST([Qt_Erro] AS VARCHAR), '-') AS[QtdErro]
                FROM[dbo].[CheckSQLServerLoginFailedEmail]
                ORDER BY CAST(REPLACE([Qt_Erro], '-', 0) AS INT) DESC
	        ) AS Lo;

	        -- LError Log do SQL Server - TOP 30 
	        IF(OBJECT_ID('tempdb..##CheckSQLServerErrorLog') IS NOT NULL)
		        DROP TABLE ##CheckSQLServerErrorLog;
	        
            SELECT*
            INTO ##CheckSQLServerErrorLog
	        FROM
            (
                SELECT TOP 30 ISNULL(CONVERT(VARCHAR, [Dt_Log], 120), '-') AS[DataLog],
					           ISNULL([ProcessInfo], '-') AS[ProcessInfo],
					           [Text]
                FROM[dbo].[CheckSQLServerErrorLog]
                ORDER BY[Dt_Log] DESC
	        ) AS SLO;

	        -- Login SQL Server 
            IF(OBJECT_ID('tempdb..##CheckSQLServerLogin') IS NOT NULL)
                DROP TABLE ##CheckSQLServerLogin;
            WITH LOGIN_AUDIT
                 AS (SELECT DISTINCT 
                            CONVERT(VARCHAR, s.login_time, 111) EventDate, 
                            DB_NAME(s.database_id) [DataBase], 
                            s.original_login_name AS LoginName, 
                            s.host_name AS HostName, 
                            c.client_net_address AS [Address],
                            CASE
                                WHEN s.program_name LIKE 'Microsoft SQL Server Management Studio%'
                                THEN 'Microsoft SQL Server Management Studio'
                                ELSE s.program_name
                            END [Application]
                     FROM sys.dm_exec_sessions s
                          JOIN sys.dm_exec_connections AS c ON c.session_id = s.session_id
                     WHERE s.is_user_process = 1
                     GROUP BY s.login_time, 
                              s.database_id, 
                              s.original_login_name, 
                              s.host_name, 
                              s.program_name, 
                              s.client_interface_name, 
                              c.client_net_address)
                 SELECT *
                 INTO ##CheckSQLServerLogin
                 FROM
                 (
                     SELECT top 10000 * 
                     FROM LOGIN_AUDIT
                     WHERE EventDate = CONVERT(VARCHAR, GETDATE(), 111)
                           AND LoginName <> HostName
                           AND LoginName NOT LIKE '%SEDE%'
                           AND LoginName NOT LIKE '%enota%'
                     ORDER BY EventDate DESC
                 ) AS LAU;

	        /*******************************************************************************************************************************
	        --	CRIA O EMAIL - ALERTA
	        *******************************************************************************************************************************/

            SET @Subject = @Ds_Email_Assunto + ' '+ @@SERVERNAME + ' - ' + @Empresa 
	        SET @TextRel1 = @Ds_Email_Texto  + @Check_Disponibilidade	
	        SET @CaminhoFim = @Ds_Caminho_Base + @CaminhoPath + '_' + @NomeRel + '.html'

            -- Gera Primeiro bloco de HTML
            SET @Query = 'SELECT * FROM [##DisponibilidadeSQL]'
	        SET @HTML = dbo.fncExportaMultiHTML(@Query, @TextRel1, 2, 0)
            -- Gera quinto bloco de HTML
            SET @TextRel5 =  @Check_CrescimentoBases	
	        SET @Query = 'SELECT * FROM [##CheckDatabaseGrowth]'
            SET @HTML += dbo.fncExportaMultiHTML(@Query, @TextRel5, 2, 1) 
	        -- Gera sexto bloco de HTML
            SET @TextRel6 =  @Check_CrescimentoTabelas	
	        SET @Query = 'SELECT [NomeServidor],CASE WHEN [NomeDatabase] = ''TOTAL GERAL''	THEN ''<h5><b>'' +[NomeDatabase]+ ''</b></h5>'' ELSE [NomeDatabase]   END AS [NomeDatabase],[NomeTabela],[TamanhoAtual],[Cresc_1_dia],[Cresc_15_dia],[Cresc_30_dia],[Cresc_60_dia]  FROM [##CheckTableGrowthTb]'
            SET @HTML += dbo.fncExportaMultiHTML(@Query, @TextRel6, 2, 1) 
	        -- Gera Segundo bloco de HTML -- ARRUMA ISSO LOGO
            SET @TextRel26 =  @Check_AlteracaoInstancia	
	        SET @Query = 'select CAST(DataEvento AS DATE) DataEvento ,[serverInstanceName],[DatabaseName],[ActionId],[Session],[statement] from ##ServerAudi where CAST(DataEvento AS DATE) = CONVERT(VARCHAR(10),GETDATE()-1,112)'
            SET @HTML += dbo.fncExportaMultiHTML(@Query, @TextRel26, 2, 1)  
	        -- Gera terceiro bloco de HTML
            SET @TextRel3 =  @Check_InfoArquivoDados	
	        SET @Query = 'SELECT * FROM [##CheckArquivosDados]'
            SET @HTML += dbo.fncExportaMultiHTML(@Query, @TextRel3, 2, 1) 
	        -- Gera quarto bloco de HTML
            SET @TextRel4 =  @Check_InfoArquivolog	
	        SET @Query = 'SELECT * FROM [##CheckArquivosLog]'
            SET @HTML += dbo.fncExportaMultiHTML(@Query, @TextRel4, 2, 1) 
	        -- Gera sétimo bloco de HTML
            SET @TextRel7 =  @Check_UtilizacaoArquivosWrites	
	        SET @Query = 'SELECT * FROM [##CheckUtilizacaoArquivoWrites]'
            SET @HTML += dbo.fncExportaMultiHTML(@Query, @TextRel7, 2, 1) 
	        -- Gera Oitavo bloco de HTML
            SET @TextRel8 =  @Check_UtilizacaoArquivosReads	
	        SET @Query = 'SELECT * FROM [##CheckUtilizacaoArquivoReads]'
            SET @HTML += dbo.fncExportaMultiHTML(@Query, @TextRel8, 2, 1) 
	        -- Gera Vigésimo Nono bloco de HTML
            SET @TextRel27 =  @Check_HistBKP	
	        SET @Query = 'SELECT TOP 20 * FROM [##Hist_DB_Backup]'
            SET @HTML += dbo.fncExportaMultiHTML(@Query, @TextRel27, 2, 1) 
            -- Gera Nono bloco de HTML
            SET @TextRel9 =  @Check_SemBKP	
	        SET @Query = 'SELECT * FROM [##CheckDatabasesSemBackup]'
            SET @HTML += dbo.fncExportaMultiHTML(@Query, @TextRel9, 2, 1) 
	        -- Gera Decimo bloco de HTML
            SET @TextRel10 =  @Check_BKP_DB	
	        SET @Query = 'SELECT * FROM [##CheckBackupsExecutados]'
            SET @HTML += dbo.fncExportaMultiHTML(@Query, @TextRel10, 2, 1)
	         -- Gera Vigésimo bloco de HTML
            SET @TextRel20 =  @Check_FragIndice	
	        SET @Query = 'SELECT *  FROM [##CheckFragmentacaoIndices]'
            SET @HTML += dbo.fncExportaMultiHTML(@Query, @TextRel20, 2, 1)
            -- Gera Decimo Sexto bloco de HTML
            SET @TextRel16 = @Check_QueryDemorada
            SET @Query = 'SELECT * FROM ##CheckDBControllerQueries'
            SET @HTML += dbo.fncExportaMultiHTML(@Query, @TextRel16, 2, 1)  
	        -- Gera Decimo Primeiro bloco de HTML
            SET @TextRel11 = @Check_QueryExec
            SET @Query = 'SELECT * FROM ##CheckQueriesRunning'
            SET @HTML += dbo.fncExportaMultiHTML(@Query, @TextRel11, 2, 1)  
	        -- Gera Decimo Setimo bloco de HTML
            SET @TextRel17 = @Check_QtdQueryExec
            SET @Query = 'SELECT * FROM ##CheckDBControllerQueriesGeral'
            SET @HTML += dbo.fncExportaMultiHTML(@Query, @TextRel17, 2, 1)
            -- Gera Vigésimo Quinto bloco de HTML
            SET @TextRel25 = @Check_ErroLog
            SET @Query = 'SELECT *  FROM [##CheckSQLServerErrorLog]'
            SET @HTML += dbo.fncExportaMultiHTML(@Query, @TextRel25, 2, 1)
            -- Gera Decimo Segundo bloco de HTML
            SET @TextRel12 = @Check_JOB_EXEC
            SET @Query = 'SELECT * FROM [##CheckJobsRunning]'
            SET @HTML += dbo.fncExportaMultiHTML(@Query, @TextRel12, 2, 1) 
	        -- Gera Decimo Terceiro bloco de HTML
            SET @TextRel13 = @Check_JOB_Alterados
            SET @Query = 'SELECT * FROM [##CheckAlteracaoJobs]'
            SET @HTML += dbo.fncExportaMultiHTML(@Query, @TextRel13, 2, 1)   
	        -- Gera Decimo Quarto bloco de HTML
            SET @TextRel14 = @Check_JOB_Falharam
            SET @Query = 'SELECT * FROM [##CheckJobsFailed]'
            SET @HTML += dbo.fncExportaMultiHTML(@Query, @TextRel14, 2, 1)
	        -- Gera Decimo Quinto bloco de HTML
            SET @TextRel15 = @Check_JOB_Demorados
            SET @Query = 'SELECT * FROM ##CheckJobDemorados'
            SET @HTML += dbo.fncExportaMultiHTML(@Query, @TextRel15, 2, 1)   
	        -- Gera Decimo Oitavo bloco de HTML
            SET @TextRel18 = @Check_Media_Contadores
            SET @Query = 'SELECT * FROM ##CheckContadores'
            SET @HTML += dbo.fncExportaMultiHTML(@Query, @TextRel18, 2, 1)  
	        -- Gera Decimo Nono bloco de HTML
            SET @TextRel19 = @Check_ConexoesUsuario
            SET @Query = 'SELECT CASE WHEN [LoginName] = ''TOTAL'' THEN ''<h5><b>'' +[LoginName]+ ''</b></h5>'' ELSE [LoginName] END AS [LoginName],[SessionCount]  FROM [##CheckConexaoAberta]'
            SET @HTML += dbo.fncExportaMultiHTML(@Query, @TextRel19, 2, 1)  
	        -- Gera Vigésimo Pirmeiro bloco de HTML
            SET @TextRel21 = @Check_WaitsStats
            SET @Query = 'SELECT *  FROM [##CheckWaitsStats]'
            SET @HTML += dbo.fncExportaMultiHTML(@Query, @TextRel21, 2, 1) 
	        -- Gera Vigésimo Segundo bloco de HTML
            SET @TextRel22 = @Check_AlertasSemSolucao
            SET @Query = 'SELECT *  FROM [##CheckAlertaSemClear]'
            SET @HTML += dbo.fncExportaMultiHTML(@Query, @TextRel22, 2, 1) 
	        -- Gera Vigésimo Terceiro bloco de HTML
            SET @TextRel23 = @Check_AlertasDiaAnterior
            SET @Query = 'SELECT *  FROM [##CheckAlerta]'
            SET @HTML += dbo.fncExportaMultiHTML(@Query, @TextRel23, 2, 1)
	        -- Gera Vigésimo Quarto bloco de HTML
            SET @TextRel24 = @Check_LoginFailed
            SET @Query = 'SELECT *  FROM [##CheckSQLServerLoginFailed]'
            SET @HTML += dbo.fncExportaMultiHTML(@Query, @TextRel24, 2, 1)
	        -- Gera Vigésimo Quinto bloco de HTML
            ---SET @TextRel24 = @Check_LoginFailed
            ---SET @Query = 'SELECT *  FROM [##CheckSQLServerLogin]'
            ---SET @HTML += dbo.fncExportaMultiHTML(@Query, @TextRel25, 2, 1)
	        -- Gera Segundo bloco de HTML
            SET @TextRel2 =  @Check_EspacoDisco	
	        SET @Query = 'SELECT * FROM [##CheckEspacoDisco]'
            SET @HTML += dbo.fncExportaMultiHTML(@Query, @TextRel2, 2, 1)
	        -- Gera ultimo bloco de HTML
	        select @HTML = @HTML + @Ass

            -- Salva Arquivo HTML de Envio
            EXEC dbo.stpWriteFile
                    @Ds_Texto = @HTML, -- nvarchar(max)
                    @Ds_Caminho = @CaminhoFim, -- nvarchar(max)
                    @Ds_Codificacao = N'UTF-8', -- nvarchar(max)
                    @Ds_Formato_Quebra_Linha = N'windows', -- nvarchar(max)
                    @Fl_Append = 0 -- bit

            /*******************************************************************************************************************************
	        --	ALERTA - ENVIA O EMAIL E MENSSAGEIROS
	        *******************************************************************************************************************************/
            IF EXISTS  (SELECT B.Ativo from AlertaParametro A 
			                INNER JOIN [dbo].[AlertaEnvio] B ON B.IdAlertaParametro = A.Id_AlertaParametro
			                WHERE B.Ativo = 1
			                AND B.Des LIKE '%Email'
			                AND [Id_AlertaParametro] = @Id_AlertaParametro
			                )
            BEGIN

                 EXEC [msdb].[dbo].[sp_send_dbmail]
                     @profile_name = @ProfileDBMail,
                     @recipients = @EmailDestination,
                     @body_format = @BodyFormatMail,
                     @subject = @Subject,
                     @importance = @Importance,
                     @body = @HTML;

            END
		 
	        -- Parametro Menssageiro
            SET @MntMsg = @Subject+', Verifique os detalhes no *E-Mail*'

            IF EXISTS  (SELECT B.Ativo from AlertaParametro A 
			            INNER JOIN [dbo].[AlertaEnvio] B ON B.IdAlertaParametro = A.Id_AlertaParametro
			            WHERE B.Ativo = 1
			            AND B.Des LIKE '%Telegram'
			            AND [Id_AlertaParametro] = @Id_AlertaParametro
			           )
            BEGIN
                 -- Envio do Telegram    
                 EXEC dbo.StpSendMsgTelegram 
                 @Destino = @CanalTelegram,
                 @Msg = @MntMsg
            END

            IF EXISTS  (SELECT B.Ativo from AlertaParametro A 
			             INNER JOIN [dbo].[AlertaEnvio] B ON B.IdAlertaParametro = A.Id_AlertaParametro
			             WHERE B.Ativo = 1
			             AND B.Des LIKE '%Teams'
			             AND [Id_AlertaParametro] = @Id_AlertaParametro
			             )
            BEGIN
                 -- MS TEAMS
                 SET @MntMsg = (select replace (@MntMsg, '\', '-'))
                 EXEC [dbo].[stpSendMsgTeams]
	             @msg = @MntMsg,
	             @channel = @Ds_Menssageiro_02,
                 @ap = @Teams
            END	

	            ";
        }
    }
}
