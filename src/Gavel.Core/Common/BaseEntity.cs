using System.ComponentModel.DataAnnotations.Schema;
using MediatR;

namespace Gavel.Domain.Common;

public abstract class BaseEntity
{
    private readonly List<INotification> _domainEvents = [];
    
    [NotMapped]
    public IReadOnlyCollection<INotification> DomainEvents => _domainEvents.AsReadOnly();
    
    public void AddDomainEvent(INotification eventItem)
    {
        _domainEvents.Add(eventItem);
    }

    public void RemoveDomainEvent(INotification eventItem)
    {
        _domainEvents.Remove(eventItem);
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}