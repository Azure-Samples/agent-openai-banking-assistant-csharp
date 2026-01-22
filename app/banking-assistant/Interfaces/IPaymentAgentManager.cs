namespace BankingAssistant.Interfaces;

/// <summary>
/// Interface for managing Payment Agents.
/// </summary>
public interface IPaymentAgentManager
{
    /// <summary>
    /// Asynchronously creates an AIAgent instance for payment operations.    
    /// </summary>
    Task<AIAgent> CreateAgentAsync();
}
