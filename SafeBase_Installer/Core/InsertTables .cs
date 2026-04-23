using System;
using System.Collections.Generic;
using System.Text;
using SafeBase_Installer.Core;

namespace SafeBase_Installer
{
    class InsertTables
    {
        public static string Query(string use)
        {
            return
            @"
            USE "+ use + @"

            -- ALTER TABLE [dbo].[AlertaMsgToken] NOCHECK CONSTRAINT [FK_AlertaMsgToken_AlertaParametro]
 
            -- ALTER TABLE [dbo].[AlertaParametro] NOCHECK CONSTRAINT [FK_AlertaParametro_AlertaMsgToken]
 
            -- ALTER TABLE [dbo].[AlertaParametro] NOCHECK CONSTRAINT [FK_AlertaParametro_AlertaMsgToken_01]
 
            -- ALTER TABLE [dbo].[AlertaParametro] NOCHECK CONSTRAINT [FK_AlertaParametro_MailAssinatura]
 
            -- ALTER TABLE [dbo].[AlertaParametroMenssage] CHECK CONSTRAINT [dbo.AlertaParametroMenssage_IdAlertaParametro]

            SET IDENTITY_INSERT [dbo].[AlertaParametro] ON 
 
            INSERT [dbo].[AlertaParametro] ([Id_AlertaParametro], [Nm_Alerta], [Nm_Procedure], [Fl_Clear], [Vl_Parametro], [Ds_Metrica], [Nm_Empresa], [Ds_Email], [Ds_Caminho], [Ds_Caminho_Log], [IgnoraDatabase], [Ds_ProfileDBMail], [Ds_BodyFormatMail], [Ds_TipoMail], [IdMailAssinatura], [Ativo], [Ds_Menssageiro_01], [Ds_Menssageiro_02], [Ds_Menssageiro_03], [Ds_Menssageiro_04], [Ds_Menssageiro_05], [Ds_MSG], [Ds_Inclusao_Exclusao]) VALUES (1, N'Processo Bloqueado', N'stpAlertaProcessoBloqueado', 1, 2, N'Minutos', N'Facta', N'paulo.kuhn@facta.com.br', NULL, N'\Jobs\Reports', NULL, N'EnviaEmail', N'HTML', N'High', 1, 1, 2, 9, NULL, NULL, NULL, N'49353855', NULL)
 
            INSERT [dbo].[AlertaParametro] ([Id_AlertaParametro], [Nm_Alerta], [Nm_Procedure], [Fl_Clear], [Vl_Parametro], [Ds_Metrica], [Nm_Empresa], [Ds_Email], [Ds_Caminho], [Ds_Caminho_Log], [IgnoraDatabase], [Ds_ProfileDBMail], [Ds_BodyFormatMail], [Ds_TipoMail], [IdMailAssinatura], [Ativo], [Ds_Menssageiro_01], [Ds_Menssageiro_02], [Ds_Menssageiro_03], [Ds_Menssageiro_04], [Ds_Menssageiro_05], [Ds_MSG], [Ds_Inclusao_Exclusao]) VALUES (2, N'Arquivo de Log Full', N'stpAlertaArquivoLogFull', 1, 85, N'Percentual', N'Facta', N'paulo.kuhn@facta.com.br', NULL, N'\Jobs\Reports', NULL, N'EnviaEmail', N'HTML', N'High', 1, 1, 2, 9, NULL, NULL, NULL, N'49353855', NULL)
 
            INSERT [dbo].[AlertaParametro] ([Id_AlertaParametro], [Nm_Alerta], [Nm_Procedure], [Fl_Clear], [Vl_Parametro], [Ds_Metrica], [Nm_Empresa], [Ds_Email], [Ds_Caminho], [Ds_Caminho_Log], [IgnoraDatabase], [Ds_ProfileDBMail], [Ds_BodyFormatMail], [Ds_TipoMail], [IdMailAssinatura], [Ativo], [Ds_Menssageiro_01], [Ds_Menssageiro_02], [Ds_Menssageiro_03], [Ds_Menssageiro_04], [Ds_Menssageiro_05], [Ds_MSG], [Ds_Inclusao_Exclusao]) VALUES (3, N'Espaco Disco', N'stpAlertaEspacoDisco', 1, 85, N'Percentual', N'Facta', N'paulo.kuhn@facta.com.br', NULL, N'\Jobs\Reports', NULL, N'EnviaEmail', N'HTML', N'High', 1, 0, 2, 9, NULL, NULL, NULL, N'49353855', NULL)
 
            INSERT [dbo].[AlertaParametro] ([Id_AlertaParametro], [Nm_Alerta], [Nm_Procedure], [Fl_Clear], [Vl_Parametro], [Ds_Metrica], [Nm_Empresa], [Ds_Email], [Ds_Caminho], [Ds_Caminho_Log], [IgnoraDatabase], [Ds_ProfileDBMail], [Ds_BodyFormatMail], [Ds_TipoMail], [IdMailAssinatura], [Ativo], [Ds_Menssageiro_01], [Ds_Menssageiro_02], [Ds_Menssageiro_03], [Ds_Menssageiro_04], [Ds_Menssageiro_05], [Ds_MSG], [Ds_Inclusao_Exclusao]) VALUES (4, N'Consumo CPU', N'stpAlertaConsumoCPU', 1, 85, N'Percentual', N'Facta', N'paulo.kuhn@facta.com.br', NULL, N'\Jobs\Reports', NULL, N'EnviaEmail', N'HTML', N'High', 1, 1, 2, 9, NULL, NULL, NULL, N'49353855', NULL)
 
            INSERT [dbo].[AlertaParametro] ([Id_AlertaParametro], [Nm_Alerta], [Nm_Procedure], [Fl_Clear], [Vl_Parametro], [Ds_Metrica], [Nm_Empresa], [Ds_Email], [Ds_Caminho], [Ds_Caminho_Log], [IgnoraDatabase], [Ds_ProfileDBMail], [Ds_BodyFormatMail], [Ds_TipoMail], [IdMailAssinatura], [Ativo], [Ds_Menssageiro_01], [Ds_Menssageiro_02], [Ds_Menssageiro_03], [Ds_Menssageiro_04], [Ds_Menssageiro_05], [Ds_MSG], [Ds_Inclusao_Exclusao]) VALUES (5, N'Tempdb Utilizacao Arquivo MDF', N'stpAlertaTempdbUtilizacaoArquivoMDF', 1, 70, N'Percentual', N'Facta', N'paulo.kuhn@facta.com.br', NULL, N'\Jobs\Reports', NULL, N'EnviaEmail', N'HTML', N'High', 1, 1, 2, 9, NULL, NULL, NULL, N'49353855', NULL)
 
            INSERT [dbo].[AlertaParametro] ([Id_AlertaParametro], [Nm_Alerta], [Nm_Procedure], [Fl_Clear], [Vl_Parametro], [Ds_Metrica], [Nm_Empresa], [Ds_Email], [Ds_Caminho], [Ds_Caminho_Log], [IgnoraDatabase], [Ds_ProfileDBMail], [Ds_BodyFormatMail], [Ds_TipoMail], [IdMailAssinatura], [Ativo], [Ds_Menssageiro_01], [Ds_Menssageiro_02], [Ds_Menssageiro_03], [Ds_Menssageiro_04], [Ds_Menssageiro_05], [Ds_MSG], [Ds_Inclusao_Exclusao]) VALUES (6, N'Conexão SQL Server', N'stpAlertaConexaoSQLServer', 1, 2000, N'Quantidade', N'Facta', N'paulo.kuhn@facta.com.br', NULL, N'\Jobs\Reports', NULL, N'EnviaEmail', N'HTML', N'High', 1, 1, 2, 9, NULL, NULL, NULL, N'49353855', NULL)
 
            INSERT [dbo].[AlertaParametro] ([Id_AlertaParametro], [Nm_Alerta], [Nm_Procedure], [Fl_Clear], [Vl_Parametro], [Ds_Metrica], [Nm_Empresa], [Ds_Email], [Ds_Caminho], [Ds_Caminho_Log], [IgnoraDatabase], [Ds_ProfileDBMail], [Ds_BodyFormatMail], [Ds_TipoMail], [IdMailAssinatura], [Ativo], [Ds_Menssageiro_01], [Ds_Menssageiro_02], [Ds_Menssageiro_03], [Ds_Menssageiro_04], [Ds_Menssageiro_05], [Ds_MSG], [Ds_Inclusao_Exclusao]) VALUES (7, N'Status Database', N'stpAlertaErroBancoDados', 1, NULL, NULL, N'Facta', N'paulo.kuhn@facta.com.br', NULL, N'\Jobs\Reports', NULL, N'EnviaEmail', N'HTML', N'High', 1, 1, 2, 9, NULL, NULL, NULL, N'49353855', NULL)
 
            INSERT [dbo].[AlertaParametro] ([Id_AlertaParametro], [Nm_Alerta], [Nm_Procedure], [Fl_Clear], [Vl_Parametro], [Ds_Metrica], [Nm_Empresa], [Ds_Email], [Ds_Caminho], [Ds_Caminho_Log], [IgnoraDatabase], [Ds_ProfileDBMail], [Ds_BodyFormatMail], [Ds_TipoMail], [IdMailAssinatura], [Ativo], [Ds_Menssageiro_01], [Ds_Menssageiro_02], [Ds_Menssageiro_03], [Ds_Menssageiro_04], [Ds_Menssageiro_05], [Ds_MSG], [Ds_Inclusao_Exclusao]) VALUES (8, N'Página Corrompida', N'stpAlertaErroBancoDados', 0, NULL, NULL, N'Facta', N'paulo.kuhn@facta.com.br', NULL, N'\Jobs\Reports', NULL, N'EnviaEmail', N'HTML', N'High', 1, 1, 2, 9, NULL, NULL, NULL, N'49353855', NULL)
 
            INSERT [dbo].[AlertaParametro] ([Id_AlertaParametro], [Nm_Alerta], [Nm_Procedure], [Fl_Clear], [Vl_Parametro], [Ds_Metrica], [Nm_Empresa], [Ds_Email], [Ds_Caminho], [Ds_Caminho_Log], [IgnoraDatabase], [Ds_ProfileDBMail], [Ds_BodyFormatMail], [Ds_TipoMail], [IdMailAssinatura], [Ativo], [Ds_Menssageiro_01], [Ds_Menssageiro_02], [Ds_Menssageiro_03], [Ds_Menssageiro_04], [Ds_Menssageiro_05], [Ds_MSG], [Ds_Inclusao_Exclusao]) VALUES (9, N'Queries Demoradas', N'stpAlertaQueriesDemoradas', 0, 100, N'Quantidade', N'Facta', N'paulo.kuhn@facta.com.br', NULL, N'\Jobs\Reports', NULL, N'EnviaEmail', N'HTML', N'High', 1, 1, 2, 9, NULL, NULL, NULL, N'49353855', NULL)
 
            INSERT [dbo].[AlertaParametro] ([Id_AlertaParametro], [Nm_Alerta], [Nm_Procedure], [Fl_Clear], [Vl_Parametro], [Ds_Metrica], [Nm_Empresa], [Ds_Email], [Ds_Caminho], [Ds_Caminho_Log], [IgnoraDatabase], [Ds_ProfileDBMail], [Ds_BodyFormatMail], [Ds_TipoMail], [IdMailAssinatura], [Ativo], [Ds_Menssageiro_01], [Ds_Menssageiro_02], [Ds_Menssageiro_03], [Ds_Menssageiro_04], [Ds_Menssageiro_05], [Ds_MSG], [Ds_Inclusao_Exclusao]) VALUES (10, N'Trace Queries Demoradas', N'stpCreateTrace', 0, 5, N'Segundos', N'Facta', N'paulo.kuhn@facta.com.br', NULL, N'\Logs\ResultadoTraceLog', NULL, N'EnviaEmail', N'HTML', N'High', 1, 1, 2, 9, NULL, NULL, NULL, N'49353855', NULL)
 
            INSERT [dbo].[AlertaParametro] ([Id_AlertaParametro], [Nm_Alerta], [Nm_Procedure], [Fl_Clear], [Vl_Parametro], [Ds_Metrica], [Nm_Empresa], [Ds_Email], [Ds_Caminho], [Ds_Caminho_Log], [IgnoraDatabase], [Ds_ProfileDBMail], [Ds_BodyFormatMail], [Ds_TipoMail], [IdMailAssinatura], [Ativo], [Ds_Menssageiro_01], [Ds_Menssageiro_02], [Ds_Menssageiro_03], [Ds_Menssageiro_04], [Ds_Menssageiro_05], [Ds_MSG], [Ds_Inclusao_Exclusao]) VALUES (11, N'Job Falha', N'stpAlertaJobFalha', 0, 24, N'Horas', N'Facta', N'paulo.kuhn@facta.com.br', NULL, N'\Jobs\Reports', NULL, N'EnviaEmail', N'HTML', N'High', 1, 1, 2, 9, NULL, NULL, NULL, N'49353855', NULL)
 
            INSERT [dbo].[AlertaParametro] ([Id_AlertaParametro], [Nm_Alerta], [Nm_Procedure], [Fl_Clear], [Vl_Parametro], [Ds_Metrica], [Nm_Empresa], [Ds_Email], [Ds_Caminho], [Ds_Caminho_Log], [IgnoraDatabase], [Ds_ProfileDBMail], [Ds_BodyFormatMail], [Ds_TipoMail], [IdMailAssinatura], [Ativo], [Ds_Menssageiro_01], [Ds_Menssageiro_02], [Ds_Menssageiro_03], [Ds_Menssageiro_04], [Ds_Menssageiro_05], [Ds_MSG], [Ds_Inclusao_Exclusao]) VALUES (12, N'SQL Server Reiniciado', N'stpAlertaSQLServerReiniciado', 0, 20, N'Minutos', N'Facta', N'paulo.kuhn@facta.com.br', NULL, N'\Jobs\Reports', NULL, N'EnviaEmail', N'HTML', N'High', 1, 1, 2, 9, NULL, NULL, NULL, N'49353855', NULL)
 
            INSERT [dbo].[AlertaParametro] ([Id_AlertaParametro], [Nm_Alerta], [Nm_Procedure], [Fl_Clear], [Vl_Parametro], [Ds_Metrica], [Nm_Empresa], [Ds_Email], [Ds_Caminho], [Ds_Caminho_Log], [IgnoraDatabase], [Ds_ProfileDBMail], [Ds_BodyFormatMail], [Ds_TipoMail], [IdMailAssinatura], [Ativo], [Ds_Menssageiro_01], [Ds_Menssageiro_02], [Ds_Menssageiro_03], [Ds_Menssageiro_04], [Ds_Menssageiro_05], [Ds_MSG], [Ds_Inclusao_Exclusao]) VALUES (13, N'Database Criada', N'stpAlertaDatabaseCriada', 0, 24, N'Horas', N'Facta', N'paulo.kuhn@facta.com.br', NULL, N'\Jobs\Reports', NULL, N'EnviaEmail', N'HTML', N'High', 1, 1, 2, 9, NULL, NULL, NULL, N'49353855', NULL)
 
            INSERT [dbo].[AlertaParametro] ([Id_AlertaParametro], [Nm_Alerta], [Nm_Procedure], [Fl_Clear], [Vl_Parametro], [Ds_Metrica], [Nm_Empresa], [Ds_Email], [Ds_Caminho], [Ds_Caminho_Log], [IgnoraDatabase], [Ds_ProfileDBMail], [Ds_BodyFormatMail], [Ds_TipoMail], [IdMailAssinatura], [Ativo], [Ds_Menssageiro_01], [Ds_Menssageiro_02], [Ds_Menssageiro_03], [Ds_Menssageiro_04], [Ds_Menssageiro_05], [Ds_MSG], [Ds_Inclusao_Exclusao]) VALUES (14, N'Database sem Backup', N'stpAlertaDatabaseSemBackup', 0, 24, N'Horas', N'Facta', N'paulo.kuhn@facta.com.br', NULL, N'\Jobs\Reports', N'''tempdb'', ''ReportServerTempDB''', N'EnviaEmail', N'HTML', N'High', 1, 1, 2, 9, NULL, NULL, NULL, N'49353855', N'''tempdb'', ''ReportServerTempDB''')
 
            INSERT [dbo].[AlertaParametro] ([Id_AlertaParametro], [Nm_Alerta], [Nm_Procedure], [Fl_Clear], [Vl_Parametro], [Ds_Metrica], [Nm_Empresa], [Ds_Email], [Ds_Caminho], [Ds_Caminho_Log], [IgnoraDatabase], [Ds_ProfileDBMail], [Ds_BodyFormatMail], [Ds_TipoMail], [IdMailAssinatura], [Ativo], [Ds_Menssageiro_01], [Ds_Menssageiro_02], [Ds_Menssageiro_03], [Ds_Menssageiro_04], [Ds_Menssageiro_05], [Ds_MSG], [Ds_Inclusao_Exclusao]) VALUES (15, N'Banco de Dados Corrompido', N'stpAlertaCheckDB', 0, NULL, NULL, N'Facta', N'paulo.kuhn@facta.com.br', NULL, N'\Jobs\Reports', NULL, N'EnviaEmail', N'HTML', N'High', 1, 1, 2, 9, NULL, NULL, NULL, N'49353855', NULL)
 
            INSERT [dbo].[AlertaParametro] ([Id_AlertaParametro], [Nm_Alerta], [Nm_Procedure], [Fl_Clear], [Vl_Parametro], [Ds_Metrica], [Nm_Empresa], [Ds_Email], [Ds_Caminho], [Ds_Caminho_Log], [IgnoraDatabase], [Ds_ProfileDBMail], [Ds_BodyFormatMail], [Ds_TipoMail], [IdMailAssinatura], [Ativo], [Ds_Menssageiro_01], [Ds_Menssageiro_02], [Ds_Menssageiro_03], [Ds_Menssageiro_04], [Ds_Menssageiro_05], [Ds_MSG], [Ds_Inclusao_Exclusao]) VALUES (16, N'Processos em Execução', N'stpEnviaEmailProcessosExecucao', 0, NULL, NULL, N'Facta', N'paulo.kuhn@facta.com.br', NULL, N'\Jobs\Reports', NULL, N'EnviaEmail', N'HTML', N'High', 1, 1, 2, 9, NULL, NULL, NULL, N'49353855', NULL)
 
            INSERT [dbo].[AlertaParametro] ([Id_AlertaParametro], [Nm_Alerta], [Nm_Procedure], [Fl_Clear], [Vl_Parametro], [Ds_Metrica], [Nm_Empresa], [Ds_Email], [Ds_Caminho], [Ds_Caminho_Log], [IgnoraDatabase], [Ds_ProfileDBMail], [Ds_BodyFormatMail], [Ds_TipoMail], [IdMailAssinatura], [Ativo], [Ds_Menssageiro_01], [Ds_Menssageiro_02], [Ds_Menssageiro_03], [Ds_Menssageiro_04], [Ds_Menssageiro_05], [Ds_MSG], [Ds_Inclusao_Exclusao]) VALUES (17, N'Alteracao database', N'stpAlertaAlteracaoDB', 0, 24, N'Horas', N'Facta', N'paulo.kuhn@facta.com.br', NULL, N'\Logs', NULL, N'EnviaEmail', N'HTML', N'High', 1, 1, 2, 9, NULL, NULL, NULL, N'49353855', NULL)
 
            INSERT [dbo].[AlertaParametro] ([Id_AlertaParametro], [Nm_Alerta], [Nm_Procedure], [Fl_Clear], [Vl_Parametro], [Ds_Metrica], [Nm_Empresa], [Ds_Email], [Ds_Caminho], [Ds_Caminho_Log], [IgnoraDatabase], [Ds_ProfileDBMail], [Ds_BodyFormatMail], [Ds_TipoMail], [IdMailAssinatura], [Ativo], [Ds_Menssageiro_01], [Ds_Menssageiro_02], [Ds_Menssageiro_03], [Ds_Menssageiro_04], [Ds_Menssageiro_05], [Ds_MSG], [Ds_Inclusao_Exclusao]) VALUES (18, N'AlwaysOn', N'stpAlertaAlwaysOn', 1, 1, N'Quantidade', N'Facta', N'paulo.kuhn@facta.com.br', NULL, N'\Jobs\Reports', NULL, N'EnviaEmail', N'HTML', N'High', 1, 1, 2, 9, NULL, NULL, NULL, N'49353855', NULL)
 
            INSERT [dbo].[AlertaParametro] ([Id_AlertaParametro], [Nm_Alerta], [Nm_Procedure], [Fl_Clear], [Vl_Parametro], [Ds_Metrica], [Nm_Empresa], [Ds_Email], [Ds_Caminho], [Ds_Caminho_Log], [IgnoraDatabase], [Ds_ProfileDBMail], [Ds_BodyFormatMail], [Ds_TipoMail], [IdMailAssinatura], [Ativo], [Ds_Menssageiro_01], [Ds_Menssageiro_02], [Ds_Menssageiro_03], [Ds_Menssageiro_04], [Ds_Menssageiro_05], [Ds_MSG], [Ds_Inclusao_Exclusao]) VALUES (19, N'Check DB', N'stpCheckDatabases', 0, NULL, NULL, N'Facta', N'paulo.kuhn@facta.com.br', NULL, N'\Jobs\Reports', N'''tempdb'', ''ReportServerTempDB''', N'EnviaEmail', N'HTML', N'High', 1, 0, 2, 9, NULL, NULL, NULL, N'49353855', N'''tempdb'', ''ReportServerTempDB''')
 
            INSERT [dbo].[AlertaParametro] ([Id_AlertaParametro], [Nm_Alerta], [Nm_Procedure], [Fl_Clear], [Vl_Parametro], [Ds_Metrica], [Nm_Empresa], [Ds_Email], [Ds_Caminho], [Ds_Caminho_Log], [IgnoraDatabase], [Ds_ProfileDBMail], [Ds_BodyFormatMail], [Ds_TipoMail], [IdMailAssinatura], [Ativo], [Ds_Menssageiro_01], [Ds_Menssageiro_02], [Ds_Menssageiro_03], [Ds_Menssageiro_04], [Ds_Menssageiro_05], [Ds_MSG], [Ds_Inclusao_Exclusao]) VALUES (20, N'Envia Telegram', N'stpSendMsgTelegram', 0, NULL, NULL, N'Facta', N'paulo.kuhn@facta.com.br', NULL, N'\Jobs\Reports', NULL, N'EnviaEmail', N'HTML', N'High', 1, 1, 2, 9, NULL, NULL, NULL, N'49353855', NULL)
 
            INSERT [dbo].[AlertaParametro] ([Id_AlertaParametro], [Nm_Alerta], [Nm_Procedure], [Fl_Clear], [Vl_Parametro], [Ds_Metrica], [Nm_Empresa], [Ds_Email], [Ds_Caminho], [Ds_Caminho_Log], [IgnoraDatabase], [Ds_ProfileDBMail], [Ds_BodyFormatMail], [Ds_TipoMail], [IdMailAssinatura], [Ativo], [Ds_Menssageiro_01], [Ds_Menssageiro_02], [Ds_Menssageiro_03], [Ds_Menssageiro_04], [Ds_Menssageiro_05], [Ds_MSG], [Ds_Inclusao_Exclusao]) VALUES (21, N'CheckList', N'stpEnviaCheckList', 0, NULL, NULL, N'Facta', N'paulo.kuhn@facta.com.br', N'C:\Data', N'\Jobs\Reports', NULL, N'EnviaEmail', N'HTML', N'High', 1, 1, 2, 9, NULL, NULL, NULL, N'49353855', NULL)
 
            INSERT [dbo].[AlertaParametro] ([Id_AlertaParametro], [Nm_Alerta], [Nm_Procedure], [Fl_Clear], [Vl_Parametro], [Ds_Metrica], [Nm_Empresa], [Ds_Email], [Ds_Caminho], [Ds_Caminho_Log], [IgnoraDatabase], [Ds_ProfileDBMail], [Ds_BodyFormatMail], [Ds_TipoMail], [IdMailAssinatura], [Ativo], [Ds_Menssageiro_01], [Ds_Menssageiro_02], [Ds_Menssageiro_03], [Ds_Menssageiro_04], [Ds_Menssageiro_05], [Ds_MSG], [Ds_Inclusao_Exclusao]) VALUES (22, N'Envia Teams', N'stpSendMsgTeams', 0, NULL, NULL, N'Facta', N'paulo.kuhn@facta.com.br', NULL, NULL, NULL, N'EnviaEmail', N'HTML', N'High', 1, 1, 2, 9, NULL, NULL, NULL, NULL, NULL)
 
            INSERT [dbo].[AlertaParametro] ([Id_AlertaParametro], [Nm_Alerta], [Nm_Procedure], [Fl_Clear], [Vl_Parametro], [Ds_Metrica], [Nm_Empresa], [Ds_Email], [Ds_Caminho], [Ds_Caminho_Log], [IgnoraDatabase], [Ds_ProfileDBMail], [Ds_BodyFormatMail], [Ds_TipoMail], [IdMailAssinatura], [Ativo], [Ds_Menssageiro_01], [Ds_Menssageiro_02], [Ds_Menssageiro_03], [Ds_Menssageiro_04], [Ds_Menssageiro_05], [Ds_MSG], [Ds_Inclusao_Exclusao]) VALUES (23, N'Fragmentacao Indice', N'stpCargaFragmentacaoIndice', 1, NULL, NULL, N'Facta', NULL, NULL, N'\Jobs\Reports', N'''master'', ''msdb'', ''tempdb''', N'EnviaEmail', N'HTML', N'High', 1, 1, 2, 9, NULL, NULL, NULL, NULL, NULL)
 
            INSERT [dbo].[AlertaParametro] ([Id_AlertaParametro], [Nm_Alerta], [Nm_Procedure], [Fl_Clear], [Vl_Parametro], [Ds_Metrica], [Nm_Empresa], [Ds_Email], [Ds_Caminho], [Ds_Caminho_Log], [IgnoraDatabase], [Ds_ProfileDBMail], [Ds_BodyFormatMail], [Ds_TipoMail], [IdMailAssinatura], [Ativo], [Ds_Menssageiro_01], [Ds_Menssageiro_02], [Ds_Menssageiro_03], [Ds_Menssageiro_04], [Ds_Menssageiro_05], [Ds_MSG], [Ds_Inclusao_Exclusao]) VALUES (24, N'Historico Erros', N'stpCargaHistoricoErrosDB', 1, NULL, NULL, N'Facta', NULL, NULL, N'\Jobs\CapturaErrosSistema', NULL, N'EnviaEmail', N'HTML', N'High', 1, 1, 2, 9, NULL, NULL, NULL, NULL, NULL)
 
            INSERT [dbo].[AlertaParametro] ([Id_AlertaParametro], [Nm_Alerta], [Nm_Procedure], [Fl_Clear], [Vl_Parametro], [Ds_Metrica], [Nm_Empresa], [Ds_Email], [Ds_Caminho], [Ds_Caminho_Log], [IgnoraDatabase], [Ds_ProfileDBMail], [Ds_BodyFormatMail], [Ds_TipoMail], [IdMailAssinatura], [Ativo], [Ds_Menssageiro_01], [Ds_Menssageiro_02], [Ds_Menssageiro_03], [Ds_Menssageiro_04], [Ds_Menssageiro_05], [Ds_MSG], [Ds_Inclusao_Exclusao]) VALUES (25, N'Envia Email', N'[msdb].[dbo].[sp_send_dbmail]', 1, NULL, NULL, N'Facta', NULL, NULL, N'\Jobs\Reports', NULL, N'EnviaEmail', N'HTML', N'High', 1, 1, NULL, NULL, NULL, NULL, NULL, NULL, NULL)

            INSERT [dbo].[AlertaParametro] ([Id_AlertaParametro], [Nm_Alerta], [Nm_Procedure], [Fl_Clear], [Vl_Parametro], [Ds_Metrica], [Nm_Empresa], [Ds_Email], [Ds_Caminho], [Ds_Caminho_Log], [IgnoraDatabase], [Ds_ProfileDBMail], [Ds_BodyFormatMail], [Ds_TipoMail], [IdMailAssinatura], [Ativo], [Ds_Menssageiro_01], [Ds_Menssageiro_02], [Ds_Menssageiro_03], [Ds_Menssageiro_04], [Ds_Menssageiro_05], [Ds_MSG], [Ds_Inclusao_Exclusao]) VALUES (26, N'Alerta Queue', N'stpQueueInfoSendMail', 0, NULL, NULL, N'Facta', N'paulo.kuhn@facta.com.br', NULL, N'\Jobs\Reports', N'''master'',''model'',''msdb'',''tempdb''', N'EnviaEmail', N'HTML', N'High', 1, 1, 2, 9, NULL, NULL, NULL, NULL, NULL)
            
            INSERT [dbo].[AlertaParametro] ([Id_AlertaParametro], [Nm_Alerta], [Nm_Procedure], [Fl_Clear], [Vl_Parametro], [Ds_Metrica], [Nm_Empresa], [Ds_Email], [Ds_Caminho], [Ds_Caminho_Log], [IgnoraDatabase], [Ds_ProfileDBMail], [Ds_BodyFormatMail], [Ds_TipoMail], [IdMailAssinatura], [Ativo], [Ds_Menssageiro_01], [Ds_Menssageiro_02], [Ds_Menssageiro_03], [Ds_Menssageiro_04], [Ds_Menssageiro_05], [Ds_MSG], [Ds_Inclusao_Exclusao]) VALUES (27, N'Alerta File DB', N'stpcheckFileBackup', 0, NULL, NULL, N'Facta', N'paulo.kuhn@facta.com.br', NULL, N'\Jobs\Reports', N'''tempdb'',''ReportServerTempDB''', N'EnviaEmail', N'HTML', N'High', 1, 1, 2, 9, NULL, NULL, NULL, NULL, NULL)
            
            INSERT [dbo].[AlertaParametro] ([Id_AlertaParametro], [Nm_Alerta], [Nm_Procedure], [Fl_Clear], [Vl_Parametro], [Ds_Metrica], [Nm_Empresa], [Ds_Email], [Ds_Caminho], [Ds_Caminho_Log], [IgnoraDatabase], [Ds_ProfileDBMail], [Ds_BodyFormatMail], [Ds_TipoMail], [IdMailAssinatura], [Ativo], [Ds_Menssageiro_01], [Ds_Menssageiro_02], [Ds_Menssageiro_03], [Ds_Menssageiro_04], [Ds_Menssageiro_05], [Ds_MSG], [Ds_Inclusao_Exclusao]) VALUES (28, N'Envia Zabbix Sender', N'stpZabbixSender', 1, NULL, NULL, N'Facta', N'dataservices@facta.com.br', NULL, N'\Jobs\Reports', NULL, N'EnviaEmail', N'HTML', N'High', 1, 1, NULL, NULL, NULL, NULL, NULL, NULL, NULL)

            INSERT [dbo].[AlertaParametro] ([Id_AlertaParametro], [Nm_Alerta], [Nm_Procedure], [Fl_Clear], [Vl_Parametro], [Ds_Metrica], [Nm_Empresa], [Ds_Email], [Ds_Caminho], [Ds_Caminho_Log], [IgnoraDatabase], [Ds_ProfileDBMail], [Ds_BodyFormatMail], [Ds_TipoMail], [IdMailAssinatura], [Ativo], [Ds_Menssageiro_01], [Ds_Menssageiro_02], [Ds_Menssageiro_03], [Ds_Menssageiro_04], [Ds_Menssageiro_05], [Ds_MSG], [Ds_Inclusao_Exclusao]) VALUES (29, N'Job Agendamento Falha', N'stpAlertaJobAgendamentoFalha', 0, 24, N'Horas', N'Facta', N'dataservices@facta.com.br', NULL, N'\Jobs\Reports', NULL, N'EnviaEmail', N'HTML', N'High', 1, 1, 2, 9, NULL, NULL, NULL, N'49353855', NULL)

            SET IDENTITY_INSERT [dbo].[AlertaParametro] OFF
        
            SET IDENTITY_INSERT [dbo].[AlertaMsgToken] ON 
 
            INSERT [dbo].[AlertaMsgToken] ([Id], [Nome], [IdAlertaParametro], [Token], [DataInclusao], [DataAlteracao], [User], [Pass], [Ativo], [NomeCanal], [Canal]) VALUES (2, N'Telegram', 20, N'187235235:AAEVrK7cZsiAnfVt-KyH6VAg5aboObBY3hI', CAST(N'2020-03-25T23:47:13.5833333' AS DateTime2), NULL, NULL, NULL, 1, NULL, N'-352712772')
 
            INSERT [dbo].[AlertaMsgToken] ([Id], [Nome], [IdAlertaParametro], [Token], [DataInclusao], [DataAlteracao], [User], [Pass], [Ativo], [NomeCanal], [Canal]) VALUES (9, N'Teams Prod', 22, N'https://outlook.office.com/webhook/b92982db-9d6e-43d4-bd2f-e475ee57ea0d@f639a7ca-9cf1-4680-897e-98e2a4125b9e/IncomingWebhook/66d5f02e201f41048af5a974040bea0f/44e07bf2-2dbc-4678-a8c3-ab41b05c0b26', CAST(N'2020-03-25T23:47:13.5833333' AS DateTime2), NULL, NULL, NULL, 1, N'AlertasDB', NULL)
 
            INSERT [dbo].[AlertaMsgToken] ([Id], [Nome], [IdAlertaParametro], [Token], [DataInclusao], [DataAlteracao], [User], [Pass], [Ativo], [NomeCanal], [Canal]) VALUES (13, N'Teams HML', 22, N'https://outlook.office.com/webhook/b92982db-9d6e-43d4-bd2f-e475ee57ea0d@f639a7ca-9cf1-4680-897e-98e2a4125b9e/IncomingWebhook/ca511955ffe94332a6c0be386f240569/44e07bf2-2dbc-4678-a8c3-ab41b05c0b26', CAST(N'2020-03-26T23:47:13.5833333' AS DateTime2), NULL, NULL, NULL, 1, N'AlertasDB_HML', NULL)
 
            INSERT [dbo].[AlertaMsgToken] ([Id], [Nome], [IdAlertaParametro], [Token], [DataInclusao], [DataAlteracao], [User], [Pass], [Ativo], [NomeCanal], [Canal]) VALUES (14, N'Teams Geral', 22, N'https://outlook.office.com/webhook/b92982db-9d6e-43d4-bd2f-e475ee57ea0d@f639a7ca-9cf1-4680-897e-98e2a4125b9e/IncomingWebhook/871578777e3f457892b75431fdd03b9b/44e07bf2-2dbc-4678-a8c3-ab41b05c0b26', CAST(N'2020-03-26T23:47:13.5833333' AS DateTime2), NULL, NULL, NULL, 1, N'Monitoramento_DB', NULL)
 
            SET IDENTITY_INSERT [dbo].[AlertaMsgToken] OFF

            SET IDENTITY_INSERT [dbo].[AlertaParametroMenssage] ON 
 
            INSERT [dbo].[AlertaParametroMenssage] ([Id], [IdAlertaParametro], [SubjectProblem], [SubjectSolution], [MailTextProblem], [MailTextSolution], [IdUsuarioCriacao], [DataCriacao], [DataAlteracao], [NomeMsg]) VALUES (1, 21, NULL, N'#CheckList Diário do Banco:', NULL, N'Prezados,<BR /><BR /> Segue <b>CheckList DB</b>, análise o relatório com atenção, através do mesmo será possível identificar falhas, não deixe os problemas te pegarem desprevenido. <BR/><BR/><BR/><BR/>', NULL, CAST(N'2020-03-26T22:56:59.823' AS DateTime), NULL, NULL)
 
            INSERT [dbo].[AlertaParametroMenssage] ([Id], [IdAlertaParametro], [SubjectProblem], [SubjectSolution], [MailTextProblem], [MailTextSolution], [IdUsuarioCriacao], [DataCriacao], [DataAlteracao], [NomeMsg]) VALUES (2, 17, N'ALERTA - #AlteracaoDataBase - Database alterada nas últimas ', NULL, N'Prezados,<BR /><BR /> Identifiquei alterações de Database no servidor mencionado no assunto do e-mail, favor verifique esta informação.', NULL, NULL, CAST(N'2020-03-30T12:33:10.560' AS DateTime), NULL, NULL)
 
            INSERT [dbo].[AlertaParametroMenssage] ([Id], [IdAlertaParametro], [SubjectProblem], [SubjectSolution], [MailTextProblem], [MailTextSolution], [IdUsuarioCriacao], [DataCriacao], [DataAlteracao], [NomeMsg]) VALUES (3, 2, N'ALERTA #TransactionLog - Detectado inconsistência de Transaction Log com mais de ', N'Solução #TransactioLog - Não existem mais inconsistência de Transaction Log com mais de ', N'Prezados,<BR /><BR /> Identificada inconsistência de <b>transaction log </b> na Instância mencionada no assunto do e-mail, verifique o relatório abaixo com <b>urgência</b>.', N'Informações dos Arquivos de Log.', NULL, CAST(N'2020-03-30T17:13:55.613' AS DateTime), NULL, NULL)
 
            INSERT [dbo].[AlertaParametroMenssage] ([Id], [IdAlertaParametro], [SubjectProblem], [SubjectSolution], [MailTextProblem], [MailTextSolution], [IdUsuarioCriacao], [DataCriacao], [DataAlteracao], [NomeMsg]) VALUES (4, 15, N'ALERTA #DatabaseCorrompido - Existe algum Banco de Dados Corrompido no Servidor:', NULL, N'Prezados,<BR /><BR /> Identifiquei um problema de <b>Dados Corrompido </b> na Instância mencionada no assunto do e-mail, verifique o relatório abaixo com <b>urgência</b>.', NULL, NULL, CAST(N'2020-03-30T17:13:55.613' AS DateTime), NULL, NULL)
 
            INSERT [dbo].[AlertaParametroMenssage] ([Id], [IdAlertaParametro], [SubjectProblem], [SubjectSolution], [MailTextProblem], [MailTextSolution], [IdUsuarioCriacao], [DataCriacao], [DataAlteracao], [NomeMsg]) VALUES (5, 6, N'ALERTA #ConexoesAbertas - Existem', N'Solução #ConexoesAbertas - Existem', N'Prezados,<BR /><BR /> Segue as TOP 25 <b>conexões Abertas</b> na Instância mencionada no assunto do e-mail, verifique o relatório abaixo com <b>urgência</b>.', N'Prezados,<BR /><BR /> Conexões Abertas no Servidor:', NULL, CAST(N'2020-03-31T17:13:55.613' AS DateTime), NULL, NULL)
 
            INSERT [dbo].[AlertaParametroMenssage] ([Id], [IdAlertaParametro], [SubjectProblem], [SubjectSolution], [MailTextProblem], [MailTextSolution], [IdUsuarioCriacao], [DataCriacao], [DataAlteracao], [NomeMsg]) VALUES (6, 4, N'ALERTA #CPU - Detectado problema de consumo de CPU no Servidor:', N'Solução #CPU - O Consumo de CPU está abaixo de', N'Prezados,<BR /><BR /> Identifiquei um problema de consumo de <b>CPU</b> na instância mencionada no assunto do e-mail, verifique o relatório abaixo com <b>urgência</b>.', N'Prezados,<BR /><BR /> O Consumo de <b>CPU</b> esta abaixo de', NULL, CAST(N'2020-03-31T17:14:55.613' AS DateTime), NULL, NULL)
 
            INSERT [dbo].[AlertaParametroMenssage] ([Id], [IdAlertaParametro], [SubjectProblem], [SubjectSolution], [MailTextProblem], [MailTextSolution], [IdUsuarioCriacao], [DataCriacao], [DataAlteracao], [NomeMsg]) VALUES (7, 13, N'ALERTA #DatabaseCriada - Database Criada nas últimas', NULL, N'Prezados,<BR /><BR /> Identifiquei <b>alterações de Database</b> na instância mencionada no assunto do e-mail, verifique essa informação realizando o planejamento necessário para que não falte de espaço em disco.', NULL, NULL, CAST(N'2020-03-31T17:14:59.613' AS DateTime), NULL, NULL)
 
            INSERT [dbo].[AlertaParametroMenssage] ([Id], [IdAlertaParametro], [SubjectProblem], [SubjectSolution], [MailTextProblem], [MailTextSolution], [IdUsuarioCriacao], [DataCriacao], [DataAlteracao], [NomeMsg]) VALUES (8, 14, N'ALERTA #DatabasesSemBackup - Existem Databases sem Backup nas últimas', NULL, N'Prezados,<BR /><BR /> Identifiquei que existem <b>Databases sem Backup</b> na instância mencionada no assunto do e-mail, verifique o relatório abaixo com <b>urgência</b>.', NULL, NULL, CAST(N'2020-03-31T17:14:59.613' AS DateTime), NULL, NULL)
 
            INSERT [dbo].[AlertaParametroMenssage] ([Id], [IdAlertaParametro], [SubjectProblem], [SubjectSolution], [MailTextProblem], [MailTextSolution], [IdUsuarioCriacao], [DataCriacao], [DataAlteracao], [NomeMsg]) VALUES (9, 8, N'ALERTA #PaginaCorrompida - Existe uma página corrompida no Servidor', NULL, N'Prezados,<BR /><BR /> Identifiquei um problema de <b>página corrompida</b> na instância mencionada no assunto do e-mail, verifique o relatório abaixo com <b>urgência</b>.', NULL, NULL, CAST(N'2020-04-01T17:14:59.613' AS DateTime), NULL, NULL)
 
            INSERT [dbo].[AlertaParametroMenssage] ([Id], [IdAlertaParametro], [SubjectProblem], [SubjectSolution], [MailTextProblem], [MailTextSolution], [IdUsuarioCriacao], [DataCriacao], [DataAlteracao], [NomeMsg]) VALUES (10, 7, N'ALERTA #DatabaseOffLine - Database OFFLINE no Servidor:', N'Solução #DatabaseOnLine - Todas as Databases estão ONLINE no Servidor:', N'Prezados,<BR /><BR /> Identifiquei que nem todas as <b>Databases</b> estao <b>ONLINE</b> na instância mencionada no assunto do e-mail, verifique o relatório abaixo com <b>urgência</b>.', N'Prezados,<BR /><BR /> Correção realizada com sucesso todas as Databases estao ONLINE na Instância', NULL, CAST(N'2020-04-01T17:14:59.613' AS DateTime), NULL, NULL)
 
            INSERT [dbo].[AlertaParametroMenssage] ([Id], [IdAlertaParametro], [SubjectProblem], [SubjectSolution], [MailTextProblem], [MailTextSolution], [IdUsuarioCriacao], [DataCriacao], [DataAlteracao], [NomeMsg]) VALUES (11, 3, N'ALERTA #DISCO - Detectado problema de espaço em disco no Servidor:', N'Solução #DISCO - Sem problema de espaço em disco, utilização abaixo de', N'Prezados,<BR /><BR /> Identifiquei um problema de espaço em disco na instância mencionada no assunto do e-mail, verifique o relatório abaixo com <b>urgência</b>.', N'Prezados,<BR /><BR /> Problema de espaço em disco solucionado no', NULL, CAST(N'2020-04-01T17:14:59.613' AS DateTime), NULL, NULL)
 
            INSERT [dbo].[AlertaParametroMenssage] ([Id], [IdAlertaParametro], [SubjectProblem], [SubjectSolution], [MailTextProblem], [MailTextSolution], [IdUsuarioCriacao], [DataCriacao], [DataAlteracao], [NomeMsg]) VALUES (12, 11, N'ALERTA #JobsFail - Falha de execução de Jobs nas últimas', NULL, N'Prezados,<BR /><BR />Segue as TOP 50 <b>jobs</b> que Falharam na instância mencionada no assunto do e-mail, verifique o relatório abaixo.', NULL, NULL, CAST(N'2020-04-01T17:14:59.613' AS DateTime), NULL, NULL)
 
            INSERT [dbo].[AlertaParametroMenssage] ([Id], [IdAlertaParametro], [SubjectProblem], [SubjectSolution], [MailTextProblem], [MailTextSolution], [IdUsuarioCriacao], [DataCriacao], [DataAlteracao], [NomeMsg]) VALUES (13, 1, N'Alerta #ProcessoBloqueado - Existe(m) processo(s) bloqueado(s) no Servidor:', N'Solução #ProcessoBloqueado - Não existem mais inconsistência de Processo Bloqueado no Servidor:', N'Prezados,<BR /><BR />Existe(m) Processo(s) Bloqueado(s) na instância mencionada no assunto do e-mail. Baixo segue os TOP 50 processos em lock.', N'Prezados,<BR /><BR />Sem registro de Processo Bloqueado na instância mencionada no assunto do e-mail, segue abaixo processos em execução no Banco de Dados', NULL, CAST(N'2020-04-01T17:14:59.613' AS DateTime), NULL, NULL)
 
            INSERT [dbo].[AlertaParametroMenssage] ([Id], [IdAlertaParametro], [SubjectProblem], [SubjectSolution], [MailTextProblem], [MailTextSolution], [IdUsuarioCriacao], [DataCriacao], [DataAlteracao], [NomeMsg]) VALUES (14, 9, N'ALERTA #QueriesDemoradas - Existem queries demoradas nos últimos 5 minutos - Total:', NULL, N'Prezados,<BR /><BR /> Segue os TOP 50 - Processos em execução na instância mencionada no assunto do e-mail, verifique o relatório abaixo.', NULL, NULL, CAST(N'2020-04-01T17:14:59.613' AS DateTime), NULL, NULL)
 
            INSERT [dbo].[AlertaParametroMenssage] ([Id], [IdAlertaParametro], [SubjectProblem], [SubjectSolution], [MailTextProblem], [MailTextSolution], [IdUsuarioCriacao], [DataCriacao], [DataAlteracao], [NomeMsg]) VALUES (15, 12, N'ALERTA #SQLServerReboot - SQL Server Reiniciado nos últimos', NULL, N'Prezados,<BR /><BR /> Identifiquei que a instância mencionada no assunto do e-mail foi reiniciado, verifique essa informação com <b>urgência</b>.', NULL, NULL, CAST(N'2020-04-01T17:14:59.613' AS DateTime), NULL, NULL)
 
            INSERT [dbo].[AlertaParametroMenssage] ([Id], [IdAlertaParametro], [SubjectProblem], [SubjectSolution], [MailTextProblem], [MailTextSolution], [IdUsuarioCriacao], [DataCriacao], [DataAlteracao], [NomeMsg]) VALUES (16, 5, N'ALERTA #ArquivoMDF - Detectado problema na utilização do Arquivo MDF do Tempdb está acima de', N'Solução #ArquivoMDF - A Utilização do Arquivo MDF do Tempdb está abaixo de', N'Prezados,<BR /><BR /> Identifiquei um problema no Arquivo <b>MDF do Tempdb<b> na instância mencionada no assunto do e-mail, verifique o relatório abaixo com <b>urgência</b>.', N'Prezados,<BR /><BR />A Utilização do Arquivo MDF do Tempdb está abaixo do mencionada no assunto do e-mail.', NULL, CAST(N'2020-04-01T17:14:59.613' AS DateTime), NULL, NULL)
 
            INSERT [dbo].[AlertaParametroMenssage] ([Id], [IdAlertaParametro], [SubjectProblem], [SubjectSolution], [MailTextProblem], [MailTextSolution], [IdUsuarioCriacao], [DataCriacao], [DataAlteracao], [NomeMsg]) VALUES (17, 16, N'#AlertaNotificação - Processos em execução no Servidor:', NULL, N'Prezados,<BR /><BR /> Segue os processos em execução na instância mencionada no assunto do e-mail, verifique essa informação.', NULL, NULL, CAST(N'2020-04-01T17:14:59.613' AS DateTime), NULL, NULL)
 
            INSERT [dbo].[AlertaParametroMenssage] ([Id], [IdAlertaParametro], [SubjectProblem], [SubjectSolution], [MailTextProblem], [MailTextSolution], [IdUsuarioCriacao], [DataCriacao], [DataAlteracao], [NomeMsg]) VALUES (18, 21, NULL, NULL, NULL, N'<b>Disponibilidade do MSSQL</b>', NULL, CAST(N'2020-04-01T14:16:53.050' AS DateTime), NULL, N'Check_Disponibilidade')
 
            INSERT [dbo].[AlertaParametroMenssage] ([Id], [IdAlertaParametro], [SubjectProblem], [SubjectSolution], [MailTextProblem], [MailTextSolution], [IdUsuarioCriacao], [DataCriacao], [DataAlteracao], [NomeMsg]) VALUES (19, 21, NULL, NULL, NULL, N'<BR/><BR/><b>Crescimento das Bases - Top 30</b>', NULL, CAST(N'2020-04-01T14:20:39.207' AS DateTime), NULL, N'Check_CrescimentoBases')
 
            INSERT [dbo].[AlertaParametroMenssage] ([Id], [IdAlertaParametro], [SubjectProblem], [SubjectSolution], [MailTextProblem], [MailTextSolution], [IdUsuarioCriacao], [DataCriacao], [DataAlteracao], [NomeMsg]) VALUES (20, 21, NULL, NULL, NULL, N'<b>Crescimento das Tabelas - Top 30</b>', NULL, CAST(N'2020-04-01T14:21:58.450' AS DateTime), NULL, N'Check_CrescimentoTabelas')
 
            INSERT [dbo].[AlertaParametroMenssage] ([Id], [IdAlertaParametro], [SubjectProblem], [SubjectSolution], [MailTextProblem], [MailTextSolution], [IdUsuarioCriacao], [DataCriacao], [DataAlteracao], [NomeMsg]) VALUES (21, 21, NULL, NULL, NULL, N'<b>Alterações na Instância - Top 10</b>', NULL, CAST(N'2020-04-01T14:23:23.460' AS DateTime), NULL, N'Check_AlteracaoInstancia')
 
            INSERT [dbo].[AlertaParametroMenssage] ([Id], [IdAlertaParametro], [SubjectProblem], [SubjectSolution], [MailTextProblem], [MailTextSolution], [IdUsuarioCriacao], [DataCriacao], [DataAlteracao], [NomeMsg]) VALUES (22, 21, NULL, NULL, NULL, N'<b>Informações dos Arquivos de Dados - Top 5</b>', NULL, CAST(N'2020-04-01T14:24:19.760' AS DateTime), NULL, N'Check_InfoArquivoDados')
 
            INSERT [dbo].[AlertaParametroMenssage] ([Id], [IdAlertaParametro], [SubjectProblem], [SubjectSolution], [MailTextProblem], [MailTextSolution], [IdUsuarioCriacao], [DataCriacao], [DataAlteracao], [NomeMsg]) VALUES (23, 21, NULL, NULL, NULL, N'<b>Informações dos Arquivos de Log - Top 5</b>', NULL, CAST(N'2020-04-01T14:26:53.237' AS DateTime), NULL, N'Check_InfoArquivolog')
 
            INSERT [dbo].[AlertaParametroMenssage] ([Id], [IdAlertaParametro], [SubjectProblem], [SubjectSolution], [MailTextProblem], [MailTextSolution], [IdUsuarioCriacao], [DataCriacao], [DataAlteracao], [NomeMsg]) VALUES (24, 21, NULL, NULL, NULL, N'<b>Utilização Arquivos de Databases - Writes 09:00 - 18:00 - Top 10</b>', NULL, CAST(N'2020-04-01T14:27:56.180' AS DateTime), NULL, N'Check_UtilizacaoArquivosWrites')
 
            INSERT [dbo].[AlertaParametroMenssage] ([Id], [IdAlertaParametro], [SubjectProblem], [SubjectSolution], [MailTextProblem], [MailTextSolution], [IdUsuarioCriacao], [DataCriacao], [DataAlteracao], [NomeMsg]) VALUES (25, 21, NULL, NULL, NULL, N'<b>Utilização Arquivos de Databases - Reads 09:00 - 18:00 - Top 10</b>', NULL, CAST(N'2020-04-01T14:30:07.180' AS DateTime), NULL, N'Check_UtilizacaoArquivosReads')
 
            INSERT [dbo].[AlertaParametroMenssage] ([Id], [IdAlertaParametro], [SubjectProblem], [SubjectSolution], [MailTextProblem], [MailTextSolution], [IdUsuarioCriacao], [DataCriacao], [DataAlteracao], [NomeMsg]) VALUES (26, 21, NULL, NULL, NULL, N'<b>Databases Sem Backup nas últimas 16 Horas</b>', NULL, CAST(N'2020-04-01T14:31:13.397' AS DateTime), NULL, N'Check_SemBKP')
 
            INSERT [dbo].[AlertaParametroMenssage] ([Id], [IdAlertaParametro], [SubjectProblem], [SubjectSolution], [MailTextProblem], [MailTextSolution], [IdUsuarioCriacao], [DataCriacao], [DataAlteracao], [NomeMsg]) VALUES (27, 21, NULL, NULL, NULL, N'<b>Backup FULL, LOG e Diferencial das Bases - TOP 10 </b>', NULL, CAST(N'2020-04-01T14:32:13.047' AS DateTime), NULL, N'Check_BKP_DB')
 
            INSERT [dbo].[AlertaParametroMenssage] ([Id], [IdAlertaParametro], [SubjectProblem], [SubjectSolution], [MailTextProblem], [MailTextSolution], [IdUsuarioCriacao], [DataCriacao], [DataAlteracao], [NomeMsg]) VALUES (28, 21, NULL, NULL, NULL, N'<b>Fragmentação dos Índices - Top 10</b>', NULL, CAST(N'2020-04-01T14:33:21.630' AS DateTime), NULL, N'Check_FragIndice')
 
            INSERT [dbo].[AlertaParametroMenssage] ([Id], [IdAlertaParametro], [SubjectProblem], [SubjectSolution], [MailTextProblem], [MailTextSolution], [IdUsuarioCriacao], [DataCriacao], [DataAlteracao], [NomeMsg]) VALUES (29, 21, NULL, NULL, NULL, N'<b>Queries Demoradas Dia Anterior 07:00 - 23:00  - TOP 10 </b>', NULL, CAST(N'2020-04-01T15:31:54.360' AS DateTime), NULL, N'Check_QueryDemorada')
 
            INSERT [dbo].[AlertaParametroMenssage] ([Id], [IdAlertaParametro], [SubjectProblem], [SubjectSolution], [MailTextProblem], [MailTextSolution], [IdUsuarioCriacao], [DataCriacao], [DataAlteracao], [NomeMsg]) VALUES (30, 21, NULL, NULL, NULL, N'<b>Queries em Execução a mais de 2 horas - TOP 5 </b>', NULL, CAST(N'2020-04-01T15:32:54.200' AS DateTime), NULL, N'Check_QueryExec')
 
            INSERT [dbo].[AlertaParametroMenssage] ([Id], [IdAlertaParametro], [SubjectProblem], [SubjectSolution], [MailTextProblem], [MailTextSolution], [IdUsuarioCriacao], [DataCriacao], [DataAlteracao], [NomeMsg]) VALUES (31, 21, NULL, NULL, NULL, N'<b>Quantidade de Queries Demoradas dos Últimos 10 Dias 07:00 - 23:00</b>', NULL, CAST(N'2020-04-01T15:33:51.757' AS DateTime), NULL, N'Check_QtdQueryExec')
 
            INSERT [dbo].[AlertaParametroMenssage] ([Id], [IdAlertaParametro], [SubjectProblem], [SubjectSolution], [MailTextProblem], [MailTextSolution], [IdUsuarioCriacao], [DataCriacao], [DataAlteracao], [NomeMsg]) VALUES (32, 21, NULL, NULL, NULL, N'<b>Error Log do SQL Server - TOP 30 </b>', NULL, CAST(N'2020-04-01T15:34:47.443' AS DateTime), NULL, N'Check_ErroLog')
 
            INSERT [dbo].[AlertaParametroMenssage] ([Id], [IdAlertaParametro], [SubjectProblem], [SubjectSolution], [MailTextProblem], [MailTextSolution], [IdUsuarioCriacao], [DataCriacao], [DataAlteracao], [NomeMsg]) VALUES (33, 21, NULL, NULL, NULL, N'<b>Jobs em Execução - TOP 10 </b>', NULL, CAST(N'2020-04-01T15:37:10.527' AS DateTime), NULL, N'Check_JOB_EXEC')
 
            INSERT [dbo].[AlertaParametroMenssage] ([Id], [IdAlertaParametro], [SubjectProblem], [SubjectSolution], [MailTextProblem], [MailTextSolution], [IdUsuarioCriacao], [DataCriacao], [DataAlteracao], [NomeMsg]) VALUES (34, 21, NULL, NULL, NULL, N'<b>Jobs Alterados - TOP 10 </b>', NULL, CAST(N'2020-04-01T15:38:13.207' AS DateTime), NULL, N'Check_JOB_Alterados')
 
            INSERT [dbo].[AlertaParametroMenssage] ([Id], [IdAlertaParametro], [SubjectProblem], [SubjectSolution], [MailTextProblem], [MailTextSolution], [IdUsuarioCriacao], [DataCriacao], [DataAlteracao], [NomeMsg]) VALUES (35, 21, NULL, NULL, NULL, N'<b>Jobs que Falharam - TOP 10 </b>', NULL, CAST(N'2020-04-01T15:41:38.983' AS DateTime), NULL, N'Check_JOB_Falharam')
 
            INSERT [dbo].[AlertaParametroMenssage] ([Id], [IdAlertaParametro], [SubjectProblem], [SubjectSolution], [MailTextProblem], [MailTextSolution], [IdUsuarioCriacao], [DataCriacao], [DataAlteracao], [NomeMsg]) VALUES (36, 21, NULL, NULL, NULL, N'<b>Jobs Demorados - TOP 10 </b>', NULL, CAST(N'2020-04-01T15:42:50.350' AS DateTime), NULL, N'Check_JOB_Demorados')
 
            INSERT [dbo].[AlertaParametroMenssage] ([Id], [IdAlertaParametro], [SubjectProblem], [SubjectSolution], [MailTextProblem], [MailTextSolution], [IdUsuarioCriacao], [DataCriacao], [DataAlteracao], [NomeMsg]) VALUES (37, 21, NULL, NULL, NULL, N'<b>Média Contadores Dia Anterior 07:00 - 23:00</b>', NULL, CAST(N'2020-04-01T15:44:13.680' AS DateTime), NULL, N'Check_Media_Contadores')
 
            INSERT [dbo].[AlertaParametroMenssage] ([Id], [IdAlertaParametro], [SubjectProblem], [SubjectSolution], [MailTextProblem], [MailTextSolution], [IdUsuarioCriacao], [DataCriacao], [DataAlteracao], [NomeMsg]) VALUES (38, 21, NULL, NULL, NULL, N'<b>Conexões Abertas por Usuários</b>', NULL, CAST(N'2020-04-01T15:46:13.533' AS DateTime), NULL, N'Check_ConexoesUsuario')
 
            INSERT [dbo].[AlertaParametroMenssage] ([Id], [IdAlertaParametro], [SubjectProblem], [SubjectSolution], [MailTextProblem], [MailTextSolution], [IdUsuarioCriacao], [DataCriacao], [DataAlteracao], [NomeMsg]) VALUES (39, 21, NULL, NULL, NULL, N'<b>Waits Stats Dia Anterior 07:00 - 23:00 - Top 10</b>', NULL, CAST(N'2020-04-01T15:47:40.800' AS DateTime), NULL, N'Check_WaitsStats')
 
            INSERT [dbo].[AlertaParametroMenssage] ([Id], [IdAlertaParametro], [SubjectProblem], [SubjectSolution], [MailTextProblem], [MailTextSolution], [IdUsuarioCriacao], [DataCriacao], [DataAlteracao], [NomeMsg]) VALUES (40, 21, NULL, NULL, NULL, N'<b>Alertas Sem Solução</b>', NULL, CAST(N'2020-04-01T15:48:31.777' AS DateTime), NULL, N'Check_AlertasSemSolucao')
 
            INSERT [dbo].[AlertaParametroMenssage] ([Id], [IdAlertaParametro], [SubjectProblem], [SubjectSolution], [MailTextProblem], [MailTextSolution], [IdUsuarioCriacao], [DataCriacao], [DataAlteracao], [NomeMsg]) VALUES (41, 21, NULL, NULL, NULL, N'<b>Alertas do Dia Anterior - TOP 40 </b>', NULL, CAST(N'2020-04-01T15:49:41.610' AS DateTime), NULL, N'Check_AlertasDiaAnterior')
 
            INSERT [dbo].[AlertaParametroMenssage] ([Id], [IdAlertaParametro], [SubjectProblem], [SubjectSolution], [MailTextProblem], [MailTextSolution], [IdUsuarioCriacao], [DataCriacao], [DataAlteracao], [NomeMsg]) VALUES (42, 21, NULL, NULL, NULL, N'<b>Login Failed - SQL Server - TOP 10 </b>', NULL, CAST(N'2020-04-01T15:51:03.380' AS DateTime), NULL, N'Check_LoginFailed')
 
            INSERT [dbo].[AlertaParametroMenssage] ([Id], [IdAlertaParametro], [SubjectProblem], [SubjectSolution], [MailTextProblem], [MailTextSolution], [IdUsuarioCriacao], [DataCriacao], [DataAlteracao], [NomeMsg]) VALUES (43, 21, NULL, NULL, NULL, N'<b>Espaço em Disco</b>', NULL, CAST(N'2020-04-01T15:52:08.270' AS DateTime), NULL, N'Check_EspacoDisco')

            INSERT [dbo].[AlertaParametroMenssage] ([Id], [IdAlertaParametro], [SubjectProblem], [SubjectSolution], [MailTextProblem], [MailTextSolution], [IdUsuarioCriacao], [DataCriacao], [DataAlteracao], [NomeMsg]) VALUES (44, 21, NULL, NULL, NULL, N'<b>Histórico dos últimos Backups - TOP 20 </b>', NULL, CAST(N'2020-04-01T15:52:08.270' AS DateTime), NULL, N'Check_HistBKP')

            INSERT [dbo].[AlertaParametroMenssage] ([Id], [IdAlertaParametro], [SubjectProblem], [SubjectSolution], [MailTextProblem], [MailTextSolution], [IdUsuarioCriacao], [DataCriacao], [DataAlteracao], [NomeMsg]) VALUES (45, 26, N'#Alerta - Indisponibilidade de Filas:', NULL, N'Prezados,<BR /><BR /> Identifiquei um problema de <b>Indisponibilidade de Filas</b> na instância mencionada no assunto do e-mail ,</b> a Fila <b style=""color: red; "">', NULL, NULL, CAST(N'2020-04-01T17:14:59.613' AS DateTime), NULL, N'Check_Filas') 

            INSERT [dbo].[AlertaParametroMenssage] ([Id], [IdAlertaParametro], [SubjectProblem], [SubjectSolution], [MailTextProblem], [MailTextSolution], [IdUsuarioCriacao], [DataCriacao], [DataAlteracao], [NomeMsg]) VALUES (46, 27, N'Alerta - #ArquivoBackup do banco:', N'Solução - #ArquivoBackup do banco: ', N'Prezados,<BR /><BR /> Identifiquei um problema, não encontrei os arquivos de backup dos bancos da tabela abaixo na instância: '+@@SERVERNAME+', favor verifique esta informação. <BR /> <BR />Com a dica abaixo é possível visualizar os diretórios onde os arquivos não constam diretamente pelo Management Studio, copie o script abaixo e informe o nome do banco antes de apertar clicar em executar.', N'Prezados,<BR /><BR /> Não existem mais arquivos de backup faltantes na Instância', NULL, CAST(N'2020-04-01T17:14:59.613' AS DateTime), NULL, N'Check_ArquivoBackup') 

            INSERT [dbo].[AlertaParametroMenssage] ([Id], [IdAlertaParametro], [SubjectProblem], [SubjectSolution], [MailTextProblem], [MailTextSolution], [IdUsuarioCriacao], [DataCriacao], [DataAlteracao], [NomeMsg]) VALUES (47, 18, N'Alerta - #AlwaysOn Status:', N'Solução - #AlwaysOn Status: ', N'Prezados,<BR /><BR /> Identifiquei um problema de AlwaysOn na instância: '+@@SERVERNAME+', favor verifique esta informação.', N'Prezados,<BR /><BR /> Não existem mais problemas de AlwaysOn na Instância: '+@@SERVERNAME+'', NULL, CAST(N'2020-04-01T17:14:59.613' AS DateTime), NULL, N'Check_AlwaysOn') 

            INSERT [dbo].[AlertaParametroMenssage] ([Id], [IdAlertaParametro], [SubjectProblem], [SubjectSolution], [MailTextProblem], [MailTextSolution], [IdUsuarioCriacao], [DataCriacao], [DataAlteracao], [NomeMsg]) VALUES (48, 29, N'Alerta #JobsAgendamentoFail - Falha de execução de Jobs Agendandos', N'Solucao #JobsAgendamentoFail', N'Prezados,<BR /><BR />Segue <b> jobs agendados internamente na SefaBase </b> que Falharam na instância mencionada no assunto do e-mail, verifique o relatório abaixo.', N'Prezados,<BR /><BR />Segue <b> TOP 5 execuções jobs agendados internamente na SefaBase </b>', NULL, CAST(N'2020-04-01T17:14:59.613' AS DateTime), NULL, NULL)   
            
            SET IDENTITY_INSERT [dbo].[AlertaParametroMenssage] OFF


            SET IDENTITY_INSERT [dbo].[AlertaEnvio] ON 

            INSERT [dbo].[AlertaEnvio] ([Id], [IdAlertaParametro], [IdTipoEnvio], [Ativo], [Des], [DataCriação], [DataAlteracao]) VALUES (1, 1, 20, 1, N'Processo Bloqueado - Telegram', CAST(N'2020-04-27T21:57:41.0000000' AS DateTime2), CAST(N'2020-04-27T21:57:41.0000000' AS DateTime2))

            INSERT [dbo].[AlertaEnvio] ([Id], [IdAlertaParametro], [IdTipoEnvio], [Ativo], [Des], [DataCriação], [DataAlteracao]) VALUES (2, 2, 20, 1, N'Arquivo de Log Full - Telegram', CAST(N'2020-04-27T21:58:36.7900000' AS DateTime2), CAST(N'2020-04-27T21:57:41.0000000' AS DateTime2))

            INSERT [dbo].[AlertaEnvio] ([Id], [IdAlertaParametro], [IdTipoEnvio], [Ativo], [Des], [DataCriação], [DataAlteracao]) VALUES (3, 3, 20, 1, N'Espaco Disco - Telegram', CAST(N'2020-04-27T21:57:41.0000000' AS DateTime2), CAST(N'2020-04-27T21:57:41.0000000' AS DateTime2))

            INSERT [dbo].[AlertaEnvio] ([Id], [IdAlertaParametro], [IdTipoEnvio], [Ativo], [Des], [DataCriação], [DataAlteracao]) VALUES (4, 4, 20, 1, N'Consumo CPU - Telegram', CAST(N'2020-04-27T21:58:36.7900000' AS DateTime2), CAST(N'2020-04-27T21:57:41.0000000' AS DateTime2))

            INSERT [dbo].[AlertaEnvio] ([Id], [IdAlertaParametro], [IdTipoEnvio], [Ativo], [Des], [DataCriação], [DataAlteracao]) VALUES (5, 5, 20, 1, N'Tempdb Utilizacao Arquivo MDF - Telegram', CAST(N'2020-04-27T21:57:41.0000000' AS DateTime2), CAST(N'2020-04-27T21:57:41.0000000' AS DateTime2))

            INSERT [dbo].[AlertaEnvio] ([Id], [IdAlertaParametro], [IdTipoEnvio], [Ativo], [Des], [DataCriação], [DataAlteracao]) VALUES (6, 6, 20, 1, N'Conexão SQL Server - Telegram', CAST(N'2020-04-27T21:58:36.7900000' AS DateTime2), CAST(N'2020-04-27T21:57:41.0000000' AS DateTime2))

            INSERT [dbo].[AlertaEnvio] ([Id], [IdAlertaParametro], [IdTipoEnvio], [Ativo], [Des], [DataCriação], [DataAlteracao]) VALUES (7, 7, 20, 1, N'Status Database - Telegram', CAST(N'2020-04-27T21:57:41.0000000' AS DateTime2), CAST(N'2020-04-27T21:57:41.0000000' AS DateTime2))

            INSERT [dbo].[AlertaEnvio] ([Id], [IdAlertaParametro], [IdTipoEnvio], [Ativo], [Des], [DataCriação], [DataAlteracao]) VALUES (8, 8, 20, 1, N'Página Corrompida - Telegram', CAST(N'2020-04-27T21:58:36.7900000' AS DateTime2), CAST(N'2020-04-27T21:57:41.0000000' AS DateTime2))

            INSERT [dbo].[AlertaEnvio] ([Id], [IdAlertaParametro], [IdTipoEnvio], [Ativo], [Des], [DataCriação], [DataAlteracao]) VALUES (9, 9, 20, 1, N'Queries Demoradas - Telegram', CAST(N'2020-04-27T21:57:41.0000000' AS DateTime2), CAST(N'2020-04-27T21:57:41.0000000' AS DateTime2))

            INSERT [dbo].[AlertaEnvio] ([Id], [IdAlertaParametro], [IdTipoEnvio], [Ativo], [Des], [DataCriação], [DataAlteracao]) VALUES (10, 10, 20, 1, N'Trace Queries Demoradas - Telegram', CAST(N'2020-04-27T21:58:36.7900000' AS DateTime2), CAST(N'2020-04-27T21:57:41.0000000' AS DateTime2))

            INSERT [dbo].[AlertaEnvio] ([Id], [IdAlertaParametro], [IdTipoEnvio], [Ativo], [Des], [DataCriação], [DataAlteracao]) VALUES (11, 11, 20, 1, N'Job Falha - Telegram', CAST(N'2020-04-27T21:57:41.0000000' AS DateTime2), CAST(N'2020-04-27T21:57:41.0000000' AS DateTime2))

            INSERT [dbo].[AlertaEnvio] ([Id], [IdAlertaParametro], [IdTipoEnvio], [Ativo], [Des], [DataCriação], [DataAlteracao]) VALUES (12, 12, 20, 1, N'SQL Server Reiniciado - Telegram', CAST(N'2020-04-27T21:58:36.7900000' AS DateTime2), CAST(N'2020-04-27T21:57:41.0000000' AS DateTime2))

            INSERT [dbo].[AlertaEnvio] ([Id], [IdAlertaParametro], [IdTipoEnvio], [Ativo], [Des], [DataCriação], [DataAlteracao]) VALUES (13, 13, 20, 1, N'Database Criada - Telegram', CAST(N'2020-04-27T21:57:41.0000000' AS DateTime2), CAST(N'2020-04-27T21:57:41.0000000' AS DateTime2))

            INSERT [dbo].[AlertaEnvio] ([Id], [IdAlertaParametro], [IdTipoEnvio], [Ativo], [Des], [DataCriação], [DataAlteracao]) VALUES (14, 14, 20, 1, N'Database sem Backup - Telegram', CAST(N'2020-04-27T21:58:36.7900000' AS DateTime2), CAST(N'2020-04-27T21:57:41.0000000' AS DateTime2))

            INSERT [dbo].[AlertaEnvio] ([Id], [IdAlertaParametro], [IdTipoEnvio], [Ativo], [Des], [DataCriação], [DataAlteracao]) VALUES (15, 15, 20, 1, N'Banco de Dados Corrompido - Telegram', CAST(N'2020-04-27T21:57:41.0000000' AS DateTime2), CAST(N'2020-04-27T21:57:41.0000000' AS DateTime2))

            INSERT [dbo].[AlertaEnvio] ([Id], [IdAlertaParametro], [IdTipoEnvio], [Ativo], [Des], [DataCriação], [DataAlteracao]) VALUES (16, 16, 20, 1, N'Processos em Execução - Telegram', CAST(N'2020-04-27T21:58:36.7900000' AS DateTime2), CAST(N'2020-04-27T21:57:41.0000000' AS DateTime2))

            INSERT [dbo].[AlertaEnvio] ([Id], [IdAlertaParametro], [IdTipoEnvio], [Ativo], [Des], [DataCriação], [DataAlteracao]) VALUES (17, 17, 20, 1, N'Alteracao database - Telegram', CAST(N'2020-04-27T21:57:41.0000000' AS DateTime2), CAST(N'2020-04-27T21:57:41.0000000' AS DateTime2))

            INSERT [dbo].[AlertaEnvio] ([Id], [IdAlertaParametro], [IdTipoEnvio], [Ativo], [Des], [DataCriação], [DataAlteracao]) VALUES (18, 18, 20, 1, N'AlwaysOn - Telegram', CAST(N'2020-04-27T21:58:36.7900000' AS DateTime2), CAST(N'2020-04-27T21:57:41.0000000' AS DateTime2))

            INSERT [dbo].[AlertaEnvio] ([Id], [IdAlertaParametro], [IdTipoEnvio], [Ativo], [Des], [DataCriação], [DataAlteracao]) VALUES (19, 19, 20, 1, N'Check DB - Telegram', CAST(N'2020-04-27T21:57:41.0000000' AS DateTime2), CAST(N'2020-04-27T21:57:41.0000000' AS DateTime2))

            INSERT [dbo].[AlertaEnvio] ([Id], [IdAlertaParametro], [IdTipoEnvio], [Ativo], [Des], [DataCriação], [DataAlteracao]) VALUES (20, 21, 20, 1, N'CheckList - Telegram', CAST(N'2020-04-27T21:58:36.7900000' AS DateTime2), CAST(N'2020-04-27T21:57:41.0000000' AS DateTime2))

            INSERT [dbo].[AlertaEnvio] ([Id], [IdAlertaParametro], [IdTipoEnvio], [Ativo], [Des], [DataCriação], [DataAlteracao]) VALUES (21, 23, 20, 1, N'Fragmentacao Indice - Telegram', CAST(N'2020-04-27T21:57:41.0000000' AS DateTime2), CAST(N'2020-04-27T21:57:41.0000000' AS DateTime2))

            INSERT [dbo].[AlertaEnvio] ([Id], [IdAlertaParametro], [IdTipoEnvio], [Ativo], [Des], [DataCriação], [DataAlteracao]) VALUES (22, 24, 20, 1, N'THistorico Erros - Telegram', CAST(N'2020-04-27T21:58:36.7900000' AS DateTime2), CAST(N'2020-04-27T21:57:41.0000000' AS DateTime2))

            INSERT [dbo].[AlertaEnvio] ([Id], [IdAlertaParametro], [IdTipoEnvio], [Ativo], [Des], [DataCriação], [DataAlteracao]) VALUES (23, 1, 22, 1, N'Processo Bloqueado - Teams', CAST(N'2020-04-27T21:57:41.0000000' AS DateTime2), CAST(N'2020-04-27T21:57:41.0000000' AS DateTime2))

            INSERT [dbo].[AlertaEnvio] ([Id], [IdAlertaParametro], [IdTipoEnvio], [Ativo], [Des], [DataCriação], [DataAlteracao]) VALUES (24, 2, 22, 1, N'Arquivo de Log Full - Teams', CAST(N'2020-04-27T21:58:36.7900000' AS DateTime2), CAST(N'2020-04-27T21:57:41.0000000' AS DateTime2))

            INSERT [dbo].[AlertaEnvio] ([Id], [IdAlertaParametro], [IdTipoEnvio], [Ativo], [Des], [DataCriação], [DataAlteracao]) VALUES (25, 3, 22, 1, N'Espaco Disco - Teams', CAST(N'2020-04-27T21:57:41.0000000' AS DateTime2), CAST(N'2020-04-27T21:57:41.0000000' AS DateTime2))

            INSERT [dbo].[AlertaEnvio] ([Id], [IdAlertaParametro], [IdTipoEnvio], [Ativo], [Des], [DataCriação], [DataAlteracao]) VALUES (26, 4, 22, 1, N'Consumo CPU - Teams', CAST(N'2020-04-27T21:58:36.7900000' AS DateTime2), CAST(N'2020-04-27T21:57:41.0000000' AS DateTime2))

            INSERT [dbo].[AlertaEnvio] ([Id], [IdAlertaParametro], [IdTipoEnvio], [Ativo], [Des], [DataCriação], [DataAlteracao]) VALUES (27, 5, 22, 1, N'Tempdb Utilizacao Arquivo MDF - Teams', CAST(N'2020-04-27T21:57:41.0000000' AS DateTime2), CAST(N'2020-04-27T21:57:41.0000000' AS DateTime2))

            INSERT [dbo].[AlertaEnvio] ([Id], [IdAlertaParametro], [IdTipoEnvio], [Ativo], [Des], [DataCriação], [DataAlteracao]) VALUES (28, 6, 22, 1, N'Conexão SQL Server - Teams', CAST(N'2020-04-27T21:58:36.7900000' AS DateTime2), CAST(N'2020-04-27T21:57:41.0000000' AS DateTime2))

            INSERT [dbo].[AlertaEnvio] ([Id], [IdAlertaParametro], [IdTipoEnvio], [Ativo], [Des], [DataCriação], [DataAlteracao]) VALUES (29, 7, 22, 1, N'Status Database - Teams', CAST(N'2020-04-27T21:57:41.0000000' AS DateTime2), CAST(N'2020-04-27T21:57:41.0000000' AS DateTime2))

            INSERT [dbo].[AlertaEnvio] ([Id], [IdAlertaParametro], [IdTipoEnvio], [Ativo], [Des], [DataCriação], [DataAlteracao]) VALUES (30, 8, 22, 1, N'Página Corrompida - Teams', CAST(N'2020-04-27T21:58:36.7900000' AS DateTime2), CAST(N'2020-04-27T21:57:41.0000000' AS DateTime2))

            INSERT [dbo].[AlertaEnvio] ([Id], [IdAlertaParametro], [IdTipoEnvio], [Ativo], [Des], [DataCriação], [DataAlteracao]) VALUES (31, 9, 22, 1, N'Queries Demoradas - Teams', CAST(N'2020-04-27T21:57:41.0000000' AS DateTime2), CAST(N'2020-04-27T21:57:41.0000000' AS DateTime2))

            INSERT [dbo].[AlertaEnvio] ([Id], [IdAlertaParametro], [IdTipoEnvio], [Ativo], [Des], [DataCriação], [DataAlteracao]) VALUES (32, 10, 22, 1, N'Trace Queries Demoradas - Teams', CAST(N'2020-04-27T21:58:36.7900000' AS DateTime2), CAST(N'2020-04-27T21:57:41.0000000' AS DateTime2))

            INSERT [dbo].[AlertaEnvio] ([Id], [IdAlertaParametro], [IdTipoEnvio], [Ativo], [Des], [DataCriação], [DataAlteracao]) VALUES (33, 11, 22, 1, N'Job Falha - Teams', CAST(N'2020-04-27T21:57:41.0000000' AS DateTime2), CAST(N'2020-04-27T21:57:41.0000000' AS DateTime2))

            INSERT [dbo].[AlertaEnvio] ([Id], [IdAlertaParametro], [IdTipoEnvio], [Ativo], [Des], [DataCriação], [DataAlteracao]) VALUES (34, 12, 22, 1, N'SQL Server Reiniciado - Teams', CAST(N'2020-04-27T21:58:36.7900000' AS DateTime2), CAST(N'2020-04-27T21:57:41.0000000' AS DateTime2))

            INSERT [dbo].[AlertaEnvio] ([Id], [IdAlertaParametro], [IdTipoEnvio], [Ativo], [Des], [DataCriação], [DataAlteracao]) VALUES (35, 13, 22, 1, N'Database Criada - Teams', CAST(N'2020-04-27T21:57:41.0000000' AS DateTime2), CAST(N'2020-04-27T21:57:41.0000000' AS DateTime2))

            INSERT [dbo].[AlertaEnvio] ([Id], [IdAlertaParametro], [IdTipoEnvio], [Ativo], [Des], [DataCriação], [DataAlteracao]) VALUES (36, 14, 22, 1, N'Database sem Backup - Teams', CAST(N'2020-04-27T21:58:36.7900000' AS DateTime2), CAST(N'2020-04-27T21:57:41.0000000' AS DateTime2))

            INSERT [dbo].[AlertaEnvio] ([Id], [IdAlertaParametro], [IdTipoEnvio], [Ativo], [Des], [DataCriação], [DataAlteracao]) VALUES (37, 15, 22, 1, N'Banco de Dados Corrompido - Teams', CAST(N'2020-04-27T21:57:41.0000000' AS DateTime2), CAST(N'2020-04-27T21:57:41.0000000' AS DateTime2))

            INSERT [dbo].[AlertaEnvio] ([Id], [IdAlertaParametro], [IdTipoEnvio], [Ativo], [Des], [DataCriação], [DataAlteracao]) VALUES (38, 16, 22, 1, N'Processos em Execução - Teams', CAST(N'2020-04-27T21:58:36.7900000' AS DateTime2), CAST(N'2020-04-27T21:57:41.0000000' AS DateTime2))

            INSERT [dbo].[AlertaEnvio] ([Id], [IdAlertaParametro], [IdTipoEnvio], [Ativo], [Des], [DataCriação], [DataAlteracao]) VALUES (39, 17, 22, 1, N'Alteracao database - Teams', CAST(N'2020-04-27T21:57:41.0000000' AS DateTime2), CAST(N'2020-04-27T21:57:41.0000000' AS DateTime2))

            INSERT [dbo].[AlertaEnvio] ([Id], [IdAlertaParametro], [IdTipoEnvio], [Ativo], [Des], [DataCriação], [DataAlteracao]) VALUES (40, 18, 22, 1, N'AlwaysOn - Teams', CAST(N'2020-04-27T21:58:36.7900000' AS DateTime2), CAST(N'2020-04-27T21:57:41.0000000' AS DateTime2))

            INSERT [dbo].[AlertaEnvio] ([Id], [IdAlertaParametro], [IdTipoEnvio], [Ativo], [Des], [DataCriação], [DataAlteracao]) VALUES (41, 19, 22, 1, N'Check DB - Teams', CAST(N'2020-04-27T21:57:41.0000000' AS DateTime2), CAST(N'2020-04-27T21:57:41.0000000' AS DateTime2))

            INSERT [dbo].[AlertaEnvio] ([Id], [IdAlertaParametro], [IdTipoEnvio], [Ativo], [Des], [DataCriação], [DataAlteracao]) VALUES (42, 21, 22, 1, N'CheckList - Teams', CAST(N'2020-04-27T21:58:36.7900000' AS DateTime2), CAST(N'2020-04-27T21:57:41.0000000' AS DateTime2))

            INSERT [dbo].[AlertaEnvio] ([Id], [IdAlertaParametro], [IdTipoEnvio], [Ativo], [Des], [DataCriação], [DataAlteracao]) VALUES (43, 23, 22, 1, N'Fragmentacao Indice - Teams', CAST(N'2020-04-27T21:57:41.0000000' AS DateTime2), CAST(N'2020-04-27T21:57:41.0000000' AS DateTime2))

            INSERT [dbo].[AlertaEnvio] ([Id], [IdAlertaParametro], [IdTipoEnvio], [Ativo], [Des], [DataCriação], [DataAlteracao]) VALUES (44, 24, 22, 1, N'THistorico Erros - Teams', CAST(N'2020-04-27T21:58:36.7900000' AS DateTime2), CAST(N'2020-04-27T21:57:41.0000000' AS DateTime2))

            INSERT [dbo].[AlertaEnvio] ([Id], [IdAlertaParametro], [IdTipoEnvio], [Ativo], [Des], [DataCriação], [DataAlteracao]) VALUES (45, 1, 25, 1, N'Processo Bloqueado - Email', CAST(N'2020-04-27T21:57:41.0000000' AS DateTime2), CAST(N'2020-04-27T21:57:41.0000000' AS DateTime2))

            INSERT [dbo].[AlertaEnvio] ([Id], [IdAlertaParametro], [IdTipoEnvio], [Ativo], [Des], [DataCriação], [DataAlteracao]) VALUES (46, 2, 25, 1, N'Arquivo de Log Full - Email', CAST(N'2020-04-27T21:58:36.7900000' AS DateTime2), CAST(N'2020-04-27T21:57:41.0000000' AS DateTime2))

            INSERT [dbo].[AlertaEnvio] ([Id], [IdAlertaParametro], [IdTipoEnvio], [Ativo], [Des], [DataCriação], [DataAlteracao]) VALUES (47, 3, 25, 1, N'Espaco Disco - Email', CAST(N'2020-04-27T21:57:41.0000000' AS DateTime2), CAST(N'2020-04-27T21:57:41.0000000' AS DateTime2))

            INSERT [dbo].[AlertaEnvio] ([Id], [IdAlertaParametro], [IdTipoEnvio], [Ativo], [Des], [DataCriação], [DataAlteracao]) VALUES (48, 4, 25, 1, N'Consumo CPU - Email', CAST(N'2020-04-27T21:58:36.7900000' AS DateTime2), CAST(N'2020-04-27T21:57:41.0000000' AS DateTime2))

            INSERT [dbo].[AlertaEnvio] ([Id], [IdAlertaParametro], [IdTipoEnvio], [Ativo], [Des], [DataCriação], [DataAlteracao]) VALUES (49, 5, 25, 1, N'Tempdb Utilizacao Arquivo MDF - Email', CAST(N'2020-04-27T21:57:41.0000000' AS DateTime2), CAST(N'2020-04-27T21:57:41.0000000' AS DateTime2))

            INSERT [dbo].[AlertaEnvio] ([Id], [IdAlertaParametro], [IdTipoEnvio], [Ativo], [Des], [DataCriação], [DataAlteracao]) VALUES (50, 6, 25, 1, N'Conexão SQL Server - Email', CAST(N'2020-04-27T21:58:36.7900000' AS DateTime2), CAST(N'2020-04-27T21:57:41.0000000' AS DateTime2))

            INSERT [dbo].[AlertaEnvio] ([Id], [IdAlertaParametro], [IdTipoEnvio], [Ativo], [Des], [DataCriação], [DataAlteracao]) VALUES (51, 7, 25, 1, N'Status Database - Email', CAST(N'2020-04-27T21:57:41.0000000' AS DateTime2), CAST(N'2020-04-27T21:57:41.0000000' AS DateTime2))

            INSERT [dbo].[AlertaEnvio] ([Id], [IdAlertaParametro], [IdTipoEnvio], [Ativo], [Des], [DataCriação], [DataAlteracao]) VALUES (52, 8, 25, 1, N'Página Corrompida - Email', CAST(N'2020-04-27T21:58:36.7900000' AS DateTime2), CAST(N'2020-04-27T21:57:41.0000000' AS DateTime2))

            INSERT [dbo].[AlertaEnvio] ([Id], [IdAlertaParametro], [IdTipoEnvio], [Ativo], [Des], [DataCriação], [DataAlteracao]) VALUES (53, 9, 25, 1, N'Queries Demoradas - Email', CAST(N'2020-04-27T21:57:41.0000000' AS DateTime2), CAST(N'2020-04-27T21:57:41.0000000' AS DateTime2))

            INSERT [dbo].[AlertaEnvio] ([Id], [IdAlertaParametro], [IdTipoEnvio], [Ativo], [Des], [DataCriação], [DataAlteracao]) VALUES (54, 10, 25, 1, N'Trace Queries Demoradas - Email', CAST(N'2020-04-27T21:58:36.7900000' AS DateTime2), CAST(N'2020-04-27T21:57:41.0000000' AS DateTime2))

            INSERT [dbo].[AlertaEnvio] ([Id], [IdAlertaParametro], [IdTipoEnvio], [Ativo], [Des], [DataCriação], [DataAlteracao]) VALUES (55, 11, 25, 1, N'Job Falha - Email', CAST(N'2020-04-27T21:57:41.0000000' AS DateTime2), CAST(N'2020-04-27T21:57:41.0000000' AS DateTime2))

            INSERT [dbo].[AlertaEnvio] ([Id], [IdAlertaParametro], [IdTipoEnvio], [Ativo], [Des], [DataCriação], [DataAlteracao]) VALUES (56, 12, 25, 1, N'SQL Server Reiniciado - Email', CAST(N'2020-04-27T21:58:36.7900000' AS DateTime2), CAST(N'2020-04-27T21:57:41.0000000' AS DateTime2))

            INSERT [dbo].[AlertaEnvio] ([Id], [IdAlertaParametro], [IdTipoEnvio], [Ativo], [Des], [DataCriação], [DataAlteracao]) VALUES (57, 13, 25, 1, N'Database Criada - Email', CAST(N'2020-04-27T21:57:41.0000000' AS DateTime2), CAST(N'2020-04-27T21:57:41.0000000' AS DateTime2))

            INSERT [dbo].[AlertaEnvio] ([Id], [IdAlertaParametro], [IdTipoEnvio], [Ativo], [Des], [DataCriação], [DataAlteracao]) VALUES (58, 14, 25, 1, N'Database sem Backup - Email', CAST(N'2020-04-27T21:58:36.7900000' AS DateTime2), CAST(N'2020-04-27T21:57:41.0000000' AS DateTime2))

            INSERT [dbo].[AlertaEnvio] ([Id], [IdAlertaParametro], [IdTipoEnvio], [Ativo], [Des], [DataCriação], [DataAlteracao]) VALUES (59, 15, 25, 1, N'Banco de Dados Corrompido - Email', CAST(N'2020-04-27T21:57:41.0000000' AS DateTime2), CAST(N'2020-04-27T21:57:41.0000000' AS DateTime2))

            INSERT [dbo].[AlertaEnvio] ([Id], [IdAlertaParametro], [IdTipoEnvio], [Ativo], [Des], [DataCriação], [DataAlteracao]) VALUES (60, 16, 25, 1, N'Processos em Execução - Email', CAST(N'2020-04-27T21:58:36.7900000' AS DateTime2), CAST(N'2020-04-27T21:57:41.0000000' AS DateTime2))

            INSERT [dbo].[AlertaEnvio] ([Id], [IdAlertaParametro], [IdTipoEnvio], [Ativo], [Des], [DataCriação], [DataAlteracao]) VALUES (61, 17, 25, 1, N'Alteracao database - Email', CAST(N'2020-04-27T21:57:41.0000000' AS DateTime2), CAST(N'2020-04-27T21:57:41.0000000' AS DateTime2))

            INSERT [dbo].[AlertaEnvio] ([Id], [IdAlertaParametro], [IdTipoEnvio], [Ativo], [Des], [DataCriação], [DataAlteracao]) VALUES (62, 18, 25, 1, N'AlwaysOn - Email', CAST(N'2020-04-27T21:58:36.7900000' AS DateTime2), CAST(N'2020-04-27T21:57:41.0000000' AS DateTime2))

            INSERT [dbo].[AlertaEnvio] ([Id], [IdAlertaParametro], [IdTipoEnvio], [Ativo], [Des], [DataCriação], [DataAlteracao]) VALUES (63, 19, 25, 1, N'Check DB - Email', CAST(N'2020-04-27T21:57:41.0000000' AS DateTime2), CAST(N'2020-04-27T21:57:41.0000000' AS DateTime2))

            INSERT [dbo].[AlertaEnvio] ([Id], [IdAlertaParametro], [IdTipoEnvio], [Ativo], [Des], [DataCriação], [DataAlteracao]) VALUES (64, 21, 25, 1, N'CheckList - Email', CAST(N'2020-04-27T21:58:36.7900000' AS DateTime2), CAST(N'2020-04-27T21:57:41.0000000' AS DateTime2))

            INSERT [dbo].[AlertaEnvio] ([Id], [IdAlertaParametro], [IdTipoEnvio], [Ativo], [Des], [DataCriação], [DataAlteracao]) VALUES (65, 23, 25, 1, N'Fragmentacao Indice - Email', CAST(N'2020-04-27T21:57:41.0000000' AS DateTime2), CAST(N'2020-04-27T21:57:41.0000000' AS DateTime2))

            INSERT [dbo].[AlertaEnvio] ([Id], [IdAlertaParametro], [IdTipoEnvio], [Ativo], [Des], [DataCriação], [DataAlteracao]) VALUES (66, 24, 25, 1, N'THistorico Erros - Email', CAST(N'2020-04-27T21:58:36.7900000' AS DateTime2), CAST(N'2020-04-27T21:57:41.0000000' AS DateTime2))

            INSERT [dbo].[AlertaEnvio] ([Id], [IdAlertaParametro], [IdTipoEnvio], [Ativo], [Des], [DataCriação], [DataAlteracao]) VALUES (67, 26, 20, 1, N'Alerta Queue - Telegram', CAST(N'2020-04-27T21:58:36.7900000' AS DateTime2), CAST(N'2020-04-27T21:57:41.0000000' AS DateTime2))
            
            INSERT [dbo].[AlertaEnvio] ([Id], [IdAlertaParametro], [IdTipoEnvio], [Ativo], [Des], [DataCriação], [DataAlteracao]) VALUES (68, 26, 22, 1, N'Alerta Queue - Teams', CAST(N'2020-04-27T21:58:36.7900000' AS DateTime2), CAST(N'2020-04-27T21:57:41.0000000' AS DateTime2))
            
            INSERT [dbo].[AlertaEnvio] ([Id], [IdAlertaParametro], [IdTipoEnvio], [Ativo], [Des], [DataCriação], [DataAlteracao]) VALUES (69, 26, 25, 1, N'Alerta Queue - Email', CAST(N'2020-04-27T21:58:36.7900000' AS DateTime2), CAST(N'2020-04-27T21:57:41.0000000' AS DateTime2))

            INSERT [dbo].[AlertaEnvio] ([Id], [IdAlertaParametro], [IdTipoEnvio], [Ativo], [Des], [DataCriação], [DataAlteracao]) VALUES (70, 27, 20, 1, N'Alerta File DB - Telegram', CAST(N'2020-04-27T21:58:36.7900000' AS DateTime2), CAST(N'2020-04-27T21:57:41.0000000' AS DateTime2))
            
            INSERT [dbo].[AlertaEnvio] ([Id], [IdAlertaParametro], [IdTipoEnvio], [Ativo], [Des], [DataCriação], [DataAlteracao]) VALUES (71, 27, 22, 0, N'Alerta File DB - Teams', CAST(N'2020-04-27T21:58:36.7900000' AS DateTime2), CAST(N'2020-04-27T21:57:41.0000000' AS DateTime2))

            INSERT [dbo].[AlertaEnvio] ([Id], [IdAlertaParametro], [IdTipoEnvio], [Ativo], [Des], [DataCriação], [DataAlteracao]) VALUES (72, 27, 25, 1, N'Alerta File DB - Email', CAST(N'2020-04-27T21:58:36.7900000' AS DateTime2), CAST(N'2020-04-27T21:57:41.0000000' AS DateTime2))

            INSERT [dbo].[AlertaEnvio] ([Id], [IdAlertaParametro], [IdTipoEnvio], [Ativo], [Des], [DataCriação], [DataAlteracao]) VALUES (73, 29, 20, 0, N'Job Agendamento Falha - Telegram', CAST(N'2020-04-27T21:58:36.7900000' AS DateTime2), CAST(N'2020-04-27T21:57:41.0000000' AS DateTime2))
            
            INSERT [dbo].[AlertaEnvio] ([Id], [IdAlertaParametro], [IdTipoEnvio], [Ativo], [Des], [DataCriação], [DataAlteracao]) VALUES (74, 29, 25, 1, N'Job Agendamento Falha - Email', CAST(N'2020-04-27T21:58:36.7900000' AS DateTime2), CAST(N'2020-04-27T21:57:41.0000000' AS DateTime2))
            
            INSERT [dbo].[AlertaEnvio] ([Id], [IdAlertaParametro], [IdTipoEnvio], [Ativo], [Des], [DataCriação], [DataAlteracao]) VALUES (75, 29, 22, 0, N'Job Agendamento Falha - Teams', CAST(N'2020-04-27T21:58:36.7900000' AS DateTime2), CAST(N'2020-04-27T21:57:41.0000000' AS DateTime2))
            
            INSERT [dbo].[AlertaEnvio] ([Id], [IdAlertaParametro], [IdTipoEnvio], [Ativo], [Des], [DataCriação], [DataAlteracao]) VALUES (76, 1, 28, 0, N'Processo Bloqueado - Zabbix Sender', CAST(N'2021-05-21T16:37:31.6866667' AS DateTime2), CAST(N'2021-05-21T16:37:31.6866667' AS DateTime2))

            INSERT [dbo].[AlertaEnvio] ([Id], [IdAlertaParametro], [IdTipoEnvio], [Ativo], [Des], [DataCriação], [DataAlteracao]) VALUES (77, 2, 28, 0, N'Arquivo de Log Full - Zabbix Sender', CAST(N'2021-05-21T16:37:31.6866667' AS DateTime2), CAST(N'2021-05-21T16:37:31.6866667' AS DateTime2))
            
            INSERT [dbo].[AlertaEnvio] ([Id], [IdAlertaParametro], [IdTipoEnvio], [Ativo], [Des], [DataCriação], [DataAlteracao]) VALUES (78, 3, 28, 0, N'Espaco Disco - Zabbix Sender', CAST(N'2021-05-21T16:37:31.6866667' AS DateTime2), CAST(N'2021-05-21T16:37:31.6866667' AS DateTime2))
            
            INSERT [dbo].[AlertaEnvio] ([Id], [IdAlertaParametro], [IdTipoEnvio], [Ativo], [Des], [DataCriação], [DataAlteracao]) VALUES (79, 4, 28, 0, N'Consumo CPU - Zabbix Sender', CAST(N'2021-05-21T16:37:31.6866667' AS DateTime2), CAST(N'2021-05-21T16:37:31.6866667' AS DateTime2))
            
            INSERT [dbo].[AlertaEnvio] ([Id], [IdAlertaParametro], [IdTipoEnvio], [Ativo], [Des], [DataCriação], [DataAlteracao]) VALUES (80, 5, 28, 0, N'Tempdb Utilizacao Arquivo MDF - Zabbix Sender', CAST(N'2021-05-21T16:37:31.6866667' AS DateTime2), CAST(N'2021-05-21T16:37:31.6866667' AS DateTime2))
            
            INSERT [dbo].[AlertaEnvio] ([Id], [IdAlertaParametro], [IdTipoEnvio], [Ativo], [Des], [DataCriação], [DataAlteracao]) VALUES (81, 6, 28, 0, N'Conexão SQL Server - Zabbix Sender', CAST(N'2021-05-21T16:37:31.6866667' AS DateTime2), CAST(N'2021-05-21T16:37:31.6866667' AS DateTime2))
            
            INSERT [dbo].[AlertaEnvio] ([Id], [IdAlertaParametro], [IdTipoEnvio], [Ativo], [Des], [DataCriação], [DataAlteracao]) VALUES (82, 7, 28, 0, N'Status Database - Zabbix Sender', CAST(N'2021-05-21T16:37:31.6866667' AS DateTime2), CAST(N'2021-05-21T16:37:31.6866667' AS DateTime2))
            
            INSERT [dbo].[AlertaEnvio] ([Id], [IdAlertaParametro], [IdTipoEnvio], [Ativo], [Des], [DataCriação], [DataAlteracao]) VALUES (83, 8, 28, 0, N'Página Corrompida - Zabbix Sender', CAST(N'2021-05-21T16:37:31.6866667' AS DateTime2), CAST(N'2021-05-21T16:37:31.6866667' AS DateTime2))
            
            INSERT [dbo].[AlertaEnvio] ([Id], [IdAlertaParametro], [IdTipoEnvio], [Ativo], [Des], [DataCriação], [DataAlteracao]) VALUES (84, 9, 28, 0, N'Queries Demoradas - Zabbix Sender', CAST(N'2021-05-21T16:37:31.6866667' AS DateTime2), CAST(N'2021-05-21T16:37:31.6866667' AS DateTime2))
            
            INSERT [dbo].[AlertaEnvio] ([Id], [IdAlertaParametro], [IdTipoEnvio], [Ativo], [Des], [DataCriação], [DataAlteracao]) VALUES (85, 10, 28, 0, N'Trace Queries Demoradas - Zabbix Sender', CAST(N'2021-05-21T16:37:31.6866667' AS DateTime2), CAST(N'2021-05-21T16:37:31.6866667' AS DateTime2))
            
            INSERT [dbo].[AlertaEnvio] ([Id], [IdAlertaParametro], [IdTipoEnvio], [Ativo], [Des], [DataCriação], [DataAlteracao]) VALUES (86, 11, 28, 0, N'Job Falha - Zabbix Sender', CAST(N'2021-05-21T16:37:31.6866667' AS DateTime2), CAST(N'2021-05-21T16:37:31.6866667' AS DateTime2))
            
            INSERT [dbo].[AlertaEnvio] ([Id], [IdAlertaParametro], [IdTipoEnvio], [Ativo], [Des], [DataCriação], [DataAlteracao]) VALUES (87, 12, 28, 0, N'SQL Server Reiniciado - Zabbix Sender', CAST(N'2021-05-21T16:37:31.6866667' AS DateTime2), CAST(N'2021-05-21T16:37:31.6866667' AS DateTime2))
            
            INSERT [dbo].[AlertaEnvio] ([Id], [IdAlertaParametro], [IdTipoEnvio], [Ativo], [Des], [DataCriação], [DataAlteracao]) VALUES (88, 13, 28, 0, N'Database Criada - Zabbix Sender', CAST(N'2021-05-21T16:37:31.6866667' AS DateTime2), CAST(N'2021-05-21T16:37:31.6866667' AS DateTime2))
            
            INSERT [dbo].[AlertaEnvio] ([Id], [IdAlertaParametro], [IdTipoEnvio], [Ativo], [Des], [DataCriação], [DataAlteracao]) VALUES (89, 14, 28, 0, N'Database sem Backup - Zabbix Sender', CAST(N'2021-05-21T16:37:31.6866667' AS DateTime2), CAST(N'2021-05-21T16:37:31.6866667' AS DateTime2))
            
            INSERT [dbo].[AlertaEnvio] ([Id], [IdAlertaParametro], [IdTipoEnvio], [Ativo], [Des], [DataCriação], [DataAlteracao]) VALUES (90, 15, 28, 0, N'Banco de Dados Corrompido - Zabbix Sender', CAST(N'2021-05-21T16:37:31.6866667' AS DateTime2), CAST(N'2021-05-21T16:37:31.6866667' AS DateTime2))
            
            INSERT [dbo].[AlertaEnvio] ([Id], [IdAlertaParametro], [IdTipoEnvio], [Ativo], [Des], [DataCriação], [DataAlteracao]) VALUES (91, 16, 28, 0, N'Processos em Execução - Zabbix Sender', CAST(N'2021-05-21T16:37:31.6866667' AS DateTime2), CAST(N'2021-05-21T16:37:31.6866667' AS DateTime2))
            
            INSERT [dbo].[AlertaEnvio] ([Id], [IdAlertaParametro], [IdTipoEnvio], [Ativo], [Des], [DataCriação], [DataAlteracao]) VALUES (92, 17, 28, 0, N'Alteracao database - Zabbix Sender', CAST(N'2021-05-21T16:37:31.6866667' AS DateTime2), CAST(N'2021-05-21T16:37:31.6866667' AS DateTime2))
            
            INSERT [dbo].[AlertaEnvio] ([Id], [IdAlertaParametro], [IdTipoEnvio], [Ativo], [Des], [DataCriação], [DataAlteracao]) VALUES (93, 18, 28, 0, N'AlwaysOn - Zabbix Sender', CAST(N'2021-05-21T16:37:31.6866667' AS DateTime2), CAST(N'2021-05-21T16:37:31.6866667' AS DateTime2))
            
            INSERT [dbo].[AlertaEnvio] ([Id], [IdAlertaParametro], [IdTipoEnvio], [Ativo], [Des], [DataCriação], [DataAlteracao]) VALUES (94, 19, 28, 0, N'Check DB - Zabbix Sender', CAST(N'2021-05-21T16:37:31.6866667' AS DateTime2), CAST(N'2021-05-21T16:37:31.6866667' AS DateTime2))
            
            INSERT [dbo].[AlertaEnvio] ([Id], [IdAlertaParametro], [IdTipoEnvio], [Ativo], [Des], [DataCriação], [DataAlteracao]) VALUES (95, 21, 28, 0, N'CheckList - Zabbix Sender', CAST(N'2021-05-21T16:37:31.6866667' AS DateTime2), CAST(N'2021-05-21T16:37:31.6866667' AS DateTime2))
            
            INSERT [dbo].[AlertaEnvio] ([Id], [IdAlertaParametro], [IdTipoEnvio], [Ativo], [Des], [DataCriação], [DataAlteracao]) VALUES (96, 23, 28, 0, N'Fragmentacao Indice - Zabbix Sender', CAST(N'2021-05-21T16:37:31.6866667' AS DateTime2), CAST(N'2021-05-21T16:37:31.6866667' AS DateTime2))
            
            INSERT [dbo].[AlertaEnvio] ([Id], [IdAlertaParametro], [IdTipoEnvio], [Ativo], [Des], [DataCriação], [DataAlteracao]) VALUES (97, 24, 28, 0, N'Historico Erros - Zabbix Sender', CAST(N'2021-05-21T16:37:31.6866667' AS DateTime2), CAST(N'2021-05-21T16:37:31.6866667' AS DateTime2))
            
            INSERT [dbo].[AlertaEnvio] ([Id], [IdAlertaParametro], [IdTipoEnvio], [Ativo], [Des], [DataCriação], [DataAlteracao]) VALUES (98, 26, 28, 0, N'Alerta Queue - Zabbix Sender', CAST(N'2021-05-21T16:37:31.6866667' AS DateTime2), CAST(N'2021-05-21T16:37:31.6866667' AS DateTime2))
            
            INSERT [dbo].[AlertaEnvio] ([Id], [IdAlertaParametro], [IdTipoEnvio], [Ativo], [Des], [DataCriação], [DataAlteracao]) VALUES (99, 27, 28, 1, N'Alerta File DB - Zabbix Sender', CAST(N'2021-05-21T16:37:31.6866667' AS DateTime2), CAST(N'2021-05-21T16:37:31.6866667' AS DateTime2))
            
            INSERT [dbo].[AlertaEnvio] ([Id], [IdAlertaParametro], [IdTipoEnvio], [Ativo], [Des], [DataCriação], [DataAlteracao]) VALUES (100, 29, 28, 0, N'Job Agendamento Falha - Zabbix Sender', CAST(N'2021-05-21T16:37:31.6866667' AS DateTime2), CAST(N'2021-05-21T16:37:31.6866667' AS DateTime2))

            SET IDENTITY_INSERT [dbo].[AlertaEnvio] OFF
            
 
            SET IDENTITY_INSERT [dbo].[LayoutHtmlCss] ON 
 
            INSERT [dbo].[LayoutHtmlCss] ([IdLayout], [NomeLayout], [DescricaoCSS]) VALUES (1, N'Layout Fundo Azul Escuro Letra Branca', N'body {font-family: ""Arial""; font-size: 12px; }
              table { padding: 0; border - spacing: 0; border - collapse: collapse; }
                        th {
                        padding: 10px; font - weight: bold; border: 1px solid #cacaca; color: #fff; background: #333333; }
              tr { padding: 0; }
              .subtitulo td {
                            border: 1px solid #cacaca; color: #fff; background: #777777; }
              td {
                                padding: 5px; border: 1px solid #cacaca; margin:0; }')
 
            INSERT[dbo].[LayoutHtmlCss]([IdLayout], [NomeLayout], [DescricaoCSS]) VALUES(2, N'Layout Fundo Verde Letra Branca ', N'body {font-family: ""Arial""; font-size: 12px; }

            table { padding: 0; border - spacing: 0; border - collapse: collapse; }
                    th { padding: 10px; font-weight: bold; border: 1px solid #000; color: #fff; background: #0D3D50; }
            tr { padding: 0; }
            .subtitulo td
                {
                    border: 1px solid #cacaca; color: #fff; background: #217D89; }
            td
                    {
                    padding: 5px; border: 1px solid #cacaca; margin:0; }')
 
            SET IDENTITY_INSERT[dbo].[LayoutHtmlCss]
                    OFF

            SET IDENTITY_INSERT [dbo].[MailAssinatura]
                    ON

            INSERT [dbo].[MailAssinatura] ([Id], [Assinatura], [Descricao], [Ativo], [DataCriacao]) VALUES (1, N'<BR />
				             Atte
				             <BR />
				             <BR />
				             <BR />
				             <BR />
				             <!--a href=""http://facta.com.br"" target=”_blank”--> 
				             <!--img src=""https://statics.facta.com.br/imagens/facta/site/facta-topo-md.png"" height=""100"" width=""400""/></a-->
				             <BR />
                                 <div>
        			             <hr />
	   			               <em>Aviso: este e-mail foi enviado automaticamente por nosso sistema, favor não respond&ecirc;-lo.</em></div>
                                 &nbsp;
                                 <BR />
                                 <BR />
                                 Nota de Confidencialidade: A informação contida neste documento é para uso único e exclusivo da pessoa a quem se destina, e pode
                                 tratar-se de assunto confidencial. Se você não é o destinatário, por favor, notifique-nos imediatamente e destrua o documento. Não leia
                                 o conteúdo para nenhuma outra pessoa, nem tome quaisquer notas, pois ambos os procedimentos podem ser punidos legalmente.', N'Assinatura 01', 1, CAST(N'2016-10-30T00:00:00.0000000' AS DateTime2))
 
            SET IDENTITY_INSERT [dbo].[MailAssinatura]
                    OFF

            SET ANSI_PADDING ON

            /****** Object:  Index [UQ__LayoutHt__0C8481F744DABB8C]    Script Date: 05/04/2020 23:24:50 ******/
            ALTER TABLE [dbo].[LayoutHtmlCss]
                    ADD UNIQUE NONCLUSTERED 
            (
	            [NomeLayout]
                    ASC
            )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]


                    --ALTER TABLE [dbo].[AlertaMsgToken]
                    --CHECK CONSTRAINT [FK_AlertaMsgToken_AlertaParametro] 

                    --ALTER TABLE [dbo].[AlertaParametro]
                    --CHECK CONSTRAINT [FK_AlertaParametro_AlertaMsgToken]

                    --ALTER TABLE [dbo].[AlertaParametro]
                    --CHECK CONSTRAINT [FK_AlertaParametro_AlertaMsgToken_01]

                    --ALTER TABLE [dbo].[AlertaParametro]
                    -- CHECK CONSTRAINT [FK_AlertaParametro_MailAssinatura]

                    ALTER TABLE [dbo].[AlertaParametroMenssage]
                    CHECK CONSTRAINT [dbo.AlertaParametroMenssage_IdAlertaParametro]
 

             INSERT [dbo].[ConfigDB] ([ParametersJson], [Ativo], [LastUploadPostLog], [LastGetSchema], [LastGetConfig], [ParametersXML]) VALUES (N'{
   ""@CompanyName"": ""facta"",
   ""@InstanceName"": """",
   ""@KEY"": ""26fc55b8-7dec-11ea-bc55-0242ac130003"",
   ""@ServerName"": """",
   ""NotifyOperators"": {
      ""Email"": ""paulo.kuhn@facta.com.br"",
      ""Mobile"": []
   },
   ""BackupDifferential"": {
      ""BackupPath"": ""C:\\Data\\MSSQLSERVER\\BACKUP\\"",
      ""DeleteOlderThan"": ""1380"",
      ""ExcludeDB"": ""tempdb;ReportServerTempDB;master"",
      ""Messages"": {
         ""@EmailOnFail"": ""1"",
         ""@EmailOnSuccess"": ""0"",
         ""@IMOnFail"": ""0"",
         ""@IMOnSuccess"": ""0"",
         ""@SMSOnFail"": ""0"",
         ""@SMSOnSuccess"": ""0"",
         ""@SendOnStart"": ""0""
      },
      ""Schedule"": {
         ""@Enabled"": ""0"",
         ""@StepSQLCommand"": ""EXECUTE [dbo].[stpStartBackupDB] @BackupType = ''BackupDifferential''; EXECUTE [dbo].[stpStartDeleteOldBackups] @BackupType = ''BackupDifferential''"",
         ""@active_end_date"": ""99991231"",
         ""@active_end_time"": ""235959"",
         ""@active_start_date"": ""20000101"",
         ""@active_start_time"": ""120000"",
         ""@freq_interval"": ""1"",
         ""@freq_recurrence_factor"": ""0"",
         ""@freq_relative_interval"": ""0"",
         ""@freq_subday_interval"": ""7"",
         ""@freq_subday_type"": ""1"",
         ""@freq_type"": ""4""
      },
      ""WITH"": ""WITH DIFFERENTIAL, COMPRESSION, NOINIT""
   },
   ""BackupFull"": {
      ""BackupPath"": ""C:\\Data\\MSSQLSERVER\\BACKUP\\"",
      ""DeleteOlderThan"": ""1380"",
      ""ExcludeDB"": ""tempdb"",
      ""Messages"": {
         ""@EmailOnFail"": ""1"",
         ""@EmailOnSuccess"": ""1"",
         ""@IMOnFail"": ""0"",
         ""@IMOnSuccess"": ""0"",
         ""@SMSOnFail"": ""0"",
         ""@SMSOnSuccess"": ""0"",
         ""@SendOnStart"": ""0""
      },
      ""Schedule"": {
         ""@Enabled"": ""0"",
         ""@StepSQLCommand"": ""EXECUTE [dbo].[stpStartBackupDB] @BackupType = ''BackupFull''; EXECUTE [dbo].[stpStartDeleteOldBackups] @BackupType = ''BackupFull''"",
         ""@active_end_date"": ""99991231"",
         ""@active_end_time"": ""235959"",
         ""@active_start_date"": ""20000101"",
         ""@active_start_time"": ""010000"",
         ""@freq_interval"": ""1"",
         ""@freq_recurrence_factor"": ""0"",
         ""@freq_relative_interval"": ""0"",
         ""@freq_subday_interval"": ""7"",
         ""@freq_subday_type"": ""1"",
         ""@freq_type"": ""4""
      },
      ""WITH"": ""WITH COMPRESSION, NOINIT""
   },
   ""BackupLog"": {
      ""BackupPath"": ""C:\\Data\\MSSQLSERVER\\BACKUP\\"",
      ""DeleteOlderThan"": ""1380"",
      ""ExcludeDB"": ""tempdb;ReportServerTempDB"",
      ""Messages"": {
         ""@EmailOnFail"": ""1"",
         ""@EmailOnSuccess"": ""0"",
         ""@IMOnFail"": ""0"",
         ""@IMOnSuccess"": ""0"",
         ""@SMSOnFail"": ""0"",
         ""@SMSOnSuccess"": ""0"",
         ""@SendOnStart"": ""0""
      },
      ""Schedule"": {
         ""@Enabled"": ""0"",
         ""@StepSQLCommand"": ""EXECUTE [dbo].[stpStartBackupDB] @BackupType = ''BackupLog''; EXECUTE [dbo].[stpStartDeleteOldBackups] @BackupType = ''BackupLog''"",
         ""@active_end_date"": ""99991231"",
         ""@active_end_time"": ""235959"",
         ""@active_start_date"": ""20000101"",
         ""@active_start_time"": ""0"",
         ""@freq_interval"": ""1"",
         ""@freq_recurrence_factor"": ""0"",
         ""@freq_relative_interval"": ""0"",
         ""@freq_subday_interval"": ""30"",
         ""@freq_subday_type"": ""4"",
         ""@freq_type"": ""4""
      },
      ""WITH"": ""WITH COMPRESSION, NOINIT""
   },
   ""CheckDB"": {
      ""ExcludeDB"": ""tempdb;ReportServerTempDB"",
      ""Messages"": {
         ""@EmailOnFail"": ""1"",
         ""@EmailOnSuccess"": ""0"",
         ""@IMOnFail"": ""0"",
         ""@IMOnSuccess"": ""0"",
         ""@SMSOnFail"": ""0"",
         ""@SMSOnSuccess"": ""0"",
         ""@SendOnStart"": ""0""
      },
      ""Schedule"": {
         ""@Enabled"": ""1"",
         ""@StepSQLCommand"": ""EXECUTE [dbo].[stpStartCheckDB]"",
         ""@active_end_date"": ""99991231"",
         ""@active_end_time"": ""235959"",
         ""@active_start_date"": ""20000101"",
         ""@active_start_time"": ""40000"",
         ""@freq_interval"": ""1"",
         ""@freq_recurrence_factor"": ""1"",
         ""@freq_relative_interval"": ""0"",
         ""@freq_subday_interval"": ""0"",
         ""@freq_subday_type"": ""1"",
         ""@freq_type"": ""8""
      }
   },
   ""Collect"": {
      ""DB_Alterts"": {
         ""@Day"": ""1"",
         ""@Periodically"": ""Daily""
      },
      ""DB_CheckList"": {
         ""@Day"": ""1"",
         ""@Periodically"": ""Daily""
      },
      ""DB_Without_AlwaysOn"": {
         ""@Day"": ""1"",
         ""@Periodically"": ""Daily""
      },
      ""HeavyQueryBy_execution_count"": {
         ""@Day"": ""15"",
         ""@Periodically"": ""Montly""
      },
      ""HeavyQueryBy_total_elapsed_time"": {
         ""@Day"": ""15"",
         ""@Periodically"": ""Montly""
      },
      ""HeavyQueryBy_total_logical_reads"": {
         ""@Day"": ""15"",
         ""@Periodically"": ""Montly""
      },
      ""HeavyQueryBy_total_logical_writes"": {
         ""@Day"": ""15"",
         ""@Periodically"": ""Montly""
      },
      ""HeavyQueryBy_total_worker_time"": {
         ""@Day"": ""15"",
         ""@Periodically"": ""Montly""
      },
      ""IndexSearches"": {
         ""@Day"": ""6"",
         ""@Periodically"": ""Weekly""
      },
      ""IndexUpdates"": {
         ""@Day"": ""6"",
         ""@Periodically"": ""Weekly""
      },
      ""Messages"": {
         ""@EmailOnFail"": ""1"",
         ""@EmailOnSuccess"": ""1"",
         ""@IMOnFail"": ""0"",
         ""@IMOnSuccess"": ""0"",
         ""@SMSOnFail"": ""0"",
         ""@SMSOnSuccess"": ""0"",
         ""@SendOnStart"": ""0""
      },
      ""MissingIndexes"": {
         ""@Day"": ""6"",
         ""@Periodically"": ""Weekly""
      },
      ""Schedule"": {
         ""@Enabled"": ""1"",
         ""@StepSQLCommand"": ""EXECUTE [dbo].[stpStartCollectServerInfo] ''ALL'', 0"",
         ""@active_end_date"": ""99991231"",
         ""@active_end_time"": ""235959"",
         ""@active_start_date"": ""20000101"",
         ""@active_start_time"": ""22000"",
         ""@freq_interval"": ""1"",
         ""@freq_recurrence_factor"": ""0"",
         ""@freq_relative_interval"": ""0"",
         ""@freq_subday_interval"": ""7"",
         ""@freq_subday_type"": ""1"",
         ""@freq_type"": ""4""
      },
      ""ServerProperties"": {
         ""@Day"": ""1"",
         ""@Periodically"": ""Daily""
      },
      ""Waits"": {
         ""@Day"": ""6"",
         ""@Periodically"": ""Weekly""
      },
      ""configurations"": {
         ""@Day"": ""1"",
         ""@Periodically"": ""Daily""
      },
      ""databases"": {
         ""@Day"": ""1"",
         ""@Periodically"": ""Daily""
      },
      ""master_files"": {
         ""@Day"": ""1"",
         ""@Periodically"": ""Daily""
      }
   },
   ""IndexDefrag"": {
      ""Database"": [],
      ""DebugMode"": ""1"",
      ""DefragDelay"": [],
      ""DefragOrderColumn"": ""fragmentation"",
      ""DefragSortOrder"": ""DESC"",
      ""ExcludeMaxPartition"": ""0"",
      ""ExecuteSQL"": ""1"",
      ""ForceRescan"": ""0"",
      ""MaxDopRestriction"": ""1"",
      ""MaxPageCount"": [],
      ""Messages"": {
         ""@EmailOnFail"": ""1"",
         ""@EmailOnSuccess"": ""0"",
         ""@IMOnFail"": ""0"",
         ""@IMOnSuccess"": ""0"",
         ""@SMSOnFail"": ""0"",
         ""@SMSOnSuccess"": ""0"",
         ""@SendOnStart"": ""0""
      },
      ""MinFragmentation"": ""10"",
      ""MinPageCount"": ""24"",
      ""OnlineRebuild"": ""1"",
      ""PrintCommands"": ""0"",
      ""PrintFragmentation"": ""1"",
      ""RebuildThreshold"": ""30"",
      ""ScanMode"": ""LIMITED"",
      ""Schedule"": {
         ""@Enabled"": ""1"",
         ""@StepSQLCommand"": ""EXECUTE [dbo].[stpStartDefraging]; EXECUTE [dbo].[stpStartUpdateStats]"",
         ""@active_end_date"": ""99991231"",
         ""@active_end_time"": ""235959"",
         ""@active_start_date"": ""20000101"",
         ""@active_start_time"": ""20000"",
         ""@freq_interval"": ""1"",
         ""@freq_recurrence_factor"": ""0"",
         ""@freq_relative_interval"": ""0"",
         ""@freq_subday_interval"": ""7"",
         ""@freq_subday_type"": ""1"",
         ""@freq_type"": ""4""
      },
      ""SortInTempDB"": ""1"",
      ""TableName"": [],
      ""TimeLimit"": ""180""
   },
   ""PostLog"": {
      ""PostLogLocal"": ""1"",
      ""PostLogSend"": ""1""
   },
   ""ShrinkingFiles"": {
      ""Messages"": {
         ""@EmailOnFail"": ""1"",
         ""@EmailOnSuccess"": ""1"",
         ""@IMOnFail"": ""0"",
         ""@IMOnSuccess"": ""0"",
         ""@SMSOnFail"": ""0"",
         ""@SMSOnSuccess"": ""0"",
         ""@SendOnStart"": ""0""
      },
      ""Schedule"": {
         ""@Enabled"": ""1"",
         ""@StepSQLCommand"": ""EXECUTE [dbo].[stpStartShrinkingLogFiles] 1024"",
         ""@active_end_date"": ""99991231"",
         ""@active_end_time"": ""235959"",
         ""@active_start_date"": ""20000101"",
         ""@active_start_time"": ""40000"",
         ""@freq_interval"": ""1"",
         ""@freq_recurrence_factor"": ""1"",
         ""@freq_relative_interval"": ""0"",
         ""@freq_subday_interval"": ""0"",
         ""@freq_subday_type"": ""1"",
         ""@freq_type"": ""8""
      }
   },
   ""UpdateStatistics"": {
      ""ExcludeDB"": ""tempdb;ReportServerTempDB"",
      ""Messages"": {
         ""@EmailOnFail"": ""1"",
         ""@EmailOnSuccess"": ""0"",
         ""@IMOnFail"": ""0"",
         ""@IMOnSuccess"": ""0"",
         ""@SMSOnFail"": ""0"",
         ""@SMSOnSuccess"": ""0"",
         ""@SendOnStart"": ""0""
      },
      ""Schedule"": {
         ""@Enabled"": ""1"",
         ""@StepSQLCommand"": ""EXECUTE [dbo].[stpStartUpdateStats]"",
         ""@active_end_date"": ""99991231"",
         ""@active_end_time"": ""235959"",
         ""@active_start_date"": ""20000101"",
         ""@active_start_time"": ""40000"",
         ""@freq_interval"": ""1"",
         ""@freq_recurrence_factor"": ""0"",
         ""@freq_relative_interval"": ""0"",
         ""@freq_subday_interval"": ""7"",
         ""@freq_subday_type"": ""1"",
         ""@freq_type"": ""4""
      }
   }
},
            }', 1, NULL, NULL, NULL, 
			N'<Customer CompanyName=""facta"" InstanceName="""" KEY=""26fc55b8-7dec-11ea-bc55-0242ac130003"" ServerName="""">
              <NotifyOperators>
                <Email>paulo.kuhn@facta.com.br</Email>
                <Mobile />
              </NotifyOperators>
              <BackupDifferential>
                <BackupPath>C:\Data\MSSQLSERVER\BACKUP\</BackupPath>
                <DeleteOlderThan>1380</DeleteOlderThan>
                <ExcludeDB>tempdb;ReportServerTempDB;master</ExcludeDB>
                <Messages EmailOnFail=""1"" EmailOnSuccess=""0"" IMOnFail=""0"" IMOnSuccess=""0"" SMSOnFail=""0"" SMSOnSuccess=""0"" SendOnStart=""0"" />
                <Schedule Enabled=""0"" StepSQLCommand=""EXECUTE [dbo].[stpStartBackup] @BackupType = ''BackupDifferential''; EXECUTE [dbo].[stpStartDeleteOldBackups] @BackupType = ''BackupDifferential''"" active_end_date=""99991231"" active_end_time=""235959"" active_start_date=""20000101"" active_start_time=""120000"" freq_interval=""1"" freq_recurrence_factor=""0"" freq_relative_interval=""0"" freq_subday_interval=""7"" freq_subday_type=""1"" freq_type=""4"" />
                <WITH>WITH DIFFERENTIAL, COMPRESSION, NOINIT</WITH>
              </BackupDifferential>
              <BackupFull>
                <BackupPath>C:\Data\MSSQLSERVER\BACKUP\</BackupPath>
                <DeleteOlderThan>1380</DeleteOlderThan>
                <ExcludeDB>tempdb</ExcludeDB>
                <Messages EmailOnFail=""1"" EmailOnSuccess=""1"" IMOnFail=""0"" IMOnSuccess=""0"" SMSOnFail=""0"" SMSOnSuccess=""0"" SendOnStart=""0"" />
                <Schedule Enabled=""0"" StepSQLCommand=""EXECUTE [dbo].[stpStartBackup] @BackupType = ''BackupFull''; EXECUTE [dbo].[stpStartDeleteOldBackups] @BackupType = ''BackupFull''"" active_end_date=""99991231"" active_end_time=""235959"" active_start_date=""20000101"" active_start_time=""010000"" freq_interval=""1"" freq_recurrence_factor=""0"" freq_relative_interval=""0"" freq_subday_interval=""7"" freq_subday_type=""1"" freq_type=""4"" />
                <WITH>WITH COMPRESSION, NOINIT</WITH>
              </BackupFull>
              <BackupLog>
                <BackupPath>C:\Data\MSSQLSERVER\BACKUP\</BackupPath>
                <DeleteOlderThan>1380</DeleteOlderThan>
                <ExcludeDB>tempdb;ReportServerTempDB</ExcludeDB>
                <Messages EmailOnFail=""1"" EmailOnSuccess=""0"" IMOnFail=""0"" IMOnSuccess=""0"" SMSOnFail=""0"" SMSOnSuccess=""0"" SendOnStart=""0"" />
                <Schedule Enabled=""0"" StepSQLCommand=""EXECUTE [dbo].[stpStartBackup] @BackupType = ''BackupLog''; EXECUTE [dbo].[stpStartDeleteOldBackups] @BackupType = ''BackupLog''"" active_end_date=""99991231"" active_end_time=""235959"" active_start_date=""20000101"" active_start_time=""0"" freq_interval=""1"" freq_recurrence_factor=""0"" freq_relative_interval=""0"" freq_subday_interval=""30"" freq_subday_type=""4"" freq_type=""4"" />
                <WITH>WITH COMPRESSION, NOINIT</WITH>
              </BackupLog>
              <CheckDB>
                <ExcludeDB>tempdb;ReportServerTempDB</ExcludeDB>
                <Messages EmailOnFail=""1"" EmailOnSuccess=""0"" IMOnFail=""0"" IMOnSuccess=""0"" SMSOnFail=""0"" SMSOnSuccess=""0"" SendOnStart=""0"" />
                <Schedule Enabled=""1"" StepSQLCommand=""EXECUTE [dbo].[stpStartCheckDB]"" active_end_date=""99991231"" active_end_time=""235959"" active_start_date=""20000101"" active_start_time=""40000"" freq_interval=""1"" freq_recurrence_factor=""1"" freq_relative_interval=""0"" freq_subday_interval=""0"" freq_subday_type=""1"" freq_type=""8"" />
              </CheckDB>
              <Collect>
                <DB_Alterts Day=""1"" Periodically=""Daily"" />
                <DB_CheckList Day=""1"" Periodically=""Daily"" />
                <DB_Without_AlwaysOn Day=""1"" Periodically=""Daily"" />
                <HeavyQueryBy_execution_count Day=""15"" Periodically=""Montly"" />
                <HeavyQueryBy_total_elapsed_time Day=""15"" Periodically=""Montly"" />
                <HeavyQueryBy_total_logical_reads Day=""15"" Periodically=""Montly"" />
                <HeavyQueryBy_total_logical_writes Day=""15"" Periodically=""Montly"" />
                <HeavyQueryBy_total_worker_time Day=""15"" Periodically=""Montly"" />
                <IndexSearches Day=""6"" Periodically=""Weekly"" />
                <IndexUpdates Day=""6"" Periodically=""Weekly"" />
                <Messages EmailOnFail=""1"" EmailOnSuccess=""1"" IMOnFail=""0"" IMOnSuccess=""0"" SMSOnFail=""0"" SMSOnSuccess=""0"" SendOnStart=""0"" />
                <MissingIndexes Day=""6"" Periodically=""Weekly"" />
                <Schedule Enabled=""1"" StepSQLCommand=""EXECUTE [dbo].[stpStartCollectServerInfo] ''ALL'', 0"" active_end_date=""99991231"" active_end_time=""235959"" active_start_date=""20000101"" active_start_time=""22000"" freq_interval=""1"" freq_recurrence_factor=""0"" freq_relative_interval=""0"" freq_subday_interval=""7"" freq_subday_type=""1"" freq_type=""4"" />
                <ServerProperties Day=""1"" Periodically=""Daily"" />
                <Waits Day=""6"" Periodically=""Weekly"" />
                <configurations Day=""1"" Periodically=""Daily"" />
                <databases Day=""1"" Periodically=""Daily"" />
                <master_files Day=""1"" Periodically=""Daily"" />
              </Collect>
              <IndexDefrag>
                <Database />
                <DebugMode>1</DebugMode>
                <DefragDelay />
                <DefragOrderColumn>fragmentation</DefragOrderColumn>
                <DefragSortOrder>DESC</DefragSortOrder>
                <ExcludeMaxPartition>0</ExcludeMaxPartition>
                <ExecuteSQL>1</ExecuteSQL>
                <ForceRescan>0</ForceRescan>
                <MaxDopRestriction>1</MaxDopRestriction>
                <MaxPageCount />
                <Messages EmailOnFail=""1"" EmailOnSuccess=""0"" IMOnFail=""0"" IMOnSuccess=""0"" SMSOnFail=""0"" SMSOnSuccess=""0"" SendOnStart=""0"" />
                <MinFragmentation>10</MinFragmentation>
                <MinPageCount>24</MinPageCount>
                <OnlineRebuild>1</OnlineRebuild>
                <PrintCommands>0</PrintCommands>
                <PrintFragmentation>1</PrintFragmentation>
                <RebuildThreshold>30</RebuildThreshold>
                <ScanMode>LIMITED</ScanMode>
                <Schedule Enabled=""1"" StepSQLCommand=""EXECUTE [dbo].[stpStartDefraging]; EXECUTE [dbo].[stpStartUpdateStats]"" active_end_date=""99991231"" active_end_time=""235959"" active_start_date=""20000101"" active_start_time=""20000"" freq_interval=""1"" freq_recurrence_factor=""0"" freq_relative_interval=""0"" freq_subday_interval=""7"" freq_subday_type=""1"" freq_type=""4"" />
                <SortInTempDB>1</SortInTempDB>
                <TableName />
                <TimeLimit>180</TimeLimit>
              </IndexDefrag>
              <PostLog>
                <PostLogLocal>1</PostLogLocal>
                <PostLogSend>1</PostLogSend>
              </PostLog>
              <ShrinkingFiles>
                <Messages EmailOnFail=""1"" EmailOnSuccess=""1"" IMOnFail=""0"" IMOnSuccess=""0"" SMSOnFail=""0"" SMSOnSuccess=""0"" SendOnStart=""0"" />
                <Schedule Enabled=""1"" StepSQLCommand=""EXECUTE [dbo].[stpStartShrinkingLogFiles] 500"" active_end_date=""99991231"" active_end_time=""235959"" active_start_date=""20000101"" active_start_time=""40000"" freq_interval=""1"" freq_recurrence_factor=""1"" freq_relative_interval=""0"" freq_subday_interval=""0"" freq_subday_type=""1"" freq_type=""8"" />
              </ShrinkingFiles>
              <UpdateStatistics>
                <ExcludeDB>tempdb;ReportServerTempDB</ExcludeDB>
                <Messages EmailOnFail=""1"" EmailOnSuccess=""0"" IMOnFail=""0"" IMOnSuccess=""0"" SMSOnFail=""0"" SMSOnSuccess=""0"" SendOnStart=""0"" />
                <Schedule Enabled=""1"" StepSQLCommand=""EXECUTE [dbo].[stpStartUpdateStats]"" active_end_date=""99991231"" active_end_time=""235959"" active_start_date=""20000101"" active_start_time=""40000"" freq_interval=""1"" freq_recurrence_factor=""0"" freq_relative_interval=""0"" freq_subday_interval=""7"" freq_subday_type=""1"" freq_type=""4"" />
              </UpdateStatistics>
            </Customer>')

            ";

        }
    }
}
