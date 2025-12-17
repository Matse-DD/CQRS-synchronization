using Application.Contracts.Events;
using Application.Contracts.Events.EventOptions;
using Application.Contracts.Persistence;

namespace ApplicationTests.Shared;

public class MockSchemaBuilder : ISchemaBuilder
{
    public Task Map(IQueryRepository queryRepository, InsertEvent insertEvent)
    {
        return Task.CompletedTask;
    }
}
