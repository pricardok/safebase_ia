using System;
using System.Collections.Generic;
using System.Text;
using SafeBase_Installer.Core;

namespace SafeBase_Installer
{
    class CreateJobsAgentDependencies
    {
        public static string Query()
        {
            return
            @"

            USE [msdb]
            GO

            /****** Object:  Operator [MonitoramentoDB]    Script Date: 06/04/2020 19:42:31 ******/
            IF EXISTS (SELECT * FROM msdb..sysoperators WHERE NAME = 'MonitoramentoDB')
                EXEC msdb.dbo.sp_delete_operator @name=N'MonitoramentoDB'
            GO

            /****** Object:  Operator [MonitoramentoDB]    Script Date: 06/04/2020 19:42:31 ******/
            EXEC msdb.dbo.sp_add_operator @name=N'MonitoramentoDB', 
		            @enabled=1, 
		            @weekday_pager_start_time=90000, 
		            @weekday_pager_end_time=180000, 
		            @saturday_pager_start_time=90000, 
		            @saturday_pager_end_time=180000, 
		            @sunday_pager_start_time=90000, 
		            @sunday_pager_end_time=180000, 
		            @pager_days=0, 
		            @email_address=N'dba@facta.com.br', 
		            @category_name=N'[Uncategorized]'
            GO

            IF NOT EXISTS (SELECT name FROM msdb.dbo.syscategories WHERE name=N'Database Maintenance' AND category_class=1)
	            BEGIN
		            EXEC  msdb.dbo.sp_add_category @class=N'JOB', @type=N'LOCAL', @name=N'Database Maintenance'
	            END
            ELSE
	            PRINT 'category Database Maintenance Exists' 
            GO
            ";

        }
    }
}
