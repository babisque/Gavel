using Gavel.Api.Infrastructure.Data;
using Gavel.Core.Domain.Services;
using Gavel.Core.Domain.Settlements;
using Gavel.Core.Infrastructure.Legal;
using Gavel.Core.Infrastructure.Logging;
using Microsoft.EntityFrameworkCore;

namespace Gavel.Api.Infrastructure.Outbox;

public sealed class SignSettlementHandler(
    IDigitalSignatureService signatureService,
    IAuditLogger auditLogger,
    TimeProvider timeProvider) : IOutboxHandler
{
    public bool CanHandle(string type) => type == "SignSettlement";

    public async Task HandleAsync(OutboxMessage message, GavelDbContext context, CancellationToken ct)
    {
        var settlementId = Guid.Parse(message.Content);

        var settlement = await context.Settlements.FirstOrDefaultAsync(s => s.Id == settlementId, ct)
            ?? throw new InvalidOperationException($"Settlement {settlementId} not found.");

        if (settlement != null)
        {
            var signature = await signatureService.SignSettlementAsync(settlement);
            settlement.ApplySignature(signature);

            await auditLogger.LogAsync(new AuditRecord(
                settlement.BidderId,
                "SettlementSigned",
                timeProvider.GetUtcNow(),
                $"Settlement: {settlement.Id}, Signature: {signature}"
            ));

            // Trigger Legal Document Generation after successful signature
            context.OutboxMessages.Add(new OutboxMessage
            {
                Type = "GenerateLegalDocuments",
                Content = settlement.Id.ToString(),
                CreatedAt = timeProvider.GetUtcNow()
            });
        }
    }
}
