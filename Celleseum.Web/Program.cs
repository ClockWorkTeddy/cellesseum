using Celleseum.Web;
using Celleseum.Web.Components;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddOutputCache();

// Forward client IP from incoming request to outgoing API calls
builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<ForwardClientIpHandler>();

// Prefer explicit URL + port for container-to-container traffic.
// Allows override via config/env: Api:BaseAddress or Api__BaseAddress
builder.Services.AddHttpClient<WeatherApiClient>(client =>
{
    var configured = builder.Configuration["Api:BaseAddress"];
    client.BaseAddress = new Uri(configured ?? "http://apiservice:8080");
})
.AddHttpMessageHandler<ForwardClientIpHandler>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseAntiforgery();

app.UseOutputCache();

app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapDefaultEndpoints();

app.Run();
