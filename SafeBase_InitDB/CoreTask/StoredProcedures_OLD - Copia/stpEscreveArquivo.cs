using System;
using System.Data.SqlTypes;
using System.IO;
using System.Text;

public partial class StoredProcedures
{
    [Microsoft.SqlServer.Server.SqlProcedure]
    public static void stpEscreveArquivo(SqlString Ds_Texto, SqlString Ds_Caminho, SqlString Ds_Codificacao, SqlString Ds_Formato_Quebra_Linha, SqlBoolean Fl_Append)
    {


        if (!Ds_Texto.IsNull && !Ds_Caminho.IsNull && !Fl_Append.IsNull)
        {

            try
            {

                var dir = Path.GetDirectoryName(Ds_Caminho.Value);

                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

            }
            catch (Exception e)
            {
                throw new ApplicationException(e.Message);
            }


            var encoding = (Ds_Codificacao.IsNull) ? "UTF-8" : Ds_Codificacao.Value;
            if (Ds_Codificacao.Value.Trim() == "")
                encoding = "UTF-8";

            var sb = new StringBuilder(Ds_Texto.Value);

            var fileStream = new FileStream(Ds_Caminho.Value, ((Fl_Append) ? FileMode.Append : FileMode.Create));
            var sw = new StreamWriter(fileStream, Encoding.GetEncoding(encoding));

            switch (Ds_Formato_Quebra_Linha.Value.ToLower())
            {
                case "unix":
                    sw.NewLine = "\n";
                    sb.Replace("\r", "");
                    break;
                case "mac":
                    sw.NewLine = "\r";
                    sb.Replace("\n", "");
                    break;
                default:
                    sw.NewLine = "\r\n";
                    break;
            }


            try
            {

                var texto = sb.ToString();

                sw.Write(texto);
                sw.Close();

            }
            catch (Exception e)
            {
                sw.Close();
                throw new ApplicationException(e.Message);
            }

        }
        else
            throw new ApplicationException("Os parâmetros de input estão vazios");

    }
};