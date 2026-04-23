using System;
using System.Collections.Generic;
using System.Text;

namespace InitDB.Client
{
    class stpCheckTableGrowth
    {
        public static string Query()
        {
            return
            // @"insert into [dbo].[Testedb] ([Nome],[DateTest]) values ('Teste da ferramenta DB - stpCheckTableGrowth',GETDATE())";
            @"  SET NOCOUNT ON
                
	            -- Declara e seta AS variaveis das datas - Tratamento para os casos que ainda não atingiram 60 dias no histórico
	            DECLARE @Dt_Hoje DATE, @Dt_1Dia DATE, @Dt_15Dias DATE, @Dt_30Dias DATE, @Dt_60Dias DATE
	
	            SELECT	@Dt_Hoje = CAST(GETDATE() AS DATE)
	
	            SELECT	@Dt_1Dia   = MIN((CASE WHEN DATEDIFF(DAY,A.[Dt_Referencia], @Dt_Hoje) <= 1  THEN A.[Dt_Referencia] END)),
			            @Dt_15Dias = MIN((CASE WHEN DATEDIFF(DAY,A.[Dt_Referencia], @Dt_Hoje) <= 15 THEN A.[Dt_Referencia] END)),
			            @Dt_30Dias = MIN((CASE WHEN DATEDIFF(DAY,A.[Dt_Referencia], @Dt_Hoje) <= 30 THEN A.[Dt_Referencia] END)),
			            @Dt_60Dias = MIN((CASE WHEN DATEDIFF(DAY,A.[Dt_Referencia], @Dt_Hoje) <= 60 THEN A.[Dt_Referencia] END))
	            FROM [dbo].[HistoricoTamanhoTabela] A
		            JOIN [dbo].[Servidor] B ON A.[Id_Servidor] = B.[Id_Servidor] 
		            JOIN [dbo].[Tabela] C ON A.[Id_Tabela] = C.[Id_Tabela]
		            JOIN [dbo].[BaseDados] D ON A.[Id_BaseDados] = D.[Id_BaseDados]
	            WHERE 	DATEDIFF(DAY,A.[Dt_Referencia], CAST(GETDATE() AS DATE)) <= 60
		            AND B.Nm_Servidor = @@SERVERNAME
	
	            /*
	            -- P/ TESTE
	            SELECT @Dt_Hoje Dt_Hoje, @Dt_1Dia Dt_1Dia, @Dt_15Dias Dt_15Dias, @Dt_30Dias Dt_30Dias, @Dt_60Dias Dt_60Dias
	
	            SELECT	CONVERT(VARCHAR, GETDATE() ,112) Hoje, CONVERT(VARCHAR, GETDATE()-1 ,112) [1Dia], CONVERT(VARCHAR, GETDATE()-15 ,112) [15Dias],
			            CONVERT(VARCHAR, GETDATE()-30 ,112) [30Dias], CONVERT(VARCHAR, GETDATE()-60 ,112) [60Dias]
	            */

	            -- Tamanho atual das DATABASES de todos os servidores e crescimento em 1, 15, 30 e 60 dias.
	            IF (OBJECT_ID('tempdb..#CheckTableGrowth') IS NOT NULL)
		            DROP TABLE #CheckTableGrowth
	
	            CREATE TABLE #CheckTableGrowth (
		            [Nm_Servidor]	VARCHAR(50) NOT NULL,
		            [Nm_Database]	VARCHAR(100) NULL,
		            [Nm_Tabela]		VARCHAR(100) NULL,
		            [Tamanho_Atual] NUMERIC(38, 2) NULL,
		            [Cresc_1_dia]	NUMERIC(38, 2) NULL,
		            [Cresc_15_dia]	NUMERIC(38, 2) NULL,
		            [Cresc_30_dia]	NUMERIC(38, 2) NULL,
		            [Cresc_60_dia]	NUMERIC(38, 2) NULL		
	            )
		
