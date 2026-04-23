using System;
using System.Collections.Generic;
using System.Text;
using SafeBase_Installer.Core;

namespace SafeBase_Installer
{
    class CreateTables
    { 
        public static string Query(string use)
        {
            return
            @"
            USE "+ use + @"
            SET ANSI_NULLS ON
            
            ALTER AUTHORIZATION ON DATABASE::[" + use + @"] TO [sa]
            EXEC dbo.sp_changedbowner [sa]
 
            SET QUOTED_IDENTIFIER ON
 
            CREATE TABLE [dbo].[Alerta](
	            [Id_Alerta] [int] IDENTITY(1,1) NOT NULL,
	            [Id_AlertaParametro] [int] NOT NULL,
	            [Ds_Mensagem] [varchar](2000) NULL,
	            [Fl_Tipo] [tinyint] NULL,
	            [Dt_Alerta] [datetime] NULL,
            PRIMARY KEY CLUSTERED 
            (
	            [Id_Alerta] ASC
            )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
            ) ON [PRIMARY]
 
            /****** Object:  Table [dbo].[AlertaAlwaysOn]    Script Date: 05/04/2020 20:25:57 ******/
            SET ANSI_NULLS ON
 
            SET QUOTED_IDENTIFIER ON
 
            CREATE TABLE [dbo].[AlertaAlwaysOn](
	            [Database] [varchar](100) NULL,
	            [Status] [varchar](100) NULL,
	            [Sync] [varchar](100) NULL,
	            [T] [varchar](100) NULL
            ) ON [PRIMARY]

            /****** Object:  Table [dbo].[AlertaEnvio]    Script Date: 27/01/2017 21:42:51 ******/
            SET ANSI_NULLS ON

            SET QUOTED_IDENTIFIER ON

            CREATE TABLE [dbo].[AlertaEnvio](
	            [Id] [int] IDENTITY(1,1) NOT NULL,
	            [IdAlertaParametro] [int] NULL,
	            [IdTipoEnvio] [int] NULL,
	            [Ativo] [bit] NOT NULL,
	            [Des] [varchar](100) NULL,
	            [DataCriação] [datetime2](7) NULL,
	            [DataAlteracao] [datetime2](7) NULL,
            PRIMARY KEY CLUSTERED 
            (
	            [Id] ASC
            )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
            ) ON [PRIMARY]

 
            /****** Object:  Table [dbo].[AlertaMsgToken]    Script Date: 05/04/2020 20:25:57 ******/
            SET ANSI_NULLS ON
 
            SET QUOTED_IDENTIFIER ON
 
            CREATE TABLE [dbo].[AlertaMsgToken](
	            [Id] [int] IDENTITY(1,1) NOT NULL,
	            [Nome] [varchar](100) NOT NULL,
	            [IdAlertaParametro] [int] NOT NULL,
	            [Token] [varchar](400) NULL,
	            [DataInclusao] [datetime2](7) NULL,
	            [DataAlteracao] [datetime2](7) NULL,
	            [User] [varchar](30) NULL,
	            [Pass] [varchar](40) NULL,
	            [Ativo] [bit] NOT NULL,
	            [NomeCanal] [varchar](255) NULL,
	            [Canal] [varchar](50) NULL,
            PRIMARY KEY CLUSTERED 
            (
	            [Id] ASC
            )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
            ) ON [PRIMARY]
 
            /****** Object:  Table [dbo].[AlertaParametro]    Script Date: 05/04/2020 20:25:57 ******/
            SET ANSI_NULLS ON
 
            SET QUOTED_IDENTIFIER ON
 
            CREATE TABLE [dbo].[AlertaParametro](
	            [Id_AlertaParametro] [int] IDENTITY(1,1) NOT NULL,
	            [Nm_Alerta] [varchar](100) NOT NULL,
	            [Nm_Procedure] [varchar](100) NOT NULL,
	            [Fl_Clear] [bit] NOT NULL,
	            [Vl_Parametro] [int] NULL,
	            [Ds_Metrica] [varchar](50) NULL,
	            [Nm_Empresa] [nvarchar](50) NULL,
	            [Ds_Email] [varchar](200) NULL,
	            [Ds_Caminho] [varchar](200) NULL,
	            [Ds_Caminho_Log] [varchar](200) NULL,
	            [IgnoraDatabase] [varchar](200) NULL,
	            [Ds_ProfileDBMail] [varchar](50) NULL,
	            [Ds_BodyFormatMail] [varchar](50) NULL,
	            [Ds_TipoMail] [varchar](30) NULL,
	            [IdMailAssinatura] [int] NULL,
	            [Ativo] [bit] NOT NULL,
	            [Ds_Menssageiro_01] [int] NULL,
	            [Ds_Menssageiro_02] [int] NULL,
	            [Ds_Menssageiro_03] [varchar](20) NULL,
	            [Ds_Menssageiro_04] [varchar](20) NULL,
	            [Ds_Menssageiro_05] [varchar](20) NULL,
	            [Ds_MSG] [varchar](20) NULL,
	            [Ds_Inclusao_Exclusao] [nvarchar](100) NULL,
				[ZabbixAlertName] varchar(128) NULL,
				[ZabbixServer] varchar(128) NULL,
				[ZabbixPath] varchar(256) NULL,
				[ZabbixLocalServer] varchar(128) NULL,
            PRIMARY KEY CLUSTERED 
            (
	            [Id_AlertaParametro] ASC
            )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
            ) ON [PRIMARY]
 
            /****** Object:  Table [dbo].[AlertaParametroMenssage]    Script Date: 05/04/2020 20:25:57 ******/
            SET ANSI_NULLS ON
 
            SET QUOTED_IDENTIFIER ON
 
            CREATE TABLE [dbo].[AlertaParametroMenssage](
	            [Id] [int] IDENTITY(1,1) NOT NULL,
	            [IdAlertaParametro] [int] NOT NULL,
	            [SubjectProblem] [varchar](max) NULL,
	            [SubjectSolution] [varchar](max) NULL,
	            [MailTextProblem] [varchar](max) NULL,
	            [MailTextSolution] [varchar](max) NULL,
	            [IdUsuarioCriacao] [int] NULL,
	            [DataCriacao] [datetime] NULL,
	            [DataAlteracao] [datetime] NULL,
	            [NomeMsg] [varchar](90) NULL,
            PRIMARY KEY CLUSTERED 
            (
	            [Id] ASC
            )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
            ) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
 
            /****** Object:  Table [dbo].[AlertaStatusDatabases]    Script Date: 05/04/2020 20:25:57 ******/
            SET ANSI_NULLS ON
 
            SET QUOTED_IDENTIFIER ON
 
            CREATE TABLE [dbo].[AlertaStatusDatabases](
	            [IdAlerta] [int] IDENTITY(1,1) NOT NULL,
	            [NomeAlerta] [varchar](200) NULL,
	            [DesMensagem] [varchar](2000) NULL,
	            [Tipo] [tinyint] NULL,
	            [DataAlerta] [datetime] NULL,
            PRIMARY KEY CLUSTERED 
            (
	            [IdAlerta] ASC
            )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
            ) ON [PRIMARY]
 
            /****** Object:  Table [dbo].[BaseDados]    Script Date: 05/04/2020 20:25:57 ******/
            SET ANSI_NULLS ON
 
            SET QUOTED_IDENTIFIER ON
 
            CREATE TABLE [dbo].[BaseDados](
	            [Id_BaseDados] [int] IDENTITY(1,1) NOT NULL,
	            [Nm_Database] [varchar](500) NULL,
             CONSTRAINT [PK_BaseDados] PRIMARY KEY CLUSTERED 
            (
	            [Id_BaseDados] ASC
            )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
            ) ON [PRIMARY]
 
            /****** Object:  Table [dbo].[BaseJobs]    Script Date: 05/04/2020 20:25:57 ******/
            SET ANSI_NULLS ON
 
            SET QUOTED_IDENTIFIER ON
 
            CREATE TABLE [dbo].[BaseJobs](
	            [Id] [int] IDENTITY(1,1) NOT NULL,
	            [SQLText] [varchar](max) NOT NULL,
	            [SQLTextValida] [varchar](max) NOT NULL,
	            [Descricao] [nvarchar](256) NOT NULL,
	            [Email] [nvarchar](256) NOT NULL,
	            [IdGrupoDeMail] [int] NOT NULL,
	            [AssuntoMail] [varchar](500) NOT NULL,
	            [DesCorpoMail] [ntext] NOT NULL,
	            [Ativo] [bit] NOT NULL,
	            [DataCriacao] [datetime2](7) NOT NULL,
             CONSTRAINT [PK_dbo.BaseJobs] PRIMARY KEY CLUSTERED 
            (
	            [Id] ASC
            )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
            ) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
 
            /****** Object:  Table [dbo].[ConfigDB]    Script Date: 13/04/2020 20:40:33 ******/
            SET ANSI_NULLS ON

            SET QUOTED_IDENTIFIER ON

            CREATE TABLE [dbo].[ConfigDB](
	            [ParametersJson] [varchar](max) NULL,
	            [ParametersXML] [xml] NULL,
	            [Ativo] [bit] NULL,
	            [LastUploadPostLog] [datetime] NULL,
	            [LastGetSchema] [datetime] NULL,
	            [LastGetConfig] [datetime] NULL
            ) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

            /****** Object:  Table [dbo].[CheckAlerta]    Script Date: 05/04/2020 20:25:57 ******/
            SET ANSI_NULLS ON
 
            SET QUOTED_IDENTIFIER ON
 
            CREATE TABLE [dbo].[CheckAlerta](
	            [Nm_Alerta] [varchar](200) NULL,
	            [Ds_Mensagem] [varchar](200) NULL,
	            [Dt_Alerta] [datetime] NULL,
	            [Run_Duration] [varchar](18) NULL
            ) ON [PRIMARY]
 
            /****** Object:  Table [dbo].[CheckAlertaSemClear]    Script Date: 05/04/2020 20:25:57 ******/
            SET ANSI_NULLS ON
 
            SET QUOTED_IDENTIFIER ON
 
            CREATE TABLE [dbo].[CheckAlertaSemClear](
	            [Nm_Alerta] [varchar](200) NULL,
	            [Ds_Mensagem] [varchar](200) NULL,
	            [Dt_Alerta] [datetime] NULL,
	            [Run_Duration] [varchar](18) NULL
            ) ON [PRIMARY]
 
            /****** Object:  Table [dbo].[CheckAlteracaoJobs]    Script Date: 05/04/2020 20:25:57 ******/
            SET ANSI_NULLS ON
 
            SET QUOTED_IDENTIFIER ON
 
            CREATE TABLE [dbo].[CheckAlteracaoJobs](
	            [Nm_Job] [varchar](1000) NULL,
	            [Fl_Habilitado] [tinyint] NULL,
	            [Dt_Criacao] [datetime] NULL,
	            [Dt_Modificacao] [datetime] NULL,
	            [Nr_Versao] [smallint] NULL
            ) ON [PRIMARY]
 
            /****** Object:  Table [dbo].[CheckArquivosDados]    Script Date: 05/04/2020 20:25:57 ******/
            SET ANSI_NULLS ON
 
            SET QUOTED_IDENTIFIER ON
 
            CREATE TABLE [dbo].[CheckArquivosDados](
	            [Server] [varchar](50) NULL,
	            [Nm_Database] [varchar](100) NULL,
	            [Logical_Name] [varchar](100) NULL,
	            [FileName] [varchar](200) NULL,
	            [Total_Reservado] [numeric](15, 2) NULL,
	            [Total_Utilizado] [numeric](15, 2) NULL,
	            [Espaco_Livre_MB] [numeric](15, 2) NULL,
	            [Espaco_Livre_Perc] [numeric](15, 2) NULL,
	            [MaxSize] [int] NULL,
	            [Growth] [varchar](25) NULL,
	            [NextSize] [numeric](15, 2) NULL,
	            [Fl_Situacao] [char](1) NULL
            ) ON [PRIMARY]
 
            /****** Object:  Table [dbo].[CheckArquivosLog]    Script Date: 05/04/2020 20:25:57 ******/
            SET ANSI_NULLS ON
 
            SET QUOTED_IDENTIFIER ON
 
            CREATE TABLE [dbo].[CheckArquivosLog](
	            [Server] [varchar](50) NULL,
	            [Nm_Database] [varchar](100) NULL,
	            [Logical_Name] [varchar](100) NULL,
	            [FileName] [varchar](200) NULL,
	            [Total_Reservado] [numeric](15, 2) NULL,
	            [Total_Utilizado] [numeric](15, 2) NULL,
	            [Espaco_Livre_MB] [numeric](15, 2) NULL,
	            [Espaco_Livre_Perc] [numeric](15, 2) NULL,
	            [MaxSize] [int] NULL,
	            [Growth] [varchar](25) NULL,
	            [NextSize] [numeric](15, 2) NULL,
	            [Fl_Situacao] [char](1) NULL
            ) ON [PRIMARY]
 
            /****** Object:  Table [dbo].[CheckBackupsExecutados]    Script Date: 05/04/2020 20:25:57 ******/
            SET ANSI_NULLS ON
 
            SET QUOTED_IDENTIFIER ON
 
            CREATE TABLE [dbo].[CheckBackupsExecutados](
	            [Database_Name] [varchar](128) NULL,
	            [Name] [varchar](128) NULL,
	            [Backup_Start_Date] [datetime] NULL,
	            [Tempo_Min] [int] NULL,
	            [Position] [int] NULL,
	            [Server_Name] [varchar](128) NULL,
	            [Recovery_Model] [varchar](60) NULL,
	            [Logical_Device_Name] [varchar](128) NULL,
	            [Device_Type] [tinyint] NULL,
	            [Type] [char](1) NULL,
	            [Tamanho_MB] [numeric](15, 2) NULL
            ) ON [PRIMARY]
 
            /****** Object:  Table [dbo].[CheckConexaoAberta]    Script Date: 05/04/2020 20:25:57 ******/
            SET ANSI_NULLS ON
 
            SET QUOTED_IDENTIFIER ON
 
            CREATE TABLE [dbo].[CheckConexaoAberta](
	            [login_name] [nvarchar](256) NULL,
	            [session_count] [int] NULL
            ) ON [PRIMARY]
 
            /****** Object:  Table [dbo].[CheckConexaoAberta_Email]    Script Date: 05/04/2020 20:25:57 ******/
            SET ANSI_NULLS ON
 
            SET QUOTED_IDENTIFIER ON
 
            CREATE TABLE [dbo].[CheckConexaoAberta_Email](
	            [Nr_Ordem] [int] NULL,
	            [login_name] [nvarchar](256) NULL,
	            [session_count] [int] NULL
            ) ON [PRIMARY]
 
            /****** Object:  Table [dbo].[CheckContadores]    Script Date: 05/04/2020 20:25:57 ******/
            SET ANSI_NULLS ON
 
            SET QUOTED_IDENTIFIER ON
 
            CREATE TABLE [dbo].[CheckContadores](
	            [Hora] [tinyint] NULL,
	            [Nm_Contador] [varchar](60) NULL,
	            [Media] [bigint] NULL
            ) ON [PRIMARY]
 
            /****** Object:  Table [dbo].[CheckContadoresEmail]    Script Date: 05/04/2020 20:25:57 ******/
            SET ANSI_NULLS ON
 
            SET QUOTED_IDENTIFIER ON
 
            CREATE TABLE [dbo].[CheckContadoresEmail](
	            [Hora] [varchar](30) NOT NULL,
	            [BatchRequests] [varchar](30) NOT NULL,
	            [CPU] [varchar](30) NOT NULL,
	            [Page_Life_Expectancy] [varchar](30) NOT NULL,
	            [User_Connection] [varchar](30) NOT NULL,
	            [Qtd_Queries_Lentas] [varchar](30) NOT NULL,
	            [Reads_Queries_Lentas] [varchar](30) NOT NULL
            ) ON [PRIMARY]
 
            /****** Object:  Table [dbo].[CheckDatabaseGrowth]    Script Date: 05/04/2020 20:25:57 ******/
            SET ANSI_NULLS ON
 
            SET QUOTED_IDENTIFIER ON
 
            CREATE TABLE [dbo].[CheckDatabaseGrowth](
	            [Nm_Servidor] [varchar](50) NULL,
	            [Nm_Database] [varchar](100) NULL,
	            [Tamanho_Atual] [numeric](38, 2) NULL,
	            [Cresc_1_dia] [numeric](38, 2) NULL,
	            [Cresc_15_dia] [numeric](38, 2) NULL,
	            [Cresc_30_dia] [numeric](38, 2) NULL,
	            [Cresc_60_dia] [numeric](38, 2) NULL
            ) ON [PRIMARY]
 
            /****** Object:  Table [dbo].[CheckDatabaseGrowthEmail]    Script Date: 05/04/2020 20:25:57 ******/
            SET ANSI_NULLS ON
 
            SET QUOTED_IDENTIFIER ON
 
            CREATE TABLE [dbo].[CheckDatabaseGrowthEmail](
	            [Nm_Servidor] [varchar](50) NULL,
	            [Nm_Database] [varchar](100) NULL,
	            [Tamanho_Atual] [numeric](38, 2) NULL,
	            [Cresc_1_dia] [numeric](38, 2) NULL,
	            [Cresc_15_dia] [numeric](38, 2) NULL,
	            [Cresc_30_dia] [numeric](38, 2) NULL,
	            [Cresc_60_dia] [numeric](38, 2) NULL
            ) ON [PRIMARY]

            /****** Object:  Table [dbo].[CheckDatabasesHistoricoBackup]    Script Date: 23/01/2014 17:28:32 ******/
            SET ANSI_NULLS ON

            SET QUOTED_IDENTIFIER ON

            CREATE TABLE [dbo].[CheckDatabasesHistoricoBackup](
	            [Servidor] [nvarchar](164) NULL,
	            [Banco] [nvarchar](128) NULL,
	            [UltimoFull] [int] NULL,
	            [DataFull] [datetime] NULL,
	            [TamanhoFull_MB] [char](30) NULL,
	            [UltimoDiff] [int] NULL,
	            [DataDiff] [datetime] NULL,
	            [UltimoFullDiff] [int] NULL,
	            [TamanhoDiff_MB] [char](30) NULL,
	            [UltimoLog_Min] [int] NULL,
	            [DataLog] [datetime] NULL,
	            [TamanhoLog_MB] [char](30) NULL
            ) ON [PRIMARY]
 
            /****** Object:  Table [dbo].[CheckDatabasesSemBackup]    Script Date: 05/04/2020 20:25:57 ******/
            SET ANSI_NULLS ON
 
            SET QUOTED_IDENTIFIER ON
 
            CREATE TABLE [dbo].[CheckDatabasesSemBackup](
	            [Nm_Database] [varchar](100) NOT NULL
            ) ON [PRIMARY]
 
            /****** Object:  Table [dbo].[CheckDBControllerQueries]    Script Date: 05/04/2020 20:25:57 ******/
            SET ANSI_NULLS ON
 
            SET QUOTED_IDENTIFIER ON
 
            CREATE TABLE [dbo].[CheckDBControllerQueries](
	            [PrefixoQuery] [varchar](400) NULL,
	            [QTD] [int] NULL,
	            [Total] [numeric](15, 2) NULL,
	            [Media] [numeric](15, 2) NULL,
	            [Menor] [numeric](15, 2) NULL,
	            [Maior] [numeric](15, 2) NULL,
	            [Writes] [bigint] NULL,
	            [CPU] [bigint] NULL,
	            [Reads] [bigint] NULL,
	            [Ordem] [tinyint] NULL
            ) ON [PRIMARY]
 
            /****** Object:  Table [dbo].[CheckDBControllerQueriesGeral]    Script Date: 05/04/2020 20:25:57 ******/
            SET ANSI_NULLS ON
 
            SET QUOTED_IDENTIFIER ON
 
            CREATE TABLE [dbo].[CheckDBControllerQueriesGeral](
	            [Data] [varchar](50) NULL,
	            [QTD] [int] NULL
            ) ON [PRIMARY]
 
            /****** Object:  Table [dbo].[CheckEspacoDisco]    Script Date: 05/04/2020 20:25:57 ******/
            SET ANSI_NULLS ON
 
            SET QUOTED_IDENTIFIER ON
 
            CREATE TABLE [dbo].[CheckEspacoDisco](
	            [Drive] [varchar](50) NULL,
	            [Size (MB)] [varchar](30) NULL,
	            [Used (MB)] [varchar](30) NULL,
	            [Free (MB)] [varchar](30) NULL,
	            [Used (%)] [varchar](30) NULL,
	            [Free (%)] [varchar](30) NULL,
	            [Used by SQL (MB)] [varchar](30) NULL
            ) ON [PRIMARY]
 
            /****** Object:  Table [dbo].[CheckFragmentacaoIndices]    Script Date: 05/04/2020 20:25:57 ******/
            SET ANSI_NULLS ON
 
            SET QUOTED_IDENTIFIER ON
 
            CREATE TABLE [dbo].[CheckFragmentacaoIndices](
	            [Dt_Referencia] [datetime] NULL,
	            [Nm_Servidor] [varchar](100) NULL,
	            [Nm_Database] [varchar](1000) NULL,
	            [Nm_Tabela] [varchar](1000) NULL,
	            [Nm_Indice] [varchar](1000) NULL,
	            [Avg_Fragmentation_In_Percent] [numeric](5, 2) NULL,
	            [Page_Count] [int] NULL,
	            [Fill_Factor] [tinyint] NULL,
	            [Fl_Compressao] [tinyint] NULL
            ) ON [PRIMARY]
 
            /****** Object:  Table [dbo].[CheckInitDBQueries]    Script Date: 05/04/2020 20:25:57 ******/
            SET ANSI_NULLS ON
 
            SET QUOTED_IDENTIFIER ON
 
            CREATE TABLE [dbo].[CheckInitDBQueries](
	            [PrefixoQuery] [varchar](400) NULL,
	            [QTD] [int] NULL,
	            [Total] [numeric](15, 2) NULL,
	            [Media] [numeric](15, 2) NULL,
	            [Menor] [numeric](15, 2) NULL,
	            [Maior] [numeric](15, 2) NULL,
	            [Writes] [bigint] NULL,
	            [CPU] [bigint] NULL,
	            [Reads] [bigint] NULL,
	            [Ordem] [tinyint] NULL
            ) ON [PRIMARY]
 
            /****** Object:  Table [dbo].[CheckInitDBQueriesGeral]    Script Date: 05/04/2020 20:25:57 ******/
            SET ANSI_NULLS ON
 
            SET QUOTED_IDENTIFIER ON
 
            CREATE TABLE [dbo].[CheckInitDBQueriesGeral](
	            [Data] [varchar](50) NULL,
	            [QTD] [int] NULL
            ) ON [PRIMARY]
 
            /****** Object:  Table [dbo].[CheckJobDemorados]    Script Date: 05/04/2020 20:25:57 ******/
            SET ANSI_NULLS ON
 
            SET QUOTED_IDENTIFIER ON
 
            CREATE TABLE [dbo].[CheckJobDemorados](
	            [Job_Name] [varchar](255) NULL,
	            [Status] [varchar](19) NULL,
	            [Dt_Execucao] [varchar](30) NULL,
	            [Run_Duration] [varchar](8) NULL,
	            [SQL_Message] [varchar](3990) NULL
            ) ON [PRIMARY]
 
            /****** Object:  Table [dbo].[CheckJobsFailed]    Script Date: 05/04/2020 20:25:57 ******/
            SET ANSI_NULLS ON
 
            SET QUOTED_IDENTIFIER ON
 
            CREATE TABLE [dbo].[CheckJobsFailed](
	            [Server] [varchar](50) NULL,
	            [Job_Name] [varchar](255) NULL,
	            [Status] [varchar](25) NULL,
	            [Dt_Execucao] [varchar](20) NULL,
	            [Run_Duration] [varchar](8) NULL,
	            [SQL_Message] [varchar](4490) NULL
            ) ON [PRIMARY]
 
            /****** Object:  Table [dbo].[CheckJobsRunning]    Script Date: 05/04/2020 20:25:57 ******/
            SET ANSI_NULLS ON
 
            SET QUOTED_IDENTIFIER ON
 
            CREATE TABLE [dbo].[CheckJobsRunning](
	            [Nm_JOB] [varchar](256) NULL,
	            [Dt_Inicio] [varchar](16) NULL,
	            [Qt_Duracao] [varchar](60) NULL,
	            [Nm_Step] [varchar](256) NULL
            ) ON [PRIMARY]
 
            /****** Object:  Table [dbo].[CheckQueries]    Script Date: 05/04/2020 20:25:57 ******/
            SET ANSI_NULLS ON
 
            SET QUOTED_IDENTIFIER ON
 
            CREATE TABLE [dbo].[CheckQueries](
	            [PrefixoQuery] [varchar](400) NULL,
	            [QTD] [int] NULL,
	            [Total] [numeric](15, 2) NULL,
	            [Media] [numeric](15, 2) NULL,
	            [Menor] [numeric](15, 2) NULL,
	            [Maior] [numeric](15, 2) NULL,
	            [Writes] [bigint] NULL,
	            [CPU] [bigint] NULL,
	            [Reads] [bigint] NULL,
	            [Ordem] [tinyint] NULL
            ) ON [PRIMARY]
 
            /****** Object:  Table [dbo].[CheckQueriesGeral]    Script Date: 05/04/2020 20:25:57 ******/
            SET ANSI_NULLS ON
 
            SET QUOTED_IDENTIFIER ON
 
            CREATE TABLE [dbo].[CheckQueriesGeral](
	            [Data] [varchar](50) NULL,
	            [QTD] [int] NULL
            ) ON [PRIMARY]
 
            /****** Object:  Table [dbo].[CheckQueriesRunning]    Script Date: 05/04/2020 20:25:57 ******/
            SET ANSI_NULLS ON
 
            SET QUOTED_IDENTIFIER ON
 
            CREATE TABLE [dbo].[CheckQueriesRunning](
	            [dd hh:mm:ss.mss] [varchar](20) NULL,
	            [database_name] [nvarchar](128) NULL,
	            [login_name] [nvarchar](128) NULL,
	            [host_name] [nvarchar](128) NULL,
	            [start_time] [datetime] NULL,
	            [status] [varchar](30) NULL,
	            [session_id] [int] NULL,
	            [blocking_session_id] [int] NULL,
	            [wait_info] [varchar](max) NULL,
	            [open_tran_count] [int] NULL,
	            [CPU] [varchar](max) NULL,
	            [reads] [varchar](max) NULL,
	            [writes] [varchar](max) NULL,
	            [sql_command] [varchar](max) NULL
            ) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
 
            /****** Object:  Table [dbo].[CheckSQLServerErrorLog]    Script Date: 05/04/2020 20:25:57 ******/
            SET ANSI_NULLS ON
 
            SET QUOTED_IDENTIFIER ON
 
            CREATE TABLE [dbo].[CheckSQLServerErrorLog](
	            [Dt_Log] [datetime] NULL,
	            [ProcessInfo] [varchar](100) NULL,
	            [Text] [varchar](max) NULL
            ) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
 
            /****** Object:  Table [dbo].[CheckSQLServerLoginFailed]    Script Date: 05/04/2020 20:25:57 ******/
            SET ANSI_NULLS ON
 
            SET QUOTED_IDENTIFIER ON
 
            CREATE TABLE [dbo].[CheckSQLServerLoginFailed](
	            [Text] [varchar](max) NULL,
	            [Qt_Erro] [int] NULL
            ) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
 
            /****** Object:  Table [dbo].[CheckSQLServerLoginFailedEmail]    Script Date: 05/04/2020 20:25:57 ******/
            SET ANSI_NULLS ON
 
            SET QUOTED_IDENTIFIER ON
 
            CREATE TABLE [dbo].[CheckSQLServerLoginFailedEmail](
	            [Nr_Ordem] [int] NULL,
	            [Text] [varchar](max) NULL,
	            [Qt_Erro] [int] NULL
            ) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
 
            /****** Object:  Table [dbo].[CheckTableGrowth]    Script Date: 05/04/2020 20:25:57 ******/
            SET ANSI_NULLS ON
 
            SET QUOTED_IDENTIFIER ON
 
            CREATE TABLE [dbo].[CheckTableGrowth](
	            [Nm_Servidor] [varchar](50) NULL,
	            [Nm_Database] [varchar](100) NULL,
	            [Nm_Tabela] [varchar](100) NULL,
	            [Tamanho_Atual] [numeric](38, 2) NULL,
	            [Cresc_1_dia] [numeric](38, 2) NULL,
	            [Cresc_15_dia] [numeric](38, 2) NULL,
	            [Cresc_30_dia] [numeric](38, 2) NULL,
	            [Cresc_60_dia] [numeric](38, 2) NULL
            ) ON [PRIMARY]
 
            /****** Object:  Table [dbo].[CheckTableGrowthEmail]    Script Date: 05/04/2020 20:25:57 ******/
            SET ANSI_NULLS ON
 
            SET QUOTED_IDENTIFIER ON
 
            CREATE TABLE [dbo].[CheckTableGrowthEmail](
	            [Nm_Servidor] [varchar](50) NULL,
	            [Nm_Database] [varchar](100) NULL,
	            [Nm_Tabela] [varchar](100) NULL,
	            [Tamanho_Atual] [numeric](38, 2) NULL,
	            [Cresc_1_dia] [numeric](38, 2) NULL,
	            [Cresc_15_dia] [numeric](38, 2) NULL,
	            [Cresc_30_dia] [numeric](38, 2) NULL,
	            [Cresc_60_dia] [numeric](38, 2) NULL
            ) ON [PRIMARY]
 
            /****** Object:  Table [dbo].[CheckUtilizacaoArquivoReads]    Script Date: 05/04/2020 20:25:57 ******/
            SET ANSI_NULLS ON
 
            SET QUOTED_IDENTIFIER ON
 
            CREATE TABLE [dbo].[CheckUtilizacaoArquivoReads](
	            [Nm_Database] [nvarchar](200) NOT NULL,
	            [file_id] [smallint] NULL,
	            [io_stall_read_ms] [bigint] NULL,
	            [num_of_reads] [bigint] NULL,
	            [avg_read_stall_ms] [numeric](15, 1) NULL
            ) ON [PRIMARY]
 
            /****** Object:  Table [dbo].[CheckUtilizacaoArquivoWrites]    Script Date: 05/04/2020 20:25:57 ******/
            SET ANSI_NULLS ON
 
            SET QUOTED_IDENTIFIER ON
 
            CREATE TABLE [dbo].[CheckUtilizacaoArquivoWrites](
	            [Nm_Database] [nvarchar](200) NOT NULL,
	            [file_id] [smallint] NULL,
	            [io_stall_write_ms] [bigint] NULL,
	            [num_of_writes] [bigint] NULL,
	            [avg_write_stall_ms] [numeric](15, 1) NULL
            ) ON [PRIMARY]
 
            /****** Object:  Table [dbo].[CheckWaitsStats]    Script Date: 05/04/2020 20:25:57 ******/
            SET ANSI_NULLS ON
 
            SET QUOTED_IDENTIFIER ON
 
            CREATE TABLE [dbo].[CheckWaitsStats](
	            [WaitType] [varchar](100) NULL,
	            [Min_Log] [datetime] NULL,
	            [Max_Log] [datetime] NULL,
	            [DIf_Wait_S] [decimal](14, 2) NULL,
	            [DIf_Resource_S] [decimal](14, 2) NULL,
	            [DIf_Signal_S] [decimal](14, 2) NULL,
	            [DIf_WaitCount] [bigint] NULL,
	            [DIf_Percentage] [decimal](4, 2) NULL,
	            [Last_Percentage] [decimal](4, 2) NULL
            ) ON [PRIMARY]
 
            /****** Object:  Table [dbo].[Config]    Script Date: 05/04/2020 20:25:57 ******/
            SET ANSI_NULLS ON
 
            SET QUOTED_IDENTIFIER ON
 
            CREATE TABLE [dbo].[Config](
	            [Parameter] [xml] NULL,
	            [LastUploadPostLog] [datetime] NULL,
	            [LastGetSchema] [datetime] NULL,
	            [LastGetConfig] [datetime] NULL
            ) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
 
            /****** Object:  Table [dbo].[Contador]    Script Date: 05/04/2020 20:25:57 ******/
            SET ANSI_NULLS ON
 
            SET QUOTED_IDENTIFIER ON
 
            CREATE TABLE [dbo].[Contador](
	            [Id_Contador] [int] IDENTITY(1,1) NOT NULL,
	            [Nm_Contador] [varchar](50) NULL,
            PRIMARY KEY CLUSTERED 
            (
	            [Id_Contador] ASC
            )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
            ) ON [PRIMARY]
 
            /****** Object:  Table [dbo].[ContadorRegistro]    Script Date: 05/04/2020 20:25:57 ******/
            SET ANSI_NULLS ON
 
            SET QUOTED_IDENTIFIER ON
 
            CREATE TABLE [dbo].[ContadorRegistro](
	            [Id_ContadorRegistro] [int] IDENTITY(1,1) NOT NULL,
	            [Dt_Log] [datetime] NULL,
	            [Id_Contador] [int] NULL,
	            [Valor] [int] NULL
            ) ON [PRIMARY]
 
            /****** Object:  Table [dbo].[GrupoDeMail]    Script Date: 05/04/2020 20:25:57 ******/
            SET ANSI_NULLS ON
 
            SET QUOTED_IDENTIFIER ON
 
            CREATE TABLE [dbo].[GrupoDeMail](
	            [Id] [int] IDENTITY(1,1) NOT FOR REPLICATION NOT NULL,
	            [Nome] [varchar](200) NOT NULL,
             CONSTRAINT [PK_dbo.SmartGrupos] PRIMARY KEY CLUSTERED 
            (
	            [Id] ASC
            )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
            ) ON [PRIMARY]
 
            /****** Object:  Table [dbo].[GrupoDeMailLista]    Script Date: 05/04/2020 20:25:57 ******/
            SET ANSI_NULLS ON
 
            SET QUOTED_IDENTIFIER ON
 
            CREATE TABLE [dbo].[GrupoDeMailLista](
	            [Id] [int] IDENTITY(1,1) NOT FOR REPLICATION NOT NULL,
	            [IdGrupoDeMail] [int] NOT NULL,
	            [Nome] [varchar](200) NOT NULL,
	            [Email] [varchar](200) NOT NULL,
             CONSTRAINT [PK_GrupoDeMailLista] PRIMARY KEY CLUSTERED 
            (
	            [Id] ASC
            )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
            ) ON [PRIMARY]


            CREATE TABLE [dbo].[HistIndexDefragExclusion](
	            [databaseID] [int] NOT NULL,
	            [databaseName] [nvarchar](128) NOT NULL,
	            [objectID] [int] NOT NULL,
	            [objectName] [nvarchar](128) NOT NULL,
	            [indexID] [int] NOT NULL,
	            [indexName] [nvarchar](128) NOT NULL,
	            [exclusionMask] [int] NOT NULL,
             CONSTRAINT [PK_indexDefragExclusion_v40] PRIMARY KEY CLUSTERED 
            (
	            [databaseID] ASC,
	            [objectID] ASC,
	            [indexID] ASC
            )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
            ) ON [PRIMARY]

            CREATE TABLE [dbo].[HistIndexDefragLog](
	            [indexDefrag_id] [int] IDENTITY(1,1) NOT NULL,
	            [databaseID] [int] NOT NULL,
	            [databaseName] [nvarchar](128) NOT NULL,
	            [objectID] [int] NOT NULL,
	            [objectName] [nvarchar](128) NOT NULL,
	            [indexID] [int] NOT NULL,
	            [indexName] [nvarchar](128) NOT NULL,
	            [partitionNumber] [smallint] NOT NULL,
	            [fragmentation] [float] NOT NULL,
	            [page_count] [int] NOT NULL,
	            [dateTimeStart] [datetime] NOT NULL,
	            [dateTimeEnd] [datetime] NULL,
	            [durationSeconds] [int] NULL,
	            [sqlStatement] [varchar](4000) NULL,
	            [errorMessage] [varchar](1000) NULL,
	            [fillfactor] [int] NULL,
             CONSTRAINT [PK_indexDefragLog_v40] PRIMARY KEY CLUSTERED 
            (
	            [indexDefrag_id] ASC
            )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, FILLFACTOR = 100) ON [PRIMARY]
            ) ON [PRIMARY]

