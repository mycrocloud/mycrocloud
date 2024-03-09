﻿// <auto-generated />
using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using WebApp.Infrastructure.Repositories.EfCore;

#nullable disable

namespace WebApp.Migrations.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20240309095515_AddLogMachineName")]
    partial class AddLogMachineName
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.2")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("WebApp.Domain.Entities.App", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("Description")
                        .HasColumnType("text");

                    b.Property<string>("Name")
                        .HasColumnType("text");

                    b.Property<int>("Status")
                        .HasColumnType("integer");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("UserId")
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.ToTable("Apps");
                });

            modelBuilder.Entity("WebApp.Domain.Entities.AuthenticationScheme", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<int>("AppId")
                        .HasColumnType("integer");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("Description")
                        .HasColumnType("text");

                    b.Property<bool>("Enabled")
                        .HasColumnType("boolean");

                    b.Property<string>("Name")
                        .HasColumnType("text");

                    b.Property<string>("OpenIdConnectAudience")
                        .HasColumnType("text");

                    b.Property<string>("OpenIdConnectAuthority")
                        .HasColumnType("text");

                    b.Property<int?>("Order")
                        .HasColumnType("integer");

                    b.Property<int>("Type")
                        .HasColumnType("integer");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.HasKey("Id");

                    b.HasIndex("AppId");

                    b.ToTable("AuthenticationSchemes");
                });

            modelBuilder.Entity("WebApp.Domain.Entities.Log", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<string>("AdditionalLogMessage")
                        .HasColumnType("text");

                    b.Property<int>("AppId")
                        .HasColumnType("integer");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<TimeSpan?>("FunctionExecutionDuration")
                        .HasColumnType("interval");

                    b.Property<string>("MachineName")
                        .HasColumnType("text");

                    b.Property<string>("Method")
                        .HasColumnType("text");

                    b.Property<string>("Path")
                        .HasColumnType("text");

                    b.Property<string>("RemoteAddress")
                        .HasColumnType("text");

                    b.Property<int>("RequestContentLength")
                        .HasColumnType("integer");

                    b.Property<string>("RequestContentType")
                        .HasColumnType("text");

                    b.Property<string>("RequestCookie")
                        .HasColumnType("text");

                    b.Property<string>("RequestFormContent")
                        .HasColumnType("text");

                    b.Property<string>("RequestHeaders")
                        .HasColumnType("text");

                    b.Property<string>("RequestPayload")
                        .HasColumnType("text");

                    b.Property<int?>("RouteId")
                        .HasColumnType("integer");

                    b.Property<int>("StatusCode")
                        .HasColumnType("integer");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.HasKey("Id");

                    b.HasIndex("AppId");

                    b.HasIndex("RouteId");

                    b.ToTable("Logs");
                });

            modelBuilder.Entity("WebApp.Domain.Entities.Route", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<int>("AppId")
                        .HasColumnType("integer");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("Description")
                        .HasColumnType("text");

                    b.Property<string>("FunctionHandler")
                        .HasColumnType("text");

                    b.Property<string[]>("FunctionHandlerDependencies")
                        .HasColumnType("text[]");

                    b.Property<string>("Method")
                        .HasColumnType("text");

                    b.Property<string>("Name")
                        .HasColumnType("text");

                    b.Property<string>("Path")
                        .HasColumnType("text");

                    b.Property<bool>("RequireAuthorization")
                        .HasColumnType("boolean");

                    b.Property<string>("ResponseBody")
                        .HasColumnType("text");

                    b.Property<string>("ResponseBodyLanguage")
                        .HasColumnType("text");

                    b.Property<int?>("ResponseStatusCode")
                        .HasColumnType("integer");

                    b.Property<string>("ResponseType")
                        .HasColumnType("text");

                    b.Property<int>("Status")
                        .HasColumnType("integer");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<bool>("UseDynamicResponse")
                        .HasColumnType("boolean");

                    b.HasKey("Id");

                    b.HasIndex("AppId");

                    b.ToTable("Routes");
                });

            modelBuilder.Entity("WebApp.Domain.Entities.RouteValidation", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<List<string>>("Expressions")
                        .HasColumnType("text[]");

                    b.Property<string>("Name")
                        .HasColumnType("text");

                    b.Property<int>("RouteId")
                        .HasColumnType("integer");

                    b.Property<string>("Rules")
                        .HasColumnType("text");

                    b.Property<string>("Source")
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.HasIndex("RouteId");

                    b.ToTable("RouteValidations");
                });

            modelBuilder.Entity("WebApp.Domain.Entities.TextStorage", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<int>("AppId")
                        .HasColumnType("integer");

                    b.Property<string>("Content")
                        .HasColumnType("text");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("Description")
                        .HasColumnType("text");

                    b.Property<string>("Name")
                        .HasColumnType("text");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.HasKey("Id");

                    b.HasIndex("AppId");

                    b.ToTable("TextStorages");
                });

            modelBuilder.Entity("WebApp.Domain.Entities.Variable", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<int>("AppId")
                        .HasColumnType("integer");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<bool>("IsSecret")
                        .HasColumnType("boolean");

                    b.Property<string>("Name")
                        .HasColumnType("text");

                    b.Property<string>("StringValue")
                        .HasColumnType("text");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<int>("ValueType")
                        .HasColumnType("integer");

                    b.HasKey("Id");

                    b.HasIndex("AppId");

                    b.ToTable("Variables");
                });

            modelBuilder.Entity("WebApp.Domain.Entities.App", b =>
                {
                    b.OwnsOne("WebApp.Domain.Entities.AppSettings", "Settings", b1 =>
                        {
                            b1.Property<int>("AppId")
                                .HasColumnType("integer");

                            b1.Property<bool>("CheckFunctionExecutionLimitMemory")
                                .HasColumnType("boolean");

                            b1.Property<bool>("CheckFunctionExecutionTimeout")
                                .HasColumnType("boolean");

                            b1.Property<long?>("FunctionExecutionLimitMemoryBytes")
                                .HasColumnType("bigint");

                            b1.Property<int?>("FunctionExecutionTimeoutSeconds")
                                .HasColumnType("integer");

                            b1.Property<bool>("FunctionUseNoSqlConnection")
                                .HasColumnType("boolean");

                            b1.HasKey("AppId");

                            b1.ToTable("Apps");

                            b1.ToJson("Settings");

                            b1.WithOwner()
                                .HasForeignKey("AppId");
                        });

                    b.OwnsOne("WebApp.Domain.Entities.CorsSettings", "CorsSettings", b1 =>
                        {
                            b1.Property<int>("AppId")
                                .HasColumnType("integer");

                            b1.Property<List<string>>("AllowedHeaders")
                                .HasColumnType("text[]");

                            b1.Property<List<string>>("AllowedMethods")
                                .HasColumnType("text[]");

                            b1.Property<List<string>>("AllowedOrigins")
                                .HasColumnType("text[]");

                            b1.Property<List<string>>("ExposeHeaders")
                                .HasColumnType("text[]");

                            b1.Property<int?>("MaxAgeSeconds")
                                .HasColumnType("integer");

                            b1.HasKey("AppId");

                            b1.ToTable("Apps");

                            b1.ToJson("CorsSettings");

                            b1.WithOwner()
                                .HasForeignKey("AppId");
                        });

                    b.Navigation("CorsSettings");

                    b.Navigation("Settings");
                });

            modelBuilder.Entity("WebApp.Domain.Entities.AuthenticationScheme", b =>
                {
                    b.HasOne("WebApp.Domain.Entities.App", "App")
                        .WithMany()
                        .HasForeignKey("AppId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("App");
                });

            modelBuilder.Entity("WebApp.Domain.Entities.Log", b =>
                {
                    b.HasOne("WebApp.Domain.Entities.App", "App")
                        .WithMany("Logs")
                        .HasForeignKey("AppId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("WebApp.Domain.Entities.Route", "Route")
                        .WithMany()
                        .HasForeignKey("RouteId");

                    b.Navigation("App");

                    b.Navigation("Route");
                });

            modelBuilder.Entity("WebApp.Domain.Entities.Route", b =>
                {
                    b.HasOne("WebApp.Domain.Entities.App", "App")
                        .WithMany("Routes")
                        .HasForeignKey("AppId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.OwnsMany("WebApp.Domain.Entities.ResponseHeader", "ResponseHeaders", b1 =>
                        {
                            b1.Property<int>("RouteId")
                                .HasColumnType("integer");

                            b1.Property<int>("Id")
                                .ValueGeneratedOnAdd()
                                .HasColumnType("integer");

                            b1.Property<string>("Name")
                                .HasColumnType("text");

                            b1.Property<string>("Value")
                                .HasColumnType("text");

                            b1.HasKey("RouteId", "Id");

                            b1.ToTable("Routes");

                            b1.ToJson("ResponseHeaders");

                            b1.WithOwner()
                                .HasForeignKey("RouteId");
                        });

                    b.Navigation("App");

                    b.Navigation("ResponseHeaders");
                });

            modelBuilder.Entity("WebApp.Domain.Entities.RouteValidation", b =>
                {
                    b.HasOne("WebApp.Domain.Entities.Route", "Route")
                        .WithMany("Validations")
                        .HasForeignKey("RouteId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Route");
                });

            modelBuilder.Entity("WebApp.Domain.Entities.TextStorage", b =>
                {
                    b.HasOne("WebApp.Domain.Entities.App", "App")
                        .WithMany()
                        .HasForeignKey("AppId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("App");
                });

            modelBuilder.Entity("WebApp.Domain.Entities.Variable", b =>
                {
                    b.HasOne("WebApp.Domain.Entities.App", "App")
                        .WithMany("Variables")
                        .HasForeignKey("AppId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("App");
                });

            modelBuilder.Entity("WebApp.Domain.Entities.App", b =>
                {
                    b.Navigation("Logs");

                    b.Navigation("Routes");

                    b.Navigation("Variables");
                });

            modelBuilder.Entity("WebApp.Domain.Entities.Route", b =>
                {
                    b.Navigation("Validations");
                });
#pragma warning restore 612, 618
        }
    }
}
