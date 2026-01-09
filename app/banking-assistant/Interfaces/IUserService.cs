namespace BankingAssistant.Interfaces;

/// <summary>
/// Interface for user service operations.
/// </summary>
public interface IUserService
{
    /// <summary>
    /// Gets the currently logged-in user.
    /// </summary>
    /// <returns>The logged user information.</returns>
    public LoggedUser GetLoggedUser();

}
