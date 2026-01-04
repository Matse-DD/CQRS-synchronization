using Application.Contracts.Events.Factory;
using Application.Contracts.Persistence;
using Application.WebApi.Contracts.Ports;
using Infrastructure.WebApi.Queries;
using Microsoft.Extensions.DependencyInjection;

namespace Main.WebApi;

public static class QueryServices
{
    public static IServiceCollection AddQueries(this IServiceCollection services)
    {
        return services.AddGetEventsByFiltersQuery();

    }

    private static IServiceCollection AddGetEventsByFiltersQuery(this IServiceCollection services)
    {
        return services.AddScoped<IGetEventsByFiltersQuery>(
            sp => new GetEventsByFiltersQuery(sp.GetRequiredService<ICommandRepository>(), sp.GetRequiredService<IEventFactory>())
        );
    }
}
