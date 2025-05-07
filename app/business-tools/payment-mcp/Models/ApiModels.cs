using System.ComponentModel;
using System.Text.Json.Serialization;


public record Payment
{
    [Description("AccountId for the payment.")]
    public required string AccountId { get; init; } 
    [Description("Description of the payment.")]
    public required string Description { get; init; }
    [Description("The type of payment: creditcard, banktransfer, directdebit, visa, mastercard, paypal, etc.")]
    public string PaymentType { get; init; }
    [Description("ID of the payment method.")]
    public string PaymentMethodId { get; init; }
    [Description("Name of the recipient.")]
    public required string RecipientName { get; init; }
    [Description("Bank code of the recipient.")]
    public required string RecipientBankCode { get; init; }
    [Description("Amount of the payment.")]
    public required string Amount { get; init; }
    [Description("Timestamp of the payment.")]
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
