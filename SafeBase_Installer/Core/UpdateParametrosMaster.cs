using System;
using System.Collections.Generic;
using System.Text;
using SafeBase_Installer.Core;

namespace SafeBase_Installer
{
    class UpdateParametrosMaster
    {
        public static string Query(string use)
        {
            return
            @"
            USE " + use + @"

            SET ANSI_NULLS ON
	
            IF(OBJECT_ID('tempdb..##AlertaParametroUpdate') IS NOT NULL)
	            DROP TABLE ##AlertaParametroUpdate;
	        
            SELECT *
            INTO ##AlertaParametroUpdate
            FROM
	            (
		          SELECT * FROM [dbo].[AlertaParametro]
		        ) AS ap;

	            SELECT * FROM ##AlertaParametroUpdate

            GO

            ";

        }
    }
}
