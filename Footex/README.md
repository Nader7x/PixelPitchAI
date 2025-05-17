# Footex

## Project Structure

The Footex project is organized into several key components:

- **Controllers**: Handle HTTP requests and responses.
  - `Footex/Controllers/AuthController.cs`
  - `Footex/Controllers/CoachesController.cs`
  - `Footex/Controllers/MatchesController.cs`
  - `Footex/Controllers/PlayersController.cs`
  - `Footex/Controllers/SeasonsController.cs`
  - `Footex/Controllers/StadiumsController.cs`
  - `Footex/Controllers/TeamsController.cs`
  - `Footex/Controllers/SearchController.cs`

- **CQRS Pattern**: Commands and queries for handling business logic.
  - `Application/CQRS/Auth`
  - `Application/CQRS/Coaches`
  - `Application/CQRS/Matches`
  - `Application/CQRS/Players`
  - `Application/CQRS/Seasons`
  - `Application/CQRS/Stadiums`
  - `Application/CQRS/Teams`

- **Dtos**: Data transfer objects for various entities.
  - `Application/Dtos`

- **MediatR**: Used for handling commands and queries.

- **Mapping Classes**: Map entities to Dtos and vice versa.
  - `Application/Mappers`

- **Azure Blob Storage**: Handling images.
  - `Application/Services/AzureBlobStorageService.cs`

- **Configurations**: Configuration classes for various entities.
  - `Infrastructure/Configurations`

- **Repositories**: Data access layer for various entities.
  - `Infrastructure/Repositories`

- **Services**: Various services for handling business logic.
  - `Infrastructure/Services`

- **Database Context**: Entity Framework Core database context.
  - `Infrastructure/FootballDbContext.cs`

- **Migrations**: Database schema changes.
  - `Infrastructure/Migrations`

- **Solution File**: Visual Studio solution file.
  - `Footex.sln`

## Features

The main features of the Footex project are:

- **User authentication and authorization**: User registration, login, email confirmation, password reset, and token management.
- **Coach management**: Creating, updating, deleting, and retrieving coaches.
- **Match management**: Creating, updating, deleting, and retrieving matches.
- **Player management**: Creating, updating, deleting, and retrieving players.
- **Season management**: Creating, updating, deleting, and retrieving seasons.
- **Stadium management**: Creating, updating, deleting, and retrieving stadiums.
- **Team management**: Creating, updating, deleting, and retrieving teams.
- **Search functionality**: Search across teams, matches, players, and coaches.
- **File storage**: File storage functionalities using Azure Blob Storage.
- **Token management**: Creating and managing JWT tokens and refresh tokens.
- **Logging**: Using Serilog for logging.

## Setup and Running the Project

### Dependencies

- .NET 8.0 SDK
- Azure Storage Blobs
- MediatR
- Microsoft.AspNetCore.Authentication.JwtBearer
- Microsoft.EntityFrameworkCore
- Serilog

### Environment Variables

Set the following environment variables:

- `ConnectionStrings__DefaultConnection`
- `ConnectionStrings__BlobConnection`
- `JWT__ValidIssuer`
- `JWT__ValidAudience`
- `JWT__Secret`
- `AdminUser__Email`
- `AdminUser__Password`
- `SmtpSettings__Host`
- `SmtpSettings__Port`
- `SmtpSettings__UserName`
- `SmtpSettings__Password`
- `SmtpSettings__FromEmail`
- `RabbitMQ__HostName`
- `RabbitMQ__UserName`
- `RabbitMQ__Password`
- `RabbitMQ__VirtualHost`
- `RabbitMQ__Port`
- `RabbitMQ__QueueName`

### Database Migrations

To apply database migrations, run the following command:

```bash
dotnet ef database update
```

## Testing Strategy

The project includes the following testing strategies:

- **Unit Tests**: Testing individual components in isolation.
- **Integration Tests**: Testing the interaction between multiple components.
- **End-to-End Tests**: Testing the entire application flow from start to finish.

## Deployment Process

The project includes a CI/CD pipeline for automated deployment. The hosting environment is configured to use Docker containers for deployment.

### CI/CD Pipeline

The CI/CD pipeline includes the following steps:

1. Build the application.
2. Run unit tests.
3. Run integration tests.
4. Deploy the application to the hosting environment.

### Hosting Environment

The application is hosted in a Docker container. The Dockerfile is included in the project.

## Email Setup

### SMTP for Email

For email functionality, the application uses SMTP configuration from appsettings.json. Update the SmtpSettings section with your email provider details:

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
```
