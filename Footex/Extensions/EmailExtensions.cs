using System.Net;
using System.Net.Mail;

namespace Footex.Extensions;

public static class EmailExtensions
{
    public static IServiceCollection ConfigureEmailServices(this IServiceCollection services,
        IConfiguration configuration, IWebHostEnvironment environment)
    {
        // Configure FluentEmail
        if (environment.IsDevelopment())
        {
            // For development, use PaperCut which runs on localhost:25 by default
            var smtpClient = new SmtpClient
            {
                Host = "localhost",
                Port = 25,
                EnableSsl = false,
                DeliveryMethod = SmtpDeliveryMethod.Network
            };

            services
                .AddFluentEmail("no-reply@footex.com")
                .AddSmtpSender(smtpClient);

            // Log info about PaperCut
            var logger = services.BuildServiceProvider().GetRequiredService<ILogger<Program>>();
            logger.LogInformation("Email configured to use local PaperCut SMTP server on localhost:25");
            logger.LogInformation("Make sure PaperCut is running to receive emails during development");
        }
        else
        {
            // For production, use SMTP settings from configuration
            var smtpSettings = configuration.GetSection("SmtpSettings");
            var host = smtpSettings["Host"];
            var port = int.Parse(smtpSettings["Port"]);
            var enableSsl = bool.Parse(smtpSettings["EnableSSL"]);
            var userName = smtpSettings["UserName"];
            var password = smtpSettings["Password"];
            var fromEmail = smtpSettings["FromEmail"];

            // Create SMTP client with credentials
            var smtpClient = new SmtpClient
            {
                Host = host,
                Port = port,
                EnableSsl = enableSsl,
                DeliveryMethod = SmtpDeliveryMethod.Network
            };

            // Add credentials if provided
            if (!string.IsNullOrEmpty(userName) && !string.IsNullOrEmpty(password))
                smtpClient.Credentials = new NetworkCredential(userName, password);

            services
                .AddFluentEmail(fromEmail)
                .AddSmtpSender(smtpClient);
        }

        return services;
    }
}