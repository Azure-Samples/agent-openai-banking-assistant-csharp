
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

/// <summary>
/// Interface for managing Transactions Reporting Agents.
/// </summary>
public interface ITransactionsReportingAgentManager
{
    /// <summary>
    /// Asynchronously creates an AIAgent instance for transactions operations.
    /// </summary>
    Task<AIAgent> CreateAgentAsync();
}