using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MockServer.Core.Repositories;
using MockServer.Core.Settings;
using MockServer.Infrastructure.Repositories.PostgreSql;
using MockServer.Web.Services;
using MockServer.Web.Services.Interfaces;

namespace MockServer.Web.Extentions
{
    public static class ServiceCollectionExtentions
    {
        public static void ConfigureServices(this IServiceCollection services, IConfiguration configuration)
        {
            var settings = new GlobalSettings();
            services.AddSingleton<GlobalSettings>(s => settings);
            services.Configure<PostgresSettings>(configuration.GetSection("Database:Application"));
            services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
            services.AddHttpContextAccessor();
            services.AddDbContext<IdentityDbContext>(options => {
                var provider = configuration.GetValue<string>("Database:Identity:Provider");
                var connectionString = configuration.GetValue<string>("Database:Identity:ConnectionString");
                if (provider == "PostgresSql")
                {
                    var assemblyName = typeof(Program).Assembly.GetName().Name;
                    options.UseNpgsql(connectionString, b => b.MigrationsAssembly(assemblyName));
                }
            });
            services.AddIdentityCore<IdentityUser>()
                    .AddEntityFrameworkStores<IdentityDbContext>();
            services.AddScoped<UserManager<IdentityUser>>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IWebApplicationRepository, WebApplicationRepository>();
            services.AddScoped<IWebApplicationRouteRepository, WebApplicationRouteRepository>();
            services.AddScoped<IWebApplicationAuthenticationSchemeRepository, WebApplicationAuthenticationSchemeRepository>();
            services.AddScoped<IWebApplicationAuthorizationPolicyRepository, WebApplicationAuthorizationPolicyRepository>();
            services.AddScoped<IDatabaseRepository, DatabaseRespository>();
            services.AddScoped<IWebApplicationWebService, WebApplicationWebService>();
            services.AddScoped<IWebApplicationRouteWebService, WebApplicationRouteWebService>();
            services.AddScoped<IWebApplicationAuthenticationWebService, WebApplicationAuthenticationWebService>();
            services.AddScoped<IWebApplicationAuthorizationWebService, WebApplicationAuthorizationWebService>();
            services.AddScoped<IServiceRepository, ServiceRepository>();
            services.AddScoped<IDatabaseWebService, DatabaseWebService>();
            services.AddScoped<IFunctionRepository, FunctionRepository>();
            services.AddScoped<IFunctionWebService, FunctionWebService>();
        }
    }
}