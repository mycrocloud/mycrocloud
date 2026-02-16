using Microsoft.EntityFrameworkCore;
using MycroCloud.WebApp.Gateway.Models;

namespace MycroCloud.WebApp.Gateway.Services;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<DeploymentFile> DeploymentFiles { get; set; }
    public DbSet<AccessLog> Logs { get; set; }
}
