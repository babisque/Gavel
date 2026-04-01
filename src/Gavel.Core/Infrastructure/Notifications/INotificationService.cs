namespace Gavel.Core.Infrastructure.Notifications;

public interface INotificationService
{
    Task SendAsync(Guid userId, string action, object? metadata = null);
}
