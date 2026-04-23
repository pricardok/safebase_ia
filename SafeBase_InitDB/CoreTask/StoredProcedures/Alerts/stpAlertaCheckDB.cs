using System;
using System.Collections.Generic;
using System.Text;

namespace InitDB.Client
{
    class stpAlertaCheckDB
    {
        public static string Query()
        {
            
            return
			// @"insert into [dbo].[Testedb] ([Nome],[DateTest]) values ('Teste da ferramenta DB - stpAlertaCheckDB',GETDATE())";
			@"
                SET NOCOUNT ON;

			SET QUOTED_IDENTIFIER ON;

	            IF ( OBJECT_ID('tempdb..#TempLog') IS NOT NULL ) 
		            DROP TABLE #TempLog
	
	            CREATE TABLE #TempLog (
		            [LogDate]		DATETIME,
		            [ProcessInfo]	NVARCHAR(50),
		            [Text]			NVARCHAR(MAX)
	            )

	            IF ( OBJECT_ID('tempdb..#logF') IS NOT NULL ) 
		            DROP TABLE #logF
	
	            CREATE TABLE #logF (
		            ArchiveNumber     INT,
		            LogDate           DATETIME,
		            LogSize           INT 
	            )

	            -- Seleciona o número de arquivos.
	            --INSERT INTO #logF  
	            --EXEC sp_enumerrorlogs
	
	
	            -- Utilizar caso apresente erro no script acima
	            IF (OBJECT_ID('tempdb..#logFAux') IS NOT NULL)
		            DROP TABLE #logFAux
	
	            CREATE TABLE #logFAux (
		            [ArchiveNumber] INT,
		            [LogDate]		VARCHAR(20),
		            [LogSize]		INT 
	            )
	
	            -- Seleciona o número de arquivos.
	            INSERT INTO #logFAux  
	            EXEC sp_enumerrorlogs

	            insert into #logF
	            select ArchiveNumber, cast((substring(LogDate,7,4)+substring(LogDate,1,2)+substring(LogDate,4,2)) as datetime), LogSize
	            from #logFAux
	
	
	            DELETE FROM #logF
	            WHERE LogDate < GETDATE()-2

	            DECLARE @TSQL NVARCHAR(2000), @lC INT	

	            SELECT @lC = MIN(ArchiveNumber) FROM #logF

	            --Loop para realizar a leitura de todo o log
	            WHILE @lC IS NOT NULL
	            BEGIN
		              INSERT INTO #TempLog
		              EXEC sp_readerrorlog @lC
		  
		              SELECT @lC = MIN(ArchiveNumber) 
		              FROM #logF
		              WHERE ArchiveNumber > @lC
	            END

	            IF OBJECT_ID('tempdb..#Result_Corruption') IS NOT NULL
		            DROP TABLE #Result_Corruption
		
	            SELECT	LogDate,
			            SUBSTRING(Text, 15, CHARINDEX(')', Text, 15) - 15) AS Nm_Database,
			            SUBSTRING(Text,charindex('found',Text),(charindex('Elapsed time',Text)-charindex('found',Text))) AS Erros,   
			            Text 
	            INTO #Result_Corruption
	            FROM #TempLog
	            WHERE LogDate >= GETDATE() 	-1 
		            and Text like '%DBCC CHECKDB (%'
		            and Text not like '%IDR%'
		            and substring(Text,charindex('found',Text), charindex('Elapsed time',Text) - charindex('found',Text)) <> 'found 0 errors and repaired 0 errors.'

	            -- Declara as variaveis
	            DECLARE @Subject VARCHAR(500), @Importance AS VARCHAR(6), @EmailBody VARCHAR(MAX), @EmptyBodyEmail VARCHAR(MAX),
			            @AlertaBancoCorrompidoHeader VARCHAR(MAX), @AlertaBancoCorrompidoTable VARCHAR(MAX), @EmailDestination VARCHAR(200),
			            @TextRel1 VARCHAR(4000), @TextRel2 VARCHAR(4000), @NomeRel VARCHAR(300),@MntMsg VARCHAR(200), @TLMsg VARCHAR(200), 
			            @SendMail VARCHAR(200), @ProfileDBMail VARCHAR(50), @BodyFormatMail VARCHAR(20), @CaminhoPath VARCHAR(50), 
			            @CaminhoFim VARCHAR(50), @Ass VARCHAR(4000),@HTML VARCHAR(MAX), @Query VARCHAR(MAX), @Ds_Email_Assunto_alerta VARCHAR (600), 
                        @Ds_Email_Assunto_solucao VARCHAR (600), @Ds_Email_Texto_alerta VARCHAR (600), @Ds_Email_Texto_solucao VARCHAR (600), 
                        @Ds_Menssageiro_01 VARCHAR (30), @Ds_Menssageiro_02 VARCHAR (30), @Ds_Menssageiro_03 VARCHAR (30)

	            --------------------------------------------------------------------------------------------------------------------------------
	            -- Recupera os parametros do Alerta
	            --------------------------------------------------------------------------------------------------------------------------------
	            -- Banco de Dados Corrompido
	            DECLARE @Id_AlertaParametro INT = (SELECT Id_AlertaParametro FROM [dbo].AlertaParametro (NOLOCK) WHERE Nm_Alerta = 'Banco de Dados Corrompido' AND Ativo = 1)
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
	            FROM [dbo].[AlertaParametro]A
                INNER JOIN [dbo].[AlertaParametroMenssage] B ON A.Id_AlertaParametro = B.IdAlertaParametro
			    INNER JOIN [dbo].[MailAssinatura] C ON C.Id = A.IdMailAssinatura
	            WHERE [Id_AlertaParametro] = @Id_AlertaParametro

                DECLARE @CanalTelegram VARCHAR(100) = (SELECT A.canal FROM [dbo].[AlertaMsgToken] A
                      INNER JOIN [dbo].AlertaParametro B ON A.Id = B.Ds_Menssageiro_01 where b.Ds_Menssageiro_01 = @Ds_Menssageiro_01 AND B.Id_AlertaParametro = @Telegram AND B.Ativo = 1) 

	            /*******************************************************************************************************************************
	            -- Verifica se existe algum Banco de Dados Corrompido
	            *******************************************************************************************************************************/
	            IF EXISTS (SELECT NULL FROM [dbo].[#Result_Corruption]) 
	            BEGIN	-- INICIO - ALERTA
		            /*******************************************************************************************************************************
		            --	CRIA O EMAIL - ALERTA
		            *******************************************************************************************************************************/			
		            SET @Subject =	@Ds_Email_Assunto_alerta + ' ' + @@SERVERNAME 	
		            SET @TextRel1 = @Ds_Email_Texto_alerta
		            SET @CaminhoFim = @Ds_Caminho_Base + @CaminhoPath + @NomeRel +'.html'
			 
		            -- Gera Primeiro bloco de HTML
		            SET @Query = 'SELECT CONVERT(VARCHAR(20), [LogDate], 120) AS [Data Log], [Nm_Database] AS [Nome Database],[Erros] AS [Erros],[Text] AS [Descricao] FROM [dbo].[#Result_Corruption]'
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
	            END		-- FIM - ALERTA

                ";
        }
    }
}
