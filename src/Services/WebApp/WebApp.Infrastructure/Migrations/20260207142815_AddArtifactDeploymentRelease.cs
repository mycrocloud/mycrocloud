using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace WebApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddArtifactDeploymentRelease : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ApiTokens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: true),
                    UserId = table.Column<string>(type: "text", nullable: true),
                    HashedToken = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApiTokens", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GitHubInstallations",
                columns: table => new
                {
                    InstallationId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AccountId = table.Column<long>(type: "bigint", nullable: false),
                    AccountLogin = table.Column<string>(type: "text", nullable: true),
                    AccountType = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GitHubInstallations", x => x.InstallationId);
                });

            migrationBuilder.CreateTable(
                name: "SlackAppSubscriptions",
                columns: table => new
                {
                    TeamId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ChannelId = table.Column<string>(type: "text", nullable: false),
                    AppId = table.Column<int>(type: "integer", nullable: false),
                    SubscriptionId = table.Column<int>(type: "integer", nullable: false),
                    SlackUserId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SlackAppSubscriptions", x => new { x.TeamId, x.ChannelId, x.AppId });
                });

            migrationBuilder.CreateTable(
                name: "SlackInstallations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TeamId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    TeamName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    BotUserId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    BotAccessToken = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    Scopes = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    InstalledByUserId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    EnterpriseId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    IsEnterpriseInstall = table.Column<bool>(type: "boolean", nullable: false),
                    InstalledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SlackInstallations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SlackUserLinks",
                columns: table => new
                {
                    TeamId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    SlackUserId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    UserId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    LinkedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SlackUserLinks", x => new { x.TeamId, x.SlackUserId });
                });

            migrationBuilder.CreateTable(
                name: "ApiKeys",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AppId = table.Column<int>(type: "integer", nullable: true),
                    Name = table.Column<string>(type: "text", nullable: true),
                    Key = table.Column<string>(type: "text", nullable: true),
                    Metadata = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApiKeys", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppBuildArtifacts",
                columns: table => new
                {
                    BuildId = table.Column<Guid>(type: "uuid", nullable: false),
                    Path = table.Column<string>(type: "text", nullable: false),
                    Content = table.Column<byte[]>(type: "bytea", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppBuildArtifacts", x => new { x.BuildId, x.Path });
                });

            migrationBuilder.CreateTable(
                name: "AppBuildJobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AppId = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: true),
                    ContainerId = table.Column<string>(type: "text", nullable: true),
                    Name = table.Column<string>(type: "text", nullable: true, defaultValue: "build"),
                    ArtifactId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppBuildJobs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppLink",
                columns: table => new
                {
                    AppId = table.Column<int>(type: "integer", nullable: false),
                    InstallationId = table.Column<long>(type: "bigint", nullable: false),
                    RepoId = table.Column<long>(type: "bigint", nullable: false),
                    RepoName = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppLink", x => x.AppId);
                    table.ForeignKey(
                        name: "FK_AppLink_GitHubInstallations_InstallationId",
                        column: x => x.InstallationId,
                        principalTable: "GitHubInstallations",
                        principalColumn: "InstallationId");
                });

            migrationBuilder.CreateTable(
                name: "Apps",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "text", nullable: true),
                    Name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    RoutingConfig = table.Column<string>(type: "text", nullable: true),
                    LatestBuildId = table.Column<Guid>(type: "uuid", nullable: true),
                    ActiveReleaseId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<Guid>(type: "uuid", nullable: false),
                    BuildConfigs = table.Column<string>(type: "jsonb", nullable: true),
                    CorsSettings = table.Column<string>(type: "jsonb", nullable: true),
                    Settings = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Apps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Apps_AppBuildJobs_LatestBuildId",
                        column: x => x.LatestBuildId,
                        principalTable: "AppBuildJobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Artifacts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AppId = table.Column<int>(type: "integer", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    DataBlob = table.Column<byte[]>(type: "bytea", nullable: true),
                    ContentHash = table.Column<string>(type: "text", nullable: true),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Artifacts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Artifacts_Apps_AppId",
                        column: x => x.AppId,
                        principalTable: "Apps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AuthenticationSchemes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AppId = table.Column<int>(type: "integer", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    OpenIdConnectAuthority = table.Column<string>(type: "text", nullable: true),
                    OpenIdConnectAudience = table.Column<string>(type: "text", nullable: true),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuthenticationSchemes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuthenticationSchemes_Apps_AppId",
                        column: x => x.AppId,
                        principalTable: "Apps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RouteFolders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: true),
                    AppId = table.Column<int>(type: "integer", nullable: true),
                    ParentId = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RouteFolders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RouteFolders_Apps_AppId",
                        column: x => x.AppId,
                        principalTable: "Apps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RouteFolders_RouteFolders_ParentId",
                        column: x => x.ParentId,
                        principalTable: "RouteFolders",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Variables",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AppId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: true),
                    Value = table.Column<string>(type: "text", nullable: true),
                    IsSecret = table.Column<bool>(type: "boolean", nullable: false),
                    Target = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Variables", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Variables_Apps_AppId",
                        column: x => x.AppId,
                        principalTable: "Apps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SpaDeployments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AppId = table.Column<int>(type: "integer", nullable: false),
                    BuildId = table.Column<Guid>(type: "uuid", nullable: false),
                    ArtifactId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExtractedPath = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpaDeployments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SpaDeployments_AppBuildJobs_BuildId",
                        column: x => x.BuildId,
                        principalTable: "AppBuildJobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SpaDeployments_Apps_AppId",
                        column: x => x.AppId,
                        principalTable: "Apps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SpaDeployments_Artifacts_ArtifactId",
                        column: x => x.ArtifactId,
                        principalTable: "Artifacts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Routes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AppId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: true),
                    Method = table.Column<string>(type: "text", nullable: true),
                    Path = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    ResponseType = table.Column<int>(type: "integer", nullable: false),
                    ResponseStatusCode = table.Column<int>(type: "integer", nullable: true),
                    Response = table.Column<string>(type: "text", nullable: true),
                    ResponseBodyLanguage = table.Column<string>(type: "text", nullable: true),
                    FunctionHandlerMethod = table.Column<string>(type: "text", nullable: true, defaultValue: "handler"),
                    FunctionHandlerDependencies = table.Column<string[]>(type: "text[]", nullable: true),
                    RequestQuerySchema = table.Column<string>(type: "text", nullable: true),
                    RequestHeaderSchema = table.Column<string>(type: "text", nullable: true),
                    RequestBodySchema = table.Column<string>(type: "text", nullable: true),
                    RequireAuthorization = table.Column<bool>(type: "boolean", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    FolderId = table.Column<int>(type: "integer", nullable: true),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    FunctionRuntime = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<Guid>(type: "uuid", nullable: false),
                    ResponseHeaders = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Routes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Routes_Apps_AppId",
                        column: x => x.AppId,
                        principalTable: "Apps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Routes_RouteFolders_FolderId",
                        column: x => x.FolderId,
                        principalTable: "RouteFolders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Releases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AppId = table.Column<int>(type: "integer", nullable: false),
                    SpaDeploymentId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    ActivatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeactivatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Releases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Releases_Apps_AppId",
                        column: x => x.AppId,
                        principalTable: "Apps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Releases_SpaDeployments_SpaDeploymentId",
                        column: x => x.SpaDeploymentId,
                        principalTable: "SpaDeployments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Logs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AppId = table.Column<int>(type: "integer", nullable: false),
                    RouteId = table.Column<int>(type: "integer", nullable: true),
                    Method = table.Column<string>(type: "text", nullable: true),
                    Path = table.Column<string>(type: "text", nullable: true),
                    StatusCode = table.Column<int>(type: "integer", nullable: false),
                    FunctionExecutionDuration = table.Column<TimeSpan>(type: "interval", nullable: true),
                    FunctionRuntime = table.Column<int>(type: "integer", nullable: true),
                    RemoteAddress = table.Column<string>(type: "text", nullable: true),
                    RequestContentLength = table.Column<long>(type: "bigint", nullable: true),
                    RequestContentType = table.Column<string>(type: "text", nullable: true),
                    RequestCookie = table.Column<string>(type: "text", nullable: true),
                    RequestFormContent = table.Column<string>(type: "text", nullable: true),
                    RequestHeaders = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<Guid>(type: "uuid", nullable: false),
                    FunctionLogs = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Logs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Logs_Apps_AppId",
                        column: x => x.AppId,
                        principalTable: "Apps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Logs_Routes_RouteId",
                        column: x => x.RouteId,
                        principalTable: "Routes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApiKeys_AppId",
                table: "ApiKeys",
                column: "AppId");

            migrationBuilder.CreateIndex(
                name: "IX_AppBuildJobs_AppId",
                table: "AppBuildJobs",
                column: "AppId");

            migrationBuilder.CreateIndex(
                name: "IX_AppBuildJobs_ArtifactId",
                table: "AppBuildJobs",
                column: "ArtifactId");

            migrationBuilder.CreateIndex(
                name: "IX_AppLink_InstallationId",
                table: "AppLink",
                column: "InstallationId");

            migrationBuilder.CreateIndex(
                name: "IX_Apps_ActiveReleaseId",
                table: "Apps",
                column: "ActiveReleaseId");

            migrationBuilder.CreateIndex(
                name: "IX_Apps_LatestBuildId",
                table: "Apps",
                column: "LatestBuildId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Apps_Name",
                table: "Apps",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Artifacts_AppId",
                table: "Artifacts",
                column: "AppId");

            migrationBuilder.CreateIndex(
                name: "IX_Artifacts_ContentHash",
                table: "Artifacts",
                column: "ContentHash");

            migrationBuilder.CreateIndex(
                name: "IX_AuthenticationSchemes_AppId",
                table: "AuthenticationSchemes",
                column: "AppId");

            migrationBuilder.CreateIndex(
                name: "IX_GitHubInstallations_AccountId",
                table: "GitHubInstallations",
                column: "AccountId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GitHubInstallations_UserId",
                table: "GitHubInstallations",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Logs_AppId",
                table: "Logs",
                column: "AppId");

            migrationBuilder.CreateIndex(
                name: "IX_Logs_RouteId",
                table: "Logs",
                column: "RouteId");

            migrationBuilder.CreateIndex(
                name: "IX_Releases_AppId_IsActive",
                table: "Releases",
                columns: new[] { "AppId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_Releases_SpaDeploymentId",
                table: "Releases",
                column: "SpaDeploymentId");

            migrationBuilder.CreateIndex(
                name: "IX_RouteFolders_AppId",
                table: "RouteFolders",
                column: "AppId");

            migrationBuilder.CreateIndex(
                name: "IX_RouteFolders_ParentId",
                table: "RouteFolders",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_Routes_AppId",
                table: "Routes",
                column: "AppId");

            migrationBuilder.CreateIndex(
                name: "IX_Routes_FolderId",
                table: "Routes",
                column: "FolderId");

            migrationBuilder.CreateIndex(
                name: "IX_SlackInstallations_TeamId",
                table: "SlackInstallations",
                column: "TeamId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SpaDeployments_AppId_Status",
                table: "SpaDeployments",
                columns: new[] { "AppId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_SpaDeployments_ArtifactId",
                table: "SpaDeployments",
                column: "ArtifactId");

            migrationBuilder.CreateIndex(
                name: "IX_SpaDeployments_BuildId",
                table: "SpaDeployments",
                column: "BuildId");

            migrationBuilder.CreateIndex(
                name: "IX_Variables_AppId",
                table: "Variables",
                column: "AppId");

            migrationBuilder.AddForeignKey(
                name: "FK_ApiKeys_Apps_AppId",
                table: "ApiKeys",
                column: "AppId",
                principalTable: "Apps",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AppBuildArtifacts_AppBuildJobs_BuildId",
                table: "AppBuildArtifacts",
                column: "BuildId",
                principalTable: "AppBuildJobs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AppBuildJobs_Apps_AppId",
                table: "AppBuildJobs",
                column: "AppId",
                principalTable: "Apps",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AppBuildJobs_Artifacts_ArtifactId",
                table: "AppBuildJobs",
                column: "ArtifactId",
                principalTable: "Artifacts",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_AppLink_Apps_AppId",
                table: "AppLink",
                column: "AppId",
                principalTable: "Apps",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Apps_Releases_ActiveReleaseId",
                table: "Apps",
                column: "ActiveReleaseId",
                principalTable: "Releases",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AppBuildJobs_Apps_AppId",
                table: "AppBuildJobs");

            migrationBuilder.DropForeignKey(
                name: "FK_Artifacts_Apps_AppId",
                table: "Artifacts");

            migrationBuilder.DropForeignKey(
                name: "FK_Releases_Apps_AppId",
                table: "Releases");

            migrationBuilder.DropForeignKey(
                name: "FK_SpaDeployments_Apps_AppId",
                table: "SpaDeployments");

            migrationBuilder.DropTable(
                name: "ApiKeys");

            migrationBuilder.DropTable(
                name: "ApiTokens");

            migrationBuilder.DropTable(
                name: "AppBuildArtifacts");

            migrationBuilder.DropTable(
                name: "AppLink");

            migrationBuilder.DropTable(
                name: "AuthenticationSchemes");

            migrationBuilder.DropTable(
                name: "Logs");

            migrationBuilder.DropTable(
                name: "SlackAppSubscriptions");

            migrationBuilder.DropTable(
                name: "SlackInstallations");

            migrationBuilder.DropTable(
                name: "SlackUserLinks");

            migrationBuilder.DropTable(
                name: "Variables");

            migrationBuilder.DropTable(
                name: "GitHubInstallations");

            migrationBuilder.DropTable(
                name: "Routes");

            migrationBuilder.DropTable(
                name: "RouteFolders");

            migrationBuilder.DropTable(
                name: "Apps");

            migrationBuilder.DropTable(
                name: "Releases");

            migrationBuilder.DropTable(
                name: "SpaDeployments");

            migrationBuilder.DropTable(
                name: "AppBuildJobs");

            migrationBuilder.DropTable(
                name: "Artifacts");
        }
    }
}
