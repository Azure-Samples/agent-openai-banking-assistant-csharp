
namespace BankingAssistant.Services;

/// <summary>
/// Service for managing logged user information.
/// </summary>
public class LoggedUserService : IUserService
{
    /// <summary>
    /// Gets the currently logged-in user.
    /// </summary>
    /// <returns>The logged user information.</returns>
    public LoggedUser GetLoggedUser()
    {
        return GetDefaultUser();
    }

    /// <summary>
    /// Gets a default user for development/testing purposes.
    /// </summary>
    /// <returns>A default logged user.</returns>
    private static LoggedUser GetDefaultUser() => new("bob.user@contoso.com", "bob.user@contoso.com", "generic", "Bob The User");
}
