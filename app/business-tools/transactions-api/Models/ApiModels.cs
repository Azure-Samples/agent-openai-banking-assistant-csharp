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