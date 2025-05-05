public interface IAccountService
{
    /// <summary>
    /// Retrieves the list of registered beneficiaries for a given account.
    /// </summary>
    /// <param name="accountId">The ID of the account.</param>
    /// <returns>A task representing the asynchronous operation, containing a list of beneficiaries.</returns>
    public Task<List<Beneficiary>> GetRegisteredBeneficiaryAsync(string accountId);

    /// <summary>
    /// Retrieves the details of a specific payment method.
    /// </summary>
    /// <param name="paymentMethodId">The ID of the payment method.</param>
    /// <returns>A task representing the asynchronous operation, containing the payment method details.</returns>
    public Task<PaymentMethod> GetPaymentMethodDetailsAsync(string paymentMethodId);

    /// <summary>
    /// Retrieves the details of a specific account.
    /// </summary>
    /// <param name="accountId">The ID of the account.</param>
    /// <returns>A task representing the asynchronous operation, containing the account details.</returns>
    public Task<Account> GetAccountDetailsAsync(string accountId);
}
