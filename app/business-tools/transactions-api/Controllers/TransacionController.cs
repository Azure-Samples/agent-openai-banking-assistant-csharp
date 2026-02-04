namespace TransactionsApi.Controllers;

using System.Diagnostics;

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
    private static readonly ActivitySource ControllerActivitySource = new ActivitySource("TransactionsApi.Controllers.TransactionsController");

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
        using var activity = ControllerActivitySource.StartActivity("GetTransactions");
        activity?.SetTag("operation.name", "GetTransactions");
        activity?.SetTag("api.input.accountId", accountId);
        activity?.SetTag("api.input.recipientName", recipientName ?? "none");

        _logger.LogInformation(
            "Received request to get transactions for accountid[{AccountId}]. Recipient filter is[{RecipientName}]",
            accountId,
            recipientName
        );

        try
        {
            List<Transaction> result;
            if (!string.IsNullOrEmpty(recipientName))
            {
                result = _transactionService.GetTransactionsByRecipientName(accountId, recipientName);
            }
            else
            {
                result = _transactionService.GetLastTransactions(accountId);
            }
            
            activity?.SetTag("api.output.transactionCount", result?.Count ?? 0);
            return result;
        }
        catch (ArgumentException ex)
        {
            activity?.SetTag("api.output.status", "error");
            activity?.SetTag("api.output.exception", ex.GetType().Name);
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
        using var activity = ControllerActivitySource.StartActivity("NotifyTransaction");
        activity?.SetTag("operation.name", "NotifyTransaction");
        activity?.SetTag("api.input.accountId", accountId);
        activity?.SetTag("api.input.transactionAmount", transaction?.Amount ?? 0);
        activity?.SetTag("api.input.transactionRecipient", transaction?.Recipient ?? "unknown");

        _logger.LogInformation(
            "Received request to notify transaction for accountid[{AccountId}]. {Transaction}",
            accountId,
            transaction
        );

        try
        {
            _transactionService.NotifyTransaction(accountId, transaction);
            activity?.SetTag("api.output.status", "success");
            return Ok();
        }
        catch (ArgumentException ex)
        {
            activity?.SetTag("api.output.status", "invalid");
            activity?.SetTag("api.output.exception", ex.GetType().Name);
            _logger.LogWarning(ex, "Invalid account ID");
            return BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            activity?.SetTag("api.output.status", "error");
            activity?.SetTag("api.output.exception", ex.GetType().Name);
            _logger.LogError(ex, "Error notifying transaction");
            return StatusCode(500, ex.Message);
        }
    }
}
