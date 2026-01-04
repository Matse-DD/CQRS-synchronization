using Application.Contracts.Events.EventOptions;
using Application.Contracts.Persistence;
using Application.CoreSyncContracts.Replay;
using Application.WebApi;
using Application.WebApi.Contracts.Ports;
using Application.WebApi.Events;
using Application.WebApi.Replay;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Main.WebApi;

public static class UseCaseServices
{
    public static IServiceCollection AddUseCases(this IServiceCollection services)
    {
        return services
            .AddUseCaseGetEventsByFiltersQuery()
            .AddUseCaseReplayTillEvent();
    }

    public static IServiceCollection AddUseCaseGetEventsByFiltersQuery(this IServiceCollection services)
    {
        return services
            .AddScoped<IUseCase<GetEventsByFiltersInput, Task<IReadOnlyList<Event>>>>(ServiceProvider =>
            {
                IGetEventsByFiltersQuery getEventsByFiltersQuery = ServiceProvider.GetRequiredService<IGetEventsByFiltersQuery>();
                return new GetEventsByFilters(getEventsByFiltersQuery);
            });
    }

    public static IServiceCollection AddUseCaseReplayTillEvent(this IServiceCollection services)
    {
        return services
            .AddScoped<IUseCase<ReplayTillEventInput, Task>>(ServiceProvider =>
            {
                IReplay replayer = ServiceProvider.GetRequiredService<IReplay>();
                ILogger<ReplayTillEvent> logger = ServiceProvider.GetRequiredService<ILogger<ReplayTillEvent>>();

                return new ReplayTillEvent(replayer, logger);
            });
    }
}
