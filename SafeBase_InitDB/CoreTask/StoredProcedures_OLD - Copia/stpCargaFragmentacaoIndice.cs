using System;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using Microsoft.SqlServer.Server;
public partial class StoredProcedures
{
    [Microsoft.SqlServer.Server.SqlProcedure]
    public static void stpCargaFragmentacaoIndice()
    {
        // Create the command
        SqlCommand myCommand = new SqlCommand();
        myCommand.CommandText =
              @"
                SET NOCOUNT ON
                IF object_id('tempdb..##HistoricoFragmentacaoIndice') IS NOT NULL DROP TABLE ##HistoricoFragmentacaoIndice
	
	            CREATE TABLE ##HistoricoFragmentacaoIndice(
		            [Id_Hitorico_Fragmentacao_Indice] [int] IDENTITY(1,1) NOT NULL,
		            [Dt_Referencia] [datetime] NULL,
		            [Nm_Servidor] VARCHAR(50) NULL,
		            [Nm_Database] VARCHAR(100) NULL,
		            [Nm_Tabela] VARCHAR(1000) NULL,
		            [Nm_Indice] [varchar](1000) NULL,
		            [Nm_Schema] varchar(50),
		            [Avg_Fragmentation_In_Percent] [numeric](5, 2) NULL,
		            [Page_Count] [int] NULL,
		            [Fill_Factor] [tinyint] NULL,
		            [Fl_Compressao] [tinyint] NULL
	            ) ON [PRIMARY]
 
	            EXEC sp_MSforeachdb 'Use [?]; 
	            declare @Id_Database int 
	            set @Id_Database = db_id()
	
	            insert into ##HistoricoFragmentacaoIndice
	            select	getdate(), @@servername Nm_Servidor,  DB_NAME(db_id()) Nm_Database, D.Name Nm_Tabela, B.Name Nm_Indice, 
			            F.name Nm_Schema, avg_fragmentation_in_percent, page_Count, fill_factor, data_compression	
	            from sys.dm_db_index_physical_stats(@Id_Database,null,null,null,null) A
		            join sys.indexes B on A.object_id = B.Object_id and A.index_id = B.index_id
                    JOIN sys.partitions C ON C.object_id = B.object_id AND C.index_id = B.index_id
                    JOIN sys.sysobjects D ON A.object_id = D.id
                    join sys.objects E on D.id = E.object_id
                    join  sys.schemas F on E.schema_id = F.schema_id
                '
          
                DELETE FROM ##HistoricoFragmentacaoIndice
                WHERE Nm_Database IN ('master', 'msdb', 'tempdb')
    
                INSERT INTO InitDB.dbo.Servidor(Nm_Servidor)
	            SELECT DISTINCT A.Nm_Servidor 
	            FROM ##HistoricoFragmentacaoIndice A
		            LEFT JOIN InitDB.dbo.Servidor B ON A.Nm_Servidor = B.Nm_Servidor
	            WHERE B.Nm_Servidor IS null
		
	            INSERT INTO InitDB.dbo.BaseDados(Nm_Database)
	            SELECT DISTINCT A.Nm_Database 
	            FROM ##HistoricoFragmentacaoIndice A
		            LEFT JOIN InitDB.dbo.BaseDados B ON A.Nm_Database = B.Nm_Database
	            WHERE B.Nm_Database IS null
	
	            INSERT INTO InitDB.dbo.Tabela(Nm_Tabela)
	            SELECT DISTINCT A.Nm_Tabela 
	            FROM ##HistoricoFragmentacaoIndice A
		            LEFT JOIN InitDB.dbo.Tabela B ON A.Nm_Tabela = B.Nm_Tabela
	            WHERE B.Nm_Tabela IS null	
	
                INSERT INTO InitDB..HistoricoFragmentacaoIndice(	Dt_Referencia, Id_Servidor, Id_BaseDados, Id_Tabela, Nm_Indice, Nm_Schema,
														            Avg_Fragmentation_In_Percent, Page_Count, Fill_Factor, Fl_Compressao)	
                SELECT	A.Dt_Referencia, E.Id_Servidor, D.Id_BaseDados, C.Id_Tabela, A.Nm_Indice, A.Nm_Schema,
			            A.Avg_Fragmentation_In_Percent, A.Page_Count, A.Fill_Factor, A.Fl_Compressao 
                FROM ##HistoricoFragmentacaoIndice A 
    	            JOIN InitDB.dbo.Tabela C ON A.Nm_Tabela = C.Nm_Tabela
		            JOIN InitDB.dbo.BaseDados D ON A.Nm_Database = D.Nm_Database
		            JOIN InitDB.dbo.Servidor E ON A.Nm_Servidor = E.Nm_Servidor 
    	            LEFT JOIN HistoricoFragmentacaoIndice B ON	E.Id_Servidor = B.Id_Servidor AND D.Id_BaseDados = B.Id_BaseDados  
    													            AND C.Id_Tabela = B.Id_Tabela AND A.Nm_Indice = B.Nm_Indice 
    													            AND CONVERT(VARCHAR, A.Dt_Referencia ,112) = CONVERT(VARCHAR, B.Dt_Referencia ,112)
	            WHERE A.Nm_Indice IS NOT NULL AND B.Id_Hitorico_Fragmentacao_Indice IS NULL
                ORDER BY 2, 3, 4, 5
	                        ";
        // Execute the command and send back the results
        SqlContext.Pipe.ExecuteAndSend(myCommand);
    }
};