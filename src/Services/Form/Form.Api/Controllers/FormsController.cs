using Form.Api.Models;
using Microsoft.AspNetCore.Mvc;

namespace Form.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class FormsController : ControllerBase
{
    private readonly ILogger<FormsController> _logger;

    public FormsController(ILogger<FormsController> logger)
    {
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> ListForm()
    {
        return Ok();
    }
    
    [HttpPost]
    public async Task<IActionResult> CreateForm([FromBody]List<FormField> formFields)
    {
        return Created();
    }
    
    [HttpPut]
    public async Task<IActionResult> UpdateForm()
    {
        return Ok();
    }
}