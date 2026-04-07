using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PortalCameras.Data;
using PortalCameras.Endpoints;
using PortalCameras.Models;
using PortalCameras.Services;
using Scalar.AspNetCore;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.AspNetCore", Serilog.Events.LogEventLevel.Warning)
    .WriteTo.Console()
    .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

try
{
    Log.Information("Démarrage de PortalCameras API");

    var builder = WebApplication.CreateBuilder(args);
    builder.Host.UseSerilog();

    // CORS
    var allowedOriginsRaw = builder.Configuration["AllowedOrigins"] ?? "http://localhost:5173";
    var allowedOrigins = allowedOriginsRaw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy =>
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials());
    });

    // Auth cookies
    builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
        .AddCookie(options =>
        {
            options.LoginPath = "/api/login";
            options.ExpireTimeSpan = TimeSpan.FromHours(24);
            options.SlidingExpiration = true;
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
            options.Cookie.SameSite = SameSiteMode.Strict;
            options.Events = new CookieAuthenticationEvents
            {
                OnRedirectToLogin = ctx =>
                {
                    ctx.Response.StatusCode = 401;
                    return Task.CompletedTask;
                }
            };
        });

    builder.Services.AddAuthorization(options =>
        options.AddPolicy("RequireAuth", policy => policy.RequireAuthenticatedUser()));

    // Rate limiting — login : 5 tentatives / 10 min par IP
    builder.Services.AddRateLimiter(options =>
    {
        options.AddPolicy("login", httpContext =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 5,
                    Window = TimeSpan.FromMinutes(10),
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 0
                }));
        options.RejectionStatusCode = 429;
    });

    // YARP
    builder.Services.AddReverseProxy()
        .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

    // Cache mémoire
    builder.Services.AddMemoryCache();

    // OpenAPI + Scalar
    builder.Services.AddOpenApi();

    // Entity Framework
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

    // Services
    builder.Services.Configure<List<CameraConfig>>(builder.Configuration.GetSection("Cameras"));
    builder.Services.AddScoped<PingService>();
    builder.Services.AddScoped<IOService>();
    builder.Services.AddScoped<DateService>();
    builder.Services.AddScoped<DiscordService>();
    builder.Services.AddScoped<DetectThingsService>();

    var app = builder.Build();

    // Créer la base et la table si elles n'existent pas
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.EnsureCreated();
    }

    app.UseCors();
    app.UseRateLimiter();
    app.UseAuthentication();
    app.UseAuthorization();

    // Scalar
    app.MapOpenApi();
    app.MapScalarApiReference();

    // Endpoints
    app.MapAuthEndpoints();
    app.MapCameraEndpoints();
    app.MapImageEndpoints();
    app.MapFavouriteEndpoints();

    // YARP
    app.MapReverseProxy().RequireAuthorization("RequireAuth");

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application crash");
}
finally
{
    Log.CloseAndFlush();
}
