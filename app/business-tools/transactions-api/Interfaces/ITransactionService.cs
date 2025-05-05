public interface ITransactionService
{
    public List<Transaction> GetTransactionsByRecipientName(string accountId, string name);
    public List<Transaction> GetLastTransactions(string accountId);
    public void NotifyTransaction(string accountId, Transaction transaction);

}

