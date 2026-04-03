using Gavel.Core.Infrastructure.Notifications;
using Microsoft.Extensions.Logging;

namespace Gavel.Api.Infrastructure.Notifications;

public class NotificationService(ILogger<NotificationService> logger) : INotificationService
{
    public Task SendAsync(Guid userId, string action, object? metadata = null)
    {
        logger.LogInformation("NOTIFICATION: To {UserId} Action {Action} Metadata {Metadata}", 
            userId, action, metadata?.ToString() ?? "none");
        return Task.CompletedTask;
    }
}
