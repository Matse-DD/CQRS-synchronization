using Application.Contracts.Events;

namespace Infrastructure.Events.Mappings.MySQL;

public class MySqlUpdateEvent(IntermediateEvent intermediateEvent) : UpdateEvent(intermediateEvent)
{
    public override string GetCommand()
    {

        Console.WriteLine("impletent the update creator");
        return "update command";
    }
}
