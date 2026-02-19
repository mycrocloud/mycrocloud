using System.ComponentModel.DataAnnotations;
using System.Net;
using Api.Domain.Entities;
using Api.Domain.Enums;
using Api.Domain.Repositories;
using Api.Domain.Services;
using Api.Filters;
using Api.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Api.Controllers;

[Route("apps/{appId:int}/customdomains")]
[TypeFilter<AppOwnerActionFilter>(Arguments = ["appId"])]
public class CustomDomainsController(
    AppDbContext dbContext,
    IAppSpecificationPublisher specPublisher,
    IAppRepository appRepository,
    IConfiguration configuration) : BaseController
{
    private string CnameTarget => configuration["CustomDomain:CnameTarget"]
        ?? throw new InvalidOperationException("CustomDomain:CnameTarget not configured");

    private async Task PublishSpec(int appId)
    {
        var app = await appRepository.GetByAppId(appId);
        if (app != null)
        {
            await specPublisher.PublishAsync(app.Slug);
        }
    }

    [HttpGet]
    public async Task<IActionResult> List(int appId)
    {
        var domains = await dbContext.CustomDomains
            .Where(d => d.AppId == appId)
            .OrderBy(d => d.CreatedAt)
            .ToListAsync();

        return Ok(domains.Select(d => new
        {
            d.Id,
            d.Domain,
            Status = d.Status.ToString(),
            d.VerifiedAt,
            d.CreatedAt,
            d.UpdatedAt,
            CnameTarget
        }));
    }

    [HttpPost]
    public async Task<IActionResult> Add(int appId, [FromBody] AddCustomDomainRequest request)
    {
        var domain = request.Domain.Trim().ToLowerInvariant();

        var taken = await dbContext.CustomDomains
            .AnyAsync(d => d.Domain == domain);
        if (taken)
            return Conflict(new { Message = "This domain is already registered" });

        var entity = new CustomDomain
        {
            AppId = appId,
            Domain = domain,
            Status = CustomDomainStatus.Pending
        };
        dbContext.CustomDomains.Add(entity);
        await dbContext.SaveChangesAsync();

        return Created("", new
        {
            entity.Id,
            entity.Domain,
            Status = entity.Status.ToString(),
            entity.CreatedAt,
            CnameTarget
        });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Remove(int appId, int id)
    {
        var domain = await dbContext.CustomDomains
            .SingleOrDefaultAsync(d => d.AppId == appId && d.Id == id);
        if (domain is null) return NotFound();

        dbContext.CustomDomains.Remove(domain);
        await dbContext.SaveChangesAsync();

        await specPublisher.InvalidateCustomDomainAsync(domain.Domain);
        await PublishSpec(appId);

        return NoContent();
    }

    [HttpPost("{id:int}/verify")]
    public async Task<IActionResult> Verify(int appId, int id)
    {
        var domain = await dbContext.CustomDomains
            .SingleOrDefaultAsync(d => d.AppId == appId && d.Id == id);
        if (domain is null) return NotFound();

        var verified = await CheckCname(domain.Domain, CnameTarget);

        if (verified)
        {
            domain.Status = CustomDomainStatus.Active;
            domain.VerifiedAt = DateTime.UtcNow;
        }
        else
        {
            domain.Status = CustomDomainStatus.Failed;
        }

        await dbContext.SaveChangesAsync();

        if (verified)
        {
            await PublishSpec(appId);
        }

        return Ok(new
        {
            Status = domain.Status.ToString(),
            domain.VerifiedAt,
            Message = verified
                ? "Domain verified successfully"
                : $"CNAME check failed. Ensure {domain.Domain} has a CNAME record pointing to {CnameTarget}"
        });
    }

    private static async Task<bool> CheckCname(string domain, string expectedTarget)
    {
        try
        {
            var entry = await Dns.GetHostEntryAsync(domain);
            return entry.HostName.TrimEnd('.')
                .Equals(expectedTarget.TrimEnd('.'), StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }
}

public class AddCustomDomainRequest
{
    [Required]
    [RegularExpression(@"^(?!-)[A-Za-z0-9-]+(\.[A-Za-z0-9-]+)*\.[A-Za-z]{2,}$",
        ErrorMessage = "Invalid domain name")]
    [MaxLength(253)]
    public string Domain { get; set; } = string.Empty;
}
