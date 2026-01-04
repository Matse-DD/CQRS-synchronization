using Infrastructure.Persistence.QueryRepository;
using Main.Initialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;

namespace Main.WebApi;

public static class AdditionalServices
{
    public static IServiceCollection AddSyncInfrastructure(this IServiceCollection services, SyncBuilder syncBuilder)
    {
        foreach (var service in syncBuilder.GetServices())
        {
            services.Add(service);
        }

        services.AddScoped<SyncApplication>();
        return services;
    }

    public static async Task SetBasicDatabaseStructureUp(SyncBuilder syncBuilder, ILogger<MySqlQueryRepository> mySqlQueryLogger)
    {
        await MySqlQueryRepository.CreateBasicStructureQueryDatabase(
            syncBuilder.QueryDatabaseName(),
            syncBuilder.ConnectionString(),
            mySqlQueryLogger
        );
    }
}
