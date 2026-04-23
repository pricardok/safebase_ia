using System;
using System.Collections.Generic;
using System.Text;

namespace InitDB.Client
{
    class stpAlertaEspacoDisco
    {
        public static string Query()
        {
            return
            //@"insert into [dbo].[Testedb] ([Nome],[DateTest]) values ('Teste da ferramenta DB - stpAlertaEspacoDisco',GETDATE())";
            @"  
                SET NOCOUNT ON;

				SET QUOTED_IDENTIFIER ON;

                DECLARE @OAP_Habilitado sql_variant

                SELECT	@OAP_Habilitado = value_in_use
                FROM sys.configurations WITH (NOLOCK)
                where name = 'Ole Automation Procedures'

                IF(OBJECT_ID('tempdb..#DiskSpace') IS NOT NULL) 
	                DROP TABLE #DiskSpace
	                CREATE TABLE #DiskSpace (
		                [Drive]				VARCHAR(50) ,
		                [Size (MB)]		INT,
		                [Used (MB)]		INT,
		                [Free (MB)]		INT,
		                [Free (%)]			INT,
		                [Used (%)]			INT,
		                [Used by SQL (MB)]	INT, 
		                [Date]				SMALLDATETIME
	                )
		
                IF (@OAP_Habilitado = 1)
                BEGIN	
	                IF(OBJECT_ID('tempdb..#dbspace') IS NOT NULL) 
	                DROP TABLE #dbspace
	                CREATE TABLE #dbspace (
		                [Name]		SYSNAME,
		                [Path]	VARCHAR(200),
		                [Size]	VARCHAR(10),
		                [Drive]		VARCHAR(30)
	                )

	                EXEC sp_MSforeachdb '	Use [?] 
							                INSERT INTO #dbspace 
							                SELECT	CONVERT(VARCHAR(25), DB_NAME())''Database'', CONVERT(VARCHAR(60), FileName),
									                CONVERT(VARCHAR(8), Size/128) ''Size in MB'', CONVERT(VARCHAR(30), Name) 
							                FROM [sysfiles]'

	                DECLARE @hr INT, @fso INT, @size FLOAT, @TotalSpace INT, @MBFree INT, @Percentage INT, 
			                @SQLDriveSize INT, @drive VARCHAR(1), @fso_Method VARCHAR(255), @mbtotal INT	
	
	                set @mbtotal = 0

	                EXEC @hr = [master].[dbo].[sp_OACreate] 'Scripting.FilesystemObject', @fso OUTPUT

	                IF (OBJECT_ID('tempdb..#space') IS NOT NULL) 
		                DROP TABLE #space

