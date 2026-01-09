namespace PaymentMcp.Mcp.Tools;

/// <summary>
/// Represents a tool for processing payment requests.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="PaymentTool"/> class.
/// </remarks>
/// <param name="paymentService">The payment service to process payments.</param>
/// <param name="logger">The logger to log information and errors.</param>
[McpServerToolType]
public class PaymentTool(IPaymentService paymentService, ILogger<PaymentTool> logger)
{
    private readonly IPaymentService _paymentService = paymentService;
    private readonly ILogger<PaymentTool> _logger = logger;

    /// <summary>
    /// Submits a payment request asynchronously.
    /// </summary>
    /// <param name="payment">Payment information.</param>
    [McpServerTool(Name = "SubmitPayment"), Description("Submit a payment request.")]
    public async Task<string> SubmitPaymentAsync([Description("Payment to submit.")]Payment payment)
    {
        _logger.LogInformation("Received payment request: {Payment}", payment);

        try
        {
            await _paymentService.ProcessPaymentAsync(payment);
            return "Payment processed successfully.";
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid payment request");
            return "Invalid payment request.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing payment");
            return "Error processing payment.";
        }
    }
}