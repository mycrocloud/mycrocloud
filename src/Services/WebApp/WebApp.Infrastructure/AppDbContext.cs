using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using WebApp.Domain.Entities;

namespace WebApp.Infrastructure;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<App> Apps { get; set; }

    public DbSet<RouteFolder> RouteFolders { get; set; }
    public DbSet<Route> Routes { get; set; }
    public DbSet<Log> Logs { get; set; }
    public DbSet<AuthenticationScheme> AuthenticationSchemes { get; set; }

    public DbSet<ApiKey> ApiKeys { get; set; }

    public DbSet<Variable> Variables { get; set; }

    //TODO: re-design?
    public DbSet<ApiToken> ApiTokens { get; set; }

    public DbSet<AppBuild> AppBuildJobs { get; set; }

    public DbSet<AppBuildArtifact> AppBuildArtifacts { get; set; }

    public DbSet<SlackInstallation> SlackInstallations { get; set; }

    public DbSet<SlackUserLink> SlackUserLinks { get; set; }

    public DbSet<SlackAppSubscription> SlackAppSubscriptions { get; set; }

    public DbSet<GitHubInstallation> GitHubInstallations { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<App>()
            .OwnsOne(app => app.Settings, builder => { builder.ToJson(); });
        modelBuilder.Entity<App>()
            .OwnsOne(app => app.CorsSettings, builder => { builder.ToJson(); });
        modelBuilder.Entity<App>()
            .Property(app => app.RoutingConfig)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                v => JsonSerializer.Deserialize<RoutingConfig>(v, (JsonSerializerOptions)null)
            );

        modelBuilder.Entity<App>()
            .HasOne(a => a.Link)
            .WithOne()
            .HasForeignKey<AppLink>(ai => ai.AppId)
            .IsRequired(false);

        modelBuilder.Entity<AppLink>()
            .HasKey(ai => ai.AppId);
        
        modelBuilder.Entity<AppLink>()
            .HasOne(ai => ai.GitHubInstallation)
            .WithMany(g => g.AppLinks)
            .HasForeignKey(ai => ai.InstallationId)
            .IsRequired(false);

        modelBuilder.Entity<App>()
            .OwnsOne(app => app.BuildConfigs, builder => { builder.ToJson(); });

        modelBuilder.Entity<Route>().OwnsMany(route => route.ResponseHeaders,
            ownedNavigationBuilder => { ownedNavigationBuilder.ToJson(); });

        modelBuilder.Entity<Route>()
            .Property(r => r.Enabled)
            .HasDefaultValue(true);

        modelBuilder.Entity<Route>()
            .HasOne(r => r.App)
            .WithMany(a => a.Routes)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Route>()
            .Property(r => r.FunctionHandlerMethod)
            .HasDefaultValue("handler");

        modelBuilder.Entity<ApiKey>()
            .HasOne(r => r.App)
            .WithMany(a => a.ApiKeys)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<AuthenticationScheme>()
            .HasOne(r => r.App)
            .WithMany(a => a.AuthenticationSchemes)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Log>()
            .HasOne(r => r.App)
            .WithMany(a => a.Logs)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Log>()
            .HasOne(r => r.Route)
            .WithMany(a => a.Logs)
            .OnDelete(DeleteBehavior.Cascade);
        
        modelBuilder.Entity<Log>()
            .OwnsMany(app => app.FunctionLogs, builder => { builder.ToJson(); });

        modelBuilder.Entity<RouteFolder>()
            .HasOne(f => f.App)
            .WithMany(a => a.RouteFolders)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<RouteFolder>()
            .HasMany(f => f.Routes)
            .WithOne(f => f.Folder)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Variable>()
            .HasOne(v => v.App)
            .WithMany(a => a.Variables)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ApiToken>()
            .HasKey(t => t.Id);

        modelBuilder.Entity<App>(entity =>
        {
            entity.HasMany(e => e.AppBuilds)
                  .WithOne(e => e.App)
                  .HasForeignKey(e => e.AppId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.LatestBuild)
                  .WithOne()
                  .HasForeignKey<App>(e => e.LatestBuildId)
                  .IsRequired(false)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<App>()
            .HasIndex(x => x.Name)
            .IsUnique();

        modelBuilder.Entity<App>()
            .Property(x => x.Name)
            .HasMaxLength(50);

        modelBuilder.Entity<AppBuild>()
            .Property(p => p.Name)
            .HasDefaultValue("build");

        modelBuilder.Entity<AppBuildArtifact>()
            .HasKey(a => new { a.BuildId, a.Path });

        modelBuilder.Entity<SlackInstallation>()
            .HasIndex(x => x.TeamId)
            .IsUnique();

        modelBuilder.Entity<SlackUserLink>()
            .HasKey(x => new { x.TeamId, x.SlackUserId });

        modelBuilder.Entity<SlackAppSubscription>()
            .HasKey(x => new { x.TeamId, x.ChannelId, x.AppId });

        // Cascade delete optional
        // modelBuilder.Entity<SlackInstallation>()
        //     .HasMany(x => x.UserLinks)
        //     .WithOne(x => x.Installation)
        //     .HasForeignKey(x => x.TeamId)
        //     .HasPrincipalKey(x => x.TeamId)
        //     .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<GitHubInstallation>()
            .HasKey(x => x.InstallationId);

        modelBuilder.Entity<GitHubInstallation>()
            .HasIndex(x => x.AccountId)
            .IsUnique();

        modelBuilder.Entity<GitHubInstallation>()
            .HasIndex(x => x.UserId); // no need to be unique because one user can have multiple installations e.g. for orgs
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        AddTimestampsAndVersion();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void AddTimestampsAndVersion()
    {
        var entries = ChangeTracker
            .Entries()
            .Where(e => e.Entity is BaseEntity && (
                e.State == EntityState.Added
                || e.State == EntityState.Modified));
        foreach (var entityEntry in entries)
        {
            if (entityEntry.State == EntityState.Added)
            {
                ((BaseEntity)entityEntry.Entity).CreatedAt = DateTime.UtcNow;
            }
            else
            {
                ((BaseEntity)entityEntry.Entity).UpdatedAt = DateTime.UtcNow;
            }

            ((BaseEntity)entityEntry.Entity).Version = Guid.NewGuid();
        }
    }
}