namespace BankingAssistant.Agents.Utils;

/// <summary>
/// A context class for agents to store and retrieve data during their operations.
/// </summary>
public class AgentContext : Dictionary<string, object>
{
    public AgentContext() : base()
    {
    }

    public AgentContext(string result) : base()
    {
        this["result"] = result;
    }

    public string Result
    {
        get => (string)this["result"];
        set => this["result"] = value;
    }

}
