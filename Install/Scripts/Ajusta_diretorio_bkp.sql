/****** Script do comando SelectTopNRows de SSMS  ******/
--SELECT TOP (1000) [ParametersJson]
--      ,[ParametersXML]
--      ,[Ativo]
--      ,[LastUploadPostLog]
--      ,[LastGetSchema]
--      ,[LastGetConfig]
--  FROM [SafeBase].[dbo].[ConfigDB]

  DECLARE @DIRBKP VARCHAR(100) = 'D:\\Backup\', @TIMEDELETEOLD VARCHAR(30) = '8640';
  

  UPDATE [SafeBase].[dbo].[ConfigDB] SET [ParametersJson] = '{
   "@CompanyName": "SafeWeb",
   "@InstanceName": "",
   "@KEY": "26fc55b8-7dec-11ea-bc55-0242ac130003",
   "@ServerName": "",
   "NotifyOperators": {
      "Email": "paulo.kuhn@safeweb.com.br",
      "Mobile": []
   },
   "BackupDifferential": {
      "BackupPath": "'+@DIRBKP+'",
      "DeleteOlderThan": "'+@TIMEDELETEOLD+'",
      "ExcludeDB": "tempdb;ReportServerTempDB;master",
      "Messages": {
         "@EmailOnFail": "1",
         "@EmailOnSuccess": "0",
         "@IMOnFail": "0",
         "@IMOnSuccess": "0",
         "@SMSOnFail": "0",
         "@SMSOnSuccess": "0",
         "@SendOnStart": "0"
      },
      "Schedule": {
         "@Enabled": "0",
         "@StepSQLCommand": "EXECUTE [dbo].[stpStartBackup] @BackupType = ''BackupDifferential''; EXECUTE [dbo].[stpStartDeleteOldBackups] @BackupType = ''BackupDifferential''",
         "@active_end_date": "99991231",
         "@active_end_time": "235959",
         "@active_start_date": "20000101",
         "@active_start_time": "120000",
         "@freq_interval": "1",
         "@freq_recurrence_factor": "0",
         "@freq_relative_interval": "0",
         "@freq_subday_interval": "7",
         "@freq_subday_type": "1",
         "@freq_type": "4"
      },
      "WITH": "WITH DIFFERENTIAL, COMPRESSION, NOINIT"
   },
   "BackupFull": {
      "BackupPath": "'+@DIRBKP+'",
      "DeleteOlderThan": "'+@TIMEDELETEOLD+'",
      "ExcludeDB": "tempdb;QUALITOR_HML",
      "Messages": {
         "@EmailOnFail": "1",
         "@EmailOnSuccess": "1",
         "@IMOnFail": "0",
         "@IMOnSuccess": "0",
         "@SMSOnFail": "0",
         "@SMSOnSuccess": "0",
         "@SendOnStart": "0"
      },
      "Schedule": {
         "@Enabled": "0",
         "@StepSQLCommand": "EXECUTE [dbo].[stpStartBackup] @BackupType = ''BackupFull''; EXECUTE [dbo].[stpStartDeleteOldBackups] @BackupType = ''BackupFull''",
         "@active_end_date": "99991231",
         "@active_end_time": "235959",
         "@active_start_date": "20000101",
         "@active_start_time": "010000",
         "@freq_interval": "1",
         "@freq_recurrence_factor": "0",
         "@freq_relative_interval": "0",
         "@freq_subday_interval": "7",
         "@freq_subday_type": "1",
         "@freq_type": "4"
      },
      "WITH": "WITH COMPRESSION, NOINIT"
   },
   "BackupLog": {
      "BackupPath": "'+@DIRBKP+'",
      "DeleteOlderThan": "'+@TIMEDELETEOLD+'",
      "ExcludeDB": "tempdb;ReportServerTempDB",
      "Messages": {
         "@EmailOnFail": "1",
         "@EmailOnSuccess": "0",
         "@IMOnFail": "0",
         "@IMOnSuccess": "0",
         "@SMSOnFail": "0",
         "@SMSOnSuccess": "0",
         "@SendOnStart": "0"
      },
      "Schedule": {
         "@Enabled": "0",
         "@StepSQLCommand": "EXECUTE [dbo].[stpStartBackup] @BackupType = ''BackupLog''; EXECUTE [dbo].[stpStartDeleteOldBackups] @BackupType = ''BackupLog''",
         "@active_end_date": "99991231",
         "@active_end_time": "235959",
         "@active_start_date": "20000101",
         "@active_start_time": "0",
         "@freq_interval": "1",
         "@freq_recurrence_factor": "0",
         "@freq_relative_interval": "0",
         "@freq_subday_interval": "30",
         "@freq_subday_type": "4",
         "@freq_type": "4"
      },
      "WITH": "WITH COMPRESSION, NOINIT"
   },
   "CheckDB": {
      "ExcludeDB": "tempdb;ReportServerTempDB",
      "Messages": {
         "@EmailOnFail": "1",
         "@EmailOnSuccess": "0",
         "@IMOnFail": "0",
         "@IMOnSuccess": "0",
         "@SMSOnFail": "0",
         "@SMSOnSuccess": "0",
         "@SendOnStart": "0"
      },
      "Schedule": {
         "@Enabled": "1",
         "@StepSQLCommand": "EXECUTE [dbo].[stpStartCheckDB]",
         "@active_end_date": "99991231",
         "@active_end_time": "235959",
         "@active_start_date": "20000101",
         "@active_start_time": "40000",
         "@freq_interval": "1",
         "@freq_recurrence_factor": "1",
         "@freq_relative_interval": "0",
         "@freq_subday_interval": "0",
         "@freq_subday_type": "1",
         "@freq_type": "8"
      }
   },
   "Collect": {
      "DB_Alterts": {
         "@Day": "1",
         "@Periodically": "Daily"
      },
      "DB_CheckList": {
         "@Day": "1",
         "@Periodically": "Daily"
      },
      "DB_Without_AlwaysOn": {
         "@Day": "1",
         "@Periodically": "Daily"
      },
      "HeavyQueryBy_execution_count": {
         "@Day": "15",
         "@Periodically": "Montly"
      },
      "HeavyQueryBy_total_elapsed_time": {
         "@Day": "15",
         "@Periodically": "Montly"
      },
      "HeavyQueryBy_total_logical_reads": {
         "@Day": "15",
         "@Periodically": "Montly"
      },
      "HeavyQueryBy_total_logical_writes": {
         "@Day": "15",
         "@Periodically": "Montly"
      },
      "HeavyQueryBy_total_worker_time": {
         "@Day": "15",
         "@Periodically": "Montly"
      },
      "IndexSearches": {
         "@Day": "6",
         "@Periodically": "Weekly"
      },
      "IndexUpdates": {
         "@Day": "6",
         "@Periodically": "Weekly"
      },
      "Messages": {
         "@EmailOnFail": "1",
         "@EmailOnSuccess": "1",
         "@IMOnFail": "0",
         "@IMOnSuccess": "0",
         "@SMSOnFail": "0",
         "@SMSOnSuccess": "0",
         "@SendOnStart": "0"
      },
      "MissingIndexes": {
         "@Day": "6",
         "@Periodically": "Weekly"
      },
      "Schedule": {
         "@Enabled": "1",
         "@StepSQLCommand": "EXECUTE [dbo].[stpStartCollectServerInfo] ''ALL'', 0",
         "@active_end_date": "99991231",
         "@active_end_time": "235959",
         "@active_start_date": "20000101",
         "@active_start_time": "22000",
         "@freq_interval": "1",
         "@freq_recurrence_factor": "0",
         "@freq_relative_interval": "0",
         "@freq_subday_interval": "7",
         "@freq_subday_type": "1",
         "@freq_type": "4"
      },
      "ServerProperties": {
         "@Day": "1",
         "@Periodically": "Daily"
      },
      "Waits": {
         "@Day": "6",
         "@Periodically": "Weekly"
      },
      "configurations": {
         "@Day": "1",
         "@Periodically": "Daily"
      },
      "databases": {
         "@Day": "1",
         "@Periodically": "Daily"
      },
      "master_files": {
         "@Day": "1",
         "@Periodically": "Daily"
      }
   },
   "IndexDefrag": {
      "Database": [],
      "DebugMode": "1",
      "DefragDelay": [],
      "DefragOrderColumn": "fragmentation",
      "DefragSortOrder": "DESC",
      "ExcludeMaxPartition": "0",
      "ExecuteSQL": "1",
      "ForceRescan": "0",
      "MaxDopRestriction": "1",
      "MaxPageCount": [],
      "Messages": {
         "@EmailOnFail": "1",
         "@EmailOnSuccess": "0",
         "@IMOnFail": "0",
         "@IMOnSuccess": "0",
         "@SMSOnFail": "0",
         "@SMSOnSuccess": "0",
         "@SendOnStart": "0"
      },
      "MinFragmentation": "10",
      "MinPageCount": "24",
      "OnlineRebuild": "1",
      "PrintCommands": "0",
      "PrintFragmentation": "1",
      "RebuildThreshold": "30",
      "ScanMode": "LIMITED",
      "Schedule": {
         "@Enabled": "1",
         "@StepSQLCommand": "EXECUTE [dbo].[stpStartDefraging]; EXECUTE [dbo].[stpStartUpdateStats]",
         "@active_end_date": "99991231",
         "@active_end_time": "235959",
         "@active_start_date": "20000101",
         "@active_start_time": "20000",
         "@freq_interval": "1",
         "@freq_recurrence_factor": "0",
         "@freq_relative_interval": "0",
         "@freq_subday_interval": "7",
         "@freq_subday_type": "1",
         "@freq_type": "4"
      },
      "SortInTempDB": "1",
      "TableName": [],
      "TimeLimit": "180"
   },
   "PostLog": {
      "PostLogLocal": "1",
      "PostLogSend": "1"
   },
   "ShrinkingFiles": {
      "Messages": {
         "@EmailOnFail": "1",
         "@EmailOnSuccess": "1",
         "@IMOnFail": "0",
         "@IMOnSuccess": "0",
         "@SMSOnFail": "0",
         "@SMSOnSuccess": "0",
         "@SendOnStart": "0"
      },
      "Schedule": {
         "@Enabled": "1",
         "@StepSQLCommand": "EXECUTE [dbo].[stpStartShrinkingLogFiles] 500",
         "@active_end_date": "99991231",
         "@active_end_time": "235959",
         "@active_start_date": "20000101",
         "@active_start_time": "40000",
         "@freq_interval": "1",
         "@freq_recurrence_factor": "1",
         "@freq_relative_interval": "0",
         "@freq_subday_interval": "0",
         "@freq_subday_type": "1",
         "@freq_type": "8"
      }
   },
   "UpdateStatistics": {
      "ExcludeDB": "tempdb;ReportServerTempDB",
      "Messages": {
         "@EmailOnFail": "1",
         "@EmailOnSuccess": "0",
         "@IMOnFail": "0",
         "@IMOnSuccess": "0",
         "@SMSOnFail": "0",
         "@SMSOnSuccess": "0",
         "@SendOnStart": "0"
      },
      "Schedule": {
         "@Enabled": "1",
         "@StepSQLCommand": "EXECUTE [dbo].[stpStartUpdateStats]",
         "@active_end_date": "99991231",
         "@active_end_time": "235959",
         "@active_start_date": "20000101",
         "@active_start_time": "40000",
         "@freq_interval": "1",
         "@freq_recurrence_factor": "0",
         "@freq_relative_interval": "0",
         "@freq_subday_interval": "7",
         "@freq_subday_type": "1",
         "@freq_type": "4"
      }
   }
},
            }', [ParametersXML] = '<Customer CompanyName="SafeWeb" InstanceName="" KEY="26fc55b8-7dec-11ea-bc55-0242ac130003" ServerName="">
  <NotifyOperators>
    <Email>paulo.kuhn@safeweb.com.br</Email>
    <Mobile />
  </NotifyOperators>
  <BackupDifferential>
    <BackupPath>'+@DIRBKP+'</BackupPath>
    <DeleteOlderThan>'+@TIMEDELETEOLD+'</DeleteOlderThan>
    <ExcludeDB>tempdb;ReportServerTempDB;master</ExcludeDB>
    <Messages EmailOnFail="1" EmailOnSuccess="0" IMOnFail="0" IMOnSuccess="0" SMSOnFail="0" SMSOnSuccess="0" SendOnStart="0" />
    <Schedule Enabled="0" StepSQLCommand="EXECUTE [dbo].[stpStartBackup] @BackupType = ''BackupDifferential''; EXECUTE [dbo].[stpStartDeleteOldBackups] @BackupType = ''BackupDifferential''" active_end_date="99991231" active_end_time="235959" active_start_date="20000101" active_start_time="120000" freq_interval="1" freq_recurrence_factor="0" freq_relative_interval="0" freq_subday_interval="7" freq_subday_type="1" freq_type="4" />
    <WITH>WITH DIFFERENTIAL, COMPRESSION, NOINIT</WITH>
  </BackupDifferential>
  <BackupFull>
    <BackupPath>'+@DIRBKP+'</BackupPath>
    <DeleteOlderThan>'+@TIMEDELETEOLD+'</DeleteOlderThan>
    <ExcludeDB>tempdb</ExcludeDB>
    <Messages EmailOnFail="1" EmailOnSuccess="1" IMOnFail="0" IMOnSuccess="0" SMSOnFail="0" SMSOnSuccess="0" SendOnStart="0" />
    <Schedule Enabled="0" StepSQLCommand="EXECUTE [dbo].[stpStartBackup] @BackupType = ''BackupFull''; EXECUTE [dbo].[stpStartDeleteOldBackups] @BackupType = ''BackupFull''" active_end_date="99991231" active_end_time="235959" active_start_date="20000101" active_start_time="010000" freq_interval="1" freq_recurrence_factor="0" freq_relative_interval="0" freq_subday_interval="7" freq_subday_type="1" freq_type="4" />
    <WITH>WITH COMPRESSION, NOINIT</WITH>
  </BackupFull>
  <BackupLog>
    <BackupPath>'+@DIRBKP+'</BackupPath>
    <DeleteOlderThan>'+@TIMEDELETEOLD+'</DeleteOlderThan>
    <ExcludeDB>tempdb;ReportServerTempDB</ExcludeDB>
    <Messages EmailOnFail="1" EmailOnSuccess="0" IMOnFail="0" IMOnSuccess="0" SMSOnFail="0" SMSOnSuccess="0" SendOnStart="0" />
    <Schedule Enabled="0" StepSQLCommand="EXECUTE [dbo].[stpStartBackup] @BackupType = ''BackupLog''; EXECUTE [dbo].[stpStartDeleteOldBackups] @BackupType = ''BackupLog''" active_end_date="99991231" active_end_time="235959" active_start_date="20000101" active_start_time="0" freq_interval="1" freq_recurrence_factor="0" freq_relative_interval="0" freq_subday_interval="30" freq_subday_type="4" freq_type="4" />
    <WITH>WITH COMPRESSION, NOINIT</WITH>
  </BackupLog>
  <CheckDB>
    <ExcludeDB>tempdb;ReportServerTempDB</ExcludeDB>
    <Messages EmailOnFail="1" EmailOnSuccess="0" IMOnFail="0" IMOnSuccess="0" SMSOnFail="0" SMSOnSuccess="0" SendOnStart="0" />
    <Schedule Enabled="1" StepSQLCommand="EXECUTE [dbo].[stpStartCheckDB]" active_end_date="99991231" active_end_time="235959" active_start_date="20000101" active_start_time="40000" freq_interval="1" freq_recurrence_factor="1" freq_relative_interval="0" freq_subday_interval="0" freq_subday_type="1" freq_type="8" />
  </CheckDB>
  <Collect>
    <DB_Alterts Day="1" Periodically="Daily" />
    <DB_CheckList Day="1" Periodically="Daily" />
    <DB_Without_AlwaysOn Day="1" Periodically="Daily" />
    <HeavyQueryBy_execution_count Day="15" Periodically="Montly" />
    <HeavyQueryBy_total_elapsed_time Day="15" Periodically="Montly" />
    <HeavyQueryBy_total_logical_reads Day="15" Periodically="Montly" />
    <HeavyQueryBy_total_logical_writes Day="15" Periodically="Montly" />
    <HeavyQueryBy_total_worker_time Day="15" Periodically="Montly" />
    <IndexSearches Day="6" Periodically="Weekly" />
    <IndexUpdates Day="6" Periodically="Weekly" />
    <Messages EmailOnFail="1" EmailOnSuccess="1" IMOnFail="0" IMOnSuccess="0" SMSOnFail="0" SMSOnSuccess="0" SendOnStart="0" />
    <MissingIndexes Day="6" Periodically="Weekly" />
    <Schedule Enabled="1" StepSQLCommand="EXECUTE [dbo].[stpStartCollectServerInfo] ''ALL'', 0" active_end_date="99991231" active_end_time="235959" active_start_date="20000101" active_start_time="22000" freq_interval="1" freq_recurrence_factor="0" freq_relative_interval="0" freq_subday_interval="7" freq_subday_type="1" freq_type="4" />
    <ServerProperties Day="1" Periodically="Daily" />
    <Waits Day="6" Periodically="Weekly" />
    <configurations Day="1" Periodically="Daily" />
    <databases Day="1" Periodically="Daily" />
    <master_files Day="1" Periodically="Daily" />
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
    <Messages EmailOnFail="1" EmailOnSuccess="0" IMOnFail="0" IMOnSuccess="0" SMSOnFail="0" SMSOnSuccess="0" SendOnStart="0" />
    <MinFragmentation>10</MinFragmentation>
    <MinPageCount>24</MinPageCount>
    <OnlineRebuild>1</OnlineRebuild>
    <PrintCommands>0</PrintCommands>
    <PrintFragmentation>1</PrintFragmentation>
    <RebuildThreshold>30</RebuildThreshold>
    <ScanMode>LIMITED</ScanMode>
    <Schedule Enabled="1" StepSQLCommand="EXECUTE [dbo].[stpStartDefraging]; EXECUTE [dbo].[stpStartUpdateStats]" active_end_date="99991231" active_end_time="235959" active_start_date="20000101" active_start_time="20000" freq_interval="1" freq_recurrence_factor="0" freq_relative_interval="0" freq_subday_interval="7" freq_subday_type="1" freq_type="4" />
    <SortInTempDB>1</SortInTempDB>
    <TableName />
    <TimeLimit>180</TimeLimit>
  </IndexDefrag>
  <PostLog>
    <PostLogLocal>1</PostLogLocal>
    <PostLogSend>1</PostLogSend>
  </PostLog>
  <ShrinkingFiles>
    <Messages EmailOnFail="1" EmailOnSuccess="1" IMOnFail="0" IMOnSuccess="0" SMSOnFail="0" SMSOnSuccess="0" SendOnStart="0" />
    <Schedule Enabled="1" StepSQLCommand="EXECUTE [dbo].[stpStartShrinkingLogFiles] 500" active_end_date="99991231" active_end_time="235959" active_start_date="20000101" active_start_time="40000" freq_interval="1" freq_recurrence_factor="1" freq_relative_interval="0" freq_subday_interval="0" freq_subday_type="1" freq_type="8" />
  </ShrinkingFiles>
  <UpdateStatistics>
    <ExcludeDB>tempdb;ReportServerTempDB</ExcludeDB>
    <Messages EmailOnFail="1" EmailOnSuccess="0" IMOnFail="0" IMOnSuccess="0" SMSOnFail="0" SMSOnSuccess="0" SendOnStart="0" />
    <Schedule Enabled="1" StepSQLCommand="EXECUTE [dbo].[stpStartUpdateStats]" active_end_date="99991231" active_end_time="235959" active_start_date="20000101" active_start_time="40000" freq_interval="1" freq_recurrence_factor="0" freq_relative_interval="0" freq_subday_interval="7" freq_subday_type="1" freq_type="4" />
  </UpdateStatistics>
</Customer>'

update AlertaParametro set Ds_Email = 'paulo.kuhn@safeweb.com.br;airton@safeweb.com.br' where Id_AlertaParametro = 21

-- select * from AlertaParametro