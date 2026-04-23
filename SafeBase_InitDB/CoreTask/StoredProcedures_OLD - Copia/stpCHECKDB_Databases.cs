using System;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using Microsoft.SqlServer.Server;
public partial class StoredProcedures
{
    [Microsoft.SqlServer.Server.SqlProcedure]
    public static void stpCHECKDB_Databases()
    {
        // Create the command
        SqlCommand myCommand = new SqlCommand();
        myCommand.CommandText =
              @"
                SET NOCOUNT ON
                
	            -- Declara a tabela que irá armazenar o nome das Databases
	            DECLARE @Databases TABLE ( 
		            [Id_Database] INT IDENTITY(1, 1), 
		            [Nm_Database] VARCHAR(50)
	            )

	            -- Declara as variaveis
	            DECLARE @Total INT, @Loop INT, @Nm_Database VARCHAR(50)
	
	            -- Busca o nome das Databases
	            INSERT INTO @Databases( [Nm_Database] )
	            SELECT [name]
	            FROM [master].[sys].[databases]
	            WHERE	[name] NOT IN ('tempdb')  -- Colocar o nome da Database aqui, caso deseje desconsiderar alguma
			            AND [state_desc] = 'ONLINE'

	            -- Quantidade Total de Databases (utilizado no Loop abaixo)
	            SELECT @Total = MAX([Id_Database])
	            FROM @Databases

	            SET @Loop = 1

	            -- Realiza o CHECKDB para cada Database
	            WHILE ( @Loop <= @Total )
	            BEGIN
		            SELECT @Nm_Database = [Nm_Database]
		            FROM @Databases
		            WHERE [Id_Database] = @Loop

		            DBCC CHECKDB(@Nm_Database) WITH NO_INFOMSGS 
		            SET @Loop = @Loop + 1
	            END
                ";
        // Execute the command and send back the results
        SqlContext.Pipe.ExecuteAndSend(myCommand);
    }
};