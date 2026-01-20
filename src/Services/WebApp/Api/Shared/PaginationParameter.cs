using Microsoft.AspNetCore.Mvc;

namespace WebApp.Api.Shared;

public class PaginationParameter
{
    [FromQuery(Name = "per_page")]
    public int PerPage { get; set; } = 10;
    
    [FromQuery(Name = "page")]
    public int Page { get; set; } = 1;
}