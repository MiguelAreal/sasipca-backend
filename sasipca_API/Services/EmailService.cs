using System.IO;
using System.Text;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Microsoft.Extensions.Configuration;

namespace sasipca_API.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(string emailAddress, string subject, string templateName, Dictionary<string, string> placeholders);
    }

    public class EmailService : IEmailService
    {
        private readonly string _smtpServer;
        private readonly int _smtpPort;
        private readonly string _smtpUsername;
        private readonly string _smtpPassword;

        public EmailService(IConfiguration config)
        {
            _smtpServer = Environment.GetEnvironmentVariable("SMTP_SERVER");
            _smtpPort = Convert.ToInt32(Environment.GetEnvironmentVariable("SMTP_PORT"));
            _smtpUsername = Environment.GetEnvironmentVariable("SMTP_USERNAME");
            _smtpPassword = Environment.GetEnvironmentVariable("SMTP_PASSWORD");
        }

        public async Task SendEmailAsync(string emailAddress, string subject, string templateName, Dictionary<string, string> placeholders)
        {
            // Carrega o template HTML
            var templatePath = Path.Combine(Directory.GetCurrentDirectory(), "Templates", $"{templateName}.html");
            string body = await File.ReadAllTextAsync(templatePath, Encoding.UTF8);

            // Substitui os placeholders
            foreach (var placeholder in placeholders)
            {
                body = body.Replace($"{{{placeholder.Key}}}", placeholder.Value);
            }

            var emailMessage = new MimeMessage();
            emailMessage.From.Add(new MailboxAddress("NeighbourLink", _smtpUsername));
            emailMessage.To.Add(new MailboxAddress("", emailAddress));
            emailMessage.Subject = subject;

            // Define o corpo do e-mail como HTML
            emailMessage.Body = new TextPart("html")
            {
                Text = body
            };

            using (var client = new SmtpClient())
            {
                await client.ConnectAsync(_smtpServer, _smtpPort, SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(_smtpUsername, _smtpPassword);
                await client.SendAsync(emailMessage);
                await client.DisconnectAsync(true);
            }
        }
    }
}
