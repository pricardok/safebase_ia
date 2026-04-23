using System.Data;
using System.Data.SqlClient;
using System;
using System.Data.SqlTypes;
using System.Collections.Generic;
using System.Text;
using External.Client;
using System.Xml;
using System.IO;
using System.Diagnostics;
using Microsoft.SqlServer.Server;
using System.Net;

public partial class StoredProcedures
{
    [Microsoft.SqlServer.Server.SqlProcedure]

	public static void stpJobSchedule(int Force_freq)
	{

		string scriptLine;
		string OperationUID = Guid.NewGuid().ToString();

		scriptLine = @"

        SET NOCOUNT ON

		DECLARE @Force_freq INT = " + Force_freq + @"

		/*Variaveis globais*/
		DECLARE 
			@DiaExecIni datetime = GETDATE(),	
			@DiaIni tinyint = DATEPART(WEEKDAY, GETDATE()),	
			@TimeExecIni datetime,
			@TimeExecFim datetime,
			@SegundosExec int,
			@GravaHist bit,
			@Error bit,
			@ErrorMessage nvarchar(4000);

		/*Dados Job*/
		DECLARE 
			@JobId int,	
			@Nome varchar(20), 
			@Descricao varchar(50), 
			@Solicitante varchar(30), 
			@Frequencia tinyint, 
			@DiaUtil bit, 
			@ExecIntervalo bit, 
			@Comando nvarchar(max), 
			@DataInicio date,
			@DataFim date,	
			@UltimaExec datetime,
			@HoraIni varchar(6),
			@HoraFIm varchar(6);

		/*Dados Schedule*/
		DECLARE 
			@JobIdSchedule int,
			@DiaSemana tinyint,
			@DataExec date,
			@HoraExec int,
			@MensalExec tinyint,
			@Intervalo int,
			@DiaMensal INT ;



			DECLARE cur_queue_jobs CURSOR
				FOR
			SELECT IdJob, Nome, Descricao, Solicitante, Frequencia, DiaUtil, ExecIntervalo, Comando, DataInicio, DataFim, HoraIni, HoraFIm, UltimaExec 
			FROM 
				[SafeBase].[job].[Job]
			WHERE 
				[Ativo] = 1
				AND DataInicio <= CAST(GETDATE() AS DATE)
				AND DataFim >= CAST(GETDATE() AS DATE);

			OPEN cur_queue_jobs
			FETCH NEXT FROM cur_queue_jobs INTO @JobId, @Nome, @Descricao, @Solicitante, @Frequencia, @DiaUtil, @ExecIntervalo, @Comando, 
												@DataInicio, @DataFim, @HoraIni, @HoraFIm, @UltimaExec;


			WHILE @@FETCH_STATUS = 0
			BEGIN	
					/*Limpa variaveis Job*/
					SET @GravaHist		= 0;
					SET @Error			= 0;
					SET @ErrorMessage	=NULL;
			
					/*Limpa variaveis JobSchedule*/
					SET @JobIdSchedule	=NULL;
					SET @DiaSemana		=NULL;
					SET @DataExec		=NULL;
					SET @HoraExec		=NULL;
					SET @MensalExec		=NULL;
					SET @Intervalo		=NULL;
					SET @DiaMensal		=NULL;


				/*Execucao unica em data escolhida*/	
				IF ((@Frequencia = 1 AND @Force_freq =0) OR (@Frequencia = 1 AND @Force_freq =1))
				BEGIN							

					/*Necessario para atualizar o GETDATE()*/
					WAITFOR DELAY '00:00:00.010'

					SELECT TOP(1)
						@JobIdSchedule	= JobId,
						@DiaSemana		= DiaSemana,
						@DataExec		= DataExec,
						@HoraExec		= HoraExec ,
						@MensalExec		= MensalExec,
						@Intervalo		= Intervalo
					FROM
						[SafeBase].[job].[JobAgendamento]
					WHERE
						JobId = @JobId				
						AND DataExec <> ISNULL(CAST(@UltimaExec AS DATE),'2099-12-31')  --Garantia de execucao somente uma vez
						AND DataExec = CAST(GETDATE() AS DATE)							--Data 
						AND CAST(HoraExec AS INT) <= CAST(REPLACE(CAST(GETDATE() AS TIME(0)),':','' ) AS INT)	--Hora em formato hhmmss
			
					WAITFOR DELAY '00:00:00.010'

					SET @TimeExecIni = GETDATE();	

					/*================= Execucao do comando ================*/
					IF @JobIdSchedule IS NOT NULL	
					BEGIN			
						BEGIN TRY
								SET @GravaHist = 1;
								EXEC sp_executesql @Comando
								SET @ErrorMessage = 'Sucesso';	
						END TRY
						BEGIN CATCH
							SET @GravaHist = 1;
							SET @Error = 1;
							SET @ErrorMessage = ERROR_MESSAGE();
						END CATCH							
					END		
						
					WAITFOR DELAY '00:00:00.010'
					
					SET @TimeExecFim = GETDATE();

					/*Calculo de tempo de execucao do comando*/
					SET @SegundosExec = ((DATEDIFF(MILLISECOND, @TimeExecIni, @TimeExecFim))/1000)
				
					/*============== Inclusao historico ============*/
					IF @GravaHist = 1
					BEGIN
						BEGIN TRY				
							EXEC [SafeBase].[dbo].[stpJobGravaHistorico] @JobId, @TimeExecIni, @TimeExecFim, @SegundosExec, @Error, @ErrorMessage 
						END TRY
						BEGIN CATCH
							print error_message()
						END CATCH
					END			
				END
				/*Execucao diaria em intevalo de tempo ou hora marcada*/
				ELSE IF ((@Frequencia = 2 AND @Force_freq =0) OR (@Frequencia = 2 AND @Force_freq = 2))
				BEGIN
				
					WAITFOR DELAY '00:00:00.010'

					SELECT TOP(1)
						@JobIdSchedule	= JobId,
						@DiaSemana		= DiaSemana,
						@DataExec		= DataExec,
						@HoraExec		= HoraExec ,
						@MensalExec		= MensalExec ,
						@Intervalo		= Intervalo
					FROM
						[SafeBase].[job].[JobAgendamento]
					WHERE
						JobId = @JobId								
						AND 
						(
							(
								@ExecIntervalo = 0  
								AND CAST(HoraExec AS INT) <= CAST(REPLACE(CAST(GETDATE() AS TIME(0)),':','' ) AS INT)	--Hora atual maior igual hora marcada											
								AND CAST(ISNULL(@UltimaExec,(GETDATE()-1)) AS DATE) <> CAST(GETDATE() AS DATE)	-- Verifica se já rodou hj
							) --Somente hora marcada
						OR

							(
								@ExecIntervalo = 1 
								AND Intervalo <= DATEDIFF(MINUTE,ISNULL(@UltimaExec,(GETDATE()-1)),GETDATE())							--Dif minutos no Intervalo da ultima execucao
								AND CAST(REPLACE(CAST(GETDATE() AS TIME(0)),':','' ) AS INT)	BETWEEN CAST(@HoraIni AS INT) AND CAST(@HoraFIm AS INT)							--Verifica horario limite para execucao
							) --Intervalo em minutos
						) 

					WAITFOR DELAY '00:00:00.010'
					
					SET @TimeExecIni = GETDATE();

					IF @JobIdSchedule IS NOT NULL 
					BEGIN
						/*================= Execucao do comando ================*/
						BEGIN TRY
							SET @GravaHist = 1;
							EXEC sp_executesql @Comando
							SET @ErrorMessage = 'Sucesso';	
						END TRY
						BEGIN CATCH
							SET @GravaHist = 1;
							SET @Error = 1;
							SET @ErrorMessage = ERROR_MESSAGE();
						END CATCH			
					END										
							
					WAITFOR DELAY '00:00:00.010'
						
					SET @TimeExecFim = GETDATE();
		
					SET @SegundosExec = ((DATEDIFF(MILLISECOND, @TimeExecIni, @TimeExecFim))/1000)

					/*============== Inclusao histórico ============*/			
					IF @GravaHist = 1
					BEGIN
						BEGIN TRY				
							EXEC [SafeBase].[dbo].[stpJobGravaHistorico] @JobId, @TimeExecIni, @TimeExecFim, @SegundosExec, @Error, @ErrorMessage 
						END TRY
						BEGIN CATCH
							print error_message()
						END CATCH
					END											
				END
				/*Semanal escolhendo os dias*/
				ELSE IF ((@Frequencia = 3 AND @Force_freq =0) OR (@Frequencia = 3 AND @Force_freq = 3))
				BEGIN			
			
					WAITFOR DELAY '00:00:00.010'

					SELECT TOP(1)						
						@JobIdSchedule	= JobId,
						@DiaSemana		= DiaSemana,
						@DataExec		= DataExec,
						@HoraExec		= HoraExec ,
						@MensalExec		= MensalExec ,
						@Intervalo		= Intervalo
					FROM
						[SafeBase].[job].[JobAgendamento]
					WHERE
						JobId = @JobId						
						AND 
						(
							(
								@ExecIntervalo = 1  
								AND Intervalo <= DATEDIFF(MINUTE,ISNULL(@UltimaExec,GETDATE()-1),GETDATE())								--Dif minutos ultima execucao
								AND CAST(REPLACE(CAST(GETDATE() AS TIME(0)),':','' ) AS INT)	BETWEEN CAST(@HoraIni AS INT) AND CAST(@HoraFIm	AS INT)	--Verifica horario limite para execucao
							)
								OR
							(						
								@ExecIntervalo = 0 
								AND 
								(
								ISNULL(CAST(@UltimaExec AS DATE),'2099-12-31') <> CAST(GETDATE() AS DATE)	
									OR
								ISNULL(CAST(@UltimaExec AS DATE),'2099-12-31') <> CAST(@DiaExecIni AS DATE)	
								) --Verifica se já rodou hoje
								AND CAST(HoraExec AS INT) <= CAST(REPLACE(CAST(GETDATE() AS TIME(0)),':','' ) AS INT)								--Hora marcada menor igual					
							)								
					
						)
						AND 
						(
							(
								@DiaUtil = 1 
								AND @DiaIni IN (2,3,4,5,6)
							) -- Somente em dias 
								OR
							(
								DiaSemana = @DiaIni
							) -- Marcado os dias da semana
						);
			
					WAITFOR DELAY '00:00:00.010'
					SET @TimeExecIni = GETDATE();

					IF @JobIdSchedule IS NOT NULL 
					BEGIN
						/*================= Execucao do comando ================*/								
						BEGIN TRY
							SET @GravaHist = 1;
							EXEC sp_executesql @Comando
							SET @ErrorMessage = 'Sucesso';	
						END TRY
						BEGIN CATCH
							SET @GravaHist = 1;
							SET @Error = 1;
							SET @ErrorMessage = ERROR_MESSAGE();
						END CATCH													
					END										
				
					WAITFOR DELAY '00:00:00.010'
			
					SET @TimeExecFim = GETDATE();

					/*Calculo tempo da execucao*/
					SET @SegundosExec = ((DATEDIFF(MILLISECOND, @TimeExecIni, @TimeExecFim))/1000)

					/*============== Inclusao histórico ============*/
					IF @GravaHist = 1
					BEGIN
						BEGIN TRY				
							EXEC [SafeBase].[dbo].[stpJobGravaHistorico] @JobId, @TimeExecIni, @TimeExecFim, @SegundosExec, @Error, @ErrorMessage 
						END TRY
						BEGIN CATCH
							print error_message()
						END CATCH
					END			
				END
				/*Mensal*/		
				ELSE IF ((@Frequencia = 4 AND @Force_freq =0) OR (@Frequencia = 4 AND @Force_freq = 4))
				BEGIN					

					WAITFOR DELAY '00:00:00.010'		

					SELECT TOP(1)						
						@JobIdSchedule	= JobId,
						@DiaSemana		= DiaSemana,
						@DataExec		= DataExec,
						@HoraExec		= HoraExec ,
						@MensalExec		= MensalExec ,
						@Intervalo		= Intervalo
					FROM
						[SafeBase].[job].[JobAgendamento]
					WHERE
						JobId = @JobId
						AND ISNULL(CAST(@UltimaExec AS DATE),'2099-12-31') <> CAST(GETDATE() AS DATE)
						AND CAST(HoraExec AS INT) <= CAST(REPLACE(CAST(GETDATE() AS TIME(0)),':','' ) AS INT)
						AND (
								(MensalExec = 1 AND (
														DAY(GETDATE()) = 1 
															OR
														DAY(@DiaExecIni) = 1

													)
								)
									OR								
								(MensalExec = 2 AND (
														DAY(GETDATE()) = 5 													
															OR
														DAY(@DiaExecIni) = 5

														)
								)
									OR
								(MensalExec = 3 AND (
														DAY(GETDATE()) = 10 
															OR
														DAY(@DiaExecIni) = 10
													)
								)
									OR
								(MensalExec = 4 AND (
														DAY(GETDATE()) = 15 
															OR
														DAY(@DiaExecIni) = 15
													)
								)
									OR
								(MensalExec = 5 AND (
														DAY(GETDATE())=  20 
															OR
														DAY(@DiaExecIni) = 20
													)
								)
									OR
								(MensalExec = 6 AND (
														DAY(GETDATE()) = 25 
															OR
														DAY(@DiaExecIni) = 25
													)
								)						
									OR
								(MensalExec = 7 AND (
														DAY(GETDATE()) = DAY(EOMONTH(GETDATE()))
																OR
														DAY(@DiaExecIni) = DAY(EOMONTH(GETDATE()))
													)
								)
								--Execução com dia do mês a escolha
									OR
								(MensalExec = 8 AND (
													DAY(GETDATE()) = (SELECT DiaMensal FROM [SafeBase].[job].[JobAgendamento] where JobId = @JobId)
														OR
													DAY(@DiaExecIni) = (SELECT DiaMensal FROM [SafeBase].[job].[JobAgendamento] where JobId = @JobId)
													)
								)
							)
			
					WAITFOR DELAY '00:00:00.010'

					SET @TimeExecIni = GETDATE();

					IF @JobIdSchedule IS NOT NULL 
					BEGIN
						/*================= Execucao do comando ================*/				
							BEGIN TRY
								SET @GravaHist = 1;
								EXEC sp_executesql @Comando
								SET @ErrorMessage = 'Sucesso';	
							END TRY
							BEGIN CATCH
								SET @GravaHist = 1;
								SET @Error = 1;
								SET @ErrorMessage = ERROR_MESSAGE();
							END CATCH						
					END										
				
					WAITFOR DELAY '00:00:00.010'
						
					SET @TimeExecFim = GETDATE();

					/*Calculo tempo de execucao*/
					SET @SegundosExec = ((DATEDIFF(MILLISECOND, @TimeExecIni, @TimeExecFim))/1000)

					/*============== Inclusao histórico ============*/
					IF @GravaHist = 1
					BEGIN
						BEGIN TRY				
							EXEC [SafeBase].[dbo].[stpJobGravaHistorico] @JobId, @TimeExecIni, @TimeExecFim, @SegundosExec, @Error, @ErrorMessage 
						END TRY
						BEGIN CATCH							
							print error_message()
						END CATCH
					END			
				END

	
				FETCH NEXT FROM cur_queue_jobs INTO @JobId, @Nome, @Descricao, @Solicitante, @Frequencia, @DiaUtil, @ExecIntervalo, @Comando, 
													@DataInicio, @DataFim, @HoraIni, @HoraFIm, @UltimaExec;
			END


			CLOSE cur_queue_jobs
			DEALLOCATE cur_queue_jobs
			";

		ExecuteSql.NonQuery(OperationUID, scriptLine, false, false, false);

	}


