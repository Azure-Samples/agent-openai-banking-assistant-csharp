namespace PaymentMcp.Mcp.Tools;

using System.Diagnostics;

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
    private static readonly ActivitySource ToolActivitySource = new ActivitySource("PaymentMcp.Tools.PaymentTool");

    /// <summary>
    /// Submits a payment request asynchronously.
    /// </summary>
    /// <param name="payment">Payment information.</param>
    [McpServerTool(Name = "SubmitPayment"), Description("Submit a payment request.")]
    public async Task<string> SubmitPaymentAsync([Description("Payment to submit.")]Payment payment)
    {
        using var activity = ToolActivitySource.StartActivity("SubmitPaymentAsync");
        activity?.SetTag("tool.name", "SubmitPayment");
        activity?.SetTag("tool.input.fromAccount", payment?.FromAccount ?? "unknown");
        activity?.SetTag("tool.input.toAccount", payment?.ToAccount ?? "unknown");
        activity?.SetTag("tool.input.amount", payment?.Amount ?? 0);

        _logger.LogInformation("Received payment request: {Payment}", payment);

        try
        {
            await _paymentService.ProcessPaymentAsync(payment);
            activity?.SetTag("tool.output.status", "success");
            return "Payment processed successfully.";
        }
        catch (ArgumentException ex)
        {
            activity?.SetTag("tool.output.status", "invalid");
            activity?.SetTag("tool.output.exception", ex.GetType().Name);
            _logger.LogWarning(ex, "Invalid payment request");
            return "Invalid payment request.";
        }
        catch (Exception ex)
        {
            activity?.SetTag("tool.output.status", "error");
            activity?.SetTag("tool.output.exception", ex.GetType().Name);
            activity?.SetTag("tool.output.message", ex.Message);
            activity?.RecordException(ex);
            _logger.LogError(ex, "Error processing payment");
            return "Error processing payment.";
        }
    }
}