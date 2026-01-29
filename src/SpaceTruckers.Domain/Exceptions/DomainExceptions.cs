namespace SpaceTruckers.Domain.Exceptions;

/// <summary>
/// Base exception for domain-specific errors.
/// </summary>
public abstract class DomainException : Exception
{
    protected DomainException(string message) : base(message) { }
    protected DomainException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// Thrown when an operation violates a domain invariant.
/// </summary>
public sealed class DomainInvariantViolationException : DomainException
{
    public DomainInvariantViolationException(string message) : base(message) { }
}

/// <summary>
/// Thrown when a trip state transition is invalid.
/// </summary>
public sealed class InvalidTripStateException : DomainException
{
    public string CurrentState { get; }
    public string AttemptedOperation { get; }

    public InvalidTripStateException(string currentState, string attemptedOperation)
        : base($"Cannot {attemptedOperation} when trip is in {currentState} state")
    {
        CurrentState = currentState;
        AttemptedOperation = attemptedOperation;
    }
}

/// <summary>
/// Thrown when a checkpoint operation is invalid.
/// </summary>
public sealed class InvalidCheckpointOperationException : DomainException
{
    public InvalidCheckpointOperationException(string message) : base(message) { }
}

/// <summary>
/// Thrown when a concurrency conflict is detected.
/// </summary>
public sealed class ConcurrencyConflictException : DomainException
{
    public Guid AggregateId { get; }
    public int ExpectedVersion { get; }
    public int ActualVersion { get; }

    public ConcurrencyConflictException(Guid aggregateId, int expectedVersion, int actualVersion)
        : base($"Concurrency conflict for aggregate {aggregateId}. Expected version {expectedVersion}, but found {actualVersion}")
    {
        AggregateId = aggregateId;
        ExpectedVersion = expectedVersion;
        ActualVersion = actualVersion;
    }
}

/// <summary>
/// Thrown when an entity is not found.
/// </summary>
public sealed class EntityNotFoundException : DomainException
{
    public string EntityType { get; }
    public Guid EntityId { get; }

    public EntityNotFoundException(string entityType, Guid entityId)
        : base($"{entityType} with ID {entityId} not found")
    {
        EntityType = entityType;
        EntityId = entityId;
    }
}

/// <summary>
/// Thrown when attempting to process a duplicate event.
/// </summary>
public sealed class DuplicateEventException : DomainException
{
    public Guid EventId { get; }

    public DuplicateEventException(Guid eventId)
        : base($"Event {eventId} has already been processed")
    {
        EventId = eventId;
    }
}
