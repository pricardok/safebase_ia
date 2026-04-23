using System;
using System.Collections.Generic;
using System.Text;

namespace InitDB.Client
{
    class stpCheckEspacoDisco
    {
        public static string Query()
        {
            return
            //@"insert into [dbo].[Testedb] ([Nome],[DateTest]) values ('Teste da ferramenta DB - stpCheckEspacoDisco',GETDATE())";
            @"
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
	
	                INSERT INTO #space EXEC 
					[master].[dbo].[xp_fixeddrives]
	
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
							
				TRUNCATE TABLE[dbo].[CheckEspacoDisco]

                INSERT INTO [dbo].[CheckEspacoDisco] ([Drive],[Size (MB)],[Used (MB)],[Free (MB)],[Used (%)],[Free (%)],[Used by SQL (MB)])
                SELECT	[Drive], 
		                CAST([Size (MB)] AS VARCHAR) AS [Size (MB)], 
		                CAST([Used (MB)] AS VARCHAR) AS [Used (MB)], 
		                CAST([Free (MB)] AS VARCHAR) AS [Free (MB)], 
		                CAST([Used (%)] AS VARCHAR) AS [Used (%)], 
		                CAST([Free (%)] AS VARCHAR) AS [Free (%)], 
		                CAST([Used by SQL (MB)] AS VARCHAR) AS [Used by SQL (MB)]
                FROM #DiskSpace

	            IF(@@ROWCOUNT = 0)

                BEGIN
				   INSERT INTO [dbo].[CheckEspacoDisco] ([Drive],[Size (MB)],[Used (MB)],[Free (MB)],[Used (%)],[Free (%)],[Used by SQL (MB)])
                   SELECT 'Sem registro de Espaço em Disco', NULL, NULL, NULL, NULL,NULL,NULL
                END
                ";


        }
    }
}
