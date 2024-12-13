using Microsoft.EntityFrameworkCore;

namespace CD.Api;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<DomainEntity> Domains { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.Entity<DomainEntity>(entity =>
        {
            entity.HasIndex(e => e.Mcrn);
        });
    }
}