            CREATE TABLE [dbo].[HisIndexDefragStatus](
	            [databaseID] [int] NOT NULL,
	            [databaseName] [nvarchar](128) NOT NULL,
	            [objectID] [int] NOT NULL,
	            [indexID] [int] NOT NULL,
	            [partitionNumber] [smallint] NOT NULL,
	            [fragmentation] [float] NOT NULL,
	            [page_count] [int] NOT NULL,
	            [range_scan_count] [bigint] NOT NULL,
	            [schemaName] [nvarchar](128) NULL,
	            [objectName] [nvarchar](128) NULL,
	            [indexName] [nvarchar](128) NULL,
	            [scanDate] [datetime] NOT NULL,
	            [defragDate] [datetime] NULL,
	            [printStatus] [bit] NOT NULL,
	            [exclusionMask] [int] NOT NULL,
             CONSTRAINT [PK_indexDefragStatus_v40] PRIMARY KEY CLUSTERED 
            (
	            [databaseID] ASC,
	            [objectID] ASC,
	            [indexID] ASC,
	            [partitionNumber] ASC
            )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
            ) ON [PRIMARY]


            CREATE TABLE [dbo].[HistIndexDefragTablesToExclude](
	            [TableName] [varchar](max) NOT NULL,
	            [DB] [varchar](200) NULL,
	            [DBID] [int] NULL,
	            [ObjectID] [int] NULL
            ) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

