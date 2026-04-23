using System;
using System.Collections.Generic;
using System.Text;
using SafeBase_Installer.Core;

namespace SafeBase_Installer
{
    class MailDependencies
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

            DECLARE 
	
                @AccountName VARCHAR(100) = 'SafeMailDB',
                @email_address VARCHAR(100) = 'dba@facta.com.br',
                @display_name VARCHAR(100) = 'SafeBase' +'-'+ @@servername,
                @replyto_address VARCHAR(100) = '',  
                @description VARCHAR(100) = '',   
                @mailserver_name VARCHAR(100) = 'smtp.sendgrid.net',   
                @port VARCHAR(100) = 587,
                @timeout INT = 30,  
                @username VARCHAR(100) = 'apikey',  
                @password VARCHAR(100) = 'SG.RNyHLhfaT4unrJa1MwywzA.',  
                @enable_ssl BIT = 1,
	            @IdProfileAccount varchar(10) = (select top 1 pa.profile_id from msdb.dbo.sysmail_profile p join msdb.dbo.sysmail_profileaccount pa on p.profile_id = pa.profile_id 
														              where p.[name] = 'EnviaEmail'),
                @ProfileAccount VARCHAR(100) = 'EnviaEmail',
	            @ProfileDescription varchar (50) = 'DB Mail Serviço de Monitoramento',
	            @IdSequenceNumberSet varchar (10) = 99,
	            @IdSequenceNumber varchar (12) = (select top 1 pa.sequence_number from msdb.dbo.sysmail_profile p join msdb.dbo.sysmail_profileaccount pa on p.profile_id = pa.profile_id 
																            where p.[name] = 'EnviaEmail')

				EXECUTE msdb.dbo.sysmail_delete_profile_sp
							@profile_name = @ProfileAccount

            IF EXISTS (SELECT 'email account already created' FROM msdb.dbo.sysmail_account AS T WHERE T.name = @ProfileAccount)

                EXEC msdb.dbo.sysmail_update_account_sp 
                    @account_name = @ProfileAccount,  
                    @email_address = @email_address,   
                    @display_name = @display_name,   
                    @replyto_address = @replyto_address,  
                    @description = @description,   
                    @mailserver_name = @mailserver_name,   
                    @port = @port,   
                    @timeout = @timeout,  
                    @username = @username,  
                    @password = @password,  
                    @enable_ssl = @enable_ssl 

            ELSE

                EXECUTE msdb.dbo.sysmail_add_account_sp
                    @account_name = @ProfileAccount,  
                    @email_address = @email_address,   
                    @display_name = @display_name,   
                    @replyto_address = @replyto_address,  
                    @description = @description,   
                    @mailserver_name = @mailserver_name,   
                    @port = @port,
                    @username = @username,  
                    @password = @password,  
                    @enable_ssl = @enable_ssl 

            /*
            Create a profile
            */ 
            IF EXISTS (SELECT 'email account already created' 
		               from msdb.dbo.sysmail_profile p 
		               join msdb.dbo.sysmail_profileaccount pa on p.profile_id = pa.profile_id 
		               where p.[name] = @ProfileAccount)

	            EXECUTE msdb.dbo.sysmail_update_profile_sp
		            @profile_id = @IdProfileAccount,
		            @profile_name = @ProfileAccount

            else
	            EXECUTE msdb.dbo.sysmail_add_profile_sp
		            @profile_name = @ProfileAccount,
		            @description = @ProfileDescription

            /*
            Add account to the profile
            */ 

            IF EXISTS (SELECT 'email account already created' 
		               from msdb.dbo.sysmail_profile p 
		               join msdb.dbo.sysmail_profileaccount pa on p.profile_id = pa.profile_id 
		               where p.[name] = @ProfileAccount)

	            EXEC msdb.dbo.sysmail_update_profileaccount_sp
		            @profile_name = @ProfileAccount,
		            @account_name = @ProfileAccount,
		            @sequence_number = @IdSequenceNumber

            else
	            EXECUTE msdb.dbo.sysmail_add_profileaccount_sp
		            @profile_name = @ProfileAccount,
		            @account_name = @ProfileAccount,
		            @sequence_number = @IdSequenceNumberSet;

            /*
            Grant access to the profile
            */ 
            IF EXISTS (SELECT 'email account already created' 
		               from msdb.dbo.sysmail_profile p 
		               join msdb.dbo.sysmail_profileaccount pa on p.profile_id = pa.profile_id 
		               where p.[name] = @ProfileAccount)

	            EXEC msdb.dbo.sysmail_update_principalprofile_sp
		            @profile_name = @ProfileAccount,
		            @principal_name = 'public',
		            @is_default = 1

            else
	            EXEC msdb.dbo.sysmail_add_principalprofile_sp
		            @profile_name = @ProfileAccount,
		            @principal_name = 'public',
		            @is_default = 1


             ";

        }
    }
}
