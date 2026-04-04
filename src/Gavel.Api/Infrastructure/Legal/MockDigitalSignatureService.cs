using System.Security.Cryptography;
using System.Text;
using Gavel.Core.Domain.Settlements;
using Gavel.Core.Infrastructure.Legal;

namespace Gavel.Api.Infrastructure.Legal;

public class MockDigitalSignatureService : IDigitalSignatureService
{
    public Task<string> SignSettlementAsync(Settlement settlement)
    {
        var dataToSign = $"{settlement.Id}|{settlement.LotId}|{settlement.PriceBreakdown.Total}|{settlement.IssuedAt:O}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(dataToSign));
        return Task.FromResult(Convert.ToHexString(hash));
    }
}
