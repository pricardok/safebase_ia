using System;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using Microsoft.SqlServer.Server;

public partial class MyStoredProcedureClass
{
    [Microsoft.SqlServer.Server.SqlProcedure]
    public static void Transpose(SqlString queryParameter)
    {
        // SECTION 1: Variable declarations
        //integers
        int maxNumberofRows = 2048;
        int rowCount = 1;
        int columnCount = 0;
        int transposedRowCount = 0;
        int transposedColumnCount = 0;
        int maxDataSize = 100;
        //strings
        string callersQuery = queryParameter.ToString();
        string[,] queryData;
        string[,] transposedData;

        // .NET SQL objects. These objects will get instantiated later in the code. 
        SqlConnection conn;
        SqlCommand comm;
        SqlDataReader dataReader;
        SqlMetaData[] transposedColumns;
        SqlDataRecord rowRecord;

        try
        {
            // SECTION 2 : Execute Caller's query and store data

            conn = new SqlConnection("context connection=true;");
            comm = new SqlCommand(callersQuery, conn);
            conn.Open();
            dataReader = comm.ExecuteReader();
            columnCount = dataReader.FieldCount;
            queryData = new string[maxNumberofRows, columnCount];
            for (int j = 0; j < columnCount; j++)
            {
                queryData[0, j] = dataReader.GetName(j);
            }

            while (dataReader.Read())
            {
                for (int j = 0; j < columnCount; j++)
                {
                    queryData[rowCount, j] = dataReader[j].ToString();
                }
                rowCount++;
            }
            dataReader.Close();
            conn.Close();

            // SECTION 3:  Transpose the data
            transposedRowCount = columnCount;
            transposedColumnCount = rowCount;
            transposedData = new string[transposedRowCount, transposedColumnCount];

            for (int i = 0; i < transposedRowCount; i++)
            {
                for (int j = 0; j < transposedColumnCount; j++)
                {
                    transposedData[i, j] = queryData[j, i];
                }
            }

            // SECTION 4: Ouput the data back to Caller
            transposedColumns = new SqlMetaData[transposedColumnCount];
            for (int j = 0; j < transposedColumnCount; j++)
            {
                transposedColumns[j]
                    = new SqlMetaData(transposedData[0, j], SqlDbType.VarChar, maxDataSize);
            }
            SqlMetaData[] transposedColumn = null;
            rowRecord = new SqlDataRecord(transposedColumn);
            SqlContext.Pipe.SendResultsStart(rowRecord);
            for (int i = 1; i < transposedRowCount; i++)
            {
                for (int j = 0; j < transposedColumnCount; j++)
                {
                    rowRecord.SetSqlString(j, transposedData[i, j]);
                }
                SqlContext.Pipe.SendResultsRow(rowRecord);
            }
            SqlContext.Pipe.SendResultsEnd();
            SqlContext.Pipe.Send("Transpose complete.");
        }

        // SECTION 5: Handle errors
        catch (Exception e)
        {
            SqlContext.Pipe.Send("There was a problem. \n\nException Report: ");
            SqlContext.Pipe.Send(e.Message.ToString());
        }
        return;
    }
};
