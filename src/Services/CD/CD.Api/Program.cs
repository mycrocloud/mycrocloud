using System.ComponentModel.DataAnnotations;
using CD.Api;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<AppDbContext>(options => { options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")); });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/mcrn/{mcrn}", async (string mcrn, AppDbContext dbContext) =>
    {
        var domains = await dbContext.Domains.Where(d => d.Mcrn == mcrn).ToListAsync();
        return Results.Ok(domains);
    })
    .WithName("GetDomainsByMCRN")
    .WithOpenApi();

app.MapGet("/{domain}",
        async (string domain, AppDbContext dbContext) => Results.Ok((object?)await dbContext.Domains.FindAsync(domain)))
    .WithName("FindMapping")
    .WithOpenApi();

app.MapPost("/",
        async (SetDomainRequest request, AppDbContext dbContext) =>
        {
            await dbContext.Domains.AddAsync(request.ToNewDomainEntity());
            await dbContext.SaveChangesAsync();
        })
    .WithName("SetMapping")
    .WithOpenApi();

app.MapDelete("/{domain}",
        async (string domain, AppDbContext dbContext) =>
        {
            var domainEntity = await dbContext.Domains.FindAsync(domain);
            if (domainEntity is null)
            {
                return Results.BadRequest();
            }

            dbContext.Domains.Remove(domainEntity);
            await dbContext.SaveChangesAsync();
            
            return Results.Ok();
        })
    .WithName("DeleteMapping")
    .WithOpenApi();

app.Run();

public class SetDomainRequest
{
    [Required] public required string Domain { get; set; }

    [Required] public required string Mcrn { get; set; }

    public DomainEntity ToNewDomainEntity() => new()
    {
        Domain = Domain, Mcrn = Mcrn,
        CreatedAt = DateTime.UtcNow
    };
}