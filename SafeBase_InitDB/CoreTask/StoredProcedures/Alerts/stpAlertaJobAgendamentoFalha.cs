using System;
using System.Collections.Generic;
using System.Text;

namespace InitDB.Client
{
    class stpAlertaJobAgendamentoFalha
    {
        public static string Query()
        {
            return
			//@"insert into [dbo].[Testedb] ([Nome],[DateTest]) values ('Teste da ferramenta DB - stpcheckFileBackup',GETDATE())";

			@"
            
            SET NOCOUNT ON;

			SET QUOTED_IDENTIFIER ON;

            ---- Recupera os parametros base
            DECLARE @Id_AlertaParametro INT = (SELECT Id_AlertaParametro FROM [dbo].AlertaParametro (NOLOCK) WHERE Nm_Alerta = 'Job Agendamento Falha' AND Ativo = 1)
            DECLARE @Ds_Caminho_Base VARCHAR(100) = (SELECT Ds_Caminho FROM [dbo].AlertaParametro (NOLOCK) WHERE Nm_Alerta = 'CheckList')
            DECLARE @Telegram INT = (select Id_AlertaParametro from AlertaParametro WHERE Nm_Alerta = 'Envia Telegram')
            DECLARE @Teams INT = (select Id_AlertaParametro from AlertaParametro WHERE Nm_Alerta = 'Envia Teams')

            ---- Recupera os parametros do Alerta
            DECLARE @File_Backup_Parametro INT, @EmailDestination VARCHAR(200), @TextRel1 VARCHAR(max), @TextRel2 VARCHAR(4000), 
                @NomeRel VARCHAR(300),@MntMsg VARCHAR(200), @TLMsg VARCHAR(200), @SendMail VARCHAR(200), @ProfileDBMail VARCHAR(50), 
                @BodyFormatMail VARCHAR(20), @CaminhoPath VARCHAR(50), @CaminhoFim VARCHAR(50), @Ass VARCHAR(4000),@HTML VARCHAR(MAX), 
                @Query VARCHAR(MAX), @Importance AS VARCHAR(6), @Subject VARCHAR(600), @Ds_Email_Assunto_alerta VARCHAR (600), 
                @Ds_Email_Assunto_solucao VARCHAR (600), @Ds_Email_Texto_alerta VARCHAR (max), @Ds_Email_Texto_solucao VARCHAR (600), 
                @Ds_Menssageiro_01 VARCHAR (30), @Ds_Menssageiro_02 VARCHAR (30), @Ds_Menssageiro_03 VARCHAR (30),@Fl_Tipo TINYINT,
	            @File NVARCHAR(10),@ZabbixPath varchar(128), @ZabbixServer varchar(128), @ZabbixLocalServer varchar(128), @ZabbixAlertName varchar(128),
				@X VARCHAR(MAX)



			 
            ---- Email, Parametro, Id Telegram, Caminho dos reports, Profile DB Mail, Body Format Mail 
            SELECT @NomeRel = Nm_Alerta, 
                @File_Backup_Parametro = Vl_Parametro, 
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
				@ZabbixPath = A.ZabbixPath, 
				@ZabbixServer = A.ZabbixServer, 
				@ZabbixLocalServer = A.ZabbixLocalServer, 
				@ZabbixAlertName = A.ZabbixAlertName, 
                @Ass = C.Assinatura
            FROM [dbo].[AlertaParametro] A
            INNER JOIN [dbo].[AlertaParametroMenssage] B ON A.Id_AlertaParametro = B.IdAlertaParametro
            INNER JOIN [dbo].[MailAssinatura] C ON C.Id = A.IdMailAssinatura
            WHERE [Id_AlertaParametro] = @Id_AlertaParametro  

            DECLARE @CanalTelegram VARCHAR(100) = (SELECT A.canal FROM [dbo].[AlertaMsgToken] A
                    INNER JOIN [dbo].AlertaParametro B ON A.Id = B.Ds_Menssageiro_01 where b.Ds_Menssageiro_01 = @Ds_Menssageiro_01 AND B.Id_AlertaParametro = @Telegram AND B.Ativo = 1) 


			-- Verifica o último Tipo do Alerta registrado
	        -- 0: CLEAR 
	        -- 1: ALERTA	
	        SELECT @Fl_Tipo = [Fl_Tipo]
	        FROM [dbo].[Alerta]
	        WHERE [Id_Alerta] = (SELECT MAX(Id_Alerta) FROM [dbo].[Alerta] WHERE [Id_AlertaParametro] = @Id_AlertaParametro )

			
			IF OBJECT_ID('tempdb..#TmpHtml') IS NOT NULL
				DROP TABLE #TmpHtml;
	
			SELECT
				x.JobId,				
				CASE 
					WHEN x.rn = 1 THEN
						x.Nome
				ELSE '' END AS Nome,
				CASE 
					WHEN x.rn = 1 THEN
						CONVERT(VARCHAR,x.UltimaExec,103)+' '+CONVERT(VARCHAR,x.UltimaExec,108)
				ELSE
					'' END AS [Ultima Execução],	
				CONVERT(VARCHAR,x.DataExec,103)+' '+CONVERT(VARCHAR,x.DataExec,108) as [Data Execução],		
				x.MessageError		
			INTO
				#TmpHtml
			FROM	
				(
				SELECT 
					j.IdJob as JobId,
					j.Nome,
					j.UltimaExec,
					h.DataExec,
					h.Error,
					h.MessageError,
					h.TempoExec,
					h.IdJobHist,
					ROW_NUMBER() OVER(PARTITION BY j.Nome ORDER BY h.DataExec DESC) AS rn
				FROM 
					[SafeBase].[job].[job] j
				INNER JOIN [SafeBase].[job].[jobHistorico] h
					ON j.IdJob = h.JobId
				WHERE
					h.Error = 1
					AND h.Enviado = 0
				)x
			ORDER BY
				x.Nome,
				x.DataExec DESC;	 

			IF EXISTS(SELECT [JobId] FROM #TmpHtml WHERE [JobId] IS NOT NULL)
			BEGIN
				IF ISNULL(@Fl_Tipo, 0) = 0	-- INICIO - ALERTA
				BEGIN
					/*******************************************************************************************************************************
					--	CRIA O EMAIL - ALERTA
					*******************************************************************************************************************************/			
					-- Parametros do Alerta
					SET @Subject =  @Ds_Email_Assunto_alerta + @@SERVERNAME
					SET @TextRel1 =  @Ds_Email_Texto_alerta 
					SET @CaminhoFim = @Ds_Caminho_Base + @CaminhoPath + @NomeRel +'.html'
					
					-- Gera Primeiro bloco de HTML
					SET @MntMsg = @Subject+', Verifique os detalhes no *E-Mail*'

					SET @Query = 'SELECT * FROM #TmpHtml WHERE [JobId] IS NOT NULL'
					SET @HTML = dbo.fncExportaMultiHTML(@Query, @TextRel1, 2, 1)
					
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
						DECLARE @TLF NVARCHAR(MAX)
						SET @TLF = @MntMsg
						-- Envio do Telegram    
						EXEC dbo.StpSendMsgTelegram 
								@Destino = @CanalTelegram,
								@Msg = @TLF
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

					/*Zabbix Sender*/
					IF EXISTS  (SELECT B.Ativo from AlertaParametro A 
							INNER JOIN [dbo].[AlertaEnvio] B ON B.IdAlertaParametro = A.Id_AlertaParametro
							WHERE B.Ativo = 1
							AND B.Des LIKE '%Zabbix Sender'
							AND [Id_AlertaParametro] = @Id_AlertaParametro
							)
					BEGIN
						EXEC [dbo].[stpZabbixSender] @ZabbixPath,@ZabbixServer,@ZabbixLocalServer,@ZabbixAlertName,1; 
					END

					INSERT INTO [dbo].[Alerta] ( [Id_AlertaParametro], [Ds_Mensagem], [Fl_Tipo] )
					SELECT @Id_AlertaParametro, @Subject, 1	
							
				END								
				 
				UPDATE [SafeBase].[job].[jobHistorico]  SET Enviado =1 					
					WHERE Error = 1 AND Enviado = 0		
			END		        
			ELSE
			BEGIN
				--SE TIVER ALERTA ATIVO E A ULTIMA EXECUCAO FOR COM SUCESSO
				IF NOT EXISTS (SELECT
									x.JobId								
								FROM	
									(
									SELECT 
										j.IdJob as JobId,						
										h.DataExec,
										h.Error,												
										h.IdJobHist,
										ROW_NUMBER() OVER(PARTITION BY j.idJob ORDER BY h.DataExec DESC) AS rn
									FROM 
										[SafeBase].[job].[job] j
									INNER JOIN [SafeBase].[job].[jobHistorico] h
										ON j.IdJob = h.JobId
									WHERE		
										j.Ativo = 1
									)x
								WHERE
									x.rn=1
									and x.Error	= 1			
						) AND @Fl_Tipo = 1  
				BEGIN
					IF OBJECT_ID('tempdb..#TmpHtmlResolucao') IS NOT NULL
						DROP TABLE #TmpHtmlResolucao;
	
					SELECT
						x.JobId,					
						CASE 
							WHEN x.rn = 1 THEN
								x.Nome
						ELSE '' END AS Nome,
						CASE 
							WHEN x.rn = 1 THEN
								CONVERT(VARCHAR,x.UltimaExec,103)+' '+CONVERT(VARCHAR,x.UltimaExec,108)
						ELSE
							'' END AS [Ultima Execução],	
						CONVERT(VARCHAR,x.DataExec,103)+' '+CONVERT(VARCHAR,x.DataExec,108) as [Data Execução],		
						x.MessageError		
					INTO
						#TmpHtmlResolucao
					FROM	
						(
						SELECT 
							j.IdJob as JobId,
							j.Nome,
							j.UltimaExec,
							h.DataExec,
							h.Error,
							h.MessageError,
							h.TempoExec,
							h.IdJobHist,
							ROW_NUMBER() OVER(PARTITION BY j.Nome ORDER BY h.DataExec DESC) AS rn
						FROM 
							[SafeBase].[job].[job] j
						INNER JOIN [SafeBase].[job].[jobHistorico] h
							ON j.IdJob = h.JobId
						WHERE		
							cast(h.DataExec as date) = cast(GETDATE() as date)
						)x
					WHERE
						rn<=5
					ORDER BY
						x.Nome,
						x.DataExec DESC;
			
			
					/*******************************************************************************************************************************
					--  ALERTA - ENVIA O EMAIL E MENSSAGEIROS
					*******************************************************************************************************************************/
					SET @Subject =  @Ds_Email_Assunto_solucao +' ' + @@SERVERNAME
					SET @TextRel1 =  @Ds_Email_Texto_solucao				             
					SET @CaminhoFim = @Ds_Caminho_Base + @CaminhoPath + @NomeRel +'.html'
					SET @MntMsg = @Subject+', Verifique os detalhes no *E-Mail*'


					-- Gera Primeiro bloco de HTML
					SET @Query = 'select * from #TmpHtmlResolucao'
					SET @HTML = dbo.fncExportaMultiHTML(@Query, @TextRel1, 2, 1)
			             
					EXEC dbo.stpWriteFile 
					@Ds_Texto = @HTML, -- nvarchar(max)
					@Ds_Caminho = @CaminhoFim, -- nvarchar(max)
					@Ds_Codificacao = N'UTF-8', -- nvarchar(max)
					@Ds_Formato_Quebra_Linha = N'windows', -- nvarchar(max)
					@Fl_Append = 0 -- bit

					--	ALERTA - ENVIA O EMAIL
					/********************************************************************************************************************************/	
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
						
					/*Zabbix Sender*/
					IF EXISTS  (SELECT B.Ativo from AlertaParametro A 
								INNER JOIN [dbo].[AlertaEnvio] B ON B.IdAlertaParametro = A.Id_AlertaParametro
								WHERE B.Ativo = 1
								AND B.Des LIKE '%Zabbix Sender'
								AND [Id_AlertaParametro] = @Id_AlertaParametro
								)
					BEGIN
						EXEC [dbo].[stpZabbixSender] @ZabbixPath,@ZabbixServer,@ZabbixLocalServer,@ZabbixAlertName,0; 
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
