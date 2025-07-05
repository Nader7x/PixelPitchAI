using System.Net.Mail;
using System.Threading.Tasks;
using Application.Interfaces;
using Application.Services;
using Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace Footex.UnitTests.Infrastructure.Services
{
    public class EmailServiceTests
    {
        private readonly Mock<IConfiguration> _configurationMock;
        private readonly Mock<IConfigurationSection> _smtpSettingsSectionMock;
        private readonly Mock<ISmtpClient> _smtpClientMock;
        private readonly EmailService _emailService;

        public EmailServiceTests()
        {
            _configurationMock = new Mock<IConfiguration>();
            _smtpSettingsSectionMock = new Mock<IConfigurationSection>();
            _smtpClientMock = new Mock<ISmtpClient>();

            _smtpSettingsSectionMock.Setup(s => s["Host"]).Returns("smtp.test.com");
            _smtpSettingsSectionMock.Setup(s => s["Port"]).Returns("587");
            _smtpSettingsSectionMock.Setup(s => s["UserName"]).Returns("testuser");
            _smtpSettingsSectionMock.Setup(s => s["Password"]).Returns("testpass");
            _smtpSettingsSectionMock.Setup(s => s["FromEmail"]).Returns("from@test.com");

            _configurationMock
                .Setup(c => c.GetSection("SmtpSettings"))
                .Returns(_smtpSettingsSectionMock.Object);

            _emailService = new EmailService(_smtpClientMock.Object, _configurationMock.Object);
        }

        [Fact]
        public async Task SendEmailAsync_ShouldSendEmailWithCorrectParameters()
        {
            // Arrange
            var to = "to@test.com";
            var subject = "Test Subject";
            var htmlMessage = "<h1>Test Message</h1>";

            // Act
            await _emailService.SendEmailAsync(to, subject, htmlMessage);

            // Assert
            _smtpClientMock.Verify(
                x =>
                    x.SendMailAsync(
                        It.Is<MailMessage>(m =>
                            m.To.ToString() == to
                            && m.Subject == subject
                            && m.Body == htmlMessage
                            && m.From != null
                            && m.From.Address == "from@test.com"
                        )
                    ),
                Times.Once
            );
        }
    }
}
