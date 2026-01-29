using SpaceTruckers.Domain.ValueObjects;

namespace SpaceTruckers.Domain.Entities;

/// <summary>
/// Represents a vehicle used for deliveries.
/// </summary>
public sealed class Vehicle
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public VehicleType Type { get; private set; }
    public double MaxCargoCapacityKg { get; private set; }
    public double MaxSpeed { get; private set; }
    public bool IsAvailable { get; private set; }

    private Vehicle() { } // For persistence

    public Vehicle(
        Guid id,
        string name,
        VehicleType type,
        double maxCargoCapacityKg,
        double maxSpeed)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Vehicle ID cannot be empty", nameof(id));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Vehicle name cannot be empty", nameof(name));
        if (maxCargoCapacityKg <= 0)
            throw new ArgumentException("Max cargo capacity must be positive", nameof(maxCargoCapacityKg));
        if (maxSpeed <= 0)
            throw new ArgumentException("Max speed must be positive", nameof(maxSpeed));

        Id = id;
        Name = name;
        Type = type;
        MaxCargoCapacityKg = maxCargoCapacityKg;
        MaxSpeed = maxSpeed;
        IsAvailable = true;
    }

    public void MarkAsUnavailable() => IsAvailable = false;
    public void MarkAsAvailable() => IsAvailable = true;

    public bool CanCarry(double weightKg) => weightKg <= MaxCargoCapacityKg;

    public void UpdateDetails(string name, double maxCargoCapacityKg, double maxSpeed)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Vehicle name cannot be empty", nameof(name));
        if (maxCargoCapacityKg <= 0)
            throw new ArgumentException("Max cargo capacity must be positive", nameof(maxCargoCapacityKg));
        if (maxSpeed <= 0)
            throw new ArgumentException("Max speed must be positive", nameof(maxSpeed));

        Name = name;
        MaxCargoCapacityKg = maxCargoCapacityKg;
        MaxSpeed = maxSpeed;
    }
}
