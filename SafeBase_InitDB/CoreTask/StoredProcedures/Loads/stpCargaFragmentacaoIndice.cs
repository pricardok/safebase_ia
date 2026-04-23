using System;
using System.Collections.Generic;
using System.Text;

namespace InitDB.Client
{
    class stpCargaFragmentacaoIndice
    {
        public static string Query()
        {
            string NocheckDataBase = ExecuteSql.ExecuteQuery("SELECT CASE WHEN IgnoraDatabase IS NULL THEN '''''' ELSE IgnoraDatabase END AS IgnoraDatabase FROM [dbo].[AlertaParametro] where Nm_Alerta = 'Fragmentacao Indice'");

            return
            // @"insert into [dbo].[Testedb] ([Nome],[DateTest]) values ('Teste da ferramenta DB - stpCargaFragmentacaoIndice',GETDATE())";
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
                -- WHERE Nm_Database IN (" + NocheckDataBase + @")
				WHERE Nm_Database IN ('master', 'msdb', 'tempdb')
    
                INSERT INTO dbo.Servidor(Nm_Servidor)
	            SELECT DISTINCT A.Nm_Servidor 
	            FROM ##HistoricoFragmentacaoIndice A
		            LEFT JOIN dbo.Servidor B ON A.Nm_Servidor COLLATE SQL_Latin1_General_CP1_CI_AI = B.Nm_Servidor
	            WHERE B.Nm_Servidor IS null
		
	            INSERT INTO dbo.BaseDados(Nm_Database)
	            SELECT DISTINCT A.Nm_Database 
	            FROM ##HistoricoFragmentacaoIndice A
		            LEFT JOIN dbo.BaseDados B ON A.Nm_Database COLLATE SQL_Latin1_General_CP1_CI_AI = B.Nm_Database
	            WHERE B.Nm_Database IS null
	
	            INSERT INTO dbo.Tabela(Nm_Tabela)
	            SELECT DISTINCT A.Nm_Tabela 
	            FROM ##HistoricoFragmentacaoIndice A
		            LEFT JOIN dbo.Tabela B ON A.Nm_Tabela COLLATE SQL_Latin1_General_CP1_CI_AI = B.Nm_Tabela
	            WHERE B.Nm_Tabela IS null	
	
                INSERT INTO dbo.HistoricoFragmentacaoIndice(Dt_Referencia, Id_Servidor, Id_BaseDados, Id_Tabela, Nm_Indice, Nm_Schema,Avg_Fragmentation_In_Percent, Page_Count, Fill_Factor, Fl_Compressao)	
                SELECT	A.Dt_Referencia, E.Id_Servidor, D.Id_BaseDados, C.Id_Tabela, A.Nm_Indice, A.Nm_Schema,
			            A.Avg_Fragmentation_In_Percent, A.Page_Count, A.Fill_Factor, A.Fl_Compressao 
                FROM ##HistoricoFragmentacaoIndice A 
    	            JOIN dbo.Tabela C ON A.Nm_Tabela COLLATE SQL_Latin1_General_CP1_CI_AI = C.Nm_Tabela
		            JOIN dbo.BaseDados D ON A.Nm_Database COLLATE SQL_Latin1_General_CP1_CI_AI = D.Nm_Database
		            JOIN dbo.Servidor E ON A.Nm_Servidor COLLATE SQL_Latin1_General_CP1_CI_AI = E.Nm_Servidor 
    	            LEFT JOIN HistoricoFragmentacaoIndice B ON	E.Id_Servidor = B.Id_Servidor 
																	AND D.Id_BaseDados = B.Id_BaseDados  
    													            AND C.Id_Tabela = B.Id_Tabela 
																	AND A.Nm_Indice COLLATE SQL_Latin1_General_CP1_CI_AI = B.Nm_Indice 
    													            AND CONVERT(VARCHAR, A.Dt_Referencia ,112) = CONVERT(VARCHAR, B.Dt_Referencia ,112)
	            WHERE A.Nm_Indice IS NOT NULL AND B.Id_Hitorico_Fragmentacao_Indice IS NULL
                ORDER BY 2, 3, 4, 5
                ";
        }
    }
}
