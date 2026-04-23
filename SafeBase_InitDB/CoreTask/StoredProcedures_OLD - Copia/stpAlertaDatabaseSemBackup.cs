using System;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using Microsoft.SqlServer.Server;
public partial class StoredProcedures
{
    [Microsoft.SqlServer.Server.SqlProcedure]
    public static void stpAlertaDatabaseSemBackup()
    {
        // Create the command
        SqlCommand myCommand = new SqlCommand();
        myCommand.CommandText =
              @"
                SET NOCOUNT ON

	            -- Databases sem Backup
	            DECLARE @Id_AlertaParametro INT = (SELECT Id_AlertaParametro FROM [InitDB].[dbo].AlertaParametro (NOLOCK) WHERE Nm_Alerta = 'Database sem Backup')

	            -- Declara as variaveis
	            DECLARE @Database_Sem_Backup_Parametro INT, @EmailDestination VARCHAR(200),@Qtd_Databases_Total INT, @Qtd_Databases_Restore INT, 
			            @TextRel1 VARCHAR(4000), @TextRel2 VARCHAR(4000), @NomeRel VARCHAR(300),@MntMsg VARCHAR(200), @TLMsg VARCHAR(200), 
			            @SendMail VARCHAR(200), @ProfileDBMail VARCHAR(50), @BodyFormatMail VARCHAR(20), @CaminhoPath VARCHAR(50), 
			            @CaminhoFim VARCHAR(50), @Ass VARCHAR(4000),@HTML VARCHAR(MAX), @Query VARCHAR(MAX), @Importance AS VARCHAR(6), @Subject VARCHAR(500)
	
	            -- Email, Parametro, Id Telegram, Caminho dos reports, Profile DB Mail, Body Format Mail 
	            SELECT @NomeRel = Nm_Alerta, 
		              @Database_Sem_Backup_Parametro = Vl_Parametro, 
		              @EmailDestination = Ds_Email, 
		              @TLMsg = Ds_MSG, 
		              @CaminhoPath = Ds_Caminho, 
		              @ProfileDBMail = Ds_ProfileDBMail, 
		              @BodyFormatMail = Ds_BodyFormatMail,
		              @importance = Ds_TipoMail
	            FROM [dbo].[AlertaParametro]
	            WHERE [Id_AlertaParametro] = @Id_AlertaParametro	

	            -- Verifica a Quantidade Total de Databases
	            IF ( OBJECT_ID('tempdb..#alerta_backup_databases_todas') IS NOT NULL )
		            DROP TABLE #alerta_backup_databases_todas

	            SELECT [name] AS [Nm_Database]
	            INTO #alerta_backup_databases_todas
	            FROM [sys].[databases]
	            WHERE [name] NOT IN ('tempdb', 'ReportServerTempDB') AND state_desc <> 'OFFLINE'

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
		
		            SET @Subject =	'ALERTA #DatabasesSemBackup - Existem Databases sem Backup nas últimas ' +  CAST((@Database_Sem_Backup_Parametro) AS VARCHAR) + ' Horas no Servidor: ' + @@SERVERNAME
		            SET @TextRel1 = 'Prezados,<BR /><BR /> Identifiquei que existem Databases sem Backup nas últimas ' +  CAST((@Database_Sem_Backup_Parametro) AS VARCHAR) + ' Horas no Servidor <b>' + @@SERVERNAME +',</b> verifique essa informação com urgência.'	
		            SET @CaminhoFim = @CaminhoPath + @NomeRel +'.html'
			 
		            -- Gera Primeiro bloco de HTML
		            SET @Query = 'SELECT [Nm_Database] AS [Database] FROM ##alerta_backup_databases_sem_backup'
		            SET @HTML = dbo.fncExportaMultiHTML(@Query, @TextRel1, 2, 1)
		            -- Gera Segundo bloco de HTML
		            SET @Ass = (SELECT Assinatura FROM MailAssinatura WHERE Id = 1)
		            select @HTML = @HTML + @Ass
		            -- Salva Arquivo HTML de Envio
		            EXEC dbo.stpEscreveArquivo 
				            @Ds_Texto = @HTML, -- nvarchar(max)
				            @Ds_Caminho = @CaminhoFim, -- nvarchar(max)
				            @Ds_Codificacao = N'UTF-8', -- nvarchar(max)
				            @Ds_Formato_Quebra_Linha = N'windows', -- nvarchar(max)
				            @Fl_Append = 0 -- bit

		            /*******************************************************************************************************************************
		            --	ALERTA - ENVIA O EMAIL - ENVIA TELEGRAM
		            *******************************************************************************************************************************/	
		            EXEC [msdb].[dbo].[sp_send_dbmail]
						                @profile_name = @ProfileDBMail,
						                @recipients = @EmailDestination,
						                @body_format = @BodyFormatMail,
						                @subject = @Subject,
						                @importance = @Importance,
						                @body = @HTML;
			 
		            -- Envio do Telegram		
		            SET @MntMsg = @Subject+', Verifique os detalhes no e-mail com *urgência*'
		            EXEC dbo.StpSendMsgTelegram 
			                @Destino = @TLMsg,
			                --@Destino = '49353855', 
			                @Msg = @MntMsg

   		            /*******************************************************************************************************************************
		            -- Insere um Registro na Tabela de Controle dos Alertas -> Fl_Tipo = 1 : ALERTA
		            *******************************************************************************************************************************/		
		            INSERT INTO [dbo].[Alerta] ( [Id_AlertaParametro], [Ds_Mensagem], [Fl_Tipo] )
		            SELECT @Id_AlertaParametro, @Subject, 1
	            END";
        // Execute the command and send back the results
        SqlContext.Pipe.ExecuteAndSend(myCommand);
    }
};