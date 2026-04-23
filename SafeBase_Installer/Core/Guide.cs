using System;
using System.Collections.Generic;
using System.Text;
using SafeBase_Installer.Core;

namespace SafeBase_Installer
{
    class guidedb
    {
        public static string Query()
        {
            return
            @"
            USE [safebase]
            insert into [dbo].[Testedb] ([Nome],[DateTest]) values ('TESTE DO INSTALADOR - stpAlertaAlteracaoDB',GETDATE())

        ";

        }
    }

}
