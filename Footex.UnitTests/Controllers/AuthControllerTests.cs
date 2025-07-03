using System.Security.Claims;
using Application.CQRS.Auth.Commands;
using Application.CQRS.Auth.Queries;
using Application.Dtos;
using Application.Interfaces;
using Application.Mappers;
using AutoFixture;
using FluentAssertions;
using Footex.Controllers;
using Footex.UnitTests.Common;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Footex.UnitTests.Controllers;

public class AuthControllerTests : IClassFixture<TestFixtureBase>
{
    private readonly TestFixtureBase _testFixtureBase;
    private readonly Mock<IMediator> _mediatorMock;
    private readonly Mock<IUserMapper> _userMapperMock;
    private readonly Mock<IFileStorageService> _fileStorageServiceMock;
    private readonly AuthController _controller;
    private readonly NoRecursionFixture _fixture;

    public AuthControllerTests(TestFixtureBase testFixtureBase)
    {
        _testFixtureBase = testFixtureBase;
        _mediatorMock = new Mock<IMediator>();
        _userMapperMock = new Mock<IUserMapper>();
        _fileStorageServiceMock = new Mock<IFileStorageService>();

        _fixture = new NoRecursionFixture();
        _fixture
            .Behaviors.OfType<ThrowingRecursionBehavior>()
            .ToList()
            .ForEach(b => _fixture.Behaviors.Remove(b));
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());

        _controller = new AuthController(
            _mediatorMock.Object,
            _userMapperMock.Object,
            _fileStorageServiceMock.Object
        );

