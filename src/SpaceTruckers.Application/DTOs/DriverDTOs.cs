namespace SpaceTruckers.Application.DTOs;

public sealed record CreateDriverRequest(
    string Name,
    string LicenseNumber,
    int ExperienceYears);

public sealed record UpdateDriverRequest(
    string Name,
    int ExperienceYears);

public sealed record DriverResponse(
    Guid Id,
    string Name,
    string LicenseNumber,
    int ExperienceYears,
    bool IsAvailable);