            CREATE TABLE [dbo].[HistoricoAlwaysOn](
	            [date_log] [datetime] NULL,
	            [replica_server_name] [varchar](256) NULL,
	            [database_name] [sysname] NULL,
	            [ag_name] [sysname] NULL,
	            [is_local] [bit] NULL,
	            [is_primary_replica] [bit] NULL,
	            [synchronization_state_desc] [varchar](60) NULL,
	            [is_commit_participant] [bit] NULL,
	            [synchronization_health_desc] [varchar](60) NULL,
	            [recovery_lsn] [numeric](25, 0) NULL,
	            [truncation_lsn] [numeric](25, 0) NULL,
	            [last_sent_lsn] [numeric](25, 0) NULL,
	            [last_sent_time] [datetime] NULL,
	            [last_received_lsn] [numeric](25, 0) NULL,
	            [last_received_time] [datetime] NULL,
	            [last_hardened_lsn] [numeric](25, 0) NULL,
	            [last_hardened_time] [datetime] NULL,
	            [last_redone_lsn] [numeric](25, 0) NULL,
	            [last_redone_time] [datetime] NULL,
	            [log_send_queue_size] [bigint] NULL,
	            [log_send_rate] [bigint] NULL,
	            [redo_queue_size] [bigint] NULL,
	            [redo_rate] [bigint] NULL,
	            [filestream_send_rate] [bigint] NULL,
	            [end_of_log_lsn] [numeric](25, 0) NULL,
	            [last_commit_lsn] [numeric](25, 0) NULL,
	            [last_commit_time] [datetime] NULL
            ) ON [PRIMARY]

