
namespace Infrastructure.Persistence;

public class PersistenceCommandInfo(string pureCommand, Dictionary<string, string>? parameters = null)
{
    public string PureCommand { get; set; } = pureCommand;
    public Dictionary<string, string> Parameters { get; set; } = parameters ?? new Dictionary<string, string>();
}
