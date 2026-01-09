namespace AccountMcp.Interfaces;

/// <summary>
/// Interface for user service operations.
/// </summary>
public interface IUserService
{
    /// <summary>
    /// Retrieves all accounts associated with a specific username.
    /// </summary>
    /// <param name="userName">The username to retrieve accounts for.</param>
    /// <returns>A task representing the asynchronous operation, containing a list of accounts.</returns>
    Task<List<Account>> GetAccountsByUserNameAsync(string userName);
}