            CREATE TABLE [dbo].[HistoricoAuditLogins](
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
                ) ON [PRIMARY]
             CREATE CLUSTERED INDEX idx_HistoricoAuditLogins ON dbo.HistoricoAuditLogins(Id);


            /****** Object:  Table [dbo].[HistoricoDBCC]    Script Date: 10/02/2016 16:41:04 ******/

            SET ANSI_NULLS ON

            SET QUOTED_IDENTIFIER ON

            CREATE TABLE [dbo].[HistoricoDBCC](
	            [ServerName] [varchar](100) NULL,
	            [DatabaseName] [varchar](256) NULL,
	            [Error] [varchar](100) NULL,
	            [Level] [varchar](100) NULL,
	            [State] [varchar](100) NULL,
	            [MessageText] [varchar](7000) NULL,
	            [RepairLevel] [varchar](100) NULL,
	            [Status] [varchar](100) NULL,
	            [DbId] [varchar](100) NULL,
	            [DbFragId] [varchar](100) NULL,
	            [ObjectId] [varchar](100) NULL,
	            [IndexId] [varchar](100) NULL,
	            [PartitionId] [varchar](100) NULL,
	            [AllocUnitId] [varchar](100) NULL,
	            [RidDbld] [varchar](100) NULL,
	            [RidPruId] [varchar](100) NULL,
	            [File] [varchar](100) NULL,
	            [Page] [varchar](100) NULL,
	            [Slot] [varchar](100) NULL,
	            [RefDBId] [varchar](100) NULL,
	            [RefPruId] [varchar](100) NULL,
	            [RefFile] [varchar](100) NULL,
	            [RefPage] [varchar](100) NULL,
	            [RefSlot] [varchar](100) NULL,
	            [Allocation] [varchar](100) NULL,
	            [TimeStamp] [datetime] NULL,
	            [Id] [varchar](100) NULL,
	            [IndId] [varchar](100) NULL
            ) ON [PRIMARY]


