
using Application.Contracts.Events.Payloads;

namespace Infrastructure.Events.Mappings.MySQL;

public class InsertPayload(Dictionary<string, object> payload) : IPayload
{
    public Dictionary<string, object> Payload { get; init; } = payload;

    public Dictionary<string, object> GetValuePairs()
    {
        return Payload;
    }
}
