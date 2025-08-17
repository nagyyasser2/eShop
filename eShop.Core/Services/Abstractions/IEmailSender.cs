namespace eShop.Core.Services.Abstractions
{
    public interface IEmailSender
    {
        Task SendEmailAsync(string toEmail, string subject, string htmlContent);
    }
}
