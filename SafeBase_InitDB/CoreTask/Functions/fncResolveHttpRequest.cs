using System;
using System.Data.SqlTypes;
using System.IO;
using System.Net;
using System.Text;
using System.Xml.Linq;
using System.Data;
using System.Data.SqlClient;
using Microsoft.SqlServer.Server; 
using InitDB.Client;
 
public partial class UserDefinedFunctions
{
    [Microsoft.SqlServer.Server.SqlFunction]
    public static SqlXml fncResolveHttpRequest(string requestMethod, string url, string parameters, string headers, int timeout, bool autoDecompress, bool convertResponseToBas64) //, bool debug
    {
        // Se houver solicitação GET e houver parâmetros, crie na url
        if (requestMethod.ToUpper() == "GET" && !string.IsNullOrWhiteSpace(parameters))
        {
            url += (url.IndexOf('?') > 0 ? "&" : "?") + parameters;
        }

        // Cria HttpWebRequest
        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
        var request = (HttpWebRequest)HttpWebRequest.Create(url);

        // Adiciona cabeçalho
        bool contentLengthSetFromHeaders = false;
        bool contentTypeSetFromHeaders = false;
        if (!string.IsNullOrWhiteSpace(headers))
        {
            // Analisar cabeçalhos fornecidos como XML e percorrer elementos de cabeçalho
            var xmlData = XElement.Parse(headers);
            foreach (XElement headerElement in xmlData.Descendants())
            {
                // Recuperar o nome e o valor do cabeçalho
                var headerName = headerElement.Attribute("Name").Value;
                var headerValue = headerElement.Value;

                // Alguns cabeçalhos não podem ser definidos por request.Headers.Add () e precisam definir a propriedade HttpWebRequest diretamente
                switch (headerName)
                {
                    case "Accept":
                        request.Accept = headerValue;
                        break;
                    case "Connection":
                        request.Connection = headerValue;
                        break;
                    case "Content-Length":
                        request.ContentLength = long.Parse(headerValue);
                        contentLengthSetFromHeaders = true;
                        break;
                    case "Content-Type":
                        request.ContentType = headerValue;
                        contentTypeSetFromHeaders = true;
                        break;
                    case "Date":
                        request.Date = DateTime.Parse(headerValue);
                        break;
                    case "Expect":
                        request.Expect = headerValue;
                        break;
                    case "Host":
                        request.Host = headerValue;
                        break;
                    case "If-Modified-Since":
                        request.IfModifiedSince = DateTime.Parse(headerValue);
                        break;
                    case "Range":
                        var parts = headerValue.Split('-');
                        request.AddRange(int.Parse(parts[0]), int.Parse(parts[1]));
                        break;
                    case "Referer":
                        request.Referer = headerValue;
                        break;
                    case "Transfer-Encoding":
                        request.TransferEncoding = headerValue;
                        break;
                    case "User-Agent":
                        request.UserAgent = headerValue;
                        break;
                    default: // outros headers
                        request.Headers.Add(headerName, headerValue);
                        break;
                }
            }
        }

        // Defina o método, tempo limite e descompressão
        request.Method = requestMethod.ToUpper();
        request.Timeout = timeout;
        if (autoDecompress)
        {
            request.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;
        }

        // Adicionar parâmetros não GET fornecidos
        if (requestMethod.ToUpper() != "GET" && !string.IsNullOrWhiteSpace(parameters))
        {
            // Converter para matriz de bytes
            var parameterData = Encoding.ASCII.GetBytes(parameters);

            // Definir informações de conteúdo
            if (!contentLengthSetFromHeaders)
            {
                request.ContentLength = parameterData.Length;
            }
            if (!contentTypeSetFromHeaders)
            {
                request.ContentType = "application/x-www-form-urlencoded";
            }

            // Adiciona dados para solicitar fluxo
            using (var stream = request.GetRequestStream())
            {
                stream.Write(parameterData, 0, parameterData.Length);
            }
        }

        //ServicePointManager.Expect100Continue = true; ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12; // Use SecurityProtocolType.Ssl3 if needed for compatibility reasons

        // Obter resultados da resposta
        XElement returnXml;
        using (var response = (HttpWebResponse)request.GetResponse())
        {
            // Obtem cabeçalhos (percorrer os cabeçalhos da resposta)
            var headersXml = new XElement("Headers");
            var responseHeaders = response.Headers;
            for (int i = 0; i < responseHeaders.Count; ++i)
            {
                // Obtem valores para este cabeçalho
                var valuesXml = new XElement("Values");
                foreach (string value in responseHeaders.GetValues(i))
                {
                    valuesXml.Add(new XElement("Value", value));
                }

                // Adiciona este cabeçalho com seus valores aos cabeçalhos xml
                headersXml.Add(
                    new XElement("Header",
                        new XElement("Name", responseHeaders.GetKey(i)),
                        valuesXml
                    )
                );
            }

            // Obtem o corpo da resposta
            var responseString = String.Empty;
            using (var stream = response.GetResponseStream())
            {
                // Se for solicitado pra converter para a sequência de base 64, use o fluxo de memória, caso contrário, o leitor de fluxo
                if (convertResponseToBas64)
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        // Copia o fluxo de resposta para fluxo de memória
                        stream.CopyTo(memoryStream);

                        // Converte o fluxo de memória em uma matriz de bytes
                        var bytes = memoryStream.ToArray();

                        // Converter para string de base 64
                        responseString = Convert.ToBase64String(bytes);
                    }
                }
                else
                {
                    using (var reader = new StreamReader(stream))
                    {
                        // Recuperar string de resposta
                        responseString = reader.ReadToEnd();
                    }
                }
            }

            // Monta o XML de resposta dos detalhes de HttpWebResponse
            returnXml =
                new XElement("Response",
                    new XElement("CharacterSet", response.CharacterSet),
                    new XElement("ContentEncoding", response.ContentEncoding),
                    new XElement("ContentLength", response.ContentLength),
                    new XElement("ContentType", response.ContentType),
                    new XElement("CookiesCount", response.Cookies.Count),
                    new XElement("HeadersCount", response.Headers.Count),
                    headersXml,
                    new XElement("IsFromCache", response.IsFromCache),
                    new XElement("IsMutuallyAuthenticated", response.IsMutuallyAuthenticated),
                    new XElement("LastModified", response.LastModified),
                    new XElement("Method", response.Method),
                    new XElement("ProtocolVersion", response.ProtocolVersion),
                    new XElement("ResponseUri", response.ResponseUri),
                    new XElement("Server", response.Server),
                    new XElement("StatusCode", response.StatusCode),
                    new XElement("StatusNumber", ((int)response.StatusCode)),
                    new XElement("StatusDescription", response.StatusDescription),
                    new XElement("SupportsHeaders", response.SupportsHeaders),
                    new XElement("Body", responseString)
                );
        }

        // Retorna os dados
        return new SqlXml(returnXml.CreateReader());
    }
}
