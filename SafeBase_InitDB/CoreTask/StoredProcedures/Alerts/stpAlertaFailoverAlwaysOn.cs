using System;
using System.Collections.Generic;
using System.Text;

namespace InitDB.Client
{
    class stpAlertaFailoverAlwaysOn
	{
        public static string Query()
        {
            return
			@"  
			SET NOCOUNT ON;

			SET QUOTED_IDENTIFIER ON;
	 
			-- Failover AlwaysOn
			DECLARE @Id_AlertaParametro INT = (SELECT Id_AlertaParametro FROM [dbo].AlertaParametro (NOLOCK) WHERE Nm_Alerta = 'Failover AlwaysOn' AND Ativo = 1)
			DECLARE @Ds_Caminho_Base VARCHAR(100) = (SELECT Ds_Caminho FROM [dbo].AlertaParametro (NOLOCK) WHERE Nm_Alerta = 'CheckList')
			DECLARE @Telegram INT = (select Id_AlertaParametro from AlertaParametro WHERE Nm_Alerta = 'Envia Telegram')
			DECLARE @Teams INT = (select Id_AlertaParametro from AlertaParametro WHERE Nm_Alerta = 'Envia Teams')
	
			-- Declara as variaveis
			DECLARE	@Subject VARCHAR(500), @Fl_Tipo TINYINT, @Qtd_Segundos INT, @Consulta VARCHAR(8000), @Importance AS VARCHAR(6), @Dt_Atual DATETIME,
					@EmailBody VARCHAR(MAX), @AlertaLockHeader VARCHAR(MAX), @AlertaLockTable VARCHAR(MAX), @EmptyBodyEmail VARCHAR(MAX),
					@AlertaLockRaizHeader VARCHAR(MAX), @AlertaLockRaizTable VARCHAR(MAX), @Processo_Bloqueado_Parametro INT, @Qt_Tempo_Raiz_Lock INT,
					@EmailDestination VARCHAR(200), @TextRel1 VARCHAR(4000), @TextRel2 VARCHAR(4000), @NomeRel VARCHAR(300),
					@MntMsg VARCHAR(200), @TLMsg VARCHAR(200), @SendMail VARCHAR(200), @ProfileDBMail VARCHAR(50), @BodyFormatMail VARCHAR(20), 
					@CaminhoPath VARCHAR(50), @CaminhoFim VARCHAR(50), @Ass VARCHAR(4000),@HTML VARCHAR(MAX), @Query VARCHAR(MAX), @Ds_Email_Assunto_alerta VARCHAR (600), 
					@Ds_Email_Assunto_solucao VARCHAR (600), @Ds_Email_Texto_alerta VARCHAR (600), @Ds_Email_Texto_solucao VARCHAR (600), 
					@Ds_Menssageiro_01 VARCHAR (30), @Ds_Menssageiro_02 VARCHAR (30), @Ds_Menssageiro_03 VARCHAR (30),
					@ZabbixPath varchar(128), @ZabbixServer varchar(128), @ZabbixLocalServer varchar(128), @ZabbixAlertName varchar(128)
	
			--------------------------------------------------------------------------------------------------------------------------------
			-- Recupera os parametros do Alerta
			--------------------------------------------------------------------------------------------------------------------------------
			SELECT @NomeRel = Nm_Alerta, 
				   @Processo_Bloqueado_Parametro = Vl_Parametro, 
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

			-- Parametros do Alerta
			SET @Subject =  @Ds_Email_Assunto_alerta +' '+ @@SERVERNAME
			SET @HTML =  @Ds_Email_Texto_alerta 
			SET @CaminhoFim = @Ds_Caminho_Base + @CaminhoPath + @NomeRel +'.html'

			--------------------------------------------------------------------------------------------------------------------------------
			-- Lista os AG com eventos de Failover nao logados
			--------------------------------------------------------------------------------------------------------------------------------
			DECLARE @IsHadrEnabled AS SQL_VARIANT  
			SET @IsHadrEnabled = (SELECT SERVERPROPERTY('IsHadrEnabled'))  

			IF @IsHadrEnabled = 1
			BEGIN
				DECLARE @FileName NVARCHAR(MAX)
	
				SELECT 
					@FileName = target_data.value('(EventFileTarget/File/@name)[1]','nvarchar(4000)')
				FROM 
					(
					SELECT
						CAST(target_data AS XML) target_data
					FROM 
						sys.dm_xe_sessions s
					INNER JOIN sys.dm_xe_session_targets t
						ON s.address = t.event_session_address
					WHERE 
						s.name = N'AlwaysOn_health'
				) ft

				DECLARE @utc INT = DATEDIFF(HOUR,GETDATE(),GETUTCDATE())

				IF OBJECT_ID('tempdb..#TempQueueFailover') IS NOT NULL
					DROP TABLE #TempQueueFailover

				CREATE TABLE #TempQueueFailover (
						availability_replica_id VARCHAR(255),
						availability_replica_name VARCHAR(255),
						availability_group_id VARCHAR(255),
						availability_group_name VARCHAR(255),
						previous_state VARCHAR(255),
						current_state VARCHAR(255),
						event_timestamp DATETIME
				);


