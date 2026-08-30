using Celleseum.ApiService;
using Celleseum.Data;
using MapProcessing;
using MessagePack;
using Microsoft.EntityFrameworkCore;
var builder = WebApplication.CreateBuilder(args);

var host = builder.Configuration["DatabaseHost"] ?? "localhost";
var port = builder.Configuration["DatabasePort"] ?? "5432";
var database = builder.Configuration["Database"];
var dbUser = builder.Configuration["DatabaseUser"];
var dbPassword = builder.Configuration["DatabasePassword"];

var connectionString = $"Host={host};Port={port};Database={database};Username={dbUser};Password={dbPassword};Pooling=true";

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddProblemDetails();
builder.Services.AddDbContext<CellesseumDbContext>(options =>
    options.UseNpgsql(connectionString, npgsql => npgsql.EnableRetryOnFailure()));
builder.Services.AddSingleton<Proccessor>();
builder.Services.AddSingleton<AuxService>();

builder.Services.AddOpenApi();

var app = builder.Build();

// Apply EF Core migrations at startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CellesseumDbContext>();
    await db.Database.MigrateAsync();
}

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/turn/{width}/{height}", (int width, int height, string? mode, int terms = 3000, int smartGrazer = 0) =>
{
    var map = new Map(width, height);

    var selectedMode = string.Equals(mode, "mutation", StringComparison.OrdinalIgnoreCase)
        ? Proccessor.GameMode.Mutation
        : Proccessor.GameMode.Simple;

    return Proccessor.ProcessMapFrames(map, terms, selectedMode, smartGrazer != 0);
})
.WithName("NextTurn");

app.MapGet("/turn/{width}/{height}/download", (int width, int height, string? mode, int terms = 3000, int smartGrazer = 0) =>
{
    var map = new Map(width, height);

    var selectedMode = string.Equals(mode, "mutation", StringComparison.OrdinalIgnoreCase)
        ? Proccessor.GameMode.Mutation
        : Proccessor.GameMode.Simple;

    var export = new MapExport
    {
        Width = width,
        Height = height,
        Mode = selectedMode.ToString().ToLowerInvariant(),
        Terms = terms,
        Frames = Proccessor.ProcessMapFrames(map, terms, selectedMode, smartGrazer != 0).ToList()
    };

    var options = MessagePackSerializerOptions.Standard
        .WithCompression(MessagePackCompression.Lz4BlockArray);

    var bytes = MessagePackSerializer.Serialize(export, options);

    return Results.File(
        bytes,
        "application/x-msgpack",
        $"map-{width}x{height}-{DateTime.UtcNow:yyyyMMddHHmmss}.mpk");
})
.WithName("DownloadTurn");

app.MapDefaultEndpoints();

await app.RunAsync();
