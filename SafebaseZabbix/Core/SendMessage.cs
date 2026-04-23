using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.SqlServer.Server;
using System.Data;

namespace External.Client
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



        public static void ThrowIfNeeded(Exception ex) 
        {
            if (ex != null)
            {
                throw (ex);
            }
        }

    }
}
