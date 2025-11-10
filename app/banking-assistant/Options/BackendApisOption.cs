using System.ComponentModel.DataAnnotations;

/// <summary>
/// Represents configuration options for backend API endpoints used by the banking assistant.
/// </summary>
public record BackendApisOption
{
    /// <summary>
    /// Gets the base URL for the Payments API.
    /// </summary>
    [Required]
    public string PaymentsApiUrl { get; init; }
    
    /// <summary>
    /// Gets the base URL for the Transactions API.
    /// </summary>
    [Required]
    public string TransactionsApiUrl { get; init; }
    
    /// <summary>
    /// Gets the base URL for the Accounts API.
    /// </summary>
    [Required]
    public string AccountsApiUrl { get; init; }
}