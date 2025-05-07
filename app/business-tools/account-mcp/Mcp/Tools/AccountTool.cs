
[McpServerToolType]
public class AccountTool
{
    private readonly IAccountService _accountService;
    private readonly ILogger<AccountTool> _logger;

    public AccountTool(IAccountService accountService, ILogger<AccountTool> logger)
    {
        _accountService = accountService;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves account details and available payment methods for a specific account.
    /// </summary>
    /// <param name="accountId">The ID of the specific account.</param>
    /// <returns>A task representing the asynchronous operation, containing the account details.</returns>
    [McpServerTool(Name = "GetAccountDetails"), Description("Get account details and available payment methods.")]
    public async Task<Account> GetAccountDetailsAsync([Description("id of specific account.")] string accountId)
    {
        _logger.LogInformation("Received request to get account details for account id: {AccountId}", accountId);
        return await _accountService.GetAccountDetailsAsync(accountId);
    }

    /// <summary>
    /// Retrieves payment method details, including the available balance, for a specific account and payment method.
    /// </summary>
    /// <param name="accountId">The ID of the specific account.</param>
    /// <param name="methodId">The ID of the specific payment method available for the account.</param>
    /// <returns>A task representing the asynchronous operation, containing the payment method details.</returns>
    [McpServerTool(Name = "GetPaymentMethodDetails"), Description("Get payment method detail with available balance.")]
    public async Task<PaymentMethod> GetPaymentMethodDetailsAsync(
        [Description("id of specific account.")] string accountId,
        [Description("id of specific payment method available for the account id.")] string methodId)
    {
        _logger.LogInformation("Received request to get payment method details for account id: {AccountId} and method id: {MethodId}",
            accountId, methodId);
        return await _accountService.GetPaymentMethodDetailsAsync(methodId);
    }

    /// <summary>
    /// Retrieves the list of registered beneficiaries for a specific account.
    /// </summary>
    /// <param name="accountId">The ID of the specific account.</param>
    /// <returns>A task representing the asynchronous operation, containing the list of beneficiaries.</returns>
    [McpServerTool(Name = "GetBeneficiaryDetails"), Description("Get list of registered beneficiaries for a specific account.")]
    public async Task<List<Beneficiary>> GetBeneficiaryDetailsAsync([Description("id of specific account.")] string accountId)
    {
        _logger.LogInformation("Received request to get beneficiary details for account id: {AccountId}", accountId);
        return await _accountService.GetRegisteredBeneficiaryAsync(accountId);
    }
}

