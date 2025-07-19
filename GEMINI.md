# GEMINI Code RAG

This document provides a comprehensive overview of the Footex project, an enterprise-level football management platform. It is intended to be used by developers to quickly understand the project's architecture, technologies, and development workflows.

## Project Overview

Footex is a football management API built with .NET 8. It provides a comprehensive set of features for managing football teams, players, coaches, and matches. The platform also includes real-time match simulation, advanced analytics, and a production-ready CI/CD pipeline.

## Architecture

The project follows the principles of Clean Architecture, with a clear separation of concerns between the different layers of the application. The main layers are:

- **Domain**: Contains the core business entities, interfaces, and logic of the application.
- **Application**: Implements the business logic of the application, including CQRS commands and queries, DTOs, and services.
- **Infrastructure**: Provides the implementation for data access, external services, and other infrastructure-related concerns.
- **API (Footex)**: Exposes the application's functionality through a RESTful API.

## Technologies

The project uses a modern technology stack, including:

- **.NET 8**: The primary framework for building the application.
- **ASP.NET Core**: For building the web API.
- **Entity Framework Core**: As the object-relational mapper (ORM).
- **PostgreSQL**: The primary database for the application.
- **Redis**: For caching and improving performance.
- **RabbitMQ**: As a message broker for asynchronous communication.
- **SignalR**: For real-time communication with clients.
- **JWT**: For authentication and authorization.
- **Docker**: For containerizing the application.

## Key Features

- **Team Management**: Create and manage football teams, players, and coaches.
- **Match Simulation**: Real-time match simulation with live statistics.
- **Stadium Management**: Comprehensive stadium and venue management.
- **Season Management**: Full season tracking and league management.
- **Real-time Updates**: SignalR integration for live match updates.
- **Advanced Search**: Comprehensive search functionality across all entities.
- **Caching**: Redis-based caching for optimal performance.
- **Message Queuing**: RabbitMQ for asynchronous processing.
- **Authentication & Authorization**: JWT-based security.

## Build and Run

### Without Docker

1. **Prerequisites**:
   - .NET 8 SDK
   - PostgreSQL
   - Redis
   - RabbitMQ

2. **Database Setup**:
   ```bash
   dotnet ef database update -p Infrastructure -s Footex
   ```

3. **Run the application**:
   ```bash
   cd Footex
   dotnet run
   ```

### With Docker

The project includes a PowerShell script (`docker-manage.ps1`) for easy Docker management.

- **Start development environment**:
  ```powershell
  .\docker-manage.ps1 dev-up
  ```

- **Stop development environment**:
  ```powershell
  .\docker-manage.ps1 dev-down
  ```

- **Rebuild and start development**:
  ```powershell
  .\docker-manage.ps1 dev-rebuild
  ```

## Testing

To run the tests, use the following command:

```bash
dotnet test
```
when creating a new test file or files dont test the whole project, just test the new file or files you created.
to run tests for a specific file, use the following command:

```bash
dotnet test --filter "FullyQualifiedName~YourNamespace.YourTestClass"
```
The project includes a comprehensive suite of tests to ensure the quality and reliability of the application. The tests cover various aspects, including unit tests, integration tests, and performance tests.
The project includes unit tests, integration tests, and performance tests to ensure the quality and reliability of the application.


The project has a comprehensive test suite with 89.2% code coverage, including unit, integration, and performance tests.

## Fixing Errors 
To fix errors in the project, follow these steps:
1. **Identify the Error**: identify the errors by building projects 
2. **Check the Error Message**: Read the error message carefully to understand what is causing the issue.
3. **Search for Solutions**: Use the error message to search for solutions online, such as on Stack Overflow or GitHub issues.
4. **Check Dependencies**: Ensure that all dependencies are correctly installed and up to date.

## Docker

The project is fully containerized using Docker. The `docker-compose.yml` file defines the services for the production environment, while the `docker-compose.dev.yml` file is used for the development environment.

## Project Structure

The project is organized into the following directories:

- `Application/`: Application layer (CQRS, DTOs, Services)
- `Domain/`: Domain layer (Entities, Repositories)
- `Infrastructure/`: Infrastructure layer (Data, External Services)
- `Footex/`: API layer (Controllers, Configuration)
- `docs/`: Documentation
- `docker-compose.yml`: Production Docker configuration
- `docker-compose.dev.yml`: Development Docker configuration
- `docker-manage.ps1`: Docker management script

## API Endpoints

The API is documented using Swagger. The Swagger UI is available at `/swagger` when running the application in a development environment.

## Environment Variables

The project uses environment variables for configuration. The required environment variables are documented in the `README.md` file.

## CI/CD

The project has a complete CI/CD pipeline with 7 GitHub Actions workflows for continuous integration, performance testing, security monitoring, and deployment.

## Code Quality

The project uses various tools to ensure code quality, including code analysis, style checking, and code coverage.

## Performance

The project has been load tested to handle up to 500 requests per second with response times under 150ms.

## Security

The project includes multi-layered security scanning and compliance to ensure the security of the application.

## Documentation

The project has comprehensive documentation, including auto-generated API documentation and technical guides.

## Contributing

Contributions are welcome. Please follow the guidelines in the `CONTRIBUTING.md` file.

## License

The project is licensed under the MIT License.

## Troubleshooting

The `README.md` file contains a troubleshooting section with common issues and how to resolve them.

## Support

For support and questions, please open an issue in the repository.
