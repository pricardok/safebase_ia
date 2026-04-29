# Data Collection Summary - SafeBase InitDB

Este documento descreve as rotinas C# que disparam os procedimentos armazenados usados pelos jobs do SQL Server Agent e as tabelas onde os dados são carregados.

## 1. Arquivos principais analisados

- `SafeBase_InitDB/Core/stpServerAlert.cs`
- `SafeBase_InitDB/Core/stpServerLoads.cs`
- `SafeBase_InitDB/Core/stpServerJob.cs`
- `SafeBase_InitDB/Core/Guide.cs`

Esses arquivos mostram o mapeamento de cada comando (`ServerAlert`, `ServerLoad`, `ServerTask`) para a procedure que executa a coleta.

## 2. Jobs e alertas executados pelo agendador

### Agendado todos os dias às 2h

Chamadas de alerta:
- `EXEC [dbo].[stpServerAlert] 'ALERT_DATABASE_CREATED'`
- `EXEC [dbo].[stpServerAlert] 'ALERT_NO_BACKUP'`
- `EXEC [dbo].[stpServerAlert] 'ALERT_JOB_FAIL'`
- `EXEC [dbo].[stpServerAlert] 'ALERT_DB_CHANGE'`
- `EXEC [dbo].[stpServerAlert] 'ALERT_CHECKDB_CHECK'`
- `EXEC [dbo].[stpServerAlert] 'ALERT_CHECKDB'`

### Step Real Time (a cada minuto)

- `EXEC [dbo].[stpServerAlert] 'ALERT_FILE_LOG_FULL'`
- `EXEC [dbo].[stpServerAlert] 'ALERT_ERRO_DATABASE'`

### Segunda a sábado às 7h

- `EXEC [dbo].[stpServerAlert] 'ALERT_CHECK_FILE_BKP'` (backup full = 1)
- `EXEC [dbo].[stpServerAlert] 'ALERT_CHECK_FILE_BKP',2` (backup diff = 2)

### Horário comercial (6h às 22h)

- `EXEC [dbo].[stpServerAlert] 'ALERT_PROCESS_BLOCKED'`
- `EXEC [dbo].[stpServerAlert] 'ALERT_CONSUMPTION_CPU'`

### A cada 5 minutos

- `EXEC [dbo].[stpServerAlert] 'ALERT_CREATE_TRACE'`
- `EXEC [dbo].[stpServerAlert] 'ALERT_DISC_SPACE'`
- `EXEC [dbo].[stpServerAlert] 'ALERT_TEMPDB_USE'`
- `EXEC [dbo].[stpServerAlert] 'ALERT_QUERY_DELAY'`
- `EXEC [dbo].[stpServerLoads] 'LOADS_LOG_ALWAYSON'`
- `EXEC [dbo].[stpGetCheckLoginCA]`

### A cada 1 hora

- `EXEC [dbo].[stpServerAlert] 'ALERT_MSSQL_CONNECTIONS'`

### A cada 20 minutos

- `EXEC [dbo].[stpServerAlert] 'ALERT_MSSQL_RESTART'`

## 3. Rotinas de carga de dados

Esses procedimentos populam as tabelas históricas usadas para análise e checklist.

### `stpServerLoads` cargas principais

- `LOADS_DB_CHANGE` -> tabela `ServerAudi`
- `LOADS_FRAGM_INDEX` -> tabelas `Servidor`, `BaseDados`, `Tabela`, `HistoricoFragmentacaoIndice`
- `LOADS_HIST_USAGE_ARCHIVE` -> tabela `HistoricoUtilizacaoArquivo`
- `LOADS_HIST_WS` -> tabela `HistoricoWaitsStats`
- `LOADS_TABLES_SIZE` -> tabelas `Servidor`, `BaseDados`, `Tabela`, `HistoricoTamanhoTabela`
- `LOADS_ACCOUNTANTS` -> tabela `ContadorRegistro`
- `LOADS_DB_ERROR_HISTORY` -> tabela `HistoricoErrosDB`
- `LOADS_LOG_ALWAYSON` -> tabela `HistoricoAlwaysOn`

### Observação
- `LOADS_FRAGM_INDEX` está comentado/desabilitado no `Guide.cs` mas existe suporte pela procedure `stpHistoricoFragmentacaoIndice`.
- `LOADS_DB_ERROR_HISTORY` e `LOADS_LOG_ALWAYSON` só são executados se a versão do SQL Server for >= 11.

## 4. Rotinas de checklist e relatórios de verificação

### `stpServerJob` checklist e verificações

