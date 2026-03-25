using Celleseum.Web;
using Celleseum.Web.Components;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddOutputCache();

// Authentication: cookie + Google OAuth
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
})
.AddCookie()
.AddGoogle(options =>
{
    options.ClientId = builder.Configuration["Authentication:Google:ClientId"]!;
    options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"]!;
});

builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();

// Forward client IP from incoming request to outgoing API calls
builder.Services.AddHttpContextAccessor();

// Prefer explicit URL + port for container-to-container traffic.
// Allows override via config/env: Api:BaseAddress or Api__BaseAddress
builder.Services.AddHttpClient<MapClient>(client =>
{
    var configured = builder.Configuration["Api:BaseAddress"];
    client.BaseAddress = new Uri(configured ?? "http://apiservice:8080");
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.UseOutputCache();

app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapDefaultEndpoints();

// Login: challenge Google and redirect back to home
app.MapGet("/Account/Login", () =>
    Results.Challenge(
        new AuthenticationProperties { RedirectUri = "/" },
        [GoogleDefaults.AuthenticationScheme]));

// Logout: sign out of cookies and redirect to home
app.MapGet("/Account/Logout", async (HttpContext context) =>
{
    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Redirect("/");
});

app.Run();
