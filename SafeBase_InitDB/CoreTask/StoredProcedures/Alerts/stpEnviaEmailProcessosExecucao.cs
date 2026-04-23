using System;
using System.Collections.Generic;
using System.Text;

namespace InitDB.Client
{
    class stpEnviaEmailProcessosExecucao
    {
        public static string Query()
        {
            return
			// @"insert into [dbo].[Testedb] ([Nome],[DateTest]) values ('Teste da ferramenta DB - stpEnviaEmailProcessosExecucao',GETDATE())";
			@"
              
                SET NOCOUNT ON;

				SET QUOTED_IDENTIFIER ON;
                
	            -- Declara as variaveis
	            DECLARE	@Subject VARCHAR(500), @Importance AS VARCHAR(6), @EmailBody VARCHAR(MAX), @EmptyBodyEmail VARCHAR(MAX),
			            @ResultadoWhoisactiveHeader VARCHAR(MAX), @ResultadoWhoisactiveTable VARCHAR(MAX), @EmailDestination VARCHAR(200),
			            @TextRel1 VARCHAR(4000), @TextRel2 VARCHAR(4000), @NomeRel VARCHAR(300),@MntMsg VARCHAR(200), @TLMsg VARCHAR(200), 
			            @SendMail VARCHAR(200), @ProfileDBMail VARCHAR(50), @BodyFormatMail VARCHAR(20), @CaminhoPath VARCHAR(50), 
			            @CaminhoFim VARCHAR(50), @Ass VARCHAR(4000),@HTML VARCHAR(MAX), @Query VARCHAR(MAX), @Ds_Email_Assunto_alerta VARCHAR (600), 
                        @Ds_Email_Assunto_solucao VARCHAR (600), @Ds_Email_Texto_alerta VARCHAR (600), @Ds_Email_Texto_solucao VARCHAR (600), 
                        @Ds_Menssageiro_01 VARCHAR (30), @Ds_Menssageiro_02 VARCHAR (30), @Ds_Menssageiro_03 VARCHAR (30)
	 
	            -- Cria a tabela que ira armazenar os dados dos processos
	            IF ( OBJECT_ID('TempDb..#Resultado_WhoisActive') IS NOT NULL )
		            DROP TABLE #Resultado_WhoisActive
		
	            CREATE TABLE #Resultado_WhoisActive (
		            [dd hh:mm:ss.mss]		VARCHAR(20),
		            [database_name]			NVARCHAR(128),		
		            [login_name]			NVARCHAR(128),
		            [host_name]				NVARCHAR(128),
		            [start_time]			DATETIME,
		            [status]				VARCHAR(30),
		            [session_id]			INT,
		            [blocking_session_id]	INT,
		            [wait_info]				VARCHAR(MAX),
		            [open_tran_count]		INT,
		            [CPU]					VARCHAR(MAX),
		            [reads]					VARCHAR(MAX),
		            [writes]				VARCHAR(MAX),
		            [sql_command]			XML
                )      

	            -- Cria a tabela processo que ira armazenar os dados envio de email
	            IF ( OBJECT_ID('tempdb..##VerificaProcessos') IS NOT NULL )
		            DROP TABLE ##VerificaProcessos
	
                CREATE TABLE ##VerificaProcessos
	               ([Duração]             VARCHAR(50),
	                [database_name]       VARCHAR(50),
	                [login_name]          VARCHAR(50),
	                [host_name]           VARCHAR(50),
	                [start_time]          VARCHAR(50),
	                [status]              VARCHAR(50),
	                [session_id]          VARCHAR(50),
	                [blocking_session_id] VARCHAR(50),
	                [Wait]                VARCHAR(50),
	                [open_tran_count]     VARCHAR(50),
	                [CPU]                 VARCHAR(50),
	                [reads]               VARCHAR(50),
	                [writes]              VARCHAR(50),
	                [sql_command]         VARCHAR(50),
	               );
      
                -- Retorna todos os processos que estão sendo executados no momento
                EXEC [dbo].[sp_WhoIsActive]
			            @get_outer_command =	1,
                        @output_column_list =	'[dd hh:mm:ss.mss][database_name][login_name][host_name][start_time][status][session_id][blocking_session_id][wait_info][open_tran_count][CPU][reads][writes][sql_command]',
			            @destination_table =	'#Resultado_WhoisActive'
				    
