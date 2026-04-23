using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Net;

namespace SafeBase_Installer.Core
{
    public static class HttpRequest
    {
        const string PORTAL_DNS = "http://DEV01:8080/";

        public static string Get(string url)
        {
            url = PORTAL_DNS + url;

            string value = null;

            var httpWebRequest = (HttpWebRequest)WebRequest.Create(url);
            httpWebRequest.ContentType = "application/json";
            httpWebRequest.Method = "GET";

            var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

            using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
            {
                value = streamReader.ReadToEnd();
            }

            return value;
        }

        public static string Post(string xmlMessage)
        {
            string url = PORTAL_DNS + "Manager.Api/api/Log";

            var result = string.Empty;

            var httpWebRequest = (HttpWebRequest)WebRequest.Create(url);
            httpWebRequest.ContentType = "application/json";
            httpWebRequest.Method = "POST";

            using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
            {
                var json =

                "{\"Xml\": \"" + xmlMessage + "\" }";

                streamWriter.Write(json);
                streamWriter.Flush();
            }
            var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

            using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
            {
                result = streamReader.ReadToEnd();
            }

            return result;
        }
    }
}