	[Microsoft.SqlServer.Server.SqlProcedure]
	public static void stpJobGravaHistorico(int JobId, DateTime TimeExecIni, DateTime TimeExecFim,
					 int SegundosExec, int Error, SqlString ErrorMessage)
	{


		string scriptLine;
		string OperationUID = Guid.NewGuid().ToString();


		using (SqlConnection connection = new SqlConnection("context connection=true"))
		{
			using (var comando = new SqlCommand())
			{
				comando.Connection = connection;
				comando.CommandText = "INSERT INTO [SafeBase].[job].[JobHistorico] (JobId,DataExec,TempoExec,Error,MessageError) " +
					"VALUES  (@JobId, @TimeExecIni, @SegundosExec,@Error,@ErrorMessage)";

				comando.Parameters.Add("@JobId", SqlDbType.Int).Value = JobId;
				comando.Parameters.Add("@TimeExecIni", SqlDbType.DateTime).Value = TimeExecIni;
				comando.Parameters.Add("@SegundosExec", SqlDbType.Int).Value = SegundosExec;
				comando.Parameters.Add("@Error", SqlDbType.Int).Value = Error;
				comando.Parameters.Add("@ErrorMessage", SqlDbType.NVarChar).Value = ErrorMessage.ToString();

				connection.Open();
				comando.ExecuteNonQuery();
				connection.Close();
			}

			scriptLine = "UPDATE [SafeBase].[job].[Job] SET UltimaExec = '" + TimeExecFim + @"'
						WHERE
							IdJob = '" + JobId + @"'";

			ExecuteSql.NonQuery(OperationUID, scriptLine, false, false, true);
		}
	}


