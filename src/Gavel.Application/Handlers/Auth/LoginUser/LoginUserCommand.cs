using MediatR;

namespace Gavel.Application.Handlers.Auth.LoginUser;

public class LoginUserCommand : IRequest<string>
{
    public string Email { get; set; }
    public string Password { get; set; }
}