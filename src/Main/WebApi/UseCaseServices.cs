using Application.Contracts.Events.EventOptions;
using Application.Contracts.Persistence;
using Application.WebApi;
using Application.WebApi.Contracts.Ports;
using Application.WebApi.Events;
using Infrastructure.Replay;
using Microsoft.Extensions.DependencyInjection;

namespace Main.WebApi;

public static class UseCaseServices
{
    public static IServiceCollection AddUseCases(this IServiceCollection services)
    {
        return services.
            AddGetEventsByFiltersQuery();
    }

    public static IServiceCollection AddGetEventsByFiltersQuery(this IServiceCollection services)
    {
        return services.AddScoped<IUseCase<GetEventsByFiltersInput, Task<IReadOnlyList<Event>>>>(
            ServiceProvider =>
            {
                IGetEventsByFiltersQuery allEventsQuery = ServiceProvider.GetRequiredService<IGetEventsByFiltersQuery>();

                ICommandRepository commandRepository = ServiceProvider.GetRequiredService<ICommandRepository>();

                return new GetEventsByFilters(allEventsQuery);
            });
    }
}
