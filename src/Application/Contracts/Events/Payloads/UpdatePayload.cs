
namespace Application.Contracts.Events.Payloads;

public class UpdatePayload(Dictionary<string, object>) : IPayload
{
    public Dictionary<string, object> Payload { get; init; }
    public Dictionary<string, object> GetValuePairs()
    {
        return Payload;
    }
}
