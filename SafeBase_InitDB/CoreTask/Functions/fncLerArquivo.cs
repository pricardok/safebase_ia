using System.IO;
using System.Collections;
using System.Data.SqlTypes;

public partial class UserDefinedFunctions
{

    private class ArquivoLer
    {

        public SqlInt32 Nr_Linha;
        public SqlString Ds_Texto;

        public ArquivoLer(SqlInt32 nrLinha, SqlString dsTexto)
        {

            Nr_Linha = nrLinha;
            Ds_Texto = dsTexto;

        }

    }

    [Microsoft.SqlServer.Server.SqlFunction(
            FillRowMethodName = "FillRow_Arquivo_Ler",
            TableDefinition = "Nr_Linha INT, Ds_Texto NVARCHAR(MAX)"
        )]
    public static IEnumerable fncLerArquivo(string Ds_Caminho)
    {

        var ArquivoLerCollection = new ArrayList();

        if (string.IsNullOrEmpty(Ds_Caminho))
            return ArquivoLerCollection;

        var contador = 1;

        using (var sr = new StreamReader(Ds_Caminho))
        {

            while (sr.Peek() >= 0)
            {

                ArquivoLerCollection.Add(new ArquivoLer(
                    contador,
                    sr.ReadLine()
                ));

                contador++;

            }

            sr.Close();

        }

        return ArquivoLerCollection;

    }

    protected static void FillRow_Arquivo_Ler(object objArquivoLer, out SqlInt32 nrLinha, out SqlString dsTexto)
    {

        var ArquivoLer = (ArquivoLer)objArquivoLer;

        nrLinha = ArquivoLer.Nr_Linha;
        dsTexto = ArquivoLer.Ds_Texto;

    }

}