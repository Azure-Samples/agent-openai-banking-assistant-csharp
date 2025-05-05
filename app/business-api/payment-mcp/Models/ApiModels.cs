using System.ComponentModel;
using System.Text.Json.Serialization;


public record Payment
{
    public required string AccountId { get; init; }
    public required string Description { get; init; }
    public string? PaymentType { get; init; }
    public string? PaymentMethodId { get; init; }
    public required string RecipientName { get; init; }
    public required string RecipientBankCode { get; init; }
    public required string Amount { get; init; }
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
