using Microsoft.EntityFrameworkCore;
using WebApp.Domain.Entities;
using File = WebApp.Domain.Entities.File;
using Object = WebApp.Domain.Entities.Object;

namespace WebApp.Infrastructure;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<App> Apps { get; set; }

    public DbSet<RouteFolder> RouteFolders { get; set; }
    public DbSet<Route> Routes { get; set; }
    public DbSet<Folder> Folders { get; set; }
    public DbSet<File> Files { get; set; }
    public DbSet<Log> Logs { get; set; }
    public DbSet<AuthenticationScheme> AuthenticationSchemes { get; set; }

    public DbSet<ApiKey> ApiKeys { get; set; }

    public DbSet<Variable> Variables { get; set; }
    public DbSet<TextStorage> TextStorages { get; set; }
    public DbSet<Object> Objects { get; set; }

    //TODO: re-design?
    public DbSet<UserToken> UserTokens { get; set; }

    public DbSet<AppBuildJob> AppBuildJobs { get; set; }

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
            .HasOne(route => route.File)
            .WithMany()
            .OnDelete(DeleteBehavior.SetNull);

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

        modelBuilder.Entity<Folder>()
            .HasOne(r => r.App)
            .WithMany(a => a.Folders)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Folder>()
            .HasMany(f => f.Files)
            .WithOne(f => f.Folder)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Log>()
            .HasOne(r => r.App)
            .WithMany(a => a.Logs)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Log>()
            .HasOne(r => r.Route)
            .WithMany(a => a.Logs)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<RouteFolder>()
            .HasOne(f => f.App)
            .WithMany(a => a.RouteFolders)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<RouteFolder>()
            .HasMany(f => f.Routes)
            .WithOne(f => f.Folder)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<TextStorage>()
            .HasOne(s => s.App)
            .WithMany(a => a.TextStorages)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Variable>()
            .HasOne(v => v.App)
            .WithMany(a => a.Variables)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Object>()
            .HasKey(bo => new { bo.AppId, bo.Key });

        modelBuilder.Entity<Object>()
            .HasOne(s => s.App)
            .WithMany(a => a.Objects)
            .OnDelete(DeleteBehavior.Cascade);

        //TODO: re-design?
        modelBuilder.Entity<UserToken>()
            .HasKey(t => new { t.UserId, t.Provider, t.Purpose });

        modelBuilder.Entity<AppBuildJob>()
            .Property(p => p.Name)
            .HasDefaultValue("build");

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