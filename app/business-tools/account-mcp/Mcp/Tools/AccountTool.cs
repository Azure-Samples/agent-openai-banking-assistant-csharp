namespace AccountMcp.Mcp.Tools;

using System.Diagnostics;

/// <summary>
/// MCP tool for managing account-related operations.
/// </summary>
[McpServerToolType]
public class AccountTool(IAccountService accountService, ILogger<AccountTool> logger)
{
    private readonly IAccountService _accountService = accountService;
    private readonly ILogger<AccountTool> _logger = logger;
    private static readonly ActivitySource ToolActivitySource = new ActivitySource("AccountMcp.Tools.AccountTool");

    /// <summary>
    /// Retrieves account details and available payment methods for a specific account.
    /// </summary>
    /// <param name="accountId">The ID of the specific account.</param>
    /// <returns>A task representing the asynchronous operation, containing the account details.</returns>
    [McpServerTool(Name = "GetAccountDetails"), Description("Get account details and available payment methods.")]
    public async Task<Account?> GetAccountDetailsAsync([Description("id of specific account.")] string accountId)
    {
        using var activity = ToolActivitySource.StartActivity("GetAccountDetailsAsync");
        activity?.SetTag("tool.name", "GetAccountDetails");
        activity?.SetTag("tool.input.accountId", accountId);

        _logger.LogInformation("Received request to get account details for account id: {AccountId}", accountId);
        
        var result = await _accountService.GetAccountDetailsAsync(accountId);
        
        activity?.SetTag("tool.output.found", result != null);
        if (result != null)
        {
            activity?.SetTag("tool.output.accountStatus", result.Status ?? "unknown");
        }
        
        return result;
    }

    /// <summary>
    /// Retrieves payment method details, including the available balance, for a specific account and payment method.
    /// </summary>
    /// <param name="accountId">The ID of the specific account.</param>
    /// <param name="methodId">The ID of the specific payment method available for the account.</param>
    /// <returns>A task representing the asynchronous operation, containing the payment method details.</returns>
    [McpServerTool(Name = "GetPaymentMethodDetails"), Description("Get payment method detail with available balance.")]
    public async Task<PaymentMethod?> GetPaymentMethodDetailsAsync(
        [Description("id of specific account.")] string accountId,
        [Description("id of specific payment method available for the account id.")] string methodId)
    {
        using var activity = ToolActivitySource.StartActivity("GetPaymentMethodDetailsAsync");
        activity?.SetTag("tool.name", "GetPaymentMethodDetails");
        activity?.SetTag("tool.input.accountId", accountId);
        activity?.SetTag("tool.input.methodId", methodId);

        _logger.LogInformation("Received request to get payment method details for account id: {AccountId} and method id: {MethodId}", accountId, methodId);
        
        var result = await _accountService.GetPaymentMethodDetailsAsync(methodId);
        
        activity?.SetTag("tool.output.found", result != null);
        if (result != null)
        {
            activity?.SetTag("tool.output.methodType", result.Type ?? "unknown");
        }
        
        return result;
    }

    /// <summary>
    /// Retrieves the list of registered beneficiaries for a specific account.
    /// </summary>
    /// <param name="accountId">The ID of the specific account.</param>
    /// <returns>A task representing the asynchronous operation, containing the list of beneficiaries.</returns>
    [McpServerTool(Name = "GetBeneficiaryDetails"), Description("Get list of registered beneficiaries for a specific account.")]
    public async Task<List<Beneficiary>> GetBeneficiaryDetailsAsync([Description("id of specific account.")] string accountId)
    {
        using var activity = ToolActivitySource.StartActivity("GetBeneficiaryDetailsAsync");
        activity?.SetTag("tool.name", "GetBeneficiaryDetails");
        activity?.SetTag("tool.input.accountId", accountId);

        _logger.LogInformation("Received request to get beneficiary details for account id: {AccountId}", accountId);
        
        var result = await _accountService.GetRegisteredBeneficiaryAsync(accountId);
        
        activity?.SetTag("tool.output.beneficiaryCount", result?.Count ?? 0);
        
        return result;
    }
}

