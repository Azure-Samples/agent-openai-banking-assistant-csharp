using ModelContextProtocol.Server;
using System.ComponentModel;

[McpServerToolType]
public class UserTool
{
    private readonly IUserService _userService;
    private readonly ILogger<UserTool> _logger;

    public UserTool(IUserService userService, ILogger<UserTool> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    [McpServerTool(Name = "GetAccountsByUserName"), Description("Get the list of all accounts for a specific user.")]
    public List<Account> GetAccountsByUserName([Description("userName once the user has logged.")] string userName)
    {
        _logger.LogInformation("Received request to get accounts for user: {UserName}", userName);
        return _userService.GetAccountsByUserName(userName);
    }
}