	                CREATE TABLE #space (
		                [drive] CHAR(1), 
		                [mbfree] INT
	                )
	
	                INSERT INTO #space EXEC [master].[dbo].[xp_fixeddrives]
	
	                DECLARE CheckDrives Cursor For SELECT [drive], [mbfree] 
	                FROM #space
	
	                Open CheckDrives
	                FETCH NEXT FROM CheckDrives INTO @drive, @MBFree

	                WHILE(@@FETCH_STATUS = 0)
	                BEGIN
		                SET @fso_Method = 'Drives(""' + @drive + ':"").TotalSize'

                        SELECT @SQLDriveSize = SUM(CONVERT(INT, Size)) 
		                FROM #dbspace 
		                WHERE SUBSTRING(Path, 1, 1) = @drive
		
		                EXEC @hr = sp_OAMethod @fso, @fso_Method, @size OUTPUT
		
		                SET @mbtotal = @size / (1024 * 1024)
		
		                INSERT INTO #DiskSpace 
		                VALUES(	@drive + ':', @mbtotal, @mbtotal-@MBFree, @MBFree, (100 * round(@MBFree, 2) / round(@mbtotal, 2)), 
				                (100 - 100 * round(@MBFree,2) / round(@mbtotal, 2)), @SQLDriveSize, GETDATE())

		                FETCH NEXT FROM CheckDrives INTO @drive, @MBFree
	                END
	                CLOSE CheckDrives
	                DEALLOCATE CheckDrives
		
                END
							
                IF ( OBJECT_ID('tempdb..##ResultadoEspacodisco') IS NOT NULL )
	                DROP TABLE ##ResultadoEspacodisco

                SELECT	[Drive], 
		                CAST([Size (MB)] AS VARCHAR) AS [Size (MB)], 
		                CAST([Used (MB)] AS VARCHAR) AS [Used (MB)], 
		                CAST([Free (MB)] AS VARCHAR) AS [Free (MB)], 
		                CAST([Used (%)] AS VARCHAR) AS [Used (%)], 
		                CAST([Free (%)] AS VARCHAR) AS [Free (%)], 
		                CAST([Used by SQL (MB)] AS VARCHAR) AS [Used by SQL (MB)]
                INTO ##ResultadoEspacodisco
                FROM #DiskSpace
	
                -- Declara as variaveis
                DECLARE @Subject VARCHAR(500), @Fl_Tipo TINYINT, @Importance AS VARCHAR(6),@EmailBody VARCHAR(MAX), 
		                @AlertaDiscoHeader VARCHAR(MAX),@AlertaDiscoTable VARCHAR(MAX), @EmptyBodyEmail VARCHAR(MAX), @EmailDestination VARCHAR(200),
		                @ResultadoWhoisactiveHeader VARCHAR(MAX), @ResultadoWhoisactiveTable VARCHAR(MAX), @Espaco_Disco_Parametro INT, @TextRel1 VARCHAR(4000), 
		                @TextRel2 VARCHAR(4000), @NomeRel VARCHAR(300),@MntMsg VARCHAR(200), @TLMsg VARCHAR(200), @SendMail VARCHAR(200), @ProfileDBMail VARCHAR(50), 
		                @BodyFormatMail VARCHAR(20), @CaminhoPath VARCHAR(50), @CaminhoFim VARCHAR(50), @Ass VARCHAR(4000),@HTML VARCHAR(MAX), @Query VARCHAR(MAX), 
                        @Ds_Email_Assunto_alerta VARCHAR (600), @Ds_Email_Assunto_solucao VARCHAR (600), @Ds_Email_Texto_alerta VARCHAR (600), 
                        @Ds_Email_Texto_solucao VARCHAR (600), @Ds_Menssageiro_01 VARCHAR (30), @Ds_Menssageiro_02 VARCHAR (30), @Ds_Menssageiro_03 VARCHAR (30)

                -- Recupera os parametros do Alerta
                DECLARE @Id_AlertaParametro INT = (SELECT Id_AlertaParametro FROM [dbo].AlertaParametro(NOLOCK) WHERE Nm_Alerta = 'Espaco Disco' AND Ativo = 1)
                DECLARE @Ds_Caminho_Base VARCHAR(100) = (SELECT Ds_Caminho FROM [dbo].AlertaParametro (NOLOCK) WHERE Nm_Alerta = 'CheckList')
                DECLARE @Telegram INT = (select Id_AlertaParametro from AlertaParametro WHERE Nm_Alerta = 'Envia Telegram')
                DECLARE @Teams INT = (select Id_AlertaParametro from AlertaParametro WHERE Nm_Alerta = 'Envia Teams')
	
                -- Email, Parametro, Id Telegram, Caminho dos reports, Profile DB Mail, Body Format Mail
                SELECT  @NomeRel = Nm_Alerta,
                        @Espaco_Disco_Parametro = Vl_Parametro,
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
                WHERE[Id_AlertaParametro] = @Id_AlertaParametro	

                DECLARE @CanalTelegram VARCHAR(100) = (SELECT A.canal FROM [dbo].[AlertaMsgToken] A
                      INNER JOIN [dbo].AlertaParametro B ON A.Id = B.Ds_Menssageiro_01 where b.Ds_Menssageiro_01 = @Ds_Menssageiro_01 AND B.Id_AlertaParametro = @Telegram AND B.Ativo = 1) 

                -- Verifica o último Tipo do Alerta registrado -> 0: CLEAR / 1: ALERTA
                SELECT @Fl_Tipo = [Fl_Tipo]
                FROM[dbo].[Alerta]
                WHERE[Id_Alerta] = (SELECT MAX(Id_Alerta) FROM[dbo].[Alerta] WHERE[Id_AlertaParametro] = @Id_AlertaParametro )


                --	Verifica o Espaço Livre em Disco
                IF EXISTS (
                                SELECT NULL
				                FROM [dbo].[##ResultadoEspacodisco]
                                WHERE [Free (%)] > @Espaco_Disco_Parametro

                            )

                BEGIN	-- INICIO - ALERTA
                    IF ISNULL(@Fl_Tipo, 0) = 0	-- Envia o Alerta apenas uma vez

                    BEGIN
			           
		                /*******************************************************************************************************************************
		                --	CRIA O EMAIL - ALERTA
		                *******************************************************************************************************************************/
		                -- Parametros do Alerta
                        SET @Subject = @Ds_Email_Assunto_alerta + ' ' + @@SERVERNAME +', Utilização acima de ' +CAST((@Espaco_Disco_Parametro) AS VARCHAR)+'%.'
		                SET @TextRel1 = @Ds_Email_Texto_alerta
		                SET @TextRel2 = 'Processos em execução no Banco de Dados.'
                        SET @CaminhoFim = @Ds_Caminho_Base + @CaminhoPath + @NomeRel + '.html'

                        -- Gera Primeiro bloco de HTML
                        SET @Query = 'SELECT * FROM [dbo].[##ResultadoEspacodisco]'
		                SET @HTML = dbo.fncExportaMultiHTML(@Query, @TextRel1, 2, 1)
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


		                -- Insere um Registro na Tabela de Controle dos Alertas -> Fl_Tipo = 1 : ALERTA
                        INSERT INTO [dbo].[Alerta] ( [Id_AlertaParametro], [Ds_Mensagem], [Fl_Tipo] )
                        SELECT @Id_AlertaParametro, @Subject, 1			

                    END
                END		-- FIM - ALERTA
                ELSE
	                BEGIN   -- INICIO - CLEAR
                    IF @Fl_Tipo = 1

                    BEGIN
			            
		                /*******************************************************************************************************************************
		                --	ALERTA - ENVIA O EMAIL - ENVIA TELEGRAM
		                *******************************************************************************************************************************/
		                SET @Subject = @Ds_Email_Assunto_solucao + ' ' +CAST((@Espaco_Disco_Parametro) AS VARCHAR)+ '% no Servidor: ' + @@SERVERNAME
		                SET @TextRel1 = @Ds_Email_Texto_solucao + ' <b>' + @@SERVERNAME +'</b>.'	
		                SET @TextRel2 = 'Processos em execução no Banco de Dados.'

                        SET @CaminhoFim = @Ds_Caminho_Base + @CaminhoPath + @NomeRel + '.html'

                        -- Gera Primeiro bloco de HTML
                        SET @Query = 'SELECT * FROM [dbo].[##ResultadoEspacodisco]'
		                SET @HTML = dbo.fncExportaMultiHTML(@Query, @TextRel1, 2, 1)
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
		                --	ALERTA - ENVIA O EMAIL
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
                END		-- FIM - CLEAR

				


                ";


        }
    }
}
