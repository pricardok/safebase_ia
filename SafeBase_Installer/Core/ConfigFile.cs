using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace SafeBase_Installer.Core
{
    public static class ConfigFile
    {
        public static string GetOneAgent_XMLFilePath()
        {
            ManagementClass mc = new ManagementClass("Win32_Service");
            string XMLFilePath = "";

            foreach (ManagementObject mo in mc.GetInstances())
            {
                if (mo.GetPropertyValue("Name").ToString() == "ManagementAgent")
                {
                    XMLFilePath = mo.GetPropertyValue("PathName").ToString().Trim('"');
                    XMLFilePath = XMLFilePath.Replace(".exe", ".xml");
                }
            }

            return XMLFilePath;
        }

        public static DataSet LoadXMLToDataSet(string OneAgent_XMLFilePath)
        {
            DataSet ds = new DataSet();
            ds.ReadXml(OneAgent_XMLFilePath);

            return ds;
        }

        public static XmlDocument LoadXML(string OneAgent_XMLFilePath)
        {
            XmlDocument OneAgent_MXL = new XmlDocument();
            if (OneAgent_XMLFilePath != "")
                OneAgent_MXL.Load(OneAgent_XMLFilePath);

            return OneAgent_MXL;

            //Get data from XML into Array
            //string[] Config = new string[1000];
            //int X = 0;
            //foreach (XmlNode Server in OneAgent_MXL.DocumentElement.ChildNodes)
            //{
            //    string line = Server.Attributes["Enabled"].Value +
            //            ";" + Server.Attributes["CompanyName"].Value +
            //            ";" + Server.Attributes["ServerName"].Value +
            //            ";" + Server.Attributes["InstanceName"].Value +
            //            ";" + Server.Attributes["KEY"].Value;                        

            //    Config[X] = line;
            //    X += 1;
            //}
            ////Array.Sort(Config, StringComparer.InvariantCulture);
            //return Config;
        }
    }
}
