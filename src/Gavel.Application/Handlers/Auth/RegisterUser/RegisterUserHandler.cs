using FluentValidation;
using Gavel.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace Gavel.Application.Handlers.Auth.RegisterUser;

public class RegisterUserHandler(UserManager<ApplicationUser> userManager)
    : IRequestHandler<RegisterUserCommand, Guid>
{
    public async Task<Guid> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName =  request.LastName,
            EmailConfirmed = true
        };
        
        var result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            var failure = result.Errors
                .Select(e => new FluentValidation.Results.ValidationFailure(e.Code, e.Description));
            
            throw new ValidationException(failure);
        }
        
        await userManager.AddToRoleAsync(user, "User");
        return user.Id;
    }
}