using Xunit;
using SpaceTruckers.Domain.Entities;
using SpaceTruckers.Domain.ValueObjects;

namespace SpaceTruckers.Domain.Tests;

public class DriverTests
{
    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateDriver()
    {
        // Arrange & Act
        var driver = new Driver(Guid.NewGuid(), "Han Solo", "PILOT-001", 15);

        // Assert
        Assert.Equal("Han Solo", driver.Name);
        Assert.Equal("PILOT-001", driver.LicenseNumber);
        Assert.Equal(15, driver.ExperienceYears);
        Assert.True(driver.IsAvailable);
    }

    [Fact]
    public void Constructor_WithEmptyName_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() =>
            new Driver(Guid.NewGuid(), "", "PILOT-001", 5));
    }

    [Fact]
    public void Constructor_WithNegativeExperience_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() =>
            new Driver(Guid.NewGuid(), "Test", "PILOT-001", -1));
    }

    [Fact]
    public void MarkAsUnavailable_ShouldSetIsAvailableFalse()
    {
        var driver = new Driver(Guid.NewGuid(), "Test", "PILOT-001", 5);
        driver.MarkAsUnavailable();
        Assert.False(driver.IsAvailable);
    }

    [Fact]
    public void MarkAsAvailable_ShouldSetIsAvailableTrue()
    {
        var driver = new Driver(Guid.NewGuid(), "Test", "PILOT-001", 5);
        driver.MarkAsUnavailable();
        driver.MarkAsAvailable();
        Assert.True(driver.IsAvailable);
    }

    [Fact]
    public void UpdateDetails_ShouldUpdateNameAndExperience()
    {
        var driver = new Driver(Guid.NewGuid(), "Test", "PILOT-001", 5);
        driver.UpdateDetails("New Name", 10);
        Assert.Equal("New Name", driver.Name);
        Assert.Equal(10, driver.ExperienceYears);
    }
}

public class VehicleTests
{
    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateVehicle()
    {
        var vehicle = new Vehicle(Guid.NewGuid(), "Falcon", VehicleType.CargoFreighter, 10000, 1500);

        Assert.Equal("Falcon", vehicle.Name);
        Assert.Equal(VehicleType.CargoFreighter, vehicle.Type);
        Assert.Equal(10000, vehicle.MaxCargoCapacityKg);
        Assert.Equal(1500, vehicle.MaxSpeed);
        Assert.True(vehicle.IsAvailable);
    }

    [Fact]
    public void Constructor_WithZeroCapacity_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() =>
            new Vehicle(Guid.NewGuid(), "Test", VehicleType.RocketVan, 0, 1000));
    }

    [Fact]
    public void CanCarry_WhenWithinCapacity_ShouldReturnTrue()
    {
        var vehicle = new Vehicle(Guid.NewGuid(), "Test", VehicleType.HoverTruck, 5000, 500);
        Assert.True(vehicle.CanCarry(4000));
    }

    [Fact]
    public void CanCarry_WhenExceedsCapacity_ShouldReturnFalse()
    {
        var vehicle = new Vehicle(Guid.NewGuid(), "Test", VehicleType.SpaceCycle, 500, 2000);
        Assert.False(vehicle.CanCarry(600));
    }
}

public class RouteTests
{
    private static SpaceLocation CreateLocation(string name) =>
        new(name, "Sol", 0, 0, 0);

    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateRoute()
    {
        var origin = CreateLocation("Earth");
        var destination = CreateLocation("Mars");
        var route = new Route(Guid.NewGuid(), "Earth-Mars", origin, destination, 5);

        Assert.Equal("Earth-Mars", route.Name);
        Assert.Equal(origin, route.Origin);
        Assert.Equal(destination, route.Destination);
        Assert.Equal(5, route.DangerRating);
        Assert.Empty(route.Checkpoints);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(11)]
    public void Constructor_WithInvalidDangerRating_ShouldThrow(int rating)
    {
        Assert.Throws<ArgumentException>(() =>
            new Route(Guid.NewGuid(), "Test", CreateLocation("A"), CreateLocation("B"), rating));
    }

    [Fact]
    public void AddCheckpoint_ShouldAddAndSortBySequence()
    {
        var route = new Route(Guid.NewGuid(), "Test", CreateLocation("A"), CreateLocation("B"), 3);
        
        var cp2 = new Checkpoint(Guid.NewGuid(), "Second", CreateLocation("C"), 2, TimeSpan.FromMinutes(30));
        var cp1 = new Checkpoint(Guid.NewGuid(), "First", CreateLocation("D"), 1, TimeSpan.FromMinutes(15));
        
        route.AddCheckpoint(cp2);
        route.AddCheckpoint(cp1);

        Assert.Equal(2, route.Checkpoints.Count);
        Assert.Equal("First", route.Checkpoints[0].Name);
        Assert.Equal("Second", route.Checkpoints[1].Name);
    }

