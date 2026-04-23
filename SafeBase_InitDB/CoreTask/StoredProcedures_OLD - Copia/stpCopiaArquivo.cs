using System;
using System.IO;

public partial class StoredProcedures
{
    [Microsoft.SqlServer.Server.SqlProcedure]
    public static void stpCopiaArquivo(string Origem, string Destino, bool Sobrescrever)
    {
        try
        {
            File.Copy(@Origem, @Destino, Sobrescrever);
        }
        catch (Exception e)
        {
            throw new ApplicationException("Erro : " + e.Message);
        }
    }
};