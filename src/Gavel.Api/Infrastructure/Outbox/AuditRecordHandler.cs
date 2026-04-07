using System.Text.Json;
using Gavel.Api.Infrastructure.Data;
using Gavel.Core.Domain.Services;
using Gavel.Core.Infrastructure.Logging;

namespace Gavel.Api.Infrastructure.Outbox;

public sealed class AuditRecordHandler(IAuditLogger auditLogger) : IOutboxHandler
{
    public bool CanHandle(string type) => type == "AuditRecord";

    public async Task HandleAsync(OutboxMessage message, GavelDbContext context, CancellationToken ct)
    {
        var record = JsonSerializer.Deserialize(message.Content, AppJsonSerializerContext.Default.AuditRecord);
        if (record != null)
        {
            await auditLogger.LogAsync(record);
        }
    }
}
