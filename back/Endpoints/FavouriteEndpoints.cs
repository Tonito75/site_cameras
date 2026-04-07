using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PortalCameras.Data;
using PortalCameras.Models;

namespace PortalCameras.Endpoints;

public static class FavouriteEndpoints
{
    public static void MapFavouriteEndpoints(this WebApplication app)
    {
        // GET /api/cameras/{name}/favourites?skip=0&take=20
        app.MapGet("/api/cameras/{name}/favourites", async (
            string name,
            int? skip,
            int? take,
            IOptions<List<CameraConfig>> cameras,
            AppDbContext db,
            ILogger<Program> logger) =>
        {
            var camera = cameras.Value.FirstOrDefault(c =>
                c.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

            if (camera is null)
            {
                logger.LogWarning("Favourites GET — caméra introuvable : {Name}", name);
                return Results.NotFound();
            }

            var actualSkip = Math.Max(0, skip ?? 0);
            var actualTake = Math.Clamp(take ?? 20, 1, 50);

            var total = await db.Favourites.CountAsync(f => f.CameraId == camera.Id);
            var favourites = await db.Favourites
                .Where(f => f.CameraId == camera.Id)
                .OrderByDescending(f => f.AddedAt)
                .Skip(actualSkip)
                .Take(actualTake)
                .Select(f => new
                {
                    f.Id,
                    url = "/camera-history/" + f.RelativePath.Replace("\\", "/"),
                    f.AddedAt
                })
                .ToListAsync();

            logger.LogInformation("Favourites GET — {Count}/{Total} favoris pour {Name}", favourites.Count, total, name);
            return Results.Ok(new { total, items = favourites });
        }).RequireAuthorization();

        // POST /api/favourites — { cameraName, imageUrl }
        app.MapPost("/api/favourites", async (
            AddFavouriteRequest req,
            IOptions<List<CameraConfig>> cameras,
            IConfiguration config,
            AppDbContext db,
            ILogger<Program> logger) =>
        {
            var camera = cameras.Value.FirstOrDefault(c =>
                c.Name.Equals(req.CameraName, StringComparison.OrdinalIgnoreCase));

            if (camera is null)
            {
                logger.LogWarning("Favourites POST — caméra introuvable : {Name}", req.CameraName);
                return Results.NotFound();
            }

            // imageUrl = "/camera-history/HistoryFolder/dateFolder/file.jpg"
            const string prefix = "/camera-history/";
            if (!req.ImageUrl.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                logger.LogWarning("Favourites POST — URL invalide : {Url}", req.ImageUrl);
                return Results.BadRequest("URL invalide");
            }

            var relativePath = req.ImageUrl[prefix.Length..];
            var baseFolder = config["BaseHistoryFolder"] ?? string.Empty;
            var absolutePath = Path.GetFullPath(Path.Combine(baseFolder, relativePath));

            var favourite = new Favourite
            {
                CameraId = camera.Id,
                RelativePath = relativePath,
                AbsolutePath = absolutePath,
                AddedAt = DateTime.UtcNow
            };

            db.Favourites.Add(favourite);
            await db.SaveChangesAsync();

            logger.LogInformation("Favourite ajouté — Id={Id}, Camera={Camera}, RelativePath={Path}",
                favourite.Id, camera.Id, relativePath);

            return Results.Ok(new { favourite.Id });
        }).RequireAuthorization();

        // GET /api/favourites?skip=0&take=20 — tous les favoris toutes caméras confondues
        app.MapGet("/api/favourites", async (
            int? skip,
            int? take,
            IOptions<List<CameraConfig>> cameras,
            AppDbContext db,
            ILogger<Program> logger) =>
        {
            var actualSkip = Math.Max(0, skip ?? 0);
            var actualTake = Math.Clamp(take ?? 20, 1, 50);

            var cameraMap = cameras.Value.ToDictionary(c => c.Id, c => c.Name);

            var total = await db.Favourites.CountAsync();
            var favourites = await db.Favourites
                .OrderByDescending(f => f.AddedAt)
                .Skip(actualSkip)
                .Take(actualTake)
                .Select(f => new
                {
                    f.Id,
                    url = "/camera-history/" + f.RelativePath.Replace("\\", "/"),
                    f.CameraId,
                    f.AddedAt
                })
                .ToListAsync();

            var result = favourites.Select(f => new
            {
                f.Id,
                f.url,
                f.AddedAt,
                cameraName = cameraMap.GetValueOrDefault(f.CameraId, f.CameraId)
            });

            logger.LogInformation("Favourites ALL GET — skip={Skip}, take={Take}, total={Total}", actualSkip, actualTake, total);
            return Results.Ok(new { total, items = result });
        }).RequireAuthorization();

        // DELETE /api/favourites/{id}
        app.MapDelete("/api/favourites/{id:int}", async (
            int id,
            AppDbContext db,
            ILogger<Program> logger) =>
        {
            var favourite = await db.Favourites.FindAsync(id);
            if (favourite is null)
            {
                logger.LogWarning("Favourites DELETE — id introuvable : {Id}", id);
                return Results.NotFound();
            }

            db.Favourites.Remove(favourite);
            await db.SaveChangesAsync();

            logger.LogInformation("Favourite supprimé — Id={Id}", id);
            return Results.Ok(new { success = true });
        }).RequireAuthorization();
    }
}

public record AddFavouriteRequest(string CameraName, string ImageUrl);
