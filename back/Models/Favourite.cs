namespace PortalCameras.Models;

public class Favourite
{
    public int Id { get; set; }
    public string CameraId { get; set; } = string.Empty;
    public string AbsolutePath { get; set; } = string.Empty;
    public string RelativePath { get; set; } = string.Empty;
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
    public DateTime PhotoDate { get; set; }
}
