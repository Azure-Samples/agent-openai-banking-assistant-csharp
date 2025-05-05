
/// <summary>
/// Represents a tool for processing payment requests.
/// </summary>
[McpServerToolType]
public class PaymentTool
{
    private readonly IPaymentService _paymentService;
    private readonly ILogger<PaymentTool> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PaymentTool"/> class.
    /// </summary>
    /// <param name="paymentService">The payment service to process payments.</param>
    /// <param name="logger">The logger to log information and errors.</param>
    public PaymentTool(IPaymentService paymentService, ILogger<PaymentTool> logger)
    {
        _paymentService = paymentService;
        _logger = logger;
    }

    /// <summary>
    /// Submits a payment request asynchronously.
    /// </summary>
    /// <param name="accountId">ID of the account.</param>
    /// <param name="description">Description of the payment.</param>
    /// <param name="recipientName">Name of the recipient.</param>
    /// <param name="recipientBankCode">Bank code of the recipient.</param>
    /// <param name="amount">Amount of the payment.</param>
    /// <param name="timestamp">Timestamp of the payment.</param>
    /// <param name="paymentType">Type of the payment (optional).</param>
    /// <param name="paymentMethodId">ID of the payment method (optional).</param>
    /// <returns>A string indicating the result of the payment processing.</returns>
    [McpServerTool(Name = "SubmitPayment"), Description("Submit a payment request.")]
    public async Task<string> SubmitPaymentAsync(
    [Description("ID of the account")] string accountId,
    [Description("Description of the payment")] string description,
    [Description("Name of the recipient")] string recipientName,
    [Description("Bank code of the recipient")] string recipientBankCode,
    [Description("Amount of the payment")] string amount,
    [Description("Timestamp of the payment")] string timestamp,
    [Description("Type of the payment")] string? paymentType = null,
    [Description("ID of the payment method")] string? paymentMethodId = null)
    {
        var payment = new Payment
        {
            AccountId = accountId,
            Description = description,
            RecipientName = recipientName,
            RecipientBankCode = recipientBankCode,
            Amount = amount,
            Timestamp = timestamp,
            PaymentType = paymentType,
            PaymentMethodId = paymentMethodId
        };

        _logger.LogInformation("Received payment request: {Payment}", payment);

        try
        {
            await _paymentService.ProcessPaymentAsync(payment);
            return "Payment processed successfully.";
        }
        catch (ArgumentException ex)
        {
            Console.WriteLine(ex.Message);
            _logger.LogWarning(ex, "Invalid payment request");
            return "Invalid payment request.";
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            _logger.LogError(ex, "Error processing payment");
            return "Error processing payment.";
        }
    }
}