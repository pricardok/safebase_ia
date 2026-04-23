using System;
using System.Collections.Generic;
using System.Text;
using SafeBase_Installer.Core;

namespace SafeBase_Installer
{
    class CreateFncDB
    {
        public static string Query(string use)
        {
            return
            @"
            USE "+ use + @"
            GO

            -- fncRetiraCaractereInvalidoXML
            CREATE FUNCTION [dbo].[fncRetiraCaractereInvalidoXML] (@Text VARCHAR(MAX))
            RETURNS VARCHAR(MAX)
            AS

            BEGIN
	            DECLARE @Result NVARCHAR(4000)
	            SELECT @Result = REPLACE(REPLACE(REPLACE(REPLACE(REPLACE
							            (REPLACE(REPLACE(REPLACE(REPLACE(REPLACE
									            (REPLACE(REPLACE(REPLACE(REPLACE(REPLACE
											            (REPLACE(REPLACE(REPLACE(REPLACE(REPLACE
													            (REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE( 
																													            @Text
													             ,NCHAR(1),N'?'),NCHAR(2),N'?'),NCHAR(3),N'?'),NCHAR(4),N'?'),NCHAR(5),N'?'),NCHAR(6),N'?')
											             ,NCHAR(7),N'?'),NCHAR(8),N'?'),NCHAR(11),N'?'),NCHAR(12),N'?'),NCHAR(14),N'?'),NCHAR(15),N'?')
									             ,NCHAR(16),N'?'),NCHAR(17),N'?'),NCHAR(18),N'?'),NCHAR(19),N'?'),NCHAR(20),N'?'),NCHAR(21),N'?')
							             ,NCHAR(22),N'?'),NCHAR(23),N'?'),NCHAR(24),N'?'),NCHAR(25),N'?'),NCHAR(26),N'?'),NCHAR(27),N'?')
						             ,NCHAR(28),N'?'),NCHAR(29),N'?'),NCHAR(30),N'?'),NCHAR(31),N'?');

	            RETURN @Result
            END
   			GO
            -- fncCaractereInvalidoXML
            CREATE FUNCTION [dbo].[fncCaractereInvalidoXML] (
	            @Text VARCHAR(MAX)
            )
            RETURNS VARCHAR(MAX)
            AS
            BEGIN
	            DECLARE @Rest NVARCHAR(4000)

	            SELECT @Rest = REPLACE(REPLACE(REPLACE(REPLACE(REPLACE
							            (REPLACE(REPLACE(REPLACE(REPLACE(REPLACE
									            (REPLACE(REPLACE(REPLACE(REPLACE(REPLACE
											            (REPLACE(REPLACE(REPLACE(REPLACE(REPLACE
													            (REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE( 
																													            @Text
													             ,NCHAR(1),N'?'),NCHAR(2),N'?'),NCHAR(3),N'?'),NCHAR(4),N'?'),NCHAR(5),N'?'),NCHAR(6),N'?')
											             ,NCHAR(7),N'?'),NCHAR(8),N'?'),NCHAR(11),N'?'),NCHAR(12),N'?'),NCHAR(14),N'?'),NCHAR(15),N'?')
									             ,NCHAR(16),N'?'),NCHAR(17),N'?'),NCHAR(18),N'?'),NCHAR(19),N'?'),NCHAR(20),N'?'),NCHAR(21),N'?')
							             ,NCHAR(22),N'?'),NCHAR(23),N'?'),NCHAR(24),N'?'),NCHAR(25),N'?'),NCHAR(26),N'?'),NCHAR(27),N'?')
						             ,NCHAR(28),N'?'),NCHAR(29),N'?'),NCHAR(30),N'?'),NCHAR(31),N'?');

	            RETURN @Rest
            END
            GO

            -- fnParseStringUdf
            CREATE FUNCTION [dbo].[fnParseStringUdf]
            (
                      @stringToParse VARCHAR(8000)  
                    , @delimiter     CHAR(1)
            )
            RETURNS @parsedString TABLE (stringValue VARCHAR(128))
            AS
            /*********************************************************************************
            Usage: 		
                SELECT *
                FROM fnParseStringUdf(<string>, <delimiter>);
 
            Test Cases:
 
                1.  multiple strings separated by space
                    SELECT * FROM dbo.fnParseStringUdf('  aaa  bbb  ccc ', ' ');
 
                2.  multiple strings separated by comma
                    SELECT * FROM dbo.fnParseStringUdf(',aaa,bbb,,,ccc,', ',');
            *********************************************************************************/
            BEGIN
 
                /* Declare variables */
                DECLARE @trimmedString  VARCHAR(8000);
 
                /* We need to trim our string input in case the user entered extra spaces */
                SET @trimmedString = LTRIM(RTRIM(@stringToParse));
 
                /* Let's create a recursive CTE to break down our string for us */
                WITH parseCTE (StartPos, EndPos)
                AS
                (
                    SELECT 1 AS StartPos
                        , CHARINDEX(@delimiter, @trimmedString + @delimiter) AS EndPos
                    UNION ALL
                    SELECT EndPos + 1 AS StartPos
                        , CHARINDEX(@delimiter, @trimmedString + @delimiter , EndPos + 1) AS EndPos
                    FROM parseCTE
                    WHERE CHARINDEX(@delimiter, @trimmedString + @delimiter, EndPos + 1) <> 0
                )
 
                /* Let's take the results and stick it in a table */  
                INSERT INTO @parsedString
                SELECT SUBSTRING(@trimmedString, StartPos, EndPos - StartPos)
                FROM parseCTE
                WHERE LEN(LTRIM(RTRIM(SUBSTRING(@trimmedString, StartPos, EndPos - StartPos)))) > 0
                OPTION (MaxRecursion 8000);
 
                RETURN;   
            END
            
            GO
            CREATE FUNCTION fncAgPrimary  (	
                @DbPrimary sysname 
            )RETURNS INT
            WITH ENCRYPTION
            AS
            BEGIN                
            	RETURN (
            			SELECT 
            				COUNT(1) AS ContDB			
            			FROM sys.databases db			
                        LEFT JOIN [SafeBase].[dbo].[vwCheckAG] ag				
            				ON db.name = ag.database_name			
            			WHERE 				
            				(				 
            				 (db.replica_id is null and name = @DbPrimary) 						
            				    OR				 
            				 (ag.is_local = 1 AND ag.is_primary_replica = 1 AND ag.database_name = @DbPrimary)				
            				)			
            			)  
            END
            GO

            ";

        }
    }
}
