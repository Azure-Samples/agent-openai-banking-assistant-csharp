using ModelContextProtocol.Server;
using System.ComponentModel;

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

    [McpServerTool(Name = "GetAccountDetails"), Description("Get account details and available payment methods.")]
    public Account GetAccountDetails([Description("id of specific account.")] string accountId)
    {
        _logger.LogInformation("Received request to get account details for account id: {AccountId}", accountId);
        return _accountService.GetAccountDetails(accountId);
    }


    [McpServerTool(Name = "GetPaymentMethodDetails"), Description("Get payment method detail with available balance.")]
    public PaymentMethod GetPaymentMethodDetails([Description("id of specific account.")] string accountId
        , [Description("id of specific payment method available for the account id.")] string methodId)
    {
        _logger.LogInformation("Received request to get payment method details for account id: {AccountId} and method id: {MethodId}",
            accountId, methodId);
        return _accountService.GetPaymentMethodDetails(methodId);
    }


    [McpServerTool(Name = "GetBeneficiaryDetails"), Description("Get list of registered beneficiaries for a specific account.")]
    public List<Beneficiary> GetBeneficiaryDetails([Description("id of specific account.")] string accountId)
    {
        _logger.LogInformation("Received request to get beneficiary details for account id: {AccountId}", accountId);
        return _accountService.GetRegisteredBeneficiary(accountId);
    }
}

