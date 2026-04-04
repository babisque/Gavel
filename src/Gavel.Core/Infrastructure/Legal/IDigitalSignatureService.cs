using Gavel.Core.Domain.Settlements;

namespace Gavel.Core.Infrastructure.Legal;

public interface IDigitalSignatureService
{
    Task<string> SignSettlementAsync(Settlement settlement);
}
