using External.Client;
using Microsoft.SqlServer.Server;
using System;
using System.Collections;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.Diagnostics;

public partial class StoredProcedures
{
    [Microsoft.SqlServer.Server.SqlProcedure]
    public static void stpRemoteQueryZabbix(SqlString Servidor, SqlString database, SqlString Query)
    {
        try
        {
   
            SqlString Ds_Tabela_Destino = "#temp";
            string Login = "s_coleta_dw";
            string Password = "0w4iprxw0D";

            string connection = (string)("Data Source=CUSCO\\EVOLUCAO;Initial Catalog=Tempdb;User ID=s_coleta_dw; Password=0w4iprxw0D");


            using (var dados = ExecutaQueryRetornaDataTable(connection, Query.Value))
            {
                string retorno = @"CREATE TABLE #TEMP (";


                for (var i = 0; i < dados.Columns.Count; i++)
                {
                    retorno += @" " + dados.Columns[i].ColumnName;

                    
                    switch (Convert.ToString(dados.Columns[i].DataType))
                    {
                        case "System.Int16":
                            retorno += @" TINYINT,";
                            break;
                        
                        case "System.Int32":
                            retorno += @" INT,";
                            break;
                        case "System.Int64":
                            retorno += @" BIGINT,";
                            break;
                        case "System.String":
                            retorno += @" NVARCHAR(MAX),";
                            break;
                        case "System.Char":
                            retorno += @" CHAR(MAX),";
                            break;
                        case "System.Decimal":
                            retorno += @" DECIMAL(10,4),";
                            break;
                        case "System.DateTime":
                            retorno += @" DATETIME,";
                            break;
                        default:                           
                            break;
                    }

                    
                }
                retorno += @" ); ";

                //using (var conn = new SqlConnection("Data Source=CUSCO\\INGLATERRATESTE;Initial Catalog=Tempdb;Integrated Security=true;"))
                var servidor = new Core.ServidorAtual().NomeServidor;
                using (var conn = new SqlConnection(Core.Servidor.Localhost.Replace("context connection = true","Integration Security=True;Initial Catalog=Tempdb;")))
                {
                    SqlContext.Pipe.Send(servidor);

                    string q = "select db_name()";
                    var qc = new SqlCommand(q,conn);

                    
                    conn.Open();
                    SqlContext.Pipe.Send(qc.ExecuteReader());
                    new SqlCommand(retorno, conn).ExecuteNonQuery();
                    conn.Close();

                    //using (var s = new SqlBulkCopy(conn))
                    //{
                    //    s.DestinationTableName = Ds_Tabela_Destino.Value;
                    //    s.BulkCopyTimeout = 7200;
                    //    s.BatchSize = 50000;
                    //    s.WriteToServer(dados);
                    //}
                    conn.Open();
                    var query = "select * from #TEMP";

                    var cmd = new SqlCommand(query, conn);

                    SqlContext.Pipe.Send(cmd.ExecuteReader());

                }
            }
        }
        catch (Exception e)
        {
            throw new ApplicationException("Erro : " + e.Message);
        }
    }

    private static DataTable ExecutaQueryRetornaDataTable(string dsConn, string dsQuery)
    {

        using (var con = new SqlConnection(dsConn))
        {

            con.Open();

            using (var cmd = new SqlCommand(dsQuery, con))
            {

                using (var sda = new SqlDataAdapter(cmd))
                {

                    var dt = new DataTable();
                    sda.Fill(dt);

                    return dt;

                }

            }

        }

    }

}
