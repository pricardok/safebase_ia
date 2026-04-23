using System;
using System.Collections.Generic;
using System.Text;
using SafeBase_Installer.Core;

namespace SafeBase_Installer
{
    class CreateTrigger
    {
        public static string Query(string use)
        {
            return
            @"
            USE "+ use + @"
            GO

            -- Log de alterações de login/user
            USE [master]
            GO

            /*
            METODO DE ANALISE

            select * from safebase.dbo.HistoricoAuditLogins order by DataEvento desc

            USE [safebase]
            GO
            GRANT CONNECT TO [guest]
            GRANT INSERT ON [dbo].[HistoricoAuditLogins] TO PUBLIC

            -- HISTORICOS
            -- 26/01/2021 -- INCLUSAO DA COLUNA AlertaEnviado NO [safebase].[dbo].[HistoricoAuditLogins]

            */

            CREATE TRIGGER [trAuditCreateLogin]
            ON ALL SERVER
            AFTER CREATE_LOGIN, ALTER_LOGIN, DROP_LOGIN, CREATE_USER, ALTER_USER, DROP_USER
            AS
            BEGIN 
                SET NOCOUNT ON
                DECLARE 
                   @vEvtData xml,
                   @vEventTime datetime,
                   @vServer varchar(40),
                   @vLoginName varchar(50),
                   @vIpClient varchar(50),      
                   @vHostName varchar(50),
                   @vAppName varchar(500),
	               @results varchar(max),
	               @subjectText varchar(max)

	            ---- Não loga conexões de usuários de sistema
	            --   IF (ORIGINAL_LOGIN() = ('ecommerce_app') AND PROGRAM_NAME() LIKE 'Microsoft SQL Server Management Studio%')
	            --       RETURN
        
	            SET @vEvtData = eventdata()
                SET @vEventTime = @vEvtData.value('(/EVENT_INSTANCE/PostTime)[1]', 'datetime')
                SET @vServer = @vEvtData.value('(/EVENT_INSTANCE/ServerName)[1]', 'varchar(40)')
                SET @vLoginName = @vEvtData.value('(/EVENT_INSTANCE/LoginName)[1]', 'varchar(50)')
                SET @vIpClient = @vEvtData.value('(/EVENT_INSTANCE/ClientHost)[1]', 'varchar(50)')
                SET @vHostName = HOST_NAME()
                SET @vAppName = APP_NAME()
	            SET @subjectText = '*ALERT* DATABASE LOGIN changed on ' + @@SERVERNAME + ' by ' + SUSER_SNAME() 
	            SET @results = (SELECT EVENTDATA().value('(/EVENT_INSTANCE/TSQLCommand/CommandText)[1]','nvarchar(max)'))

	            IF(OBJECT_ID('safebase.dbo.HistoricoAuditLogins') IS NULL)
	            BEGIN
                        -- DROP TABLE [safebase].[safebase].[dbo].[HistoricoAuditLogins]
			            CREATE TABLE [safebase].[dbo].[HistoricoAuditLogins](
				            [Id] [int] IDENTITY(1,1) NOT NULL,
				            [Server] [varchar](50) NULL,
				            [Login] [varchar](100) NULL,
				            [NomeHost] [varchar](100) NULL,
				            [Aplicacao] [varchar](200) NULL,
				            [IPClient] [varchar](30) NULL,
				            [DataEvento] [datetime] NULL,
				            [DesEvento] [varchar](400) NULL,
				            [DesMSG] [varchar](200) NULL,
				            [AlertaEnviado] [bit] NULL 
			            ) ON [PRIMARY];
                        CREATE CLUSTERED INDEX idx_HistoricoAuditLogins ON safebase.dbo.HistoricoAuditLogins(Id);
	            END;

	            INSERT [safebase].[dbo].[HistoricoAuditLogins](DataEvento,Server,Login,NomeHost,Aplicacao,IPClient,DesEvento,DesMSG)
                SELECT  @vEventTime,@vServer, @vLoginName, @vHostName, @vAppName, @vIpClient,@results, @subjectText  

            END
            GO
            ENABLE TRIGGER [trAuditCreateLogin] ON ALL SERVER
            GO

            -- Trigger de logon
            USE [master]
            GO
            CREATE TRIGGER [trAuditLogin] 
            ON ALL SERVER 
            FOR LOGON
            AS

            BEGIN
                   SET NOCOUNT ON
	               DECLARE
                   @vEvtData xml,
                   @vEventTime datetime,
                   @vServer varchar(40),
                   @vLoginName varchar(50),
                   @vIpClient varchar(50),
                   @vHostName varchar(50),
                   @vAppName varchar(500),
	               @results varchar(max)

                   -- Verifica se o login utilizado é um user de aplicação e se a aplicacao a ser utilizada é o Management Studio
                   IF ORIGINAL_LOGIN() like '%_app' and APP_NAME() LIKE 'Microsoft SQL Server Management Studio%'

                   BEGIN
                         -- Variaveis
                        SET @vEvtData = eventdata()
                        SET @vEventTime = @vEvtData.value('(/EVENT_INSTANCE/PostTime)[1]', 'datetime')
                        SET @vServer = @vEvtData.value('(/EVENT_INSTANCE/ServerName)[1]', 'varchar(40)')
                        SET @vLoginName = @vEvtData.value('(/EVENT_INSTANCE/LoginName)[1]', 'varchar(50)')
                        SET @vIpClient = @vEvtData.value('(/EVENT_INSTANCE/ClientHost)[1]', 'varchar(50)')
                        SET @vHostName = HOST_NAME()
                        SET @vAppName = APP_NAME()
			            SET @results = (SELECT EVENTDATA().value('(/EVENT_INSTANCE/TSQLCommand/CommandText)[1]','nvarchar(max)'))

			            -- Nao permite continuar com a conexao, retornando uma janela com erro
                        ROLLBACK

			            -- Se for o evento será logado, pois esta havendo uma tentativa de burlar a segurança dos dados
			            INSERT [safebase].[dbo].[HistoricoAuditLogins](DataEvento,Server,Login,NomeHost,Aplicacao,IPClient,DesEvento)
			            SELECT  @vEventTime,@vServer, @vLoginName, @vHostName, @vAppName, @vIpClient,'Login nao permitido'  
                   END
            END
            GO

            ENABLE TRIGGER [trAuditLogin] ON ALL SERVER
            GO

            -- Alteração de Objetos 
            USE [master]
            GO
            CREATE TRIGGER [trLogAlterObject] ON ALL SERVER
            FOR DDL_DATABASE_LEVEL_EVENTS
            AS
                 BEGIN

                     SET NOCOUNT ON;

                     DECLARE @Evento XML, 
		             @Mensagem VARCHAR(MAX), 
		             @Dt_Evento DATETIME, 
		             @Ds_Tipo_Evento VARCHAR(30), 
		             @Ds_Database VARCHAR(50), 
		             @Ds_Usuario VARCHAR(100), 
		             @Ds_Schema VARCHAR(20), 
		             @Ds_Objeto VARCHAR(100), 
		             @Ds_Tipo_Objeto VARCHAR(20), 
		             @Ds_Query VARCHAR(MAX),
		             @Ds_SPID VARCHAR(100);
         
		             SET @Evento = EVENTDATA();

                     SELECT @Dt_Evento = @Evento.value('(/EVENT_INSTANCE/PostTime/text())[1]', 'datetime'), 
                            @Ds_Tipo_Evento = @Evento.value('(/EVENT_INSTANCE/EventType/text())[1]', 'varchar(30)'), 
                            @Ds_Database = @Evento.value('(/EVENT_INSTANCE/DatabaseName/text())[1]', 'varchar(50)'), 
                            @Ds_Usuario = @Evento.value('(/EVENT_INSTANCE/LoginName/text())[1]', 'varchar(100)'),
				            @Ds_SPID = HOST_NAME (), 
                            @Ds_Schema = @Evento.value('(/EVENT_INSTANCE/SchemaName/text())[1]', 'varchar(20)'), 
                            @Ds_Objeto = @Evento.value('(/EVENT_INSTANCE/ObjectName/text())[1]', 'varchar(100)'), 
                            @Ds_Tipo_Objeto = @Evento.value('(/EVENT_INSTANCE/ObjectType/text())[1]', 'varchar(20)'), 
                            @Ds_Query = @Evento.value('(/EVENT_INSTANCE/TSQLCommand/CommandText/text())[1]', 'varchar(max)');

                     IF(@Ds_Database IN('master', 'model', 'msdb'))
                        BEGIN
                             IF(LEFT(@Ds_Tipo_Evento, 6) = 'CREATE')
                                BEGIN
                                     SET @Mensagem = 'Tche, você (' + @Ds_Usuario + ') acabou de criar um objeto na database de sistema ' + @Ds_Database + ' e essa operação foi logada e um alerta foi enviado ao time de Banco. 
            Você sera auditado.';
                                     PRINT @Mensagem;
					            END;
                             ELSE
                                BEGIN
                                     IF(LEFT(@Ds_Tipo_Evento, 5) = 'ALTER')
                                         BEGIN
                                             SET @Mensagem = 'Tche, você (' + @Ds_Usuario + ') acabou de criar um objeto na database de sistema ' + @Ds_Database + ' e essa operação foi logada e um alerta foi enviado ao time de Banco. 
            Você sera auditado.';
                                             PRINT @Mensagem;
							             END;
					            END;
			            END;
                     IF(OBJECT_ID('safebase.dbo.HistoricoAlteracaoObjetos') IS NULL)
			            BEGIN

                             -- DROP TABLE [safebase].[dbo].[HistoricoAlteracaoObjetos]
                             CREATE TABLE safebase.dbo.HistoricoAlteracaoObjetos
                             (Id         INT IDENTITY(1, 1), 
                              DataEvento DATETIME, 
                              TipoEvento VARCHAR(30), 
                              [Database] VARCHAR(50), 
				              Usuario    VARCHAR(100),
				              [Host]     VARCHAR(100),
                              [Schema]   VARCHAR(20), 
                              Objeto     VARCHAR(100), 
                              TipoObjeto VARCHAR(20), 
                              DesQuery   XML
                             );
                             CREATE CLUSTERED INDEX idx_HistoricoAlteracaoObjetos ON safebase.dbo.HistoricoAlteracaoObjetos(Id);
			            END;

                     IF(@Ds_Database NOT IN('tempdb'))
                        BEGIN
                            INSERT INTO safebase.dbo.HistoricoAlteracaoObjetos
                            SELECT @Dt_Evento, 
                                    @Ds_Tipo_Evento, 
                                    @Ds_Database, 
                                    @Ds_Usuario,
						            @Ds_SPID,
                                    @Ds_Schema, 
                                    @Ds_Objeto, 
                                    @Ds_Tipo_Objeto, 
                                    @Evento;
			            END;
                 END;
            GO
            ENABLE TRIGGER [trLogAlterObject] ON ALL SERVER
            GO

            USE " + use + @"
            GO
            GRANT CONNECT TO [guest]
            GRANT INSERT ON [dbo].[HistoricoAlteracaoObjetos] TO PUBLIC
            GRANT INSERT ON [dbo].[HistoricoAuditLogins] TO PUBLIC


            ";

        }
    }
}
