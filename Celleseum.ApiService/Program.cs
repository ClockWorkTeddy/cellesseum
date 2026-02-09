using Celleseum.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;
using MapProcessing;
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

app.MapGet("/turn/{size}", async (int size, CellesseumDbContext db, HttpContext httpContext) =>
{
    var processor = new Proccessor();
    var data = processor.ProcessMap(size);

    // Prefer X-Forwarded-For if present (first IP), otherwise use connection address
    var xff = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
    var clientIp = !string.IsNullOrWhiteSpace(xff)
        ? xff.Split(',')[0].Trim()
        : httpContext.Connection.RemoteIpAddress?.ToString();

    return data;
})
.WithName("NextTurn");

app.MapDefaultEndpoints();

app.Run();

string TrimClientIp(string? ipAddress)
{
    if (string.IsNullOrWhiteSpace(ipAddress))
    {
        return "";
    }

    var segments = ipAddress.Split('.');

    return segments.Last();
}

record NumberSet
{
    [JsonConstructor]
    public NumberSet(int[] numbers)
    {
        Numbers = numbers ?? Array.Empty<int>();
    }

    public int[] Numbers { get; init; }
    public int Average => Numbers.Length > 0 ? Numbers.Sum() / Numbers.Length : 0;
}
