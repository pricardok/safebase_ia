using System;
using System.Collections.Generic;
using System.Text;

namespace InitDB.Client
{
    class stpTestTools
    {
        public static string Query()
        {
            return
            @"insert into [dbo].[Testedb] ([Nome],[DateTest]) values ('Teste da ferramenta DB',GETDATE())";

        }
    }
}
