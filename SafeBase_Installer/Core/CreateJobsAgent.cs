using System;
using System.Collections.Generic;
using System.Text;
using SafeBase_Installer.Core;

namespace SafeBase_Installer
{
    class CreateJobsAgent
    {
        public static string Query(string use)
        {
            return
            @"

            USE [" + use + @"]
            GO

            IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'stpAddJobQuick')
            DROP PROCEDURE dbo.stpAddJobQuick
            GO

            CREATE procedure [dbo].[stpAddJobQuick] 
                @job nvarchar(128),
			    @category nvarchar(128),
                @owner_name nvarchar(128),
				@name_step nvarchar(128),
                @mycommand nvarchar(max),
				@al_freq_type nvarchar(15), -- 4 = Diariamente 
				@al_freq_interval nvarchar(15), -- Ocorre a cada 1 dia na frequencia 
				@al_freq_subday_type nvarchar(15), -- Ocorre a cada hora, minuto e segundo na frequencia diaria
				@al_freq_subday_interval nvarchar(15), -- habilita horario de intervalo na frequencia diaria
				@al_freq_relative_interval nvarchar(15), 
				@al_freq_recurrence_factor nvarchar(15),
				@al_active_start_date nvarchar(15), -- ok
				@al_active_end_date nvarchar(15), -- ok
				@al_active_start_time nvarchar(15), -- ok
				@al_active_end_time  nvarchar(15), -- ok
                @servername nvarchar(28) -- ok
            WITH ENCRYPTION
            as
            BEGIN 
            --Add a job
            EXEC msdb.dbo.sp_add_job
                @job_name = @job,
				@category_name = @category,
	            @owner_login_name = @owner_name;
            --Add a job step named process step. This step runs the stored procedure
            EXEC msdb.dbo.sp_add_jobstep
                @job_name = @job,
                @step_name = @name_step,
                @subsystem = N'TSQL',
                @command = @mycommand,
	            @database_name=N'" + use + @"'
            --Schedule the job at a specified date and time
            exec msdb.dbo.sp_add_jobschedule @job_name = @job,
	            @name = 'schedule',
	            @freq_type = @al_freq_type, -- 4 = Diariamente 
	            @freq_interval = @al_freq_interval, -- Ocorre a cada 1 dia na frequencia 
	            @freq_subday_type = @al_freq_subday_type, -- Ocorre a cada hora, minuto e segundo na frequencia diaria 
	            @freq_subday_interval = @al_freq_subday_interval, -- habilita horario de intervalo na frequencia diaria 
	            @freq_relative_interval = @al_freq_relative_interval, 
	            @freq_recurrence_factor = @al_freq_recurrence_factor, 
	            @active_start_date = @al_active_start_date, -- Data de Inicio
	            @active_end_date =@al_active_end_date, 
	            @active_start_time = @al_active_start_time, -- hora de Inicio 
	            @active_end_time = @al_active_end_time
            -- Add the job to the SQL Server Server
            EXEC msdb.dbo.sp_add_jobserver
                @job_name =  @job,
                @server_name = @servername
            END
            GO

            -- 
            IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'stpAddJobQuickMulti')
            DROP PROCEDURE dbo.stpAddJobQuickMulti
            GO

            CREATE procedure [dbo].[stpAddJobQuickMulti] 
                @job nvarchar(128),
			    @category nvarchar(128),
                @owner_name nvarchar(128),
				@name_step_01 nvarchar(128),
				@name_step_02 nvarchar(128),
                @mycommand_01 nvarchar(max),
				@mycommand_02 nvarchar(max),
				@al_freq_type nvarchar(15), -- 4 = Diariamente 
				@al_freq_interval nvarchar(15), -- Ocorre a cada 1 dia na frequencia 
				@al_freq_subday_type nvarchar(15), -- Ocorre a cada hora, minuto e segundo na frequencia diaria
				@al_freq_subday_interval nvarchar(15), -- habilita horario de intervalo na frequencia diaria
				@al_freq_relative_interval nvarchar(15), 
				@al_freq_recurrence_factor nvarchar(15),
				@al_active_start_date nvarchar(15), -- ok
				@al_active_end_date nvarchar(15), -- ok
				@al_active_start_time nvarchar(15), -- ok
				@al_active_end_time  nvarchar(15), -- ok
                @servername nvarchar(28) -- ok
            WITH ENCRYPTION
            as
            BEGIN 
            --Add a job
            EXEC msdb.dbo.sp_add_job
                @job_name = @job,
				@category_name = @category,
	            @owner_login_name = @owner_name;
            --01 Add a job step named process step. This step runs the stored procedure
            EXEC msdb.dbo.sp_add_jobstep
                @job_name = @job,
                @step_name = @name_step_01,
				@step_id=1, 
				@cmdexec_success_code=0, 
				@on_success_action=3, 
				@on_success_step_id=0, 
				@on_fail_action=2, 
				@on_fail_step_id=0, 
				@retry_attempts=0, 
				@retry_interval=0,
                @subsystem = N'TSQL',
                @command = @mycommand_01,
	            @database_name=N'" + use + @"'
			-- 02 Add a job step named process step. This step runs the stored procedure
            EXEC msdb.dbo.sp_add_jobstep
                @job_name = @job,
                @step_name = @name_step_02,
				@step_id=2, 
				@cmdexec_success_code=0, 
				@on_success_action=1, 
				@on_success_step_id=0, 
				@on_fail_action=2, 
				@on_fail_step_id=0, 
				@retry_attempts=0, 
				@retry_interval=0, 
                @subsystem = N'TSQL',
                @command = @mycommand_02,
	            @database_name=N'" + use + @"'
            --Schedule the job at a specified date and time
            exec msdb.dbo.sp_add_jobschedule @job_name = @job,
	            @name = 'schedule',
	            @freq_type = @al_freq_type, -- 4 = Diariamente 
	            @freq_interval = @al_freq_interval, -- Ocorre a cada 1 dia na frequencia 
	            @freq_subday_type = @al_freq_subday_type, -- Ocorre a cada hora, minuto e segundo na frequencia diaria 
	            @freq_subday_interval = @al_freq_subday_interval, -- habilita horario de intervalo na frequencia diaria 
	            @freq_relative_interval = @al_freq_relative_interval, 
	            @freq_recurrence_factor = @al_freq_recurrence_factor, 
	            @active_start_date = @al_active_start_date, -- Data de Inicio
	            @active_end_date =@al_active_end_date, 
	            @active_start_time = @al_active_start_time, -- hora de Inicio 
	            @active_end_time = @al_active_end_time
            -- Add the job to the SQL Server Server
            EXEC msdb.dbo.sp_add_jobserver
                @job_name =  @job,
                @server_name = @servername
            END
			GO
 
            ";

        }
    }
}
