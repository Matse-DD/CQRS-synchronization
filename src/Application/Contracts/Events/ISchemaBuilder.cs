using Application.Contracts.Events.EventOptions;
using Application.Contracts.Persistence;
namespace Application.Contracts.Events;

public interface ISchemaBuilder
{
    public Task Map(IQueryRepository queryRepository, InsertEvent insertEvent);
}
