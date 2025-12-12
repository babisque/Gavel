using Gavel.Domain.Entities;
using Gavel.Domain.Interfaces.Services;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace Gavel.Application.Handlers.Auth.LoginUser;

public class LoginUserHandler(UserManager<ApplicationUser> userManager, ITokenService tokenService) : IRequestHandler<LoginUserCommand, LoginUserResponse>
{
    public async Task<LoginUserResponse> Handle(LoginUserCommand request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null || !await userManager.CheckPasswordAsync(user, request.Password))
            throw new UnauthorizedAccessException("Invalid credentials.");

        var result = new LoginUserResponse
        {
            AccessToken = await tokenService.GenerateToken(user),
            FirstName = user.FirstName
        };
        
        return result;
    }
}