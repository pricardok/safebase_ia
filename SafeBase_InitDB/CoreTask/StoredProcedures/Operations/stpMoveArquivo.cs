using System;
using System.Data.SqlTypes;
using System.IO;

public partial class StoredProcedures
{
    [Microsoft.SqlServer.Server.SqlProcedure]
    public static void stpMoveArquivo(SqlString ArquivoOrigem, SqlString PastaDestino, SqlBoolean Sobrescrever)
    {

        if (ArquivoOrigem.IsNull)
            throw new ApplicationException("Favor informar o arquivo de origem");

        if (PastaDestino.IsNull)
            throw new ApplicationException("Favor informar a pasta de destino");


        try
        {

            var _pasta = new DirectoryInfo(PastaDestino.Value);
            var _arquivo = new FileInfo(ArquivoOrigem.Value);
            var _aquivoNovo = new FileInfo(_pasta.FullName + "\\" + _arquivo.Name);

            if (!_pasta.Exists)
                throw new ApplicationException("A pasta de destino " + _pasta.FullName + " não existe.");

            if (!_arquivo.Exists)
                throw new ApplicationException("O arquivo de origem " + _arquivo.FullName + " não existe.");

            if (_aquivoNovo.FullName == _arquivo.FullName)
                throw new ApplicationException("O caminho de origem e destino não podem ser iguais.");

            if (Sobrescrever)
                if (_aquivoNovo.Exists)
                    _aquivoNovo.Delete();

            _arquivo.MoveTo(_aquivoNovo.FullName);

        }
        catch (Exception e)
        {
            throw new ApplicationException("Erro : " + e.Message);
        }
    }
};