using FluentValidation;
using Gavel.Application.Handlers.Auth.RegisterUser;
using Gavel.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Moq;

namespace Gavel.Tests.Application.Handlers;

public class RegisterUserHandlerTests
{
    private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
    private readonly RegisterUserHandler _handler;

    public RegisterUserHandlerTests()
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        _mockUserManager = new Mock<UserManager<ApplicationUser>>(
            store.Object, null, null, null, null, null, null, null, null);

        _handler = new RegisterUserHandler(_mockUserManager.Object);
    }

    [Fact]
    public async Task Handle_WhenValidRequest_CreatesUserAndReturnsId()
    {
        // Arrange
        var command = new RegisterUserCommand
        {
            Email = "test@example.com",
            Password = "Password123!",
            ConfirmPassword = "Password123!",
            FirstName = "John",
            LastName = "Doe"
        };

        ApplicationUser capturedUser = null;

        _mockUserManager
            .Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success)
            .Callback<ApplicationUser, string>((user, _) => 
            {
                user.Id = Guid.NewGuid(); // Simulate UserManager setting the ID
                capturedUser = user;
            });

        _mockUserManager
            .Setup(x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), "User"))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotEqual(Guid.Empty, result);
        Assert.NotNull(capturedUser);
        Assert.Equal(command.Email, capturedUser.Email);
        Assert.Equal(command.Email, capturedUser.UserName);
        Assert.Equal(command.FirstName, capturedUser.FirstName);
        Assert.Equal(command.LastName, capturedUser.LastName);
        Assert.True(capturedUser.EmailConfirmed);

        _mockUserManager.Verify(x => x.CreateAsync(It.IsAny<ApplicationUser>(), command.Password), Times.Once);
        _mockUserManager.Verify(x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), "User"), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenUserCreationFails_ThrowsValidationException()
    {
        // Arrange
        var command = new RegisterUserCommand
        {
            Email = "test@example.com",
            Password = "Password123!",
            ConfirmPassword = "Password123!",
            FirstName = "John",
            LastName = "Doe"
        };

        var identityErrors = new[]
        {
            new IdentityError { Code = "DuplicateEmail", Description = "Email is already taken." }
        };

        _mockUserManager
            .Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Failed(identityErrors));

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            () => _handler.Handle(command, CancellationToken.None)
        );

        _mockUserManager.Verify(x => x.CreateAsync(It.IsAny<ApplicationUser>(), command.Password), Times.Once);
        _mockUserManager.Verify(x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), "User"), Times.Never);
    }
}

