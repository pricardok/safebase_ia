using System;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using Microsoft.SqlServer.Server;
public partial class StoredProcedures
{
    [Microsoft.SqlServer.Server.SqlProcedure]
    public static void stpAlertaEspacoDisco()
    {
        // Create the command
        SqlCommand myCommand = new SqlCommand();
        myCommand.CommandText =
              @"
                SET NOCOUNT ON
                
	            -- Cria as tabelas que irão armazenar as informações do Espaço em Disco	
	            IF ( OBJECT_ID('tempdb..#dbspace') IS NOT NULL )
		            DROP TABLE #dbspace
		
	            CREATE TABLE #dbspace (
		            [name]		SYSNAME,
		            [caminho]	VARCHAR(200),
		            [tamanho]	VARCHAR(10),
		            [drive]		VARCHAR(30)
	            )
	
	            IF ( OBJECT_ID('tempdb..#espacodisco') IS NOT NULL )
		            DROP TABLE #espacodisco

	            CREATE TABLE [#espacodisco] (
		            [Drive]				VARCHAR(10) ,
		            [TamanhoMB]		INT,
		            [UsadoMB]		INT,
		            [LivreMB]		INT,
		            [LivrePerc]			INT,
		            [UsadoPerc]			INT,
		            [OcupadoSQ_LMB]	INT, 
		            [Data]				SMALLDATETIME
	            )
	
	            IF ( OBJECT_ID('tempdb..#space') IS NOT NULL ) 
		            DROP TABLE #space 

