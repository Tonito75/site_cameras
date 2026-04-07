using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using PortalCameras.Models;
using PortalCameras.Services;

namespace PortalCameras.Endpoints;

public static class CameraEndpoints
{
    public static void MapCameraEndpoints(this WebApplication app)
    {
        // GET /api/cameras — liste les caméras
        app.MapGet("/api/cameras", (IOptions<List<CameraConfig>> cameras, ILogger<Program> logger) =>
        {
            logger.LogInformation("GET /api/cameras — {Count} caméras retournées", cameras.Value.Count);
            var result = cameras.Value.Select(c => new
            {
                c.Name,
                c.Ip,
                c.Url,
                c.Group
            });
            return Results.Ok(result);
        }).RequireAuthorization();

        // GET /api/cameras/{name}/ping — ping une caméra
        app.MapGet("/api/cameras/{name}/ping", async (
            string name,
            IOptions<List<CameraConfig>> cameras,
            PingService pingService,
            ILogger<Program> logger) =>
        {
            var camera = cameras.Value.FirstOrDefault(c =>
                c.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

            if (camera is null)
            {
                logger.LogWarning("Ping — caméra introuvable : {Name}", name);
                return Results.NotFound();
            }

            logger.LogInformation("Ping {Name} ({Ip})...", camera.Name, camera.Ip);
            var isOnline = await pingService.PingAsync(camera.Ip);
            logger.LogInformation("Ping {Name} ({Ip}) — {Status}", camera.Name, camera.Ip, isOnline ? "en ligne" : "hors ligne");
            return Results.Ok(new { name = camera.Name, isOnline });
        }).RequireAuthorization();

        // GET /api/cameras/{name}/history?useAI=false&skip=0&count=20&fromDate=2026-04-01 — images récentes
        app.MapGet("/api/cameras/{name}/history", async (
            string name,
            bool? useAI,
            int? skip,
            int? count,
            string? fromDate,
            IOptions<List<CameraConfig>> cameras,
            IConfiguration config,
            IOService ioService,
            DateService dateService,
            DetectThingsService detectService,
            DiscordService discordService,
            ILogger<Program> logger) =>
        {
            var camera = cameras.Value.FirstOrDefault(c =>
                c.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

            if (camera is null || string.IsNullOrEmpty(camera.HistoryFolder))
            {
                logger.LogWarning("History — caméra introuvable ou sans dossier : {Name}", name);
                return Results.NotFound();
            }

            var baseFolder = config["BaseHistoryFolder"] ?? string.Empty;
            var apiDetectUrl = config["ApiDetectThingsUrl"] ?? string.Empty;
            var physicalBase = Path.Combine(baseFolder, camera.HistoryFolder);
            var urlBase = $"/camera-history/{camera.HistoryFolder.Replace("\\", "/")}";

            logger.LogInformation("History {Name} — baseFolder={BaseFolder}, physicalBase={PhysicalBase}, useAI={UseAI}",
                name, baseFolder, physicalBase, useAI);

            if (string.IsNullOrEmpty(baseFolder))
                logger.LogWarning("History {Name} — BaseHistoryFolder non configuré !", name);

            await discordService.SendAsync($"⚠️ Quelqu'un consulte l'historique de {name}.");

            var images = new List<object>();
            int toSkip = Math.Max(0, skip ?? 0);
            int maxImages = Math.Clamp(count ?? 20, 1, 200);
            const int maxIterations = 500;
            const int maxApiFails = 5;

            // Calcul du point de départ si fromDate est fourni
            int startDays = 0;
            if (!string.IsNullOrEmpty(fromDate) && DateTime.TryParse(fromDate, out var parsedDate))
            {
                startDays = Math.Max(0, (int)(DateTime.Today - parsedDate.Date).TotalDays);
                logger.LogInformation("History {Name} — fromDate={FromDate}, startDays={StartDays}", name, fromDate, startDays);
            }

            int days = startDays, skipped = 0, found = 0, iterations = 0, apiFails = 0;

            while (found < maxImages && iterations < maxIterations)
            {
                var dateFolder = dateService.GetDateFolderDaysAgo(days);
                var physicalDir = Path.Combine(physicalBase, dateFolder);

                if (ioService.DirectoryExists(physicalDir))
                {
                    var files = ioService.ListFileNames(physicalDir, "*.jpg", true);
                    logger.LogDebug("History {Name} — dossier {Dir} : {Count} fichiers", name, physicalDir, files.Count);

                    foreach (var (fileName, creationDate) in files)
                    {
                        if (found >= maxImages) break;

                        var imageUrl = $"{urlBase}/{dateFolder}/{fileName}";

                        if (useAI == true)
                        {
                            var physicalFile = Path.Combine(physicalDir, fileName);
                            logger.LogDebug("History {Name} — détection IA sur {File}", name, fileName);
                            var (detected, error) = await detectService.DetectAsync(physicalFile, apiDetectUrl);

                            if (!string.IsNullOrEmpty(error))
                            {
                                logger.LogError("History {Name} — Detect API erreur ({Fails}/{Max}) sur {File}: {Error}",
                                    name, apiFails + 1, maxApiFails, fileName, error);
                                apiFails++;
                                if (apiFails > maxApiFails) goto Done;
                                continue;
                            }

                            if (!detected)
                            {
                                logger.LogDebug("History {Name} — rien détecté sur {File}", name, fileName);
                                continue;
                            }

                            logger.LogDebug("History {Name} — détection positive sur {File}", name, fileName);
                        }

                        if (skipped < toSkip)
                        {
                            skipped++;
                            continue;
                        }

                        images.Add(new
                        {
                            url = imageUrl,
                            date = creationDate,
                            timeAgo = dateService.FormatTimeAgoFrench(creationDate)
                        });
                        found++;
                    }
                }

                days++;
                iterations++;
            }

            Done:
            logger.LogInformation("History {Name} — {Found} images retournées (skip={Skip}) en {Iterations} itérations", name, found, toSkip, iterations);
            return Results.Ok(images);
        }).RequireAuthorization();

        // GET /api/cameras/{name}/stats — nombre d'images par jour sur les 30 derniers jours
        app.MapGet("/api/cameras/{name}/stats", (
            string name,
            IOptions<List<CameraConfig>> cameras,
            IConfiguration config,
            IOService ioService,
            DateService dateService,
            IMemoryCache cache,
            ILogger<Program> logger) =>
        {
            var camera = cameras.Value.FirstOrDefault(c =>
                c.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

            if (camera is null || string.IsNullOrEmpty(camera.HistoryFolder))
            {
                logger.LogWarning("Stats — caméra introuvable ou sans dossier : {Name}", name);
                return Results.NotFound();
            }

            var cacheKey = $"stats_{camera.Id}_{DateTime.Today:yyyyMMdd}";
            if (cache.TryGetValue(cacheKey, out var cached))
            {
                logger.LogDebug("Stats {Name} — servi depuis le cache", name);
                return Results.Ok(cached);
            }

            var baseFolder = config["BaseHistoryFolder"] ?? string.Empty;
            var physicalBase = Path.Combine(baseFolder, camera.HistoryFolder);

            var result = Enumerable.Range(0, 30)
                .Select(daysAgo =>
                {
                    var dateFolder = dateService.GetDateFolderDaysAgo(daysAgo);
                    var physicalDir = Path.Combine(physicalBase, dateFolder);
                    var count = ioService.DirectoryExists(physicalDir)
                        ? ioService.ListFileNames(physicalDir, "*.jpg", false).Count
                        : 0;
                    return new { date = dateFolder.Replace("/", "-"), count };
                })
                .Reverse()
                .ToList();

            // Cache jusqu'à minuit (les stats du jour peuvent encore évoluer)
            var expiry = DateTime.Today.AddDays(1) - DateTime.Now;
            cache.Set(cacheKey, result, expiry);

            logger.LogInformation("Stats {Name} — 30 jours calculés et mis en cache pour {Expiry}", name, expiry);
            return Results.Ok(result);
        }).RequireAuthorization();
    }
}