				;WITH EventPrincipal AS (
					SELECT
						ROW_NUMBER() OVER(PARTITION BY availability_group_name ORDER BY event_timestamp desc) as rn,
						availability_replica_id,
						availability_replica_name,
						availability_group_id,
						availability_group_name,
						previous_state,
						current_state,
						event_timestamp
					FROM
						(
						SELECT 	
							DATEADD(HH,-(@utc),XEData.value('(event/@timestamp)[1]','datetime')) AS event_timestamp,
							--XEData.value('(event/@timestamp)[1]','datetime2(3)') AS event_timestamp,
							XEData.value('(event/data[@name=""previous_state""]/text)[1]', 'varchar(255)') AS previous_state,
							XEData.value('(event/data[@name=""current_state""]/text)[1]', 'varchar(255)') AS current_state,
							XEData.value('(event/data[@name=""availability_replica_name""]/value)[1]', 'varchar(255)') AS availability_replica_name,
							XEData.value('(event/data[@name=""availability_replica_id""]/value)[1]', 'varchar(255)') AS availability_replica_id,
							XEData.value('(event/data[@name=""availability_group_name""]/value)[1]', 'varchar(255)') AS availability_group_name,
							XEData.value('(event/data[@name=""availability_group_id""]/value)[1]', 'varchar(255)') AS availability_group_id
						FROM
							(
							SELECT
								CAST(event_data AS XML) XEData, *
							FROM
								sys.fn_xe_file_target_read_file(@FileName, NULL, NULL, NULL)
							WHERE
								object_name = 'availability_replica_state_change'
						) event_data
						WHERE
							XEData.value('(event/data[@name=""current_state""]/text)[1]', 'varchar(255)') = 'PRIMARY_NORMAL'
						) x
					), Historico AS
					(
					SELECT
						AvailabilityGroupName,
						AvailabilityReplicaName,
						MAX(EventTime) AS EventTime
					FROM
						[dbo].[HistoricoAlwaysOnFailover]
					GROUP BY
						AvailabilityGroupName,
						AvailabilityReplicaName
					)
				INSERT INTO #TempQueueFailover 
				SELECT
					availability_replica_id,
					availability_replica_name,
					availability_group_id,
					availability_group_name,
					previous_state,
					current_state,
					event_timestamp
				FROM
					EventPrincipal ev
				INNER JOIN sys.dm_hadr_availability_replica_states rs
					ON rs.group_id = ev.availability_group_id
				WHERE
					ev.rn = 1
					AND rs.role = 1
					AND NOT EXISTS(
									SELECT 1
									FROM Historico h
									WHERE
										h.AvailabilityGroupName = ev.availability_group_name
										AND h.AvailabilityReplicaName = ev.availability_replica_name
										AND h.EventTime = ev.event_timestamp
									)
					/*Para nao alertar a criacao do grupo*/
					AND CAST(ev.event_timestamp AS DATE) <> (SELECT CAST(create_date AS DATE)
																FROM
																	master.sys.availability_replicas ar
																WHERE
																	ar.group_id = ev.availability_group_id
																	AND ar.replica_id = ev.availability_replica_id
																);

