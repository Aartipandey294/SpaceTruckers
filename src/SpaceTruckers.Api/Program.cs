using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using SpaceTruckers.Application.Services;
using SpaceTruckers.Domain.Exceptions;
using SpaceTruckers.Domain.Ports;
using SpaceTruckers.Infrastructure.Repositories;
using SpaceTruckers.Infrastructure.Seed;
using SpaceTruckers.Infrastructure.Services;

// Configure Serilog early for startup logging
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.Hosting.Lifetime", Serilog.Events.LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

try
{
    Log.Information("Starting SpaceTruckers Delivery API");

    var builder = WebApplication.CreateBuilder(args);

    // Use Serilog for all logging
    builder.Host.UseSerilog();

    // Add Problem Details for RFC 7807 compliant error responses
    builder.Services.AddProblemDetails(options =>
    {
        options.CustomizeProblemDetails = context =>
        {
            context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
        };
    });

    // Add services to the container
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new()
        {
            Title = "SpaceTruckers Delivery API",
            Version = "v1",
            Description = "The Great Galactic Delivery Race - Backend API for managing interplanetary deliveries"
        });
    });

    // Register Infrastructure services (thread-safe singletons for in-memory storage)
    // Note: IEventStore is used internally by InMemoryTripRepository for event sourcing
    builder.Services.AddSingleton<IEventStore, InMemoryEventStore>();
    builder.Services.AddSingleton<ITripRepository, InMemoryTripRepository>();
    builder.Services.AddSingleton<IDriverRepository, InMemoryDriverRepository>();
    builder.Services.AddSingleton<IVehicleRepository, InMemoryVehicleRepository>();
    builder.Services.AddSingleton<IRouteRepository, InMemoryRouteRepository>();

    // Register Infrastructure utilities
    builder.Services.AddSingleton<IClock, SystemClock>();
    builder.Services.AddSingleton<IIdGenerator, GuidGenerator>();

    // Register Application services
    builder.Services.AddScoped<TripService>();
    builder.Services.AddScoped<DriverService>();
    builder.Services.AddScoped<VehicleService>();
    builder.Services.AddScoped<RouteService>();

    // Add CORS for development
    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
    });

    var app = builder.Build();

    // Global exception handling middleware
    app.UseExceptionHandler("/error");

    // Error endpoint for RFC 7807 ProblemDetails responses
    app.Map("/error", (HttpContext context) =>
    {
        var exceptionFeature = context.Features.Get<IExceptionHandlerFeature>();
        var exception = exceptionFeature?.Error;

        var problemDetails = exception switch
        {
            DomainInvariantViolationException e => new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Domain Invariant Violation",
                Detail = e.Message,
                Type = "https://tools.ietf.org/html/rfc7807",
                Extensions = { ["code"] = "DOMAIN_INVARIANT_VIOLATION" }
            },
            InvalidTripStateException e => new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid Trip State",
                Detail = e.Message,
                Type = "https://tools.ietf.org/html/rfc7807",
                Extensions = { ["code"] = "INVALID_TRIP_STATE", ["currentState"] = e.CurrentState }
            },
            ConcurrencyConflictException e => new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Concurrency Conflict",
                Detail = e.Message,
                Type = "https://tools.ietf.org/html/rfc7807",
                Extensions = 
                { 
                    ["code"] = "CONCURRENCY_CONFLICT",
                    ["expectedVersion"] = e.ExpectedVersion,
                    ["actualVersion"] = e.ActualVersion
                }
            },
            EntityNotFoundException e => new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Entity Not Found",
                Detail = e.Message,
                Type = "https://tools.ietf.org/html/rfc7807",
                Extensions = { ["code"] = "ENTITY_NOT_FOUND", ["entityType"] = e.EntityType }
            },
            ArgumentException e => new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Validation Error",
                Detail = e.Message,
                Type = "https://tools.ietf.org/html/rfc7807",
                Extensions = { ["code"] = "VALIDATION_ERROR" }
            },
            _ => new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Internal Server Error",
                Detail = app.Environment.IsDevelopment() ? exception?.Message : "An unexpected error occurred",
                Type = "https://tools.ietf.org/html/rfc7807",
                Extensions = { ["code"] = "INTERNAL_ERROR" }
            }
        };

        problemDetails.Extensions["traceId"] = context.TraceIdentifier;

        Log.Error(exception, "Unhandled exception occurred. TraceId: {TraceId}", context.TraceIdentifier);

        return Results.Problem(problemDetails);
    });

    // Configure the HTTP request pipeline
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    // Add request logging
    app.UseSerilogRequestLogging(options =>
    {
        options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
        {
            diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
            diagnosticContext.Set("UserAgent", httpContext.Request.Headers["User-Agent"].ToString());
        };
    });

    app.UseCors();
    app.UseAuthorization();
    
    // Redirect root to Swagger
    app.MapGet("/", () => Results.Redirect("/swagger"));
    
    app.MapControllers();

    // Seed demo data only in development
    if (app.Environment.IsDevelopment())
    {
        await app.Services.SeedDemoDataAsync();
    }

    Log.Information("SpaceTruckers Delivery API started successfully");
    
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
