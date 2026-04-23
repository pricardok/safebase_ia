using System;
using System.Collections.Generic;
using System.Text;

namespace InitDB.Client
{
    class stpAlertaTempdbUtilizacaoArquivoMDF
    {
        public static string Query()
        {
            return
			//@"insert into [dbo].[Testedb] ([Nome],[DateTest]) values ('Teste da ferramenta DB - stpAlertaTempdbUtilizacaoArquivoMDF',GETDATE())";
			@"
                SET NOCOUNT ON;              

				SET QUOTED_IDENTIFIER ON;

	            -- Tamanho Arquivo MDF Tempdb
	            DECLARE @Id_AlertaParametro INT = (SELECT Id_AlertaParametro FROM [dbo].AlertaParametro (NOLOCK) WHERE Nm_Alerta = 'Tempdb Utilizacao Arquivo MDF' AND Ativo = 1)
                DECLARE @Ds_Caminho_Base VARCHAR(100) = (SELECT Ds_Caminho FROM [dbo].AlertaParametro (NOLOCK) WHERE Nm_Alerta = 'CheckList')
                DECLARE @Telegram INT = (select Id_AlertaParametro from AlertaParametro WHERE Nm_Alerta = 'Envia Telegram')
                DECLARE @Teams INT = (select Id_AlertaParametro from AlertaParametro WHERE Nm_Alerta = 'Envia Teams')

	            DECLARE @Tempo_Conexoes_Hs tinyint, @Tempdb_Parametro int, @EmailDestination VARCHAR(200), @Tamanho_Tempdb INT,@Subject VARCHAR(500), @Fl_Tipo TINYINT, 
			            @Importance AS VARCHAR(6), @EmailBody VARCHAR(MAX), @AlertaTamanhoMDFTempdbHeader VARCHAR(MAX), @AlertaTamanhoMDFTempdbTable VARCHAR(MAX), 
			            @AlertaTempdbUtilizacaoArquivoHeader VARCHAR(MAX), @AlertaTamanhoMDFTempdbConexoesTable VARCHAR(MAX), @EmptyBodyEmail VARCHAR(MAX),	
			            @TextRel1 VARCHAR(4000), @TextRel2 VARCHAR(4000), @NomeRel VARCHAR(300),@MntMsg VARCHAR(200), @TLMsg VARCHAR(200), @SendMail VARCHAR(200), 
			            @ProfileDBMail VARCHAR(50), @BodyFormatMail VARCHAR(20), @CaminhoPath VARCHAR(50), @CaminhoFim VARCHAR(50), @Ass VARCHAR(4000),
			            @HTML VARCHAR(MAX), @Query VARCHAR(MAX), @Ds_Email_Assunto_alerta VARCHAR (600), 
                        @Ds_Email_Assunto_solucao VARCHAR (600), @Ds_Email_Texto_alerta VARCHAR (600), @Ds_Email_Texto_solucao VARCHAR (600), 
                        @Ds_Menssageiro_01 VARCHAR (30), @Ds_Menssageiro_02 VARCHAR (30), @Ds_Menssageiro_03 VARCHAR (30)

	            --------------------------------------------------------------------------------------------------------------------------------
	            -- Recupera os parametros do Alerta
	            --------------------------------------------------------------------------------------------------------------------------------

                -- Email, Parametro, Id Telegram, Caminho dos reports, Profile DB Mail, Body Format Mail 
	            SELECT @NomeRel = Nm_Alerta, 
		              @Tempdb_Parametro = Vl_Parametro, 
		              @EmailDestination = Ds_Email, 
		              @TLMsg = Ds_MSG,
				      @Ds_Menssageiro_01 = A.Ds_Menssageiro_01,
				      @Ds_Menssageiro_02 = A.Ds_Menssageiro_02,
                      @Ds_Menssageiro_03 = A.Ds_Menssageiro_03,
		              @CaminhoPath = Ds_Caminho, 
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

	            -- Conexões mais antigas que 1 hora
	            SELECT	@Tempo_Conexoes_Hs = 1,
			            @Tamanho_Tempdb = 10000	-- 10 GB
				
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
			            SET @Subject =	@Ds_Email_Assunto_alerta + ' ' + cast(@Tempdb_Parametro as varchar) + '% na instância: ' + @@SERVERNAME
			            SET @TextRel1 = @Ds_Email_Texto_alerta 	
			            SET @TextRel2 =  'Conexões com Transação Aberta.'	
			            SET @CaminhoFim = @Ds_Caminho_Base + @CaminhoPath + @NomeRel +'.html'
			 
			            -- Gera Primeiro bloco de HTML
			            SET @Query = 'SELECT file_id AS [File ID],reserved_MB AS [Espaço Reservado (MB)],CAST( ((1 - (unallocated_extent_MB / reserved_MB)) * 100) AS NUMERIC(15,2)) AS [Percentual Utilizado (%)],unallocated_extent_MB AS [Espaço Não Alocado (MB)],internal_object_reserved_MB AS [Espaço Objetos Internos (MB)],version_store_reserved_MB AS [Espaço Version Store (MB)],user_object_reserved_MB AS [Espaço Objetos de Usuário (MB)] FROM ##Alerta_Tamanho_MDF_Tempdb'
			            SET @HTML = dbo.fncExportaMultiHTML(@Query, @TextRel1, 2, 1)
			            -- Gera Segundo bloco de HTML
			            SET @Query = 'SELECT ISNULL(CAST(session_id AS VARCHAR),''-'') AS session_id,ISNULL(login_time,''-'') AS login_time, ISNULL(login_name,''-'') AS login_name,ISNULL(host_name,''-'') AS host_name, ISNULL(CAST(open_transaction_Count AS VARCHAR),''-'') AS open_transaction_Count, ISNULL(status,''-'') AS status, ISNULL(CAST(cpu_time AS VARCHAR),''-'') AS cpu_time, ISNULL(CAST(total_elapsed_time AS VARCHAR),''-'') AS total_elapsed_time, ISNULL(CAST(reads AS VARCHAR),''-'') AS reads, ISNULL(CAST(writes AS VARCHAR),''-'') AS writes, ISNULL(CAST(logical_reads AS VARCHAR),''-'') AS logical_reads from [##Alerta_Tamanho_MDF_Tempdb_Conexoes]'
			            SET @HTML += dbo.fncExportaMultiHTML(@Query, @TextRel2, 2, 1) -- na 2a query não precisa do HTML completo
			            -- Gera Terceiro bloco de HTML
			            select @HTML = @HTML + @Ass
			            -- Salva Arquivo HTML de Envio
			            EXEC dbo.stpWriteFile 
				            @Ds_Texto = @HTML, -- nvarchar(max)
				            @Ds_Caminho = @CaminhoFim, -- nvarchar(max)
				            @Ds_Codificacao = N'UTF-8', -- nvarchar(max)
				            @Ds_Formato_Quebra_Linha = N'windows', -- nvarchar(max)
				            @Fl_Append = 0 -- bit

			            /*******************************************************************************************************************************
			            --	ALERTA - ENVIA O EMAIL - ENVIA TELEGRAM
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
	            END		-- FIM - ALERTA
	            ELSE 
	            BEGIN	-- INICIO - CLEAR		
		            IF @Fl_Tipo = 1
		            BEGIN
			            /*******************************************************************************************************************************
			            --	CRIA O EMAIL - CLEAR - TAMANHO ARQUIVO MDF TEMPDB
			            *******************************************************************************************************************************/

			            SET @Subject =	@Ds_Email_Assunto_solucao +' ' + cast(@Tempdb_Parametro as varchar) + '% na instância: ' + @@SERVERNAME
			            SET @TextRel1 = @Ds_Email_Texto_solucao	
			            SET @TextRel2 =  'Conexões com Transação Aberta.'	
			            SET @CaminhoFim = @Ds_Caminho_Base + @CaminhoPath + @NomeRel +'.html'
			 
			            -- Gera Primeiro bloco de HTML
			            SET @Query = 'SELECT file_id AS [File ID],reserved_MB AS [Espaço Reservado (MB)],CAST( ((1 - (unallocated_extent_MB / reserved_MB)) * 100) AS NUMERIC(15,2)) AS [Percentual Utilizado (%)],unallocated_extent_MB AS [Espaço Não Alocado (MB)],internal_object_reserved_MB AS [Espaço Objetos Internos (MB)],version_store_reserved_MB AS [Espaço Version Store (MB)],user_object_reserved_MB AS [Espaço Objetos de Usuário (MB)] FROM ##Alerta_Tamanho_MDF_Tempdb'
			            SET @HTML = dbo.fncExportaMultiHTML(@Query, @TextRel1, 2, 1)
			            -- Gera Segundo bloco de HTML
			            SET @Query = 'SELECT ISNULL(CAST(session_id AS VARCHAR),''-'') AS session_id,ISNULL(login_time,''-'') AS login_time, ISNULL(login_name,''-'') AS login_name,ISNULL(host_name,''-'') AS host_name, ISNULL(CAST(open_transaction_Count AS VARCHAR),''-'') AS open_transaction_Count, ISNULL(status,''-'') AS status, ISNULL(CAST(cpu_time AS VARCHAR),''-'') AS cpu_time, ISNULL(CAST(total_elapsed_time AS VARCHAR),''-'') AS total_elapsed_time, ISNULL(CAST(reads AS VARCHAR),''-'') AS reads, ISNULL(CAST(writes AS VARCHAR),''-'') AS writes, ISNULL(CAST(logical_reads AS VARCHAR),''-'') AS logical_reads from [##Alerta_Tamanho_MDF_Tempdb_Conexoes]'
			            SET @HTML += dbo.fncExportaMultiHTML(@Query, @TextRel2, 2, 1) -- na 2a query não precisa do HTML completo
			            -- Gera Terceiro bloco de HTML
			            select @HTML = @HTML + @Ass
			            -- Salva Arquivo HTML de Envio
			            EXEC dbo.stpWriteFile 
				            @Ds_Texto = @HTML, -- nvarchar(max)
				            @Ds_Caminho = @CaminhoFim, -- nvarchar(max)
				            @Ds_Codificacao = N'UTF-8', -- nvarchar(max)
				            @Ds_Formato_Quebra_Linha = N'windows', -- nvarchar(max)
				            @Fl_Append = 0 -- bit

			            /*******************************************************************************************************************************
			            --	ALERTA - ENVIA O EMAIL - ENVIA TELEGRAM
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
			            -- Insere um Registro na Tabela de Controle dos Alertas -> Fl_Tipo = 0 : CLEAR
			            *******************************************************************************************************************************/
			            INSERT INTO [dbo].[Alerta] ( [Id_AlertaParametro], [Ds_Mensagem], [Fl_Tipo] )
			            SELECT @Id_AlertaParametro, @Subject, 0		
		            END
	            END
	            ";

        }
    }
}
