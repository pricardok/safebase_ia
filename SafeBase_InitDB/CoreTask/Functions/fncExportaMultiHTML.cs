using System.Data;
using System.Data.SqlTypes;
//using Bibliotecas.Model;
using Microsoft.SqlServer.Server;
using InitDB.Client;
using System;
using System.Collections.Generic;
using System.Text;
using System.Data.SqlClient;
using System.Xml;
using System.IO;

public partial class UserDefinedFunctions
{
    [Microsoft.SqlServer.Server.SqlFunction(
        DataAccess = DataAccessKind.Read,
        SystemDataAccess = SystemDataAccessKind.Read
    )]

    public static SqlString fncExportaMultiHTML(SqlString Ds_Query, SqlString Ds_Mensagem_Mail, SqlInt32 Fl_Estilo, SqlBoolean Fl_Html_Completo)
    {

        if (Ds_Query.IsNull)
            return SqlString.Null;
        var titulo = Ds_Query.IsNull ? "" : Ds_Query.Value;
        var MensagemMail = Ds_Mensagem_Mail.IsNull ? "" : Ds_Mensagem_Mail.Value;

        if (Core.QueryPerigosa(Ds_Query.Value))
            return "Identifiquei que a <b style=color:red;> Query </b> utilizada para geração deste relatório pode ser prejudicial. Tarefa abortada. <BR /> <BR /> <BR />";

        var estilo = 1;
        if (!Fl_Estilo.IsNull)
            estilo = Fl_Estilo.Value;

        var servidor = new Core.ServidorAtual().NomeServidor;
        using (var dados = Core.ExecutaQueryRetornaDataTable(servidor, Ds_Query.Value))
        {
            var retorno = CriaCabecalhoHtmlMail(estilo, Fl_Html_Completo.Value);
            retorno += @"
	                    <BR />
                        <BR />
                        <tr>
                           <th colspan=''>" + MensagemMail + @"</th>
                        </tr>
	                    <BR />
	                    <BR />
                        <table cellspacing='0' cellpadding='0'>";

            if (titulo.Length > 0)
            {
                retorno += @"
                            <tbody>
                                 <tr class='subtitulo'>";
                for (var i = 0; i < dados.Columns.Count; i++)
                {
                    retorno += @"<td>" + dados.Columns[i].ColumnName + "</td>";
                }
                retorno += @"</tr>";
            }
            else
            {
                retorno += @"
                            <thead>
                                <tr>";
                for (var i = 0; i < dados.Columns.Count; i++)
                {
                    retorno += @"<th>" + dados.Columns[i].ColumnName + "</th>";
                }
                retorno += @"
                                    </tr>
                                </thead>
                            <tbody>";
            }

            foreach (DataRow linha in dados.Rows)
            {
                retorno += @"<tr>";
                foreach (DataColumn coluna in dados.Columns)
                {
                    retorno += @"<td>" + linha[coluna.ColumnName] + "</td>";
                }
                retorno += @"</tr>";
            }

            retorno += @"
                            </tbody>
                        </table>";
            retorno += CriaRodapeHtmlMail(Fl_Html_Completo.Value);
            return retorno;
        }
    }

    private static string AplicaEstiloCssHTML(int estilo)
    {

        var servidor = new Core.ServidorAtual().NomeServidor;
        var dsQuery = "SELECT DescricaoCSS FROM dbo.LayoutHtmlCss WHERE IdLayout = " + estilo;
        var html = Core.ExecutaQueryScalar(servidor, dsQuery);

        if (string.IsNullOrEmpty(html))
        {
            html = @"
	                table { padding:0; border-spacing: 0; border-collapse: collapse; }
	                thead { background: #00B050; border: 1px solid #ddd; }
	                th { padding: 10px; font-weight: bold; border: 1px solid #000; color: #fff; }
	                tr { padding: 0; }
	                td { padding: 5px; border: 1px solid #cacaca; margin:0; }";
        }
        return html;
    }

    private static string CriaCabecalhoHtmlMail(int estilo, bool Fl_Html_Completo)
    {
        var retorno = "";
        if (Fl_Html_Completo)
        {
            retorno = @"<html>
                        <head>
	                        <title>Relatorio</title>";
        }

        retorno += @"<style type='text/css'>";
        retorno += AplicaEstiloCssHTML(estilo);
        retorno += @"</style>";
        
        if (Fl_Html_Completo)
        {
            retorno += @"
                        </head>
                        <body>";
        }
        return retorno;

    }

    private static string CriaRodapeHtmlMail(bool Fl_Html_Completo)
    {
        var retorno = "";

        if (Fl_Html_Completo)
        {

            retorno += @"
                            </body>
                        </html>
	                    <BR />
	                    <BR />
                        <BR />";

        }
        return retorno;
    }
}