    [Fact]
    public void AddCheckpoint_WithDuplicateId_ShouldThrow()
    {
        var route = new Route(Guid.NewGuid(), "Test", CreateLocation("A"), CreateLocation("B"), 3);
        var checkpointId = Guid.NewGuid();
        
        var cp1 = new Checkpoint(checkpointId, "First", CreateLocation("C"), 1, TimeSpan.FromMinutes(15));
        var cp2 = new Checkpoint(checkpointId, "Duplicate", CreateLocation("D"), 2, TimeSpan.FromMinutes(30));
        
        route.AddCheckpoint(cp1);
        Assert.Throws<InvalidOperationException>(() => route.AddCheckpoint(cp2));
    }

    [Fact]
    public void AddCheckpoint_WithDuplicateSequence_ShouldThrow()
    {
        var route = new Route(Guid.NewGuid(), "Test", CreateLocation("A"), CreateLocation("B"), 3);
        
        var cp1 = new Checkpoint(Guid.NewGuid(), "First", CreateLocation("C"), 1, TimeSpan.FromMinutes(15));
        var cp2 = new Checkpoint(Guid.NewGuid(), "Second", CreateLocation("D"), 1, TimeSpan.FromMinutes(30));
        
        route.AddCheckpoint(cp1);
        Assert.Throws<InvalidOperationException>(() => route.AddCheckpoint(cp2));
    }

    [Fact]
    public void TotalDistance_WithoutCheckpoints_ShouldCalculateDirectDistance()
    {
        var origin = new SpaceLocation("A", "Sol", 0, 0, 0);
        var destination = new SpaceLocation("B", "Sol", 100, 0, 0);
        var route = new Route(Guid.NewGuid(), "Test", origin, destination, 1);

        Assert.Equal(100, route.TotalDistance());
    }

    [Fact]
    public void TotalDistance_WithCheckpoints_ShouldCalculateThroughCheckpoints()
    {
        var origin = new SpaceLocation("A", "Sol", 0, 0, 0);
        var destination = new SpaceLocation("B", "Sol", 100, 0, 0);
        var route = new Route(Guid.NewGuid(), "Test", origin, destination, 1);
        
        var checkpoint = new Checkpoint(
            Guid.NewGuid(),
            "Mid",
            new SpaceLocation("Mid", "Sol", 50, 0, 0),
            1,
            TimeSpan.FromMinutes(15));
        route.AddCheckpoint(checkpoint);

        // 0 -> 50 -> 100 = 100 total
        Assert.Equal(100, route.TotalDistance());
    }

    [Fact]
    public void EstimatedTotalDuration_ShouldSumCheckpointDurations()
    {
        var route = new Route(Guid.NewGuid(), "Test", CreateLocation("A"), CreateLocation("B"), 1);
        
        route.AddCheckpoint(new Checkpoint(Guid.NewGuid(), "CP1", CreateLocation("C"), 1, TimeSpan.FromMinutes(15)));
        route.AddCheckpoint(new Checkpoint(Guid.NewGuid(), "CP2", CreateLocation("D"), 2, TimeSpan.FromMinutes(30)));

        Assert.Equal(TimeSpan.FromMinutes(45), route.EstimatedTotalDuration());
    }

    [Fact]
    public void RemoveCheckpoint_ShouldRemoveExisting()
    {
        var route = new Route(Guid.NewGuid(), "Test", CreateLocation("A"), CreateLocation("B"), 1);
        var cpId = Guid.NewGuid();
        route.AddCheckpoint(new Checkpoint(cpId, "CP", CreateLocation("C"), 1, TimeSpan.FromMinutes(15)));
        
        route.RemoveCheckpoint(cpId);
        
        Assert.Empty(route.Checkpoints);
    }

    [Fact]
    public void RemoveCheckpoint_NotFound_ShouldThrow()
    {
        var route = new Route(Guid.NewGuid(), "Test", CreateLocation("A"), CreateLocation("B"), 1);
        Assert.Throws<InvalidOperationException>(() => route.RemoveCheckpoint(Guid.NewGuid()));
    }
}

public class ValueObjectTests
{
    [Fact]
    public void SpaceLocation_DistanceTo_ShouldCalculate3DDistance()
    {
        var a = new SpaceLocation("A", "Sol", 0, 0, 0);
        var b = new SpaceLocation("B", "Sol", 3, 4, 0);

        Assert.Equal(5, a.DistanceTo(b));
    }

    [Fact]
    public void Checkpoint_WithInvalidSequence_ShouldThrow()
    {
        var location = new SpaceLocation("Test", "Sol", 0, 0, 0);
        Assert.Throws<ArgumentException>(() =>
            new Checkpoint(Guid.NewGuid(), "CP", location, -1, TimeSpan.FromMinutes(15)));
    }

    [Fact]
    public void Cargo_Constructor_ShouldValidateInput()
    {
        var cargo = new Cargo("Medical Supplies", 100, true, false);
        
        Assert.Equal("Medical Supplies", cargo.Description);
        Assert.Equal(100, cargo.WeightKg);
        Assert.True(cargo.IsFragile);
        Assert.False(cargo.RequiresRefrigeration);
    }

    [Fact]
    public void Cargo_WithZeroWeight_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() =>
            new Cargo("Test", 0));
    }
}
