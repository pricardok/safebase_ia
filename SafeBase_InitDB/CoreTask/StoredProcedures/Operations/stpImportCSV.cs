using System;
using System.Data;
using System.Data.SqlTypes;
using System.IO;
using System.Text;
using InitDB.Client;
//using System.Data.Linq;
using System.Data.SqlClient;
using Microsoft.SqlServer.Server;
//using Bibliotecas.Model;

public partial class StoredProcedures
{
    [Microsoft.SqlServer.Server.SqlProcedure]
    public static void stpImportCSV(SqlString Ds_Caminho_Arquivo, SqlString Ds_Separador, SqlBoolean Fl_Primeira_Linha_Cabecalho, SqlInt32 Nr_Linha_Inicio, SqlInt32 Nr_Linhas_Retirar_Final, SqlString Ds_Tabela_Destino, SqlString Ds_Codificacao)
    {

        try
        {

            if (!File.Exists(Ds_Caminho_Arquivo.Value))
                Core.Erro("Não foi possível encontrar o arquivo no caminho informado (" + Ds_Caminho_Arquivo.Value + ")");

            var encoding = (Ds_Codificacao.IsNull) ? "UTF-8" : Ds_Codificacao.Value;
            if (Ds_Codificacao.Value.Trim() == "")
                encoding = "UTF-8";

            var arrLinhas = File.ReadAllLines(Ds_Caminho_Arquivo.Value, Encoding.GetEncoding(encoding));
            string[] linha;
            var nrLinhas = arrLinhas.Length;

            var nrLinhaInicioLeitura = Nr_Linha_Inicio.Value;

            if (nrLinhaInicioLeitura <= 0)
                nrLinhaInicioLeitura = 1;

            var nrLinhasRetirarLeitura = Nr_Linhas_Retirar_Final.Value;

            if (nrLinhasRetirarLeitura >= nrLinhas)
                nrLinhasRetirarLeitura = 0;

            if (nrLinhaInicioLeitura > nrLinhas)
                Core.Erro("O parâmetro Nr_Linhas_Inicio é maior que a quantidade total de linhas do arquivo.");

            nrLinhas = nrLinhas - nrLinhasRetirarLeitura;

            var nrColunas = arrLinhas[nrLinhaInicioLeitura - 1].Split(new string[] { Ds_Separador.Value }, StringSplitOptions.None).Length;
            var rowId = 1;


            if (!Ds_Tabela_Destino.IsNull && Ds_Tabela_Destino.Value != "")
            {

                using (var conn = new SqlConnection(Core.Servidor.getLocalhost()))
                {

                    conn.Open();

                    var objectId = new SqlCommand("SELECT OBJECT_ID('" + Ds_Tabela_Destino.Value + "')", conn).ExecuteScalar().ToString();
                    if (!string.IsNullOrEmpty(objectId))
                    {
                        Core.Erro("A tabela de destino '" + Ds_Tabela_Destino.Value + "' já existe! Favor apagar antes de importar o CSV");
                    }


                    var queryCriacaoTabela = "CREATE TABLE " + Ds_Tabela_Destino.Value + "( RowID INT";
                    using (var dados = new DataTable())
                    {

                        dados.Columns.Add("RowID", typeof(int));

                        if (Fl_Primeira_Linha_Cabecalho.Value)
                        {

                            var cabecalho = arrLinhas[nrLinhaInicioLeitura - 1].Split(new string[] { Ds_Separador.Value }, StringSplitOptions.None);

                            for (var i = 0; i < nrColunas; i++)
                            {

                                var nomeColuna = cabecalho[i].Replace("\"", "");
                                if (nomeColuna.Length == 0)
                                    nomeColuna = "Coluna_" + i;

                                dados.Columns.Add(nomeColuna, typeof(string));
                                queryCriacaoTabela += ", " + nomeColuna + " VARCHAR(MAX)";

                            }

                            nrLinhaInicioLeitura = nrLinhaInicioLeitura + 1;

                        }
                        else
                        {

                            for (var i = 0; i < nrColunas; i++)
                            {

                                dados.Columns.Add("Ds_Coluna_" + (i + 1), typeof(string));
                                queryCriacaoTabela += ", Ds_Coluna_" + (i + 1) + " VARCHAR(MAX)";
                            }

                        }

                        queryCriacaoTabela += " )";

                        for (var i = (nrLinhaInicioLeitura - 1); i < nrLinhas; i++)
                        {

                            linha = arrLinhas[i].Split(new string[] { Ds_Separador.Value }, StringSplitOptions.None);
                            var arrId = new string[] { rowId.ToString() };

                            //linha = arrId.Concat(linha).ToArray();
                            dados.Rows.Add(linha);
                            rowId++;

                        }

                        // Grava os dados

                        new SqlCommand(queryCriacaoTabela, conn).ExecuteNonQuery();

                        using (var s = new SqlBulkCopy(conn))
                        {
                            s.DestinationTableName = Ds_Tabela_Destino.Value;
                            s.BulkCopyTimeout = 7200;
                            s.BatchSize = 50000;
                            s.WriteToServer(dados);
                        }
                    }
                }
            }
            else
            {

                var pipe = SqlContext.Pipe;

                // Cria o cabeçalho
                var colunas = new SqlMetaData[nrColunas + 1];
                colunas[0] = new SqlMetaData("RowID", SqlDbType.Int);

                if (Fl_Primeira_Linha_Cabecalho)
                {

                    var cabecalho = arrLinhas[nrLinhaInicioLeitura - 1].Split(new string[] { Ds_Separador.Value }, StringSplitOptions.None);
                    for (var i = 0; i < nrColunas; i++)
                        colunas[i + 1] = new SqlMetaData(cabecalho[i].Replace("\"", ""), SqlDbType.VarChar, 1024);
                    nrLinhaInicioLeitura = nrLinhaInicioLeitura + 1;

                }
                else
                {

                    for (var i = 0; i < nrColunas; i++)
                        colunas[i + 1] = new SqlMetaData("Ds_Coluna_" + (i + 1), SqlDbType.VarChar, 1024);

                }


                // Recupera os registros
                var linhaSql = new SqlDataRecord(colunas);
                pipe?.SendResultsStart(linhaSql);

                for (var i = (nrLinhaInicioLeitura - 1); i < nrLinhas; i++)
                {

                    linha = arrLinhas[i].Split(new string[] { Ds_Separador.Value }, StringSplitOptions.None);
                    linhaSql.SetSqlInt32(0, new SqlInt32(rowId));

                    for (var j = 0; j < nrColunas; j++)
                    {
                        linhaSql.SetSqlString(j + 1, new SqlString(linha[j].Replace("\"", "")));
                    }

                    pipe?.SendResultsRow(linhaSql);
                    rowId++;

                }

                pipe?.SendResultsEnd();

            }

        }
        catch (Exception e)
        {
            Core.Erro("Erro : " + e.Message);
        }
    }
};