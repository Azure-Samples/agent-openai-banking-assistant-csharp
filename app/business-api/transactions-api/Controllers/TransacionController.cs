

[ApiController]
[Route("[controller]")]
public class TransactionsController : ControllerBase
{
    private readonly ITransactionService _transactionService;
    private readonly ILogger<TransactionsController> _logger;

    public TransactionsController(
        ITransactionService transactionService,
        ILogger<TransactionsController> logger)
    {
        _transactionService = transactionService;
        _logger = logger;
    }

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
