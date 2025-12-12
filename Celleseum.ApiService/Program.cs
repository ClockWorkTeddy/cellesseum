using System;
using System.Drawing;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddProblemDetails();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/turn", () =>
{
    Random rnd = new();
    var numbers = new int[100];
    for (int i = 0; i < 100; i++)
    {
        numbers[i] = rnd.Next(1, 101);
    }
    var numbersSet = new NumberSet(numbers);
    return numbersSet;
})
.WithName("NextTurn");

app.MapDefaultEndpoints();

app.Run();

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
