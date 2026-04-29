# Guia Completo: Criando e Implantando Projetos CLR no SQL Server

## Índice
1. [Visão Geral](#visão-geral)
2. [Pré-requisitos e Ferramentas](#pré-requisitos-e-ferramentas)
3. [Preparando o Ambiente SQL Server](#preparando-o-ambiente-sql-server)
4. [Criando o Projeto no Visual Studio](#criando-o-projeto-no-visual-studio)
5. [Exemplo Prático - Hello World CLR](#exemplo-prático---hello-world-clr)
6. [Configurações Avançadas do Projeto](#configurações-avançadas-do-projeto)
7. [Implantação (Deploy)](#implantação-deploy)
   - [Deploy via Visual Studio](#deploy-via-visual-studio)
   - [Deploy Manual com Scripts SQL](#deploy-manual-com-scripts-sql)
8. [Permissões Especiais (EXTERNAL_ACCESS / UNSAFE)](#permissões-especiais-external_access--unsafe)
9. [Testando sua Procedure CLR](#testando-sua-procedure-clr)
10. [Dicas e Boas Práticas](#dicas-e-boas-práticas)
11. [Checklist Final para Implantação](#checklist-final-para-implantação)
12. [Solução de Problemas Comuns](#solução-de-problemas-comuns)
13. [Recursos Adicionais](#recursos-adicionais)

---

## Visão Geral

Este tutorial ensina como criar, configurar e implantar assemblies CLR (Common Language Runtime) no SQL Server usando **.NET Framework**. O CLR permite executar código gerenciado dentro do banco de dados, estendendo funcionalidades do SQL Server com lógica .NET.

O CLR é ideal quando o T-SQL se torna complexo ou limitado, por exemplo para:

- manipulações avançadas de strings e expressões regulares
- conversões de dados complexas
- processamento de arquivos e acesso a recursos externos (com permissões apropriadas)
- geração dinâmica de resultados em formato tabular
- integração com APIs internas quando o T-SQL não entrega a mesma expressividade

> ⚠️ Importante: projetos CLR no SQL Server usam **.NET Framework**, não **.NET Core / .NET 5+**. O assembly deve ser compilado para uma versão suportada pelo SQL Server, como **.NET Framework 4.6.1, 4.7.2 ou 4.8**.

---

## Pré-requisitos e Ferramentas

### Software Necessário

| Ferramenta | Versão Recomendada | Propósito |
|------------|-------------------|-----------|
| Windows | 10/11 (64 bits) | Ambiente de desenvolvimento |
| Visual Studio | 2022 ou 2026 Community | Criação do assembly CLR |
| SQL Server | Developer ou Express (com Advanced Services) | Execução do assembly |
| SSMS | Última versão estável | Administração e deploy |

### Workloads do Visual Studio

Durante a instalação do Visual Studio, selecione:

- ✅ **.NET desktop development** (obrigatório)
- ✅ **Data storage and processing** (inclui SSDT/SQL Server Data Tools)
- ✅ Componente adicional: **.NET Framework 4.8 targeting pack**

### Verificação Pós-Instalação

```powershell
# Verificar .NET Framework 4.8
Get-ItemProperty -Path 'HKLM:\SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full' -Name Release
```

O valor de `Release` deve ser maior ou igual a `528040` para .NET Framework 4.8.

---

## Preparando o Ambiente SQL Server

### 1. Habilitar CLR no SQL Server

Conecte-se ao SSMS como `sysadmin` e execute:

```sql
USE master;
GO

EXEC sp_configure 'clr enabled', 1;
RECONFIGURE;
GO

-- Verificar se está habilitado
EXEC sp_configure 'clr enabled';
GO
```

### 2. Configuração Adicional de Segurança

A partir do SQL Server 2017, o SQL Server introduziu o `clr strict security` como padrão. Ele adiciona uma camada de validação de assinaturas e certidões de segurança.

```sql
EXEC sp_configure 'clr strict security', 1;
RECONFIGURE;
GO
```

Se precisar testar rapidamente, você pode desabilitar temporariamente. Porém, em produção, prefira usar assemblies assinados com chave assimétrica.

```sql
EXEC sp_configure 'clr strict security', 0;
RECONFIGURE;
GO
```

---

## Criando o Projeto no Visual Studio

Existem duas formas principais de criar um projeto CLR:

### Opção A: SQL CLR Database Project (Recomendado)

1. Abra o Visual Studio
2. `File` → `New` → `Project`
3. Procure por **SQL CLR**
4. Escolha **SQL CLR C# Database Project**
5. Nomeie o projeto, por exemplo: `MeuProjetoCLR`
6. Defina o **Target Framework** como `.NET Framework 4.8`

Esse template já configura referências básicas e suporta deployment direto.

### Opção B: Class Library (.NET Framework)

Se o template CLR não estiver disponível, use um projeto de biblioteca de classes:

1. `File` → `New` → `Project`
2. Selecione **Class Library (.NET Framework)**
3. Defina **Target Framework** para `.NET Framework 4.8`
4. Adicione referências:
   - `System.Data`
   - `System.Data.SqlTypes`
   - `Microsoft.SqlServer.Server`

No `.csproj`, a referência pode ser algo como:

```xml
<Reference Include="Microsoft.SqlServer.Server" />
```

### Como usar CLR com projetos existentes

Se você já tem um projeto .NET Framework, basta:

- mudar o `Target Framework` para `.NET Framework 4.8`
- garantir que o `Output Type` seja `Class Library`
- adicionar as referências do SQL CLR
- nunca use `.NET Standard` para assembly SQL CLR

---

## Exemplo Prático - Hello World CLR

### 1. Criar a classe da procedure

Crie um arquivo `HelloClr.cs` com:

```csharp
using System;
using Microsoft.SqlServer.Server;
using System.Data.SqlTypes;
using System.Data;

public class Procedures
{
    [SqlProcedure]
    public static void HelloClr()
    {
        SqlContext.Pipe.Send("Hello from CLR! Executando dentro do SQL Server!");

        var record = new SqlDataRecord(
            new SqlMetaData("Mensagem", SqlDbType.NVarChar, 200),
            new SqlMetaData("DataHora", SqlDbType.DateTime)
        );

        record.SetString(0, "Execução bem-sucedida");
        record.SetDateTime(1, DateTime.Now);

        SqlContext.Pipe.Send(record);
    }
}
```

### 2. Exemplo de função escalar

Crie um arquivo `RegexFunctions.cs` com:

```csharp
using System.Text.RegularExpressions;
using Microsoft.SqlServer.Server;
using System.Data.SqlTypes;

public class Functions
{
    [SqlFunction(DataAccess = DataAccessKind.None, IsDeterministic = true)]
    public static SqlString RegexReplace(
        SqlString input,
        SqlString pattern,
        SqlString replacement)
    {
        if (input.IsNull || pattern.IsNull || replacement.IsNull)
            return SqlString.Null;

        string result = Regex.Replace(input.Value, pattern.Value, replacement.Value);
        return new SqlString(result);
    }
}
```

### 3. Build do Projeto

- `Build` → `Build Solution` (`Ctrl+Shift+B`)
- A DLL será gerada em `bin\Debug\MeuProjetoCLR.dll`

> Dica: use `Release` para deploy em produção e `Debug` apenas para testes.

---

## Configurações Avançadas do Projeto

### Propriedades essenciais

1. `Project Properties` → `Application`
   - Target framework: `.NET Framework 4.8`
   - Output type: `Class Library`
2. `Project Properties` → `Signing`
   - Marque `Sign the assembly`
   - Escolha `New...` para criar uma chave forte (`.snk`)
   - Exemplo: `MeuProjetoCLR.snk`
3. `Project Properties` → `SQL CLR` (se disponível)
   - Permission Level: `SAFE` (padrão)
   - `EXTERNAL_ACCESS` para acesso a recursos externos
   - `UNSAFE` para código que precisa de permissões especiais

### Configurações de debugging

Para depurar o assembly no SQL Server:

- Abra o projeto no Visual Studio
- `Debug` → `Attach to Process`
- Selecione `sqlservr.exe`
- Ative a opção `Managed Code`
- Coloque breakpoints no código C# e execute a procedure pelo SSMS

### Estrutura recomendada de namespaces e classes

- Organize o código em classes pequenas e focadas
- Use namespaces claros, por exemplo `SafeBase.SqlClr`
- Mantenha métodos `public static`
- Evite variáveis estáticas que mantenham estado entre invocações
- Prefira `SqlTypes` em vez de tipos CLR puros sempre que possível

---

## Implantação (Deploy)

### Deploy via Visual Studio

1. Clique com o botão direito no projeto → `Publish`
2. Configure a conexão com o banco:
   - Server name: `localhost\SQLEXPRESS` ou seu servidor
   - Authentication: `Windows Authentication`
   - Database: `MinhaDatabase`
3. Clique em `Publish`

O Visual Studio criará o assembly e os objetos SQL automaticamente, dependendo do template.

### Deploy manual com scripts SQL

Quando você precisa de controle total, use scripts SQL para criar assemblies, procedures e funções.

#### Script completo de deploy manual

```sql
USE [MinhaDatabase];
GO

-- 1. Verificar se CLR está habilitado
IF EXISTS (
    SELECT 1 FROM sys.configurations 
    WHERE name = 'clr enabled' AND value_in_use = 0
)
BEGIN
    EXEC sp_configure 'clr enabled', 1;
    RECONFIGURE;
END
GO

-- 2. Criar Assembly (ajuste o caminho da DLL)
CREATE ASSEMBLY MeuProjetoCLR
FROM 'C:\caminho\para\seu\projeto\bin\Debug\MeuProjetoCLR.dll'
WITH PERMISSION_SET = SAFE;
GO

-- 3. Criar Stored Procedure
CREATE PROCEDURE dbo.sp_HelloClr
AS
EXTERNAL NAME MeuProjetoCLR.[Procedures].HelloClr;
GO

-- 4. Criar Function
CREATE FUNCTION dbo.fn_RegexReplace(
    @input NVARCHAR(MAX),
    @pattern NVARCHAR(MAX),
    @replacement NVARCHAR(MAX)
)
RETURNS NVARCHAR(MAX)
AS
EXTERNAL NAME MeuProjetoCLR.[Functions].RegexReplace;
GO

-- 5. Conceder permissões (se necessário)
GRANT EXECUTE ON dbo.sp_HelloClr TO public;
GRANT EXECUTE ON dbo.fn_RegexReplace TO public;
GO

PRINT 'Deploy concluído com sucesso!';
```

#### Atualizar assembly após mudanças

```sql
DROP PROCEDURE IF EXISTS dbo.sp_HelloClr;
DROP FUNCTION IF EXISTS dbo.fn_RegexReplace;
DROP ASSEMBLY IF EXISTS MeuProjetoCLR;
GO

CREATE ASSEMBLY MeuProjetoCLR
FROM 'C:\novo\caminho\MeuProjetoCLR.dll'
WITH PERMISSION_SET = SAFE;
GO

CREATE PROCEDURE dbo.sp_HelloClr
AS
EXTERNAL NAME MeuProjetoCLR.[Procedures].HelloClr;
GO

CREATE FUNCTION dbo.fn_RegexReplace(
    @input NVARCHAR(MAX),
    @pattern NVARCHAR(MAX),
    @replacement NVARCHAR(MAX)
)
RETURNS NVARCHAR(MAX)
AS
EXTERNAL NAME MeuProjetoCLR.[Functions].RegexReplace;
GO
```

> Nota: se você usar `EXTERNAL_ACCESS` ou `UNSAFE`, mantenha os scripts de deploy com os comandos de criação de login/chave assimétrica em um lugar seguro.

---

## Permissões Especiais (EXTERNAL_ACCESS / UNSAFE)

### Quando usar cada permission set

| Permission Set | Uso | Exemplo |
|----------------|-----|---------|
| SAFE | Padrão, sem acesso externo | Manipulação de strings, cálculos internos |
| EXTERNAL_ACCESS | Acesso controlado a arquivos, rede e serviços externos | Ler arquivos locais, consumir APIs HTTP |
| UNSAFE | Acesso irrestrito e código inseguro | P/Invoke, memória nativa, código legado |

> Use `SAFE` sempre que possível. Somente selecione `EXTERNAL_ACCESS` ou `UNSAFE` quando for realmente necessário.

### Assinando assemblies com chave forte

O método mais seguro para liberar assemblies com acesso elevado é assinar a DLL e criar uma chave pública no SQL Server.

#### 1. Criar chave forte no Visual Studio

- `Project Properties` → `Signing`
- Marque `Sign the assembly`
- `Choose a strong name key file` → `New...`
- Nomeie: `MinhaChave.snk`

#### 2. Extrair chave pública

Abra o `Developer Command Prompt` ou `PowerShell` com o SDK instalado e execute:

```powershell
sn -p MinhaChave.snk MinhaChave.public
sn -tp MinhaChave.public
```

#### 3. Configurar no SQL Server

```sql
USE master;
GO

CREATE ASYMMETRIC KEY MinhaChave
FROM FILE = 'C:\caminho\MinhaChave.public';
GO

CREATE LOGIN MinhaChaveLogin
FROM ASYMMETRIC KEY MinhaChave;
GO

GRANT EXTERNAL ACCESS ASSEMBLY TO MinhaChaveLogin;
GRANT UNSAFE ASSEMBLY TO MinhaChaveLogin; -- somente se necessário
GO
```

#### 4. Criar assembly com permissão elevada

```sql
USE MinhaDatabase;
GO

CREATE ASSEMBLY MeuProjetoCLR
FROM 'C:\caminho\MeuProjetoCLR.dll'
WITH PERMISSION_SET = EXTERNAL_ACCESS;
GO
```

### Alternativa menos segura: TRUSTWORTHY

```sql
ALTER DATABASE MinhaDatabase SET TRUSTWORTHY ON;
EXEC sp_changedbowner 'sa';
```

> ⚠️ Não recomendado em produção. O banco fica mais exposto a assemblies maliciosos.

---

## Testando sua Procedure CLR

### Teste básico

```sql
EXEC dbo.sp_HelloClr;
GO

SELECT dbo.fn_RegexReplace(
    'Meu email é teste@exemplo.com',
    '\\w+@\\w+\\.\\w+',
    '[EMAIL OCULTO]'
) AS EmailTratado;
GO
```

### Teste avançado com mensagens

```sql
SET NOCOUNT ON;
PRINT 'Iniciando teste...';

EXEC dbo.sp_HelloClr;

PRINT 'Teste concluído!';
GO
```

### Debugging no SQL Server

Verifique assemblies e módulos carregados:

```sql
SELECT * FROM sys.assemblies;
SELECT * FROM sys.assembly_modules;
SELECT * FROM sys.assembly_files;

SELECT 
    o.name AS ObjectName,
    o.type_desc,
    m.assembly_class,
    m.assembly_method
FROM sys.sql_modules m
JOIN sys.objects o ON m.object_id = o.object_id
WHERE m.assembly_class IS NOT NULL;
```

### Debugging no Visual Studio

1. Abra o projeto no Visual Studio
2. `Debug` → `Attach to Process`
3. Selecione `sqlservr.exe`
4. Ative `Managed Code`
5. Defina breakpoints no código C# 
6. Execute a procedure no SSMS

---

## Dicas e Boas Práticas

### Performance

- ✅ Use `IsDeterministic = true` em funções puras
- ✅ Marque `DataAccessKind.None` quando sua função não acessar dados
- ✅ Evite grandes alocações temporárias no CLR
- ✅ Não mantenha estado estático entre chamadas
- ✅ Use `SqlContext.Pipe.Send` somente quando necessário

### Segurança

- ✅ Use `SAFE` sempre que possível
- ✅ Assine assemblies para produção
- ✅ Valide parâmetros com `SqlString.IsNull`, `SqlInt32.IsNull`, etc.
- ✅ Evite confiança em dados não validados
- ❌ Não use `TRUSTWORTHY ON` em produção

### Manutenção

- Documente todas as procedures e funções CLR
- Versione os scripts de deploy junto com a DLL
- Prefira `Release` para produção e mantenha builds automatizados
- Teste o deploy em um ambiente de homologação antes de produção

### Integração CI/CD

Exemplo de pipeline YAML para build/deploy:

```yaml
- task: VSBuild@1
  inputs:
    solution: '**/*.csproj'
    configuration: 'Release'

- task: SqlAzureDacpacDeployment@1
  inputs:
    sqlFile: 'Deploy.sql'
    serverName: '$(SqlServer)'
    databaseName: '$(Database)'
```

> Use ferramentas de pipeline para gerar o assembly, empacotar o script e executar o deploy em etapas claras.

---

## Checklist Final para Implantação

### Desenvolvimento
- [ ] Visual Studio instalado com workloads corretos
- [ ] Projeto criado como `.NET Framework 4.8`
- [ ] Código implementado com `[SqlProcedure]` / `[SqlFunction]`
- [ ] Build bem-sucedido sem erros

### SQL Server
- [ ] SQL Server Developer/Express instalado
- [ ] CLR habilitado (`sp_configure 'clr enabled', 1`)
- [ ] Banco de dados alvo criado

### Deploy Manual
- [ ] DLL gerada no caminho conhecido
- [ ] Script SQL preparado (`CREATE ASSEMBLY`, `CREATE PROCEDURE`)
- [ ] Assembly criado com sucesso
- [ ] Procedure/Function criada
- [ ] Permissões concedidas, se necessário

### Testes
- [ ] Procedure executou sem erros
- [ ] Retorno esperado recebido
- [ ] Logs verificados
- [ ] Performance aceitável

### Produção
- [ ] Assembly assinado com chave forte
- [ ] Backup do banco realizado
- [ ] Documentação atualizada
- [ ] Rollback planejado

---

## Solução de Problemas Comuns

### Erro: "CREATE ASSEMBLY failed because the assembly is not trusted"

Solução:

```sql
EXEC sp_configure 'clr strict security';
EXEC sp_configure 'clr enabled';
```

Se `clr strict security = 1`, use chave assimétrica ou desabilite temporariamente:

```sql
EXEC sp_configure 'clr strict security', 0;
RECONFIGURE;
```

### Erro: "Unsafe assembly permission set"

Solução: use `EXTERNAL_ACCESS` ou `UNSAFE` apenas com chave assimétrica e login associado.

### Erro: "Method not found"

Solução:

- verifique se a classe e o método são `public static`
- verifique se o namespace/classe/método no `EXTERNAL NAME` estão corretos
- atualize o assembly e recrie os objetos dependentes

### Erro: `SQLCLR error: System.Security.SecurityException`

Solução:

- confirme o `PERMISSION_SET` correto
- confira se o assembly está assinado
- verifique se o banco está configurado com `clr strict security`

### Debugging falhou no Visual Studio

- verifique se o `sqlservr.exe` pertence ao mesmo bitness do Visual Studio
- execute o Visual Studio como administrador
- garanta que o servidor SQL permita debugging remoto

---

## Recursos Adicionais

- Documentação Microsoft: [CLR Integration in SQL Server](https://learn.microsoft.com/sql/relational-databases/clr-integration)
- Exemplo oficial: [SQL Server Samples](https://github.com/microsoft/sql-server-samples)
- Comunidade: [Stack Overflow - `sqlclr` tag](https://stackoverflow.com/questions/tagged/sqlclr)

---

## Versão do Documento

- Versão do Documento: `1.0`
- Última Atualização: `2026`
- Compatível com: SQL Server 2016+ e .NET Framework 4.8
