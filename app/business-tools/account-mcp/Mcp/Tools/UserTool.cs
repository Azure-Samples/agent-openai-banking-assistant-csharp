[McpServerToolType]
public class UserTool
{
    private readonly IUserService _userService;
    private readonly ILogger<UserTool> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserTool"/> class.
    /// </summary>
    /// <param name="userService">The user service to retrieve user-related data.</param>
    /// <param name="logger">The logger instance for logging operations.</param>
    public UserTool(IUserService userService, ILogger<UserTool> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves the list of all accounts associated with a specific user.
    /// </summary>
    /// <param name="userName">The username of the logged-in user.</param>
    /// <returns>A task representing the asynchronous operation, containing the list of accounts.</returns>
    [McpServerTool(Name = "GetAccountsByUserName"), Description("Get the list of all accounts for a specific user.")]
    public async Task<List<Account>> GetAccountsByUserNameAsync([Description("userName once the user has logged.")] string userName)
    {
        _logger.LogInformation("Received request to get accounts for user: {UserName}", userName);
        return await _userService.GetAccountsByUserNameAsync(userName);
    }
}