	[Microsoft.SqlServer.Server.SqlProcedure]
	public static void stpJobExecuteSSIS(string package_name_INPUT, string folder_name_INPUT, string project_name_INPUT)
	{

		string OperationUID = Guid.NewGuid().ToString();
		string scriptLine;

		scriptLine = @"

        SET NOCOUNT ON

		DECLARE @ErrorMessage  NVARCHAR(MAX) 
		DECLARE @execution_id bigint  
		DECLARE @var0 smallint = 1   
        BEGIN TRY    
		    EXEC [SSISDB].[catalog].[create_execution]	@package_name= '" + package_name_INPUT + @"',     --Parametro de entrada
													    @execution_id=@execution_id OUTPUT,      
													    @folder_name= '" + folder_name_INPUT + @"',		--Parametro de entrada
													    @project_name= '" + project_name_INPUT + @"',      --Parametro de entrada
													    @use32bitruntime=False,      
													    @reference_id=Null 
	
		    SELECT @execution_id   
	
		    EXEC [SSISDB].[catalog].[set_execution_parameter_value] @execution_id,      
																    @object_type=50,      
																    @parameter_name=N'LOGGING_LEVEL',      
																    @parameter_value=@var0  
		    EXEC [SSISDB].[catalog].[start_execution] @execution_id

		    /*criado (1), em execução (2), cancelado (3), com falha (4), pendente (5), encerrado inesperadamente (6), êxito (7), parando (8) e concluído (9)*/

		    WHILE EXISTS(SELECT package_name,status FROM SSISDB.catalog.executions WHERE execution_id = @execution_id AND [status] NOT IN (3, 4, 6, 7, 9))
		    BEGIN
			    WAITFOR DELAY '00:00:01';
		    END
        END TRY
        BEGIN CATCH
            SET @ErrorMessage = error_message()
            RAISERROR (@ErrorMessage, -- Message text.  
						    16, -- Severity,  
						    1-- State,  
						    ); 
        END CATCH

		IF EXISTS(SELECT status FROM SSISDB.catalog.executions WHERE execution_id = @execution_id AND [status] IN (3, 4, 6))
		BEGIN
			SELECT        
			@ErrorMessage = 
				STUFF((	SELECT  
							'; '+ msg.[message]
						FROM
							[SSISDB].[catalog].[event_messages] msg LEFT JOIN [SSISDB].[catalog].[extended_operation_info] info ON msg.extended_info_id = info.info_id
						WHERE
							msg.[operation_id] = @execution_id
							AND msg.[message_type] = 120
						FOR XML PATH('')
					),1,1,'') 

			SET @ErrorMessage = ISNULL(SUBSTRING(@ErrorMessage,1,4000),'Mais informações no report de execução no SSIS')
		
			RAISERROR (@ErrorMessage, -- Message text.  
						16, -- Severity,  
						1-- State,  
						); 
		END  
	";

		ExecuteSql.NonQuery(OperationUID, scriptLine, false, false, false);

	}

}
