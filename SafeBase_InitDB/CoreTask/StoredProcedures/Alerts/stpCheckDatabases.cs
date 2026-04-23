using System;
using System.Collections.Generic;
using System.Text;

namespace InitDB.Client
{
    class stpCheckDatabases
    {
        public static string Query()
        {
            string NocheckDataBase = ExecuteSql.ExecuteQuery("SELECT CASE WHEN IgnoraDatabase IS NULL THEN '''''' ELSE IgnoraDatabase END AS IgnoraDatabase FROM [dbo].[AlertaParametro] where Nm_Alerta = 'Check DB'");

            return
			//@"insert into [dbo].[Testedb] ([Nome],[DateTest]) values ('Teste da ferramenta DB - stpCheckDatabases',GETDATE())";
			@"
                SET NOCOUNT ON;
				
				SET QUOTED_IDENTIFIER ON;

                IF exists (SELECT Id_AlertaParametro FROM [dbo].AlertaParametro (NOLOCK) WHERE Nm_Alerta = 'Check DB' AND Ativo = 1)
                    BEGIN

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
	                    WHERE	[name] NOT IN (" + NocheckDataBase + @")  -- Colocar o nome da Database aqui, caso deseje desconsiderar alguma
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

		                    DBCC CHECKDB (@Nm_Database) WITH NO_INFOMSGS 
		                    SET @Loop = @Loop + 1
	                    END
                    END

             ELSE 
	            PRINT 'DESABILITADO'

                ";
        }
    }
}
