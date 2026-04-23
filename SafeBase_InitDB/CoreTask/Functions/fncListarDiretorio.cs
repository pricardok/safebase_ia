using System.IO;
using System.Collections;
using System.Data.SqlTypes;

public partial class UserDefinedFunctions
{

    private class FileProperties
    {

        public SqlInt32 NrLinha;
        public SqlString Tipo;
        public SqlString FileName;
        public SqlString FileNameWithoutExtension;
        public SqlString DirectoryName;
        public SqlString Extension;
        public SqlString FullName;
        public SqlInt64 FileSize;
        public SqlBoolean IsReadOnly;
        public SqlDateTime CreationTime;
        public SqlDateTime LastAccessTime;
        public SqlDateTime LastWriteTime;


        public FileProperties(SqlInt32 nrLinha, SqlString tipo, SqlString fileName, SqlString fileNameWithoutExtension, SqlString directoryName, SqlString extension, SqlString fullName, SqlInt64 fileSize, SqlBoolean isReadOnly, SqlDateTime creationTime, SqlDateTime lastAccessTime, SqlDateTime lastWriteTime)
        {

            NrLinha = nrLinha;
            Tipo = tipo;
            FileNameWithoutExtension = fileNameWithoutExtension;
            FileName = fileName;
            DirectoryName = directoryName;
            Extension = extension;
            FullName = fullName;
            FileSize = fileSize;
            IsReadOnly = isReadOnly;
            CreationTime = creationTime;
            LastAccessTime = lastAccessTime;
            LastWriteTime = lastWriteTime;

        }
    }

    [Microsoft.SqlServer.Server.SqlFunction(
        FillRowMethodName = "listarArquivos",
        TableDefinition = "Linha int, Tipo nvarchar(50), Arquivo nvarchar(500), ArquivoSemExtensao nvarchar(500), Diretorio nvarchar(500), " +
                          "Extensao nvarchar(20), CaminhoCompleto nvarchar(500), QuantidadeTamanho bigint, SomenteLeitura bit, DataCriacao datetime, " +
                          "DataUltimoAcesso datetime, DataModificacao datetime"
    )]
    public static IEnumerable fncListarDiretorio(string Ds_Diretorio, string Ds_Filtro)
    {

        var FilePropertiesCollection = new ArrayList();
        var dirInfo = new DirectoryInfo(Ds_Diretorio);
        var files = dirInfo.GetFiles(Ds_Filtro);
        var directories = dirInfo.GetDirectories(Ds_Filtro);
        var contador = 1;



        foreach (var fileInfo in directories)
        {

            FilePropertiesCollection.Add(new FileProperties(
                contador,
                "Diretorio",
                fileInfo.Name,
                fileInfo.Name,
                fileInfo.Name,
                "",
                fileInfo.FullName + "\\",
                0,
                false,
                fileInfo.CreationTime,
                fileInfo.LastAccessTime,
                fileInfo.LastWriteTime
            ));

            contador++;

        }

        foreach (var fileInfo in files)
        {

            FilePropertiesCollection.Add(new FileProperties(
                contador,
                "Arquivo",
                fileInfo.Name,
                (fileInfo.Extension.Length > 0) ? fileInfo.Name.Replace(fileInfo.Extension, "") : "",
                fileInfo.DirectoryName,
                fileInfo.Extension.ToLower(),
                fileInfo.FullName,
                fileInfo.Length,
                fileInfo.IsReadOnly,
                fileInfo.CreationTime,
                fileInfo.LastAccessTime,
                fileInfo.LastWriteTime
            ));

            contador++;

        }

        return FilePropertiesCollection;

    }

    protected static void listarArquivos(object objFileProperties, out SqlInt32 nrLinha, out SqlString tipo, out SqlString fileName, out SqlString fileNameWithoutExtension, out SqlString directoryName, out SqlString extension, out SqlString fullName, out SqlInt64 fileSize, out SqlBoolean isReadOnly, out SqlDateTime creationTime, out SqlDateTime lastAccessTime, out SqlDateTime lastWriteTime)
    {

        var fileProperties = (FileProperties)objFileProperties;

        nrLinha = fileProperties.NrLinha;
        tipo = fileProperties.Tipo;
        fileName = fileProperties.FileName;
        fileNameWithoutExtension = fileProperties.FileNameWithoutExtension;
        directoryName = fileProperties.DirectoryName;
        extension = fileProperties.Extension;
        fullName = fileProperties.FullName;
        fileSize = fileProperties.FileSize;
        isReadOnly = fileProperties.IsReadOnly;
        creationTime = fileProperties.CreationTime;
        lastAccessTime = fileProperties.LastAccessTime;
        lastWriteTime = fileProperties.LastWriteTime;

    }

}