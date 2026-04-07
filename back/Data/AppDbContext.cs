using Microsoft.EntityFrameworkCore;
using PortalCameras.Models;

namespace PortalCameras.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Favourite> Favourites { get; set; }
}
