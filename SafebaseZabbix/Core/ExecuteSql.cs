using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.Xml;
using System.IO;

namespace External.Client
{
    public static class ExecuteSql
    {
        public static void ExecuteQuery_(string commandText)
        {

            //using (SqlConnection connection = new SqlConnection("Server=(local);Database=db;Trusted_Connection=True;"))
            using (SqlConnection connection = new SqlConnection("context connection=true"))
            {
                SqlCommand command = new SqlCommand(commandText, connection);
                command.CommandType = CommandType.Text;

                connection.Open();
                command.ExecuteNonQuery();
                connection.Close();
            }

        }

        public static String ExecuteQuery(string scriptLine)
        {
            //scriptLine = "insert into [dbo].[Testedb] ([Nome],[DateTest]) values ('Teste da ferramenta DB - Class ExecuteSql',GETDATE())";

            SqlDataReader reader;
            string Value = "";

            try
            {
                using (SqlConnection connection = new SqlConnection("context connection=true"))
                {
                    SqlCommand command = new SqlCommand(scriptLine, connection);

                    command.CommandType = CommandType.Text;

                    connection.Open();

                    reader = command.ExecuteReader();

                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            Value = Value + reader[0].ToString();
                        }
                    }

                    reader.Close();
                    connection.Close();
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
        public static void ExecuteText(string commandText)
        {
            try
            {
                
                using (SqlConnection connection = new SqlConnection("context connection=true"))
                {
                    SqlCommand command = new SqlCommand(commandText, connection)
                    {
                        CommandType = CommandType.Text
                    };

                    connection.Open();
                    command.ExecuteNonQuery();
                    connection.Close();
                }
            }
            catch (Exception ex)
            {
                SendMessage.PostBack(ex.Message);
            }
        }

        public static void myConnection_InfoMessage(object sender, SqlInfoMessageEventArgs e)
        {
            SendMessage.PostBack(e.Message);
        }

        public static void NonQuery(string OperationUID, string scriptLine, bool Output_InfoMessages, bool Ouput_RowsAfected, bool ShowScriptLine)
        {

            try
            {
                using (SqlConnection connection = new SqlConnection("context connection=true"))
                {
                    SqlCommand command = new SqlCommand(scriptLine, connection);

                    command.CommandType = CommandType.Text;

                    if (Output_InfoMessages)
                        connection.InfoMessage += new SqlInfoMessageEventHandler(myConnection_InfoMessage);

                    if (ShowScriptLine)
                        SendMessage.PostBack(scriptLine);

                    connection.Open();
                    int rows = command.ExecuteNonQuery();
                    connection.Close();

                    if (Ouput_RowsAfected)
                        SendMessage.PostBack("(" + rows.ToString() + " row(s) affected)");
                }
            }
            catch (Exception ex)
            {
                SendMessage.PostBack(ex.Message);

                throw ex;
            }
            finally
            {
            }
        }

        public static DataTable Reader(string OperationUID, string scriptLine)
        {
            SqlDataReader reader;
            DataTable Dados = new DataTable();

            try
            {
                using (SqlConnection connection = new SqlConnection("context connection=true"))
                {
                    SqlCommand command = new SqlCommand(scriptLine, connection);
                    command.CommandType = CommandType.Text;

                    connection.Open();
                    reader = command.ExecuteReader();
                    Dados.Load(reader);

                    reader.Close();
                    connection.Close();
                }
            }
            catch (Exception ex)
            {
                SendMessage.PostBack(ex.Message);
            }
            finally
            {
            }

            return Dados;
        }

        public static String Reader_FirstRowColumnOnly(string OperationUID, string scriptLine)
        {

            SqlDataReader reader;
            string Value = "";

            try
            {
                using (SqlConnection connection = new SqlConnection("context connection=true"))
                {
                    SqlCommand command = new SqlCommand(scriptLine, connection);

                    command.CommandType = CommandType.Text;
                    connection.Open();
                    reader = command.ExecuteReader();

                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            Value = Value + reader[0].ToString();
                        }
                    }

                    reader.Close();
                    connection.Close();
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

    }
}