            /****** Object:  Table [dbo].[HistoricoErrosDB]    Script Date: 05/04/2020 20:25:57 ******/
            SET ANSI_NULLS ON
 
            SET QUOTED_IDENTIFIER ON
 
            CREATE TABLE [dbo].[HistoricoErrosDB](
	            [DataEvento] [datetime] NULL,
	            [SessionID] [int] NULL,
	            [DatabaseName] [varchar](100) NULL,
	            [SessionUsername] [varchar](100) NULL,
	            [ClientHostname] [varchar](100) NULL,
	            [ClientAppName] [varchar](100) NULL,
	            [ErrorNumber] [int] NULL,
	            [Severity] [int] NULL,
	            [State] [int] NULL,
	            [SqlText] [xml] NULL,
	            [message] [varchar](max) NULL
            ) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
 
            /****** Object:  Table [dbo].[HistoricoFragmentacaoIndice]    Script Date: 05/04/2020 20:25:57 ******/
            SET ANSI_NULLS ON
 
            SET QUOTED_IDENTIFIER ON
 
            CREATE TABLE [dbo].[HistoricoFragmentacaoIndice](
	            [Id_Hitorico_Fragmentacao_Indice] [int] IDENTITY(1,1) NOT NULL,
	            [Dt_Referencia] [datetime] NULL,
	            [Id_Servidor] [smallint] NULL,
	            [Id_BaseDados] [smallint] NULL,
	            [Id_Tabela] [int] NULL,
	            [Nm_Indice] [varchar](1000) NULL,
	            [Nm_Schema] [varchar](50) NULL,
	            [Avg_Fragmentation_In_Percent] [numeric](5, 2) NULL,
	            [Page_Count] [int] NULL,
	            [Fill_Factor] [tinyint] NULL,
	            [Fl_Compressao] [tinyint] NULL
            ) ON [PRIMARY]

            /****** Object:  Table [dbo].[HistoricoQueue]    Script Date: 03/05/2020 02:40:32 ******/
            SET ANSI_NULLS ON

            SET QUOTED_IDENTIFIER ON

            CREATE TABLE [dbo].[HistoricoQueue](
	            [Id] [int] IDENTITY(1,1) NOT NULL,
	            [ServerName] [varchar](100) NULL,
	            [DatabaseName] [varchar](100) NULL,
	            [Queue] [varchar](100) NULL,
	            [Status] [varchar](100) NULL,
	            [OldestNotDelivered] [varchar](100) NULL,
	            [QuantityNotDelivered] [varchar](100) NULL,
	            [statusQueueActivated] [varchar](100) NULL,
	            [NeedsRestart] [varchar](100) NULL,
	            [DescriptionAction] [varchar](100) NULL,
	            [Data] datetime2 NULL,

            PRIMARY KEY CLUSTERED 
            (
	            [Id] ASC
            )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
            ) ON [PRIMARY]

 
            /****** Object:  Table [dbo].[HistoricoSuspectPages]    Script Date: 05/04/2020 20:25:57 ******/
            SET ANSI_NULLS ON
 
            SET QUOTED_IDENTIFIER ON
 
            CREATE TABLE [dbo].[HistoricoSuspectPages](
	            [database_id] [int] NOT NULL,
	            [file_id] [int] NOT NULL,
	            [page_id] [bigint] NOT NULL,
	            [event_type] [int] NOT NULL,
	            [Dt_Corrupcao] [datetime] NOT NULL
            ) ON [PRIMARY]
 
            /****** Object:  Table [dbo].[HistoricoTamanhoTabela]    Script Date: 05/04/2020 20:25:57 ******/
            SET ANSI_NULLS ON
 
            SET QUOTED_IDENTIFIER ON
 
            CREATE TABLE [dbo].[HistoricoTamanhoTabela](
	            [Id_Historico_Tamanho] [int] IDENTITY(1,1) NOT NULL,
	            [Id_Servidor] [smallint] NULL,
	            [Id_BaseDados] [smallint] NULL,
	            [Id_Tabela] [int] NULL,
	            [Nm_Drive] [char](1) NULL,
	            [Nr_Tamanho_Total] [numeric](9, 2) NULL,
	            [Nr_Tamanho_Dados] [numeric](9, 2) NULL,
	            [Nr_Tamanho_Indice] [numeric](9, 2) NULL,
	            [Qt_Linhas] [bigint] NULL,
	            [Dt_Referencia] [date] NULL,
             CONSTRAINT [PK_HistoricoTamanhoTabela] PRIMARY KEY CLUSTERED 
            (
	            [Id_Historico_Tamanho] ASC
            )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
            ) ON [PRIMARY]
 
            /****** Object:  Table [dbo].[HistoricoUtilizacaoArquivo]    Script Date: 05/04/2020 20:25:57 ******/
            SET ANSI_NULLS ON
 
            SET QUOTED_IDENTIFIER ON
 
            CREATE TABLE [dbo].[HistoricoUtilizacaoArquivo](
	            [Nm_Database] [nvarchar](128) NULL,
	            [file_id] [smallint] NOT NULL,
	            [io_stall_read_ms] [bigint] NOT NULL,
	            [num_of_reads] [bigint] NOT NULL,
	            [avg_read_stall_ms] [numeric](10, 1) NULL,
	            [io_stall_write_ms] [bigint] NOT NULL,
	            [num_of_writes] [bigint] NOT NULL,
	            [avg_write_stall_ms] [numeric](10, 1) NULL,
	            [io_stalls] [bigint] NULL,
	            [total_io] [bigint] NULL,
	            [avg_io_stall_ms] [numeric](10, 1) NULL,
	            [Dt_Registro] [datetime] NOT NULL
            ) ON [PRIMARY]

            /****** Object:  Table [dbo].[HistoricoVersionamentoDB]    Script Date: 29/06/2020 13:46:05 ******/
            SET ANSI_NULLS ON

            SET QUOTED_IDENTIFIER ON

