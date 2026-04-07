using Gavel.Api.Infrastructure.Data;

namespace Gavel.Api.Infrastructure.Outbox;

/// <summary>
/// Defines a handler for a specific type of Outbox message.
/// Resides in Api/Infrastructure to avoid circular dependencies with Core.
/// </summary>
public interface IOutboxHandler
{
    /// <summary>
    /// Determines if this handler can process the given message type.
    /// </summary>
    bool CanHandle(string type);

    /// <summary>
    /// Processes the outbox message using its own scoped context.
    /// </summary>
    Task HandleAsync(OutboxMessage message, GavelDbContext context, CancellationToken ct);
}
