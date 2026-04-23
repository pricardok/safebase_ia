using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.SqlServer.Server;
using System.Data;

namespace InitDB.Client
{
    public static class SendMessage
    {

        public static void PostBack(string Message)
        {
            try
            {
                int chunkSize = 3899;
                int MessageLength = Message.Length;

                for (int i = 0; i < MessageLength; i += chunkSize)
                {
                    if (i + chunkSize > MessageLength)
                        chunkSize = MessageLength - i;

                    SqlContext.Pipe.Send(Message.Substring(i, chunkSize));
                }
                SqlContext.Pipe.Send("\r\n");
            }
            catch (Exception ex)
            {
                SqlContext.Pipe.Send("Error sending message on PostBack \r\n \r\n" + ex.Message);
            }
        }

        public static void PostLogMSG(string Operation, string OperationUID, string Message, string xmlMessage, bool ForceSendMessage)
        {
            string ConfigCompanyName = "??";
            string ConfigServerName = "";
            string ConfigInstanceName = "";
            string ConfigKEY = "??";
            string GetDate = "";
            string InstanceName = "";
            int SendOnStart = 1;
            Exception ToThrow = null;

            //Foi "chumbado a database SafeBase, se a database de manutencao alterar, tera que mudar aqui tbm
            string scriptLine = @"
                                 SET QUOTED_IDENTIFIER ON; SET ARITHABORT ON; 
                                 SELECT TOP 1                                                                                                        
                                     ParametersXML.value('(/Customer/@CompanyName)[1]', 'varchar(max)') as ConfigCompanyName,                            
                                     ParametersXML.value('(/Customer/@ServerName)[1]', 'varchar(max)') as ConfigServerName,                              
                                     ParametersXML.value('(/Customer/@InstanceName)[1]', 'varchar(max)') as ConfigInstanceName,                          
                                     ParametersXML.value('(/Customer/@KEY)[1]', 'varchar(max)') as ConfigKEY,                                            
                                     CONVERT(VARCHAR(max), getdate(), 121) as Agora,                                                                 
                                     @@SERVICENAME as InstanceName,                                                                                  
                                     ParametersXML.value('(/Customer/" + Operation + @"/Messages/@SendOnStart)[1]', 'varchar(max)') as SendPostLogOnStart 
                                 FROM [SafeBase].[dbo].[ConfigDB] 
                                ";

            DataTable Config = ExecuteSql.Reader(OperationUID, scriptLine);

            ConfigCompanyName = Config.Rows[0][0].ToString();
            ConfigServerName = Config.Rows[0][1].ToString();
            ConfigInstanceName = Config.Rows[0][2].ToString();
            ConfigKEY = Config.Rows[0][3].ToString();
            GetDate = Config.Rows[0][4].ToString();
            InstanceName = Config.Rows[0][5].ToString();

            bool isNumeric = int.TryParse(Config.Rows[0][6].ToString(), out SendOnStart);
            if (isNumeric == false)
                SendOnStart = 1;

            if ((SendOnStart == 1 && Message == "Starting") || Message != "Starting" || ForceSendMessage == true)
            {

                xmlMessage = "<DBMessage " +
                                " ConfigCompanyName='" + ConfigCompanyName + "' " +
                                " ConfigServerName='" + ConfigServerName + "' " +
                                " ConfigInstanceName='" + ConfigInstanceName + "' " +
                                " ConfigKEY='" + ConfigKEY + "' " +
                                " ServerName ='" + Environment.MachineName + "' " +
                                " InstanceName='" + InstanceName + "' >" +
                                " <What " +
                                    " GetDate='" + GetDate + "' " +
                                    " Operation='" + Operation + "' " +
                                    " OperationUID='" + OperationUID + "' " +
                                    " Message='" + Message + "' >" +

                                        xmlMessage +

                               " </What> " +
                              " </DBMessage> ";
                try
                {
                    ExecuteSql.InsertIntoPostLogQueue(xmlMessage);
                }
                catch (Exception ex)
                {
                    SendMessage.PostBack("Error on PostLog: " + ex.Message);
                    ToThrow = ex;
                }
                finally
                {
                    ThrowIfNeeded(ToThrow);
                }
                
            }
        }


        public static void ThrowIfNeeded(Exception ex) 
        {
            if (ex != null)
            {
                throw (ex);
            }
        }

    }
}