- `CHECK_LIST` -> envia checklist diário
- `CHECK_LIST_JOBS_SLOW` -> `CheckJobDemorados`
- `CHECK_LIST_MSSQL_CONNECTIONS` -> `CheckConexaoAberta`
- `CHECK_LIST_FRAGMENTATION_INDEX` -> `CheckFragmentacaoIndices`
- `CHECK_LIST_ALERT` -> `CheckAlertaSemClear`, `CheckAlerta`
- `CHECK_LIST_JOBS_RUN` -> `CheckJobsRunning`
- `CHECK_LIST_JOBS_CHANGED` -> `CheckAlteracaoJobs`
- `CHECK_LIST_JOBS_FAILED` -> `CheckJobsFailed`
- `CHECK_LIST_QUERIES_RUNNING` -> `CheckQueriesRunning`
- `CHECK_LIST_BACKUP` -> `CheckBackupsExecutados`
- `CHECK_LIST_NO_BACKUP` -> `CheckDatabasesSemBackup`
- `CHECK_LIST_USE_FILE` -> `CheckUtilizacaoArquivoWrites`, `CheckUtilizacaoArquivoReads`
- `CHECK_LIST_GROWTH_TABLE` -> `CheckTableGrowth`, `CheckTableGrowthEmail`
- `CHECK_LIST_GROWTH_DATABASE` -> `CheckDatabaseGrowth`, `CheckDatabaseGrowthEmail`
- `CHECK_LIST_USED_FILE` -> `CheckArquivosDados`, `CheckArquivosLog`
- `CHECK_LIST_USED_DISC` -> `CheckEspacoDisco`
- `CHECK_LIST_ACCOUNTANTS` -> `ContadorRegistro`
- `CHECK_LIST_WAITS_STATS` -> `CheckWaitsStats`
- `CHECK_LIST_SQL_ERROR` -> `CheckSQLServerErrorLog`, `CheckSQLServerLoginFailed`, `CheckSQLServerLoginFailedEmail`
- `CHECK_LIST_SQL_TRACELOG` -> `CheckDBControllerQueries`, `CheckDBControllerQueriesGeral`
- `TEST` -> `Testedb`

## 5. Alertas monitorados

Todas as rotinas de alerta usam essencialmente as tabelas de configuração/registro de alertas:

- `Alerta`
- `AlertaParametro`

### Alertas acionados

- `ALERT_DB_CHANGE`
- `ALERT_MSSQL_CONNECTIONS`
- `ALERT_FILE_LOG_FULL`
- `ALERT_CHECKDB_CHECK`
- `ALERT_CHECKDB`
- `ALERT_CONSUMPTION_CPU`
- `ALERT_DATABASE_CREATED`
- `ALERT_ERRO_DATABASE`
- `ALERT_MSSQL_RESTART`
- `ALERT_PROCESS_BLOCKED`
- `ALERT_QUERY_DELAY`
- `ALERT_CREATE_TRACE`
- `ALERT_NO_BACKUP`
- `ALERT_JOB_FAIL`
- `ALERT_DISC_SPACE`
- `ALERT_TEMPDB_USE`
- `ALERT_RUN_PROCESSES`
- `ALERT_TEST_TRACE`
- `ALERT_CHECK_FILE_BKP`
- `ALERT_JOB_AGENDAMENTO_FAIL`
- `ALERT_FAILOVER`
- `QUEUE_DB`

## 6. Outras procedures relevantes

- `stpGetCheckLoginCA` -> alterações/criação de logins e usuários
- `stpGetCheckLogin` -> alerta de acesso (comentado no script)
- `stpQueueInfoSendMail` -> envio de informações de fila (configuração de alerta/queue)
- `stpGetInfo 'HELP'` -> informações gerais do SQL Server
- `stpToZabbix` -> integração Zabbix / descoberta de objetos

## 7. Como usar este documento

1. Verifique no banco quais tabelas existem e quais colunas são relevantes.
2. Analise os dados em cada tabela listada acima para entender o histórico de eventos.
3. Use as rotinas `CHECK_LIST_*` para ver os relatórios prontos e os dados das tabelas de verificação.
4. Para alertas, comece por `Alerta` e `AlertaParametro` e compare com o histórico gerado em `Check*`.

## 8. Observações finais

- O código C# não mostra o conteúdo exato das colunas, apenas o mapeamento de procedimentos para tabelas.
- Se precisar, posso ajudar a extrair a lista de colunas de cada tabela a partir do model do banco ou dos scripts SQL correspondentes.
