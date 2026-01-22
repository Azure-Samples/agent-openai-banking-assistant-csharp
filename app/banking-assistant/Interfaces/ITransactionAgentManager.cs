namespace BankingAssistant.Interfaces;

/// <summary>
/// Interface for managing Transaction Agents.
/// </summary>
public interface ITransactionAgentManager
{
    /// <summary>
    /// Asynchronously creates an AIAgent instance for transactions operations.
    /// </summary>
    Task<AIAgent> CreateAgentAsync();
}