            CREATE TABLE [dbo].[HistoricoVersionamentoDB](
	            [Id] [int] IDENTITY(1,1) NOT NULL,
	            [DataEvento] [datetime] NULL,
	            [TipoEvento] [varchar](30) NULL,
	            [Database] [varchar](50) NULL,
	            [Usuario] [varchar](100) NULL,
	            [Host] [varchar](100) NULL,
	            [Schema] [varchar](20) NULL,
	            [Objeto] [varchar](100) NULL,
	            [TipoObjeto] [varchar](20) NULL,
	            [DesQuery] [xml] NULL
            ) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

            CREATE CLUSTERED INDEX [idx_HistoricoVersionamentoDB] ON [dbo].[HistoricoVersionamentoDB]
            (
	            [Id] ASC
            )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, FILLFACTOR = 95) ON [PRIMARY]
 
	        
            /****** Object:  Table [dbo].[HistoricoUsuariosAD]    Script Date: 05/04/2020 20:25:57 ******/
            SET ANSI_NULLS ON
 
            SET QUOTED_IDENTIFIER ON
            CREATE TABLE [dbo].[HistoricoUsuariosAD](
		        [Id] [int] IDENTITY(1,1) NOT NULL,
		        [Usuario] [varchar](300) NULL,
		        [Grupo] [varchar](200) NULL,
		        [AdsPath] [varchar](800) NULL,
		        [DataColeta] [datetime2] NULL,
		        CONSTRAINT [PK_HistoricoUsuariosAD] PRIMARY KEY CLUSTERED
	            (
			        [Id] ASC
                )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
                ) ON [PRIMARY] 
	            


            /****** Object:  Table [dbo].[HistoricoWaitsStats]    Script Date: 05/04/2020 20:25:57 ******/
            SET ANSI_NULLS ON
 
            SET QUOTED_IDENTIFIER ON
 
            CREATE TABLE [dbo].[HistoricoWaitsStats](
	            [Id_HistoricoWaitsStats] [int] IDENTITY(1,1) NOT NULL,
	            [Dt_Referencia] [datetime] NULL,
	            [WaitType] [varchar](60) NOT NULL,
	            [Wait_S] [decimal](14, 2) NULL,
	            [Resource_S] [decimal](14, 2) NULL,
	            [Signal_S] [decimal](14, 2) NULL,
	            [WaitCount] [bigint] NULL,
	            [Percentage] [decimal](4, 2) NULL,
	            [Id_Coleta] [int] NULL
            ) ON [PRIMARY]
 
            /****** Object:  Table [dbo].[HitoricoFragmentacaoIndice]    Script Date: 05/04/2020 20:25:57 ******/
            SET ANSI_NULLS ON
 
            SET QUOTED_IDENTIFIER ON
 
            CREATE TABLE [dbo].[HitoricoFragmentacaoIndice](
	            [Id] [int] IDENTITY(1,1) NOT NULL,
	            [DataReferencia] [datetime] NULL,
	            [Servidor] [varchar](50) NULL,
	            [NomeDatabase] [varchar](50) NULL,
	            [NomeTabela] [varchar](50) NULL,
	            [NomeIndice] [varchar](70) NULL,
	            [Avg_Fragmentation_In_Percent] [numeric](5, 2) NULL,
	            [Page_Count] [int] NULL,
	            [Fill_Factor] [tinyint] NULL
            ) ON [PRIMARY]
 
            /****** Object:  Table [dbo].[HorariosGrupoPeriodo]    Script Date: 05/04/2020 20:25:57 ******/
            SET ANSI_NULLS ON
 
            SET QUOTED_IDENTIFIER ON
 
            CREATE TABLE [dbo].[HorariosGrupoPeriodo](
	            [Id] [int] IDENTITY(1,1) NOT NULL,
	            [IdHorariosJobs] [int] NOT NULL,
	            [IdPeriodo] [int] NOT NULL,
	            [IdPeriodoSemana] [int] NULL,
            PRIMARY KEY CLUSTERED 
            (
	            [Id] ASC
            )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
            ) ON [PRIMARY]
 
            /****** Object:  Table [dbo].[HorariosJobs]    Script Date: 05/04/2020 20:25:57 ******/
            SET ANSI_NULLS ON
 
            SET QUOTED_IDENTIFIER ON
 
            CREATE TABLE [dbo].[HorariosJobs](
	            [Id] [int] IDENTITY(1,1) NOT NULL,
	            [Nome] [varchar](400) NOT NULL,
	            [Ativo] [int] NOT NULL,
	            [FreqSubdayType] [int] NOT NULL,
	            [DataAtivacao] [int] NOT NULL,
	            [DataDesativacao] [int] NULL,
	            [HoraAtivacao] [int] NULL,
	            [HoraDesativacao] [int] NULL,
	            [HoraExecucao] [int] NULL,
	            [DataCriacao] [datetime] NOT NULL,
	            [DataAlteracao] [datetime] NULL,
	            [version] [int] NULL,
            PRIMARY KEY CLUSTERED 
            (
	            [Id] ASC
            )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
            ) ON [PRIMARY]
 
            /****** Object:  Table [dbo].[JobsDB]    Script Date: 05/04/2020 20:25:57 ******/
            SET ANSI_NULLS ON
 
            SET QUOTED_IDENTIFIER ON
 
            CREATE TABLE [dbo].[JobsDB](
	            [Id] [int] IDENTITY(1,1) NOT NULL,
	            [Nome] [varchar](100) NULL,
	            [DataCriacao] [datetime2](7) NULL,
	            [HorarioJob] [int] NOT NULL,
	            [IdPeriodo] [int] NOT NULL,
	            [IdPeriodoSemana] [int] NOT NULL,
             CONSTRAINT [PK_dbo.JobsDB] PRIMARY KEY CLUSTERED 
            (
	            [Id] ASC
            )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
            ) ON [PRIMARY]
 
            /****** Object:  Table [dbo].[LayoutHtmlCss]    Script Date: 05/04/2020 20:25:57 ******/
            SET ANSI_NULLS ON
 
            SET QUOTED_IDENTIFIER ON
 
