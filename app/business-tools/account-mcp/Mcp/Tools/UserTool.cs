namespace AccountMcp.Mcp.Tools;

using System.Diagnostics;

/// <summary>
/// MCP tool for managing user-related operations.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="UserTool"/> class.
/// </remarks>
/// <param name="userService">The user service to retrieve user-related data.</param>
/// <param name="logger">The logger instance for logging operations.</param>
[McpServerToolType]
public class UserTool(IUserService userService, ILogger<UserTool> logger)
{
    private readonly IUserService _userService = userService;
    private readonly ILogger<UserTool> _logger = logger;
    private static readonly ActivitySource ToolActivitySource = new ActivitySource("AccountMcp.Tools.UserTool");

    /// <summary>
    /// Retrieves the list of all accounts associated with a specific user.
    /// </summary>
    /// <param name="userName">The username of the logged-in user.</param>
    /// <returns>A task representing the asynchronous operation, containing the list of accounts.</returns>
    [McpServerTool(Name = "GetAccountsByUserName"), Description("Get the list of all accounts for a specific user.")]
    public async Task<List<Account>> GetAccountsByUserNameAsync([Description("userName once the user has logged.")] string userName)
    {
        using var activity = ToolActivitySource.StartActivity("GetAccountsByUserNameAsync");
        activity?.SetTag("tool.name", "GetAccountsByUserName");
        activity?.SetTag("tool.input.userName", userName);

        _logger.LogInformation("Received request to get accounts for user: {UserName}", userName);
        
        var result = await _userService.GetAccountsByUserNameAsync(userName);
        
        activity?.SetTag("tool.output.accountCount", result?.Count ?? 0);
        
        return result;
    }
}