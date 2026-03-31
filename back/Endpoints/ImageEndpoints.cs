namespace PortalCameras.Endpoints;

public static class ImageEndpoints
{
    public static void MapImageEndpoints(this WebApplication app)
    {
        app.MapGet("/camera-history/{**path}", (HttpContext ctx, string path, IConfiguration config, ILogger<Program> logger) =>
        {
            var baseFolder = config["BaseHistoryFolder"];
            if (string.IsNullOrEmpty(baseFolder))
            {
                logger.LogError("Image — BaseHistoryFolder non configuré, impossible de servir {Path}", path);
                return Results.NotFound();
            }

            // Résoudre les chemins en absolu pour éviter le path traversal
            var resolvedBase = Path.GetFullPath(baseFolder);
            var fullPath = Path.GetFullPath(Path.Combine(resolvedBase, path));

            // Vérifier que le chemin résolu est bien sous baseFolder
            if (!fullPath.StartsWith(resolvedBase + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
                && !fullPath.Equals(resolvedBase, StringComparison.OrdinalIgnoreCase))
            {
                logger.LogWarning("Image — tentative de path traversal bloquée : {Path}", path);
                return Results.Forbid();
            }

            if (!File.Exists(fullPath))
            {
                logger.LogWarning("Image — fichier introuvable : {FullPath}", fullPath);
                return Results.NotFound();
            }

            var contentType = Path.GetExtension(fullPath).ToLower() switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                _ => "application/octet-stream"
            };

            logger.LogDebug("Image — fichier servi : {FullPath}", fullPath);
            ctx.Response.Headers.CacheControl = "no-cache, no-store, must-revalidate";
            return Results.File(fullPath, contentType);
        }).RequireAuthorization();
    }
}
