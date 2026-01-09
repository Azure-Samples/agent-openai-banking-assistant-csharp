namespace TransactionsApi.Controllers;

/// <summary>
/// Controller for managing transaction-related operations.
/// </summary>
[ApiController]
[Route("[controller]")]
public class TransactionsController(
    ITransactionService transactionService,
    ILogger<TransactionsController> logger) : ControllerBase
{
    private readonly ITransactionService _transactionService = transactionService;
    private readonly ILogger<TransactionsController> _logger = logger;

    /// <summary>
    /// Retrieves transactions for a specific account, optionally filtered by recipient name.
    /// </summary>
    /// <param name="accountId">The account ID to retrieve transactions for.</param>
    /// <param name="recipientName">Optional recipient name filter.</param>
    /// <returns>A list of transactions matching the criteria.</returns>
    [HttpGet("{accountId}")]
    public ActionResult<List<Transaction>> GetTransactions(
        string accountId,
        [FromQuery(Name = "recipient_name")] string? recipientName)
    {
        _logger.LogInformation(
            "Received request to get transactions for accountid[{AccountId}]. Recipient filter is[{RecipientName}]",
            accountId,
            recipientName
        );

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
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Notifies a new transaction for a specific account.
    /// </summary>
    /// <param name="accountId">The account ID for the transaction.</param>
    /// <param name="transaction">The transaction details to notify.</param>
    /// <returns>An action result indicating success or failure.</returns>
    [HttpPost("{accountId}")]
    public IActionResult NotifyTransaction(
        string accountId,
        [FromBody] Transaction transaction)
    {
        _logger.LogInformation(
            "Received request to notify transaction for accountid[{AccountId}]. {Transaction}",
            accountId,
            transaction
        );

        try
        {
            _transactionService.NotifyTransaction(accountId, transaction);
            return Ok();
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid account ID");
            return BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Error notifying transaction");
            return StatusCode(500, ex.Message);
        }
    }
}
