using Gavel.API.Controllers;
using Gavel.Application.Handlers.Auth.LoginUser;
using Gavel.Application.Handlers.Auth.RegisterUser;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Gavel.Tests.API;

public class AuthControllerTests
{
    private readonly Mock<IMediator> _mockMediator;
    private readonly AuthController _controller;

    public AuthControllerTests()
    {
        _mockMediator = new Mock<IMediator>();
        _controller = new AuthController(_mockMediator.Object);
    }

    [Fact]
    public async Task RegisterUser_WhenCalled_ReturnsCreatedResult()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new RegisterUserCommand
        {
            Email = "test@example.com",
            Password = "Password123!",
            ConfirmPassword = "Password123!",
            FirstName = "John",
            LastName = "Doe"
        };

        _mockMediator
            .Setup(m => m.Send(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(userId);

        // Act
        var result = await _controller.RegisterUser(request);

        // Assert
        var createdResult = Assert.IsType<CreatedResult>(result);
        Assert.NotNull(createdResult.Value);
        
        _mockMediator.Verify(m => m.Send(request, It.IsAny<CancellationToken>()), Times.Once);
    }
}

