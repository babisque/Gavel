using Gavel.Application.Handlers.Auth.LoginUser;
using Gavel.Domain.Entities;
using Gavel.Domain.Interfaces.Services;
using Microsoft.AspNetCore.Identity;
using Moq;

namespace Gavel.Tests.Application.Handlers;

public class LoginUserHandlerTests
{
    private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
    private readonly Mock<ITokenService> _mockTokenService;
    private readonly LoginUserHandler _handler;

    public LoginUserHandlerTests()
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        _mockUserManager = new Mock<UserManager<ApplicationUser>>(
            store.Object, null, null, null, null, null, null, null, null);

        _mockTokenService = new Mock<ITokenService>();

        _handler = new LoginUserHandler(_mockUserManager.Object, _mockTokenService.Object);
    }

    [Fact]
    public async Task Handle_WhenCredentialsAreValid_ReturnsToken()
    {
        // Arrange
        var command = new LoginUserCommand
        {
            Email = "test@example.com",
            Password = "Password123!"
        };

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = command.Email,
            UserName = command.Email
        };

        var expectedToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...";

        _mockUserManager
            .Setup(x => x.FindByEmailAsync(command.Email))
            .ReturnsAsync(user);

        _mockUserManager
            .Setup(x => x.CheckPasswordAsync(user, command.Password))
            .ReturnsAsync(true);

        _mockTokenService
            .Setup(x => x.GenerateToken(user))
            .ReturnsAsync(expectedToken);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(expectedToken, result);

        _mockUserManager.Verify(x => x.FindByEmailAsync(command.Email), Times.Once);
        _mockUserManager.Verify(x => x.CheckPasswordAsync(user, command.Password), Times.Once);
        _mockTokenService.Verify(x => x.GenerateToken(user), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ThrowsUnauthorizedException()
    {
        // Arrange
        var command = new LoginUserCommand
        {
            Email = "nonexistent@example.com",
            Password = "Password123!"
        };

        _mockUserManager
            .Setup(x => x.FindByEmailAsync(command.Email))
            .ReturnsAsync((ApplicationUser)null);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _handler.Handle(command, CancellationToken.None)
        );

        _mockUserManager.Verify(x => x.FindByEmailAsync(command.Email), Times.Once);
        _mockUserManager.Verify(x => x.CheckPasswordAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never);
        _mockTokenService.Verify(x => x.GenerateToken(It.IsAny<ApplicationUser>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenPasswordIsIncorrect_ThrowsUnauthorizedException()
    {
        // Arrange
        var command = new LoginUserCommand
        {
            Email = "test@example.com",
            Password = "WrongPassword123!"
        };

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = command.Email,
            UserName = command.Email
        };

        _mockUserManager
            .Setup(x => x.FindByEmailAsync(command.Email))
            .ReturnsAsync(user);

        _mockUserManager
            .Setup(x => x.CheckPasswordAsync(user, command.Password))
            .ReturnsAsync(false);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _handler.Handle(command, CancellationToken.None)
        );

        _mockUserManager.Verify(x => x.FindByEmailAsync(command.Email), Times.Once);
        _mockUserManager.Verify(x => x.CheckPasswordAsync(user, command.Password), Times.Once);
        _mockTokenService.Verify(x => x.GenerateToken(It.IsAny<ApplicationUser>()), Times.Never);
    }
}

