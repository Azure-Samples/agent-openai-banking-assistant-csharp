using ModelContextProtocol.Server;
using System.ComponentModel;

[McpServerToolType]
public class TransactionTool
{
    private readonly ITransactionService _transactionService;
    private readonly ILogger<TransactionTool> _logger;

    public TransactionTool(ITransactionService transactionService, ILogger<TransactionTool> logger)
    {
        _transactionService = transactionService;
        _logger = logger;
    }

    [McpServerTool(Name = "GetTransactions"), Description("Get transactions list.")]
    public IList<Transaction> GetTransactions([Description("id of specific account.")] string accountId,
        [Description("Name of the payee, recipient.")] string? recipientName)
    {
        _logger.LogInformation("Received request to get transactions for accountid[{AccountId}]. Recipient filter is[{RecipientName}]",
            accountId, recipientName);

        try
        {
            if (!string.IsNullOrEmpty(recipientName))
            {
                return _transactionService.GetTransactionsByRecipientName(accountId, recipientName);
            }
            else
            {
                return _transactionService.GetLastTransactions(accountId);
            }
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid account ID");
            return new List<Transaction>();

        }
    }

    [McpServerTool(Name = "NotifyTransaction"), Description("Notify the banking transaction so that it's being stored in the history.")]
    public void NotifyTransaction([Description("id of specific account.")] string accountId,
        [Description("Transaction to notify.")] Transaction transaction)
    {
        _logger.LogInformation("Received request to notify transaction for accountid[{AccountId}]. {Transaction}",
            accountId, transaction);

        try
        {
            _transactionService.NotifyTransaction(accountId, transaction);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid account ID");
        }
    }
}