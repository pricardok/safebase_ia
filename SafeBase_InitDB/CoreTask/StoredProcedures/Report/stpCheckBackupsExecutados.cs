using System;
using System.Collections.Generic;
using System.Text;

namespace InitDB.Client
{
    class stpCheckBackupsExecutados
    {
        public static string Query()
        {
            return
            // @"insert into [dbo].[Testedb] ([Nome],[DateTest]) values ('Teste da ferramenta DB - stpCheckBackupsExecutados',GETDATE())";

            @"SET NOCOUNT ON
                DECLARE @Dt_Referencia DATETIME
	            SELECT @Dt_Referencia = GETDATE()

	            TRUNCATE TABLE [dbo].[CheckBackupsExecutados]
	
	            INSERT INTO [dbo].[CheckBackupsExecutados] (	[Database_Name], [Name], [Backup_Start_Date], [Tempo_Min], [Position], [Server_Name],
														            [Recovery_Model], [Logical_Device_Name], [Device_Type], [Type], [Tamanho_MB] )
	            SELECT	[database_name], [name], [backup_start_date], DATEdiff(mi, [backup_start_date], [backup_finish_date]) AS [Tempo_Min], 
			            [position], [server_name], [recovery_model], isnull([logical_device_name], ' ') AS [logical_device_name],
			            [device_type], [type], CAST([backup_size]/1024/1024 AS NUMERIC(15,2)) AS [Tamanho (MB)]
	            FROM [msdb].[dbo].[backupset] B
		            JOIN [msdb].[dbo].[backupmediafamily] BF ON B.[media_set_id] = BF.[media_set_id]
	            WHERE [backup_start_date] >= DATEADD(hh, -24 ,@Dt_Referencia) AND [type] in ('D','I')
		  
	            IF (@@ROWCOUNT = 0)
	            BEGIN
		            INSERT INTO [dbo].[CheckBackupsExecutados] (	[Database_Name], [Name], [Backup_Start_Date], [Tempo_Min], [Position], [Server_Name],
															            [Recovery_Model], [Logical_Device_Name], [Device_Type], [Type], [Tamanho_MB] )
		            SELECT 'Sem registro de Backup FULL ou Diferencial nas últimas 24 horas.', NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL
	            END
	            ";

        }
    }
}
