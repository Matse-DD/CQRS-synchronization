using Application.Contracts.Events.EventOptions;
using Application.WebApi;
using Application.WebApi.Events;
using Application.WebApi.Replay;
using Infrastructure.WebApi.Controllers.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Infrastructure.WebApi.Controllers;

public class ReplayTillEventController() : IController
{

    public static async Task<Results<Ok, BadRequest>> Invoke(
        [AsParameters] ReplayTillEventParameters parameters,
        [FromServices] IUseCase<ReplayTillEventInput, Task> replayTillEvent
    )
    {
        ReplayTillEventInput input = new(
                parameters.EventId
            );
        try
        {
            await replayTillEvent.Execute(input);
        }
        catch
        {
            return TypedResults.BadRequest();
        }
        return TypedResults.Ok();
    }
}

public sealed record ReplayTillEventParameters
{
    public required string? EventId { get; init; }
}

