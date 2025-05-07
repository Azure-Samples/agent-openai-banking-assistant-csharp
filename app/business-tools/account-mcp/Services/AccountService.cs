public class AccountService : IAccountService
{
    private readonly Dictionary<string, Account> _accounts;
    private readonly Dictionary<string, PaymentMethod> _paymentMethods;

    public AccountService()
    {
        _accounts = new Dictionary<string, Account>();
        _paymentMethods = new Dictionary<string, PaymentMethod>();

        // Fill the dictionary with dummy data
        _accounts["1000"] = new Account(
            "1000",
            "alice.user@contoso.com",
            "Alice User",
            "USD",
            "2022-01-01",
            "5000",
            new List<PaymentMethodSummary>
            {
                    new PaymentMethodSummary("12345", "Visa", "2022-01-01", "2025-01-01"),
                    new PaymentMethodSummary("23456", "BankTransfer", "2022-01-01", "9999-01-01")
            }
        );

        _accounts["1010"] = new Account(
            "1010",
            "bob.user@contoso.com",
            "Bob User",
            "EUR",
            "2022-01-01",
            "10000",
            new List<PaymentMethodSummary>
            {
                    new PaymentMethodSummary("345678", "BankTransfer", "2022-01-01", "9999-01-01"),
                    new PaymentMethodSummary("55555", "Visa", "2022-01-01", "2026-01-01")
            }
        );

        _accounts["1020"] = new Account(
            "1020",
            "charlie.user@contoso.com",
            "Charlie User",
            "EUR",
            "2022-01-01",
            "3000",
            new List<PaymentMethodSummary>
            {
                    new PaymentMethodSummary("46748576", "DirectDebit", "2022-02-01", "9999-02-01")
            }
        );

        _paymentMethods["12345"] = new PaymentMethod("12345", "Visa", "2022-01-01", "2025-01-01", "500.00", "1234567812345678");
        _paymentMethods["55555"] = new PaymentMethod("55555", "Visa", "2024-01-01", "2028-01-01", "350.00", "637362551913266");
        _paymentMethods["23456"] = new PaymentMethod("23456", "BankTransfer", "2022-01-01", "9999-01-01", "5000.00", null);
        _paymentMethods["345678"] = new PaymentMethod("345678", "BankTransfer", "2022-01-01", "9999-01-01", "10000.00", null);
    }

    /// <summary>
    /// Retrieves the details of a specific account asynchronously.
    /// </summary>
    /// <param name="accountId">The ID of the account to retrieve.</param>
    /// <returns>A task representing the asynchronous operation, containing the account details if found, or null otherwise.</returns>
    public async Task<Account> GetAccountDetailsAsync(string accountId)
    {
        ValidateAccountId(accountId);
        return await Task.FromResult(_accounts.TryGetValue(accountId, out var account) ? account : null);
    }

    public async Task<PaymentMethod> GetPaymentMethodDetailsAsync(string paymentMethodId)
    {
        ValidateAccountId(paymentMethodId);
        return await Task.FromResult(_paymentMethods.TryGetValue(paymentMethodId, out var paymentMethod) ? paymentMethod : null);
    }

    public async Task<List<Beneficiary>> GetRegisteredBeneficiaryAsync(string accountId)
    {
        ValidateAccountId(accountId);

        // Return dummy list of beneficiaries
        var beneficiaries = new List<Beneficiary>
            {
                new Beneficiary("1", "Mike ThePlumber", "123456789", "Intesa Sanpaolo"),
                new Beneficiary("2", "Jane TheElectrician", "987654321", "UBS")
            };
        return await Task.FromResult(beneficiaries);
    }

    // Optionally keep the old synchronous methods if needed, or remove them if not required.

    private void ValidateAccountId(string accountId)
    {
        if (string.IsNullOrEmpty(accountId))
            throw new ArgumentException("AccountId is empty or null");

        if (!int.TryParse(accountId, out _))
            throw new ArgumentException("AccountId is not a valid number");
    }
}

