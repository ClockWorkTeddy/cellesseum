using Celleseum.Data;
using Celleseum.Web;
using Celleseum.Web.Components;
using Celleseum.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddRazorPages(options =>
{
    options.RootDirectory = "/Components/Pages";
});

builder.Services.AddOutputCache();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddFixedWindowLimiter("auth-login", limiterOptions =>
    {
        limiterOptions.PermitLimit = 12;
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.QueueLimit = 0;
    });

    options.AddFixedWindowLimiter("auth-register", limiterOptions =>
    {
        limiterOptions.PermitLimit = 5;
        limiterOptions.Window = TimeSpan.FromMinutes(5);
        limiterOptions.QueueLimit = 0;
    });

    options.AddFixedWindowLimiter("auth-external", limiterOptions =>
    {
        limiterOptions.PermitLimit = 20;
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.QueueLimit = 0;
    });
});

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
    options.SignIn.RequireConfirmedEmail = true;
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
builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection(SmtpSettings.SectionName));
builder.Services.AddScoped<IAccountEmailSender, SmtpAccountEmailSender>();

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
app.UseRateLimiter();

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
    if (result.IsNotAllowed)
    {
        return Results.Redirect("/Account/Login?error=Please%20confirm%20your%20email%20before%20signing%20in.");
    }

    if (!result.Succeeded)
    {
        return Results.Redirect("/Account/Login?error=Invalid%20email%20or%20password.");
    }

    return Results.Redirect("/");
})
.DisableAntiforgery()
.RequireRateLimiting("auth-login");

app.MapPost("/Account/Register/Local", async (HttpContext context, UserManager<ApplicationUser> userManager, IAccountEmailSender emailSender, ILogger<Program> logger) =>
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
        EmailConfirmed = false
    };

    var createResult = await userManager.CreateAsync(user, password);
    if (!createResult.Succeeded)
    {
        var firstError = createResult.Errors.FirstOrDefault()?.Description ?? "Registration failed.";
        return Results.Redirect($"/Account/Register?error={Uri.EscapeDataString(firstError)}");
    }

    // Send confirmation email after user is created
    try
    {
        var rawToken = await userManager.GenerateEmailConfirmationTokenAsync(user);
        var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(rawToken));
        var confirmUrl = $"{context.Request.Scheme}://{context.Request.Host}/Account/ConfirmEmail?userId={Uri.EscapeDataString(user.Id)}&token={Uri.EscapeDataString(encodedToken)}";

        await emailSender.SendEmailConfirmationAsync(email, confirmUrl);
    }
    catch (Exception ex)
    {
        // Email sending failed, rollback by deleting the user
        logger.LogError(ex, "Failed to send confirmation email for {Email}. Deleting user account.", email);
        await userManager.DeleteAsync(user);
        return Results.Redirect($"/Account/Register?error={Uri.EscapeDataString("Failed to send confirmation email. Please try again later.")}");
    }

    return Results.Redirect("/Account/Login?message=Registration%20successful.%20Please%20confirm%20your%20email%20before%20signing%20in.");
})
.DisableAntiforgery()
.RequireRateLimiting("auth-register");

app.MapGet("/Account/ConfirmEmail", async (string? userId, string? token, UserManager<ApplicationUser> userManager) =>
{
    if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(token))
    {
        return Results.Redirect("/Account/Login?error=Invalid%20email%20confirmation%20link.");
    }

    var user = await userManager.FindByIdAsync(userId);
    if (user is null)
    {
        return Results.Redirect("/Account/Login?error=Invalid%20email%20confirmation%20link.");
    }

    string decodedToken;
    try
    {
        decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(token));
    }
    catch (FormatException)
    {
        return Results.Redirect("/Account/Login?error=Invalid%20email%20confirmation%20token.");
    }

    var confirmResult = await userManager.ConfirmEmailAsync(user, decodedToken);
    if (!confirmResult.Succeeded)
    {
        var firstError = confirmResult.Errors.FirstOrDefault()?.Description ?? "Email confirmation failed.";
        return Results.Redirect($"/Account/Login?error={Uri.EscapeDataString(firstError)}");
    }

    return Results.Redirect("/Account/Login?message=Email%20confirmed.%20You%20can%20sign%20in%20now.");
})
.RequireRateLimiting("auth-external");

// Google sign-in challenge endpoint
app.MapGet("/Account/Login/Google", (SignInManager<ApplicationUser> signInManager) =>
{
    var properties = signInManager.ConfigureExternalAuthenticationProperties(
        GoogleDefaults.AuthenticationScheme,
        "/signin-google-complete");

    return Results.Challenge(properties, [GoogleDefaults.AuthenticationScheme]);
})
.RequireRateLimiting("auth-external");

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
        return Results.Redirect("/menu");
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
    return Results.Redirect("/menu");
})
.RequireRateLimiting("auth-external");

// Logout: sign out and redirect to main page
app.MapGet("/Account/Logout", async (HttpContext context, SignInManager<ApplicationUser> signInManager, string? returnUrl) =>
{
    await signInManager.SignOutAsync();
    await context.SignOutAsync(IdentityConstants.ApplicationScheme);
    await context.SignOutAsync(IdentityConstants.ExternalScheme);

    var target = "/?loggedOut=true";
    if (!string.IsNullOrWhiteSpace(returnUrl) && Uri.IsWellFormedUriString(returnUrl, UriKind.Relative) && returnUrl.StartsWith('/'))
    {
        target = returnUrl.Contains('?')
            ? $"{returnUrl}&loggedOut=true"
            : $"{returnUrl}?loggedOut=true";
    }

    return Results.Redirect(target);
});

await app.RunAsync();
