using MediatR;

namespace Gavel.Application.Handlers.Auth.LoginUser;

public class LoginUserCommand : IRequest<LoginUserResponse>
{
    public string Email { get; set; }
    public string Password { get; set; }
}