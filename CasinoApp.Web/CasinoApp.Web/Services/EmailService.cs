using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace CasinoApp.Web.Services
{
    public class EmailService
    {
        public async Task SendPasswordResetEmail(string toEmail, string resetLink)
        {
            var email = new MimeMessage();

            email.From.Add(new MailboxAddress("EpicSpin Casino", "epicspin.suport@gmail.com"));
            email.To.Add(MailboxAddress.Parse(toEmail));
            email.Subject = "Resetare parolă EpicSpin";

            email.Body = new TextPart("html")
            {
                Text = $@"
                    <h2>Resetare parolă</h2>
                    <p>Ai cerut resetarea parolei pentru contul tău.</p>
                    <p>Apasă pe link-ul de mai jos:</p>
                    <p><a href='{resetLink}'>Resetează parola</a></p>
                    <p>Link-ul expiră în 30 de minute.</p>
                "
            };

            using var smtp = new SmtpClient();
            smtp.Timeout = 10000;

            await smtp.ConnectAsync(
                "smtp-relay.brevo.com",
                2525,
                SecureSocketOptions.StartTls
            );

            await smtp.AuthenticateAsync(
                "aa9321001@smtp-brevo.com",
                "bskGVH0l6vh7nxr"
            );

            await smtp.SendAsync(email);
            await smtp.DisconnectAsync(true);
        }
    }
}