namespace Application.Contracts.Events.Payloads;

public interface IPayload 
{
    public Dictionary<string, object> GetValuePairs();
}
