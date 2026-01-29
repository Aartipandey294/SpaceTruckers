namespace SpaceTruckers.Domain.Entities;

/// <summary>
/// Represents a driver (pilot) who operates vehicles for deliveries.
/// </summary>
public sealed class Driver
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public string LicenseNumber { get; private set; }
    public int ExperienceYears { get; private set; }
    public bool IsAvailable { get; private set; }

    private Driver() { } // For persistence

    public Driver(Guid id, string name, string licenseNumber, int experienceYears)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Driver ID cannot be empty", nameof(id));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Driver name cannot be empty", nameof(name));
        if (string.IsNullOrWhiteSpace(licenseNumber))
            throw new ArgumentException("License number cannot be empty", nameof(licenseNumber));
        if (experienceYears < 0)
            throw new ArgumentException("Experience years cannot be negative", nameof(experienceYears));

        Id = id;
        Name = name;
        LicenseNumber = licenseNumber;
        ExperienceYears = experienceYears;
        IsAvailable = true;
    }

    public void MarkAsUnavailable() => IsAvailable = false;
    public void MarkAsAvailable() => IsAvailable = true;

    public void UpdateDetails(string name, int experienceYears)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Driver name cannot be empty", nameof(name));
        if (experienceYears < 0)
            throw new ArgumentException("Experience years cannot be negative", nameof(experienceYears));

        Name = name;
        ExperienceYears = experienceYears;
    }
}
