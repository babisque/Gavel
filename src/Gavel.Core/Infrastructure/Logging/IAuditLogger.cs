namespace Gavel.Core.Infrastructure.Logging;

public record AuditRecord(Guid BidderId, string Action, DateTimeOffset Timestamp, string Metadata);

public interface IAuditLogger
{
    Task LogAsync(AuditRecord record);
}
