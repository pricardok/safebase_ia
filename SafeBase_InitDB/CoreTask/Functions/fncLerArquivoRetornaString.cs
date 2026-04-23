using System.Data.SqlTypes;
using System.IO;

public partial class UserDefinedFunctions
{

    [Microsoft.SqlServer.Server.SqlFunction]
    public static SqlString fncLerArquivoRetornaString(SqlString Ds_Caminho)
    {

        if (Ds_Caminho.IsNull)
            return SqlString.Null;


        if (!File.Exists(Ds_Caminho.Value))
            return SqlString.Null;


        using (var sr = new StreamReader(Ds_Caminho.Value))
        {
            return sr.ReadToEnd();
        }

    }
}