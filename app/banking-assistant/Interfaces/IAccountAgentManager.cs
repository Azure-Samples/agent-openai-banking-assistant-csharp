namespace BankingAssistant.Interfaces;

/// <summary>
/// Interface for managing Account Agents.
/// </summary>
public interface IAccountAgentManager
{
    /// <summary>
    /// Asynchronously creates an AIAgent instance for account operations.    
    /// </summary>
    Task<AIAgent> CreateAgentAsync();
}
