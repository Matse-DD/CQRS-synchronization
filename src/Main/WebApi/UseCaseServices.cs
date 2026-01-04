using Application.Contracts.Events.EventOptions;
using Application.Contracts.Persistence;
using Application.WebApi;
using Application.WebApi.Contracts.Ports;
using Application.WebApi.Events;
using Microsoft.Extensions.DependencyInjection;

namespace Main.WebApi;

public static class UseCaseServices
{
    public static IServiceCollection AddUseCases(this IServiceCollection services)
    {
        return services.
            AddUseCaseGetEventsByFiltersQuery();
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
}
