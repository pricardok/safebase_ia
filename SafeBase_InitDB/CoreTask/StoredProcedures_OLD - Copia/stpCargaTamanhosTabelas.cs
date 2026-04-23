using System;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using Microsoft.SqlServer.Server;
public partial class StoredProcedures
{
    [Microsoft.SqlServer.Server.SqlProcedure]
    public static void stpCargaTamanhosTabelas()
    {
        // Create the command
        SqlCommand myCommand = new SqlCommand();
        myCommand.CommandText =
              @"
                SET NOCOUNT ON

                DECLARE @Databases TABLE
                (Id_Database INT IDENTITY(1, 1),
                 Nm_Database VARCHAR(220)
                );
                DECLARE @Total INT, @i INT, @Database VARCHAR(120), @cmd VARCHAR(8000);
                INSERT INTO @Databases(Nm_Database)
                       SELECT name
                       FROM sys.databases
                       WHERE name NOT IN('master', 'model', 'tempdb')
                       AND state_desc = 'online';
                SELECT @Total = MAX(Id_Database)
                FROM @Databases;
                SET @i = 1;
                IF OBJECT_ID('tempdb..##Tamanho_Tabelas') IS NOT NULL
                    DROP TABLE ##Tamanho_Tabelas;
                CREATE TABLE ##Tamanho_Tabelas
                (Nm_Servidor      VARCHAR(256),
                 Nm_Database      VARCHAR(256),
                 [Nm_Schema]      VARCHAR(8000) NULL,
                 [Nm_Tabela]      VARCHAR(8000) NULL,
                 [Nm_Index]       VARCHAR(8000) NULL,
                 Nm_Drive         CHAR(2),
                 [Used_in_kb]     [INT] NULL,
                 [Reserved_in_kb] [INT] NULL,
                 [Tbl_Rows]       [BIGINT] NULL,
                 [Type_Desc]      [VARCHAR](50) NULL
                )
                ON [PRIMARY];
                WHILE(@i <= @Total)
                    BEGIN
                        IF EXISTS
                (
                    SELECT NULL
                    FROM @Databases
                    WHERE Id_Database = @i
                ) -- caso a database foi deletada da tabela @databases, não faz nada.
                            BEGIN
                                SELECT @Database = Nm_Database
                                FROM @Databases
                                WHERE Id_Database = @i;
                                SET @cmd = '
				                insert into ##Tamanho_Tabelas
				                select @@SERVERNAME 
					                , '''+@Database+''' Nm_Database, t.schema_name, t.table_Name, t.Index_name,
					                (SELECT SUBSTRING(filename,1,1) 
					                FROM ['+@Database+'].sys.sysfiles 
					                WHERE fileid = 1),
				                sum(t.used) as used_in_kb,
				                sum(t.reserved) as Reserved_in_kb,
				                --case grouping (t.Index_name) when 0 then sum(t.ind_rows) else sum(t.tbl_rows) end as rows,
				                 max(t.tbl_rows)  as rows,
				                type_Desc
				                from (
					                select s.name as schema_name, 
							                o.name as table_Name,
							                coalesce(i.name,''heap'') as Index_name,
							                p.used_page_Count*8 as used,
							                p.reserved_page_count*8 as reserved, 
							                p.row_count as ind_rows,
							                (case when i.index_id in (0,1) then p.row_count else 0 end) as tbl_rows, 
							                i.type_Desc as type_Desc
					                from 
						                ['+@Database+'].sys.dm_db_partition_stats p
						                join ['+@Database+'].sys.objects o on o.object_id = p.object_id
						                join ['+@Database+'].sys.schemas s on s.schema_id = o.schema_id
						                left join ['+@Database+'].sys.indexes i on i.object_id = p.object_id and i.index_id = p.index_id
					                where o.type_desc = ''user_Table'' and o.is_Ms_shipped = 0
				                ) as t
				                group by t.schema_name, t.table_Name,t.Index_name,type_Desc
				                --with rollup -- no sql server 2005, essa linha deve ser habilitada **********************************************
				                --order by grouping(t.schema_name),t.schema_name,grouping(t.table_Name),t.table_Name,	grouping(t.Index_name),t.Index_name
				                ';
                                EXEC (@cmd);
			
			                 /*print @cmd; -- para debbug
			                 print '
				                ##################################################################################
			                 '; -- para debbug*/

                            END;
                        SET @i = @i + 1;
                    END; 

	                INSERT INTO dbo.Servidor(Nm_Servidor)
	                SELECT DISTINCT A.Nm_Servidor 
	                FROM ##Tamanho_Tabelas A
		                LEFT JOIN dbo.Servidor B ON A.Nm_Servidor = B.Nm_Servidor
	                WHERE B.Nm_Servidor IS null
		
	                INSERT INTO dbo.BaseDados(Nm_Database)
	                SELECT DISTINCT A.Nm_Database 
	                FROM ##Tamanho_Tabelas A
		                LEFT JOIN dbo.BaseDados B ON A.Nm_Database = B.Nm_Database
	                WHERE B.Nm_Database IS null
	
	                INSERT INTO dbo.Tabela(Nm_Tabela)
	                SELECT DISTINCT A.Nm_Tabela 
	                FROM ##Tamanho_Tabelas A
		                LEFT JOIN dbo.Tabela B ON A.Nm_Tabela = B.Nm_Tabela
	                WHERE B.Nm_Tabela IS null	

	                insert into dbo.HistoricoTamanhoTabela(Id_Servidor,Id_BaseDados,Id_Tabela,Nm_Drive,Nr_Tamanho_Total,
				                Nr_Tamanho_Dados,Nr_Tamanho_Indice,Qt_Linhas,Dt_Referencia)
	                select B.Id_Servidor, D.Id_BaseDados, C.Id_Tabela ,UPPER(A.Nm_Drive),
			                sum(Reserved_in_kb)/1024.00 [Reservado (KB)], 
			                sum(case when Type_Desc in ('CLUSTERED','HEAP') then Reserved_in_kb else 0 end)/1024.00 [Dados (KB)], 
			                sum(case when Type_Desc in ('NONCLUSTERED') then Reserved_in_kb else 0 end)/1024.00 [Indices (KB)],
			                max(Tbl_Rows) Qtd_Linhas,
			                CONVERT(VARCHAR, GETDATE() ,112)
						 
	                from ##Tamanho_Tabelas A
		                JOIN dbo.Servidor B ON A.Nm_Servidor = B.Nm_Servidor 
		                JOIN dbo.Tabela C ON A.Nm_Tabela = C.Nm_Tabela
		                JOIN dbo.BaseDados D ON A.Nm_Database = D.Nm_Database
			                LEFT JOIN dbo.HistoricoTamanhoTabela E ON B.Id_Servidor = E.Id_Servidor 
								                AND D.Id_BaseDados = E.Id_BaseDados AND C.Id_Tabela = E.Id_Tabela 
								                AND E.Dt_Referencia = CONVERT(VARCHAR, GETDATE() ,112)    
	                where Nm_Index is not null	and Type_Desc is not NULL
		                AND E.Id_Historico_Tamanho IS NULL 
	                group by B.Id_Servidor, D.Id_BaseDados, C.Id_Tabela,UPPER(A.Nm_Drive), E.Dt_Referencia

	            ";
        // Execute the command and send back the results
        SqlContext.Pipe.ExecuteAndSend(myCommand);
    }
};