	            INSERT INTO #CheckTableGrowth
	            SELECT	B.[Nm_Servidor], [Nm_Database], [Nm_Tabela], 
			            SUM(CASE WHEN [Dt_Referencia] = @Dt_Hoje   THEN A.[Nr_Tamanho_Total] ELSE 0 END) AS [Tamanho_Atual],
			            SUM(CASE WHEN [Dt_Referencia] = @Dt_1Dia   THEN A.[Nr_Tamanho_Total] ELSE 0 END) AS [Cresc_1_dia],
			            SUM(CASE WHEN [Dt_Referencia] = @Dt_15Dias THEN A.[Nr_Tamanho_Total] ELSE 0 END) AS [Cresc_15_dia],
			            SUM(CASE WHEN [Dt_Referencia] = @Dt_30Dias THEN A.[Nr_Tamanho_Total] ELSE 0 END) AS [Cresc_30_dia],
			            SUM(CASE WHEN [Dt_Referencia] = @Dt_60Dias THEN A.[Nr_Tamanho_Total] ELSE 0 END) AS [Cresc_60_dia]           
	            FROM [dbo].[HistoricoTamanhoTabela] A
		            JOIN [dbo].[Servidor] B ON A.[Id_Servidor] = B.[Id_Servidor] 
		            JOIN [dbo].[Tabela] C ON A.[Id_Tabela] = C.[Id_Tabela]
		            JOIN [dbo].[BaseDados] D ON A.[Id_BaseDados] = D.[Id_BaseDados]
	            WHERE 	A.[Dt_Referencia] IN( @Dt_Hoje, @Dt_1Dia, @Dt_15Dias, @Dt_30Dias, @Dt_60Dias) -- Hoje, 1 dia, 15 dias, 30 dias, 60 dias
		            AND B.Nm_Servidor = @@SERVERNAME
	            GROUP BY B.[Nm_Servidor], [Nm_Database], [Nm_Tabela]
			
	            TRUNCATE TABLE [dbo].[CheckTableGrowth]
	            TRUNCATE TABLE [dbo].[CheckTableGrowthEmail]
			
	            INSERT INTO [dbo].[CheckTableGrowth] ( [Nm_Servidor], [Nm_Database], [Nm_Tabela], [Tamanho_Atual], [Cresc_1_dia], [Cresc_15_dia], [Cresc_30_dia], [Cresc_60_dia] )
	            SELECT	[Nm_Servidor], [Nm_Database], [Nm_Tabela], [Tamanho_Atual], 
			            [Tamanho_Atual] - [Cresc_1_dia] AS [Cresc_1_dia],
			            [Tamanho_Atual] - [Cresc_15_dia] AS [Cresc_15_dia],
			            [Tamanho_Atual] - [Cresc_30_dia] AS [Cresc_30_dia],
			            [Tamanho_Atual] - [Cresc_60_dia] AS [Cresc_60_dia]
	            FROM #CheckTableGrowth
	
	            IF (@@ROWCOUNT <> 0)
	            BEGIN
		            INSERT INTO [dbo].[CheckTableGrowthEmail] ( [Nm_Servidor], [Nm_Database], [Nm_Tabela], [Tamanho_Atual], [Cresc_1_dia], [Cresc_15_dia], [Cresc_30_dia], [Cresc_60_dia] )
		            SELECT	TOP 10
				            [Nm_Servidor], [Nm_Database], [Nm_Tabela], [Tamanho_Atual], [Cresc_1_dia], [Cresc_15_dia], [Cresc_30_dia], [Cresc_60_dia]
		            FROM [dbo].[CheckTableGrowth]
		            ORDER BY ABS([Cresc_1_dia]) DESC, ABS([Cresc_15_dia]) DESC, ABS([Cresc_30_dia]) DESC, ABS([Cresc_60_dia]) DESC, [Tamanho_Atual] DESC
	
		            INSERT INTO [dbo].[CheckTableGrowthEmail] ( [Nm_Servidor], [Nm_Database], [Nm_Tabela], [Tamanho_Atual], [Cresc_1_dia], [Cresc_15_dia], [Cresc_30_dia], [Cresc_60_dia] )
		            SELECT NULL, 'TOTAL GERAL', NULL, SUM([Tamanho_Atual]), SUM([Cresc_1_dia]), SUM([Cresc_15_dia]), SUM([Cresc_30_dia]), SUM([Cresc_60_dia])
		            FROM [dbo].[CheckTableGrowth]
	            END
	            ELSE
	            BEGIN
		            INSERT INTO [dbo].[CheckTableGrowthEmail] ( [Nm_Servidor], [Nm_Database], [Nm_Tabela], [Tamanho_Atual], [Cresc_1_dia], [Cresc_15_dia], [Cresc_30_dia], [Cresc_60_dia] )
		            SELECT NULL, 'Sem registro de Crescimento de mais de 1 MB das Tabelas', NULL, NULL, NULL, NULL, NULL, NULL
	            END
                ";

        }
    }
}
