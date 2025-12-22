using Application.Contracts.Events.EventOptions;
using Application.Contracts.Persistence;
namespace Application.Contracts.Events;

public interface ISchemaBuilder
{
    public Task Create(IQueryRepository queryRepository, InsertEvent insertEvent);
}
