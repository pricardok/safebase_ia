using System;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using Microsoft.SqlServer.Server;
public partial class StoredProcedures
{
    [Microsoft.SqlServer.Server.SqlProcedure]
    public static void stpAlertaTempdbUtilizacaoArquivoMDF()
    {
        // Create the command
        SqlCommand myCommand = new SqlCommand();
        myCommand.CommandText =
              @"
                SET NOCOUNT ON
                
	            -- Tamanho Arquivo MDF Tempdb
	            DECLARE @Id_AlertaParametro INT = (SELECT Id_AlertaParametro FROM [InitDB].[dbo].AlertaParametro (NOLOCK) WHERE Nm_Alerta = 'Tempdb Utilizacao Arquivo MDF')

	            DECLARE @Tempo_Conexoes_Hs tinyint, @Tempdb_Parametro int, @EmailDestination VARCHAR(200), @Tamanho_Tempdb INT,@Subject VARCHAR(500), @Fl_Tipo TINYINT, 
			            @Importance AS VARCHAR(6), @EmailBody VARCHAR(MAX), @AlertaTamanhoMDFTempdbHeader VARCHAR(MAX), @AlertaTamanhoMDFTempdbTable VARCHAR(MAX), 
			            @AlertaTempdbUtilizacaoArquivoHeader VARCHAR(MAX), @AlertaTamanhoMDFTempdbConexoesTable VARCHAR(MAX), @EmptyBodyEmail VARCHAR(MAX),	
			            @TextRel1 VARCHAR(4000), @TextRel2 VARCHAR(4000), @NomeRel VARCHAR(300),@MntMsg VARCHAR(200), @TLMsg VARCHAR(200), @SendMail VARCHAR(200), 
			            @ProfileDBMail VARCHAR(50), @BodyFormatMail VARCHAR(20), @CaminhoPath VARCHAR(50), @CaminhoFim VARCHAR(50), @Ass VARCHAR(4000),
			            @HTML VARCHAR(MAX), @Query VARCHAR(MAX)

	            --------------------------------------------------------------------------------------------------------------------------------
	            -- Recupera os parametros do Alerta
	            --------------------------------------------------------------------------------------------------------------------------------

                -- Email, Parametro, Id Telegram, Caminho dos reports, Profile DB Mail, Body Format Mail 
	            SELECT @NomeRel = Nm_Alerta, 
		              @Tempdb_Parametro = Vl_Parametro, 
		              @EmailDestination = Ds_Email, 
		              @TLMsg = Ds_MSG, 
		              @CaminhoPath = Ds_Caminho, 
		              @ProfileDBMail = Ds_ProfileDBMail, 
		              @BodyFormatMail = Ds_BodyFormatMail,
		              @importance = Ds_TipoMail
	            FROM [dbo].[AlertaParametro]
	            WHERE [Id_AlertaParametro] = @Id_AlertaParametro	

	            -- Conexões mais antigas que 1 hora
	            SELECT	@Tempo_Conexoes_Hs = 1,
			            @Tamanho_Tempdb = 10000		--	10 GB
				
	            -- Verifica o último Tipo do Alerta registrado -> 0: CLEAR / 1: ALERTA
	            SELECT @Fl_Tipo = [Fl_Tipo]
	            FROM [dbo].[Alerta]
	            WHERE [Id_Alerta] = (SELECT MAX(Id_Alerta) FROM [dbo].[Alerta] WHERE [Id_AlertaParametro] = @Id_AlertaParametro )
	
	            -- Busca as informações do Tempdb
	            IF ( OBJECT_ID('tempdb..##Alerta_Tamanho_MDF_Tempdb') IS NOT NULL )
		            DROP TABLE ##Alerta_Tamanho_MDF_Tempdb

	            select 
		            file_id,
		            reserved_MB = CAST((unallocated_extent_page_count+version_store_reserved_page_count+user_object_reserved_page_count + internal_object_reserved_page_count+mixed_extent_page_count)*8/1024. AS numeric(15,2)) ,
		            unallocated_extent_MB = CAST(unallocated_extent_page_count*8/1024. AS NUMERIC(15,2)),
		            internal_object_reserved_MB = CAST(internal_object_reserved_page_count*8/1024. AS NUMERIC(15,2)),
		            version_store_reserved_MB = CAST(version_store_reserved_page_count*8/1024. AS NUMERIC(15,2)),
		            user_object_reserved_MB = convert(numeric(10,2),round(user_object_reserved_page_count*8/1024.,2))
	            into ##Alerta_Tamanho_MDF_Tempdb
	            from tempdb.sys.dm_db_file_space_usage

	            IF ( OBJECT_ID('tempdb..##Alerta_Tamanho_MDF_Tempdb_Conexoes') IS NOT NULL )
		            DROP TABLE ##Alerta_Tamanho_MDF_Tempdb_Conexoes

	            -- Busca as transações que estão abertas
	            CREATE TABLE ##Alerta_Tamanho_MDF_Tempdb_Conexoes(
		            [session_id] [smallint] NULL,
		            [login_time] [varchar](40) NULL,
		            [login_name] [nvarchar](128) NULL,
		            [host_name] [nvarchar](128) NULL,
		            [open_transaction_Count] [int] NULL,
		            [status] [nvarchar](30) NULL,
		            [cpu_time] [int] NULL,
		            [total_elapsed_time] [int] NULL,
		            [reads] [bigint] NULL,
		            [writes] [bigint] NULL,
		            [logical_reads] [bigint] NULL
	            ) ON [PRIMARY]