            CREATE TABLE [dbo].[LayoutHtmlCss](
	            [IdLayout] [int] IDENTITY(1,1) NOT NULL,
	            [NomeLayout] [varchar](100) NOT NULL,
	            [DescricaoCSS] [varchar](max) NOT NULL,
            PRIMARY KEY CLUSTERED 
            (
	            [IdLayout] ASC
            )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY],
            UNIQUE NONCLUSTERED 
            (
	            [NomeLayout] ASC
            )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
            ) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
 
            /****** Object:  Table [dbo].[LogEmail]    Script Date: 05/04/2020 20:25:57 ******/
            SET ANSI_NULLS ON
 
            SET QUOTED_IDENTIFIER ON
 
            CREATE TABLE [dbo].[LogEmail](
	            [IdLog] [bigint] IDENTITY(1,1) NOT NULL,
	            [DataLog] [datetime] NULL,
	            [Destinatario] [varchar](max) NOT NULL,
	            [Assunto] [varchar](max) NULL,
	            [Mensagem] [varchar](max) NULL,
	            [Arquivos] [varchar](max) NULL,
	            [Usuario] [varchar](100) NULL
            ) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
 
            /****** Object:  Table [dbo].[LogErro]    Script Date: 05/04/2020 20:25:57 ******/
            SET ANSI_NULLS ON
 
            SET QUOTED_IDENTIFIER ON
 
            CREATE TABLE [dbo].[LogErro](
	            [IdErro] [int] IDENTITY(1,1) NOT NULL,
	            [DataErro] [datetime] NULL,
	            [NomeObjeto] [varchar](100) NULL,
	            [Erro] [varchar](max) NULL,
             CONSTRAINT [PK_LogErro] PRIMARY KEY CLUSTERED 
            (
	            [IdErro] ASC
            )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
            ) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]


            /****** Object:  Table [dbo].[LogQueue]    Script Date: 14/04/2020 15:30:28 ******/
            SET ANSI_NULLS ON

            SET QUOTED_IDENTIFIER ON

            CREATE TABLE [dbo].[LogQueue](
	            [id] [int] IDENTITY(1,1) NOT NULL,
	            [XMLMessage] [xml] NULL,
	            [JSON_Message] Varchar(MAX) NULL,
	            [QueueInputTime] [datetime] NULL,
            PRIMARY KEY CLUSTERED 
            (
	            [id] ASC
            )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
            ) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

            /****** Object:  Table [dbo].[MailAssinatura]    Script Date: 05/04/2020 20:25:57 ******/
            SET ANSI_NULLS ON
 
            SET QUOTED_IDENTIFIER ON
 
            CREATE TABLE [dbo].[MailAssinatura](
	            [Id] [int] IDENTITY(1,1) NOT NULL,
	            [Assinatura] [varchar](max) NOT NULL,
	            [Descricao] [nvarchar](256) NOT NULL,
	            [Ativo] [bit] NOT NULL,
	            [DataCriacao] [datetime2](7) NULL,
             CONSTRAINT [PK_dbo.MailAssinatura] PRIMARY KEY CLUSTERED 
            (
	            [Id] ASC
            )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
            ) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
 
            /****** Object:  Table [dbo].[PasswordAudit]    Script Date: 05/04/2020 20:25:57 ******/
            SET ANSI_NULLS ON
 
            SET QUOTED_IDENTIFIER ON
 
            CREATE TABLE [dbo].[PasswordAudit](
	            [ID] [int] IDENTITY(1,1) NOT NULL,
	            [ServerName] [varchar](50) NOT NULL,
	            [SQLLogin] [varchar](50) NOT NULL,
	            [IsSysAdmin] [bit] NOT NULL,
	            [IsWeakPassword] [bit] NOT NULL,
	            [WeakPassword] [varchar](250) NULL,
	            [PwdLastUpdate] [datetime2](7) NOT NULL,
	            [PwdDaysOld] [int] NULL,
	            [DateAudited] [datetime2](7) NOT NULL
            ) ON [PRIMARY]
 
            /****** Object:  Table [dbo].[Periodo]    Script Date: 05/04/2020 20:25:57 ******/
            SET ANSI_NULLS ON
 
            SET QUOTED_IDENTIFIER ON
 
            CREATE TABLE [dbo].[Periodo](
	            [Id] [int] IDENTITY(1,1) NOT NULL,
	            [Nome] [varchar](100) NULL,
             CONSTRAINT [PK_dbo.Periodo] PRIMARY KEY CLUSTERED 
            (
	            [Id] ASC
            )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
            ) ON [PRIMARY]
 
            /****** Object:  Table [dbo].[PeriodoSemana]    Script Date: 05/04/2020 20:25:57 ******/
            SET ANSI_NULLS ON
 
            SET QUOTED_IDENTIFIER ON
 
            CREATE TABLE [dbo].[PeriodoSemana](
	            [Id] [int] IDENTITY(1,1) NOT NULL,
	            [Nome] [varchar](100) NULL,
             CONSTRAINT [PK_dbo.PeriodoSemana] PRIMARY KEY CLUSTERED 
            (
	            [Id] ASC
            )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
            ) ON [PRIMARY]
 
            /****** Object:  Table [dbo].[ResultadoEspacodisco]    Script Date: 05/04/2020 20:25:57 ******/
            SET ANSI_NULLS ON
 
            SET QUOTED_IDENTIFIER ON
 
            CREATE TABLE [dbo].[ResultadoEspacodisco](
	            [Drive] [varchar](10) NULL,
	            [Tamanho (MB)] [int] NULL,
	            [Usado (MB)] [int] NULL,
	            [Livre (MB)] [int] NULL,
	            [Livre (%)] [int] NULL,
	            [Usado (%)] [int] NULL,
	            [Ocupado SQL (MB)] [int] NULL,
	            [Data] [smalldatetime] NULL
            ) ON [PRIMARY]
 
            /****** Object:  Table [dbo].[ResultadoProc]    Script Date: 05/04/2020 20:25:57 ******/
            SET ANSI_NULLS ON
 
            SET QUOTED_IDENTIFIER ON
 
            CREATE TABLE [dbo].[ResultadoProc](
	            [Duração] [varchar](50) NULL,
	            [database_name] [varchar](50) NULL,
	            [login_name] [varchar](50) NULL,
	            [host_name] [varchar](50) NULL,
	            [start_time] [varchar](50) NULL,
	            [status] [varchar](50) NULL,
	            [session_id] [varchar](50) NULL,
	            [blocking_session_id] [varchar](50) NULL,
	            [Wait] [varchar](50) NULL,
	            [open_tran_count] [varchar](50) NULL,
	            [CPU] [varchar](50) NULL,
	            [reads] [varchar](50) NULL,
	            [writes] [varchar](50) NULL,
	            [sql_command] [varchar](max) NULL
            ) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
 
            /****** Object:  Table [dbo].[ResultadoProcBlock]    Script Date: 05/04/2020 20:25:57 ******/
            SET ANSI_NULLS ON
 
            SET QUOTED_IDENTIFIER ON
 
            CREATE TABLE [dbo].[ResultadoProcBlock](
	            [Nr_Nivel_Lock] [varchar](50) NULL,
	            [Duração] [varchar](50) NULL,
	            [database_name] [varchar](50) NULL,
	            [login_name] [varchar](50) NULL,
	            [host_name] [varchar](50) NULL,
	            [start_time] [varchar](50) NULL,
	            [status] [varchar](50) NULL,
	            [session_id] [varchar](50) NULL,
	            [blocking_session_id] [varchar](50) NULL,
	            [Wait] [varchar](50) NULL,
	            [open_tran_count] [varchar](50) NULL,
	            [CPU] [varchar](50) NULL,
	            [reads] [varchar](50) NULL,
	            [writes] [varchar](50) NULL,
	            [sql_command] [varchar](max) NULL
            ) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
 
            /****** Object:  Table [dbo].[ResultadoTraceLog]    Script Date: 05/04/2020 20:25:57 ******/
            SET ANSI_NULLS ON
 
            SET QUOTED_IDENTIFIER ON
 
            CREATE TABLE [dbo].[ResultadoTraceLog](
	            [TextData] [text] NULL,
	            [NTUserName] [varchar](128) NULL,
	            [HostName] [varchar](128) NULL,
	            [ApplicationName] [varchar](128) NULL,
	            [LoginName] [varchar](128) NULL,
	            [SPID] [int] NULL,
	            [Duration] [numeric](15, 2) NULL,
	            [StartTime] [datetime] NULL,
	            [EndTime] [datetime] NULL,
	            [Reads] [int] NULL,
	            [Writes] [int] NULL,
	            [CPU] [int] NULL,
	            [ServerName] [varchar](128) NULL,
	            [DataBaseName] [varchar](128) NULL,
	            [RowCounts] [int] NULL,
	            [SessionLoginName] [varchar](128) NULL
            ) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
 
            /****** Object:  Table [dbo].[ServerAudi]    Script Date: 05/04/2020 20:25:57 ******/
            SET ANSI_NULLS ON
 
            SET QUOTED_IDENTIFIER ON
 
            CREATE TABLE [dbo].[ServerAudi](
	            [Id] [int] IDENTITY(1,1) NOT NULL,
	            [DataEvento] [datetime] NULL,
	            [serverInstanceName] [varchar](100) NULL,
	            [DatabaseName] [varchar](100) NULL,
	            [ActionId] [varchar](100) NULL,
	            [Session] [varchar](100) NULL,
	            [statement] [varchar](max) NULL
            ) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
 
            /****** Object:  Table [dbo].[Servidor]    Script Date: 05/04/2020 20:25:57 ******/
            SET ANSI_NULLS ON
 
            SET QUOTED_IDENTIFIER ON
 
            CREATE TABLE [dbo].[Servidor](
	            [Id_Servidor] [int] IDENTITY(1,1) NOT NULL,
	            [Nm_Servidor] [varchar](100) NOT NULL,
             CONSTRAINT [PK_Servidor] PRIMARY KEY CLUSTERED 
            (
	            [Id_Servidor] ASC
            )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
            ) ON [PRIMARY]
 
            /****** Object:  Table [dbo].[SQLTraceLog]    Script Date: 05/04/2020 20:25:57 ******/
            SET ANSI_NULLS ON
 
            SET QUOTED_IDENTIFIER ON
 
            CREATE TABLE [dbo].[SQLTraceLog](
	            [TextData] [varchar](max) NULL,
	            [NTUserName] [varchar](128) NULL,
	            [HostName] [varchar](128) NULL,
	            [ApplicationName] [varchar](128) NULL,
	            [LoginName] [varchar](128) NULL,
	            [SPID] [int] NULL,
	            [Duration] [numeric](15, 2) NULL,
	            [StartTime] [datetime] NULL,
	            [EndTime] [datetime] NULL,
	            [ServerName] [varchar](128) NULL,
	            [Reads] [int] NULL,
	            [Writes] [int] NULL,
	            [CPU] [int] NULL,
	            [DataBaseName] [varchar](128) NULL,
	            [RowCounts] [int] NULL,
	            [SessionLoginName] [varchar](128) NULL
            ) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

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

 
            /****** Object:  Table [dbo].[Tabela]    Script Date: 05/04/2020 20:25:57 ******/
            SET ANSI_NULLS ON
 
            SET QUOTED_IDENTIFIER ON
 
            CREATE TABLE [dbo].[Tabela](
	            [Id_Tabela] [int] IDENTITY(1,1) NOT NULL,
	            [Nm_Tabela] [varchar](1000) NULL,
             CONSTRAINT [PK_Tabela] PRIMARY KEY CLUSTERED 
            (
	            [Id_Tabela] ASC
            )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
            ) ON [PRIMARY]
 
            /****** Object:  Table [dbo].[Testedb]    Script Date: 05/04/2020 20:25:57 ******/
            SET ANSI_NULLS ON
 
            SET QUOTED_IDENTIFIER ON
 
            CREATE TABLE [dbo].[Testedb](
	            [Id] [int] IDENTITY(1,1) NOT NULL,
	            [Nome] [varchar](1000) NULL,
	            [DateTest] [datetime] NULL,
             CONSTRAINT [PK_Testedb] PRIMARY KEY CLUSTERED 
            (
	            [Id] ASC
            )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
            ) ON [PRIMARY]
 
			GO
			CREATE SCHEMA job
			
			GO

			SET ANSI_NULLS ON
			GO

			SET QUOTED_IDENTIFIER ON
			GO
			
			CREATE TABLE [job].[Job](
				[IdJob] [int] IDENTITY(1,1) NOT NULL,
				[Nome] [varchar](128) NULL,
				[Descricao] [varchar](128) NULL,
				[Solicitante] [varchar](30) NULL,
				[Frequencia] [tinyint] NULL,
				[DiaUtil] [bit] NULL,
				[ExecIntervalo] [bit] NULL,
				[Comando] [nvarchar](max) NULL,
				[DataInicio] [date] NULL,
				[DataFim] [date] NULL,
				[HoraIni] [varchar](6) NULL,
				[HoraFim] [varchar](6) NULL,
				[Ativo] [bit] NULL,
				[UltimaExec] [datetime] NULL,
			 CONSTRAINT [Pk_Job] PRIMARY KEY CLUSTERED 
			(
				[IdJob] ASC
			)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
			) ON [PRIMARY] 
			GO
			
			ALTER TABLE [job].[Job] ADD  DEFAULT ((0)) FOR [DiaUtil]
			GO
			
			ALTER TABLE [job].[Job] ADD  DEFAULT ((0)) FOR [ExecIntervalo]
			GO
			
			ALTER TABLE [job].[Job] ADD  DEFAULT (getdate()) FOR [DataInicio]
			GO
			
			ALTER TABLE [job].[Job] ADD  DEFAULT ('2099-12-31') FOR [DataFim]
			GO
			
			ALTER TABLE [job].[Job] ADD  DEFAULT ((1)) FOR [Ativo]
			GO
			
			
			SET ANSI_NULLS ON
			GO
			
			SET QUOTED_IDENTIFIER ON
			GO
			
			CREATE TABLE [job].[JobAgendamento](
				[IdJobAgend] [int] IDENTITY(1,1) NOT NULL,
				[JobId] [int] NULL,
				[DiaSemana] [tinyint] NULL,
				[DataExec] [date] NULL,
				[HoraExec] [varchar](6) NULL,
				[MensalExec] [tinyint] NULL,
				[Intervalo] [int] NULL,
			 CONSTRAINT [Pk_JobAgend] PRIMARY KEY CLUSTERED 
			(
				[IdJobAgend] ASC
			)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
			) ON [PRIMARY]
			GO
			
			
			SET ANSI_NULLS ON
			GO
			
			SET QUOTED_IDENTIFIER ON
			GO
			
			CREATE TABLE [job].[JobHistorico](
				[IdJobHist] [int] IDENTITY(1,1) NOT NULL,
				[JobId] [int] NULL,
				[DataExec] [datetime] NULL,
				[TempoExec] [int] NULL,
				[MessageError] [nvarchar](4000) NULL,
				[Error] [bit] NULL,
				[Enviado] [bit] NULL,
			 CONSTRAINT [Pk_JobHist] PRIMARY KEY CLUSTERED 
			(
				[IdJobHist] ASC
			)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
			) ON [PRIMARY]
			GO
			
			ALTER TABLE [job].[JobHistorico] ADD  DEFAULT ((0)) FOR [Error]
			GO
			
			ALTER TABLE [job].[JobHistorico] ADD  DEFAULT ((0)) FOR [Enviado]
			GO

            ALTER TABLE [dbo].[Alerta] ADD  DEFAULT (getdate()) FOR [Dt_Alerta]

            ALTER TABLE [dbo].[AlertaEnvio] ADD  DEFAULT (getdate()) FOR [DataCriação]

            ALTER TABLE [dbo].[AlertaEnvio]  WITH NOCHECK ADD  CONSTRAINT [FK_AlertaParametro_idAlertaParametro] FOREIGN KEY([IdAlertaParametro])
            REFERENCES [dbo].[AlertaParametro] ([Id_AlertaParametro])

            ALTER TABLE [dbo].[AlertaEnvio] CHECK CONSTRAINT [FK_AlertaParametro_idAlertaParametro]
 
            ALTER TABLE [dbo].[AlertaMsgToken] ADD  CONSTRAINT [C_DataInclusao]  DEFAULT (getdate()) FOR [DataInclusao]
 
            ALTER TABLE [dbo].[AlertaParametroMenssage] ADD  DEFAULT (getdate()) FOR [DataCriacao]
 
            ALTER TABLE [dbo].[AlertaStatusDatabases] ADD  DEFAULT (getdate()) FOR [DataAlerta]

            ALTER TABLE [dbo].[HistoricoDBCC] ADD  CONSTRAINT [DF_dbcchistory_TimeStamp]  DEFAULT (getdate()) FOR [TimeStamp]
 
            ALTER TABLE [dbo].[HistoricoWaitsStats] ADD  DEFAULT (getdate()) FOR [Dt_Referencia]
 
            ALTER TABLE [dbo].[HorariosJobs] ADD  DEFAULT (getdate()) FOR [DataCriacao]
 
            ALTER TABLE [dbo].[HorariosJobs] ADD  DEFAULT (getdate()) FOR [DataAlteracao]
 
            ALTER TABLE [dbo].[HorariosJobs] ADD  DEFAULT ((1)) FOR [version]
 
            ALTER TABLE [dbo].[JobsDB] ADD  DEFAULT (getdate()) FOR [DataCriacao]
 
            ALTER TABLE [dbo].[LogEmail] ADD  DEFAULT (getdate()) FOR [DataLog]
 
            ALTER TABLE [dbo].[LogErro] ADD  DEFAULT (getdate()) FOR [DataErro]
 
            ALTER TABLE [dbo].[PasswordAudit] ADD  DEFAULT ((0)) FOR [IsSysAdmin]
 
            ALTER TABLE [dbo].[PasswordAudit] ADD  DEFAULT ((0)) FOR [IsWeakPassword]
 
            ALTER TABLE [dbo].[PasswordAudit] ADD  DEFAULT (getdate()) FOR [DateAudited]
 
            ALTER TABLE [dbo].[Alerta]  WITH NOCHECK ADD  CONSTRAINT [FK_Alerta_AlertaParametro] FOREIGN KEY([Id_AlertaParametro])
            REFERENCES [dbo].[AlertaParametro] ([Id_AlertaParametro])
 
            ALTER TABLE [dbo].[Alerta] NOCHECK CONSTRAINT [FK_Alerta_AlertaParametro]
 
            ALTER TABLE [dbo].[AlertaMsgToken]  WITH CHECK ADD  CONSTRAINT [FK_AlertaMsgToken_AlertaParametro] FOREIGN KEY([Id])
            REFERENCES [dbo].[AlertaParametro] ([Id_AlertaParametro])
 
            ALTER TABLE [dbo].[AlertaMsgToken] CHECK CONSTRAINT [FK_AlertaMsgToken_AlertaParametro]
 
            -- ALTER TABLE [dbo].[AlertaParametro]  WITH CHECK ADD  CONSTRAINT [FK_AlertaParametro_AlertaMsgToken] FOREIGN KEY([Ds_Menssageiro_02])
            -- REFERENCES [dbo].[AlertaMsgToken] ([Id])
 
            -- ALTER TABLE [dbo].[AlertaParametro] CHECK CONSTRAINT [FK_AlertaParametro_AlertaMsgToken]
 
            -- ALTER TABLE [dbo].[AlertaParametro]  WITH CHECK ADD  CONSTRAINT [FK_AlertaParametro_AlertaMsgToken_01] FOREIGN KEY([Ds_Menssageiro_01])
            -- REFERENCES [dbo].[AlertaMsgToken] ([Id])
 
            -- ALTER TABLE [dbo].[AlertaParametro] CHECK CONSTRAINT [FK_AlertaParametro_AlertaMsgToken_01]
 
            -- ALTER TABLE [dbo].[AlertaParametro]  WITH CHECK ADD  CONSTRAINT [FK_AlertaParametro_MailAssinatura] FOREIGN KEY([IdMailAssinatura])
            -- REFERENCES [dbo].[MailAssinatura] ([Id])
 
            -- ALTER TABLE [dbo].[AlertaParametro] CHECK CONSTRAINT [FK_AlertaParametro_MailAssinatura]
 
            ALTER TABLE [dbo].[AlertaParametroMenssage]  WITH CHECK ADD  CONSTRAINT [dbo.AlertaParametroMenssage_IdAlertaParametro] FOREIGN KEY([IdAlertaParametro])
            REFERENCES [dbo].[AlertaParametro] ([Id_AlertaParametro])
 
            ALTER TABLE [dbo].[AlertaParametroMenssage] CHECK CONSTRAINT [dbo.AlertaParametroMenssage_IdAlertaParametro]
 
            ALTER TABLE [dbo].[GrupoDeMailLista]  WITH CHECK ADD  CONSTRAINT [FK_dbo.GrupoDeMailLista_IdGrupoDeMail] FOREIGN KEY([IdGrupoDeMail])
            REFERENCES [dbo].[GrupoDeMail] ([Id])
 
            ALTER TABLE [dbo].[GrupoDeMailLista] CHECK CONSTRAINT [FK_dbo.GrupoDeMailLista_IdGrupoDeMail]
 
            ALTER TABLE [dbo].[JobsDB]  WITH CHECK ADD  CONSTRAINT [FK_dbo.JobsDB_IdPeriodo] FOREIGN KEY([IdPeriodo])
            REFERENCES [dbo].[Periodo] ([Id])
 
            ALTER TABLE [dbo].[JobsDB] CHECK CONSTRAINT [FK_dbo.JobsDB_IdPeriodo]
 
            ALTER TABLE [dbo].[JobsDB]  WITH CHECK ADD  CONSTRAINT [FK_dbo.JobsDB_IdPeriodoSemana] FOREIGN KEY([IdPeriodoSemana])
            REFERENCES [dbo].[PeriodoSemana] ([Id])
 
            ALTER TABLE [dbo].[JobsDB] CHECK CONSTRAINT [FK_dbo.JobsDB_IdPeriodoSemana]

           ALTER TABLE [dbo].[LogQueue] ADD  DEFAULT (getdate()) FOR [QueueInputTime]

            ALTER TABLE [dbo].[HisIndexDefragStatus] ADD  CONSTRAINT [DF_index_printstatus]  DEFAULT ((0)) FOR [printStatus]

            ALTER TABLE [dbo].[HisIndexDefragStatus] ADD  CONSTRAINT [DF_index_exclusionMask]  DEFAULT ((0)) FOR [exclusionMask]

            ALTER TABLE [dbo].[HistoricoAuditLogins] ADD  DEFAULT ((0)) FOR [AlertaEnviado]

            GO
 
            ";

        }
    }
}
