using Gavel.Domain.Entities;

namespace Gavel.Domain.Interfaces.Services;

public interface ITokenService
{
    string GenerateToken(ApplicationUser user);
}