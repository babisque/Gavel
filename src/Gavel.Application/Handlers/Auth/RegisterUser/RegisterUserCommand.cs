using MediatR;

namespace Gavel.Application.Handlers.Auth.RegisterUser;

public class RegisterUserCommand : IRequest<Guid>
{
    public string Email { get; set; } = String.Empty;
    public string Password { get; set; } = String.Empty;
    public string ConfirmPassword { get; set; } = String.Empty;
    public string FirstName { get; set; } = String.Empty;
    public string LastName { get; set; } = String.Empty;
}