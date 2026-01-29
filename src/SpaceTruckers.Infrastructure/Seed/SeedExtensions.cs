using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SpaceTruckers.Domain.Entities;
using SpaceTruckers.Domain.Ports;
using SpaceTruckers.Domain.ValueObjects;

namespace SpaceTruckers.Infrastructure.Seed;

/// <summary>
/// Extension methods for seeding demo data.
/// </summary>
public static class SeedExtensions
{
    /// <summary>
    /// Seeds demo data for development and testing purposes.
    /// </summary>
    public static async Task SeedDemoDataAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<object>>();
        var driverRepo = scope.ServiceProvider.GetRequiredService<IDriverRepository>();
        var vehicleRepo = scope.ServiceProvider.GetRequiredService<IVehicleRepository>();
        var routeRepo = scope.ServiceProvider.GetRequiredService<IRouteRepository>();
        var idGenerator = scope.ServiceProvider.GetRequiredService<IIdGenerator>();

        logger.LogInformation("Seeding demo data...");

        // Seed Drivers
        var driver1 = new Driver(idGenerator.NewId(), "Han Solo", "PILOT-001", 15);
        var driver2 = new Driver(idGenerator.NewId(), "Starbuck", "PILOT-002", 10);

        await driverRepo.SaveAsync(driver1);
        await driverRepo.SaveAsync(driver2);
        
        logger.LogInformation("Seeded {Count} drivers: {Names}", 2, new[] { driver1.Name, driver2.Name });

        // Seed Vehicles
        var vehicle1 = new Vehicle(
            idGenerator.NewId(),
            "Millennium Falcon",
            VehicleType.CargoFreighter,
            10000,
            1500);
        
        var vehicle2 = new Vehicle(
            idGenerator.NewId(),
            "Express Runner",
            VehicleType.RocketVan,
            2000,
            2500);

        await vehicleRepo.SaveAsync(vehicle1);
        await vehicleRepo.SaveAsync(vehicle2);
        
        logger.LogInformation("Seeded {Count} vehicles: {Names}", 2, new[] { vehicle1.Name, vehicle2.Name });

        // Seed Route with checkpoints
        var origin = new SpaceLocation("Earth Station Alpha", "Sol", 0, 0, 0);
        var destination = new SpaceLocation("Mars Colony Beta", "Sol", 225, 0, 0);

        var route = new Route(idGenerator.NewId(), "Earth-Mars Express", origin, destination, 5);

        var checkpoint1 = new Checkpoint(
            idGenerator.NewId(),
            "Lunar Waystation",
            new SpaceLocation("Moon", "Sol", 0.384, 0, 0),
            1,
            TimeSpan.FromMinutes(30));

        var checkpoint2 = new Checkpoint(
            idGenerator.NewId(),
            "Asteroid Belt Checkpoint",
            new SpaceLocation("Ceres Station", "Sol", 100, 0, 0),
            2,
            TimeSpan.FromMinutes(45));

        route.AddCheckpoint(checkpoint1);
        route.AddCheckpoint(checkpoint2);

        await routeRepo.SaveAsync(route);
        
        logger.LogInformation(
            "Seeded route '{RouteName}' with {CheckpointCount} checkpoints",
            route.Name,
            route.Checkpoints.Count);

        logger.LogInformation("Demo data seeding completed successfully");
    }
}
