using System.Net.Mail;
using Application.Interfaces;
using Application.Services;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ISmtpClient _smtpClient;

    public EmailService(ISmtpClient smtpClient, IConfiguration configuration)
    {
        _smtpClient = smtpClient;
        _configuration = configuration;
    }

    public async Task SendEmailAsync(string to, string subject, string htmlMessage)
    {
        var smtpSettings = _configuration.GetSection("SmtpSettings");
        var from = smtpSettings["FromEmail"];

        var mailMessage = new MailMessage
        {
            From = new MailAddress(from),
            Subject = subject,
            Body = htmlMessage,
            IsBodyHtml = true,
        };

        mailMessage.To.Add(to);

        await _smtpClient.SendMailAsync(mailMessage);
    }
}
