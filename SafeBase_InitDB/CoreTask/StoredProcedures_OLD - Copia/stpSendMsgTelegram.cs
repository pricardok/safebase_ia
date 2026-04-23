using System;
using System.Data.SqlTypes;
using System.IO;
using System.Net;
using System.Text;
using Bibliotecas.Model;

public partial class StoredProcedures
{
    [Microsoft.SqlServer.Server.SqlProcedure]
    public static void stpSendMsgTelegram(SqlString Destino, SqlString Msg)
    {


        const string token = "187235235:AAEVrK7cZsiAnfVt-KyH6VAg5aboObBY3hI";


        try
        {

            var mensagem = Msg.Value;
            var canais = Destino.Value.Split(';');

            foreach (var canal in canais)
            {

                var dsScript = $"chat_id={canal.Trim()}&text={mensagem}&parse_mode=Markdown";

                var url = $"https://api.telegram.org/bot{token}/sendMessage";

                var request = (HttpWebRequest)WebRequest.Create(url);

                request.Method = "POST";
                request.UserAgent = "curl/7.45.0";
                request.ContentType = "application/x-www-form-urlencoded";

                var buffer = Encoding.GetEncoding("UTF-8").GetBytes(dsScript);
                using (var reqstr = request.GetRequestStream())
                {

                    reqstr.Write(buffer, 0, buffer.Length);

                    using (var response = request.GetResponse())
                    {

                        using (var dataStream = response.GetResponseStream())
                        {

                            if (dataStream == null) return;

                            using (var reader = new StreamReader(dataStream))
                            {
                                var responseFromServer = reader.ReadToEnd();
                                Retorno.Mensagem(responseFromServer);
                            }
                        }

                    }

                }

            }

        }
        catch (Exception e)
        {
            Retorno.Erro("Erro : " + e.Message);
        }

    }

};