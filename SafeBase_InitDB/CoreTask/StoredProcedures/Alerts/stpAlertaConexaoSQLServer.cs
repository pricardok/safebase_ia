using System;
using System.Collections.Generic;
using System.Text;

namespace InitDB.Client
{
    class stpAlertaConexaoSQLServer
    {
        public static string Query()
        {
            return
			//@"insert into [dbo].[Testedb] ([Nome],[DateTest]) values ('Teste da ferramenta DB - stpAlertaConexaoSQLServer',GETDATE())";
			@"  SET NOCOUNT ON;

			SET QUOTED_IDENTIFIER ON;

	            -- Conexões SQL Server
	            DECLARE @Id_AlertaParametro INT = (SELECT Id_AlertaParametro FROM [dbo].AlertaParametro (NOLOCK) WHERE Nm_Alerta = 'Conexão SQL Server' AND Ativo = 1)
                DECLARE @Ds_Caminho_Base VARCHAR(100) = (SELECT Ds_Caminho FROM [dbo].AlertaParametro (NOLOCK) WHERE Nm_Alerta = 'CheckList')
                DECLARE @Telegram INT = (select Id_AlertaParametro from AlertaParametro WHERE Nm_Alerta = 'Envia Telegram')
                DECLARE @Teams INT = (select Id_AlertaParametro from AlertaParametro WHERE Nm_Alerta = 'Envia Teams')

	            -- Declara as variaveis
	            DECLARE @EmailBody VARCHAR(MAX), @AlertaConexaoSQLServerHeader VARCHAR(MAX), @AlertaConexaoSQLServerTable VARCHAR(MAX), @EmptyBodyEmail VARCHAR(MAX), 
		               @Importance AS VARCHAR(6), @Subject VARCHAR(500), @Qtd_Conexoes INT, @Conexoes_SQLServer_Parametro INT, @Fl_Tipo INT, @EmailDestination VARCHAR(200),	
		               @TextRel1 VARCHAR(4000), @TextRel2 VARCHAR(4000), @NomeRel VARCHAR(300),@MntMsg VARCHAR(200), @TLMsg VARCHAR(200), @SendMail VARCHAR(200), 
		               @ProfileDBMail VARCHAR(50), @BodyFormatMail VARCHAR(20), @CaminhoPath VARCHAR(50), @CaminhoFim VARCHAR(50), @Ass VARCHAR(4000),
		               @HTML VARCHAR(MAX), @Query VARCHAR(MAX), @Ds_Email_Assunto_alerta VARCHAR (600), @Ds_Email_Assunto_solucao VARCHAR (600), 
                       @Ds_Email_Texto_alerta VARCHAR (600), @Ds_Email_Texto_solucao VARCHAR (600), @Ds_Menssageiro_01 VARCHAR (30), 
                       @Ds_Menssageiro_02 VARCHAR (30), @Ds_Menssageiro_03 VARCHAR (30)

	            --------------------------------------------------------------------------------------------------------------------------------
	            -- Recupera os parametros do Alerta
	            --------------------------------------------------------------------------------------------------------------------------------

	            -- Email, Parametro, Id Telegram, Caminho dos reports, Profile DB Mail, Body Format Mail 
	            SELECT @NomeRel = Nm_Alerta, 
		              @Conexoes_SQLServer_Parametro = Vl_Parametro, 
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


	            SELECT @Qtd_Conexoes = count(*) FROM sys.dm_exec_sessions WHERE session_id > 50

	            -- Verifica o último Tipo do Alerta registrado -> 0: CLEAR / 1: ALERTA
	            SELECT @Fl_Tipo = [Fl_Tipo]
	            FROM [dbo].[Alerta]
	            WHERE [Id_Alerta] = (SELECT MAX(Id_Alerta) FROM [dbo].[Alerta] WHERE [Id_AlertaParametro] = @Id_AlertaParametro )
	
	            /*******************************************************************************************************************************
	            --	Verifica se o limite de conexões para o Alerta foi atingido
	            *******************************************************************************************************************************/
	            IF (@Qtd_Conexoes > @Conexoes_SQLServer_Parametro)
	            BEGIN	-- INICIO - ALERTA		
		            /*******************************************************************************************************************************
		            --	CRIA O EMAIL - ALERTA
		            *******************************************************************************************************************************/
		
		            if object_id('tempdb..##ConexoesAbertas') is not null
			            drop table ##ConexoesAbertas

		            SELECT	TOP 25 IDENTITY(INT, 1, 1) AS id, 
				            replace(replace(ec.client_net_address,'<',''),'>','') client_net_address, 
				            case when es.[program_name] = '' then 'Sem nome na string de conexão' else [program_name] end [program_name], 
				            es.[host_name], es.login_name, '' Base,-- db_name(database_id) Base,
				            COUNT(ec.session_id)  AS [connection count] 
		            into ##ConexoesAbertas
		            FROM sys.dm_exec_sessions AS es  
		            INNER JOIN sys.dm_exec_connections AS ec ON es.session_id = ec.session_id   
		            GROUP BY ec.client_net_address, es.[program_name], es.[host_name], es.login_name,
		            db_name(database_id)	-- Somente a partir do SQL Server 2008 R2
		            order by [connection count] desc

		            SET @Subject =	@Ds_Email_Assunto_alerta + ' ' + cast(@Qtd_Conexoes as varchar) + ' conexões Abertas no Servidor: ' + @@SERVERNAME
		            SET @TextRel1 = @Ds_Email_Texto_alerta 	
		            SET @CaminhoFim = @Ds_Caminho_Base + @CaminhoPath + @NomeRel +'.html'
			 
		            -- Gera Primeiro bloco de HTML
		            SET @Query = 'SELECT client_net_address AS [IP], [program_name] AS [Aplicacao], [host_name] AS [Hostname], login_name AS [Login], Base AS [Database],cast([connection count] as varchar) [Qtd. Conexões],id AS [ID] FROM ##ConexoesAbertas'
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
		            --  ALERTA - ENVIA O EMAIL E MENSSAGEIROS
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
	            END		-- FIM - ALERTA
	            ELSE 
	            BEGIN	-- INICIO - CLEAR		
		            IF @Fl_Tipo = 1
		            BEGIN
			            /*******************************************************************************************************************************
			            --	CRIA O EMAIL - CLEAR
			            *******************************************************************************************************************************/
		
		                if object_id('tempdb..##ConexoesAbertasClear') is not null
				            drop table ##ConexoesAbertasClear

		                SELECT	top 25 IDENTITY(INT, 1, 1) AS id, 
					            replace(replace(ec.client_net_address,'<',''),'>','') client_net_address, 
					            case when es.[program_name] = '' then 'Sem nome na string de conexão' else [program_name] end [program_name], 
					            es.[host_name], es.login_name, '' Base,-- db_name(database_id) Base,
					            COUNT(ec.session_id)  AS [connection count] 
		                into ##ConexoesAbertasClear
		                FROM sys.dm_exec_sessions AS es  
		                INNER JOIN sys.dm_exec_connections AS ec  
		                ON es.session_id = ec.session_id   
		                GROUP BY ec.client_net_address, es.[program_name], es.[host_name], es.login_name, 
		                db_name(database_id)
   		                order by [connection count] desc

		                SET @Subject =	@Ds_Email_Assunto_solucao +' ' + cast(@Qtd_Conexoes as varchar) + ' conexões Abertas no Servidor: ' + @@SERVERNAME
		                SET @TextRel1 = @Ds_Email_Texto_solucao +' <b>' + @@SERVERNAME +'</b>.'	
		                SET @CaminhoFim = @Ds_Caminho_Base + @CaminhoPath + @NomeRel +'.html'
			 
		                -- Gera Primeiro bloco de HTML
		                SET @Query = 'SELECT client_net_address AS [IP], [program_name] AS [Aplicacao], [host_name] AS [Hostname], login_name AS [Login], Base AS [Database],cast([connection count] as varchar) [Qtd. Conexões],id AS [ID] FROM ##ConexoesAbertasClear'
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
		                --  ALERTA - ENVIA O EMAIL E MENSSAGEIROS
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
	            END		-- FIM - CLEAR";

        }
    }
}
