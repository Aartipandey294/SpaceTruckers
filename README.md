#  SpaceTruckers Inc. - The Great Galactic Delivery Race

A .NET 8 backend system for managing interplanetary deliveries, built with **Domain-Driven Design (DDD)**, **Event Sourcing patterns**, and **Clean Architecture principles**.

---

##  Table of Contents

- [Prerequisites](#prerequisites)
- [Getting Started](#getting-started)
  - [Clone the Repository](#clone-the-repository)
  - [Build the Solution](#build-the-solution)
  - [Run the Application](#run-the-application)
  - [Access the API](#access-the-api)
- [Running Tests](#running-tests)
- [Project Structure](#project-structure)
- [API Endpoints](#api-endpoints)
- [Design Decisions](#design-decisions)
- [Assumptions](#assumptions)
- [Technologies Used](#technologies-used)

---

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (version 8.0 or later)
- IDE: Visual Studio 2022, VS Code, or JetBrains Rider

Verify installation:
```bash
dotnet --version
# Should output: 8.0.x
```

---

## Getting Started

### Clone the Repository

```bash
git clone https://github.com/YOUR_USERNAME/SpaceTruckers.git
cd SpaceTruckers
```

### Build the Solution

```bash
# Restore NuGet packages and build
dotnet restore
dotnet build
```

**Expected output:**
```
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

### Run the Application

```bash
dotnet run --project src/SpaceTruckers.Api
```

**Expected output:**
```
[12:00:00 INF] Starting SpaceTruckers Delivery API
[12:00:00 INF] Seeding demo data...
[12:00:00 INF] Seeded 2 drivers: Han Solo, Starbuck
[12:00:00 INF] Seeded 2 vehicles: Millennium Falcon, Express Runner
[12:00:00 INF] Seeded route 'Earth-Mars Express' with 2 checkpoints
[12:00:00 INF] SpaceTruckers Delivery API started successfully
```

### Access the API

| Interface | URL |
|-----------|-----|
| **Swagger UI** | https://localhost:51533/swagger |


The Swagger UI provides interactive documentation where you can test all endpoints.

---

## Running Tests

### Run All Tests

```bash
dotnet test
```

**Expected output:**
```
Passed!  - Failed:     0, Passed:    76, Skipped:     0, Total:    76
```

### Run Tests with Detailed Output

```bash
dotnet test --logger "console;verbosity=detailed"
```

### Run Specific Test Project

```bash
# Domain layer tests (entities, aggregates, value objects)
dotnet test tests/SpaceTruckers.Domain.Tests

# Application layer tests (services, concurrency)
dotnet test tests/SpaceTruckers.Application.Tests
```

### Run Tests by Category

```bash
# Run only Trip-related tests
dotnet test --filter "Trip"

# Run only concurrency tests
dotnet test --filter "Concurrency"
```

### Test Coverage Summary

| Test Project | Tests | Coverage Area |
|--------------|-------|---------------|
| **Domain.Tests** | 53 | Trip aggregate, entities, value objects, event sourcing |
| **Application.Tests** | 23 | Services, orchestration, concurrency, thread safety |
| **Total** | **76** | |

---

## Project Structure

```
SpaceTruckers/
├── src/
│   ├── SpaceTruckers.Domain/           # Core business logic (no external dependencies)
│   │   ├── Aggregates/                  # Trip aggregate root
│   │   ├── Entities/                    # Driver, Vehicle, Route
│   │   ├── Events/                      # Domain events (TripStarted, CheckpointReached, etc.)
│   │   ├── ValueObjects/                # SpaceLocation, Checkpoint, Incident types
│   │   ├── Exceptions/                  # Domain-specific exceptions
│   │   └── Ports/                       # Repository interfaces
│   │
│   ├── SpaceTruckers.Application/      # Use cases & orchestration
│   │   ├── Services/                    # TripService, DriverService, etc.
│   │   └── DTOs/                        # Request/Response objects
│   │
│   ├── SpaceTruckers.Infrastructure/   # External concerns implementation
│   │   ├── Repositories/                # In-memory repository implementations
│   │   ├── Seed/                        # Demo data seeding
│   │   └── Services/                    # Clock, ID generator
│   │
│   └── SpaceTruckers.Api/              # REST API layer
│       ├── Controllers/                 # Thin API controllers
│       └── Program.cs                   # DI configuration, middleware, Serilog
│
├── tests/
│   ├── SpaceTruckers.Domain.Tests/     # Unit tests for domain layer
│   └── SpaceTruckers.Application.Tests/ # Unit tests for application layer
│
│
├── .editorconfig                        # C# code style rules
└── SpaceTruckers.sln                   # Solution file
```

---

## API Endpoints

### Trips (Core Workflow)

| Method | Endpoint | Description |
|--------|----------|-------------|
| `POST` | `/api/Trips` | Create a new trip |
| `GET` | `/api/Trips/{id}` | Get trip by ID |
| `GET` | `/api/Trips/active` | Get all active trips |
| `POST` | `/api/Trips/{id}/checkpoints` | Record checkpoint reached |
| `POST` | `/api/Trips/{id}/incidents` | Record an incident |
| `POST` | `/api/Trips/{id}/incidents/resolve` | Resolve an incident |
| `POST` | `/api/Trips/{id}/complete` | Complete the trip |
| `POST` | `/api/Trips/{id}/cancel` | Cancel the trip |
| `GET` | `/api/Trips/{id}/summary` | Get trip summary |

### Supporting Resources

| Method | Endpoint | Description |
|--------|----------|-------------|
| `POST` | `/api/Drivers` | Create driver |
| `GET` | `/api/Drivers` | List all drivers |
| `GET` | `/api/Drivers/available` | List available drivers |
| `POST` | `/api/Vehicles` | Create vehicle |
| `GET` | `/api/Vehicles` | List all vehicles |
| `GET` | `/api/Vehicles/available` | List available vehicles |
| `POST` | `/api/Routes` | Create route with checkpoints |
| `GET` | `/api/Routes` | List all routes |

### Example: Complete Trip Workflow

```bash
# 1. Create a trip
POST /api/Trips
{
  "driverId": "...",
  "vehicleId": "...",
  "routeId": "...",
  "cargoDescription": "Medical supplies"
}
# Response: { "id": "...", "version": 1, "status": "InProgress" }

# 2. Record checkpoint (use version from previous response)
POST /api/Trips/{id}/checkpoints
{
  "checkpointId": "...",
  "expectedVersion": 1
}
# Response: { "version": 2, "checkpointsReached": 1 }

# 3. Complete trip
POST /api/Trips/{id}/complete
{
  "expectedVersion": 2
}
# Response: Trip summary with duration, incidents, etc.
```

---

## Design Decisions

### 1. Domain-Driven Design with Clean Architecture

**Decision:** Strict separation into Domain → Application → Infrastructure/API layers with dependencies pointing inward.

**Rationale:**
- Domain layer has zero external dependencies (pure C#)
- Business rules are isolated and testable
- Infrastructure can be swapped without affecting domain logic
- Follows SOLID principles

### 2. Event Sourcing Pattern for Trip Aggregate

**Decision:** Trip state is derived from a sequence of domain events (TripStarted, CheckpointReached, IncidentOccurred, etc.) using Apply/When pattern.

**Rationale:**
- Complete audit trail of "what happened during each trip"
- State can be rehydrated from events
- Supports idempotency (duplicate events are safely ignored)
- Natural fit for the delivery tracking domain

### 3. Optimistic Concurrency Control

**Decision:** Version-based concurrency using `expectedVersion` parameter on all mutations.

**Rationale:**
- Handles concurrent updates without pessimistic locks
- Scales better than database locks
- Client receives 409 Conflict and can retry with fresh version
- Prevents lost updates in distributed scenarios

### 4. Trip as the Aggregate Root

**Decision:** Trip encapsulates all delivery events (checkpoints, incidents) rather than having separate aggregates.

**Rationale:**
- Single transactional boundary for the entire delivery lifecycle
- Enforces invariants (e.g., cannot complete with unresolved critical incidents)
- Events naturally belong to the trip context
- Simpler consistency model

### 5. Global Exception Handling with RFC 7807 ProblemDetails

**Decision:** All errors return standardised RFC 7807 ProblemDetails responses.

**Rationale:**
- Consistent error format across all endpoints
- Machine-readable error codes for client handling
- Includes traceId for debugging
- Industry standard for REST APIs

### 6. Structured Logging with Serilog

**Decision:** Serilog with structured properties for key events.

**Rationale:**
- Searchable logs (filter by TripId, DriverId, etc.)
- Production-ready (can add Seq, Elasticsearch sinks)
- Performance-friendly (message templates, not string concatenation)

### 7. One Class Per File

**Decision:** All repositories, DTOs, and services split into separate files.

**Rationale:**
- Reduces merge conflicts
- Improves code navigation
- Clear responsibility boundaries

---

## Assumptions

### Business Assumptions

1. **Resource Exclusivity:** Drivers and vehicles are exclusively assigned to one active trip at a time. No handoff or sharing scenarios.

2. **Incident Blocking:** Only **Critical** severity incidents block trip completion. Minor, Moderate, and Major incidents can remain unresolved.

3. **Checkpoint Order:** Checkpoints can be reached in any order. The system tracks which checkpoints were reached but doesn't enforce sequence.

4. **Time Handling:** All timestamps use UTC via `DateTimeOffset` for consistency across time zones.

### Technical Assumptions

5. **Single Instance:** The system is designed for single-instance deployment. Multi-instance deployment would require distributed locking or database-level concurrency.

6. **In-Memory Storage:** Data resets on application restart. This is intentional for the demo; production would use persistent storage (e.g., PostgreSQL, SQL Server).

7. **Demo Data:** The application seeds demo data (2 drivers, 2 vehicles, 1 route) on startup in Development mode for easier testing.

8. **API Versioning:** Not implemented for this demo. Production API would include versioning (e.g., `/api/v1/trips`).

---

## Technologies Used

| Technology | Purpose |
|------------|---------|
| .NET 8 | Runtime and SDK |
| ASP.NET Core | Web API framework |
| Serilog | Structured logging |
| Swashbuckle | Swagger/OpenAPI documentation |
| xUnit | Testing framework |
| FluentAssertions | Test assertion library |

---

## Quick Start Summary

```bash
# 1. Clone
git clone https://github.com/YOUR_USERNAME/SpaceTruckers.git
cd SpaceTruckers

# 2. Build
dotnet build

# 3. Test
dotnet test

# 4. Run
dotnet run --project src/SpaceTruckers.Api

# 5. Open Swagger
# Navigate to: http://localhost:5000/swagger
```

---

### Incident Types

| Value | Incident Type        |
| ----: | -------------------- |
|     0 | AsteroidField        |
|     1 | CosmicStorm          |
|     2 | EmergencyMaintenance |
|     3 | PirateEncounter      |
|     4 | NavigationError      |
|     5 | CargoShift           |
|     6 | FuelLeak             |
|     7 | CommunicationFailure |
|     8 | Other                |

### Severity Levels

| Value | Severity Level |
| ----: | -------------- |
|     0 | Minor          |
|     1 | Moderate       |
|     2 | Major          |
|     3 | Critical       |
