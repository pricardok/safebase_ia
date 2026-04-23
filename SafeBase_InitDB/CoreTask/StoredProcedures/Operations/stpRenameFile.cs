using System;
using System.Data.SqlTypes;
using System.IO;

public partial class StoredProcedures
{
    [Microsoft.SqlServer.Server.SqlProcedure]
    public static void stpRenameFile(SqlString CaminhoOrigem, SqlString CaminhoDestino, SqlBoolean Sobrescrever)
    {
        try
        {
            if (Sobrescrever.Value)
                if (File.Exists(CaminhoDestino.Value))
                    File.Delete(CaminhoDestino.Value);

            File.Move(CaminhoOrigem.Value, CaminhoDestino.Value);
        }
        catch (Exception e)
        {
            throw new ApplicationException("Erro : " + e.Message);
        }
    }
};