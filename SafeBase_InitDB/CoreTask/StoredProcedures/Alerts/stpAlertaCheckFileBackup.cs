
using System;
using System.Collections.Generic;
using System.Text;

namespace InitDB.Client
{
    class stpAlertaCheckFileBackup
    {
        public static string Query(string type)
        {
            return
			//@"insert into [dbo].[Testedb] ([Nome],[DateTest]) values ('Teste da ferramenta DB - stpcheckFileBackup',GETDATE())";

			@"  
            
            SET NOCOUNT ON;
			
			SET QUOTED_IDENTIFIER ON;
			
			---- Parametriza qual alerta no momento			
			DECLARE @TipodeBackup  VARCHAR(5)

			SET @TipodeBackup =  '" + type + @"'
			
			DECLARE @Id_AlertaParametro INT; 
			
			IF @TipodeBackup = 'I'
				SET @Id_AlertaParametro  = (SELECT Id_AlertaParametro FROM [dbo].AlertaParametro (NOLOCK) WHERE Nm_Alerta = 'Alerta File DB DIFF' AND Ativo = 1)
			ELSE
				SET @Id_AlertaParametro  = (SELECT Id_AlertaParametro FROM [dbo].AlertaParametro (NOLOCK) WHERE Nm_Alerta = 'Alerta File DB' AND Ativo = 1)	

            ---- Recupera os parametros base           
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
	            @File NVARCHAR(10),@ZabbixPath varchar(128), @ZabbixServer varchar(128), @ZabbixLocalServer varchar(128), @ZabbixAlertName varchar(128)

			---- Exclusao de Database da Rotina
			DECLARE
				@ExcludeDB	varchar(4000)=NULL,
				@Start INT,
				@End INT =0,
				@Name SYSNAME=''
  
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
			
			SET @File = '" + type + @"'
			
			DECLARE @TagBackup VARCHAR(30) = CASE @File
													WHEN 'D' THEN 'BackupFull'
													WHEN 'I' THEN 'BackupDifferential'
													WHEN 'L' THEN 'BackupLog'
													ELSE 'BackupFull'
													END 
			DECLARE @Extensao VARCHAR(30) = CASE @File
													WHEN 'D' THEN '.bak'
													WHEN 'I' THEN '.dif'
													WHEN 'L' THEN '.trn'
													ELSE '.bak'
													END 
			-- Verifica o último Tipo do Alerta registrado
	        -- 0: CLEAR 
	        -- 1: ALERTA	
	        SELECT @Fl_Tipo = [Fl_Tipo]
	        FROM [dbo].[Alerta]
	        WHERE [Id_Alerta] = (SELECT MAX(Id_Alerta) FROM [dbo].[Alerta] WHERE [Id_AlertaParametro] = @Id_AlertaParametro )

		
			-- Cria a tabela que ira armazenar os dados dos processos
			IF(OBJECT_ID('tempdb..#tb_bkp') IS NOT NULL)
				DROP TABLE #tb_bkp;
			CREATE TABLE #tb_bkp
				([sqldb] NVARCHAR(400),
				[dir] NVARCHAR(900));

			---- Cria a tabela de exclusao de database
			IF OBJECT_ID('Tempdb..#TmpExcludeDB') IS NOT NULL
				DROP TABLE #TmpExcludeDB
			CREATE TABLE #TmpExcludeDB
			(
			Id INT IDENTITY(1,1),
			DatabaseName varchar(128)
			);						

			-- Declara valores de contexto
			DECLARE @Lines TABLE (Line NVARCHAR(MAX)) ;
			DECLARE @FullText NVARCHAR(MAX) = '' ;
			DECLARE @M VARCHAR(200), @X VARCHAR(MAX)
			DECLARE @QB NVARCHAR(1000)
			DECLARE @QP NVARCHAR(1000)
			DECLARE @BX NVARCHAR(1000)


			SET  @QB = 'SELECT TOP 1   
						@BackupPath_OUT =  ParametersXML.value(''(/Customer/'+@TagBackup+'/BackupPath)[1]'', ''varchar(max)'')
						+CASE WHEN RIGHT((ParametersXML.value(''(/Customer/'+@TagBackup+'/BackupPath)[1]'', ''varchar(max)'')), 1) = ''\\'' THEN '''' ELSE ''\\'' END	+ @@ServerName + ''\\'' + 
						+CASE WHEN RIGHT(@@ServerName, LEN(@@ServiceName)) = @@ServiceName THEN '''' ELSE @@ServiceName + ''\\'' END,
						@ExcludeDB_OUT = ParametersXML.value(''(/Customer/'+@TagBackup+'/ExcludeDB)[1]'', ''varchar(max)'')
					FROM [dbo].[ConfigDB]
			'
			SET @QP='@BackupPath_OUT nvarchar(4000) OUTPUT, @ExcludeDB_OUT nvarchar(4000) OUTPUT'

			EXECUTE sp_executesql @QB, @QP, @BackupPath_OUT = @BX OUTPUT,@ExcludeDB_OUT = @ExcludeDB OUTPUT
	            
			/*Loop exclude db*/
			SET @Start = 1
		
			IF @ExcludeDB IS NOT NULL
			BEGIN	
				WHILE @Start >0
				BEGIN
					/*Controle para o Loop*/
					SET @Start = CHARINDEX(';', @ExcludeDB)
		
					/*Posição para encerrar a String*/
					SET @End = CHARINDEX(';', @ExcludeDB);

					/*Se tivermos ainda alguma vígula, senão tras o restante da string*/
					IF @End > 0
					BEGIN
						SET @Name =  SUBSTRING(@ExcludeDB,1,@End-1)
					END				
					ELSE
					BEGIN
						SET @Name =  @ExcludeDB
					END

					/*Trunca a string à partir da ultima virgula, se não tiver vígula esse resultado trará ''*/
					SET @ExcludeDB = SUBSTRING(@ExcludeDB,LEN(@Name)+2,(LEN(@ExcludeDB)-LEN(@Name)))

					/*Para no caso do usuário encerrar com vígula, descarta o resultado vazio*/
					IF @Name IS NOT NULL
					BEGIN				
						INSERT INTO #TmpExcludeDB (DatabaseName) values (@Name)
					END
				END
			END

			DECLARE @F NVARCHAR(767)
			DECLARE cursor_d CURSOR

			FOR 

			SELECT RTRIM(name) name 
			FROM sys.databases sd
			INNER JOIN fncListarDiretorio (''+@BX+'', '*') ld ON sd.name = ld.arquivo
			WHERE sd.state_desc not in ('OFFLINE','RESTORING','RECOVERY_PENDING') and sd.is_in_standby = 0 and sd.is_read_only = 0 and sd.database_id > 4 and [name] not like 'SafeBase'
					AND sd.NAME NOT IN (SELECT 
											ADC.database_name                               
										FROM sys.availability_groups_cluster as AGC                                                                            
										JOIN sys.dm_hadr_availability_replica_cluster_states as RCS ON AGC.group_id = RCS.group_id                             
										JOIN sys.dm_hadr_availability_replica_states as ARS ON RCS.replica_id = ARS.replica_id and RCS.group_id = ARS.group_id 
										JOIN sys.availability_databases_cluster as ADC ON AGC.group_id = ADC.group_id                                          
										WHERE ARS.is_local = 1
										AND ARS.role_desc LIKE 'SECONDARY')
	
			OPEN cursor_d;
			FETCH NEXT FROM cursor_d INTO @F
			WHILE @@FETCH_STATUS = 0

				BEGIN 

					DECLARE @TypeBackup VARCHAR(30) = CASE @File
														WHEN 'D' THEN '.bak'
														WHEN 'I' THEN '.dif'
														WHEN 'L' THEN '.trn'
														else '.bak'
														END 

					IF NOT EXISTS (	SELECT DatabaseName as Arquivo 
									FROM 
										#TmpExcludeDB
									WHERE DatabaseName = @F
									UNION
									SELECT Arquivo 
									FROM dbo.fncListarDiretorio (''+@BX + @F+'', '*') A
									WHERE @F in (
												SELECT RTRIM(Banco) 
												FROM vwcheckbackup 
												WHERE banco LIKE @F -- AND DataLog is not null -- AND DataDiff is not null
												)
										AND (
											(	
												@TypeBackup = '.bak'
												AND DataCriacao >= (GetDate()-6)				-- Arquivo a mais de uma semana e extensao
												AND Extensao like @TypeBackup
											)
										OR 
											(												
												@TypeBackup = '.dif' 
												AND 													
												(
													(												
													CONVERT(char(10), DataModificacao,126)  = CONVERT(char(10), GetDate(),126)
													AND Extensao like @TypeBackup 													
													)
																							
												OR
													@F IN ('master')									
												)-- Databse master nao tem Dif
											)			
										OR 
											(					
												@TypeBackup = '.trn' 
												AND
												(
													(
														CONVERT(char(10), DataModificacao,126)  = CONVERT(char(10), GetDate(),126)
														AND Extensao like @TypeBackup
													)																							
												OR
													@F IN (SELECT NAME FROM sys.databases where recovery_model_desc = 'SIMPLE')
												)
											)																				
										)
								)
					BEGIN

						insert #tb_bkp ([sqldb],[dir])
						select @F,+@BX + @F

						insert @Lines select @F+'; '

						SELECT @M = 'Alerta: Arquivo de #Backup do banco: #'+@F+@TypeBackup+' Não Encontrado, na instância ' +@@servername + '.'
						PRINT @M

					END
					/* DESCOMENTE PRA VER TODOS OS BANCOS OK
					ELSE
					BEGIN

						PRINT 'AQUIVO DE BACKUP: '+@F+' - TIPO: '+@TypeBackup+' - OK' 

					END
					*/
			
					FETCH NEXT FROM cursor_d into @F
			
				END;

				select @FullText = @FullText + Char(13) +  Line from @Lines ;
				select @X = ' - Arquivos de #Backup dos bancos não encontrados na instância ' +@@servername + ': ' + @FullText

			CLOSE cursor_d;
			DEALLOCATE cursor_d;


			IF EXISTS(select [sqldb] from #tb_bkp where [sqldb] <> '' )
			BEGIN
				IF ISNULL(@Fl_Tipo, 0) = 0	-- INICIO - ALERTA
				BEGIN

					/*******************************************************************************************************************************
					--	CRIA O EMAIL - ALERTA
					*******************************************************************************************************************************/			
					-- Parametros do Alerta
					SET @Subject =  @Ds_Email_Assunto_alerta +' '+ @@SERVERNAME
					SET @TextRel1 =  @Ds_Email_Texto_alerta +'<p style=color:red;> Tipo de Arquivo faltante: '+@TypeBackup + '</p><BR /><BR />USE [SafeBase] <BR /><BR />GO <BR /> <BR />DECLARE @DB NVARCHAR(500) = ''NOME DO BANCO'' -- INFORME O NOME DO BANCO <BR />DECLARE @BX NVARCHAR(1000) = (SELECT TOP 1 <BR />ParametersXML.value(''(/Customer/'+@TagBackup+'/BackupPath)[1]'', ''varchar(max)'') <BR />+CASE WHEN RIGHT((ParametersXML.value(''(/Customer/'+@TagBackup+'/BackupPath)[1]'', ''varchar(max)'')), 1) = ''\\'' THEN '''' ELSE ''\\'' END	+ @@ServerName + ''\\'' + <BR />+CASE WHEN RIGHT(@@ServerName, LEN(@@ServiceName)) = @@ServiceName THEN '''' ELSE @@ServiceName + ''\\'' END AS BackupPath <BR />FROM [dbo].[ConfigDB]) <BR /><BR />SELECT * FROM dbo.fncListarDiretorio (''''+@BX + @DB+'''', ''*'')'	
					SET @CaminhoFim = @Ds_Caminho_Base + @CaminhoPath + @NomeRel +'.html'
			 
					-- Gera Primeiro bloco de HTML
					SET @Query = 'select [sqldb] as Banco, [dir] as Diretorio from #tb_bkp order by sqldb'
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
							DECLARE @TLF NVARCHAR(MAX)
							SET @TLF = @MntMsg + @X
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
			END			            
			ELSE
			BEGIN
				select @FullText = @FullText + Char(13) +  Line from @Lines ;	
				select @X = 'Todos arquivos de Backup:'+ @TypeBackup + ' dos bancos foram encontrados na instância ' +@@servername + ': ' + @FullText
				/*******************************************************************************************************************************
				--  ALERTA - ENVIA O EMAIL E MENSSAGEIROS
				*******************************************************************************************************************************/
				IF ISNULL(@Fl_Tipo, 1) = 1	-- INICIO - ALERTA
				BEGIN
				
					DECLARE cursor_db CURSOR
					FOR 

					SELECT RTRIM(name) name 
					FROM sys.databases sd
					INNER JOIN fncListarDiretorio (''+@BX+'', '*') ld ON sd.name = ld.arquivo
					WHERE sd.state_desc not in ('OFFLINE','RESTORING','RECOVERY_PENDING') and sd.is_in_standby = 0 and sd.is_read_only = 0 and sd.database_id > 4 and [name] not like 'SafeBase'
							AND sd.NAME NOT IN (SELECT 
													ADC.database_name                               
												FROM sys.availability_groups_cluster as AGC                                                                            
												JOIN sys.dm_hadr_availability_replica_cluster_states as RCS ON AGC.group_id = RCS.group_id                             
												JOIN sys.dm_hadr_availability_replica_states as ARS ON RCS.replica_id = ARS.replica_id and RCS.group_id = ARS.group_id 
												JOIN sys.availability_databases_cluster as ADC ON AGC.group_id = ADC.group_id                                          
												WHERE ARS.is_local = 1
												AND ARS.role_desc LIKE 'SECONDARY')

					OPEN cursor_db;
					FETCH NEXT FROM cursor_db INTO @F
					WHILE @@FETCH_STATUS = 0

						BEGIN 
								insert #tb_bkp ([sqldb],[dir])
								SELECT 
									@F,Arquivo 
								FROM 
									dbo.fncListarDiretorio (''+@BX + @F+'', '*') A
								WHERE
									Extensao = @Extensao
					
			
							FETCH NEXT FROM cursor_db into @F
			
						END;
				

					CLOSE cursor_db;
					DEALLOCATE cursor_db;
				
					SET @Subject =  @Ds_Email_Assunto_solucao +' '+ @@SERVERNAME
					SET @TextRel1 =  @Ds_Email_Texto_solucao +'<BR /> Tipo de Arquivo: '+@TypeBackup +'<BR />' 				             
					SET @CaminhoFim = @Ds_Caminho_Base + @CaminhoPath + @NomeRel +'.html'

					-- Gera Primeiro bloco de HTML
			    
				
					set @Query = 'select [sqldb],[dir] from #tb_bkp order by [sqldb] '
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
