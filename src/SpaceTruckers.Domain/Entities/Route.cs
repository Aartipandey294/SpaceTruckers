using SpaceTruckers.Domain.ValueObjects;

namespace SpaceTruckers.Domain.Entities;

/// <summary>
/// Represents a delivery route from origin to destination with checkpoints.
/// </summary>
public sealed class Route
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public SpaceLocation Origin { get; private set; }
    public SpaceLocation Destination { get; private set; }
    public int DangerRating { get; private set; }
    
    private readonly List<Checkpoint> _checkpoints = new();
    public IReadOnlyList<Checkpoint> Checkpoints => _checkpoints.AsReadOnly();

    private Route() { } // For persistence

    public Route(
        Guid id,
        string name,
        SpaceLocation origin,
        SpaceLocation destination,
        int dangerRating = 1)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Route ID cannot be empty", nameof(id));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Route name cannot be empty", nameof(name));
        if (dangerRating < 1 || dangerRating > 10)
            throw new ArgumentException("Danger rating must be between 1 and 10", nameof(dangerRating));

        Id = id;
        Name = name;
        Origin = origin ?? throw new ArgumentNullException(nameof(origin));
        Destination = destination ?? throw new ArgumentNullException(nameof(destination));
        DangerRating = dangerRating;
    }

    public void AddCheckpoint(Checkpoint checkpoint)
    {
        ArgumentNullException.ThrowIfNull(checkpoint);

        if (_checkpoints.Any(c => c.Id == checkpoint.Id))
            throw new InvalidOperationException($"Checkpoint with ID {checkpoint.Id} already exists");

        if (_checkpoints.Any(c => c.SequenceNumber == checkpoint.SequenceNumber))
            throw new InvalidOperationException($"Checkpoint with sequence number {checkpoint.SequenceNumber} already exists");

        _checkpoints.Add(checkpoint);
        _checkpoints.Sort((a, b) => a.SequenceNumber.CompareTo(b.SequenceNumber));
    }

    public void RemoveCheckpoint(Guid checkpointId)
    {
        var checkpoint = _checkpoints.FirstOrDefault(c => c.Id == checkpointId);
        if (checkpoint is null)
            throw new InvalidOperationException($"Checkpoint with ID {checkpointId} not found");

        _checkpoints.Remove(checkpoint);
    }

    public double TotalDistance()
    {
        if (!_checkpoints.Any())
            return Origin.DistanceTo(Destination);

        var sortedCheckpoints = _checkpoints.OrderBy(c => c.SequenceNumber).ToList();
        
        double total = Origin.DistanceTo(sortedCheckpoints.First().Location);
        
        for (int i = 0; i < sortedCheckpoints.Count - 1; i++)
        {
            total += sortedCheckpoints[i].Location.DistanceTo(sortedCheckpoints[i + 1].Location);
        }
        
        total += sortedCheckpoints.Last().Location.DistanceTo(Destination);
        
        return total;
    }

    public TimeSpan EstimatedTotalDuration()
    {
        return _checkpoints.Aggregate(
            TimeSpan.Zero,
            (total, checkpoint) => total + checkpoint.EstimatedDuration);
    }
}
