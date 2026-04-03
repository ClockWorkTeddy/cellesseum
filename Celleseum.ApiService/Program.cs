using Celleseum.ApiService;
using Celleseum.Data;
using MapProcessing;
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

app.MapGet("/turn/{size}", async (int size, CellesseumDbContext db, HttpContext httpContext) =>
{
    var processor = new Proccessor();
    var map = new Map(size);
    var data = processor.ProcessMap(map);

    return data;
})
.WithName("NextTurn");

app.MapDefaultEndpoints();

await app.RunAsync();