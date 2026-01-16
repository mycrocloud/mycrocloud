using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize]
public class BaseController : ControllerBase
{
    protected const string ETagHeader = "ETag";
    
    protected const string IfMatchHeader = "If-Match";
}