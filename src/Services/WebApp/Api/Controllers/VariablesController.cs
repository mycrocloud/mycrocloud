using System.Text.Json.Serialization;
using Api.Filters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Api.Domain.Entities;
using Api.Infrastructure;
using Api.Domain.Services;
using Api.Domain.Repositories;

namespace Api.Controllers;

[Route("apps/{appId:int}/[controller]")]
[TypeFilter<AppOwnerActionFilter>(Arguments = ["appId"])]
public class VariablesController(
    AppDbContext appDbContext,
    IAppSpecificationPublisher specPublisher,
    IAppRepository appRepository) : BaseController
{
    private async Task PublishSpec(int appId)
    {
        var app = await appRepository.GetByAppId(appId);
        if (app != null)
        {
            await specPublisher.PublishAsync(app.Slug);
        }
    }
    [HttpGet]
    public async Task<IActionResult> List(int appId, [FromQuery] VariableTarget? target = null)
    {
        var query = appDbContext.Variables.Where(v => v.AppId == appId);
        if (target.HasValue)
        {
            query = query.Where(v => v.Target == target.Value || v.Target == VariableTarget.All);
        }
        var variables = await query.ToListAsync();
        return Ok(variables.Select(variable => new
        {
            variable.Id,
            variable.Name,
            variable.IsSecret,
            Target = variable.Target.ToString(),
            variable.Value,
            variable.CreatedAt,
            variable.UpdatedAt
        }));
    }

    [HttpPost]
    public async Task<IActionResult> Post(int appId, CreateUpdateVariableRequest createUpdateVariableRequest)
    {
        var entity = createUpdateVariableRequest.ToEntity();
        entity.AppId = appId;
        await appDbContext.Variables.AddAsync(entity);
        await appDbContext.SaveChangesAsync();
        await PublishSpec(appId);
        return Created("", new { entity.Id });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Put(int appId, int id, CreateUpdateVariableRequest createUpdateVariableRequest)
    {
        var entity = await appDbContext.Variables.SingleAsync(v => v.AppId == appId && v.Id == id);
        createUpdateVariableRequest.CopyToEntity(entity);
        appDbContext.Variables.Update(entity);
        await appDbContext.SaveChangesAsync();
        await PublishSpec(appId);
        return NoContent();
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int appId, int id)
    {
        var variable = await appDbContext.Variables.SingleAsync(v => v.AppId == appId && v.Id == id);
        return Ok(new
        {
            variable.Id,
            variable.Name,
            variable.IsSecret,
            Target = variable.Target.ToString(),
            variable.Value,
            variable.CreatedAt,
            variable.UpdatedAt
        });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int appId, int id)
    {
        var variable = await appDbContext.Variables.SingleAsync(v => v.AppId == appId && v.Id == id);
        appDbContext.Variables.Remove(variable);
        await appDbContext.SaveChangesAsync();
        await PublishSpec(appId);
        return NoContent();
    }
}

public class CreateUpdateVariableRequest
{
    public string Name { get; set; }
    public string? Value { get; set; }
    public bool IsSecret { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public VariableTarget Target { get; set; } = VariableTarget.Runtime;

    public Variable ToEntity()
    {
        return new()
        {
            Name = Name,
            Value = Value,
            IsSecret = IsSecret,
            Target = Target
        };
    }

    public void CopyToEntity(Variable variable)
    {
        variable.Name = Name;
        variable.Value = Value;
        variable.IsSecret = IsSecret;
        variable.Target = Target;
    }
}