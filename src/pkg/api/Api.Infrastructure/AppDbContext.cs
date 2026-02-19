using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Api.Domain.Entities;

namespace Api.Infrastructure;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<App> Apps { get; set; }

    public DbSet<RouteFolder> RouteFolders { get; set; }
    public DbSet<Route> Routes { get; set; }
    public DbSet<AccessLog> Logs { get; set; }
    public DbSet<AuthenticationScheme> AuthenticationSchemes { get; set; }

    public DbSet<Variable> Variables { get; set; }

    //TODO: re-design?
    public DbSet<ApiToken> ApiTokens { get; set; }

    public DbSet<AppBuild> AppBuildJobs { get; set; }

    public DbSet<AppBuildArtifact> AppBuildArtifacts { get; set; }

    public DbSet<Artifact> Artifacts { get; set; }

    public DbSet<ObjectBlob> ObjectBlobs { get; set; }

    public DbSet<DeploymentFile> DeploymentFiles { get; set; }
    
    public DbSet<Deployment> Deployments { get; set; }
    public DbSet<SpaDeployment> SpaDeployments { get; set; }
    public DbSet<ApiDeployment> ApiDeployments { get; set; }

    public DbSet<SlackInstallation> SlackInstallations { get; set; }

    public DbSet<SlackUserLink> SlackUserLinks { get; set; }

    public DbSet<SlackAppSubscription> SlackAppSubscriptions { get; set; }

    public DbSet<GitHubInstallation> GitHubInstallations { get; set; }

    public DbSet<CustomDomain> CustomDomains { get; set; }

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

        // Configure AppBuild.Metadata to use PostgreSQL JSONB
        modelBuilder.Entity<AppBuild>()
            .Property(b => b.Metadata)
            .HasColumnType("jsonb");

        modelBuilder.Entity<Route>()
            .OwnsMany(route => route.ResponseHeaders,
                ownedNavigationBuilder => { ownedNavigationBuilder.ToJson(); });

        modelBuilder.Entity<Route>()
            .Property(r => r.Enabled)
            .HasDefaultValue(true);

        modelBuilder.Entity<Route>()
            .HasOne(r => r.App)
            .WithMany(a => a.Routes)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<AuthenticationScheme>()
            .HasOne(r => r.App)
            .WithMany(a => a.AuthenticationSchemes)
            .OnDelete(DeleteBehavior.Cascade);
        
        modelBuilder.Entity<AccessLog>()
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

        modelBuilder.Entity<App>()
            .HasMany(e => e.AppBuilds)
            .WithOne(e => e.App)
            .HasForeignKey(e => e.AppId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<App>()
            .HasIndex(x => x.Slug)
            .IsUnique();

        modelBuilder.Entity<App>()
            .Property(x => x.Slug)
            .HasMaxLength(50);

        modelBuilder.Entity<AppBuildArtifact>()
            .HasKey(a => new { a.BuildJobId, a.ArtifactId });

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

        // Artifact, ObjectBlob, DeploymentFile, SpaDeployment, Release configurations
        modelBuilder.Entity<Artifact>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.App)
                .WithMany(a => a.Artifacts)
                .HasForeignKey(e => e.AppId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Build)
                .WithMany()
                .HasForeignKey(e => e.BuildId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasIndex(e => e.BundleHash).IsUnique();
            entity.Property(e => e.BundleHash).HasMaxLength(64);
            entity.Property(e => e.StorageKey).HasMaxLength(500);
            entity.Property(e => e.Compression).HasMaxLength(50);
        });

        modelBuilder.Entity<ObjectBlob>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ContentHash).IsUnique();
            entity.Property(e => e.ContentHash).HasMaxLength(64);
            entity.Property(e => e.ContentType).HasMaxLength(255);
            entity.Property(e => e.StorageKey).HasMaxLength(500);
        });

        modelBuilder.Entity<DeploymentFile>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.DeploymentId);
            entity.HasIndex(e => e.BlobId);
            entity.HasIndex(e => new { e.DeploymentId, e.Path }).IsUnique();
            
            entity.Property(e => e.Path).HasMaxLength(500);
            entity.Property(e => e.ETag).HasMaxLength(64);
            
            entity.HasOne(e => e.Deployment)
                .WithMany(d => d.Files)
                .HasForeignKey(e => e.DeploymentId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasOne(e => e.Blob)
                .WithMany(b => b.DeploymentFiles)
                .HasForeignKey(e => e.BlobId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Deployment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.AppId, e.Status });
            
            entity.HasDiscriminator<string>("DeploymentType")
                .HasValue<SpaDeployment>("SPA")
                .HasValue<ApiDeployment>("API");
        });

        modelBuilder.Entity<SpaDeployment>(entity =>
        {
            entity.HasOne(e => e.App)
                .WithMany(a => a.SpaDeployments)
                .HasForeignKey(e => e.AppId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Build)
                .WithMany()
                .HasForeignKey(e => e.BuildId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Artifact)
                .WithMany(a => a.Deployments)
                .HasForeignKey(e => e.ArtifactId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ApiDeployment>(entity =>
        {
            entity.HasOne(e => e.App)
                .WithMany(a => a.ApiDeployments)
                .HasForeignKey(e => e.AppId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CustomDomain>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Domain).IsUnique();
            entity.Property(e => e.Domain).HasMaxLength(253);
            entity.HasOne(e => e.App)
                .WithMany(a => a.CustomDomains)
                .HasForeignKey(e => e.AppId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<App>()
            .HasOne(a => a.ActiveSpaDeployment)
            .WithMany()
            .HasForeignKey(a => a.ActiveSpaDeploymentId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<App>()
            .HasOne(a => a.ActiveApiDeployment)
            .WithMany()
            .HasForeignKey(a => a.ActiveApiDeploymentId)
            .OnDelete(DeleteBehavior.SetNull);
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
