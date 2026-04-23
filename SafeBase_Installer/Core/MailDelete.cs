using System;
using System.Collections.Generic;
using System.Text;
using SafeBase_Installer.Core;

namespace SafeBase_Installer
{
    class MailDelete
    {
        public static string Query(string use)
        {
            return
            @"
 
            /*
                select *
                from msdb.dbo.sysmail_profile p 
                join msdb.dbo.sysmail_profileaccount pa on p.profile_id = pa.profile_id 
                join msdb.dbo.sysmail_account a on pa.account_id = a.account_id 
                join msdb.dbo.sysmail_server s on a.account_id = s.account_id
                where p.[name] = 'EnviaEmail'
            */

            	DECLARE @ProfileAccountDB VARCHAR(100) = 'EnviaEmail'
					
				IF EXISTS (SELECT top 1 'email account already created' 
		               from msdb.dbo.sysmail_profile p 
		               join msdb.dbo.sysmail_profileaccount pa on p.profile_id = pa.profile_id 
		               where p.[name] = @ProfileAccountDB)


					EXECUTE msdb.dbo.sysmail_delete_account_sp  
							@account_name = @ProfileAccountDB ; 

				IF EXISTS (SELECT top 1 'email account already created' 
		               from msdb.dbo.sysmail_profile p 
		               join msdb.dbo.sysmail_profileaccount pa on p.profile_id = pa.profile_id 
		               where p.[name] = @ProfileAccountDB)

					EXECUTE msdb.dbo.sysmail_delete_profile_sp
							@profile_name = @ProfileAccountDB
             ";

        }
    }
}
