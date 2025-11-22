using Gavel.Domain.Entities;

namespace Gavel.Domain.Interfaces.Services;

public interface ITokenService
{
    Task<string> GenerateToken(ApplicationUser user);
}