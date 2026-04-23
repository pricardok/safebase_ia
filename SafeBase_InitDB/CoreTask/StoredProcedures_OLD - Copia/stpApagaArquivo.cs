using System;
using System.IO;

public partial class StoredProcedures
{
    [Microsoft.SqlServer.Server.SqlProcedure]
    public static void stpApagaArquivo(string caminho)
    {
        try
        {

            var Arquivo = new FileInfo(caminho);

            if (Arquivo.Exists)
            {
                Arquivo.Delete();
            }
            else
            {
                throw new ApplicationException("O Arquivo especificado não existe.");
            }
        }
        catch (Exception e)
        {
            throw new ApplicationException("Erro : " + e.Message);
        }
    }
};