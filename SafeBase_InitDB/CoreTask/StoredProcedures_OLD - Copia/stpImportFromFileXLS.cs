using System;
//using System.Data;
using System.Data.SqlClient;
//using System.Data.SqlTypes;
using System.Data.OleDb;
using System.Data.Common;
//using Microsoft.SqlServer.Server;

public partial class StoredProcedures
{
    [Microsoft.SqlServer.Server.SqlProcedure]

    public static void stpImportFromFileXLS(String FileName, String WorkBook, String TableName)
    {
        using (SqlConnection cn = new SqlConnection("context connection = true"))
        {
            cn.Open();

            // Connection String to Excel Workbook
            string excelConnectionString = @"Provider=Microsoft.Jet.OLEDB.4.0;Data Source=" + FileName + ";Extended Properties='Excel 8.0;HDR=YES;'";


            // Create Connection to Excel Workbook
            using (OleDbConnection connection = new OleDbConnection(excelConnectionString))
            {
                OleDbCommand command = new OleDbCommand("Select Number FROM " + WorkBook + "$]", connection);

                connection.Open();

                // Create DbDataReader to Data Worksheet
                using (DbDataReader dr = command.ExecuteReader())
                {
                    // Bulk Copy to SQL Server
                    using (SqlBulkCopy bulkCopy = new SqlBulkCopy(cn))
                    {
                        bulkCopy.DestinationTableName = TableName;
                        bulkCopy.WriteToServer(dr);
                    }
                }
            }
        }
    }
};