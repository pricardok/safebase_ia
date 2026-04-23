using System;
using System.Collections.Generic;
using System.Text;

namespace InitDB.Client
{
    class stpSendNotification
    {
        public static string Query(int Id_AlertaParametro, string ProfileDBMail, string mailDestination, string BodyFormatMail, string Subject, string Importance, string HTML, string MntMsg, string CanalTelegram, string Ds_Menssageiro_02, string Teams) // public static string Query(string use)
        {
            return
            @"  
             DECLARE @Id_AlertaParametro INT = " + Id_AlertaParametro + @"		    -- Numero do Id de Alerta ([dbo].[AlertaParametro])
	                ,@ProfileDBMail VARCHAR(50) = " + ProfileDBMail + @"		    -- Nome do profile de envio
	                ,@EmailDestination VARCHAR(1000) = " + mailDestination + @"	    -- Destinatarios de email 
	                ,@BodyFormatMail VARCHAR(20) = " + BodyFormatMail + @"		    -- Formato do email (html)
	                ,@Subject VARCHAR(600) = " + Subject + @"			            -- Assunto do email
	                ,@Importance AS VARCHAR(6) = " + Importance + @"		        -- Importancia do email (High)
	                ,@HTML VARCHAR(MAX)	= " + HTML + @"				                -- Corpo do email
	                ,@MntMsg VARCHAR(200) = " + MntMsg + @"				            -- Menssagem (Email, Telegram, Teamns)
	                ,@CanalTelegram VARCHAR(100) = " + CanalTelegram + @"		    -- Canal do Telegram
	                ,@Ds_Menssageiro_02 VARCHAR (30) = " + Ds_Menssageiro_02 + @"	-- Canal do Teamns
	                ,@Teams INT	= " + Teams + @"						            -- Grupo de envio do Teamns

            BEGIN
                /*******************************************************************************************************************************
                --	ALERTA - ENVIA O EMAIL E MENSSAGEIROS
                *******************************************************************************************************************************/
                IF EXISTS  (SELECT B.Ativo from AlertaParametro A 
			                    INNER JOIN [dbo].[AlertaEnvio] B ON B.IdAlertaParametro = A.Id_AlertaParametro
			                    WHERE B.Ativo = 1
			                    AND B.Des LIKE '%Email'
			                    AND [Id_AlertaParametro] = @Id_AlertaParametro
			                )
                BEGIN

                    EXEC [msdb].[dbo].[sp_send_dbmail]
                            @profile_name = @ProfileDBMail,
                            @recipients = @EmailDestination,
                            @body_format = @BodyFormatMail,
                            @subject = @Subject,
                            @importance = @Importance,
                            @body = @HTML;

                END

	            -- Parametro Menssageiro
                SET @MntMsg = @Subject+', Verifique os detalhes no *E-Mail*'

                IF EXISTS  (SELECT B.Ativo from AlertaParametro A 
			                INNER JOIN [dbo].[AlertaEnvio] B ON B.IdAlertaParametro = A.Id_AlertaParametro
			                WHERE B.Ativo = 1
			                    AND B.Des LIKE '%Telegram'
			                    AND [Id_AlertaParametro] = @Id_AlertaParametro
			                )
                BEGIN
                    -- Envio do Telegram    
                    EXEC dbo.StpSendMsgTelegram 
                            @Destino = @CanalTelegram,
                            @Msg = @MntMsg
                END

                IF EXISTS  (SELECT B.Ativo from AlertaParametro A 
			                INNER JOIN [dbo].[AlertaEnvio] B ON B.IdAlertaParametro = A.Id_AlertaParametro
			                WHERE B.Ativo = 1
			                    AND B.Des LIKE '%Teams'
			                    AND [Id_AlertaParametro] = @Id_AlertaParametro
			                )
                BEGIN
                    -- MS TEAMS
                    SET @MntMsg = (select replace (@MntMsg, '\', '-'))
                    EXEC [dbo].[stpSendMsgTeams]
	                        @msg = @MntMsg,
	                        @channel = @Ds_Menssageiro_02,
                            @ap = @Teams
                END
            END

            ";

        }
    }
}
