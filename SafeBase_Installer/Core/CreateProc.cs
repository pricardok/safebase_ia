using System;
using System.Collections.Generic;
using System.Text;
using SafeBase_Installer.Core;

namespace SafeBase_Installer
{
    class CreateProc
    {
        public static string Query(string use)
        {
            return
            @"
            USE "+ use + @"
            GO

            SET ANSI_NULLS ON
            GO
            SET QUOTED_IDENTIFIER ON
            GO

            -- stpObjetosImport
            CREATE PROCEDURE [dbo].[stpObjetosImport]

            @Objeto nvarchar(800),
            @Arquivo nvarchar(800),
            @DsDatabase SYSNAME,
            @Nmschema nvarchar(200)

            WITH ENCRYPTION
            AS

            /*

            EXEC [SafeBase].[dbo].[stpObjetosImport] 'eCommerce','C:\Data\Jobs\Reports\fsCalcularData.sql'

              SELECT
	              [Id]
                  ,[DataEvento]
                  ,[TipoEvento]
                  ,[Database]
                  ,[Usuario]
                  ,[Host]
                  ,[Schema]
                  ,[Objeto]
                  ,[TipoObjeto]
                  ,[DesQuery]
              FROM [SafeBase].[dbo].[HistoricoVersionamentoDB] order by DataEvento desc

            */
            begin	
	            DECLARE @Lines TABLE (Line NVARCHAR(MAX)) ;
	            DECLARE @FullText NVARCHAR(MAX) = '' ;
	            DECLARE @B NVARCHAR(767)
	            DECLARE cursor_imp CURSOR
                FOR 

                    select Nr_Linha from [dbo].[fncLerArquivo](''+@Arquivo+'')
		
                OPEN cursor_imp;
                FETCH NEXT FROM cursor_imp INTO @B
                WHILE @@FETCH_STATUS = 0

                    begin
            
			            insert @Lines select Ds_Texto from [dbo].[fncLerArquivo](''+@Arquivo+'') where Nr_Linha = @B
 
                        FETCH NEXT FROM cursor_imp into @B
			
                    END;
			
		            select @FullText = @FullText + Char(13) + Line from @Lines ; 
	
                CLOSE cursor_imp;
                DEALLOCATE cursor_imp;

	            IF(OBJECT_ID('tempdb..#tb_imp') IS NOT NULL)
                DROP TABLE #tb_imp;
	            CREATE TABLE #tb_imp
	                ([sqldb] NVARCHAR(max));

	            INSERT INTO #tb_imp
	            SELECT @FullText;

	            --SET @FullText  = '' ;

	            DECLARE @x XML = (SELECT [sqldb] FROM #tb_imp FOR XML PATH(''), ROOT('CustomersData'))

	            INSERT INTO safebase.dbo.HistoricoVersionamentoDB (DataEvento,TipoEvento,[Database],Host,[Schema],Objeto,DesQuery)
	            SELECT (getdate()),'Versionamento DB',@DsDatabase,(@@servername),@Nmschema,@Objeto,@x

              /*
              SELECT
	              [Id]
                  ,[DataEvento]
                  ,[TipoEvento]
                  ,[Database]
                  ,[Usuario]
                  ,[Host]
                  ,[Schema]
                  ,[Objeto]
                  ,[TipoObjeto]
                  ,[DesQuery]
              FROM [SafeBase].[dbo].[HistoricoVersionamentoDB] order by DataEvento desc
              */
	
            END

            GO
            
            -- stpGetCheckLoginCA
            CREATE PROCEDURE [dbo].[stpGetCheckLoginCA]
            AS
            /*
            METODO DE USO
            EXEC [dbo].[stpGetCheckLoginCA]

            -- VERIFICAR LOGINS CRIADOS E ALTERADOS E ENVIA ALERTA
            SELECT DataEvento,Server,Login,NomeHost,Aplicacao,DesEvento AS [sql],DesMSG  from HistoricoAuditLogins WHERE [AlertaEnviado] = 0 order by DataEvento desc

            */

            IF EXISTS (SELECT TOP 1 [AlertaEnviado] from HistoricoAuditLogins WHERE [AlertaEnviado] = 0)

            BEGIN 

	            DECLARE @TextRel1 VARCHAR(4000), @CaminhoFim VARCHAR(50), @Ass VARCHAR(4000),@HTML VARCHAR(MAX), @Query VARCHAR(MAX), @Subject VARCHAR(600),@MntMsg VARCHAR(MAX)
	            DECLARE @Teams INT = 2000
	            DECLARE @Emails NVARCHAR(150) = 'dataservices@facta.com.br'
	            --DECLARE @Emails NVARCHAR(150) = 'paulo.kuhn@facta.com.br'
	
	            DECLARE @DB NVARCHAR(50) =  @@SERVERNAME
	            SET @Subject =  'Alerta #Usuario - ' + @@SERVERNAME
	            SET @TextRel1 = 'Prezados, Identifiquei que na instancia <strong>'+@DB+'</strong>, um ou mais usuários acabam de ser criados ou tiveram suas permissões alteradas, favor verifique esta informação.'  
	            SET @CaminhoFim = 'C:\Data\Jobs\ReportsUserLocal' +'.html'
	            SET @MntMsg = @Subject 
	            -- 
	            -- Gera Primeiro bloco de HTML
	            SET @Query = 'SELECT DataEvento,Server,Login,NomeHost,Aplicacao,DesEvento AS [sql],DesMSG  from HistoricoAuditLogins WHERE [AlertaEnviado] = 0  AND DesEvento NOT LIKE ''Login nao permitido'' order by DataEvento desc'
	            SET @HTML = safebase.dbo.fncExportaMultiHTML(@Query, @TextRel1, 2, 1)
	            -- Gera Segundo bloco de HTML
	            SET @Ass = (SELECT Assinatura FROM safebase.dbo.MailAssinatura WHERE Id = 1)
	            select @HTML = @HTML + @Ass
	            -- Salva Arquivo HTML de Envio
	            EXEC safebase.dbo.stpWriteFile 
		            @Ds_Texto = @HTML, -- nvarchar(max)
		            @Ds_Caminho = @CaminhoFim, -- nvarchar(max)
		            @Ds_Codificacao = N'UTF-8', -- nvarchar(max)
		            @Ds_Formato_Quebra_Linha = N'windows', -- nvarchar(max)
		            @Fl_Append = 0 -- bit

	            EXEC [msdb].[dbo].[sp_send_dbmail]
		            @profile_name = 'EnviaEmail',
		            @recipients = @Emails, 
		            @body_format = 'HTML',
		            @subject = @Subject,
		            @importance = 'High',
		            @body = @HTML;

	            UPDATE [safebase].[dbo].[HistoricoAuditLogins] SET [AlertaEnviado] = 1

            END
            GO

            -- stpQueryPlansCached
            SET ANSI_NULLS ON
            GO
            SET QUOTED_IDENTIFIER ON
            GO
            CREATE PROC [dbo].[stpQueryPlansCached]
	            @NomeObjeto nvarchar(200) = null
            -- WITH ENCRYPTION
            AS

            /*
	            Metodo de Uso
	            EXEC [dbo].[stpQueryPlansCached]
	            EXEC [dbo].[stpQueryPlansCached] @NomeObjeto = 'usp_AssinaturaDocumentoNovo_GetList'
	            EXEC [dbo].[stpQueryPlansCached] 'usp_AssinaturaDocumentoNovo_GetList'

            */

            DECLARE @dtdb DATETIME--, @NomeObjeto nvarchar(200) = null
            SET @dtdb = GETDATE();
            WITH XMLNAMESPACES(DEFAULT N'http://schemas.microsoft.com/sqlserver/2004/07/showplan',N'http://schemas.microsoft.com/sqlserver/2004/07/showplan' AS ShowPlan)
            SELECT  
	            @dtdb AS Data,
	            DB_NAME(CONVERT(INT, st.dbid)) Banco,
	            OBJECT_SCHEMA_NAME (qp.[objectid],qp.dbid) NomeSchema, 
	            OBJECT_NAME (qp.[objectid],qp.dbid) NomeObjeto,
	            cp.[usecounts] UseCounts,
	            cp.[refcounts] QtdRef_Objeto,
	            cp.[objtype] TipoObjeto,
	            qp.[query_plan].value(N'(/ShowPlanXML/BatchSequence/Batch/Statements/StmtSimple/QueryPlan/MissingIndexes/MissingIndexGroup/@Impact)[1]','[decimal]') as Impacto,
	            --cp.[cacheobjtype],
	            st.[objectid],
	            st.[text] Query,
	            qp.[query_plan]
            FROM sys.dm_exec_cached_plans AS cp
            CROSS APPLY sys.dm_exec_sql_text(cp.[plan_handle]) AS st
            CROSS APPLY sys.dm_exec_query_plan(cp.[plan_handle]) AS qp
            WHERE cp.[usecounts] > 1 
            AND qp.[query_plan].exist(N'/ShowPlanXML/BatchSequence/Batch/Statements/StmtSimple/QueryPlan/MissingIndexes/MissingIndexGroup') <> 0
            AND qp.[query_plan].value(N'(/ShowPlanXML/BatchSequence/Batch/Statements/StmtSimple/QueryPlan/MissingIndexes/MissingIndexGroup/@Impact)[1]','[decimal]') > 90
            AND cp.[objtype] <> 'Prepared'
            AND ((@NomeObjeto IS NULL ) OR (@NomeObjeto = OBJECT_NAME (qp.[objectid],qp.dbid) ))
            AND DB_NAME(st.dbid) IN (SELECT 
					            ADC.database_name                               
				            FROM sys.availability_groups_cluster as AGC                                                                            
				            JOIN sys.dm_hadr_availability_replica_cluster_states as RCS ON AGC.group_id = RCS.group_id                             
				            JOIN sys.dm_hadr_availability_replica_states as ARS ON RCS.replica_id = ARS.replica_id and RCS.group_id = ARS.group_id 
				            JOIN sys.availability_databases_cluster as ADC ON AGC.group_id = ADC.group_id                                          
				            WHERE ARS.is_local = 1
				            AND ARS.role_desc LIKE 'primary')
            ORDER BY cp.[usecounts] DESC

            GO
            -- stpHistoricoWaitsStats
            CREATE procedure [dbo].[stpHistoricoWaitsStats] @Dt_Inicial datetime, @Dt_Final datetime
            WITH ENCRYPTION
            AS
            BEGIN
	            --declare @Dt_Inicial datetime, @Dt_Final datetime
	            --select @Dt_Inicial = '20110505 12:00',@Dt_Final = '20110505 13:00'
	 
	            declare @Wait_Stats table(WaitType varchar(60), Min_Id int, Max_Id int, Menor_Data datetime)
	 
	            insert into @Wait_Stats(WaitType, Min_Id,Max_Id, Menor_Data)
	            select WaitType, min(Id_HistoricoWaitsStats) AS Min_Id, max(Id_HistoricoWaitsStats) AS Max_Id, min(Dt_Referencia) AS Menor_Data
	            from HistoricoWaitsStats (nolock)
	            where Dt_Referencia >= @Dt_Inicial and Dt_Referencia < @Dt_Final
	            group by WaitType
	 
	            -- Tratamento de erro simples para o caso de uma limpeza das estatísticas
	            if exists (select null from @Wait_Stats where WaitType = 'RESET WAITS STATS')
	            begin
		            select	'Foi realizada uma limpeza dos WaitStats' AS WaitType, getdate() AS Min_Log, getdate() AS Max_Log, 0 AS DIf_Wait_S,
				            0 AS DIf_Resource_S, 0 AS DIf_Signal_S, 0 AS DIf_WaitCount, 0 AS DIf_Percentage, 0 AS Last_Percentage
			
		            /*
		            select 'Houve uma limpeza das Waits Stats após a coleta do dia: ' + cast(Menor_Data as varchar) +
		            ' | Favor alterar o período para que não inclua essa limpeza.'
		            from @Wait_Stats where WaitType = 'RESET WAITS STATS'
		            */
		 
		            return
	            End

	            -- Procurar o menor id depois da última limpeza antes do intervalo final e utilizar
	            --tratar caso da limpeza da estatistica
	            select	A.WaitType, B.Dt_Referencia Min_Log, C.Dt_Referencia Max_Log, C.Wait_S - B.Wait_S DIf_Wait_S,
			            C.Resource_S - B.Resource_S DIf_Resource_S, C.Signal_S - B.Signal_S DIf_Signal_S, C.WaitCount - B.WaitCount DIf_WaitCount,
			            C.Percentage - B.Percentage DIf_Percentage, B.Percentage Last_Percentage
	            from @Wait_Stats A
		            join HistoricoWaitsStats B on A.Min_Id = B.Id_HistoricoWaitsStats -- Primeiro
		            join HistoricoWaitsStats C on A.Max_Id = C.Id_HistoricoWaitsStats -- Último 
            END

            GO
            -- stpQueueInfo
  			CREATE PROC [dbo].[stpQueueInfo]
	            (@DB AS      VARCHAR(100),
				 @S as		 VARCHAR(100),
	             @Q AS       VARCHAR(200),
				 @Verbose AS BIT = 0
	            )
                WITH ENCRYPTION AS
                 BEGIN
                     IF @Verbose = 1
                         BEGIN
                             DECLARE @sql1 AS VARCHAR(4000), @sql2 AS VARCHAR(4000);
                             SELECT @sql1 ='USE '+@DB+';
								            SELECT 
									            CASE WHEN '''+@Q+''' LIKE '''' 
										            THEN ISNULL(DATEDIFF(MINUTE, MIN(CAST(
										            LEFT(RIGHT(CAST(message_body AS NVARCHAR(100)), 16),8) + '' ''
										            + LEFT(RIGHT(CAST(message_body AS NVARCHAR(100)), 8),2) + '':''
										            + LEFT(RIGHT(CAST(message_body AS NVARCHAR(100)), 6),2) + '':''
										            + LEFT(RIGHT(CAST(message_body AS NVARCHAR(100)), 4),2)
										            AS DATETIME)), GETDATE()),0) ELSE 0 END AS OldestNotDelivered,
									            COUNT(message_body) AS QuantityNotDelivered,
									            ISNULL((SELECT is_enqueue_enabled FROM sys.service_queues WHERE name = '''+@Q+'''),0) AS statusQueueActivated,
									            ISNULL((SELECT CASE State WHEN ''NOTIFIED'' THEN 1 ELSE 0 END 
										            FROM sys.dm_broker_queue_monitors mon WITH (nolock) 
										            INNER JOIN  sys.service_queues q WITH (nolock) ON q.object_id = mon.queue_id
										            WHERE q.name = '''+@Q+'''
										            AND mon.state <> ''DROPPED''),0) AS NeedsRestart
								            FROM '+@S+'.['+@Q+'] WITH(NOLOCK)
								            WHERE status = 1
								            AND message_body IS NOT NULL';
                             EXEC (@sql1);
							 --PRINT (@sql1);
                         END;
                         ELSE
                         BEGIN
                             SELECT @sql2 ='USE '+@DB+';
								            DECLARE @dt AS VARCHAR(50)
								            SELECT 
									            @dt = ISNULL(DATEDIFF(MINUTE, MIN(CAST(
									            LEFT(RIGHT(CAST(message_body AS NVARCHAR(100)), 16),8) + '' ''
									            + LEFT(RIGHT(CAST(message_body AS NVARCHAR(100)), 8),2) + '':''
									            + LEFT(RIGHT(CAST(message_body AS NVARCHAR(100)), 6),2) + '':''
									            + LEFT(RIGHT(CAST(message_body AS NVARCHAR(100)), 4),2)
									            AS DATETIME)), GETDATE()),0)
								            FROM '+@S+'.['+@Q+'] WITH(NOLOCK)
								            WHERE status = 1
								            PRINT @dt ';
                             EXEC (@sql2);
                         END;
                 END;            

            GO

            -- stpQueueInfoAutoRestart
            CREATE PROC [dbo].[stpQueueInfoAutoRestart]
	            (@db AS  VARCHAR(100), 
	             @msg VARCHAR(4000) OUTPUT)
                WITH ENCRYPTION AS
                 BEGIN 

		             SET NOCOUNT ON; 

                     DECLARE @s AS VARCHAR(1000),@q AS VARCHAR(1000), @OldestNotDelivered AS INT, @QuantityNotDelivered AS INT, @statusQueueActivated AS BIT, @NeedsRestart AS BIT, @schema VARCHAR(10);
                     DECLARE @tq TABLE([Schema] VARCHAR(100), [Name] VARCHAR(100));
         
		             INSERT INTO @tq
                     EXEC ('SELECT 
								t3.name AS [Schema],  
								t2.name AS [Name]   
							FROM '+@db+'.sys.services t1 
							INNER JOIN '+@db+'.sys.service_queues t2  ON ( t1.service_queue_id = t2.object_id )     
							INNER JOIN '+@db+'.sys.schemas t3 ON ( t2.schema_id = t3.schema_id )    
							LEFT OUTER JOIN '+@db+'.sys.dm_broker_queue_monitors t4   ON ( t2.object_id = t4.queue_id  AND t4.database_id = DB_ID() )    
							INNER JOIN sys.databases t5 ON ( t5.database_id = DB_ID() )
							where t2.is_ms_shipped = 0
							'
				            );   
                     IF EXISTS
                     (
                         SELECT [Schema],[name] FROM @tq
                     )
                         BEGIN

                             DECLARE cursor_queues CURSOR
                             FOR 
					            SELECT [Schema],[name]  FROM @tq;
                 
				             DECLARE @notificacao AS VARCHAR(4000)= ''; 
				 
                             OPEN cursor_queues;
                             FETCH NEXT FROM cursor_queues INTO @s,@q;
                             WHILE @@FETCH_STATUS = 0

                                 BEGIN
									 
                                     SELECT @notificacao = '';
                         
						             DECLARE @t AS TABLE
                                     (OldestNotDelivered   INT, 
                                      QuantityNotDelivered INT, 
                                      statusQueueActivated BIT, 
                                      NeedsRestart         BIT
                                     ); 
						  
                                     --PRINT @q  
                                     INSERT INTO @t
                                     EXEC dbo.stpQueueInfo 
                                          @db,
										  @s,
                                          @q, 
                                          1;
                                     SELECT @OldestNotDelivered = ISNULL(OldestNotDelivered, 0), 
                                            @QuantityNotDelivered = ISNULL(QuantityNotDelivered, 0), 
                                            @statusQueueActivated = ISNULL(statusQueueActivated, 0), 
                                            @NeedsRestart = ISNULL(NeedsRestart, 0)
                                     FROM @t;

                                     IF @NeedsRestart = 1
                                        AND (@OldestNotDelivered > 5
                                             OR @QuantityNotDelivered > 10)
                                         BEGIN 
								
								             -- NECESSARIO RESTART 
                                             EXEC ('BEGIN TRAN ALTER QUEUE '+@db+'.'+@s+'.['+@q+'] WITH STATUS = OFF; WAITFOR DELAY ''00:00:00.500''; COMMIT');
                                             EXEC ('ALTER QUEUE '+@db+'.'+@s+'.['+@q+'] WITH STATUS = ON;');
								             INSERT INTO [dbo].[HistoricoQueue] ([ServerName],[DatabaseName],[Queue],[Status],[OldestNotDelivered],[QuantityNotDelivered],[statusQueueActivated],[NeedsRestart],[DescriptionAction],[Data])
								             VALUES (@@SERVERNAME,UPPER(@db),@q,'RESTART',@OldestNotDelivered,@QuantityNotDelivered,@statusQueueActivated,@NeedsRestart,'BEGIN TRAN ALTER QUEUE '+@db+'.'+@s+'.['+@q+'] WITH STATUS = OFF; WAITFOR DELAY ''00:00:00.500''; COMMIT; ALTER QUEUE '+@db+'.'+@s+'.['+@q+'] WITH STATUS = ON;',GETDATE())
                                             SELECT @notificacao = @notificacao+'Queue: ['+@q+']: Foi reiniciada'+CHAR(13)+CHAR(10);

                                         END;
                                     IF @statusQueueActivated = 0 
                                         BEGIN

                                             -- DESATIVADA, NECESSARIO ATIVAR, NOTIFICAR  
                                             EXEC ('ALTER QUEUE '+@db+'.'+@s+'.['+@q+'] WITH STATUS = ON ;'); 
								             INSERT INTO [dbo].[HistoricoQueue] ([ServerName],[DatabaseName],[Queue],[Status],[OldestNotDelivered],[QuantityNotDelivered],[statusQueueActivated],[NeedsRestart],[DescriptionAction],[Data])
								             VALUES (@@SERVERNAME,UPPER(@db),@q,'RESTART',@OldestNotDelivered,@QuantityNotDelivered,@statusQueueActivated,@NeedsRestart,'ALTER QUEUE '+@db+'.'+@s+'.['+@q+'] WITH STATUS = ON ;',GETDATE())
                                             SELECT @notificacao = @notificacao+'Queue: ['+@q+']: Está desativada, ativando...'+CHAR(13)+CHAR(10);

                                         END;
                                     IF @OldestNotDelivered > 2
                                         BEGIN
							     
								             -- PARADA A MAIS DE 5 MIN 
								             INSERT INTO [dbo].[HistoricoQueue] ([ServerName],[DatabaseName],[Queue],[Status],[OldestNotDelivered],[QuantityNotDelivered],[statusQueueActivated],[NeedsRestart],[DescriptionAction],[Data])
								             VALUES (@@SERVERNAME,UPPER(@db),@q,'RESTART',@OldestNotDelivered,@QuantityNotDelivered,@statusQueueActivated,@NeedsRestart,'QUEUE: ['+@q+']: EXISTEM REGISTROS NÃO ENVIADOS HÁ MAIS DE 2 MINS',GETDATE()) 
                                             SELECT @notificacao = @notificacao+'Queue: ['+@q+']: Existem registros não enviados há mais de 2 min'+CHAR(13)+CHAR(10);

                                         END;
                                     IF @QuantityNotDelivered > 100000  
                                         BEGIN
							 
								             -- MAIS QUE 10 MSG PARA ENVIAR'  
							 	             INSERT INTO [dbo].[HistoricoQueue] ([ServerName],[DatabaseName],[Queue],[Status],[OldestNotDelivered],[QuantityNotDelivered],[statusQueueActivated],[NeedsRestart],[DescriptionAction],[Data])
								             VALUES (@@SERVERNAME,UPPER(@db),@q,'RESTART',@OldestNotDelivered,@QuantityNotDelivered,@statusQueueActivated,@NeedsRestart,'QUEUE: ['+@q+']: EXISTEM MAIS DE 10000 REGISTROS NÃO ENVIADOS',GETDATE())   
                                             SELECT @notificacao = @notificacao+'Queue: ['+@q+']: Existem mais de 100000 registros não enviados'+CHAR(13)+CHAR(10);

                                         END;
                                     IF @notificacao <> ''
                                         BEGIN
                                             SELECT @msg = @msg + @notificacao;
                                         END;
 
                                     FETCH NEXT FROM cursor_queues INTO @s,@q;
                                 END;
					            IF @msg <> ''
                                 BEGIN
                                         SELECT @msg = 'SERVER: '+ @@SERVERNAME +' DATABASE: ' + UPPER(@db) + CHAR(13)+CHAR(10) + @msg  
                                 END;

                             CLOSE cursor_queues;
                             DEALLOCATE cursor_queues;
                         END;
                     RETURN;   

                 END;

            GO

            -- stpQueueInfoSendMail
            CREATE PROC [dbo].[stpQueueInfoSendMail]
            WITH ENCRYPTION AS
                 BEGIN

                     /*
                        METODO DE USO
                        EXEC dbo.stpQueueInfoSendMail

                        Etapa 1 - Ao rodar a dbo.stpQueueInfoSendMail a mesma chama dbo.stpQueueInfoAutoRestart
                        Etapa 2 - A dbo.stpQueueInfoAutoRestart primeiro chama dbo.stpQueueInfo que retorna informações de filas 
                        Etapa 3 - O dbo.stpQueueInfoAutoRestart com base do retorno da dbo.stpQueueInfo verifica a necessidade de:
				                        Need Restart
				                        status Queue Activated
				                        Old dest Not Delivered
				                        Quantity Not Delivered
                        Etapa 4 - a dbo.stpQueueInfoSendMail recebe os retornos e começa a enviar emails em lote.
                     */

		             SET NOCOUNT ON;

		             -- Recupera os parametros base
                     DECLARE @Id_AlertaParametro INT = (SELECT Id_AlertaParametro FROM [dbo].AlertaParametro (NOLOCK) WHERE Nm_Alerta = 'Alerta Queue' AND Ativo = 1)
                     DECLARE @Ds_Caminho_Base VARCHAR(100) = (SELECT Ds_Caminho FROM [dbo].AlertaParametro (NOLOCK) WHERE Nm_Alerta = 'CheckList')
                     DECLARE @Telegram INT = (select Id_AlertaParametro from AlertaParametro WHERE Nm_Alerta = 'Envia Telegram')
                     DECLARE @Teams INT = (select Id_AlertaParametro from AlertaParametro WHERE Nm_Alerta = 'Envia Teams')
		
                     -- Recupera os parametros do Alerta
                     DECLARE @Subject VARCHAR(500), @Importance AS VARCHAR(6), @EmailBody VARCHAR(MAX), @EmptyBodyEmail VARCHAR(MAX), @AlertaBancoCorrompidoHeader VARCHAR(MAX), 
			             @AlertaBancoCorrompidoTable VARCHAR(MAX), @EmailDestination VARCHAR(200), @TextRel1 VARCHAR(4000), @TextRel2 VARCHAR(4000), @NomeRel VARCHAR(300), 
			             @MntMsg VARCHAR(200), @TLMsg VARCHAR(200), @SendMail VARCHAR(200), @ProfileDBMail VARCHAR(50), @BodyFormatMail VARCHAR(20), @CaminhoPath VARCHAR(50), 
			             @CaminhoFim VARCHAR(50), @Ass VARCHAR(4000), @HTML VARCHAR(MAX), @Query VARCHAR(MAX), @msg VARCHAR(1000)= '', @recipients VARCHAR(4000), 
			             @Ds_Email_Assunto_alerta VARCHAR (600), @Ds_Email_Assunto_solucao VARCHAR (600), @Ds_Email_Texto_alerta VARCHAR (600), @Ds_Email_Texto_solucao VARCHAR (600), 
			             @Ds_Menssageiro_01 VARCHAR (30), @Ds_Menssageiro_02 VARCHAR (30), @Ds_Menssageiro_03 VARCHAR (30)

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
		             --	BLOCO DE ANÁLISE
		             *******************************************************************************************************************************/
 					 IF(OBJECT_ID('tempdb..#ResultsQueue') IS NOT NULL)
						DROP TABLE #CResultsQueue;
					 CREATE TABLE #ResultsQueue
						([c1] VARCHAR(1000));
 
					 --IF EXISTS
			   --          (SELECT *
				  --           FROM tempdb.sys.objects
				  --           WHERE name = 'ResultsQueue'
			   --          )
      --                   DROP TABLE tempdb.dbo.ResultsQueue;

      --               CREATE TABLE tempdb.dbo.ResultsQueue(
			   --         c1 VARCHAR(8000));

		             DECLARE @DBName VARCHAR(100);
		             DECLARE Qe CURSOR FOR 				 
				            SELECT name 
				            FROM sys.databases 
				            WHERE name NOT IN ('master','model','msdb','tempdb')
				            AND state_desc = 'ONLINE'
				            AND is_broker_enabled = 1 
						
		             OPEN Qe;
		             FETCH NEXT FROM Qe INTO @DBName;
		             WHILE @@FETCH_STATUS = 0
				            BEGIN
						
					            DECLARE @out AS VARCHAR(6000) = ''
					            EXEC [dbo].[stpQueueInfoAutoRestart] @DBName, @out OUTPUT;
					
					            IF @out <> '' 
						            BEGIN
							            INSERT INTO #ResultsQueue(C1) SELECT @out
							
							            SELECT @msg = ISNULL(STUFF(
							             (
								             SELECT c1
								             FROM #ResultsQueue FOR XML PATH(''), TYPE
							             ).value('.', 'NVARCHAR(MAX)'), 1, 0, ''), '');

							            IF(OBJECT_ID('tempdb..##CheckQueue') IS NOT NULL)
								            DROP TABLE ##CheckQueue;
							            CREATE TABLE ##CheckQueue
								            ([QueueInfo] VARCHAR(200));
							            INSERT INTO ##CheckQueue
								            SELECT @msg

 							            /*******************************************************************************************************************************
							            --	CRIA O EMAIL - ALERTA
							            *******************************************************************************************************************************/			
							            SELECT @subject = @Ds_Email_Assunto_alerta + ' ' +@@SERVERNAME
							            SET @TextRel1 = @Ds_Email_Texto_alerta +' '+(SELECT TOP 1 SUBSTRING(QueueInfo, CHARINDEX('Queue: ', QueueInfo) + 6, LEN(QueueInfo)) FROM ##CheckQueue)+'</b> verifique essa informação, listagem completa abaixo.'	
							            SET @CaminhoFim = @CaminhoPath + @NomeRel +'.html'

							            -- Gera Primeiro bloco de HTML
							            SET @Query = 'Select Case When [QueueInfo] <> NULL THEN '''' ELSE ''<p style=""color: red; "">''+[QueueInfo]+''</p>'' END [QueueInfo] FROM ##CheckQueue'

                                        SET @HTML = [dbo].[fncExportaMultiHTML](@Query, @TextRel1, 2, 1)
                                        -- Gera Segundo bloco de HTML
                                        -- SET @Ass = (SELECT Assinatura FROM MailAssinatura WHERE Id = 1)
							            select @HTML = @HTML + @Ass
                                        -- Salva Arquivo HTML de Envio
                                        EXEC[dbo].[stpWriteFile]
                                                @Ds_Texto = @HTML, -- nvarchar(max)
                                                @Ds_Caminho = @CaminhoFim, -- nvarchar(max)
                                                @Ds_Codificacao = N'UTF-8', -- nvarchar(max)
                                                @Ds_Formato_Quebra_Linha = N'windows', -- nvarchar(max)
                                                @Fl_Append = 0 -- bit

                                         IF @msg<> '' 
								             BEGIN

                                                IF EXISTS(SELECT B.Ativo from AlertaParametro A
                                                            INNER JOIN [dbo].[AlertaEnvio] B ON B.IdAlertaParametro = A.Id_AlertaParametro
                                                            WHERE B.Ativo = 1
                                                            AND B.Des LIKE '%Email'
                                                            AND[Id_AlertaParametro] = @Id_AlertaParametro
                                                            )

                                                BEGIN

                                                        EXEC[msdb].[dbo].[sp_send_dbmail]
                                                            @profile_name = @ProfileDBMail,
												            @recipients = @EmailDestination,
												            @body_format = @BodyFormatMail,
												            @subject = @Subject,
												            @importance = @Importance,
												            @body = @HTML;

									            END
 
									            -- Parametro Menssageiro
                                                SET @MntMsg = @Subject + ', Verifique os detalhes no *E-Mail*'

                                                IF EXISTS(SELECT B.Ativo from AlertaParametro A
                                                            INNER JOIN [dbo].[AlertaEnvio] B ON B.IdAlertaParametro = A.Id_AlertaParametro
                                                            WHERE B.Ativo = 1
                                                            AND B.Des LIKE '%Telegram'
                                                            AND[Id_AlertaParametro] = @Id_AlertaParametro
                                                            )

                                                BEGIN
										            -- Envio do Telegram
                                                    EXEC dbo.StpSendMsgTelegram
                                                            @Destino = @CanalTelegram,
                                                            @Msg = @MntMsg

                                                END

                                                IF EXISTS(SELECT B.Ativo from AlertaParametro A
                                                            INNER JOIN [dbo].[AlertaEnvio] B ON B.IdAlertaParametro = A.Id_AlertaParametro
                                                            WHERE B.Ativo = 1
                                                            AND B.Des LIKE '%Teams'
                                                            AND[Id_AlertaParametro] = @Id_AlertaParametro
                                                            )

                                                BEGIN
										            -- MS TEAMS
                                                    SET @MntMsg = (select replace(@MntMsg, '\', '-'))
                                                    EXEC[dbo].[stpSendMsgTeams]
                                                        @msg = @MntMsg,
                                                        @channel = @Ds_Menssageiro_02,
                                                        @ap = @Teams

                                                END

                                             END;

                            END

                            FETCH NEXT FROM Qe INTO @DBName;
		            END;
		            CLOSE Qe;
                    DEALLOCATE Qe;

                 END;

            GO

            -- stpRunDefraging
            CREATE PROCEDURE [dbo].[stpRunDefraging] 
            /* Declare Parameters */
                @minFragmentation       FLOAT               = 10.0  
                    /* in percent, will not defrag if fragmentation less than specified */
              , @rebuildThreshold       FLOAT               = 30.0  
                    /* in percent, greater than @rebuildThreshold will result in rebuild instead of reorg */
              , @executeSQL             BIT                 = 1     
                    /* 1 = execute; 0 = print command only */
              , @defragOrderColumn      NVARCHAR(20)        = 'range_scan_count'
                    /* Valid options are: range_scan_count, fragmentation, page_count */
              , @defragSortOrder        NVARCHAR(4)         = 'DESC'
                    /* Valid options are: ASC, DESC */
              , @timeLimit              INT                 = 480 /* defaulted to 8 hours */
                    /* Optional time limitation; expressed in minutes */
              , @database               VARCHAR(128)        = NULL
                    /* Option to specify one or more database names, separated by commas; NULL will return all */
              , @tableName              VARCHAR(4000)       = NULL  -- databaseName.schema.tableName  
                    /* Option to specify a table name; null will return all */
              , @forceRescan            BIT                 = 0
                    /* Whether or not to force a rescan of indexes; 1 = force, 0 = use existing scan, if available */
              , @scanMode               VARCHAR(10)         = N'LIMITED'
                    /* Options are LIMITED, SAMPLED, and DETAILED */
              , @minPageCount           INT                 = 8 
                    /*  MS recommends > 1 extent (8 pages) */
              , @maxPageCount           INT                 = NULL
                    /* NULL = no limit */
              , @excludeMaxPartition    BIT                 = 0
                    /* 1 = exclude right-most populated partition; 0 = do not exclude; see notes for caveats */
              , @onlineRebuild          BIT                 = 1     
                    /* 1 = online rebuild; 0 = offline rebuild; only in Enterprise */
              , @sortInTempDB           BIT                 = 1
                    /* 1 = perform sort operation in TempDB; 0 = perform sort operation in the index's database */
              , @maxDopRestriction      TINYINT             = NULL
                    /* Option to restrict the number of processors for the operation; only in Enterprise */
              , @printCommands          BIT                 = 0     
                    /* 1 = print commands; 0 = do not print commands */
              , @printFragmentation     BIT                 = 0
                    /* 1 = print fragmentation prior to defrag; 
                       0 = do not print */
              , @defragDelay            CHAR(8)             = '00:00:05'
                    /* time to wait between defrag commands */
              , @debugMode              BIT                 = 0
                    /* display some useful comments to help determine if/WHERE issues occur */
            WITH ENCRYPTION
            AS /*********************************************************************************
                Name:       dba_indexDefrag_sp

                Author:     Michelle Ufford, http://sqlfool.com

                Purpose:    Defrags one or more indexes for one or more databases

                Notes:

                CAUTION: TRANSACTION LOG SIZE SHOULD BE MONITORED CLOSELY WHEN DEFRAGMENTING.
                         DO NOT RUN UNATTENDED ON LARGE DATABASES DURING BUSINESS HOURS.

                  @minFragmentation     defaulted to 10%, will not defrag if fragmentation 
                                        is less than that
      
                  @rebuildThreshold     defaulted to 30% AS recommended by Microsoft in BOL;
                                        greater than 30% will result in rebuild instead

                  @executeSQL           1 = execute the SQL generated by this proc; 
                                        0 = print command only

                  @defragOrderColumn    Defines how to prioritize the order of defrags.  Only
                                        used if @executeSQL = 1.  
                                        Valid options are: 
                                        range_scan_count = count of range and table scans on the
                                                           index; in general, this is what benefits 
                                                           the most FROM defragmentation
                                        fragmentation    = amount of fragmentation in the index;
                                                           the higher the number, the worse it is
                                        page_count       = number of pages in the index; affects
                                                           how long it takes to defrag an index

                  @defragSortOrder      The sort order of the ORDER BY clause.
                                        Valid options are ASC (ascending) or DESC (descending).

                  @timeLimit            Optional, limits how much time can be spent performing 
                                        index defrags; expressed in minutes.

                                        NOTE: The time limit is checked BEFORE an index defrag
                                              is begun, thus a long index defrag can exceed the
                                              time limitation.

                  @database             Optional, specify specific database name to defrag;
                                        If not specified, all non-system databases will
                                        be defragged.

                  @tableName            Specify if you only want to defrag indexes for a 
                                        specific table, format = databaseName.schema.tableName;
                                        if not specified, all tables will be defragged.

                  @forceRescan          Whether or not to force a rescan of indexes.  If set
                                        to 0, a rescan will not occur until all indexes have
                                        been defragged.  This can span multiple executions.
                                        1 = force a rescan
                                        0 = use previous scan, if there are indexes left to defrag

                  @scanMode             Specifies which scan mode to use to determine
                                        fragmentation levels.  Options are:
                                        LIMITED - scans the parent level; quickest mode,
                                                  recommended for most cases.
                                        SAMPLED - samples 1% of all data pages; if less than
                                                  10k pages, performs a DETAILED scan.
                                        DETAILED - scans all data pages.  Use great care with
                                                   this mode, AS it can cause performance issues.

                  @minPageCount         Specifies how many pages must exist in an index in order 
                                        to be considered for a defrag.  Defaulted to 8 pages, AS 
                                        Microsoft recommends only defragging indexes with more 
                                        than 1 extent (8 pages).  

                                        NOTE: The @minPageCount will restrict the indexes that
                                        are stored in HisIndexDefragStatus table.

                  @maxPageCount         Specifies the maximum number of pages that can exist in 
                                        an index and still be considered for a defrag.  Useful
                                        for scheduling small indexes during business hours and
                                        large indexes for non-business hours.

                                        NOTE: The @maxPageCount will restrict the indexes that
                                        are defragged during the current operation; it will not
                                        prevent indexes FROM being stored in the 
                                        HisIndexDefragStatus table.  This way, a single scan
                                        can support multiple page count thresholds.

                  @excludeMaxPartition  If an index is partitioned, this option specifies whether
                                        to exclude the right-most populated partition.  Typically,
                                        this is the partition that is currently being written to in
                                        a sliding-window scenario.  Enabling this feature may reduce
                                        contention.  This may not be applicable in other types of 
                                        partitioning scenarios.  Non-partitioned indexes are 
                                        unaffected by this option.
                                        1 = exclude right-most populated partition
                                        0 = do not exclude

                  @onlineRebuild        1 = online rebuild; 
                                        0 = offline rebuild

                  @sortInTempDB         Specifies whether to defrag the index in TEMPDB or in the
                                        database the index belongs to.  Enabling this option may
                                        result in faster defrags and prevent database file size 
                                        inflation.
                                        1 = perform sort operation in TempDB
                                        0 = perform sort operation in the index's database 

                  @maxDopRestriction    Option to specify a processor limit for index rebuilds

                  @printCommands        1 = print commands to screen; 
                                        0 = do not print commands

                  @printFragmentation   1 = print fragmentation to screen;
                                        0 = do not print fragmentation

                  @defragDelay          Time to wait between defrag commands; gives the
                                        server a little time to catch up 

                  @debugMode            1 = display debug comments; helps with troubleshooting
                                        0 = do not display debug comments

                Called by:  SQL Agent Job or DBA

                ----------------------------------------------------------------------------
                DISCLAIMER: 
                This code and information are provided \AS IS\ without warranty of any kind,
                either expressed or implied, including but not limited to the implied
                warranties or merchantability and/ or fitness for a particular purpose.
 
                 ----------------------------------------------------------------------------
 
                 LICENSE: 

                 This index defrag script is free to download and use for personal, educational,
                 and internal corporate purposes, provided that this header is preserved.
                 Redistribution or sale of this index defrag script, in whole or in part, is 

                 prohibited without the author's express written consent.
                ----------------------------------------------------------------------------

                 Date Initials    Version Description
                ----------------------------------------------------------------------------
                2007-12-18  MFU         1.0     Initial Release
                2008-10-17  MFU         1.1     Added @defragDelay, CIX_temp_indexDefragList
                 2008-11-17  MFU         1.2     Added page_count to log table
 
                                                 , added @printFragmentation option
                 2009-03-17  MFU         2.0     Provided support for centralized execution
                                                 , consolidated Enterprise & Standard versions
                                                 , added @debugMode, @maxDopRestriction
 
                                                 , modified LOB and partition logic
                 2009-06-18  MFU         3.0     Fixed bug in LOB logic, added @scanMode option
 
                                                 , added support for stat rebuilds (@rebuildStats)
 
                                                 , support model and msdb defrag
 
                                                 , added columns to the HistIndexDefragLog table
                                                 , modified logging to show \in progress\ defrags
                                                , added defrag exclusion list (scheduling)
                 2009-08-28  MFU         3.1     Fixed read_only bug for database lists
                2010-04-20  MFU         4.0     Added time limit option
                                                 , added static table with rescan logic
                                                 , added parameters for page count & SORT_IN_TEMPDB
                                                , added try/catch logic and additional debug options
                                                , added options for defrag prioritization
                                                , fixed bug for indexes with allow_page_lock = off
                                                , added option to exclude right - most partition
                                                , removed @rebuildStats option
                                                , refer to http://sqlfool.com for full release notes
                2011 - 04 - 28  MFU         4.1     Bug fixes for databases requiring[]
                                                , cleaned up the create table section
                                                , updated syntax for case-sensitive databases
                                                , comma - delimited list for @database now supported
            * ********************************************************************************
                Example of how to call this script:

                    EXECUTE [dbo].[stpRunDefraging]
                          @executeSQL = 1
                        , @printCommands = 1
                        , @debugMode = 1
                        , @printFragmentation = 1
                        , @forceRescan = 1
                        , @maxDopRestriction = 1
                        , @minPageCount = 8
                        , @maxPageCount = NULL
                        , @minFragmentation = 1
                        , @rebuildThreshold = 30
                        , @defragDelay = '00:00:05'
                        , @defragOrderColumn = 'page_count'
                        , @defragSortOrder = 'DESC'
                        , @excludeMaxPartition = 1
                        , @timeLimit = NULL
                        , @database = 'sandbox,sandbox_caseSensitive';
            *********************************************************************************/																
            SET NOCOUNT ON;
            SET XACT_ABORT ON;
            SET QUOTED_IDENTIFIER ON;

            BEGIN

                BEGIN TRY

                    /* Just a little validation... */
                    IF @minFragmentation IS NULL
                        OR @minFragmentation NOT BETWEEN 0.00 AND 100.0
                            SET @minFragmentation = 10.0;

                    IF @rebuildThreshold IS NULL
                        OR @rebuildThreshold NOT BETWEEN 0.00 AND 100.0
                            SET @rebuildThreshold = 30.0;

                    IF @defragDelay NOT LIKE '00:[0-5][0-9]:[0-5][0-9]'
                        SET @defragDelay = '00:00:05';

                    IF @defragOrderColumn IS NULL
                        OR @defragOrderColumn NOT IN('range_scan_count', 'fragmentation', 'page_count')
                            SET @defragOrderColumn = 'range_scan_count';

                    IF @defragSortOrder IS NULL
                        OR @defragSortOrder NOT IN('ASC', 'DESC')
                            SET @defragSortOrder = 'DESC';

                    IF @scanMode NOT IN('LIMITED', 'SAMPLED', 'DETAILED')
                        SET @scanMode = 'LIMITED';

                    IF @debugMode IS NULL
                        SET @debugMode = 0;

                    IF @forceRescan IS NULL
                        SET @forceRescan = 0;

                    IF @sortInTempDB IS NULL
                        SET @sortInTempDB = 1;

                    IF @database = ''

                        SET @database = NULL


                    IF @tableName = ''

                        SET @tableName = NULL


                    IF @maxPageCount = 0

                        SET @maxPageCount = NULL


                    IF @debugMode = 1 RAISERROR('Undusting the cogs AND starting up...', 0, 42) WITH NOWAIT;

                    /* Declare our variables */
                    DECLARE @objectID                 INT
                            , @databaseID INT
                            , @databaseName             NVARCHAR(128)
                            , @indexID INT
                            , @partitionCount           BIGINT
                            , @schemaName NVARCHAR(128)
                            , @objectName NVARCHAR(128)
                            , @indexName NVARCHAR(128)
                            , @partitionNumber SMALLINT
                            , @fragmentation            FLOAT
                            , @pageCount INT
                            , @sqlCommand               NVARCHAR(4000)
                            , @rebuildCommand NVARCHAR(200)
                            , @datetimestart DATETIME
                            , @dateTimeEnd              DATETIME
                            , @containsLOB BIT
                            , @editionCheck             BIT
                            , @debugMessage NVARCHAR(4000)
                            , @updateSQL NVARCHAR(4000)
                            , @partitionSQL NVARCHAR(4000)
                            , @partitionSQL_Param NVARCHAR(1000)
                            , @LOB_SQL NVARCHAR(4000)
                            , @LOB_SQL_Param NVARCHAR(1000)
                            , @indexDefrag_id INT
                            , @startdatetime            DATETIME
                            , @enddatetime DATETIME
                            , @getIndexSQL              NVARCHAR(4000)
                            , @getIndexSQL_Param NVARCHAR(4000)
                            , @allowPageLockSQL NVARCHAR(4000)
                            , @allowPageLockSQL_Param NVARCHAR(4000)
                            , @allowPageLocks INT
                            , @excludeMaxPartitionSQL   NVARCHAR(4000)
				            , @IndexFillFactor INT;

                    /* Initialize our variables */
                    SELECT @startdatetime = GETDATE()
                        , @enddatetime = DATEADD(minute, @timeLimit, GETDATE());

                    /* Create our temporary tables */
                    CREATE TABLE #databaseList
                    (
                          databaseID INT
                        , databaseName VARCHAR(128)
                        , scanStatus BIT
                    );

                    CREATE TABLE #processor 
                    (
                          [index]           INT
                        , Name VARCHAR(128)
                        , Internal_Value INT
                        , Character_Value   INT
                    );

                    CREATE TABLE #maxPartitionList
                    (
                          databaseID INT
                        , objectID INT
                        , indexID INT
                        , maxPartition INT
                    );

		            --Created to get FillFactor of Reindexed indexes
                    CREATE TABLE #ServerIndexes 
		            (
                        object_id           int,
                        name varchar(8000), 
			            type_desc varchar(8000), 
			            fill_factor int
		            )

		            EXECUTE sp_MSforeachdb 'use [?]; insert #ServerIndexes select object_id, name, type_desc, fill_factor from sys.indexes where name is not null'



                    IF @debugMode = 1 RAISERROR('Beginning validation...', 0, 42) WITH NOWAIT;

                    /* Make sure we're not exceeding the number of processors we have available */
                    INSERT INTO #processor
                    EXECUTE xp_msver 'ProcessorCount';

                    IF @maxDopRestriction IS NOT NULL AND @maxDopRestriction > (SELECT Internal_Value FROM #processor)
                        SELECT @maxDopRestriction = Internal_Value
                        FROM #processor;

                    /* Check our server version; 1804890536 = Enterprise, 610778273 = Enterprise Evaluation, -2117995310 = Developer 
									             1872460670 = Enterprise Core-bases														*/
                    IF(SELECT ServerProperty('EditionID')) IN(1804890536, 610778273, -2117995310, 1872460670)
                        SET @editionCheck = 1-- supports online rebuilds
                   ELSE
                        SET @editionCheck = 0; -- does not support online rebuilds

                    /* Output the parameters we're working with */
                    IF @debugMode = 1 
                    BEGIN

                        SELECT @debugMessage = 'Your SELECTed parameters are... 
                        Defrag indexes WITH fragmentation greater than ' + CAST(@minFragmentation AS VARCHAR(10)) + ';
                    REBUILD indexes WITH fragmentation greater than ' + CAST(@rebuildThreshold AS VARCHAR(10)) + ';
                    You' + CASE WHEN @executeSQL = 1 THEN ' DO' ELSE ' DO NOT' END + ' want the commands to be executed automatically; 
                        You want to defrag indexes in ' + @defragSortOrder + ' order of the ' + UPPER(@defragOrderColumn) + ' value;
                        You have' + CASE WHEN @timeLimit IS NULL THEN ' NOT specified a time limit;' ELSE ' specified a time limit of ' 
                            + CAST(@timeLimit AS VARCHAR(10)) END + ' minutes;
                        ' + CASE WHEN @database IS NULL THEN 'ALL databases' ELSE 'The ' + @database + ' database(s)' END + ' will be defragged;
                        ' + CASE WHEN @tableName IS NULL THEN 'ALL tables' ELSE 'The ' + @tableName + ' TABLE' END + ' will be defragged;
                        We' + CASE WHEN EXISTS(SELECT Top 1 * FROM dbo.HisIndexDefragStatus WHERE defragDate IS NULL)
                            AND @forceRescan<> 1 THEN ' WILL NOT' ELSE ' WILL' END + ' be rescanning indexes;
                        The scan will be performed in ' + @scanMode + ' mode;
                    You want to limit defrags to indexes with' + CASE WHEN @maxPageCount IS NULL THEN ' more than ' 
                            + CAST(@minPageCount AS VARCHAR(10)) ELSE
                            ' BETWEEN ' + CAST(@minPageCount AS VARCHAR(10))
                            + ' AND ' + CAST(@maxPageCount AS VARCHAR(10)) END + ' pages;
                        Indexes will be defragged' + CASE WHEN @editionCheck = 0 OR @onlineRebuild = 0 THEN ' OFFLINE;' ELSE ' ONLINE;' END + '
                        Indexes will be sorted in' + CASE WHEN @sortInTempDB = 0 THEN ' the DATABASE' ELSE ' TEMPDB;' END + '
                        Defrag operations will utilize ' + CASE WHEN @editionCheck = 0 OR @maxDopRestriction IS NULL 
                            THEN 'system defaults for processors;' 
                            ELSE CAST(@maxDopRestriction AS VARCHAR(2)) + ' processors;' END + '
                        You' + CASE WHEN @printCommands = 1 THEN ' DO' ELSE ' DO NOT' END + ' want to PRINT the ALTER INDEX commands; 
                        You' + CASE WHEN @printFragmentation = 1 THEN ' DO' ELSE ' DO NOT' END + ' want to OUTPUT fragmentation levels; 
                        You want to wait ' + @defragDelay + ' (hh:mm:ss) BETWEEN defragging indexes;
                        You want to run in' + CASE WHEN @debugMode = 1 THEN ' DEBUG' ELSE ' SILENT' END + ' mode.';

                        RAISERROR(@debugMessage, 0, 42) WITH NOWAIT;

                    END;

                    IF @debugMode = 1 RAISERROR('Grabbing a list of our databases...', 0, 42) WITH NOWAIT;

                    /* Retrieve the list of databases to investigate */
                    /* If @database is NULL, it means we want to defrag *all* databases */
                    IF @database IS NULL
                    BEGIN

                        INSERT INTO #databaseList
                        SELECT database_id
                            , name
                            , 0 -- not scanned yet for fragmentation
                        FROM sys.databases
                        WHERE[name] NOT IN('master', 'tempdb')-- exclude system databases
                          AND[state] = 0-- state must be ONLINE
                            AND is_read_only = 0;  -- cannot be read_only

                    END;
                    ELSE
                    /* Otherwise, we're going to just defrag our list of databases */
                    BEGIN

                        INSERT INTO #databaseList
                        SELECT database_id
                            , name
                            , 0 -- not scanned yet for fragmentation
                        FROM sys.databases AS d
                        JOIN dbo.fnParseStringUdf(@database, ',') AS x
                            ON d.name COLLATE Latin1_General_CI_AI = x.stringValue
                        WHERE[name] NOT IN ('master', 'tempdb')-- exclude system databases
                           AND[state] = 0-- state must be ONLINE
                            AND is_read_only = 0;  -- cannot be read_only

                    END;

                    /* Check to see IF we have indexes in need of defrag; otherwise, re-scan the database(s) */
                    IF NOT EXISTS(SELECT Top 1 * FROM dbo.HisIndexDefragStatus WHERE defragDate IS NULL)
                        OR @forceRescan = 1
                    BEGIN

                        /* Truncate our list of indexes to prepare for a new scan */
                        TRUNCATE TABLE dbo.HisIndexDefragStatus;

                    IF @debugMode = 1 RAISERROR('Looping through our list of databases and checking for fragmentation...', 0, 42) WITH NOWAIT;

                    /* Loop through our list of databases */
                    WHILE(SELECT COUNT(*) FROM #databaseList WHERE scanStatus = 0) > 0
                        BEGIN

                        SELECT Top 1 @databaseID = databaseID

                        FROM #databaseList
                            WHERE scanStatus = 0;

                    SELECT @debugMessage = '  working on ' + DB_NAME(@databaseID) + '...';

                    IF @debugMode = 1
                                RAISERROR(@debugMessage, 0, 42) WITH NOWAIT;

                    /* Determine which indexes to defrag using our user-defined parameters */
                    INSERT INTO dbo.HisIndexDefragStatus
                            (
                                  databaseID
                                , databaseName
                                , objectID
                                , indexID
                                , partitionNumber
                                , fragmentation
                                , page_count
                                , range_scan_count
                                , scanDate
                            )
                            SELECT
                                  ps.database_id AS 'databaseID'
                                , QUOTENAME(DB_NAME(ps.database_id)) AS 'databaseName'
                                , ps.[object_id] AS 'objectID'
                                , ps.index_id AS 'indexID'
                                , ps.partition_number AS 'partitionNumber'
                                , SUM(ps.avg_fragmentation_in_percent) AS 'fragmentation'
                                , SUM(ps.page_count) AS 'page_count'
                                , os.range_scan_count
                                , GETDATE() AS 'scanDate'
                            FROM sys.dm_db_index_physical_stats(@databaseID, OBJECT_ID(@tableName), NULL , NULL, @scanMode) AS ps
                            JOIN sys.dm_db_index_operational_stats(@databaseID, OBJECT_ID(@tableName), NULL , NULL) AS os
                                ON ps.database_id = os.database_id
                                AND ps.[object_id] = os.[object_id]
                                AND ps.index_id = os.index_id
                                AND ps.partition_number = os.partition_number
                            WHERE avg_fragmentation_in_percent >= @minFragmentation
                                AND ps.index_id > 0 -- ignore heaps
                                AND ps.page_count > @minPageCount
                                AND ps.index_level = 0-- leaf-level nodes only, supports @scanMode
                                AND ps.[OBJECT_ID] not in (SELECT ObjectID FROM [dbo].[HistIndexDefragTablesToExclude])
                            GROUP BY ps.database_id 
                                , QUOTENAME(DB_NAME(ps.database_id))
                                , ps.[object_id]
                                , ps.index_id 
                                , ps.partition_number 
                                , os.range_scan_count
                            OPTION(MAXDOP 2);

                    /* Do we want to exclude right-most populated partition of our partitioned indexes? */
                    IF @excludeMaxPartition = 1
                            BEGIN

                                SET @excludeMaxPartitionSQL = '
                                    SELECT ' + CAST(@databaseID AS VARCHAR(10)) + ' AS[databaseID]
                                        , [object_id]
                                        , index_id
                                        , MAX(partition_number) AS[maxPartition]
                                    FROM[' + DB_NAME(@databaseID) + '].sys.partitions
                                   WHERE partition_number > 1
                                        AND[rows] > 0
                                    GROUP BY object_id
                                        , index_id;';

                                INSERT INTO #maxPartitionList
                                EXECUTE sp_executesql @excludeMaxPartitionSQL;

                            END;
                
                            /* Keep track of which databases have already been scanned */
                            UPDATE #databaseList
                            SET scanStatus = 1
                            WHERE databaseID = @databaseID;

                    END

                    /* We don't want to defrag the right-most populated partition, so
                       delete any records for partitioned indexes where partition = MAX(partition) */
                    IF @excludeMaxPartition = 1
                        BEGIN

                            DELETE ids
                            FROM dbo.HisIndexDefragStatus AS ids
                            JOIN #maxPartitionList AS mpl
                                ON ids.databaseID = mpl.databaseID
                                AND ids.objectID = mpl.objectID
                                AND ids.indexID = mpl.indexID
                                AND ids.partitionNumber = mpl.maxPartition;

                    END;

                        /* Update our exclusion mask for any index that has a restriction ON the days it can be defragged */
                        UPDATE ids
                        SET ids.exclusionMask = ide.exclusionMask
                        FROM dbo.HisIndexDefragStatus AS ids
                        JOIN dbo.HistIndexDefragExclusion AS ide
                            ON ids.databaseID = ide.databaseID
                            AND ids.objectID = ide.objectID
                            AND ids.indexID = ide.indexID;

                    END

                    SELECT @debugMessage = 'Looping through our list... there are ' + CAST(COUNT(*) AS VARCHAR(10)) + ' indexes to defrag!'
                    FROM dbo.HisIndexDefragStatus
                    WHERE defragDate IS NULL
                        AND page_count BETWEEN @minPageCount AND ISNULL(@maxPageCount, page_count);

                    IF @debugMode = 1 RAISERROR(@debugMessage, 0, 42) WITH NOWAIT;

                    /* Begin our loop for defragging */
                    WHILE(SELECT COUNT(*)
                           FROM dbo.HisIndexDefragStatus
                           WHERE (
                                       (@executeSQL = 1 AND defragDate IS NULL)
                                    OR(@executeSQL = 0 AND defragDate IS NULL AND printStatus = 0)
                                 )
                            AND exclusionMask & POWER(2, DATEPART(weekday, GETDATE())-1) = 0
                            AND page_count BETWEEN @minPageCount AND ISNULL(@maxPageCount, page_count)) > 0
                    BEGIN

                        /* Check to see IF we need to exit our loop because of our time limit */
                        IF ISNULL(@enddatetime, GETDATE()) < GETDATE()
                        BEGIN
                            RAISERROR('Our time limit has been exceeded!', 11, 42) WITH NOWAIT;
                    END;

                        IF @debugMode = 1 RAISERROR('  Picking an index to beat into shape...', 0, 42) WITH NOWAIT;

                    /* Grab the index with the highest priority, based on the values submitted; 
                       Look at the exclusion mask to ensure it can be defragged today */
                    SET @getIndexSQL = N'
                        SELECT TOP 1 
                              @objectID_Out         = objectID
                            , @indexID_Out          = indexID
                            , @databaseID_Out       = databaseID
                            , @databaseName_Out     = databaseName
                            , @fragmentation_Out    = fragmentation
                            , @partitionNumber_Out  = partitionNumber
                            , @pageCount_Out        = page_count
                        FROM dbo.HisIndexDefragStatus
                        WHERE defragDate IS NULL ' 
                            + CASE WHEN @executeSQL = 0 THEN 'AND printStatus = 0' ELSE '' END + '
                            AND exclusionMask & Power(2, DatePart(weekday, GETDATE())-1) = 0
                            AND page_count BETWEEN @p_minPageCount AND ISNULL(@p_maxPageCount, page_count)
                        ORDER BY + ' + @defragOrderColumn + ' ' + @defragSortOrder;


                        SET @getIndexSQL_Param = N'@objectID_Out        INT OUTPUT
                                                 , @indexID_Out         INT OUTPUT
                                                 , @databaseID_Out      INT OUTPUT
                                                 , @databaseName_Out    NVARCHAR(128) OUTPUT
                                                 , @fragmentation_Out INT OUTPUT
                                                 , @partitionNumber_Out INT OUTPUT
                                                 , @pageCount_Out INT OUTPUT
                                                 , @p_minPageCount INT
                                                 , @p_maxPageCount      INT';

                        EXECUTE sp_executesql @getIndexSQL
                            , @getIndexSQL_Param
                            , @p_minPageCount       = @minPageCount
                            , @p_maxPageCount       = @maxPageCount
                            , @objectID_Out         = @objectID OUTPUT
                            , @indexID_Out = @indexID          OUTPUT
                            , @databaseID_Out       = @databaseID OUTPUT
                            , @databaseName_Out = @databaseName     OUTPUT
                            , @fragmentation_Out    = @fragmentation OUTPUT
                            , @partitionNumber_Out = @partitionNumber  OUTPUT
                            , @pageCount_Out        = @pageCount OUTPUT;

                    IF @debugMode = 1 RAISERROR('  Looking up the specifics for our index...', 0, 42) WITH NOWAIT;

                    /* Look up index information */
                    SELECT @updateSQL = N'UPDATE ids
                            SET schemaName = QUOTENAME(s.name)
                                , objectName = QUOTENAME(o.name)
                                , indexName = QUOTENAME(i.name)
                            FROM dbo.HisIndexDefragStatus AS ids
                            INNER JOIN ' + @databaseName + '.sys.objects AS o
                                ON ids.objectID = o.[object_id]
                            INNER JOIN ' + @databaseName + '.sys.indexes AS i
                                ON o.[object_id] = i.[object_id]
                                AND ids.indexID = i.index_id
                            INNER JOIN ' + @databaseName + '.sys.schemas AS s
                                ON o.schema_id = s.schema_id
                            WHERE o.[object_id] = ' + CAST(@objectID AS VARCHAR(10)) + '
                                AND i.index_id = ' + CAST(@indexID AS VARCHAR(10)) + '
                                AND i.type > 0
                                AND ids.databaseID = ' + CAST(@databaseID AS VARCHAR(10));

                        EXECUTE sp_executesql @updateSQL;

                    /* Grab our object names */
                    SELECT @objectName = objectName
                        , @schemaName = schemaName
                        , @indexName = indexName
                        FROM dbo.HisIndexDefragStatus
                        WHERE objectID = @objectID
                            AND indexID = @indexID
                            AND databaseID = @databaseID;

                    IF @debugMode = 1 RAISERROR('  Grabbing the partition COUNT...', 0, 42) WITH NOWAIT;

                    /* Determine if the index is partitioned */
                    SELECT @partitionSQL = 'SELECT @partitionCount_OUT = COUNT(*)
                                                    FROM ' + @databaseName + '.sys.partitions
                                                    WHERE object_id = ' + CAST(@objectID AS VARCHAR(10)) + '
                                                        AND index_id = ' + CAST(@indexID AS VARCHAR(10)) + ';'
                            , @partitionSQL_Param = '@partitionCount_OUT INT OUTPUT';

                        EXECUTE sp_executesql @partitionSQL, @partitionSQL_Param, @partitionCount_OUT = @partitionCount OUTPUT;

                    IF @debugMode = 1 RAISERROR('  Seeing IF there are any LOBs to be handled...', 0, 42) WITH NOWAIT;

                    /* Determine if the table contains LOBs */
                    SELECT @LOB_SQL = ' SELECT @containsLOB_OUT = COUNT(*)
                                            FROM ' + @databaseName + '.sys.columns WITH(NoLock)
                                            WHERE[object_id] = ' + CAST(@objectID AS VARCHAR(10)) + '
                                               AND(system_type_id IN (34, 35, 99)
                                                        OR max_length = -1);'
                                            /*  system_type_id --> 34 = IMAGE, 35 = TEXT, 99 = NTEXT
                                                max_length = -1 --> VARBINARY(MAX), VARCHAR(MAX), NVARCHAR(MAX), XML */
                                , @LOB_SQL_Param = '@containsLOB_OUT INT OUTPUT';

                        EXECUTE sp_executesql @LOB_SQL, @LOB_SQL_Param, @containsLOB_OUT = @containsLOB OUTPUT;

                    IF @debugMode = 1 RAISERROR('  Checking for indexes that do NOT allow page locks...', 0, 42) WITH NOWAIT;

                    /* Determine if page locks are allowed; for those indexes, we need to always REBUILD */
                    SELECT @allowPageLockSQL = 'SELECT @allowPageLocks_OUT = COUNT(*)
                                                    FROM ' + @databaseName + '.sys.indexes
                                                    WHERE object_id = ' + CAST(@objectID AS VARCHAR(10)) + '
                                                        AND index_id = ' + CAST(@indexID AS VARCHAR(10)) + '
                                                        AND Allow_Page_Locks = 0;'
                            , @allowPageLockSQL_Param = '@allowPageLocks_OUT INT OUTPUT';

                        EXECUTE sp_executesql @allowPageLockSQL, @allowPageLockSQL_Param, @allowPageLocks_OUT = @allowPageLocks OUTPUT;

                    IF @debugMode = 1 RAISERROR('  Building our SQL statements...', 0, 42) WITH NOWAIT;

                    /* IF there's not a lot of fragmentation, or if we have a LOB, we should REORGANIZE */
                    IF(@fragmentation<@rebuildThreshold OR @containsLOB >= 1 OR @partitionCount> 1)
                            AND @allowPageLocks = 0
                        BEGIN

                            SET @sqlCommand = N'ALTER INDEX ' + @indexName + N' ON ' + @databaseName + N'.' 
                                                + @schemaName + N'.' + @objectName + N' REORGANIZE';

                            /* If our index is partitioned, we should always REORGANIZE */
                            IF @partitionCount > 1
                                SET @sqlCommand = @sqlCommand + N' PARTITION = ' 
                                                + CAST(@partitionNumber AS NVARCHAR(10));

                        END
                        /* If the index is heavily fragmented and doesn't contain any partitions or LOB's, 
                           or if the index does not allow page locks, REBUILD it */
                        ELSE IF(@fragmentation >= @rebuildThreshold OR @allowPageLocks<> 0)
                            AND ISNULL(@containsLOB, 0) != 1 AND @partitionCount <= 1
                        BEGIN

                            /* Set online REBUILD options; requires Enterprise Edition */
                            IF @onlineRebuild = 1 AND @editionCheck = 1
                                SET @rebuildCommand = N' REBUILD WITH (ONLINE = ON';
                    ELSE
                        SET @rebuildCommand = N' REBUILD WITH (ONLINE = Off';
                
                            /* Set sort operation preferences */
                            IF @sortInTempDB = 1
                                SET @rebuildCommand = @rebuildCommand + N', SORT_IN_TEMPDB = ON';
                    ELSE
                        SET @rebuildCommand = @rebuildCommand + N', SORT_IN_TEMPDB = Off';

                            /* Set processor restriction options; requires Enterprise Edition */
                            IF @maxDopRestriction IS NOT NULL AND @editionCheck = 1
                                SET @rebuildCommand = @rebuildCommand + N', MAXDOP = ' + CAST(@maxDopRestriction AS VARCHAR(2)) + N')';
                            ELSE
                                SET @rebuildCommand = @rebuildCommand + N')';

                            SET @sqlCommand = N'ALTER INDEX ' + @indexName + N' ON ' + @databaseName + N'.'
                                            + @schemaName + N'.' + @objectName + @rebuildCommand;

                        END
                        ELSE
                            /* Print an error message if any indexes happen to not meet the criteria above */
                            IF @printCommands = 1 OR @debugMode = 1
                                RAISERROR('We are unable to defrag this index.', 0, 42) WITH NOWAIT;

                    /* Are we executing the SQL?  IF so, do it */
                    IF @executeSQL = 1
                        BEGIN

                            SET @debugMessage = 'Executing: ' + @sqlCommand;
                
                            /* Print the commands we're executing if specified to do so */
                            IF @printCommands = 1 OR @debugMode = 1
                                RAISERROR(@debugMessage, 0, 42) WITH NOWAIT;

                    /* Grab the time for logging purposes */
                    SET @datetimestart = GETDATE();

                    /* Log our actions, removing indexes deleted after last scan */
                    IF @objectName is null

                            BEGIN
                                UPDATE HisIndexDefragStatus
                                    SET schemaName = 'notFound',
							            objectName = 'notFound',
							            indexName  = 'notFound',
							            defragDate = getdate()

                                    WHERE objectID = @objectID

                                    and indexID = @indexID

                            END
                            ELSE

                            BEGIN

                            SELECT @IndexFillFactor = fill_factor
                            FROM  #ServerIndexes
				            WHERE object_id = @objectID

                            INSERT INTO dbo.HistIndexDefragLog
                            (databaseID,
                                databaseName,
                                objectID,
                                objectName,
                                indexID,
                                indexName,
                                partitionNumber,
                                fragmentation,
                                page_count,
                                dateTimeStart,
                                sqlStatement,

                                [fillfactor]
                            )
                            SELECT

                                @databaseID, 
					            @databaseName, 
					            @objectID, 
					            @objectName, 
					            @indexID, 
					            @indexName, 
					            @partitionNumber, 
					            @fragmentation, 
					            @pageCount,
					            @datetimestart, 
					            @sqlCommand, 
					            @IndexFillFactor

                            SET @indexDefrag_id = SCOPE_IDENTITY();
                    END

                    /* Wrap our execution attempt in a TRY/CATCH and log any errors that occur */
                    BEGIN TRY

                        /* Execute our defrag! */
                        EXECUTE sp_executesql @sqlCommand;
                    SET @dateTimeEnd = GETDATE();

                    /* Update our log with our completion time */
                    UPDATE dbo.HistIndexDefragLog

                    SET dateTimeEnd = @dateTimeEnd
                        , durationSeconds = DATEDIFF(second, @datetimestart, @dateTimeEnd)

                    WHERE indexDefrag_id = @indexDefrag_id;

                    END TRY
                            BEGIN CATCH

                                /* Update our log with our error message */
                                UPDATE dbo.HistIndexDefragLog
                                SET dateTimeEnd = GETDATE()
                                    , durationSeconds = -1
                                    , errorMessage = ERROR_MESSAGE()
                                WHERE indexDefrag_id = @indexDefrag_id;

                    IF @debugMode = 1
                                    RAISERROR('  An error has occurred executing this command! Please review the HistIndexDefragLog table for details.'
                                        , 0, 42) WITH NOWAIT;

                    END CATCH

                            /* Just a little breather for the server */
                            WAITFOR DELAY @defragDelay;

                            UPDATE dbo.HisIndexDefragStatus
                            SET defragDate = GETDATE()
                                , printStatus = 1
                            WHERE databaseID = @databaseID
                              AND objectID = @objectID
                              AND indexID = @indexID
                              AND partitionNumber = @partitionNumber;

                    END
                    ELSE
                        /* Looks like we're not executing, just printing the commands */
                        BEGIN
                            IF @debugMode = 1 RAISERROR('  Printing SQL statements...', 0, 42) WITH NOWAIT;

                    IF @printCommands = 1 OR @debugMode = 1
                                PRINT ISNULL(@sqlCommand, 'error!');

                    UPDATE dbo.HisIndexDefragStatus

                    SET printStatus = 1

                    WHERE databaseID = @databaseID

                      AND objectID = @objectID

                      AND indexID = @indexID

                      AND partitionNumber = @partitionNumber;
                    END

                END

                    /* Do we want to output our fragmentation results? */
                    IF @printFragmentation = 1
                    BEGIN

                        IF @debugMode = 1 RAISERROR('  Displaying a summary of our action...', 0, 42) WITH NOWAIT;

                    SELECT databaseID
                        , databaseName
                        , objectID
                        , objectName
                        , indexID
                        , indexName
                        , partitionNumber
                        , fragmentation
                        , page_count
                        , range_scan_count
                        FROM dbo.HisIndexDefragStatus
                        WHERE defragDate >= @startdatetime
                        ORDER BY defragDate;

                    END;

                END TRY
                BEGIN CATCH

                    SET @debugMessage = ERROR_MESSAGE() + ' (Line Number: ' + CAST(ERROR_LINE() AS VARCHAR(10)) + ')';
                    PRINT @debugMessage;

                    END CATCH;

                    /* When everything is said and done, make sure to get rid of our temp table */
                    DROP TABLE #databaseList;
                DROP TABLE #processor;
                DROP TABLE #maxPartitionList;


                DROP TABLE #ServerIndexes

                IF @debugMode = 1 RAISERROR('DONE!  Thank you for taking care of your indexes!  :)', 0, 42) WITH NOWAIT;

                    SET NOCOUNT OFF;
                RETURN 0;
            END

            GO
            
            SET ANSI_NULLS ON
            GO
            SET QUOTED_IDENTIFIER ON
            GO
            
            ALTER PROCEDURE [dbo].[stpRunDefraging] 
            /* Declare Parameters */
                @minFragmentation       FLOAT               = 10.0  
                    /* in percent, will not defrag if fragmentation less than specified */
              , @rebuildThreshold       FLOAT               = 30.0  
                    /* in percent, greater than @rebuildThreshold will result in rebuild instead of reorg */
              , @executeSQL             BIT                 = 1     
                    /* 1 = execute; 0 = print command only */
              , @defragOrderColumn      NVARCHAR(20)        = 'range_scan_count'
                    /* Valid options are: range_scan_count, fragmentation, page_count */
              , @defragSortOrder        NVARCHAR(4)         = 'DESC'
                    /* Valid options are: ASC, DESC */
              , @timeLimit              INT                 = 480 /* defaulted to 8 hours */
                    /* Optional time limitation; expressed in minutes */
              , @database               VARCHAR(128)        = NULL
                    /* Option to specify one or more database names, separated by commas; NULL will return all */
              , @tableName              VARCHAR(4000)       = NULL  -- databaseName.schema.tableName  
                    /* Option to specify a table name; null will return all */
              , @forceRescan            BIT                 = 0
                    /* Whether or not to force a rescan of indexes; 1 = force, 0 = use existing scan, if available */
              , @scanMode               VARCHAR(10)         = N'LIMITED'
                    /* Options are LIMITED, SAMPLED, and DETAILED */
              , @minPageCount           INT                 = 8 
                    /*  MS recommends > 1 extent (8 pages) */
              , @maxPageCount           INT                 = NULL
                    /* NULL = no limit */
              , @excludeMaxPartition    BIT                 = 0
                    /* 1 = exclude right-most populated partition; 0 = do not exclude; see notes for caveats */
              , @onlineRebuild          BIT                 = 1     
                    /* 1 = online rebuild; 0 = offline rebuild; only in Enterprise */
              , @sortInTempDB           BIT                 = 1
                    /* 1 = perform sort operation in TempDB; 0 = perform sort operation in the index's database */
              , @maxDopRestriction      TINYINT             = NULL
                    /* Option to restrict the number of processors for the operation; only in Enterprise */
              , @printCommands          BIT                 = 0     
                    /* 1 = print commands; 0 = do not print commands */
              , @printFragmentation     BIT                 = 0
                    /* 1 = print fragmentation prior to defrag; 
                       0 = do not print */
              , @defragDelay            CHAR(8)             = '00:00:05'
                    /* time to wait between defrag commands */
              , @debugMode              BIT                 = 0
                    /* display some useful comments to help determine if/WHERE issues occur */
            WITH ENCRYPTION
            AS /*********************************************************************************
                Name:       dba_indexDefrag_sp

                Author:     Michelle Ufford, http://sqlfool.com

                Purpose:    Defrags one or more indexes for one or more databases

                Notes:

                CAUTION: TRANSACTION LOG SIZE SHOULD BE MONITORED CLOSELY WHEN DEFRAGMENTING.
                         DO NOT RUN UNATTENDED ON LARGE DATABASES DURING BUSINESS HOURS.

                  @minFragmentation     defaulted to 10%, will not defrag if fragmentation 
                                        is less than that
      
                  @rebuildThreshold     defaulted to 30% AS recommended by Microsoft in BOL;
                                        greater than 30% will result in rebuild instead

                  @executeSQL           1 = execute the SQL generated by this proc; 
                                        0 = print command only

                  @defragOrderColumn    Defines how to prioritize the order of defrags.  Only
                                        used if @executeSQL = 1.  
                                        Valid options are: 
                                        range_scan_count = count of range and table scans on the
                                                           index; in general, this is what benefits 
                                                           the most FROM defragmentation
                                        fragmentation    = amount of fragmentation in the index;
                                                           the higher the number, the worse it is
                                        page_count       = number of pages in the index; affects
                                                           how long it takes to defrag an index

                  @defragSortOrder      The sort order of the ORDER BY clause.
                                        Valid options are ASC (ascending) or DESC (descending).

                  @timeLimit            Optional, limits how much time can be spent performing 
                                        index defrags; expressed in minutes.

                                        NOTE: The time limit is checked BEFORE an index defrag
                                              is begun, thus a long index defrag can exceed the
                                              time limitation.

                  @database             Optional, specify specific database name to defrag;
                                        If not specified, all non-system databases will
                                        be defragged.

                  @tableName            Specify if you only want to defrag indexes for a 
                                        specific table, format = databaseName.schema.tableName;
                                        if not specified, all tables will be defragged.

                  @forceRescan          Whether or not to force a rescan of indexes.  If set
                                        to 0, a rescan will not occur until all indexes have
                                        been defragged.  This can span multiple executions.
                                        1 = force a rescan
                                        0 = use previous scan, if there are indexes left to defrag

                  @scanMode             Specifies which scan mode to use to determine
                                        fragmentation levels.  Options are:
                                        LIMITED - scans the parent level; quickest mode,
                                                  recommended for most cases.
                                        SAMPLED - samples 1% of all data pages; if less than
                                                  10k pages, performs a DETAILED scan.
                                        DETAILED - scans all data pages.  Use great care with
                                                   this mode, AS it can cause performance issues.

                  @minPageCount         Specifies how many pages must exist in an index in order 
                                        to be considered for a defrag.  Defaulted to 8 pages, AS 
                                        Microsoft recommends only defragging indexes with more 
                                        than 1 extent (8 pages).  

                                        NOTE: The @minPageCount will restrict the indexes that
                                        are stored in HisIndexDefragStatus table.

                  @maxPageCount         Specifies the maximum number of pages that can exist in 
                                        an index and still be considered for a defrag.  Useful
                                        for scheduling small indexes during business hours and
                                        large indexes for non-business hours.

                                        NOTE: The @maxPageCount will restrict the indexes that
                                        are defragged during the current operation; it will not
                                        prevent indexes FROM being stored in the 
                                        HisIndexDefragStatus table.  This way, a single scan
                                        can support multiple page count thresholds.

                  @excludeMaxPartition  If an index is partitioned, this option specifies whether
                                        to exclude the right-most populated partition.  Typically,
                                        this is the partition that is currently being written to in
                                        a sliding-window scenario.  Enabling this feature may reduce
                                        contention.  This may not be applicable in other types of 
                                        partitioning scenarios.  Non-partitioned indexes are 
                                        unaffected by this option.
                                        1 = exclude right-most populated partition
                                        0 = do not exclude

                  @onlineRebuild        1 = online rebuild; 
                                        0 = offline rebuild

                  @sortInTempDB         Specifies whether to defrag the index in TEMPDB or in the
                                        database the index belongs to.  Enabling this option may
                                        result in faster defrags and prevent database file size 
                                        inflation.
                                        1 = perform sort operation in TempDB
                                        0 = perform sort operation in the index's database 

                  @maxDopRestriction    Option to specify a processor limit for index rebuilds

                  @printCommands        1 = print commands to screen; 
                                        0 = do not print commands

                  @printFragmentation   1 = print fragmentation to screen;
                                        0 = do not print fragmentation

                  @defragDelay          Time to wait between defrag commands; gives the
                                        server a little time to catch up 

                  @debugMode            1 = display debug comments; helps with troubleshooting
                                        0 = do not display debug comments

                Called by:  SQL Agent Job or DBA

                ----------------------------------------------------------------------------
                DISCLAIMER: 
                This code and information are provided \AS IS\ without warranty of any kind,
                either expressed or implied, including but not limited to the implied
                warranties or merchantability and/ or fitness for a particular purpose.
 
                 ----------------------------------------------------------------------------
 
                 LICENSE: 

                 This index defrag script is free to download and use for personal, educational,
                 and internal corporate purposes, provided that this header is preserved.
                 Redistribution or sale of this index defrag script, in whole or in part, is 

                 prohibited without the author's express written consent.
                ----------------------------------------------------------------------------

                 Date Initials    Version Description
                ----------------------------------------------------------------------------
                2007-12-18  MFU         1.0     Initial Release
                2008-10-17  MFU         1.1     Added @defragDelay, CIX_temp_indexDefragList
                 2008-11-17  MFU         1.2     Added page_count to log table
 
                                                 , added @printFragmentation option
                 2009-03-17  MFU         2.0     Provided support for centralized execution
                                                 , consolidated Enterprise & Standard versions
                                                 , added @debugMode, @maxDopRestriction
 
                                                 , modified LOB and partition logic
                 2009-06-18  MFU         3.0     Fixed bug in LOB logic, added @scanMode option
 
                                                 , added support for stat rebuilds (@rebuildStats)
 
                                                 , support model and msdb defrag
 
                                                 , added columns to the HistIndexDefragLog table
                                                 , modified logging to show \in progress\ defrags
                                                , added defrag exclusion list (scheduling)
                 2009-08-28  MFU         3.1     Fixed read_only bug for database lists
                2010-04-20  MFU         4.0     Added time limit option
                                                 , added static table with rescan logic
                                                 , added parameters for page count & SORT_IN_TEMPDB
                                                , added try/catch logic and additional debug options
                                                , added options for defrag prioritization
                                                , fixed bug for indexes with allow_page_lock = off
                                                , added option to exclude right - most partition
                                                , removed @rebuildStats option
                                                , refer to http://sqlfool.com for full release notes
                2011 - 04 - 28  MFU         4.1     Bug fixes for databases requiring[]
                                                , cleaned up the create table section
                                                , updated syntax for case-sensitive databases
                                                , comma - delimited list for @database now supported
            * ********************************************************************************
                Example of how to call this script:

                    EXECUTE [dbo].[stpRunDefraging]
                          @executeSQL = 1
                        , @printCommands = 1
                        , @debugMode = 1
                        , @printFragmentation = 1
                        , @forceRescan = 1
                        , @maxDopRestriction = 1
                        , @minPageCount = 8
                        , @maxPageCount = NULL
                        , @minFragmentation = 1
                        , @rebuildThreshold = 30
                        , @defragDelay = '00:00:05'
                        , @defragOrderColumn = 'page_count'
                        , @defragSortOrder = 'DESC'
                        , @excludeMaxPartition = 1
                        , @timeLimit = NULL
                        , @database = 'sandbox,sandbox_caseSensitive';
            *********************************************************************************/																
            SET NOCOUNT ON;
            SET XACT_ABORT ON;
            SET QUOTED_IDENTIFIER ON;

            BEGIN

                BEGIN TRY

                    /* Just a little validation... */
                    IF @minFragmentation IS NULL
                        OR @minFragmentation NOT BETWEEN 0.00 AND 100.0
                            SET @minFragmentation = 10.0;

                    IF @rebuildThreshold IS NULL
                        OR @rebuildThreshold NOT BETWEEN 0.00 AND 100.0
                            SET @rebuildThreshold = 30.0;

                    IF @defragDelay NOT LIKE '00:[0-5][0-9]:[0-5][0-9]'
                        SET @defragDelay = '00:00:05';

                    IF @defragOrderColumn IS NULL
                        OR @defragOrderColumn NOT IN('range_scan_count', 'fragmentation', 'page_count')
                            SET @defragOrderColumn = 'range_scan_count';

                    IF @defragSortOrder IS NULL
                        OR @defragSortOrder NOT IN('ASC', 'DESC')
                            SET @defragSortOrder = 'DESC';

                    IF @scanMode NOT IN('LIMITED', 'SAMPLED', 'DETAILED')
                        SET @scanMode = 'LIMITED';

                    IF @debugMode IS NULL
                        SET @debugMode = 0;

                    IF @forceRescan IS NULL
                        SET @forceRescan = 0;

                    IF @sortInTempDB IS NULL
                        SET @sortInTempDB = 1;

                    IF @database = ''

                        SET @database = NULL


                    IF @tableName = ''

                        SET @tableName = NULL


                    IF @maxPageCount = 0

                        SET @maxPageCount = NULL


                    IF @debugMode = 1 RAISERROR('Undusting the cogs AND starting up...', 0, 42) WITH NOWAIT;

                    /* Declare our variables */
                    DECLARE @objectID                 INT
                            , @databaseID INT
                            , @databaseName             NVARCHAR(128)
                            , @indexID INT
                            , @partitionCount           BIGINT
                            , @schemaName NVARCHAR(128)
                            , @objectName NVARCHAR(128)
                            , @indexName NVARCHAR(128)
                            , @partitionNumber SMALLINT
                            , @fragmentation            FLOAT
                            , @pageCount INT
                            , @sqlCommand               NVARCHAR(4000)
                            , @rebuildCommand NVARCHAR(200)
                            , @datetimestart DATETIME
                            , @dateTimeEnd              DATETIME
                            , @containsLOB BIT
                            , @editionCheck             BIT
                            , @debugMessage NVARCHAR(4000)
                            , @updateSQL NVARCHAR(4000)
                            , @partitionSQL NVARCHAR(4000)
                            , @partitionSQL_Param NVARCHAR(1000)
                            , @LOB_SQL NVARCHAR(4000)
                            , @LOB_SQL_Param NVARCHAR(1000)
                            , @indexDefrag_id INT
                            , @startdatetime            DATETIME
                            , @enddatetime DATETIME
                            , @getIndexSQL              NVARCHAR(4000)
                            , @getIndexSQL_Param NVARCHAR(4000)
                            , @allowPageLockSQL NVARCHAR(4000)
                            , @allowPageLockSQL_Param NVARCHAR(4000)
                            , @allowPageLocks INT
                            , @excludeMaxPartitionSQL   NVARCHAR(4000)
				            , @IndexFillFactor INT;

                    /* Initialize our variables */
                    SELECT @startdatetime = GETDATE()
                        , @enddatetime = DATEADD(minute, @timeLimit, GETDATE());

                    /* Create our temporary tables */
                    CREATE TABLE #databaseList
                    (
                          databaseID INT
                        , databaseName VARCHAR(128)
                        , scanStatus BIT
                    );

                    CREATE TABLE #processor 
                    (
                          [index]           INT
                        , Name VARCHAR(128)
                        , Internal_Value INT
                        , Character_Value   INT
                    );

                    CREATE TABLE #maxPartitionList
                    (
                          databaseID INT
                        , objectID INT
                        , indexID INT
                        , maxPartition INT
                    );

		            --Created to get FillFactor of Reindexed indexes
                    CREATE TABLE #ServerIndexes 
		            (
                        object_id           int,
                        name varchar(8000), 
			            type_desc varchar(8000), 
			            fill_factor int
		            )

		            EXECUTE sp_MSforeachdb 'use [?]; insert #ServerIndexes select object_id, name, type_desc, fill_factor from sys.indexes where name is not null'



                    IF @debugMode = 1 RAISERROR('Beginning validation...', 0, 42) WITH NOWAIT;

                    /* Make sure we're not exceeding the number of processors we have available */
                    INSERT INTO #processor
                    EXECUTE xp_msver 'ProcessorCount';

                    IF @maxDopRestriction IS NOT NULL AND @maxDopRestriction > (SELECT Internal_Value FROM #processor)
                        SELECT @maxDopRestriction = Internal_Value
                        FROM #processor;

                    /* Check our server version; 1804890536 = Enterprise, 610778273 = Enterprise Evaluation, -2117995310 = Developer 
									             1872460670 = Enterprise Core-bases														*/
                    IF(SELECT ServerProperty('EditionID')) IN(1804890536, 610778273, -2117995310, 1872460670)
                        SET @editionCheck = 1-- supports online rebuilds
                   ELSE
                        SET @editionCheck = 0; -- does not support online rebuilds

                    /* Output the parameters we're working with */
                    IF @debugMode = 1 
                    BEGIN

                        SELECT @debugMessage = 'Your SELECTed parameters are... 
                        Defrag indexes WITH fragmentation greater than ' + CAST(@minFragmentation AS VARCHAR(10)) + ';
                    REBUILD indexes WITH fragmentation greater than ' + CAST(@rebuildThreshold AS VARCHAR(10)) + ';
                    You' + CASE WHEN @executeSQL = 1 THEN ' DO' ELSE ' DO NOT' END + ' want the commands to be executed automatically; 
                        You want to defrag indexes in ' + @defragSortOrder + ' order of the ' + UPPER(@defragOrderColumn) + ' value;
                        You have' + CASE WHEN @timeLimit IS NULL THEN ' NOT specified a time limit;' ELSE ' specified a time limit of ' 
                            + CAST(@timeLimit AS VARCHAR(10)) END + ' minutes;
                        ' + CASE WHEN @database IS NULL THEN 'ALL databases' ELSE 'The ' + @database + ' database(s)' END + ' will be defragged;
                        ' + CASE WHEN @tableName IS NULL THEN 'ALL tables' ELSE 'The ' + @tableName + ' TABLE' END + ' will be defragged;
                        We' + CASE WHEN EXISTS(SELECT Top 1 * FROM dbo.HisIndexDefragStatus WHERE defragDate IS NULL)
                            AND @forceRescan<> 1 THEN ' WILL NOT' ELSE ' WILL' END + ' be rescanning indexes;
                        The scan will be performed in ' + @scanMode + ' mode;
                    You want to limit defrags to indexes with' + CASE WHEN @maxPageCount IS NULL THEN ' more than ' 
                            + CAST(@minPageCount AS VARCHAR(10)) ELSE
                            ' BETWEEN ' + CAST(@minPageCount AS VARCHAR(10))
                            + ' AND ' + CAST(@maxPageCount AS VARCHAR(10)) END + ' pages;
                        Indexes will be defragged' + CASE WHEN @editionCheck = 0 OR @onlineRebuild = 0 THEN ' OFFLINE;' ELSE ' ONLINE;' END + '
                        Indexes will be sorted in' + CASE WHEN @sortInTempDB = 0 THEN ' the DATABASE' ELSE ' TEMPDB;' END + '
                        Defrag operations will utilize ' + CASE WHEN @editionCheck = 0 OR @maxDopRestriction IS NULL 
                            THEN 'system defaults for processors;' 
                            ELSE CAST(@maxDopRestriction AS VARCHAR(2)) + ' processors;' END + '
                        You' + CASE WHEN @printCommands = 1 THEN ' DO' ELSE ' DO NOT' END + ' want to PRINT the ALTER INDEX commands; 
                        You' + CASE WHEN @printFragmentation = 1 THEN ' DO' ELSE ' DO NOT' END + ' want to OUTPUT fragmentation levels; 
                        You want to wait ' + @defragDelay + ' (hh:mm:ss) BETWEEN defragging indexes;
                        You want to run in' + CASE WHEN @debugMode = 1 THEN ' DEBUG' ELSE ' SILENT' END + ' mode.';

                        RAISERROR(@debugMessage, 0, 42) WITH NOWAIT;

                    END;

                    IF @debugMode = 1 RAISERROR('Grabbing a list of our databases...', 0, 42) WITH NOWAIT;

                    /* Retrieve the list of databases to investigate */
                    /* If @database is NULL, it means we want to defrag *all* databases */
                    IF @database IS NULL
                    BEGIN

                        INSERT INTO #databaseList
                        SELECT database_id
                            , name
                            , 0 -- not scanned yet for fragmentation
                        FROM sys.databases
                        WHERE[name] NOT IN('master', 'tempdb')-- exclude system databases
                          AND[state] = 0-- state must be ONLINE
                          AND is_read_only = 0 -- cannot be read_only
						  AND [name] NOT IN (
											SELECT 
									               ADC.database_name                               
								            FROM sys.availability_groups_cluster as AGC                                                                            
								            JOIN sys.dm_hadr_availability_replica_cluster_states as RCS ON AGC.group_id = RCS.group_id                             
								            JOIN sys.dm_hadr_availability_replica_states as ARS ON RCS.replica_id = ARS.replica_id and RCS.group_id = ARS.group_id 
								            JOIN sys.availability_databases_cluster as ADC ON AGC.group_id = ADC.group_id                                          
								            WHERE ARS.is_local = 1
								            AND ARS.role_desc LIKE 'SECONDARY'); --Only databases Primary  

                    END;
                    ELSE
                    /* Otherwise, we're going to just defrag our list of databases */
                    BEGIN

                        INSERT INTO #databaseList
                        SELECT database_id
                            , name
                            , 0 -- not scanned yet for fragmentation
                        FROM sys.databases AS d
                        JOIN dbo.fnParseStringUdf(@database, ',') AS x
                            ON d.name COLLATE Latin1_General_CI_AI = x.stringValue
                        WHERE[name] NOT IN ('master', 'tempdb')-- exclude system databases
                           AND[state] = 0-- state must be ONLINE
                            AND is_read_only = 0;  -- cannot be read_only

                    END;

                    /* Check to see IF we have indexes in need of defrag; otherwise, re-scan the database(s) */
                    IF NOT EXISTS(SELECT Top 1 * FROM dbo.HisIndexDefragStatus WHERE defragDate IS NULL)
                        OR @forceRescan = 1
                    BEGIN

                        /* Truncate our list of indexes to prepare for a new scan */
                        TRUNCATE TABLE dbo.HisIndexDefragStatus;

                    IF @debugMode = 1 RAISERROR('Looping through our list of databases and checking for fragmentation...', 0, 42) WITH NOWAIT;

                    /* Loop through our list of databases */
                    WHILE(SELECT COUNT(*) FROM #databaseList WHERE scanStatus = 0) > 0
                        BEGIN

                        SELECT Top 1 @databaseID = databaseID

                        FROM #databaseList
                            WHERE scanStatus = 0;

                    SELECT @debugMessage = '  working on ' + DB_NAME(@databaseID) + '...';

                    IF @debugMode = 1
                                RAISERROR(@debugMessage, 0, 42) WITH NOWAIT;

                    /* Determine which indexes to defrag using our user-defined parameters */
                    INSERT INTO dbo.HisIndexDefragStatus
                            (
                                  databaseID
                                , databaseName
                                , objectID
                                , indexID
                                , partitionNumber
                                , fragmentation
                                , page_count
                                , range_scan_count
                                , scanDate
                            )
                            SELECT
                                  ps.database_id AS 'databaseID'
                                , QUOTENAME(DB_NAME(ps.database_id)) AS 'databaseName'
                                , ps.[object_id] AS 'objectID'
                                , ps.index_id AS 'indexID'
                                , ps.partition_number AS 'partitionNumber'
                                , SUM(ps.avg_fragmentation_in_percent) AS 'fragmentation'
                                , SUM(ps.page_count) AS 'page_count'
                                , os.range_scan_count
                                , GETDATE() AS 'scanDate'
                            FROM sys.dm_db_index_physical_stats(@databaseID, OBJECT_ID(@tableName), NULL , NULL, @scanMode) AS ps
                            JOIN sys.dm_db_index_operational_stats(@databaseID, OBJECT_ID(@tableName), NULL , NULL) AS os
                                ON ps.database_id = os.database_id
                                AND ps.[object_id] = os.[object_id]
                                AND ps.index_id = os.index_id
                                AND ps.partition_number = os.partition_number
                            WHERE avg_fragmentation_in_percent >= @minFragmentation
                                AND ps.index_id > 0 -- ignore heaps
                                AND ps.page_count > @minPageCount
                                AND ps.index_level = 0-- leaf-level nodes only, supports @scanMode
                                AND ps.[OBJECT_ID] not in (SELECT ObjectID FROM [dbo].[HistIndexDefragTablesToExclude])
                            GROUP BY ps.database_id 
                                , QUOTENAME(DB_NAME(ps.database_id))
                                , ps.[object_id]
                                , ps.index_id 
                                , ps.partition_number 
                                , os.range_scan_count
                            OPTION(MAXDOP 2);

                    /* Do we want to exclude right-most populated partition of our partitioned indexes? */
                    IF @excludeMaxPartition = 1
                            BEGIN

                                SET @excludeMaxPartitionSQL = '
                                    SELECT ' + CAST(@databaseID AS VARCHAR(10)) + ' AS[databaseID]
                                        , [object_id]
                                        , index_id
                                        , MAX(partition_number) AS[maxPartition]
                                    FROM[' + DB_NAME(@databaseID) + '].sys.partitions
                                   WHERE partition_number > 1
                                        AND[rows] > 0
                                    GROUP BY object_id
                                        , index_id;';

                                INSERT INTO #maxPartitionList
                                EXECUTE sp_executesql @excludeMaxPartitionSQL;

                            END;
                
                            /* Keep track of which databases have already been scanned */
                            UPDATE #databaseList
                            SET scanStatus = 1
                            WHERE databaseID = @databaseID;

                    END

                    /* We don't want to defrag the right-most populated partition, so
                       delete any records for partitioned indexes where partition = MAX(partition) */
                    IF @excludeMaxPartition = 1
                        BEGIN

                            DELETE ids
                            FROM dbo.HisIndexDefragStatus AS ids
                            JOIN #maxPartitionList AS mpl
                                ON ids.databaseID = mpl.databaseID
                                AND ids.objectID = mpl.objectID
                                AND ids.indexID = mpl.indexID
                                AND ids.partitionNumber = mpl.maxPartition;

                    END;

                        /* Update our exclusion mask for any index that has a restriction ON the days it can be defragged */
                        UPDATE ids
                        SET ids.exclusionMask = ide.exclusionMask
                        FROM dbo.HisIndexDefragStatus AS ids
                        JOIN dbo.HistIndexDefragExclusion AS ide
                            ON ids.databaseID = ide.databaseID
                            AND ids.objectID = ide.objectID
                            AND ids.indexID = ide.indexID;

                    END

                    SELECT @debugMessage = 'Looping through our list... there are ' + CAST(COUNT(*) AS VARCHAR(10)) + ' indexes to defrag!'
                    FROM dbo.HisIndexDefragStatus
                    WHERE defragDate IS NULL
                        AND page_count BETWEEN @minPageCount AND ISNULL(@maxPageCount, page_count);

                    IF @debugMode = 1 RAISERROR(@debugMessage, 0, 42) WITH NOWAIT;

                    /* Begin our loop for defragging */
                    WHILE(SELECT COUNT(*)
                           FROM dbo.HisIndexDefragStatus
                           WHERE (
                                       (@executeSQL = 1 AND defragDate IS NULL)
                                    OR(@executeSQL = 0 AND defragDate IS NULL AND printStatus = 0)
                                 )
                            AND exclusionMask & POWER(2, DATEPART(weekday, GETDATE())-1) = 0
                            AND page_count BETWEEN @minPageCount AND ISNULL(@maxPageCount, page_count)) > 0
                    BEGIN

                        /* Check to see IF we need to exit our loop because of our time limit */
                        IF ISNULL(@enddatetime, GETDATE()) < GETDATE()
                        BEGIN
                            RAISERROR('Our time limit has been exceeded!', 11, 42) WITH NOWAIT;
                    END;

                        IF @debugMode = 1 RAISERROR('  Picking an index to beat into shape...', 0, 42) WITH NOWAIT;

                    /* Grab the index with the highest priority, based on the values submitted; 
                       Look at the exclusion mask to ensure it can be defragged today */
                    SET @getIndexSQL = N'
                        SELECT TOP 1 
                              @objectID_Out         = objectID
                            , @indexID_Out          = indexID
                            , @databaseID_Out       = databaseID
                            , @databaseName_Out     = databaseName
                            , @fragmentation_Out    = fragmentation
                            , @partitionNumber_Out  = partitionNumber
                            , @pageCount_Out        = page_count
                        FROM dbo.HisIndexDefragStatus
                        WHERE defragDate IS NULL ' 
                            + CASE WHEN @executeSQL = 0 THEN 'AND printStatus = 0' ELSE '' END + '
                            AND exclusionMask & Power(2, DatePart(weekday, GETDATE())-1) = 0
                            AND page_count BETWEEN @p_minPageCount AND ISNULL(@p_maxPageCount, page_count)
                        ORDER BY + ' + @defragOrderColumn + ' ' + @defragSortOrder;


                        SET @getIndexSQL_Param = N'@objectID_Out        INT OUTPUT
                                                 , @indexID_Out         INT OUTPUT
                                                 , @databaseID_Out      INT OUTPUT
                                                 , @databaseName_Out    NVARCHAR(128) OUTPUT
                                                 , @fragmentation_Out INT OUTPUT
                                                 , @partitionNumber_Out INT OUTPUT
                                                 , @pageCount_Out INT OUTPUT
                                                 , @p_minPageCount INT
                                                 , @p_maxPageCount      INT';

                        EXECUTE sp_executesql @getIndexSQL
                            , @getIndexSQL_Param
                            , @p_minPageCount       = @minPageCount
                            , @p_maxPageCount       = @maxPageCount
                            , @objectID_Out         = @objectID OUTPUT
                            , @indexID_Out = @indexID          OUTPUT
                            , @databaseID_Out       = @databaseID OUTPUT
                            , @databaseName_Out = @databaseName     OUTPUT
                            , @fragmentation_Out    = @fragmentation OUTPUT
                            , @partitionNumber_Out = @partitionNumber  OUTPUT
                            , @pageCount_Out        = @pageCount OUTPUT;

                    IF @debugMode = 1 RAISERROR('  Looking up the specifics for our index...', 0, 42) WITH NOWAIT;

                    /* Look up index information */
                    SELECT @updateSQL = N'UPDATE ids
                            SET schemaName = QUOTENAME(s.name)
                                , objectName = QUOTENAME(o.name)
                                , indexName = QUOTENAME(i.name)
                            FROM dbo.HisIndexDefragStatus AS ids
                            INNER JOIN ' + @databaseName + '.sys.objects AS o
                                ON ids.objectID = o.[object_id]
                            INNER JOIN ' + @databaseName + '.sys.indexes AS i
                                ON o.[object_id] = i.[object_id]
                                AND ids.indexID = i.index_id
                            INNER JOIN ' + @databaseName + '.sys.schemas AS s
                                ON o.schema_id = s.schema_id
                            WHERE o.[object_id] = ' + CAST(@objectID AS VARCHAR(10)) + '
                                AND i.index_id = ' + CAST(@indexID AS VARCHAR(10)) + '
                                AND i.type > 0
                                AND ids.databaseID = ' + CAST(@databaseID AS VARCHAR(10));

                        EXECUTE sp_executesql @updateSQL;

                    /* Grab our object names */
                    SELECT @objectName = objectName
                        , @schemaName = schemaName
                        , @indexName = indexName
                        FROM dbo.HisIndexDefragStatus
                        WHERE objectID = @objectID
                            AND indexID = @indexID
                            AND databaseID = @databaseID;

                    IF @debugMode = 1 RAISERROR('  Grabbing the partition COUNT...', 0, 42) WITH NOWAIT;

                    /* Determine if the index is partitioned */
                    SELECT @partitionSQL = 'SELECT @partitionCount_OUT = COUNT(*)
                                                    FROM ' + @databaseName + '.sys.partitions
                                                    WHERE object_id = ' + CAST(@objectID AS VARCHAR(10)) + '
                                                        AND index_id = ' + CAST(@indexID AS VARCHAR(10)) + ';'
                            , @partitionSQL_Param = '@partitionCount_OUT INT OUTPUT';

                        EXECUTE sp_executesql @partitionSQL, @partitionSQL_Param, @partitionCount_OUT = @partitionCount OUTPUT;

                    IF @debugMode = 1 RAISERROR('  Seeing IF there are any LOBs to be handled...', 0, 42) WITH NOWAIT;

                    /* Determine if the table contains LOBs */
                    SELECT @LOB_SQL = ' SELECT @containsLOB_OUT = COUNT(*)
                                            FROM ' + @databaseName + '.sys.columns WITH(NoLock)
                                            WHERE[object_id] = ' + CAST(@objectID AS VARCHAR(10)) + '
                                               AND(system_type_id IN (34, 35, 99)
                                                        OR max_length = -1);'
                                            /*  system_type_id --> 34 = IMAGE, 35 = TEXT, 99 = NTEXT
                                                max_length = -1 --> VARBINARY(MAX), VARCHAR(MAX), NVARCHAR(MAX), XML */
                                , @LOB_SQL_Param = '@containsLOB_OUT INT OUTPUT';

                        EXECUTE sp_executesql @LOB_SQL, @LOB_SQL_Param, @containsLOB_OUT = @containsLOB OUTPUT;

                    IF @debugMode = 1 RAISERROR('  Checking for indexes that do NOT allow page locks...', 0, 42) WITH NOWAIT;

                    /* Determine if page locks are allowed; for those indexes, we need to always REBUILD */
                    SELECT @allowPageLockSQL = 'SELECT @allowPageLocks_OUT = COUNT(*)
                                                    FROM ' + @databaseName + '.sys.indexes
                                                    WHERE object_id = ' + CAST(@objectID AS VARCHAR(10)) + '
                                                        AND index_id = ' + CAST(@indexID AS VARCHAR(10)) + '
                                                        AND Allow_Page_Locks = 0;'
                            , @allowPageLockSQL_Param = '@allowPageLocks_OUT INT OUTPUT';

                        EXECUTE sp_executesql @allowPageLockSQL, @allowPageLockSQL_Param, @allowPageLocks_OUT = @allowPageLocks OUTPUT;

                    IF @debugMode = 1 RAISERROR('  Building our SQL statements...', 0, 42) WITH NOWAIT;

                    /* IF there's not a lot of fragmentation, or if we have a LOB, we should REORGANIZE */
                    IF(@fragmentation<@rebuildThreshold OR @containsLOB >= 1 OR @partitionCount> 1)
                            AND @allowPageLocks = 0
                        BEGIN

                            SET @sqlCommand = N'ALTER INDEX ' + @indexName + N' ON ' + @databaseName + N'.' 
                                                + @schemaName + N'.' + @objectName + N' REORGANIZE';

                            /* If our index is partitioned, we should always REORGANIZE */
                            IF @partitionCount > 1
                                SET @sqlCommand = @sqlCommand + N' PARTITION = ' 
                                                + CAST(@partitionNumber AS NVARCHAR(10));

                        END
                        /* If the index is heavily fragmented and doesn't contain any partitions or LOB's, 
                           or if the index does not allow page locks, REBUILD it */
                        ELSE IF(@fragmentation >= @rebuildThreshold OR @allowPageLocks<> 0)
                            AND ISNULL(@containsLOB, 0) != 1 AND @partitionCount <= 1
                        BEGIN

                            /* Set online REBUILD options; requires Enterprise Edition */
                            IF @onlineRebuild = 1 AND @editionCheck = 1
                                SET @rebuildCommand = N' REBUILD WITH (ONLINE = ON';
                    ELSE
                        SET @rebuildCommand = N' REBUILD WITH (ONLINE = Off';
                
                            /* Set sort operation preferences */
                            IF @sortInTempDB = 1
                                SET @rebuildCommand = @rebuildCommand + N', SORT_IN_TEMPDB = ON';
                    ELSE
                        SET @rebuildCommand = @rebuildCommand + N', SORT_IN_TEMPDB = Off';

                            /* Set processor restriction options; requires Enterprise Edition */
                            IF @maxDopRestriction IS NOT NULL AND @editionCheck = 1
                                SET @rebuildCommand = @rebuildCommand + N', MAXDOP = ' + CAST(@maxDopRestriction AS VARCHAR(2)) + N')';
                            ELSE
                                SET @rebuildCommand = @rebuildCommand + N')';

                            SET @sqlCommand = N'ALTER INDEX ' + @indexName + N' ON ' + @databaseName + N'.'
                                            + @schemaName + N'.' + @objectName + @rebuildCommand;

                        END
                        ELSE
                            /* Print an error message if any indexes happen to not meet the criteria above */
                            IF @printCommands = 1 OR @debugMode = 1
                                RAISERROR('We are unable to defrag this index.', 0, 42) WITH NOWAIT;

                    /* Are we executing the SQL?  IF so, do it */
                    IF @executeSQL = 1
                        BEGIN

                            SET @debugMessage = 'Executing: ' + @sqlCommand;
                
                            /* Print the commands we're executing if specified to do so */
                            IF @printCommands = 1 OR @debugMode = 1
                                RAISERROR(@debugMessage, 0, 42) WITH NOWAIT;

                    /* Grab the time for logging purposes */
                    SET @datetimestart = GETDATE();

                    /* Log our actions, removing indexes deleted after last scan */
                    IF @objectName is null

                            BEGIN
                                UPDATE HisIndexDefragStatus
                                    SET schemaName = 'notFound',
							            objectName = 'notFound',
							            indexName  = 'notFound',
							            defragDate = getdate()

                                    WHERE objectID = @objectID

                                    and indexID = @indexID

                            END
                            ELSE

                            BEGIN

                            SELECT @IndexFillFactor = fill_factor
                            FROM  #ServerIndexes
				            WHERE object_id = @objectID

                            INSERT INTO dbo.HistIndexDefragLog
                            (databaseID,
                                databaseName,
                                objectID,
                                objectName,
                                indexID,
                                indexName,
                                partitionNumber,
                                fragmentation,
                                page_count,
                                dateTimeStart,
                                sqlStatement,

                                [fillfactor]
                            )
                            SELECT

                                @databaseID, 
					            @databaseName, 
					            @objectID, 
					            @objectName, 
					            @indexID, 
					            @indexName, 
					            @partitionNumber, 
					            @fragmentation, 
					            @pageCount,
					            @datetimestart, 
					            @sqlCommand, 
					            @IndexFillFactor

                            SET @indexDefrag_id = SCOPE_IDENTITY();
                    END

                    /* Wrap our execution attempt in a TRY/CATCH and log any errors that occur */
                    BEGIN TRY

                        /* Execute our defrag! */
                        EXECUTE sp_executesql @sqlCommand;
                    SET @dateTimeEnd = GETDATE();

                    /* Update our log with our completion time */
                    UPDATE dbo.HistIndexDefragLog

                    SET dateTimeEnd = @dateTimeEnd
                        , durationSeconds = DATEDIFF(second, @datetimestart, @dateTimeEnd)

                    WHERE indexDefrag_id = @indexDefrag_id;

                    END TRY
                            BEGIN CATCH

                                /* Update our log with our error message */
                                UPDATE dbo.HistIndexDefragLog
                                SET dateTimeEnd = GETDATE()
                                    , durationSeconds = -1
                                    , errorMessage = ERROR_MESSAGE()
                                WHERE indexDefrag_id = @indexDefrag_id;

                    IF @debugMode = 1
                                    RAISERROR('  An error has occurred executing this command! Please review the HistIndexDefragLog table for details.'
                                        , 0, 42) WITH NOWAIT;

                    END CATCH

                            /* Just a little breather for the server */
                            WAITFOR DELAY @defragDelay;

                            UPDATE dbo.HisIndexDefragStatus
                            SET defragDate = GETDATE()
                                , printStatus = 1
                            WHERE databaseID = @databaseID
                              AND objectID = @objectID
                              AND indexID = @indexID
                              AND partitionNumber = @partitionNumber;

                    END
                    ELSE
                        /* Looks like we're not executing, just printing the commands */
                        BEGIN
                            IF @debugMode = 1 RAISERROR('  Printing SQL statements...', 0, 42) WITH NOWAIT;

                    IF @printCommands = 1 OR @debugMode = 1
                                PRINT ISNULL(@sqlCommand, 'error!');

                    UPDATE dbo.HisIndexDefragStatus

                    SET printStatus = 1

                    WHERE databaseID = @databaseID

                      AND objectID = @objectID

                      AND indexID = @indexID

                      AND partitionNumber = @partitionNumber;
                    END

                END

                    /* Do we want to output our fragmentation results? */
                    IF @printFragmentation = 1
                    BEGIN

                        IF @debugMode = 1 RAISERROR('  Displaying a summary of our action...', 0, 42) WITH NOWAIT;

                    SELECT databaseID
                        , databaseName
                        , objectID
                        , objectName
                        , indexID
                        , indexName
                        , partitionNumber
                        , fragmentation
                        , page_count
                        , range_scan_count
                        FROM dbo.HisIndexDefragStatus
                        WHERE defragDate >= @startdatetime
                        ORDER BY defragDate;

                    END;

                END TRY
                BEGIN CATCH

                    SET @debugMessage = ERROR_MESSAGE() + ' (Line Number: ' + CAST(ERROR_LINE() AS VARCHAR(10)) + ')';
                    PRINT @debugMessage;

                    END CATCH;

                    /* When everything is said and done, make sure to get rid of our temp table */
                    DROP TABLE #databaseList;
                DROP TABLE #processor;
                DROP TABLE #maxPartitionList;


                DROP TABLE #ServerIndexes

                IF @debugMode = 1 RAISERROR('DONE!  Thank you for taking care of your indexes!  :)', 0, 42) WITH NOWAIT;

                    SET NOCOUNT OFF;
                RETURN 0;
            END

            
            ";

        }
    }
}
