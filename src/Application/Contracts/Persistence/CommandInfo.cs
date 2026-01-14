
namespace Infrastructure.Persistence;

public class CommandInfo(string pureCommand, Dictionary<string, object>? parameters = null)
{
    public string PureCommand { get; set; } = pureCommand;
    public Dictionary<string, object> Parameters { get; set; } = parameters ?? new Dictionary<string, object>();
}
