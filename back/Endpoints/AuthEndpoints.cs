using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;

namespace PortalCameras.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        app.MapPost("/api/login", async (HttpContext ctx, IConfiguration config, ILogger<Program> logger) =>
        {
            var ip = ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var form = await ctx.Request.ReadFormAsync();
            var password = form["password"].ToString();
            var validPassword = config["Authentication:Password"] ?? string.Empty;

            if (password != validPassword)
            {
                logger.LogWarning("Tentative de login échouée depuis {Ip}", ip);
                return Results.Unauthorized();
            }

            var claims = new[] { new Claim(ClaimTypes.Name, "user") };
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await ctx.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
            logger.LogInformation("Login réussi depuis {Ip}", ip);
            return Results.Ok(new { success = true });
        }).AllowAnonymous().RequireRateLimiting("login");

        app.MapPost("/api/logout", async (HttpContext ctx, ILogger<Program> logger) =>
        {
            var ip = ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            await ctx.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            logger.LogInformation("Logout depuis {Ip}", ip);
            return Results.Ok(new { success = true });
        }).RequireAuthorization();

        app.MapGet("/api/me", (HttpContext ctx) =>
        {
            var isAuth = ctx.User.Identity?.IsAuthenticated ?? false;
            return Results.Ok(new { isAuthenticated = isAuth });
        }).AllowAnonymous();
    }
}
