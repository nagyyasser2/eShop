using eShop.Core.Configurations;
using eShop.Core.Services.Abstractions;
using MimeKit;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Options;

namespace eShop.Core.Services.Implementations
{
    public class EmailSender : IEmailSender
    {
        private readonly IOptions<EmailConfiguration> _emailConfig;

        public EmailSender(IOptions<EmailConfiguration> emailConfig)
        {
            this._emailConfig = emailConfig;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string htmlContent)
        {
            var emailMessage = new MimeMessage();
            emailMessage.From.Add(new MailboxAddress(_emailConfig.Value.SenderName, _emailConfig.Value.SenderEmail));
            emailMessage.To.Add(new MailboxAddress("", toEmail));
            emailMessage.Subject = subject;
            emailMessage.Body = new TextPart(MimeKit.Text.TextFormat.Html) { Text = htmlContent };

            using var client = new SmtpClient();
            try
            {
                await client.ConnectAsync(_emailConfig.Value.SmtpServer, _emailConfig.Value.Port, MailKit.Security.SecureSocketOptions.StartTls);
                client.AuthenticationMechanisms.Remove("XOAUTH2");
                await client.AuthenticateAsync(_emailConfig.Value.SenderEmail, _emailConfig.Value.Password);
                await client.SendAsync(emailMessage);
            }
            catch (Exception ex)
            {
                // Log the exception (e.g., using ILogger)
                throw new Exception($"Failed to send email: {ex.Message}", ex);
            }
            finally
            {
                await client.DisconnectAsync(true);
            }
        }
    }
}
