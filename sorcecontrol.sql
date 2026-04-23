            -- stpObjetosImport
            ALTER PROCEDURE [dbo].[stpObjetosImport]

            @Objeto nvarchar(800),
            @Arquivo nvarchar(800),
            @DsDatabase SYSNAME,
            @Nmschema nvarchar(200)

            WITH ENCRYPTION
            AS

            /*

            EXEC [SafeBase].[dbo].[stpObjetosImport] 'eCommerce','C:\Data\Jobs\Reports\fsCalcularData.sql'

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
            begin	
	            DECLARE @Lines TABLE (Line NVARCHAR(MAX)) ;
	            DECLARE @FullText NVARCHAR(MAX) = '' ;
	            DECLARE @B NVARCHAR(767)
	            DECLARE cursor_imp CURSOR
                FOR 

                    select Nr_Linha from [dbo].[fncLerArquivo](''+@Arquivo+'')
		
                OPEN cursor_imp;
                FETCH NEXT FROM cursor_imp INTO @B
                WHILE @@FETCH_STATUS = 0

                    begin
            
			            insert @Lines select Ds_Texto from [dbo].[fncLerArquivo](''+@Arquivo+'') where Nr_Linha = @B
 
                        FETCH NEXT FROM cursor_imp into @B
			
                    END;
			
		            select @FullText = @FullText + Char(13) + Line from @Lines ; 
	
                CLOSE cursor_imp;
                DEALLOCATE cursor_imp;

	            IF(OBJECT_ID('tempdb..#tb_imp') IS NOT NULL)
                DROP TABLE #tb_imp;
	            CREATE TABLE #tb_imp
	                ([sqldb] NVARCHAR(max));

	            INSERT INTO #tb_imp
	            SELECT @FullText;

	            --SET @FullText  = '' ;

	            DECLARE @x XML = (SELECT [sqldb] FROM #tb_imp FOR XML PATH(''), ROOT('CustomersData'))

	            INSERT INTO safebase.dbo.HistoricoVersionamentoDB (DataEvento,TipoEvento,[Database],Host,[Schema],Objeto,DesQuery)
	            SELECT (getdate()),'Versionamento DB',@DsDatabase,(@@servername),@Nmschema,@Objeto,@x

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
	
            end
            

------------------------------------------------

            GO
            -- stpObjetosExport
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

            WITH ENCRYPTION
            AS 

            /*
            EXEC dbo.stpObjetosExport
                @Ds_Database = 'Safe2PayDB', -- sysname
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

               -- Apaga o arquivo destino, se já existir
                IF (@Fl_Arquivo_Unico = 1)
                BEGIN
        
                    SET @Caminho = @Diretorio_Destino + '\ExportObjetos.sql'

                    SET @CabecalhoArquivo = '
            USE [' + @Ds_Database + ']
            GO

            '
                    EXEC [safebase].[dbo].[stpWriteFile]
                        @Ds_Texto = @CabecalhoArquivo, -- nvarchar(max)
                        @Ds_Caminho = @Caminho, -- nvarchar(max)
                        @Ds_Codificacao = N'UTF-8', -- nvarchar(max)
                        @Ds_Formato_Quebra_Linha = N'UNIX', -- nvarchar(max)
                        @Fl_Append = @Fl_Arquivo_Unico -- bit

                END

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
            
                        SET @Nm_Arquivo = 'ExportObjetos.sql'
                        SET @CabecalhoArquivo = '
            GO

            -- ' + @Ds_Objeto + '

            '

                        EXEC [safebase].[dbo].[stpWriteFile]
                            @Ds_Texto = @CabecalhoArquivo, -- nvarchar(max)
                            @Ds_Caminho = @Caminho, -- nvarchar(max)
                            @Ds_Codificacao = N'UTF-8', -- nvarchar(max)
                            @Ds_Formato_Quebra_Linha = N'UNIX', -- nvarchar(max)
                            @Fl_Append = @Fl_Arquivo_Unico -- bit

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


            GO


            -----------------------------------

            -- stpSourceControl
             GO
            /****** Object:  StoredProcedure [dbo].[stpSourceControl]    Script Date: 07/07/2020 14:52:29 ******/
            SET ANSI_NULLS ON
            GO
            SET QUOTED_IDENTIFIER ON
            GO
            ALTER PROCEDURE [dbo].[stpSourceControl]

            WITH ENCRYPTION
            AS

            BEGIN

	            /*

	            METODO DE USO

	            EXEC [dbo].[stpSourceControl]

	            EXEC dbo.stpObjetosExport
			            @Ds_Database = @DB, 
			            @Diretorio_Destino = 'C:\Data\Jobs\Reports\', -- Caminho do export
			            @Fl_Arquivo_Unico = 0,		-- Gera um arquivo na apeas, nao recomendado em bancos com muitos objetos - 0 Não, 1 Sim
			            @Fl_Gera_Com_Create = 1,	-- Gera sql com create
			            @Fl_Exporta_Procedures = 1, -- Exporta Procedures - 0 Não, 1 Sim
			            @Fl_Exporta_Functions = 1,	-- Exporta Functions - 0 Não, 1 Sim
			            @Fl_Exporta_Triggers = 1,	-- Exporta Triggers - 0 Não, 1 Sim
			            @Fl_Exporta_Views = 1		-- Exporta Views - 0 Não, 1 Sim

		            select [Id]
		              ,[DataEvento]
		              ,[TipoEvento]
		              ,[Database]
		              ,[Usuario]
		              ,[Host]
		              ,[Schema]
		              ,[Objeto]
		              ,[TipoObjeto]
		              ,[DesQuery]
		              from safebase.dbo.HistoricoVersionamentoDB order by DataEvento desc

	            */

	            DECLARE  @TextRel1 VARCHAR(4000), @CaminhoFim VARCHAR(50), @Ass VARCHAR(4000),@HTML VARCHAR(MAX), @Query VARCHAR(MAX), @Subject VARCHAR(600)

	            BEGIN TRANSACTION;  

		            BEGIN TRY  
		 
			            DECLARE @DB NVARCHAR(767)
			            DECLARE cursor_sc CURSOR
			            FOR 

				            SELECT name FROM sys.databases WHERE state_desc not in ('OFFLINE','RESTORING') and is_in_standby = 0 and is_read_only = 0 and database_id > 4 and [name] not like 'SafeBase'
							AND name NOT IN (
												SELECT 
												ADC.database_name                               
												FROM sys.availability_groups_cluster as AGC                                                                            
												JOIN sys.dm_hadr_availability_replica_cluster_states as RCS ON AGC.group_id = RCS.group_id                             
												JOIN sys.dm_hadr_availability_replica_states as ARS ON RCS.replica_id = ARS.replica_id and RCS.group_id = ARS.group_id 
												JOIN sys.availability_databases_cluster as ADC ON AGC.group_id = ADC.group_id                                          
												WHERE ARS.is_local = 1
												AND ARS.role_desc LIKE 'SECONDARY'
											 )

			            OPEN cursor_sc;
			            FETCH NEXT FROM cursor_sc INTO @DB
			            WHILE @@FETCH_STATUS = 0

				            BEGIN 

					            PRINT 'START ' +@DB
					            EXEC dbo.stpObjetosExport
						            @Ds_Database = @DB, 
						            @Diretorio_Destino = 'C:\Data\Jobs\Reports\', -- Caminho do export
						            @Fl_Arquivo_Unico = 0,		-- Gera um arquivo na apeas, nao recomendado em bancos com muitos objetos - 0 Não, 1 Sim
						            @Fl_Gera_Com_Create = 1,	-- Gera sql com create
						            @Fl_Exporta_Procedures = 1, -- Exporta Procedures - 0 Não, 1 Sim
						            @Fl_Exporta_Functions = 1,	-- Exporta Functions - 0 Não, 1 Sim
						            @Fl_Exporta_Triggers = 1,	-- Exporta Triggers - 0 Não, 1 Sim
						            @Fl_Exporta_Views = 1		-- Exporta Views - 0 Não, 1 Sim
			
					            FETCH NEXT FROM cursor_sc into @DB
			
				            END;

			            CLOSE cursor_sc;
			            DEALLOCATE cursor_sc;

		            END TRY  
		            BEGIN CATCH  

			            SELECT   
				            ERROR_NUMBER() AS ErrorNumber  
				            ,ERROR_SEVERITY() AS ErrorSeverity  
				            ,ERROR_STATE() AS ErrorState  
				            ,ERROR_PROCEDURE() AS ErrorProcedure  
				            ,ERROR_LINE() AS ErrorLine  
				            ,ERROR_MESSAGE() AS ErrorMessage;  

			            IF @@TRANCOUNT > 0  
				            ROLLBACK TRANSACTION; 

			            CLOSE cursor_sc;
			            DEALLOCATE cursor_sc;

			            BEGIN
				            SET @Subject = '#Alerta - Erro SourceControl: ' + @@SERVERNAME
				            SET @TextRel1 = 'Prezados,<BR /><BR /> Identifiquei um problema ao criar o versionamento de objetos no '+@@SERVERNAME+', favor verifique esta informação.'  
				            SET @CaminhoFim = 'C:\Data\Jobs\ReportsSCM'+'.html'

				            -- Gera Primeiro bloco de HTML
				            SET @Query = 'SELECT  ERROR_NUMBER() AS ErrorNumber  ,ERROR_SEVERITY() AS ErrorSeverity  ,ERROR_STATE() AS ErrorState  ,ERROR_PROCEDURE() AS ErrorProcedure ,ERROR_LINE() AS ErrorLine  ,ERROR_MESSAGE() AS ErrorMessage;'
				            SET @HTML = safebase.dbo.fncExportaMultiHTML(@Query, @TextRel1, 2, 1)
				            -- Gera Segundo bloco de HTML
				            SET @Ass = (SELECT Assinatura FROM safebase.dbo.MailAssinatura WHERE Id = 1)
				            select @HTML = @HTML + @Ass
				            -- Salva Arquivo HTML de Envio
				            EXEC safebase.dbo.stpWriteFile 
					            @Ds_Texto = @HTML, -- nvarchar(max)
					            @Ds_Caminho = @CaminhoFim, -- nvarchar(max)
					            @Ds_Codificacao = N'UTF-8', -- nvarchar(max)
					            @Ds_Formato_Quebra_Linha = N'windows', -- nvarchar(max)
					            @Fl_Append = 0 -- bit

				            EXEC [msdb].[dbo].[sp_send_dbmail]
					            @profile_name = 'EnviaEmail',
					            @recipients = 'dataservices@facta.com.br',
					            @body_format = 'HTML',
					            @subject = @Subject,
					            @importance = 'High',
					            @body = @HTML;
		
			            END		
	            END CATCH;  

	            IF @@TRANCOUNT > 0  
		            COMMIT TRANSACTION;  

            END

            GO
