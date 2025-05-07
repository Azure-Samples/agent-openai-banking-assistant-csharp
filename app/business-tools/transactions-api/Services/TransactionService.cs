public class TransactionService : ITransactionService
{
    private Dictionary<string, List<Transaction>> LastTransactions { get; } = new();
    private Dictionary<string, List<Transaction>> AllTransactions { get; } = new();

    public TransactionService()
    {
        LastTransactions["1010"] = new List<Transaction>
        {
            new Transaction("11", "Payment of the bill 334398", "outcome", "acme", "0001", "1010", "BankTransfer", 100.00m, DateTime.Parse("2024-4-01T12:00:00Z")),
            new Transaction("22", "Payment of the bill 4613", "outcome", "contoso", "0002", "1010", "CreditCard", 200.00m, DateTime.Parse("2024-3-02T12:00:00Z")),
            new Transaction("33", "Payment of the bill 724563", "outcome", "duff", "0003", "1010", "BankTransfer", 300.00m, DateTime.Parse("2023-10-03T12:00:00Z")),
            new Transaction("43", "Payment of the bill 8898943", "outcome", "wayne enterprises", "0004", "1010", "DirectDebit", 400.00m, DateTime.Parse("2023-8-04T12:00:00Z")),
            new Transaction("53", "Payment of the bill 19dee", "outcome", "oscorp", "0005", "1010", "BankTransfer", 500.00m, DateTime.Parse("2023-4-05T12:00:00Z"))
        };

        AllTransactions["1010"] = new List<Transaction>
        {
            new Transaction("11", "payment of bill id with 0001", "outcome", "acme", "A012TABTYT156!", "1010", "BankTransfer", 100.00m, DateTime.Parse("2024-4-01T12:00:00Z")),
            new Transaction("21", "Payment of the bill 4200", "outcome", "acme", "0002", "1010", "BankTransfer", 200.00m, DateTime.Parse("2024-1-02T12:00:00Z")),
            new Transaction("31", "Payment of the bill 3743", "outcome", "acme", "0003", "1010", "DirectDebit", 300.00m, DateTime.Parse("2023-10-03T12:00:00Z")),
            new Transaction("41", "Payment of the bill 8921", "outcome", "acme", "0004", "1010", "Transfer", 400.00m, DateTime.Parse("2023-8-04T12:00:00Z")),
            new Transaction("51", "Payment of the bill 7666", "outcome", "acme", "0005", "1010", "CreditCard", 500.00m, DateTime.Parse("2023-4-05T12:00:00Z")),

            new Transaction("12", "Payment of the bill 5517", "outcome", "contoso", "0001", "1010", "CreditCard", 100.00m, DateTime.Parse("2024-3-01T12:00:00Z")),
            new Transaction("22", "Payment of the bill 682222", "outcome", "contoso", "0002", "1010", "CreditCard", 200.00m, DateTime.Parse("2023-1-02T12:00:00Z")),
            new Transaction("32", "Payment of the bill 94112", "outcome", "contoso", "0003", "1010", "Transfer", 300.00m, DateTime.Parse("2022-10-03T12:00:00Z")),
            new Transaction("42", "Payment of the bill 23122", "outcome", "contoso", "0004", "1010", "Transfer", 400.00m, DateTime.Parse("2022-8-04T12:00:00Z")),
            new Transaction("52", "Payment of the bill 171443", "outcome", "contoso", "0005", "1010", "Transfer", 500.00m, DateTime.Parse("2020-4-05T12:00:00Z"))
        };
    }

    public List<Transaction> GetTransactionsByRecipientName(string accountId, string name)
    {
        ValidateAccountId(accountId);

        if (!AllTransactions.TryGetValue(accountId, out var transactions))
            return new List<Transaction>();

        return transactions
            .Where(t => t.RecipientName.ToLower().Contains(name.ToLower()))
            .ToList();
    }

    public List<Transaction> GetLastTransactions(string accountId)
    {
        ValidateAccountId(accountId);

        return LastTransactions.TryGetValue(accountId, out var transactions)
            ? transactions
            : new List<Transaction>();
    }

    public void NotifyTransaction(string accountId, Transaction transaction)
    {
        ValidateAccountId(accountId);

        if (!AllTransactions.TryGetValue(accountId, out var allTransactionsList))
            throw new InvalidOperationException($"Cannot find all transactions for account id: {accountId}");

        if (!LastTransactions.TryGetValue(accountId, out var lastTransactionsList))
            throw new InvalidOperationException($"Cannot find last transactions for account id: {accountId}");

        allTransactionsList.Add(transaction);
        lastTransactionsList.Add(transaction);
    }

    private void ValidateAccountId(string accountId)
    {
        if (string.IsNullOrEmpty(accountId))
            throw new ArgumentException("AccountId is empty or null");

        if (!int.TryParse(accountId, out _))
            throw new ArgumentException("AccountId is not a valid number");
    }
}
