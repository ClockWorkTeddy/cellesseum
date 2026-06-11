using Celleseum.Data;
using Celleseum.Web;
using Celleseum.Web.Components;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddRazorPages();

builder.Services.AddOutputCache();

var host = builder.Configuration["DatabaseHost"] ?? "localhost";
var port = builder.Configuration["DatabasePort"] ?? "5432";
var database = builder.Configuration["Database"];
var dbUser = builder.Configuration["DatabaseUser"];
var dbPassword = builder.Configuration["DatabasePassword"];

var connectionString = $"Host={host};Port={port};Database={database};Username={dbUser};Password={dbPassword};Pooling=true";

builder.Services.AddDbContext<CellesseumDbContext>(options =>
    options.UseNpgsql(connectionString, npgsql => npgsql.EnableRetryOnFailure()));

builder.Services.AddIdentityCore<ApplicationUser>(options =>
{
    options.User.RequireUniqueEmail = true;
    options.Password.RequireDigit = true;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;
})
    .AddEntityFrameworkStores<CellesseumDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

// Authentication: Identity cookie + Google OAuth
var authenticationBuilder = builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = IdentityConstants.ApplicationScheme;
    options.DefaultChallengeScheme = IdentityConstants.ApplicationScheme;
    options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
});

authenticationBuilder.AddIdentityCookies();
authenticationBuilder.AddGoogle(options =>
{
    options.ClientId = builder.Configuration["Authentication:Google:ClientId"]!;
    options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"]!;
    options.CallbackPath = "/signin-google";
    options.SignInScheme = IdentityConstants.ExternalScheme;
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
    client.Timeout = TimeSpan.FromSeconds(60);
});

var app = builder.Build();

var forwardedHeadersOptions = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
    ForwardLimit = 2,
    RequireHeaderSymmetry = false
};
forwardedHeadersOptions.KnownNetworks.Clear();
forwardedHeadersOptions.KnownProxies.Clear();
app.UseForwardedHeaders(forwardedHeadersOptions);

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

app.UseStaticFiles();
app.MapStaticAssets();

app.MapRazorPages();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapDefaultEndpoints();

app.MapPost("/Account/Login/Local", async (HttpContext context, SignInManager<ApplicationUser> signInManager) =>
{
    var form = await context.Request.ReadFormAsync();
    var email = form["email"].ToString().Trim();
    var password = form["password"].ToString();

    if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
    {
        return Results.Redirect("/Account/Login?error=Email%20and%20password%20are%20required.");
    }

    var result = await signInManager.PasswordSignInAsync(email, password, isPersistent: true, lockoutOnFailure: false);
    if (!result.Succeeded)
    {
        return Results.Redirect("/Account/Login?error=Invalid%20email%20or%20password.");
    }

    return Results.Redirect("/");
})
.DisableAntiforgery();

app.MapPost("/Account/Register/Local", async (HttpContext context, UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager) =>
{
    var form = await context.Request.ReadFormAsync();
    var email = form["email"].ToString().Trim();
    var password = form["password"].ToString();
    var confirmPassword = form["confirmPassword"].ToString();

    if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
    {
        return Results.Redirect("/Account/Register?error=Email%20and%20password%20are%20required.");
    }

    if (!string.Equals(password, confirmPassword, StringComparison.Ordinal))
    {
        return Results.Redirect("/Account/Register?error=Passwords%20do%20not%20match.");
    }

    var user = new ApplicationUser
    {
        UserName = email,
        Email = email,
        EmailConfirmed = true
    };

    var createResult = await userManager.CreateAsync(user, password);
    if (!createResult.Succeeded)
    {
        var firstError = createResult.Errors.FirstOrDefault()?.Description ?? "Registration failed.";
        return Results.Redirect($"/Account/Register?error={Uri.EscapeDataString(firstError)}");
    }

    await signInManager.SignInAsync(user, isPersistent: true);
    return Results.Redirect("/");
})
.DisableAntiforgery();

// Google sign-in challenge endpoint
app.MapGet("/Account/Login/Google", (SignInManager<ApplicationUser> signInManager) =>
{
    var properties = signInManager.ConfigureExternalAuthenticationProperties(
        GoogleDefaults.AuthenticationScheme,
        "/signin-google-complete");

    return Results.Challenge(properties, [GoogleDefaults.AuthenticationScheme]);
});

// Google OAuth post-auth processing endpoint
app.MapGet("/signin-google-complete", async (SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager) =>
{
    var info = await signInManager.GetExternalLoginInfoAsync();
    if (info is null)
    {
        return Results.Redirect("/Account/Login?error=Google%20login%20failed.");
    }

    var signInResult = await signInManager.ExternalLoginSignInAsync(
        info.LoginProvider,
        info.ProviderKey,
        isPersistent: true,
        bypassTwoFactor: true);

    if (signInResult.Succeeded)
    {
        return Results.Redirect("/");
    }

    var email = info.Principal.FindFirstValue(ClaimTypes.Email);
    if (string.IsNullOrWhiteSpace(email))
    {
        return Results.Redirect("/Account/Login?error=Unable%20to%20read%20email%20from%20Google.");
    }

    var user = await userManager.FindByEmailAsync(email);
    if (user is null)
    {
        user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true
        };

        var createResult = await userManager.CreateAsync(user);
        if (!createResult.Succeeded)
        {
            var firstError = createResult.Errors.FirstOrDefault()?.Description ?? "Failed to create account.";
            return Results.Redirect($"/Account/Login?error={Uri.EscapeDataString(firstError)}");
        }
    }

    var existingLogins = await userManager.GetLoginsAsync(user);
    var hasGoogleLogin = existingLogins.Any(x => x.LoginProvider == info.LoginProvider && x.ProviderKey == info.ProviderKey);
    if (!hasGoogleLogin)
    {
        var addLoginResult = await userManager.AddLoginAsync(user, info);
        if (!addLoginResult.Succeeded)
        {
            var firstError = addLoginResult.Errors.FirstOrDefault()?.Description ?? "Failed to link Google account.";
            return Results.Redirect($"/Account/Login?error={Uri.EscapeDataString(firstError)}");
        }
    }

    await signInManager.SignInAsync(user, isPersistent: true);
    return Results.Redirect("/");
});

// Logout: sign out of Identity application cookie and redirect to home
app.MapGet("/Account/Logout", async (HttpContext context) =>
{
    await context.SignOutAsync(IdentityConstants.ApplicationScheme);
    return Results.Redirect("/");
});

await app.RunAsync();
