using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApp.Api.Filters;
using WebApp.Domain.Entities;
using WebApp.Infrastructure;

namespace WebApp.Api.Controllers;

[Route("apps/{appId:int}/[controller]")]
[TypeFilter<AppOwnerActionFilter>(Arguments = ["appId"])]
public class VariablesController(AppDbContext appDbContext) : BaseController
{
    [HttpGet]
    public async Task<IActionResult> List(int appId)
    {
        var variables = await appDbContext.Variables.Where(v => v.AppId == appId).ToListAsync();
        return Ok(variables.Select(variable => new
        {
            variable.Id,
            variable.Name,
            variable.IsSecret,
            ValueType = variable.ValueType.ToString(),
            variable.StringValue,
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
        return Created("", new { entity.Id });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Put(int appId, int id, CreateUpdateVariableRequest createUpdateVariableRequest)
    {
        var entity = await appDbContext.Variables.SingleAsync(v => v.AppId == appId && v.Id == id);
        createUpdateVariableRequest.CopyToEntity(entity);
        appDbContext.Variables.Update(entity);
        await appDbContext.SaveChangesAsync();
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
            ValueType = variable.ValueType.ToString(),
            variable.StringValue,
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
        return NoContent();
    }
}

public class CreateUpdateVariableRequest
{
    public string Name { get; set; }
    public string? StringValue { get; set; }
    
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public VariableValueType ValueType { get; set; }
    
    public bool IsSecret { get; set; }

    public Variable ToEntity()
    {
        return new()
        {
            Name = Name,
            ValueType = ValueType,
            StringValue = StringValue,
            IsSecret = IsSecret
        };
    }

    public void CopyToEntity(Variable variable)
    {
        variable.Name = Name;
        variable.ValueType = ValueType;
        variable.StringValue = StringValue;
        variable.IsSecret = IsSecret;
    }
}