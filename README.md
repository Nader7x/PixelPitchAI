# Footex
Football Matches Simulation Platform
# Email Setup with FluentEmail and PaperCut

## Local Development

For local development, the application uses [PaperCut](https://github.com/ChangemakerStudios/Papercut-SMTP) as a local SMTP server. PaperCut captures all outgoing emails and displays them in a user interface without actually sending them.

### Setup Instructions:

1. Install PaperCut:
   - Download from: https://github.com/ChangemakerStudios/Papercut-SMTP/releases
   - Run the installer and start PaperCut

2. Configure your application:
   - No additional configuration is needed for development
   - The application is already set up to use localhost:25 (PaperCut's default port)

3. Testing emails:
   - When you trigger an email in your application (password reset, email confirmation, etc.)
   - Open the PaperCut UI to see the captured email
   - No actual emails are sent out

## Production Environment

For production, update the SMTP settings in appsettings.json: