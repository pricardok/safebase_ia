using System;
using System.Collections.Generic;
using System.Text;

namespace InitDB.Client
{
    class stpCheckAlerta
    {
        public static string Query()
        {
            return
            // @"insert into [dbo].[Testedb] ([Nome],[DateTest]) values ('Teste da ferramenta DB',GETDATE())";
            @"SET NOCOUNT ON

                IF(OBJECT_ID('tempdb..#CheckAlerta') IS NOT NULL)
		            DROP TABLE #CheckAlerta

	            CREATE TABLE #CheckAlerta (
		            Id_Alerta INT,
		            Id_AlertaParametro INT,
		            Nm_Alerta VARCHAR(200),
		            Ds_Mensagem VARCHAR(2000),
		            Dt_Alerta DATETIME,
		            Fl_Tipo BIT,
		            Run_Duration VARCHAR(18)
	            )

	            -- Seta a Data de Referencia
	            DECLARE @Dt_Referencia DATETIME = DATEADD(HOUR, -24, GETDATE())

	            -- Busca os Alertas a partir da Data de Referência
	            INSERT INTO #CheckAlerta
	            SELECT [Id_Alerta], A.[Id_AlertaParametro], [Nm_Alerta], [Ds_Mensagem], [Dt_Alerta], [Fl_Tipo], NULL	
	            FROM [dbo].[Alerta] A WITH(NOLOCK)
	            JOIN [dbo].[AlertaParametro] B WITH(NOLOCK) ON A.Id_AlertaParametro = B.Id_AlertaParametro
	            WHERE [Dt_Alerta] > @Dt_Referencia

	            IF(OBJECT_ID('tempdb..#CheckAlertaClear') IS NOT NULL)
		            DROP TABLE #CheckAlertaClear

	            select A.Id_Alerta, A.Dt_Alerta AS Dt_Clear, MAX(B.Dt_Alerta) AS Dt_Alerta
	            into #CheckAlertaClear
	            from #CheckAlerta A
	            JOIN [dbo].[AlertaParametro] C WITH(NOLOCK) ON A.Id_AlertaParametro = C.Id_AlertaParametro
	            JOIN [dbo].[Alerta] B ON A.Id_AlertaParametro = C.Id_AlertaParametro and B.Fl_Tipo = 1 and B.Dt_Alerta < A.Dt_Alerta	
	            where A.Fl_Tipo = 0
	            group by A.Id_Alerta, A.Dt_Alerta

	            UPDATE A
	            SET	A.Run_Duration =
			            RIGHT('00' + CAST((DATEDIFF(SECOND,B.Dt_Alerta, B.Dt_Clear) / 86400) AS VARCHAR), 2) + ' Dia(s) ' +	-- Dia
			            RIGHT('00' + CAST((DATEDIFF(SECOND,B.Dt_Alerta, B.Dt_Clear) / 3600 % 24) AS VARCHAR), 2) + ':' +	-- Hora
			            RIGHT('00' + CAST((DATEDIFF(SECOND,B.Dt_Alerta, B.Dt_Clear) / 60 % 60) AS VARCHAR), 2) + ':' +		-- Minutos
			            RIGHT('00' + CAST((DATEDIFF(SECOND,B.Dt_Alerta, B.Dt_Clear) % 60) AS VARCHAR), 2)					-- Segundos	
	            from #CheckAlerta A
	            join #CheckAlertaClear B on A.Id_Alerta = B.Id_Alerta
	
	            -- Limpa os dados antigos da tabela do CheckList	
	            TRUNCATE TABLE [dbo].[CheckAlerta]
	
	            INSERT INTO [dbo].[CheckAlerta]
	            SELECT Nm_Alerta, Ds_Mensagem, Dt_Alerta, Run_Duration 
	            FROM #CheckAlerta

	            IF (@@ROWCOUNT = 0)
	            BEGIN
		            INSERT INTO [dbo].[CheckAlerta] ( [Nm_Alerta], [Ds_Mensagem], [Dt_Alerta], [Run_Duration] )
		            SELECT 'Sem registro de Alertas no dia Anterior', NULL, NULL, NULL
	            END

	            TRUNCATE TABLE [dbo].[CheckAlertaSemClear]
	
	            -- Busca os Alertas que estão sem o CLEAR
	            INSERT INTO [dbo].[CheckAlertaSemClear]
	            SELECT	[Nm_Alerta], [Ds_Mensagem], [Dt_Alerta],
			            RIGHT('00' + CAST((DATEDIFF(SECOND,Dt_Alerta, GETDATE()) / 86400) AS VARCHAR), 2) + ' Dia(s) ' +	-- Dia
			            RIGHT('00' + CAST((DATEDIFF(SECOND,Dt_Alerta, GETDATE()) / 3600 % 24) AS VARCHAR), 2) + ':' +		-- Hora
			            RIGHT('00' + CAST((DATEDIFF(SECOND,Dt_Alerta, GETDATE()) / 60 % 60) AS VARCHAR), 2) + ':' +			-- Minutos
			            RIGHT('00' + CAST((DATEDIFF(SECOND,Dt_Alerta, GETDATE()) % 60) AS VARCHAR), 2) AS [Run_Duration]	-- Segundos	
	            FROM [dbo].[Alerta] A WITH(NOLOCK)
	            JOIN [dbo].[AlertaParametro] B WITH(NOLOCK) ON A.Id_AlertaParametro = B.Id_AlertaParametro
	            WHERE	[Id_Alerta] = ( SELECT MAX([Id_Alerta]) FROM [dbo].[Alerta] B WITH(NOLOCK) WHERE A.Id_AlertaParametro = B.Id_AlertaParametro )
			            AND B.[Fl_Clear] = 1	-- Possui CLEAR
			            AND A.[Fl_Tipo] = 1		-- ALERTA
	 
	            IF (@@ROWCOUNT = 0)
	            BEGIN
		            INSERT INTO [dbo].[CheckAlertaSemClear] ( [Nm_Alerta], [Ds_Mensagem], [Dt_Alerta], [Run_Duration] )
		            SELECT 'Sem registro de Alerta sem CLEAR', NULL, NULL, NULL
	            END
	            ";
        }
    }
}
