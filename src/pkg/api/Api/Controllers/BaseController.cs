using Api.Authentications;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize(AuthenticationSchemes = Constants.MultiAuthSchemes)]
public class BaseController : ControllerBase
{
    protected const string ETagHeader = "ETag";
    
    protected const string IfMatchHeader = "If-Match";
}