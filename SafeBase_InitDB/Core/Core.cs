using System.Data;
using System.Data.SqlClient;
using System;
using System.Data.SqlTypes;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.IO;
using InitDB.Client;
using System.Diagnostics;
using Microsoft.SqlServer.Server;
using System.Net;

namespace InitDB.Client
{

    public static class Core
    {

        public static bool QueryPerigosa(string dsQuery)
        {

            var query = dsQuery.ToUpper();

            if (query.Contains("INSERT "))
                return true;

            if (query.Contains("INTO "))
                return true;

            if (query.Contains("DELETE "))
                return true;

            if (query.Contains("TRUNCATE "))
                return true;

            if (query.Contains("UPDATE "))
                return true;

            if (query.Contains("DROP "))
                return true;

            if (query.Contains("ALTER "))
                return true;

            if (query.Contains("CREATE "))
                return true;

            if (query.Contains("DBCC "))
                return true;

            if (query.Contains("EXEC "))
                return true;

            if (query.Contains("BACKUP "))
                return true;

            if (query.Contains("RESTORE "))
                return true;

            if (query.Contains("GRANT "))
                return true;

            if (query.Contains("REVOKE "))
                return true;

            if (query.Contains("DISABLE "))
                return true;

            if (query.Contains("sp_"))
                return true;

            return false;

        }

        public static String ExecuteCreateDir(string commandText)
        {

            string Value = "";

            try
            {
                string folder = commandText;

                if (!Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                }
            }
            catch (Exception ex)
            {
                SendMessage.PostBack(ex.Message);
            }
            finally
            {
            }

            return Value;
        }

        public static String ExecutestpSendMsgTeams(string msg, string channel)
        {

            string Value = "";

            try
            {
                string url = channel;

                string address = "" + url + "";
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(address);
                request.ContentType = "application/json; charset=utf-8";
                request.Method = "POST";

                using (var streamWriter = new StreamWriter(request.GetRequestStream()))
                {
                    string json = "{\"text\": \"" + msg + "\"}";

                    streamWriter.Write(json);
                    streamWriter.Flush();
                }

                var httpResponse = (HttpWebResponse)request.GetResponse();
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    var result = streamReader.ReadToEnd();
                }
            }
            catch (Exception ex)
            {
                SendMessage.PostBack(ex.Message);
            }
            finally
            {
            }

            return Value;
        }

        public static void ExecuteCheckDir()
        {

            //string Value = "";

            try
            {
                string scriptLine;
                scriptLine = ExecuteSql.ExecuteQuery("SELECT Ds_Caminho FROM [dbo].AlertaParametro (NOLOCK) WHERE Nm_Alerta = 'CheckList'");

                // Criar diretorios caso nao existam 
                Core.ExecuteCreateDir(scriptLine);
                Core.ExecuteCreateDir(scriptLine + @"\Logs");
                Core.ExecuteCreateDir(scriptLine + @"\Jobs");
                Core.ExecuteCreateDir(scriptLine + @"\Jobs\Reports");
            }
            catch (Exception ex)
            {
                SendMessage.PostBack(ex.Message);
            }
            finally
            {
            }

            
        }

        public static void GetMSSQL_Version()
        {

            // get SQL Server Version
            int Version = 0;
            try
            {
                string ProductVersion = ExecuteSql.ExecuteQueryReadFast("", "SELECT SERVERPROPERTY('ProductVersion')");
                ProductVersion = ProductVersion.Substring(0, ProductVersion.IndexOf('.'));
                Version = Convert.ToInt32(ProductVersion);
            }
            catch (Exception e)
            {
                SendMessage.PostBack(e.ToString());
            }
            /**
                 * 9  - SQL Server 2005
                 * 10 - SQL Server 2008 R2
                 * 11 - SQL Server 2012
                 * 12 - SQL Server 2014
                 * 13 - SQL Server 2016
                 * 14 - SQL Server 2017 
                 * 14 - SQL Server 2019 
            **/


        }

        public static void Erro(string erro)
        {

            using (var conexao = new SqlConnection("context connection=true"))

            {

                var comando = new SqlCommand("INSERT INTO dbo.LogErro (NomeObjeto, Erro) VALUES (@NomeObjeto, @Erro)", conexao);
                var stackTrace = new StackTrace();
                var objeto = stackTrace.GetFrame(1).GetMethod().Name;

                comando.Parameters.Add(new SqlParameter("@NomeObjeto", SqlDbType.VarChar, 100)).Value = objeto;
                comando.Parameters.Add(new SqlParameter("@Erro", SqlDbType.VarChar, 8000)).Value = erro;
                conexao.Open();
                comando.ExecuteNonQuery();

            }

            throw new ApplicationException(erro);

        }

        internal static object ExecutaQueryScalar(SqlConnection servidor, string dsQuery)
        {
            throw new NotImplementedException();
        }

