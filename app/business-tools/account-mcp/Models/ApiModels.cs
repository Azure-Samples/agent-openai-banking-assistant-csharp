namespace AccountMcp.Models;

/// <summary>
/// Represents a banking account with associated details.
/// </summary>
/// <param name="id">The unique account identifier.</param>
/// <param name="userName">The username of the account holder.</param>
/// <param name="accountHolderFullName">The full name of the account holder.</param>
/// <param name="currency">The currency type for the account.</param>
/// <param name="activationDate">The date when the account was activated.</param>
/// <param name="balance">The current balance of the account.</param>
/// <param name="paymentMethods">List of payment methods associated with the account.</param>
public record Account(
    string id,
    string userName,
    string accountHolderFullName,
    string currency,
    string activationDate,
    string balance,
    List<PaymentMethodSummary>? paymentMethods
);

/// <summary>
/// Represents a summary of a payment method.
/// </summary>
/// <param name="id">The unique payment method identifier.</param>
/// <param name="type">The type of payment method (e.g., Visa, BankTransfer).</param>
/// <param name="activationDate">The date when the payment method was activated.</param>
/// <param name="expirationDate">The date when the payment method expires.</param>
public record PaymentMethodSummary(
    string id,
    string type,
    string activationDate,
    string expirationDate
);

/// <summary>
/// Represents detailed information about a payment method.
/// </summary>
/// <param name="id">The unique payment method identifier.</param>
/// <param name="type">The type of payment method.</param>
/// <param name="activationDate">The date when the payment method was activated.</param>
/// <param name="expirationDate">The date when the payment method expires.</param>
/// <param name="availableBalance">The available balance for this payment method.</param>
/// <param name="cardNumber">The card number (only for credit card types).</param>
public record PaymentMethod(
    string id,
    string type,
    string activationDate,
    string expirationDate,
    string availableBalance,
    // card number is valued only for credit card type
    string? cardNumber
);

/// <summary>
/// Represents a registered beneficiary for payment transfers.
/// </summary>
/// <param name="id">The unique beneficiary identifier.</param>
/// <param name="fullName">The full name of the beneficiary.</param>
/// <param name="bankCode">The bank code of the beneficiary's bank.</param>
/// <param name="bankName">The name of the beneficiary's bank.</param>
public record Beneficiary(
    string id,
    string fullName,
    string bankCode,
    string bankName
);

