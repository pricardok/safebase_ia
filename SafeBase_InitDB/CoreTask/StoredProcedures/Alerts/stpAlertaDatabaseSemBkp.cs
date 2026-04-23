using System;
using System.Collections.Generic;
using System.Text;

namespace InitDB.Client
{
    class stpAlertaDatabaseSemBkp
    {
        public static string Query()
        {

            string NocheckBackup = ExecuteSql.ExecuteQuery("SELECT CASE WHEN IgnoraDatabase IS NULL THEN '''''' ELSE IgnoraDatabase END AS IgnoraDatabase FROM [dbo].[AlertaParametro] where Nm_Alerta = 'Database sem Backup'");
            //string NocheckBackup = "''";
            return
			// @"insert into [dbo].[Testedb] ([Nome],[DateTest]) values ('Teste da ferramenta DB - stpAlertaDatabaseSemBkp',GETDATE())";

			@"  SET NOCOUNT ON;

				SET QUOTED_IDENTIFIER ON;

	            -- Databases sem Backup
	            DECLARE @Id_AlertaParametro INT = (SELECT Id_AlertaParametro FROM [dbo].AlertaParametro (NOLOCK) WHERE Nm_Alerta = 'Database sem Backup' AND Ativo = 1)
                DECLARE @Ds_Caminho_Base VARCHAR(100) = (SELECT Ds_Caminho FROM [dbo].AlertaParametro (NOLOCK) WHERE Nm_Alerta = 'CheckList')
                DECLARE @Telegram INT = (select Id_AlertaParametro from AlertaParametro WHERE Nm_Alerta = 'Envia Telegram')
                DECLARE @Teams INT = (select Id_AlertaParametro from AlertaParametro WHERE Nm_Alerta = 'Envia Teams' and Id_AlertaParametro = 22)

	            -- Declara as variaveis
	            DECLARE @Database_Sem_Backup_Parametro INT, @EmailDestination VARCHAR(200),@Qtd_Databases_Total INT, @Qtd_Databases_Restore INT, 
			            @TextRel1 VARCHAR(4000), @TextRel2 VARCHAR(4000), @NomeRel VARCHAR(300),@MntMsg VARCHAR(200), @TLMsg VARCHAR(200), 
			            @SendMail VARCHAR(200), @ProfileDBMail VARCHAR(50), @BodyFormatMail VARCHAR(20), @CaminhoPath VARCHAR(50), 
			            @CaminhoFim VARCHAR(50), @Ass VARCHAR(4000),@HTML VARCHAR(MAX), @Query VARCHAR(MAX), @Importance AS VARCHAR(6), 
                        @Subject VARCHAR(500), @Ds_Email_Assunto_alerta VARCHAR (600), @Ds_Email_Assunto_solucao VARCHAR (600), 
                        @Ds_Email_Texto_alerta VARCHAR (600), @Ds_Email_Texto_solucao VARCHAR (600), @Ds_Menssageiro_01 VARCHAR (30), 
                        @Ds_Menssageiro_02 VARCHAR (30), @Ds_Menssageiro_03 VARCHAR (30)

	            -- Email, Parametro, Id Telegram, Caminho dos reports, Profile DB Mail, Body Format Mail 
	            SELECT @NomeRel = Nm_Alerta, 
		               @Database_Sem_Backup_Parametro = Vl_Parametro, 
		               @EmailDestination = Ds_Email, 
		               @TLMsg = Ds_MSG,
				       @Ds_Menssageiro_01 = A.Ds_Menssageiro_01,
				       @Ds_Menssageiro_02 = A.Ds_Menssageiro_02,
                       @Ds_Menssageiro_03 = A.Ds_Menssageiro_03,
		               @CaminhoPath = Ds_Caminho_Log, 
		               @ProfileDBMail = Ds_ProfileDBMail, 
		               @BodyFormatMail = Ds_BodyFormatMail,
		               @importance = Ds_TipoMail,
                       @Ds_Email_Assunto_solucao = B.SubjectSolution,
                       @Ds_Email_Texto_solucao = B.MailTextSolution,
                       @Ds_Email_Assunto_alerta = B.SubjectProblem,
                       @Ds_Email_Texto_alerta = B.MailTextProblem,
                       @Ass = C.Assinatura
	            FROM [dbo].[AlertaParametro] A
                INNER JOIN [dbo].[AlertaParametroMenssage] B ON A.Id_AlertaParametro = B.IdAlertaParametro
			    INNER JOIN [dbo].[MailAssinatura] C ON C.Id = A.IdMailAssinatura
	            WHERE [Id_AlertaParametro] = @Id_AlertaParametro	

               DECLARE @CanalTelegram VARCHAR(100) = (SELECT A.canal FROM [dbo].[AlertaMsgToken] A
                      INNER JOIN [dbo].AlertaParametro B ON A.Id = B.Ds_Menssageiro_01 where b.Ds_Menssageiro_01 = @Ds_Menssageiro_01 AND B.Id_AlertaParametro = @Telegram AND B.Ativo = 1) 

	            -- Verifica a Quantidade Total de Databases
	            IF ( OBJECT_ID('tempdb..#alerta_backup_databases_todas') IS NOT NULL )
		            DROP TABLE #alerta_backup_databases_todas

	            SELECT [name] AS [Nm_Database]
	            INTO #alerta_backup_databases_todas
	            FROM [sys].[databases]
	            WHERE [name] NOT IN (" + NocheckBackup + @") 
                AND state_desc <> 'OFFLINE'
				AND [name] NOT IN (SELECT 
									    ADC.database_name                               
								  FROM sys.availability_groups_cluster as AGC                                                                            
								  JOIN sys.dm_hadr_availability_replica_cluster_states as RCS ON AGC.group_id = RCS.group_id                             
								  JOIN sys.dm_hadr_availability_replica_states as ARS ON RCS.replica_id = ARS.replica_id and RCS.group_id = ARS.group_id 
								  JOIN sys.availability_databases_cluster as ADC ON AGC.group_id = ADC.group_id                                          
								  WHERE ARS.is_local = 1
								  AND ARS.role_desc LIKE 'SECONDARY')

	            SELECT @Qtd_Databases_Total = COUNT(*)
	            FROM #alerta_backup_databases_todas

	            -- Verifica a Quantidade de Databases que tiveram Backup nas ultimas 14 horas
	            IF ( OBJECT_ID('tempdb..#alerta_backup_databases_com_backup') IS NOT NULL)
		            DROP TABLE #alerta_backup_databases_com_backup

	            SELECT DISTINCT [database_name] AS [Nm_Database]
	            INTO #alerta_backup_databases_com_backup
	            FROM [msdb].[dbo].[backupset] B
	            JOIN [msdb].[dbo].[backupmediafamily] BF ON B.[media_set_id] = BF.[media_set_id]
	            WHERE	[backup_start_date] >= DATEADD(hh, -@Database_Sem_Backup_Parametro, GETDATE())
			            AND [type] IN ('D','I')
                        AND [database_name] NOT IN (SELECT 
												        ADC.database_name                               
											        FROM sys.availability_groups_cluster as AGC                                                                            
											        JOIN sys.dm_hadr_availability_replica_cluster_states as RCS ON AGC.group_id = RCS.group_id                             
											        JOIN sys.dm_hadr_availability_replica_states as ARS ON RCS.replica_id = ARS.replica_id and RCS.group_id = ARS.group_id 
											        JOIN sys.availability_databases_cluster as ADC ON AGC.group_id = ADC.group_id                                          
											        WHERE ARS.is_local = 1
											        AND ARS.role_desc LIKE 'SECONDARY')

	            SELECT @Qtd_Databases_Restore = COUNT(*) 
	            FROM #alerta_backup_databases_com_backup
	
	            /*******************************************************************************************************************************
	            --	Verifica se menos de 70 % das databases tiveram Backup
	            *******************************************************************************************************************************/
	            if(@Qtd_Databases_Restore < @Qtd_Databases_Total * 0.7)
	            BEGIN	
		            -- Databases que não tiveram Backup
		            IF ( OBJECT_ID('tempdb..##alerta_backup_databases_sem_backup') IS NOT NULL )
			            DROP TABLE ##alerta_backup_databases_sem_backup
		
		            SELECT A.[Nm_Database]
		            INTO ##alerta_backup_databases_sem_backup
		            FROM #alerta_backup_databases_todas A WITH(NOLOCK)
		            LEFT JOIN #alerta_backup_databases_com_backup B WITH(NOLOCK) ON A.[Nm_Database] = B.[Nm_Database]
		            WHERE B.[Nm_Database] IS NULL
		
		            /*******************************************************************************************************************************
		            --	CRIA O EMAIL - ALERTA
		            *******************************************************************************************************************************/
		
		            SET @Subject =	@Ds_Email_Assunto_alerta + ' ' +  CAST((@Database_Sem_Backup_Parametro) AS VARCHAR) + ' Horas no Servidor: ' + @@SERVERNAME
		            SET @TextRel1 = @Ds_Email_Texto_alerta	
		            SET @CaminhoFim = @Ds_Caminho_Base  + @CaminhoPath + @NomeRel +'.html'
			 
		            -- Gera Primeiro bloco de HTML
		            SET @Query = 'SELECT [Nm_Database] AS [Database] FROM ##alerta_backup_databases_sem_backup'
		            SET @HTML = dbo.fncExportaMultiHTML(@Query, @TextRel1, 2, 1)
		            -- Gera Segundo bloco de HTML
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

   		            /*******************************************************************************************************************************
		            -- Insere um Registro na Tabela de Controle dos Alertas -> Fl_Tipo = 1 : ALERTA
		            *******************************************************************************************************************************/		
		            INSERT INTO [dbo].[Alerta] ( [Id_AlertaParametro], [Ds_Mensagem], [Fl_Tipo] )
		            SELECT @Id_AlertaParametro, @Subject, 1
	            END
            ";
        }
    }
}
