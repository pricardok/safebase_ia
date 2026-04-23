using System;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using Microsoft.SqlServer.Server;
public partial class StoredProcedures
{
    [Microsoft.SqlServer.Server.SqlProcedure]
    public static void stpCheckEspacoDisco()
    {
        // Create the command
        SqlCommand myCommand = new SqlCommand();
        myCommand.CommandText =
              @"
                SET NOCOUNT ON
                
	            CREATE TABLE #dbspace (
		            [Name]		SYSNAME,
		            [Caminho]	VARCHAR(200),
		            [Tamanho]	VARCHAR(10),
		            [Drive]		VARCHAR(30)
	            )

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

	            EXEC sp_MSforeachdb '	Use [?] 
							            INSERT INTO #dbspace 
							            SELECT	CONVERT(VARCHAR(25), DB_NAME())''Database'', CONVERT(VARCHAR(60), FileName),
									            CONVERT(VARCHAR(8), Size/128) ''Size in MB'', CONVERT(VARCHAR(30), Name) 
							            FROM [sysfiles]'

	            DECLARE @hr INT, @fso INT, @size FLOAT, @TotalSpace INT, @MBFree INT, @Percentage INT, 
			            @SQLDriveSize INT, @drive VARCHAR(1), @fso_Method VARCHAR(255), @mbtotal INT = 0	
	
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
		            SET @fso_Method = 'Drives(' + @drive + ':).TotalSize'


                    SELECT @SQLDriveSize = SUM(CONVERT(INT, Tamanho))

                    FROM #dbspace 
		            WHERE SUBSTRING(Caminho, 1, 1) = @drive


                    EXEC @hr = sp_OAMethod @fso, @fso_Method, @size OUTPUT


                    SET @mbtotal = @size / (1024 * 1024)


                    INSERT INTO #espacodisco 
		            VALUES(@drive + ':', @mbtotal, @mbtotal - @MBFree, @MBFree, (100 * round(@MBFree, 2) / round(@mbtotal, 2)),
                            (100 - 100 * round(@MBFree, 2) / round(@mbtotal, 2)), @SQLDriveSize, GETDATE())


                    FETCH NEXT FROM CheckDrives INTO @drive, @MBFree

                END
                CLOSE CheckDrives
                DEALLOCATE CheckDrives

                TRUNCATE TABLE[dbo].[CheckEspacoDisco]


                INSERT INTO[dbo].[CheckEspacoDisco]
                ( [DriveName], [TotalSize_GB], [FreeSpace_GB], [SpaceUsed_GB], [SpaceUsed_Percent])
               SELECT[Drive], [TamanhoMB], [LivreMB], [UsadoMB], [UsadoPerc]

                FROM #espacodisco

	            IF(@@ROWCOUNT = 0)

                BEGIN
                    INSERT INTO[dbo].[CheckEspacoDisco]
                ( [DriveName], [TotalSize_GB], [FreeSpace_GB], [SpaceUsed_GB], [SpaceUsed_Percent])
                   SELECT 'Sem registro de Espaço em Disco', NULL, NULL, NULL, NULL

                END
                ";
        // Execute the command and send back the results
        SqlContext.Pipe.ExecuteAndSend(myCommand);
    }
};