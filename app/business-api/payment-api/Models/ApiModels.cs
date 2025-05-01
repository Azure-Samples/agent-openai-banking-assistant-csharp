using System.ComponentModel;
using System.Text.Json.Serialization;


public record Payment
{
    [JsonPropertyName("accountId")]
    [Description("ID of the account")]
    public required string AccountId { get; init; }

    [JsonPropertyName("description")]
    [Description("Description of the payment")]
    public required string Description { get; init; }

    [JsonPropertyName("paytmentType")]
    [Description("Type of the payment")]
    public string? PaymentType { get; init; }

    [JsonPropertyName("paymentMethodId")]
    [Description("ID of the payment method")]
    public string? PaymentMethodId { get; init; }

    [JsonPropertyName("recipientName")]
    [Description("Name of the recipient")]
    public required string RecipientName { get; init; }

    [JsonPropertyName("recipientBankCode")]
    [Description("Bank code of the recipient")]
    public required string RecipientBankCode { get; init; }

    [JsonPropertyName("amount")]
    [Description("Amount of the payment")]
    public required string Amount { get; init; }

    [JsonPropertyName("timestamp")]
    [Description("Timestamp of the payment")]
    public required string Timestamp { get; init; }
}

public record Transaction
{
    public string Id { get; init; }

    public string Description { get; init; }

    public string Type { get; init; }

    public string RecipientName { get; init; }

    public string RecipientBankCode { get; init; }

    public string AccountId { get; init; }

    public string PaymentType { get; init; }

    public string? Amount { get; init; }

    public string? Timestamp { get; init; }
}
