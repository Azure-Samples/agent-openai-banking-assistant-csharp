namespace TransactionsApi.Models;

/// <summary>
/// Represents a financial transaction.
/// </summary>
/// <param name="Id">The unique transaction identifier.</param>
/// <param name="Description">The transaction description.</param>
/// <param name="Type">The transaction type (e.g., income, outcome).</param>
/// <param name="RecipientName">The name of the transaction recipient.</param>
/// <param name="RecipientBankCode">The bank code of the recipient.</param>
/// <param name="AccountId">The account ID associated with the transaction.</param>
/// <param name="PaymentType">The payment method type.</param>
/// <param name="Amount">The transaction amount.</param>
/// <param name="Timestamp">The timestamp when the transaction occurred.</param>
public record Transaction(
      string Id,
      string Description,
      string Type,
      string RecipientName,
      string RecipientBankCode,
      string AccountId,
      string PaymentType,
      decimal Amount,
      DateTime Timestamp
  );