        public static void Mensagem(string mensagem)
        {

            using (var conexao = new SqlConnection("context connection=true"))
            {

                var Comando = new SqlCommand("IF ( (512 & @@OPTIONS) = 512 ) select 1 else select 0", conexao);
                conexao.Open();

                if ((int)Comando.ExecuteScalar() != 0) return;

                var retorno = SqlContext.Pipe;
                retorno?.Send(mensagem.Length > 4000 ? mensagem.Substring(0, 4000) : mensagem);

            }

        }

        public static void RetornaReader(SqlDataReader dataReader)
        {
            var retorno = SqlContext.Pipe;
            retorno?.Send(dataReader);
        }

        public class ServidorAtual
        {

            public string NomeServidor { get; set; }
            public ServidorAtual()
            {

                try
                {
                    using (var conn = new SqlConnection(Servidor.Context))
                    {

                        conn.Open();
                        using (var cmd = conn.CreateCommand())
                        {
                            cmd.CommandText = "SELECT @@SERVERNAME AS InstanceName";
                            NomeServidor = (string)cmd.ExecuteScalar();
                        }

                        var partes = NomeServidor.Split('\\');
                        if (partes.Length <= 1) return;
                        if (string.Equals(partes[0], partes[1], StringComparison.CurrentCultureIgnoreCase))
                            NomeServidor = partes[0];
                    }

                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }

            public static string[] UsuarioAtual()
            {

                try
                {
                    using (var conn = new SqlConnection(Servidor.Context))
                    {

                        conn.Open();
                        using (var cmd = conn.CreateCommand())
                        {

                            cmd.CommandText = "SELECT suser_name() AS usuario, host_name() AS maquina, APP_NAME() AS programa";
                            using (var dr = cmd.ExecuteReader())
                            {

                                dr.Read();
                                var item = new string[3];

                                item.SetValue(dr["usuario"].ToString(), 0);
                                item.SetValue(dr["maquina"].ToString(), 1);
                                item.SetValue(dr["programa"].ToString(), 2);
                                return item;

                            }

                        }

                    }

                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }
        }

        public static class Servidor
        {
            //Bloco de E-mail
            public static string Ds_Servidor_SMTP => "smtp.office365.com";
            public static int Nr_Porta_SMTP => 587;
            public static bool Fl_Conexao_SSL => true;
            public static bool Fl_Credencial_Padrao_SMTP => false;
            public static string Ds_Remetente_SMTP => "paulo.kuhn@facta.com.br";
            public static int Nr_Timeout_SMTP => 60000;
            public static string Ds_Usuario_SMTP => "paulo.kuhn@facta.com.br";
            public static string Ds_Senha_SMTP => "";
            // fim do bloco
            public static string Ds_Usuario => "";
            public static string Ds_Senha => "";

            //public static string PRODUCAO => "data source=dbaws.sig-energy.com.br,49890;initial catalog=InitDB;persist security info=False;Enlist=False;packet size=4096;user id='" + Ds_Usuario + "';password='" + Ds_Senha + "'";
            public static string PRODUCAO => "context connection=true";
            public static string Context => "context connection=true";
            //public static string Localhost => "data source=LOCALHOST;initial catalog=InitDB;Application Name=SQLCLR;persist security info=False;Enlist=False;packet size=4096;user id='" + Ds_Usuario + "';password='" + Ds_Senha + "'";
            public static string Localhost => "context connection=true";

            public static string getLocalhost()
            {

                var servidorAtual = new ServidorAtual().NomeServidor;
                return "data source=" + servidorAtual + ";initial catalog=SafeBase;Application Name=SQLCLR;persist security info=False;Enlist=False;packet size=4096;user id='" + Ds_Usuario + "';password='" + Ds_Senha + "'";

            }

            public static List<string> Servidores
            {
                get
                {
                    var servidores = new List<string>
                {
                    PRODUCAO,
                    Localhost
                };

                    return servidores;

                }
            }

        }

        public static DataTable ExecutaQueryRetornaDataTable(string dsServidor, string dsQuery)
        {

            using (var con = new SqlConnection(Servidor.Localhost.Replace("context connection = true", dsServidor)))
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

        public static string ExecutaQueryScalar(string dsServidor, string dsQuery)
        {

            string retorno;

            using (var con = new SqlConnection(Servidor.Localhost.Replace("context connection = true", dsServidor)))
            {

                con.Open();

                using (var cmd = new SqlCommand(dsQuery, con))
                {
                    retorno = (cmd.ExecuteScalar() == null) ? "" : cmd.ExecuteScalar().ToString();
                }
            }

            return retorno;

        }

        public static void verificaParametroVazio(object input, string nomeInput, bool permiteStringVazia = false)
        {

            var msgNull = $"O valor do parâmetro '@{nomeInput}' não pode ser NULL";
            var msgVazio = $"O valor do parâmetro '@{nomeInput}' não pode ser uma string vazia";


            if (input is SqlString)
            {

                var parametro = (SqlString)input;

                if (parametro.IsNull)
                    Core.Erro(msgNull);

                if (!permiteStringVazia && parametro.Value.Trim().Length == 0)
                    Core.Erro(msgVazio);

            }
            else if (input is SqlChars)
            {

                var parametro = (SqlChars)input;

                if (parametro.IsNull)
                    Core.Erro(msgNull);

                if (!permiteStringVazia && parametro.Value.Length == 0)
                    Core.Erro(msgVazio);

            }
            else if (input is SqlInt16)
            {

                var parametro = (SqlInt16)input;

                if (parametro.IsNull)
                    Core.Erro(msgNull);

            }
            else if (input is SqlInt32)
            {

                var parametro = (SqlInt32)input;

                if (parametro.IsNull)
                    Core.Erro(msgNull);

            }
            else if (input is SqlInt64)
            {

                var parametro = (SqlInt64)input;

                if (parametro.IsNull)
                    Core.Erro(msgNull);

            }
            else if (input is SqlBoolean)
            {

                var parametro = (SqlBoolean)input;

                if (parametro.IsNull)
                    Core.Erro(msgNull);

            }
            else if (input is SqlByte)
            {

                var parametro = (SqlByte)input;

                if (parametro.IsNull)
                    Core.Erro(msgNull);

            }
            else if (input is SqlBinary)
            {

                var parametro = (SqlBinary)input;

                if (parametro.IsNull)
                    Core.Erro(msgNull);

            }
            else if (input is SqlDateTime)
            {

                var parametro = (SqlDateTime)input;

                if (parametro.IsNull)
                    Core.Erro(msgNull);

            }
            else if (input is SqlDecimal)
            {

                var parametro = (SqlDecimal)input;

                if (parametro.IsNull)
                    Core.Erro(msgNull);

            }
            else if (input is SqlDouble)
            {

                var parametro = (SqlDouble)input;

                if (parametro.IsNull)
                    Core.Erro(msgNull);

            }
            else if (input is SqlGuid)
            {

                var parametro = (SqlGuid)input;

                if (parametro.IsNull)
                    Core.Erro(msgNull);

                if (!permiteStringVazia && parametro.Value.ToString().Length == 0)
                    Core.Erro(msgVazio);

            }
            else if (input is SqlXml)
            {

                var parametro = (SqlXml)input;

                if (parametro.IsNull)
                    Core.Erro(msgNull);

                if (!permiteStringVazia && parametro.Value.Length == 0)
                    Core.Erro(msgVazio);

            }
            else if (input is SqlMoney)
            {

                var parametro = (SqlMoney)input;

                if (parametro.IsNull)
                    Core.Erro(msgNull);

            }
            else if (input is SqlSingle)
            {

                var parametro = (SqlSingle)input;

                if (parametro.IsNull)
                    Core.Erro(msgNull);

            }
        }

        public static bool gravaLogEmail(SqlString destinatarios, SqlString assunto, SqlString mensagem, SqlString arquivos)
        {

            /*

            CREATE TABLE dbo.LogEmail (
                IdLog BIGINT IDENTITY(1, 1) NOT NULL,
                DataLog DATETIME DEFAULT GETDATE(),
                Destinatario VARCHAR(MAX) NOT NULL,
                Assunto VARCHAR(MAX) NULL,
                Mensagem VARCHAR(MAX) NULL,
                Arquivos VARCHAR(MAX) NULL,
                Usuario VARCHAR(100) NULL
            )

            */


            try
            {

                using (var conexao = new SqlConnection(Servidor.getLocalhost()))
                {

                    conexao.Open();

                    using (var comando = new SqlCommand("INSERT INTO dbo.LogEmail (Destinatario, Assunto, Mensagem, Arquivos, Usuario) VALUES (@Destinatario, @Assunto, @Mensagem, @Arquivos, @Usuario)", conexao))
                    {

                        var dadosUsuario = ServidorAtual.UsuarioAtual();

                        comando.Parameters.Add(new SqlParameter("@Destinatario", SqlDbType.VarChar, -1)).Value = destinatarios.Value;
                        comando.Parameters.Add(new SqlParameter("@Assunto", SqlDbType.VarChar, -1)).Value = assunto.Value;
                        comando.Parameters.Add(new SqlParameter("@Mensagem", SqlDbType.VarChar, -1)).Value = mensagem.Value;
                        comando.Parameters.Add(new SqlParameter("@Arquivos", SqlDbType.VarChar, -1)).Value = arquivos.Value;
                        comando.Parameters.Add(new SqlParameter("@Usuario", SqlDbType.VarChar, -1)).Value = dadosUsuario[0];

                        comando.ExecuteNonQuery();

                        return true;
                    }
                }

            }
            catch (Exception)
            {
                return false;
            }

        }

    }

}