        // Setup controller context
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "test-user-id"),
            new(ClaimTypes.Name, "test-user"),
            new(ClaimTypes.Role, "User"),
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal },
        };
    }

    [Fact]
    public async Task Register_WithValidUser_ReturnsOkResult()
    {
        // Arrange
        var registerDto = new RegisterUserDto
        {
            FirstName = "Test",
            LastName = "User",
            UserName = "testuser",
            Email = "test@example.com",
            Password = "Password123!",
            Age = 25,
        };

        var registerCommand = new RegisterUserCommand
        {
            FirstName = registerDto.FirstName,
            LastName = registerDto.LastName,
            UserName = registerDto.UserName,
            Email = registerDto.Email,
            Password = registerDto.Password,
            Age = registerDto.Age,
        };

        var expectedResponse = new RegisterUserCommandResponse
        {
            Succeeded = true,
            UserId = "user-id-123",
        };

        _userMapperMock
            .Setup(x => x.ToRegisterCommandFromDto(It.IsAny<RegisterUserDto>()))
            .Returns(registerCommand);

        _mediatorMock
            .Setup(x => x.Send(It.IsAny<RegisterUserCommand>(), default))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.Register(registerDto);

        // Assert
        result.Should().BeOfType<ActionResult<RegisterUserCommandResponse>>();
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.Value.Should().BeEquivalentTo(expectedResponse);

        _mediatorMock.Verify(x => x.Send(It.IsAny<RegisterUserCommand>(), default), Times.Once);
    }

    [Fact]
    public async Task Register_WithFailedValidation_ReturnsBadRequest()
    {
        // Arrange
        var registerDto = new RegisterUserDto
        {
            FirstName = "Test",
            LastName = "User",
            UserName = "testuser",
            Email = "test@example.com",
            Password = "Password123!",
            Age = 25,
        };

        var registerCommand = new RegisterUserCommand
        {
            FirstName = registerDto.FirstName,
            LastName = registerDto.LastName,
            UserName = registerDto.UserName,
            Email = registerDto.Email,
            Password = registerDto.Password,
            Age = registerDto.Age,
        };

        var expectedResponse = new RegisterUserCommandResponse
        {
            Succeeded = false,
            Error = "Email already exists",
        };

        _userMapperMock
            .Setup(x => x.ToRegisterCommandFromDto(It.IsAny<RegisterUserDto>()))
            .Returns(registerCommand);

        _mediatorMock
            .Setup(x => x.Send(It.IsAny<RegisterUserCommand>(), default))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.Register(registerDto);

        // Assert
        result.Should().BeOfType<ActionResult<RegisterUserCommandResponse>>();
        var badRequestResult = result.Result as BadRequestObjectResult;
        badRequestResult.Should().NotBeNull();
        badRequestResult!.Value.Should().BeEquivalentTo(expectedResponse);

        _mediatorMock.Verify(x => x.Send(It.IsAny<RegisterUserCommand>(), default), Times.Once);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsOkResult()
    {
        // Arrange
        var loginDto = new UserLoginDto { Email = "test@example.com", Password = "Password123!" };

        var expectedResponse = new LoginUserCommandResponse
        {
            Succeeded = true,
            AccessToken = "jwt-token-here",
            Email = loginDto.Email,
            UserId = "user-id-123",
            RefreshToken = "refresh-token-here",
        };

        _mediatorMock
            .Setup(x => x.Send(It.IsAny<LoginUserCommand>(), default))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.Login(loginDto);

        // Assert
        result.Should().BeOfType<ActionResult<LoginUserCommandResponse>>();
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.Value.Should().BeEquivalentTo(expectedResponse);

        _mediatorMock.Verify(
            x =>
                x.Send(
                    It.Is<LoginUserCommand>(c =>
                        c.Email == loginDto.Email && c.Password == loginDto.Password
                    ),
                    default
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ReturnsBadRequest()
    {
        // Arrange
        var loginDto = new UserLoginDto { Email = "test@example.com", Password = "WrongPassword" };

        var expectedResponse = new LoginUserCommandResponse
        {
            Succeeded = false,
            Error = "Invalid credentials",
        };

        _mediatorMock
            .Setup(x => x.Send(It.IsAny<LoginUserCommand>(), default))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.Login(loginDto);

        // Assert
        result.Should().BeOfType<ActionResult<LoginUserCommandResponse>>();
        var badRequestResult = result.Result as BadRequestObjectResult;
        badRequestResult.Should().NotBeNull();
        badRequestResult!.Value.Should().BeEquivalentTo(expectedResponse);

        _mediatorMock.Verify(x => x.Send(It.IsAny<LoginUserCommand>(), default), Times.Once);
    }

    [Fact]
    public async Task GetProfile_WithValidUser_ReturnsOkResult()
    {
        // Arrange
        var expectedResponse = new GetUserProfileQueryResponse
        {
            Succeeded = true,
            UserId = "test-user-id",
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            Age = 25,
        };

        _mediatorMock
            .Setup(x => x.Send(It.IsAny<GetUserProfileQuery>(), default))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.GetProfile();

        // Assert
        result.Should().BeOfType<ActionResult<GetUserProfileQueryResponse>>();
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.Value.Should().BeEquivalentTo(expectedResponse);

        _mediatorMock.Verify(x => x.Send(It.IsAny<GetUserProfileQuery>(), default), Times.Once);
    }

    [Fact]
    public async Task UpdateUser_WithValidData_ReturnsOkResult()
    {
        // Arrange
        var updateDto = new UpdateUserDto
        {
            Id = "test-user-id",
            FirstName = "Updated",
            LastName = "User",
            Email = "updated@example.com",
        };

        var updateCommand = new UpdateUserCommand
        {
            Id = updateDto.Id,
            FirstName = updateDto.FirstName,
            LastName = updateDto.LastName,
            Email = updateDto.Email,
        };

        var expectedResponse = new UpdateUserCommandResponse { Succeeded = true };

        _userMapperMock
            .Setup(x => x.ToUpdateCommand(It.IsAny<UpdateUserDto>()))
            .Returns(updateCommand);

        _mediatorMock
            .Setup(x => x.Send(It.IsAny<UpdateUserCommand>(), default))
            .ReturnsAsync(expectedResponse);

        _mediatorMock
            .Setup(x => x.Send(It.IsAny<GetUserProfileQuery>(), default))
            .ReturnsAsync(new GetUserProfileQueryResponse { Succeeded = true });

        // Act
        var result = await _controller.UpdateUser(updateDto);

        // Assert
        result.Should().BeOfType<ActionResult<UpdateUserCommandResponse>>();
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.Value.Should().BeEquivalentTo(expectedResponse);

        _mediatorMock.Verify(x => x.Send(It.IsAny<UpdateUserCommand>(), default), Times.Once);
    }
}