					IF EXISTS(SELECT* FROM #TempQueueFailover)		
				BEGIN
						DECLARE
	
							@availability_replica_id VARCHAR(255),
						@availability_replica_name VARCHAR(255),
						@availability_group_id VARCHAR(255),
						@availability_group_name VARCHAR(255),
						@previous_state VARCHAR(255),
						@current_state VARCHAR(255),
						@event_timestamp DATETIME;

					DECLARE Cur_replica CURSOR
						FOR
					SELECT
						rs.availability_replica_id,
						rs.availability_replica_name,
						rs.availability_group_id,
						rs.availability_group_name,
						rs.previous_state,
						rs.current_state,
						rs.event_timestamp
					FROM
						#TempQueueFailover rs  
	
					OPEN Cur_replica;
					FETCH NEXT FROM Cur_replica INTO @availability_replica_id, @availability_replica_name, @availability_group_id,
													@availability_group_name, @previous_state, @current_state, @event_timestamp;

					WHILE @@FETCH_STATUS = 0
					BEGIN

						IF OBJECT_ID('tempdb..#TempAG') IS NOT NULL
							DROP TABLE #TempAG

						CREATE TABLE #TempAG (
								[Availability Group] VARCHAR(255),
								[Replica] VARCHAR(255),
								[Role] VARCHAR(255),
								[Availability Mode] VARCHAR(255),
								[Failover Mode] VARCHAR(255),
								[Availability Read] VARCHAR(255),
								[Listner] VARCHAR(255),
								[Data ultimo evento] VARCHAR(255)
						);

					INSERT INTO #TempAG
						SELECT
							ag.name AS 'GroupName'
							,cs.replica_server_name AS 'Replica'
							,rs.role_desc AS 'Role'
							,REPLACE(ar.availability_mode_desc, '_', ' ') AS 'AvailabilityMode'
							,ar.failover_mode_desc AS 'FailoverMode'
							,CASE rs.role
								WHEN 1 THEN
									IIF(ar.primary_role_allow_connections_desc = 'ALL', 'READ_WHITE', ar.primary_role_allow_connections_desc)
								WHEN 2 THEN
									IIF(ar.secondary_role_allow_connections_desc = 'ALL', 'READ_ONLY', ar.secondary_role_allow_connections_desc)
							END AS 'AvailabilityReadMode'
							,al.dns_name AS 'Listener'
							,@event_timestamp
						FROM
							sys.availability_groups ag
						INNER JOIN sys.dm_hadr_availability_group_states ags
							ON ag.group_id = ags.group_id
						INNER JOIN sys.dm_hadr_availability_replica_cluster_states cs
							ON ags.group_id = cs.group_id
						INNER JOIN sys.availability_replicas ar
							ON ar.replica_id = cs.replica_id
						INNER JOIN sys.dm_hadr_availability_replica_states rs
							ON rs.replica_id = cs.replica_id
						LEFT JOIN sys.availability_group_listeners al
							ON ar.group_id = al.group_id
						WHERE
							al.group_id = @availability_group_id
						ORDER BY
							rs.role,
							ar.failover_mode

						-- Gera Primeiro bloco de HTML
						SET @TextRel1 = '<BR>
											Availability Group
											<b>  '+@availability_group_name+' </b>
										</BR > '


						SET @Query = 'SELECT * FROM #TempAG ORDER BY [Role],[Failover Mode]'
						SET @HTML = @HTML + dbo.fncExportaMultiHTML(@Query, @TextRel1, 2, 1)



						INSERT INTO
							[dbo].[HistoricoAlwaysOnFailover] (AvailabilityGroupName, AvailabilityGroupID, AvailabilityReplicaName, AvailabilityReplicaID, PreviusState, CurrentState, EventTime)
						SELECT
							@availability_group_name,
							@availability_group_id,
							@availability_replica_name,
							@availability_replica_id,
							@previous_state,
							@current_state,
							@event_timestamp

