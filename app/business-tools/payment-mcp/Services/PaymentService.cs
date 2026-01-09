namespace PaymentMcp.Services;

/// <summary>
/// Service for processing payment requests and notifying transactions.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="PaymentService"/> class.
/// </remarks>
/// <param name="logger">The logger to log information and errors.</param>
/// <param name="httpClient">The HTTP client for making API requests.</param>
/// <param name="transactionApiURL">The URL of the transaction API.</param>
public class PaymentService(
    ILogger<PaymentService> logger,
    HttpClient httpClient,
    string transactionApiURL) : IPaymentService
{
    private readonly ILogger<PaymentService> _logger = logger;
    private readonly HttpClient _httpClient = httpClient;
    private readonly string _transactionApiUrl = transactionApiURL;

    /// <summary>
    /// Processes a payment request asynchronously.
    /// </summary>
    /// <param name="payment">The payment details to process.</param>
    /// <exception cref="ArgumentException">Thrown when payment details are invalid.</exception>
    /// <exception cref="HttpRequestException">Thrown when there is an error notifying the transaction API.</exception>
    public async Task ProcessPaymentAsync(Payment payment)
    {
        // Validate AccountId
        if (string.IsNullOrEmpty(payment.AccountId))
            throw new ArgumentException("AccountId is empty or null");

        if (!int.TryParse(payment.AccountId, out _))
            throw new ArgumentException("AccountId is not a valid number");

        // Validate PaymentMethodId
        if (payment.PaymentType?.ToLower() != "transfer" &&
            string.IsNullOrEmpty(payment.PaymentMethodId))
            throw new ArgumentException("paymentMethodId is empty or null");

        if (!int.TryParse(payment.PaymentMethodId, out _))
            throw new ArgumentException("paymentMethodId is not a valid number");

        // Log payment details
        _logger.LogInformation($"Payment successful for: {payment}");

        // Convert Payment to Transaction
        var transaction = ConvertPaymentToTransaction(payment);

        // Send transaction to API
        _logger.LogInformation($"Notifying payment [{payment.Description}] for account[{transaction.AccountId}]");

        try
        {
            var response = await _httpClient.PostAsJsonAsync(
                $"{_transactionApiUrl}/transactions/{payment.AccountId}",
                transaction
            );

            response.EnsureSuccessStatusCode();
            var responseBody = await response.Content.ReadAsStringAsync();

            _logger.LogInformation($"Transaction notified for: {transaction}");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(
                ex,
                "Error notifying transaction for account {AccountId}",
                payment.AccountId
            );
            throw;
        }
    }

    /// <summary>
    /// Converts a payment object to a transaction object.
    /// </summary>
    /// <param name="payment">The payment details to convert.</param>
    /// <returns>A transaction object containing the converted details.</returns>
    private Transaction ConvertPaymentToTransaction(Payment payment)
    {
        return new Transaction() { 
            Id = Guid.NewGuid().ToString(),
            Description = payment.Description,
            Type = "outcome",
            RecipientName = payment.RecipientName,
            RecipientBankCode = payment.RecipientBankCode,
            PaymentType = payment.PaymentType,
            AccountId = payment.AccountId,
            Amount = payment.Amount,
            Timestamp = payment.Timestamp
        };
    }
}
