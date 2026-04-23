using System.Data.SqlTypes;
using System.Net;
using System.Net.Mail;

public partial class StoredProcedures
{
    [Microsoft.SqlServer.Server.SqlProcedure]
    public static void stpSendMsgMail(SqlString destinatarios, SqlString assunto, SqlString mensagem, SqlString arquivos)
    {

        const string smtpEndereco = "smtp.office365.com";
        const int smtpPorta = 587; // porta
        const int smtpTimeout = 60000; // 60 segundos
        const bool smtpUsaCredenciaisPadrao = false;
        const bool smtpUsaSsl = true;
        const string smtpUsuario = ""; 
        const string smtpSenha = "";


        using (var clienteSmtp = new SmtpClient(smtpEndereco, smtpPorta) { DeliveryMethod = SmtpDeliveryMethod.Network, Timeout = smtpTimeout, UseDefaultCredentials = smtpUsaCredenciaisPadrao, EnableSsl = smtpUsaSsl })
        {

            if (!string.IsNullOrEmpty(smtpUsuario))
                clienteSmtp.Credentials = new NetworkCredential(smtpUsuario, smtpSenha);

            using (var eMail = new MailMessage())
            {

                var emailOrigem = new MailAddress(smtpUsuario);

                eMail.From = emailOrigem;

                foreach (var destinatario in destinatarios.Value.Split(';'))
                {
                    if (!string.IsNullOrEmpty(destinatario))
                        eMail.To.Add(destinatario);
                }

                foreach (var arquivo in arquivos.Value.Split(';'))
                {
                    if (!string.IsNullOrEmpty(arquivo))
                        eMail.Attachments.Add(new Attachment(arquivo));
                }

                eMail.Subject = assunto.Value;
                eMail.IsBodyHtml = true;
                eMail.Body = (string.IsNullOrEmpty(mensagem.Value)) ? "" : mensagem.Value;

                clienteSmtp.Send(eMail);

            }

        }

    }

}