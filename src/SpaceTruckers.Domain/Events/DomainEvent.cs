namespace SpaceTruckers.Domain.Events;

/// <summary>
/// Base class for all domain events in the system.
/// Events are immutable records of something that happened in the domain.
/// </summary>
public abstract record DomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; init; }
    
    protected DomainEvent(DateTimeOffset occurredAt)
    {
        OccurredAt = occurredAt;
    }
}
