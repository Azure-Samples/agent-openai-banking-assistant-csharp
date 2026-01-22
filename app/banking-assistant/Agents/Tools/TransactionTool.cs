using System.ComponentModel;

namespace BankingAssistant.Agents.Tools;

/// <summary>
/// Tool for retrieving transaction history by making HTTP calls to the Transactions History API.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="TransactionTool"/> class.
/// </remarks>
/// <param name="httpClientFactory">The HTTP client factory for creating clients.</param>
/// <param name="configuration">The application configuration.</param>
/// <param name="logger">The logger instance.</param>
public class TransactionTool(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<TransactionTool> logger)
{
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private readonly IConfiguration _configuration = configuration;
    private readonly ILogger<TransactionTool> _logger = logger;
    
    /// <summary>
    /// Retrieves transactions for a specific account from the TransactionsController.
    /// </summary>
    /// <param name="accountId">The account ID to retrieve transactions for.</param>
    /// <param name="recipientName">Optional filter by recipient name.</param>
    /// <returns>A JSON string containing the transaction data.</returns>
    [Description("Retrieve transaction history for a specific account, optionally filtered by recipient name")]
    public async Task<string> GetTransactionsAsync(
        [Description("The account ID to retrieve transactions for (required)")] string accountId,
        [Description("Optional: Filter transactions by recipient name")] string? recipientName = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(accountId, nameof(accountId));

        try
        {
            var baseUrl = _configuration["BackendAPIs:TransactionsApiUrl"]
                ?? throw new InvalidOperationException("BackendAPIs:TransactionsApiUrl is not configured");

            var httpClient = _httpClientFactory.CreateClient();
            httpClient.BaseAddress = new Uri(baseUrl);
            
            var url = $"transactions/{accountId}";
            if (!string.IsNullOrWhiteSpace(recipientName))
            {
                url += $"?recipient_name={Uri.EscapeDataString(recipientName)}";
            }

            _logger.LogInformation("Retrieving transactions for account {AccountId} with recipient filter: {RecipientName}", accountId, recipientName ?? "none");

            var response = await httpClient.GetAsync(url);
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("Successfully retrieved transactions for account {AccountId}", accountId);
                return content;
            }
            else
            {
                _logger.LogWarning("TransactionsController returned status {StatusCode} for account {AccountId}", response.StatusCode, accountId);
                return JsonSerializer.Serialize(new { error = $"Failed to retrieve transactions: {response.StatusCode}", accountId });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving transactions for account {AccountId}", accountId);
            return JsonSerializer.Serialize(new { error = ex.Message, accountId });
        }
    }
}
