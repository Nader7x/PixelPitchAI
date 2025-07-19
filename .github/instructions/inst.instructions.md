---
applyTo: "**"
---

Coding standards, domain knowledge, and preferences that AI should follow.

# Instructions for AI

- after creating files use the following command to format c# files : dotnet csharpier format <filePath>
- use the following command to format all c# files in a directory you are in : dotnet csharpier format .
- use the following command to format all c# files in a directory and subdirectories : dotnet csharpier format --recursive .

# Writing or Refactoring Tests

- use the `xUnit` testing framework
- use the `FluentAssertions` library for assertions
- use the `Moq` library for mocking dependencies
- derive the test architecture from the existing tests in the test project to understand the structure and naming conventions, etc.

# Project-specific Test Instructions

## Footex.UnitTests

- Use xUnit, Moq, and FluentAssertions.
- Mock all dependencies; focus on isolated unit logic.
- Place new test classes in the appropriate subfolder (e.g., Services, Infrastructure).
- Name test classes as `<ClassUnderTest>Tests` and methods as `<MethodUnderTest>_ExpectedBehavior`.
- Initialize mocks and the system under test in the constructor.
- Use `[Fact]` for test methods and follow the Arrange-Act-Assert pattern.

## Footex.IntegrationTests

- Use xUnit and FluentAssertions.
- Use real service/repository instances, not mocks.
- Use provided fixtures (e.g., FootexWebApplicationFactory, IUnitOfWork) for setup.
- Decorate test classes with `[Collection("Database")]` if database access is required.
- Name test classes as `<RepositoryOrHandler>IntegrationTests` and methods as `<Action>_ExpectedBehavior`.
- Use `[Fact]` for test methods.

## Footex.PerformanceTests

- Use xUnit and NBomber for performance/load testing.
- Structure each performance test as a `[Fact]` method that defines and runs an NBomber scenario.
- Use shared test fixtures for setup.
- Place new tests in the appropriate subfolder (e.g., LoadTests, Benchmarks).
- Name test classes and methods to clearly indicate the performance aspect being tested.
