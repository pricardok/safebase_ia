using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Globalization;
using System.Text;

public partial class StoredProcedures
{
    [Microsoft.SqlServer.Server.SqlProcedure]
    public static void stpExportaQueryArquivo(string Query, string Separador, string Caminho, int Coluna)
    {

        var fileStream = new FileStream(Caminho, FileMode.Create);
        var sw = new StreamWriter(fileStream, Encoding.Default);

        try
        {
            using (var conn = new SqlConnection("context connection=true"))
            {

                var getOutput = new SqlCommand
                {
                    CommandText = Query,
                    CommandType = CommandType.Text,
                    CommandTimeout = 120,
                    Connection = conn
                };


                conn.Open();

                var exportData = getOutput.ExecuteReader();

                if (Coluna == 1)
                {
                    for (var i = 0; i < exportData.FieldCount; i++)
                    {
                        sw.Write(exportData.GetName(i));
                        if (i < exportData.FieldCount - 1)
                            sw.Write(Separador);
                    }
                    sw.WriteLine();
                }

                if (string.IsNullOrEmpty(Separador))
                {
                    while (exportData.Read())
                    {
                        for (var i = 0; i < exportData.FieldCount; i++)
                        {
                            sw.Write(Convert.ToString(exportData.GetValue(i), CultureInfo.GetCultureInfo("pt-BR")));
                            if (i < exportData.FieldCount - 1)
                                sw.Write(Separador);
                        }
                        sw.WriteLine();
                    }
                }
                else
                {
                    var SeparadorTroca = new string(' ', Separador.Length);

                    while (exportData.Read())
                    {
                        for (var i = 0; i < exportData.FieldCount; i++)
                        {
                            sw.Write(Convert.ToString(exportData.GetValue(i), CultureInfo.GetCultureInfo("pt-BR")).Replace(Separador, SeparadorTroca));
                            if (i < exportData.FieldCount - 1)
                                sw.Write(Separador);
                        }
                        sw.WriteLine();
                    }
                }


                conn.Close();
                sw.Close();
                conn.Dispose();
                getOutput.Dispose();

            }

        }
        catch (Exception e)
        {
            sw.Close();
            throw new ApplicationException(e.Message);
        }
    }
};