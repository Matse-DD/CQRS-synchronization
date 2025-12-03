using Application.Contracts.Events;

namespace Infrastructure.Events.Mappings.MySQL;

public class MySqlDeleteEvent(IntermediateEvent intermediateEvent) : DeleteEvent(intermediateEvent)
{
    public override string GetCommand()
    {
        return
            $"DELETE FROM {AggregateName} WHERE {Condition}";
    }
}
