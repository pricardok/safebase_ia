using System;
using System.Collections.Generic;
using System.Text;
using SafeBase_Installer.Core;

namespace SafeBase_Installer
{
    class CreateProcN
    {
        public static string Query(string use)
        {
            return
            @"
            USE "+ use + @"
            GO

            SET ANSI_NULLS ON
            GO
            SET QUOTED_IDENTIFIER ON
            GO

            CREATE PROCEDURE [dbo].[stpObjetosExport] ( 
                @Ds_Database SYSNAME,
                @Diretorio_Destino VARCHAR(600),
                @Fl_Arquivo_Unico BIT = 0,
                @Fl_Gera_Com_Create BIT = 1,
                @Fl_Exporta_Procedures BIT = 0,
                @Fl_Exporta_Functions BIT = 0,
                @Fl_Exporta_Triggers BIT = 0,
                @Fl_Exporta_Views BIT = 0)
            AS 

            /*
            EXEC dbo.stpObjetosExport
                @Ds_Database = 'safebase', -- sysname
                @Diretorio_Destino = 'C:\Data\Jobs\Reports\', -- varchar(600)
                @Fl_Arquivo_Unico = 0, -- bit
                @Fl_Gera_Com_Create = 1, -- bit
                @Fl_Exporta_Procedures = 1, -- bit
                @Fl_Exporta_Functions = 1, -- bit
                @Fl_Exporta_Triggers = 1, -- bit
                @Fl_Exporta_Views = 1 -- bit

	            SELECT
		              [Id]
		              ,[DataEvento]
		              ,[TipoEvento]
		              ,[Database]
		              ,[Usuario]
		              ,[Host]
		              ,[Schema]
		              ,[Objeto]
		              ,[TipoObjeto]
		              ,[DesQuery]
	            FROM [SafeBase].[dbo].[HistoricoVersionamentoDB] order by DataEvento desc
	
	            -- DELETE FROM safebase.dbo.HistoricoVersionamentoDB 
            */

            BEGIN

	            -- REMOVE AQUIVOS ANTIGOS
	
	            DECLARE @F NVARCHAR(767)
	            DECLARE cursor_d CURSOR
                FOR 

		            select CaminhoCompleto as Arquivo from dbo.fncListarDiretorio (''+@Diretorio_Destino+'', '*') where Extensao like '.sql'
	
                OPEN cursor_d;
                FETCH NEXT FROM cursor_d INTO @F
                WHILE @@FETCH_STATUS = 0

                    BEGIN 

                        PRINT 'REMOVENDO AQUIVO ' +@F
			            EXEC [dbo].[stpDeleteFile] @F
			
                        FETCH NEXT FROM cursor_d into @F
			
                    END;

                CLOSE cursor_d;
                DEALLOCATE cursor_d;

                -- RECUPERA OS OBJETOS
                DECLARE 
                    @Query VARCHAR(MAX),
                    @Filtro_Tipos VARCHAR(MAX) = '''X''',
		            @A INT;

                IF (@Fl_Exporta_Procedures = 1)
                    SET @Filtro_Tipos = @Filtro_Tipos + ', ''SQL_STORED_PROCEDURE'''
                IF (@Fl_Exporta_Functions = 1)
                    SET @Filtro_Tipos = @Filtro_Tipos + ', ''SQL_INLINE_TABLE_VALUED_FUNCTION''' + ', ''SQL_SCALAR_FUNCTION''' + ', ''SQL_TABLE_VALUED_FUNCTION'''
                IF (@Fl_Exporta_Triggers = 1)
                    SET @Filtro_Tipos = @Filtro_Tipos + ', ''SQL_TRIGGER'''
                IF (@Fl_Exporta_Views = 1)
                    SET @Filtro_Tipos = @Filtro_Tipos + ', ''VIEW'''
        
                SET @Query = '
                IF (OBJECT_ID(''tempdb..##ExportObjetosdb'') IS NOT NULL) DROP TABLE ##ExportObjetosdb
                SELECT 
                    IDENTITY(INT, 1, 1) AS Ordem,
		            schema_name(B.schema_id) as schema_name,
                    B.name AS Ds_Objeto,
                    B.[type_desc] AS Ds_Tipo,
                    (CASE B.[type_desc]
                        WHEN ''SQL_INLINE_TABLE_VALUED_FUNCTION'' THEN ''TableFunction''
                        WHEN ''SQL_SCALAR_FUNCTION'' THEN ''ScalarFunction''
                        WHEN ''SQL_TABLE_VALUED_FUNCTION'' THEN ''TableFunction''
                        WHEN ''SQL_STORED_PROCEDURE'' THEN ''StoredProcedure''
                        WHEN ''SQL_TRIGGER'' THEN ''Trigger''
                        WHEN ''VIEW'' THEN ''View''
                    END) AS Nm_Tipo,
                    A.[definition] AS Ds_Comando
                INTO
                    ##ExportObjetosdb
                FROM 
                    [' + @Ds_Database + '].sys.sql_modules                 A   WITH(NOLOCK)
                    JOIN [' + @Ds_Database + '].sys.objects                B   WITH(NOLOCK)    ON  A.[object_id] = B.[object_id]
                WHERE
                    B.[type_desc] IN (' + @Filtro_Tipos + ')
		            AND A.[definition]  is not null'

                --PRINT(@Query)
                EXEC(@Query)
	
                -- GERA OS ARQUIVOS .SQL
                DECLARE 
                    @Contador INT = 1, 
                    @Total INT = (SELECT COUNT(*) FROM ##ExportObjetosdb),
                    @Nm_Arquivo VARCHAR(MAX),
		            @Nm_Arquivo_s VARCHAR(MAX),
                    @Comando VARCHAR(MAX),
                    @Caminho VARCHAR(MAX),
		            @Caminho_s VARCHAR(MAX),
		            @Nm_schema VARCHAR(200),
		            @Database VARCHAR(200),
                    @Ds_Objeto VARCHAR(MAX),
                    @CabecalhoArquivo VARCHAR(MAX)

                WHILE(@Contador <= @Total)
                BEGIN

                    SELECT
                        @Comando = Ds_Comando,
                        @Nm_Arquivo =  Ds_Objeto + '.sql',
			            @Nm_Arquivo_s =  Ds_Objeto + '.'+[schema_name],
			            @Nm_schema =  [schema_name],
			            @Database = @Ds_Database,
                        @Ds_Objeto = Ds_Objeto
                    FROM 
                        ##ExportObjetosdb
                    WHERE
                        Ordem = @Contador

                    IF (@Fl_Gera_Com_Create = 0)
                    BEGIN

                        SET @Comando = REPLACE(@Comando, 'CREATE PROCEDURE ', 'ALTER PROCEDURE ')
                        SET @Comando = REPLACE(@Comando, 'CREATE VIEW ', 'ALTER VIEW ')
                        SET @Comando = REPLACE(@Comando, 'CREATE TRIGGER ', 'ALTER TRIGGER ')
                        SET @Comando = REPLACE(@Comando, 'CREATE FUNCTION ', 'ALTER FUNCTION ')
            
                    END

                    IF (@Fl_Arquivo_Unico = 1)
                    BEGIN
            
			            PRINT ' DESABILITADO'

                    END
                    ELSE BEGIN

                        SET @Caminho = @Diretorio_Destino + '\' + @Nm_Arquivo
			            SET @Caminho_s = @Diretorio_Destino + '\' + @Nm_Arquivo_s
                        SET @CabecalhoArquivo = 'USE [' + @Ds_Database + ']
                                                    GO

                                                 '

                        EXEC [safebase].[dbo].[stpWriteFile]
                            @Ds_Texto = @CabecalhoArquivo, -- nvarchar(max)
                            @Ds_Caminho = @Caminho, -- nvarchar(max)
                            @Ds_Codificacao = N'UTF-8', -- nvarchar(max)
                            @Ds_Formato_Quebra_Linha = N'UNIX', -- nvarchar(max)
                            @Fl_Append = 0 -- bit

                    END

                    EXEC [safebase].[dbo].[stpWriteFile]
                        @Ds_Texto = @Comando, -- nvarchar(max)
                        @Ds_Caminho = @Caminho, -- nvarchar(max)
                        @Ds_Codificacao = N'UTF-8', -- nvarchar(max)
                        @Ds_Formato_Quebra_Linha = N'UNIX', -- nvarchar(max)
                        @Fl_Append = 1 -- bit

                    SET @Contador = @Contador + 1

                END

	            DECLARE @OB NVARCHAR(767), @AR NVARCHAR(1000),@SN NVARCHAR(300)
	            DECLARE cursor_impdb CURSOR
                FOR 

		            select A.ArquivoSemExtensao as Objeto, A.CaminhoCompleto as Arquivo,B.[schema_name] as [Schema]  from dbo.fncListarDiretorio (''+@Diretorio_Destino+'', '*') A
		            inner join  ##ExportObjetosdb B ON A.ArquivoSemExtensao = B.Ds_Objeto where Extensao like '.sql'
		            /*
		            select A.ArquivoSemExtensao as Objeto, A.CaminhoCompleto as Arquivo,B.[schema_name] as [Schema]  from dbo.fncListarDiretorio ('C:\Data\Jobs\Reports\', '*') A
		            inner join  ##ExportObjetosdb B ON A.ArquivoSemExtensao = B.Ds_Objeto where Extensao like '.sql'
		            */
	
                OPEN cursor_impdb;
                FETCH NEXT FROM cursor_impdb INTO @OB,@AR,@SN
                WHILE @@FETCH_STATUS = 0

                    BEGIN
            
			            PRINT 'START ' + @AR
			            exec [dbo].[stpObjetosImport] @OB,@AR,@Database,@SN
			
                        FETCH NEXT FROM cursor_impdb into @OB,@AR,@SN
			
                    END;
	
                CLOSE cursor_impdb;
                DEALLOCATE cursor_impdb;

	            /*
	              SELECT
		              [Id]
		              ,[DataEvento]
		              ,[TipoEvento]
		              ,[Database]
		              ,[Usuario]
		              ,[Host]
		              ,[Schema]
		              ,[Objeto]
		              ,[TipoObjeto]
		              ,[DesQuery]
	              FROM [SafeBase].[dbo].[HistoricoVersionamentoDB] order by DataEvento desc
	            */

            END


  
            ";

        }
    }
}
