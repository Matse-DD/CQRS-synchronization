using Application.Contracts.Events.EventOptions;
using Application.WebApi;
using Application.WebApi.Events;
using Infrastructure.Replay;
using Microsoft.Extensions.DependencyInjection;

namespace Main.WebApi;

public static class UseCaseServices
{
    public static IServiceCollection AddUseCases(this IServiceCollection services)
    {
        return services.
            AddGetEventsByFilters();
    }

    public static IServiceCollection AddGetEventsByFilters(this IServiceCollection services)
    {
        return services.AddScoped<IUseCase<GetEventsByFilterInput, Task<IReadOnlyList<Event>>>>(
            ServiceProvider =>
            {
                // TODO momentele is voor replay mechanisme we willen alle events opvragen welk ding weet hiervan de recovery
                // mischien extraheren naar een aparte service
                 //replayer = ServiceProvider.GetRequiredService<Replayer>();
            });

    }
}
