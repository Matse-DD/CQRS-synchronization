using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Infrastructure.WebApi.Controllers;

public class GetEventsByFiltersController
{
    public static IResult Invoke([FromQuery] string? filter, [FromServices] SyncApplication application)
    {
        return Results.Ok();
    }
}
