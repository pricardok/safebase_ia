using System;
using System.Data.SqlTypes;
using System.Diagnostics;

public partial class StoredProcedures
{  

    [Microsoft.SqlServer.Server.SqlProcedure]    
    public static void stpZabbixSender(SqlString Ds_caminho, SqlString Ds_ZabbixServer, SqlString Ds_ZabbixLocalServer, SqlString ZabbixAlertName, SqlInt64 valor)
    {
  

        try 
        {
            var scriptProc = new Process
            {
                StartInfo =
                {
                    FileName = Ds_caminho.Value,
                    Arguments = " -z "+ Ds_ZabbixServer.Value + " -s " + Ds_ZabbixLocalServer.Value
                                + " -k " + ZabbixAlertName.Value  + " -o " + valor.Value,
                    CreateNoWindow = true                    
                }
            };

            

            scriptProc.Start();
        }
        catch (Exception e)
        {
            throw new ApplicationException(e.Message);
        }
    }

}
