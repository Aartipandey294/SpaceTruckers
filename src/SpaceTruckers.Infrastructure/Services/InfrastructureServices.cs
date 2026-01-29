using SpaceTruckers.Domain.Ports;

namespace SpaceTruckers.Infrastructure.Services;

/// <summary>
/// System clock implementation.
/// </summary>
public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}

/// <summary>
/// Standard GUID generator.
/// </summary>
public sealed class GuidGenerator : IIdGenerator
{
    public Guid NewId() => Guid.NewGuid();
}
