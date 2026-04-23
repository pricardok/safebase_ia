using System;
using System.Collections.Generic;
using System.Text;
using SafeBase_Installer.Core;

namespace SafeBase_Installer
{
    class DeleteJobs
    {
        public static string Query()
        {
            return
            @"
            USE msdb ; 

            IF EXISTS (SELECT [name] FROM msdb.dbo.sysjobs where name like 'DBMaintenancePlan.AlertDB.No.RealTime')
            BEGIN
                EXEC msdb.dbo.sp_delete_job @job_name = N'DBMaintenancePlan.AlertDB.No.RealTime', @delete_unused_schedule = 1
            END

            IF EXISTS (SELECT [name] FROM msdb.dbo.sysjobs where name like 'DBMaintenancePlan.AlertDB.RealTime')
            BEGIN
                EXEC msdb.dbo.sp_delete_job @job_name = N'DBMaintenancePlan.AlertDB.RealTime', @delete_unused_schedule = 1;
            END

            IF EXISTS (SELECT [name] FROM msdb.dbo.sysjobs where name like 'DBMaintenancePlan.CheckListDB')
            BEGIN
                EXEC msdb.dbo.sp_delete_job @job_name = N'DBMaintenancePlan.CheckListDB', @delete_unused_schedule = 1
            END

            IF EXISTS (SELECT [name] FROM msdb.dbo.sysjobs where name like 'DBMaintenancePlan.Data.Load')
            BEGIN
                EXEC msdb.dbo.sp_delete_job @job_name = N'DBMaintenancePlan.Data.Load', @delete_unused_schedule = 1
            END

            IF EXISTS (SELECT [name] FROM msdb.dbo.sysjobs where name like 'DBMaintenancePlan.StartBackup.Diff')
            BEGIN
                EXEC msdb.dbo.sp_delete_job @job_name = N'DBMaintenancePlan.StartBackup.Diff', @delete_unused_schedule = 1;
            END

            IF EXISTS (SELECT [name] FROM msdb.dbo.sysjobs where name like 'DBMaintenancePlan.StartBackup.Full')
            BEGIN
                EXEC msdb.dbo.sp_delete_job @job_name = N'DBMaintenancePlan.StartBackup.Full', @delete_unused_schedule = 1
            END

            IF EXISTS (SELECT [name] FROM msdb.dbo.sysjobs where name like 'DBMaintenancePlan.StartBackup.Log')
            BEGIN
                EXEC msdb.dbo.sp_delete_job @job_name = N'DBMaintenancePlan.StartBackup.Log', @delete_unused_schedule = 1
            END

            IF EXISTS (SELECT [name] FROM msdb.dbo.sysjobs where name like 'DBMaintenancePlan.UpdateStats')
            BEGIN
                EXEC msdb.dbo.sp_delete_job @job_name = N'DBMaintenancePlan.UpdateStats', @delete_unused_schedule = 1
            END

            IF EXISTS (SELECT [name] FROM msdb.dbo.sysjobs where name like 'DBMaintenancePlan.ShrinkingLogFiles')
            BEGIN
                EXEC msdb.dbo.sp_delete_job @job_name = N'DBMaintenancePlan.ShrinkingLogFiles', @delete_unused_schedule = 1
            END

            IF EXISTS (SELECT [name] FROM msdb.dbo.sysjobs where name like 'DBMaintenancePlan.CheckDB')
            BEGIN
                EXEC msdb.dbo.sp_delete_job @job_name = N'DBMaintenancePlan.CheckDB', @delete_unused_schedule = 1
            END

            IF EXISTS (SELECT [name] FROM msdb.dbo.sysjobs where name like 'DBMaintenancePlan.Defraging')
            BEGIN
                EXEC msdb.dbo.sp_delete_job @job_name = N'DBMaintenancePlan.Defraging', @delete_unused_schedule = 1
            END

            IF EXISTS (SELECT [name] FROM msdb.dbo.sysjobs where name like 'DBMaintenancePlan.RemoveTrash')
            BEGIN
                EXEC msdb.dbo.sp_delete_job @job_name = N'DBMaintenancePlan.RemoveTrash', @delete_unused_schedule = 1
            END

            ";

        }
    }
}
