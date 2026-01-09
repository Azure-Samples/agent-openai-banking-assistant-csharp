namespace TransactionsApi.Interfaces;

/// <summary>
/// Interface for transaction service operations.
/// </summary>
public interface ITransactionService
{
    /// <summary>
    /// Retrieves transactions for a specific account filtered by recipient name.
    /// </summary>
    /// <param name="accountId">The account ID.</param>
    /// <param name="name">The recipient name to filter by.</param>
    /// <returns>A list of transactions matching the recipient name.</returns>
    public List<Transaction> GetTransactionsByRecipientName(string accountId, string name);
    
    /// <summary>
    /// Retrieves the last transactions for a specific account.
    /// </summary>
    /// <param name="accountId">The account ID.</param>
    /// <returns>A list of the most recent transactions.</returns>
    public List<Transaction> GetLastTransactions(string accountId);
    
    /// <summary>
    /// Notifies a new transaction for a specific account.
    /// </summary>
    /// <param name="accountId">The account ID.</param>
    /// <param name="transaction">The transaction to notify.</param>
    public void NotifyTransaction(string accountId, Transaction transaction);

}

