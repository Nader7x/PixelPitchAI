using System.Net.Mail;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface ISmtpClient
    {
        Task SendMailAsync(MailMessage message);
    }
}
