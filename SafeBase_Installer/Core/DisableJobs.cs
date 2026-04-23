using System;
using System.Collections.Generic;
using System.Text;
using SafeBase_Installer.Core;

namespace SafeBase_Installer
{
    class DisableJobs
    {
        public static string Query()
        {
            return
            @"

            USE msdb ;  
            GO  

            IF EXISTS (SELECT [name] FROM msdb.dbo.sysjobs where name like 'DBMaintenancePlan.AlertDB.No.RealTime')
            BEGIN
                EXEC msdb.dbo.sp_update_job @job_name = N'DBMaintenancePlan.AlertDB.No.RealTime', @enabled = 0 ; 
            END

            IF EXISTS (SELECT [name] FROM msdb.dbo.sysjobs where name like 'DBMaintenancePlan.AlertDB.RealTime')
            BEGIN
                EXEC msdb.dbo.sp_update_job @job_name = N'DBMaintenancePlan.AlertDB.RealTime', @enabled = 0 ;
            END

            IF EXISTS (SELECT [name] FROM msdb.dbo.sysjobs where name like 'DBMaintenancePlan.CheckListDB')
            BEGIN
                EXEC msdb.dbo.sp_update_job @job_name = N'DBMaintenancePlan.CheckListDB', @enabled = 0 ;
            END

            IF EXISTS (SELECT [name] FROM msdb.dbo.sysjobs where name like 'DBMaintenancePlan.Data.Load')
            BEGIN
                EXEC msdb.dbo.sp_update_job @job_name = N'DBMaintenancePlan.Data.Load', @enabled = 0 ;
            END

            IF EXISTS (SELECT [name] FROM msdb.dbo.sysjobs where name like 'DBMaintenancePlan.StartBackup.Diff')
            BEGIN
                EXEC msdb.dbo.sp_update_job @job_name = N'DBMaintenancePlan.StartBackup.Diff', @enabled = 0 ;
            END

            IF EXISTS (SELECT [name] FROM msdb.dbo.sysjobs where name like 'DBMaintenancePlan.StartBackup.Full')
            BEGIN
                EXEC msdb.dbo.sp_update_job @job_name = N'DBMaintenancePlan.StartBackup.Full', @enabled = 0 ;
            END

            IF EXISTS (SELECT [name] FROM msdb.dbo.sysjobs where name like 'DBMaintenancePlan.StartBackup.Log')
            BEGIN
                EXEC msdb.dbo.sp_update_job @job_name = N'DBMaintenancePlan.StartBackup.Log', @enabled = 0 ;
            END

            IF EXISTS (SELECT [name] FROM msdb.dbo.sysjobs where name like 'DBMaintenancePlan.UpdateStats')
            BEGIN
                EXEC msdb.dbo.sp_update_job @job_name = N'DBMaintenancePlan.UpdateStats', @enabled = 0 ;
            END

            IF EXISTS (SELECT [name] FROM msdb.dbo.sysjobs where name like 'DBMaintenancePlan.ShrinkingLogFiles')
            BEGIN
                EXEC msdb.dbo.sp_update_job @job_name = N'DBMaintenancePlan.ShrinkingLogFiles', @enabled = 0 ;
            END

            IF EXISTS (SELECT [name] FROM msdb.dbo.sysjobs where name like 'DBMaintenancePlan.CheckDB')
            BEGIN
                EXEC msdb.dbo.sp_update_job @job_name = N'DBMaintenancePlan.CheckDB', @enabled = 0 ;
            END

            IF EXISTS (SELECT [name] FROM msdb.dbo.sysjobs where name like 'DBMaintenancePlan.Defraging')
            BEGIN
                EXEC msdb.dbo.sp_update_job @job_name = N'DBMaintenancePlan.Defraging', @enabled = 0 ;
            END

            IF EXISTS (SELECT [name] FROM msdb.dbo.sysjobs where name like 'DBMaintenancePlan.RemoveTrash')
            BEGIN
                EXEC msdb.dbo.sp_update_job @job_name = N'DBMaintenancePlan.RemoveTrash', @enabled = 0 ;
            END

            IF EXISTS (SELECT [name] FROM msdb.dbo.sysjobs where name like 'DBMaintenancePlan.SourceControl')
            BEGIN
                EXEC msdb.dbo.sp_update_job @job_name = N'DBMaintenancePlan.SourceControl', @enabled = 0 ;
            END

            ";

        }
    }
}
