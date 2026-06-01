using System.Net.Mail;

namespace Application.Interfaces;

public interface ISmtpClient
{
    Task SendMailAsync(MailMessage message);
}
