using Microsoft.EntityFrameworkCore;
using Storages.Core.Entities.Kv;

namespace Storages.Core.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<KvInstance> KvInstances { get; set; }
    public DbSet<KvValue> KvValues { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.Entity<KvInstance>()
            .HasIndex(ns => new { ns.UserId, ns.Name })
            .IsUnique();
        
        modelBuilder.Entity<KvValue>()
            .HasKey(kv => new { kv.InstanceId, kv.Key });
    }
}