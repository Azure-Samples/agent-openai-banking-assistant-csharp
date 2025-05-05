public record Account(
    string id,
    string userName,
    string accountHolderFullName,
    string currency,
    string activationDate,
    string balance,
    List<PaymentMethodSummary> paymentMethods
);
public record PaymentMethodSummary(
    string id,
    string type,
    string activationDate,
    string expirationDate
);
public record PaymentMethod(
    string id,
    string type,
    string activationDate,
    string expirationDate,
    string availableBalance,
    // card number is valued only for credit card type
    string cardNumber
);
public record Beneficiary(
    string id,
    string fullName,
    string bankCode,
    string bankName
);

