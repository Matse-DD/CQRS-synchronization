using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi;

using System.Net.Mime;

namespace Infrastructure.WebApi;

public static class Routes
{
    public static OpenApiInfo OpenApiInfo { get; } = new OpenApiInfo
    {
        Version = "v1",
        Title = "Movies API",
        Description = "A simple API to manage movies and movie events.",
        Contact = new OpenApiContact
        {
            Name = "Matse De Deyn | Lander Debeir | Pratik Lohani | Youri Haentjens",
            Email = "matse.de.deyn@student.howest.be"
        }
    };

    public static WebApplication MapRoutes(this WebApplication app)
    {
        MapCqrsRoutes(app);
        MapCqrsReplayRoutes(app);
        return app;
    }

    private static void MapCqrsRoutes(WebApplication app)
    {
        var cqrsGroup = app.MapGroup("/api/cqrs")
            .WithTags("Events")
            .WithDescription("Endpoints related to cqrs information")
            .WithOpenApi();

        cqrsGroup.MapGet("/", GetEventsByFiltersController.Invoke)
            .WithName("GetEventsByFilters")
            .WithDescription("Gets all events from the outbox based on the filters")
            .WithMetadata(new ProducesAttribute(MediaTypeNames.Application.Json))
            .WithOpenApi();
    }

    private static void MapCqrsReplayRoutes(WebApplication app)
    {
        var cqrsReplayGroup = app.MapGroup("/api/cqrs-replay")
            .WithTags("CqrsReplay")
            .WithDescription("Endpoints related to replay management")
            .WithOpenApi();

        cqrsReplayGroup.MapPost("/", ReplayTillEventController.Invoke)
            .WithName("ReplayTillEvent")
            .WithDescription("Replay till a certain eventid")
            .WithMetadata(new ConsumesAttribute(MediaTypeNames.Application.Json))
            .AddEndpointFilter<BodyValidatorFilter<ScheduleMovieEventBody>>()
            .WithOpenApi();

        // TODO look of this is inside the correct group maybe place this in seperate group
        cqrsReplayGroup.MapPost("/", TakeSnapshotController.Invoke)
            .WithName("TakeSnapshot")
            .WithDescription("Make a snapshot of the current state")
            .WithMetadata(new ConsumesAttribute(MediaTypeNames.Application.Json))
            .AddEndpointFilter<BodyValidatorFilter<ScheduleMovieEventBody>>()
            .WithOpenApi();
    }
}