	            -- Query Alerta Tempdb - Conexões abertas - Incluir no Alerta TempDb
	            INSERT INTO ##Alerta_Tamanho_MDF_Tempdb_Conexoes
	            SELECT	session_id, convert(varchar(20),login_time,120) AS login_time, login_name, host_name, 
			            /*open_transaction_Count,*/ NULL, status, cpu_time, total_elapsed_time, reads, writes, logical_reads	
	            FROM sys.dm_exec_sessions
	            WHERE	session_id > 50 
			            --and open_transaction_Count > 0
			            and dateadd(hour,-@Tempo_Conexoes_Hs,getdate()) > login_time
			
	            -- Tratamento caso não retorne nenhuma conexão
	            IF NOT EXISTS (SELECT TOP 1 session_id FROM ##Alerta_Tamanho_MDF_Tempdb_Conexoes)
	            BEGIN
		            INSERT INTO ##Alerta_Tamanho_MDF_Tempdb_Conexoes
		            VALUES(NULL, 'Sem conexao aberta a mais de 1 hora', NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL)
	            END
	
	            /*******************************************************************************************************************************
	            --	Verifica se o Consumo do Arquivo do Tempdb está muito grande
	            *******************************************************************************************************************************/
	            IF EXISTS	(
					            select TOP 1 unallocated_extent_MB 
					            from ##Alerta_Tamanho_MDF_Tempdb
					            where	reserved_MB > @Tamanho_Tempdb 
							            and unallocated_extent_MB < reserved_MB * (1 - (@Tempdb_Parametro / 100.0))
				            )

	            BEGIN	-- INICIO - ALERTA				
		            IF ISNULL(@Fl_Tipo, 0) = 0	-- Envia o Alerta apenas uma vez
		            BEGIN
			            /*******************************************************************************************************************************
			            --	CRIA O EMAIL - ALERTA - TAMANHO ARQUIVO MDF TEMPDB
			            *******************************************************************************************************************************/
			            SET @Subject =	'ALERTA #ArquivoMDF - Detectado problema na utilização do Arquivo MDF do Tempdb está acima de ' + cast(@Tempdb_Parametro as varchar) + '% no Servidor: ' + @@SERVERNAME
			            SET @TextRel1 = 'Prezados,<BR /><BR /> Identifiquei um problema no Arquivo <b>MDF do Tempdb<b>, sua utilização esta acima de ' +  CAST((@Tempdb_Parametro) AS VARCHAR) + '% no Servidor: <b>' + @@SERVERNAME +',</b> verifique o relatório abaixo com <b>urgência</b>.'	
			            SET @TextRel2 =  'Conexões com Transação Aberta.'	
			            SET @CaminhoFim = @CaminhoPath + @NomeRel +'.html'
			 
			            -- Gera Primeiro bloco de HTML
			            SET @Query = 'SELECT file_id AS [File ID],reserved_MB AS [Espaço Reservado (MB)],CAST( ((1 - (unallocated_extent_MB / reserved_MB)) * 100) AS NUMERIC(15,2)) AS [Percentual Utilizado (%)],unallocated_extent_MB AS [Espaço Não Alocado (MB)],internal_object_reserved_MB AS [Espaço Objetos Internos (MB)],version_store_reserved_MB AS [Espaço Version Store (MB)],user_object_reserved_MB AS [Espaço Objetos de Usuário (MB)] FROM ##Alerta_Tamanho_MDF_Tempdb'
			            SET @HTML = dbo.fncExportaMultiHTML(@Query, @TextRel1, 2, 1)
			            -- Gera Segundo bloco de HTML
			            SET @Query = 'SELECT ISNULL(CAST(session_id AS VARCHAR),''-'') AS session_id,ISNULL(login_time,''-'') AS login_time, ISNULL(login_name,''-'') AS login_name,ISNULL(host_name,''-'') AS host_name, ISNULL(CAST(open_transaction_Count AS VARCHAR),''-'') AS open_transaction_Count, ISNULL(status,''-'') AS status, ISNULL(CAST(cpu_time AS VARCHAR),''-'') AS cpu_time, ISNULL(CAST(total_elapsed_time AS VARCHAR),''-'') AS total_elapsed_time, ISNULL(CAST(reads AS VARCHAR),''-'') AS reads, ISNULL(CAST(writes AS VARCHAR),''-'') AS writes, ISNULL(CAST(logical_reads AS VARCHAR),''-'') AS logical_reads from [##Alerta_Tamanho_MDF_Tempdb_Conexoes]'
			            SET @HTML += dbo.fncExportaMultiHTML(@Query, @TextRel2, 2, 1) -- na 2a query não precisa do HTML completo
			            -- Gera Terceiro bloco de HTML
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
		            END
	            END		-- FIM - ALERTA
	            ELSE 
	            BEGIN	-- INICIO - CLEAR		
		            IF @Fl_Tipo = 1
		            BEGIN
			            /*******************************************************************************************************************************
			            --	CRIA O EMAIL - CLEAR - TAMANHO ARQUIVO MDF TEMPDB
			            *******************************************************************************************************************************/

			            SET @Subject =	'Solução #ArquivoMDF - A Utilização do Arquivo MDF do Tempdb está abaixo de ' + cast(@Tempdb_Parametro as varchar) + '% no Servidor: ' + @@SERVERNAME
			            SET @TextRel1 = 'Prezados,<BR /><BR />A Utilização do Arquivo MDF do Tempdb está abaixo de ' +  CAST((@Tempdb_Parametro) AS VARCHAR) + '% no Servidor: <b>' + @@SERVERNAME +',</b>.'	
			            SET @TextRel2 =  'Conexões com Transação Aberta.'	
			            SET @CaminhoFim = @CaminhoPath + @NomeRel +'.html'
			 
			            -- Gera Primeiro bloco de HTML
			            SET @Query = 'SELECT file_id AS [File ID],reserved_MB AS [Espaço Reservado (MB)],CAST( ((1 - (unallocated_extent_MB / reserved_MB)) * 100) AS NUMERIC(15,2)) AS [Percentual Utilizado (%)],unallocated_extent_MB AS [Espaço Não Alocado (MB)],internal_object_reserved_MB AS [Espaço Objetos Internos (MB)],version_store_reserved_MB AS [Espaço Version Store (MB)],user_object_reserved_MB AS [Espaço Objetos de Usuário (MB)] FROM ##Alerta_Tamanho_MDF_Tempdb'
			            SET @HTML = dbo.fncExportaMultiHTML(@Query, @TextRel1, 2, 1)
			            -- Gera Segundo bloco de HTML
			            SET @Query = 'SELECT ISNULL(CAST(session_id AS VARCHAR),''-'') AS session_id,ISNULL(login_time,''-'') AS login_time, ISNULL(login_name,''-'') AS login_name,ISNULL(host_name,''-'') AS host_name, ISNULL(CAST(open_transaction_Count AS VARCHAR),''-'') AS open_transaction_Count, ISNULL(status,''-'') AS status, ISNULL(CAST(cpu_time AS VARCHAR),''-'') AS cpu_time, ISNULL(CAST(total_elapsed_time AS VARCHAR),''-'') AS total_elapsed_time, ISNULL(CAST(reads AS VARCHAR),''-'') AS reads, ISNULL(CAST(writes AS VARCHAR),''-'') AS writes, ISNULL(CAST(logical_reads AS VARCHAR),''-'') AS logical_reads from [##Alerta_Tamanho_MDF_Tempdb_Conexoes]'
			            SET @HTML += dbo.fncExportaMultiHTML(@Query, @TextRel2, 2, 1) -- na 2a query não precisa do HTML completo
			            -- Gera Terceiro bloco de HTML
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
			            SET @MntMsg = @Subject+', Verifique os detalhes no e-mail'
			            EXEC dbo.StpSendMsgTelegram 
			                @Destino = @TLMsg,
			                --@Destino = '49353855', 
			                @Msg = @MntMsg 	
		
			            /*******************************************************************************************************************************
			            -- Insere um Registro na Tabela de Controle dos Alertas -> Fl_Tipo = 0 : CLEAR
			            *******************************************************************************************************************************/
			            INSERT INTO [dbo].[Alerta] ( [Id_AlertaParametro], [Ds_Mensagem], [Fl_Tipo] )
			            SELECT @Id_AlertaParametro, @Subject, 0		
		            END
	            END
	            ";
        // Execute the command and send back the results
        SqlContext.Pipe.ExecuteAndSend(myCommand);
    }
};