                -- Altera a coluna que possui o comando SQL
	            ALTER TABLE #Resultado_WhoisActive
	            ALTER COLUMN [sql_command] VARCHAR(MAX)
	
	            UPDATE #Resultado_WhoisActive
	            SET [sql_command] = REPLACE( REPLACE( REPLACE( REPLACE( CAST([sql_command] AS VARCHAR(1000)), '<?query --', ''), '--?>', ''), '&gt;', '>'), '&lt;', '')
	
	            -- Verifica se não existe nenhum processo em Execução
	            IF NOT EXISTS ( SELECT TOP 1 * FROM #Resultado_WhoisActive )
	            BEGIN
		            INSERT INTO #Resultado_WhoisActive
		            SELECT NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL
	            END


	            -- Insert de dados para envio de Processo
	            INSERT INTO ##VerificaProcessos ( [Duração],[database_name],[login_name],[host_name],[start_time],[status],[session_id],[blocking_session_id],[Wait],[open_tran_count],[CPU],[reads],[writes],[sql_command] )
	            SELECT	ISNULL([dd hh:mm:ss.mss], '-')							            AS [Duração], 
						            ISNULL([database_name], '-')							AS [database_name],
						            ISNULL([login_name], '-')								AS [login_name],
						            ISNULL([host_name], '-')								AS [host_name],
						            ISNULL(CONVERT(VARCHAR(20), [start_time], 120), '-')	AS [start_time],
						            ISNULL([status], '-')									AS [status],
						            ISNULL(CAST([session_id] AS VARCHAR), '-')				AS [session_id],
						            ISNULL(CAST([blocking_session_id] AS VARCHAR), '-')		AS [blocking_session_id],
						            ISNULL([wait_info], '-')								AS [Wait],
						            ISNULL(CAST([open_tran_count] AS VARCHAR), '-')			AS [open_tran_count],
						            ISNULL([CPU], '-')										AS [CPU],
						            ISNULL([reads], '-')									AS [reads],
						            ISNULL([writes], '-')									AS [writes],
						            ISNULL(SUBSTRING([sql_command], 1, 300), '-')			AS [sql_command]
				            FROM #Resultado_WhoisActive
	
	            --------------------------------------------------------------------------------------------------------------------------------
	            -- Recupera os parametros do Alerta
	            --------------------------------------------------------------------------------------------------------------------------------
	            -- Processos em Execução
	            DECLARE @Id_AlertaParametro INT = (SELECT Id_AlertaParametro FROM [dbo].AlertaParametro (NOLOCK) WHERE Nm_Alerta = 'Processos em Execução' AND Ativo = 1)
                DECLARE @Ds_Caminho_Base VARCHAR(100) = (SELECT Ds_Caminho FROM [dbo].AlertaParametro (NOLOCK) WHERE Nm_Alerta = 'CheckList')
                DECLARE @Telegram INT = (select Id_AlertaParametro from AlertaParametro WHERE Nm_Alerta = 'Envia Telegram')
                DECLARE @Teams INT = (select Id_AlertaParametro from AlertaParametro WHERE Nm_Alerta = 'Envia Teams')
	
	            -- Email, Parametro, Id Telegram, Caminho dos reports, Profile DB Mail, Body Format Mail 
	            SELECT @NomeRel = Nm_Alerta, 
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

	            /*******************************************************************************************************************************
	            --	CRIA O EMAIL
	            *******************************************************************************************************************************/							
	
	            SET @Subject =	@Ds_Email_Assunto_alerta + ' ' + @@SERVERNAME	
	            SET @TextRel1 = @Ds_Email_Texto_alerta	
	            SET @CaminhoFim = @Ds_Caminho_Base + @CaminhoPath + @NomeRel +'.html'
			 
	            -- Gera Primeiro bloco de HTML
	            SET @Query = 'SELECT * FROM ##VerificaProcessos'
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
	
  
	
              ";
        }
    }
}