						FETCH NEXT FROM Cur_replica INTO @availability_replica_id, @availability_replica_name, @availability_group_id,
											@availability_group_name, @previous_state, @current_state,@event_timestamp;

					END

					CLOSE Cur_replica
					DEALLOCATE Cur_replica;

					--Gera Segundo bloco de HTML
					SELECT @HTML = @HTML + @Ass
					-- Salva Arquivo HTML de Envio
					EXEC dbo.stpWriteFile
						@Ds_Texto = @HTML, --nvarchar(max)
						@Ds_Caminho = @CaminhoFim, --nvarchar(max)
						@Ds_Codificacao = N'UTF-8', --nvarchar(max)
						@Ds_Formato_Quebra_Linha = N'windows', --nvarchar(max)
						@Fl_Append = 0-- bit

				   /*******************************************************************************************************************************
					   ALERTA - ENVIA O EMAIL - ENVIA TELEGRAM
				   *******************************************************************************************************************************/
				   IF EXISTS(SELECT B.Ativo from AlertaParametro A

								 INNER JOIN[dbo].[AlertaEnvio] B ON B.IdAlertaParametro = A.Id_AlertaParametro

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
						DECLARE @TLF NVARCHAR(MAX)
						SET @TLF = @MntMsg
						-- Envio do Telegram
						EXEC dbo.StpSendMsgTelegram
										@Destino = @CanalTelegram,
										@Msg = @TLF
					END

					IF EXISTS(SELECT B.Ativo from AlertaParametro A

								  INNER JOIN[dbo].[AlertaEnvio] B ON B.IdAlertaParametro = A.Id_AlertaParametro
								WHERE B.Ativo = 1
									AND B.Des LIKE '%Teams'
									AND[Id_AlertaParametro] = @Id_AlertaParametro
									)
					BEGIN
						-- MS TEAMS
						SET @MntMsg = (select replace(@MntMsg, '\', ' - '))
						EXEC[dbo].[stpSendMsgTeams]
									@msg = @MntMsg,
									@channel = @Ds_Menssageiro_02,
									@ap = @Teams
					END

					/*Zabbix Sender*/
					IF EXISTS(SELECT B.Ativo from AlertaParametro A


					  INNER JOIN[dbo].[AlertaEnvio] B ON B.IdAlertaParametro = A.Id_AlertaParametro
								WHERE B.Ativo = 1
									AND B.Des LIKE '%Zabbix Sender'
									AND[Id_AlertaParametro] = @Id_AlertaParametro
								)
					BEGIN
						EXEC[dbo].[stpZabbixSender] @ZabbixPath, @ZabbixServer, @ZabbixLocalServer, @ZabbixAlertName, 1;
					END
				END
				ELSE
				BEGIN
					/*Zabbix Sender envia se nao tem ocorrencia*/
					IF EXISTS(SELECT B.Ativo from AlertaParametro A
								INNER JOIN [dbo].[AlertaEnvio] B ON B.IdAlertaParametro = A.Id_AlertaParametro
								WHERE B.Ativo = 1
									AND B.Des LIKE '%Zabbix Sender'
									AND[Id_AlertaParametro] = @Id_AlertaParametro
								)
					BEGIN
						EXEC[dbo].[stpZabbixSender] @ZabbixPath,@ZabbixServer,@ZabbixLocalServer,@ZabbixAlertName,0;
					END
				END

			END
			ELSE
			BEGIN
				/*Zabbix Sender envia se nao tem AG configurado*/
				IF EXISTS(SELECT B.Ativo from AlertaParametro A

						  INNER JOIN[dbo].[AlertaEnvio] B ON B.IdAlertaParametro = A.Id_AlertaParametro
							WHERE B.Ativo = 1
								AND B.Des LIKE '%Zabbix Sender'
								AND[Id_AlertaParametro] = @Id_AlertaParametro
							)
				BEGIN
					EXEC[dbo].[stpZabbixSender] @ZabbixPath,@ZabbixServer,@ZabbixLocalServer,@ZabbixAlertName,0;
					END
				END

			";

		}
    }
}
