using System.Net;
using System.Net.Mail;
using Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.Services;

public class SmtpClientWrapper : ISmtpClient
{
    private readonly SmtpClient _smtpClient;

    public SmtpClientWrapper(IConfiguration configuration)
    {
        var smtpSettings = configuration.GetSection("SmtpSettings");
        var host = smtpSettings["Host"];
        var port = int.Parse(smtpSettings["Port"]);
        var userName = smtpSettings["UserName"];
        var password = smtpSettings["Password"];

        _smtpClient = new SmtpClient(host, port)
        {
            Credentials = new NetworkCredential(userName, password),
            EnableSsl = true,
        };
    }

    public Task SendMailAsync(MailMessage message)
    {
        return _smtpClient.SendMailAsync(message);
    }
}
