using Gavel.Core.Infrastructure.Logging;
using Microsoft.Extensions.Logging;

namespace Gavel.Api.Infrastructure.Logging;

public class AuditLogger(ILogger<AuditLogger> logger) : IAuditLogger
{
    public Task LogAsync(AuditRecord record)
    {
        logger.LogInformation("AUDIT: {Action} by {BidderId} at {Timestamp}. Metadata: {Metadata}", 
            record.Action, record.BidderId, record.Timestamp, record.Metadata);
        return Task.CompletedTask;
    }
}
