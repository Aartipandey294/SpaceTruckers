namespace SpaceTruckers.Domain.ValueObjects;

/// <summary>
/// Represents the type of vehicle used for deliveries.
/// </summary>
public enum VehicleType
{
    HoverTruck,
    RocketVan,
    SpaceCycle,
    CargoFreighter,
    ExpressPod
}

/// <summary>
/// Represents the current status of a trip.
/// </summary>
public enum TripStatus
{
    NotStarted,
    InProgress,
    Completed,
    Cancelled
}

/// <summary>
/// Represents types of incidents that can occur during a trip.
/// </summary>
public enum IncidentType
{
    AsteroidField,
    CosmicStorm,
    EmergencyMaintenance,
    PirateEncounter,
    NavigationError,
    CargoShift,
    FuelLeak,
    CommunicationFailure,
    Other
}

/// <summary>
/// Represents the severity level of an incident.
/// </summary>
public enum IncidentSeverity
{
    Minor,
    Moderate,
    Major,
    Critical
}

/// <summary>
/// Represents a location in space.
/// </summary>
public sealed record SpaceLocation
{
    public string Name { get; }
    public string Sector { get; }
    public double X { get; }
    public double Y { get; }
    public double Z { get; }

    public SpaceLocation(string name, string sector, double x, double y, double z)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Location name cannot be empty", nameof(name));
        if (string.IsNullOrWhiteSpace(sector))
            throw new ArgumentException("Sector cannot be empty", nameof(sector));

        Name = name;
        Sector = sector;
        X = x;
        Y = y;
        Z = z;
    }

    public double DistanceTo(SpaceLocation other)
    {
        return Math.Sqrt(
            Math.Pow(X - other.X, 2) +
            Math.Pow(Y - other.Y, 2) +
            Math.Pow(Z - other.Z, 2));
    }
}

/// <summary>
/// Represents a checkpoint along a route.
/// </summary>
public sealed record Checkpoint
{
    public Guid Id { get; }
    public string Name { get; }
    public SpaceLocation Location { get; }
    public int SequenceNumber { get; }
    public TimeSpan EstimatedDuration { get; }

    public Checkpoint(
        Guid id,
        string name,
        SpaceLocation location,
        int sequenceNumber,
        TimeSpan estimatedDuration)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Checkpoint ID cannot be empty", nameof(id));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Checkpoint name cannot be empty", nameof(name));
        if (sequenceNumber < 0)
            throw new ArgumentException("Sequence number cannot be negative", nameof(sequenceNumber));

        Id = id;
        Name = name;
        Location = location ?? throw new ArgumentNullException(nameof(location));
        SequenceNumber = sequenceNumber;
        EstimatedDuration = estimatedDuration;
    }
}

/// <summary>
/// Represents cargo being transported.
/// </summary>
public sealed record Cargo
{
    public string Description { get; }
    public double WeightKg { get; }
    public bool IsFragile { get; }
    public bool RequiresRefrigeration { get; }

    public Cargo(string description, double weightKg, bool isFragile = false, bool requiresRefrigeration = false)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Cargo description cannot be empty", nameof(description));
        if (weightKg <= 0)
            throw new ArgumentException("Cargo weight must be positive", nameof(weightKg));

        Description = description;
        WeightKg = weightKg;
        IsFragile = isFragile;
        RequiresRefrigeration = requiresRefrigeration;
    }
}
