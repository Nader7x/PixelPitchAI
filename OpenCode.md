### OpenCode.md

#### Build/Test/Lint/Format Commands
- Build: `dotnet build Footex.sln`
- Run all tests: `dotnet test Footex.sln`
- Run single test: `dotnet test --filter FullyQualifiedName=Namespace.ClassName.MethodName`
- Run Single Test File: `dotnet test --filter FullyQualifiedName~Namespace.ClassName`
- Format: `dotnet csharpier format .`
- Lint: `dotnet format --check`

#### Code Style & Architecture Guidelines
1. Use PascalCase for class, method, property names; camelCase for fields/parameters
2. Organize by Clean Architecture: Domain, Application, Infrastructure, API layers
3. Domain: logic-rich entities, no dependencies on other layers
4. Application: CQRS pattern, MediatR style handlers, use dependency injection
5. Infrastructure: repositories, database/service clients, defined by interfaces
6. API: RESTful controllers, minimal business logic
7. Prefer explicit types, avoid dynamic/var except LINQ
8. Place error handling at API layer; validate input, log errors
9. Use async/await for IO, always configure await
10. Separate test projects for unit, integration, performance
11. Tests follow AAA (Arrange-Act-Assert); mock external dependencies
12. Keep classes short, single responsibility, favor composition
13. All public APIs should have XML doc comments
14. One-feature-per-folder; predictable structure
15. Use DI for all application/infrastructure services
16. Avoid static helpers in business logic; use extension methods
17. Serialization: default camelCase, configure JSON globally
18. Keep configuration outside code (appsettings, .env)
19. Follow .NET 8 coding conventions
20. Never commit secrets or passwords

#### General Instructions
when creating a new test file or files, don't test the whole project, test the new file or files you created.
to run tests for a specific file, use the following command:

```bash
dotnet test --filter "FullyQualifiedName~YourNamespace.YourTestClass"
```
