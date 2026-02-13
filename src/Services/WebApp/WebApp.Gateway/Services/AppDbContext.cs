using Microsoft.EntityFrameworkCore;
using WebApp.Gateway.Models;

namespace WebApp.Gateway.Services;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<ApiKey> ApiKeys { get; set; }
    public DbSet<DeploymentFile> DeploymentFiles { get; set; }
    public DbSet<AccessLog> Logs { get; set; }
}
