using Configuration.Core.Abstractions;
using Configuration.Core.Models;
using Configuration.Infrastructure.Mongo;
using Microsoft.OpenApi.Models;
using MongoDB.Driver;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Admin API", Version = "v1" });
});

// CORS for Admin Web UI
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:5173", "http://localhost:3000")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// Basic DI for repository using connection string from configuration
builder.Services.AddSingleton<IConfigurationRepository>(sp =>
{
    var cs = builder.Configuration.GetConnectionString("Mongo") ?? "mongodb://localhost:27017";
    var logger = sp.GetRequiredService<ILogger<ResilientMongoConfigurationRepository>>();
    return new ResilientMongoConfigurationRepository(cs, logger: logger);
});

var app = builder.Build();

// Enable CORS
app.UseCors();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Endpoints scoped by applicationName
app.MapGet("/{applicationName}/configs", async (string applicationName, IConfigurationRepository repo, string? name = null, bool includeInactive = false, CancellationToken ct = default) =>
{
    var items = includeInactive 
        ? await repo.GetAllEntriesAsync(applicationName, ct)
        : await repo.GetActiveEntriesAsync(applicationName, ct);
    
    if (!string.IsNullOrWhiteSpace(name))
    {
        items = items.Where(x => x.Name.Contains(name, StringComparison.OrdinalIgnoreCase)).ToList();
    }
    return Results.Ok(items);
});

app.MapPost("/{applicationName}/configs", async (string applicationName, ConfigurationEntry entry, IConfigurationRepository repo, CancellationToken ct) =>
{
    if (string.IsNullOrWhiteSpace(entry.Name)) return Results.BadRequest("Name is required");
    var toCreate = new ConfigurationEntry
    {
        Id = entry.Id,
        ApplicationName = applicationName,
        Name = entry.Name,
        Type = entry.Type,
        Value = entry.Value,
        IsActive = entry.IsActive,
        UpdatedAtUtc = DateTime.UtcNow
    };
    
    try
    {
        var created = await repo.CreateAsync(toCreate, ct);
        return Results.Created($"/{applicationName}/configs/{created.Id}", created);
    }
    catch (MongoDB.Driver.MongoWriteException ex) when (ex.WriteError?.Category == MongoDB.Driver.ServerErrorCategory.DuplicateKey)
    {
        return Results.Conflict($"Configuration with name '{entry.Name}' already exists for application '{applicationName}'");
    }
});

app.MapPut("/{applicationName}/configs/{id}", async (string applicationName, string id, ConfigurationEntry entry, IConfigurationRepository repo, CancellationToken ct) =>
{
    var toUpdate = new ConfigurationEntry
    {
        Id = id,
        ApplicationName = applicationName,
        Name = entry.Name,
        Type = entry.Type,
        Value = entry.Value,
        IsActive = entry.IsActive,
        UpdatedAtUtc = DateTime.UtcNow
    };
    var updated = await repo.UpdateAsync(id, applicationName, toUpdate, ct);
    if (updated is not null)
    {
        return Results.Ok(updated);
    }
    return Results.NotFound();
});

app.MapPatch("/{applicationName}/configs/{id}/activate", async (string applicationName, string id, bool isActive, IConfigurationRepository repo, CancellationToken ct) =>
{
    var ok = await repo.SetActiveAsync(id, applicationName, isActive, ct);
    if (ok)
    {
        return Results.NoContent();
    }
    return Results.NotFound();
});

app.Run();
