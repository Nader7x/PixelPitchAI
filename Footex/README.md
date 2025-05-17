# Email Setup for Development

## PaperCut for Local Email Testing

This project uses [PaperCut](https://github.com/ChangemakerStudios/Papercut-SMTP) for local email testing. PaperCut is a simple desktop SMTP server designed to receive and notify of emails sent to any user at any domain. Emails are stored to disk as .eml files which can be opened with many email clients.

### Setup Instructions:

1. Install PaperCut from https://github.com/ChangemakerStudios/Papercut-SMTP/releases
2. Run PaperCut before starting the application
3. PaperCut will listen on localhost:25 by default
4. Any emails sent by the application will be captured by PaperCut

## Email in Production

For production environments, the application uses FluentEmail with SMTP configuration from appsettings.json. Update the SmtpSettings section with your production email provider details:

```json
{
  "SmtpSettings": {
    "Host": "smtp.example.com",
    "Port": 587,
    "EnableSSL": true,
    "UserName": "your-email@example.com",
    "Password": "your-smtp-password",
    "FromEmail": "no-reply@yourapp.com"
  }
}
