using Application.Services;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Mail;

namespace Infrastructure.Services;

public class EmailService(IConfiguration configuration) : IEmailService
{
    public async Task SendEmailAsync(string to, string subject, string htmlMessage)
    {
        var smtpSettings = configuration.GetSection("SmtpSettings");
        var host = smtpSettings["Host"];
        var port = int.Parse(smtpSettings["Port"]);
        var userName = smtpSettings["UserName"];
        var password = smtpSettings["Password"];
        var from = smtpSettings["FromEmail"];

        using var client = new SmtpClient(host, port);
        client.EnableSsl = true;
        client.Credentials = new NetworkCredential(userName, password);

        var mailMessage = new MailMessage
        {
            From = new MailAddress(from),
            Subject = subject,
            Body = htmlMessage,
            IsBodyHtml = true
        };

        mailMessage.To.Add(to);

        await client.SendMailAsync(mailMessage);
    }
}