	            CREATE TABLE #space (
		            [drive]		CHAR(1),
		            [mbfree]	INT
	            )

	            -- Cria a tabela processo que ira armazenar os dados envio de email
	            IF ( OBJECT_ID('tempdb..##VerificaProc') IS NOT NULL )
		            DROP TABLE ##VerificaProc
	
                 CREATE TABLE ##VerificaProc
	               ([Duração]             VARCHAR(1000),
	                [database_name]       VARCHAR(1000),
	                [login_name]          VARCHAR(1000),
	                [host_name]           VARCHAR(1000),
	                [start_time]          VARCHAR(1000),
	                [status]              VARCHAR(1000),
	                [session_id]          VARCHAR(1000),
	                [blocking_session_id] VARCHAR(1000),
	                [Wait]                VARCHAR(1000),
	                [open_tran_count]     VARCHAR(1000),
	                [CPU]                 VARCHAR(1000),
	                [reads]               VARCHAR(1000),
	                [writes]              VARCHAR(1000),
	                [sql_command]         VARCHAR(1000),
	               );
			
	            -- Popula as tabelas com as informações sobre o Espaço em Disco
	            EXEC sp_MSforeachdb 'Use [?] INSERT INTO #dbspace SELECT CONVERT(VARCHAR(25), DB_Name()) ''Database'', CONVERT(VARCHAR(60), FileName), CONVERT(VARCHAR(8), Size / 128) ''Size in MB'', CONVERT(VARCHAR(30), Name) FROM sysfiles'

	            -- Declara as variaveis
	            DECLARE @hr INT, @fso INT, @mbtotal INT, @TotalSpace INT, @MBFree INT, @Percentage INT,
			            @SQLDriveSize INT, @size float, @drive VARCHAR(1), @fso_Method VARCHAR(255)

	            SELECT	@mbtotal = 0, 
			            @mbtotal = 0
			
	            EXEC @hr = [master].[dbo].[sp_OACreate] 'Scripting.FilesystemObject', @fso OUTPUT
		
	            INSERT INTO #space 
	            EXEC [master].[dbo].[xp_fixeddrives]
	
	            -- Utiliza o Cursor para gerar as informações de cada Disco
	            DECLARE CheckDrives CURSOR FOR SELECT drive,mbfree FROM #space
	            OPEN CheckDrives
	            FETCH NEXT FROM CheckDrives INTO @drive, @MBFree
	            WHILE(@@FETCH_STATUS = 0)
	            BEGIN
		            SET @fso_Method = 'Drives("" + @drive + :"").TotalSize'


                    SELECT @SQLDriveSize = SUM(CONVERT(INT, [tamanho]))

                    FROM #dbspace 
		            WHERE SUBSTRING([caminho], 1, 1) = @drive


                    EXEC @hr = [sp_OAMethod] @fso, @fso_Method, @size OUTPUT


                    SET @mbtotal = @size / (1024 * 1024)


                    INSERT INTO #espacodisco 
		            VALUES(@drive + ':', @mbtotal, @mbtotal - @MBFree, @MBFree, (100 * ROUND(@MBFree, 2) / ROUND(@mbtotal, 2)),
                            (100 - 100 * ROUND(@MBFree, 2) / ROUND(@mbtotal, 2)), @SQLDriveSize, GETDATE())


                    FETCH NEXT FROM CheckDrives INTO @drive, @MBFree

                END
                CLOSE CheckDrives
                DEALLOCATE CheckDrives

                -- Tabela com os dados resumidos sobre o Espaço em Disco

                IF(OBJECT_ID('_DTS_Espacodisco ') IS NOT NULL)

                    DROP TABLE _DTS_Espacodisco

	            SELECT *

                INTO[dbo].[_DTS_Espacodisco]

                FROM #espacodisco

				---SELECT * FROM [dbo].[_DTS_Espacodisco]

	            -- Cria a tabela que ira armazenar os dados dos processos

                IF(OBJECT_ID('tempdb..#Resultado_WhoisActive') IS NOT NULL)

                    DROP TABLE #Resultado_WhoisActive
				

                CREATE TABLE #Resultado_WhoisActive (		
		            [dd hh:mm: ss.mss]		VARCHAR(20),

                    [database_name] NVARCHAR(128),		
		            [login_name] NVARCHAR(128),
		            [host_name] NVARCHAR(128),
		            [start_time] DATETIME,
		            [status] VARCHAR(30),
		            [session_id] INT,
		            [blocking_session_id] INT,
		            [wait_info] VARCHAR(MAX),
		            [open_tran_count] INT,
		            [CPU] VARCHAR(MAX),
		            [reads] VARCHAR(MAX),
		            [writes] VARCHAR(MAX),
		            [sql_command] XML
	            )

	            -- Declara as variaveis
                DECLARE @Subject VARCHAR(500), @Fl_Tipo TINYINT, @Importance AS VARCHAR(6),@EmailBody VARCHAR(MAX), 
			            @AlertaDiscoHeader VARCHAR(MAX),@AlertaDiscoTable VARCHAR(MAX), @EmptyBodyEmail VARCHAR(MAX), @EmailDestination VARCHAR(200),
			            @ResultadoWhoisactiveHeader VARCHAR(MAX), @ResultadoWhoisactiveTable VARCHAR(MAX), @Espaco_Disco_Parametro INT, @TextRel1 VARCHAR(4000), 
			            @TextRel2 VARCHAR(4000), @NomeRel VARCHAR(300),@MntMsg VARCHAR(200), @TLMsg VARCHAR(200), @SendMail VARCHAR(200), @ProfileDBMail VARCHAR(50), 
			            @BodyFormatMail VARCHAR(20), @CaminhoPath VARCHAR(50), @CaminhoFim VARCHAR(50), @Ass VARCHAR(4000),@HTML VARCHAR(MAX), @Query VARCHAR(MAX)

	            --------------------------------------------------------------------------------------------------------------------------------
	            -- Recupera os parametros do Alerta
	            --------------------------------------------------------------------------------------------------------------------------------
	            -- Espaco Disco

                DECLARE @Id_AlertaParametro INT = (SELECT Id_AlertaParametro FROM[InitDB].[dbo].AlertaParametro(NOLOCK) WHERE Nm_Alerta = 'Espaco Disco')
	
	            -- Email, Parametro, Id Telegram, Caminho dos reports, Profile DB Mail, Body Format Mail

                SELECT @NomeRel = Nm_Alerta,
                      @Espaco_Disco_Parametro = Vl_Parametro,
                      @EmailDestination = Ds_Email,
                      @TLMsg = Ds_MSG,
                      @CaminhoPath = Ds_Caminho,
                      @ProfileDBMail = Ds_ProfileDBMail,
                      @BodyFormatMail = Ds_BodyFormatMail,
                      @importance = Ds_TipoMail

                FROM[dbo].[AlertaParametro]
                WHERE[Id_AlertaParametro] = @Id_AlertaParametro	

	            -- Verifica o último Tipo do Alerta registrado -> 0: CLEAR / 1: ALERTA
                SELECT @Fl_Tipo = [Fl_Tipo]
                FROM[dbo].[Alerta]
                WHERE[Id_Alerta] = (SELECT MAX(Id_Alerta) FROM[dbo].[Alerta] WHERE[Id_AlertaParametro] = @Id_AlertaParametro )

	            /*******************************************************************************************************************************
	            --	Verifica o Espaço Livre em Disco
	            *******************************************************************************************************************************/
	            IF EXISTS(
                                SELECT NULL

                                FROM[dbo].[_DTS_Espacodisco]
                                WHERE [Usado (%)] > @Espaco_Disco_Parametro

                            )

                BEGIN	-- INICIO - ALERTA
                    IF ISNULL(@Fl_Tipo, 0) = 0	-- Envia o Alerta apenas uma vez

                    BEGIN
			            --------------------------------------------------------------------------------------------------------------------------------
			            --	ALERTA - DADOS - WHOISACTIVE
			            --------------------------------------------------------------------------------------------------------------------------------
			            -- Retorna todos os processos que estão sendo executados no momento

                        EXEC[dbo].[sp_WhoIsActive]
                @get_outer_command =	1,
					            @output_column_list =	'[dd hh:mm:ss.mss][database_name][login_name][host_name][start_time][status][session_id][blocking_session_id][wait_info][open_tran_count][CPU][reads][writes][sql_command]',
					            @destination_table =	'#Resultado_WhoisActive'
						    
			            -- Altera a coluna que possui o comando SQL

                        ALTER TABLE #Resultado_WhoisActive
			            ALTER COLUMN[sql_command] VARCHAR(MAX)


                        UPDATE #Resultado_WhoisActive
			            SET[sql_command] = REPLACE(REPLACE(REPLACE(REPLACE(CAST([sql_command] AS VARCHAR(1000)), '<?query --', ''), '--?>', ''), '&gt;', '>'), '&lt;', '')
			
			            -- Verifica se não existe nenhum processo em Execução

                        IF NOT EXISTS(SELECT TOP 1 * FROM #Resultado_WhoisActive )
			            BEGIN

                            INSERT INTO #Resultado_WhoisActive
				            SELECT NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL
                        END


			            -- Insert de dados para envio de Processo
                         --INSERT INTO ##VerificaProc ( [Duração],[database_name],[login_name],[host_name],[start_time],[status],[session_id],[blocking_session_id],[Wait],[open_tran_count],[CPU],[reads],[writes],[sql_command] )
		                 --SELECT * FROM #Resultado_WhoisActive

			            /*******************************************************************************************************************************
			            --	CRIA O EMAIL - ALERTA
			            *******************************************************************************************************************************/
			            -- Parametros do Alerta

                        SET @Subject = (SELECT 'ALERTA #DISCO - Detectado problema de espaço em disco no Servidor: ' + @@SERVERNAME +', Utilização acima de ' +CAST((@Espaco_Disco_Parametro) AS VARCHAR)+'%.')
			            SET @TextRel1 = 'Prezados,<BR /><BR /> Identifiquei um problema de espaço em disco. Volume de disco com mais de ' + CAST((@Espaco_Disco_Parametro)AS VARCHAR) + '% de utilização no Servidor: <b>' + @@SERVERNAME +',</b> verifique o relatório abaixo com <b>urgência</b>.'	
			            SET @TextRel2 = 'Processos em execução no Banco de Dados.'

                        SET @CaminhoFim = @CaminhoPath + @NomeRel + '.html'

                        -- Gera Primeiro bloco de HTML
                        SET @Query = '	SELECT [Drive], CAST([Tamanho (MB)] AS VARCHAR) AS [Tamanho (MB)], CAST([Usado (MB)] AS VARCHAR) AS [Usado (MB)], CAST([Livre (MB)] AS VARCHAR) AS [Livre (MB)], 
								            CAST([Livre(%)] AS VARCHAR) AS[Livre(%)], CAST([Usado(%)] AS VARCHAR) AS[Usado(%)], CAST([Ocupado SQL(MB)] AS VARCHAR) AS[Ocupado SQL(MB)]
                             FROM[InitDB].[dbo].[_DTS_Espacodisco]'
			            SET @HTML = dbo.fncExportaMultiHTML(@Query, @TextRel1, 2, 1)
                        -- Gera Segundo bloco de HTML
                        SET @Query = 'SELECT * FROM #Resultado_WhoisActive'
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
                         EXEC[msdb].[dbo].[sp_send_dbmail]
                                        @profile_name = @ProfileDBMail,
						                @recipients = @EmailDestination,
						                @body_format = @BodyFormatMail,
						                @subject = @Subject,
						                @importance = @Importance,
						                @body = @HTML;
			 
			             -- Envio do Telegram
                         SET @MntMsg = @Subject+', Verifique os detalhes no e-mail com *urgência*'
			             EXEC dbo.StpSendMsgTelegram

                            @Destino = @TLMsg,--'49353855',
                            @Msg = @MntMsg

                        /*******************************************************************************************************************************
			            -- Insere um Registro na Tabela de Controle dos Alertas -> Fl_Tipo = 1 : ALERTA
			            *******************************************************************************************************************************/
                        INSERT INTO [dbo].[Alerta] ( [Id_AlertaParametro], [Ds_Mensagem], [Fl_Tipo] )
                        SELECT @Id_AlertaParametro, @Subject, 1			

                    END
                END		-- FIM - ALERTA
                ELSE

                BEGIN   -- INICIO - CLEAR
                    IF @Fl_Tipo = 1

                    BEGIN
			            --------------------------------------------------------------------------------------------------------------------------------
			            --	CLEAR - DADOS - WHOISACTIVE
			            --------------------------------------------------------------------------------------------------------------------------------		      
			            -- Retorna todos os processos que estão sendo executados no momento

                        EXEC[dbo].[sp_WhoIsActive]
                                @get_outer_command =    1,
                                @output_column_list = '[dd hh:mm:ss.mss][database_name][login_name][host_name][start_time][status][session_id][blocking_session_id][wait_info][open_tran_count][CPU][reads][writes][sql_command]',
                                @destination_table = '#Resultado_WhoisActive'

                        -- Altera a coluna que possui o comando SQL

                        ALTER TABLE #Resultado_WhoisActive
			            ALTER COLUMN [sql_command] VARCHAR(MAX)

                        UPDATE #Resultado_WhoisActive
			            SET[sql_command] = REPLACE(REPLACE(REPLACE(REPLACE(CAST([sql_command] AS VARCHAR(1000)), '<?query --', ''), '--?>', ''), '&gt;', '>'), '&lt;', '')
			
			            -- select* from #Resultado_WhoisActive
			
			            -- Verifica se não existe nenhum processo em Execução

                        IF NOT EXISTS(SELECT TOP 1 * FROM #Resultado_WhoisActive )
			            BEGIN

                            INSERT INTO #Resultado_WhoisActive
				            SELECT NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL
                        END

			            -- Insert de dados para envio de Processo
                         --INSERT INTO ##VerificaProc ( [Duração],[database_name],[login_name],[host_name],[start_time],[status],[session_id],[blocking_session_id],[Wait],[open_tran_count],[CPU],[reads],[writes],[sql_command] )
		                 --SELECT * FROM #Resultado_WhoisActive

			            /*******************************************************************************************************************************
			            --	ALERTA - ENVIA O EMAIL - ENVIA TELEGRAM
			            *******************************************************************************************************************************/
			            SET @Subject = (SELECT 'Solução #DISCO - Sem problema de espaço em disco, utilização abaixo de ' +CAST((@Espaco_Disco_Parametro) AS VARCHAR)+ '% no Servidor: ' + @@SERVERNAME)
			            SET @TextRel1 = 'Prezados,<BR /><BR /> Problema de espaço em disco solucionado no <b>' + @@SERVERNAME +'</b>.'	
			            SET @TextRel2 = 'Processos em execução no Banco de Dados.'

                        SET @CaminhoFim = @CaminhoPath + @NomeRel + '.html'

                        -- Gera Primeiro bloco de HTML
                        SET @Query = '	SELECT [Drive], CAST([Tamanho (MB)] AS VARCHAR) AS [Tamanho (MB)], CAST([Usado (MB)] AS VARCHAR) AS [Usado (MB)], CAST([Livre (MB)] AS VARCHAR) AS [Livre (MB)], 
								            CAST([Livre(%)] AS VARCHAR) AS[Livre(%)], CAST([Usado(%)] AS VARCHAR) AS[Usado(%)], CAST([Ocupado SQL(MB)] AS VARCHAR) AS[Ocupado SQL(MB)]
                             FROM[InitDB].[dbo].[_DTS_Espacodisco]'
			            SET @HTML = dbo.fncExportaMultiHTML(@Query, @TextRel1, 2, 1)
                        -- Gera Segundo bloco de HTML
                        SET @Query = 'SELECT * FROM #Resultado_WhoisActive'
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
			            --	ALERTA - ENVIA O EMAIL
			            *******************************************************************************************************************************/
                        EXEC[msdb].[dbo].[sp_send_dbmail]
                                        @profile_name = @ProfileDBMail,
						                @recipients = @EmailDestination,
						                @body_format = @BodyFormatMail,
						                @subject = @Subject,
						                @importance = @Importance,
						                @body = @HTML;
			 
			             -- Envio do Telegram
                        SET @MntMsg = @Subject+', Verifique os detalhes no e-mail'
			            EXEC dbo.StpSendMsgTelegram

                            @Destino = @TLMsg,--'49353855',
                            @Msg = @MntMsg

                        /*******************************************************************************************************************************
			            -- Insere um Registro na Tabela de Controle dos Alertas -> Fl_Tipo = 0 : CLEAR
			            *******************************************************************************************************************************/
                        INSERT INTO [dbo].[Alerta] ( [Id_AlertaParametro], [Ds_Mensagem], [Fl_Tipo] )
                        SELECT @Id_AlertaParametro, @Subject, 0		

                    END
                END		-- FIM - CLEAR

                ";
        // Execute the command and send back the results
        SqlContext.Pipe.ExecuteAndSend(myCommand);
    }
};