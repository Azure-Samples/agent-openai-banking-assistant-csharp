public class UserService : IUserService
{
    private readonly Dictionary<string, Account> _accounts;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserService"/> class.
    /// </summary>
    public UserService()
    {
        _accounts = new Dictionary<string, Account>
        {
            {
                "alice.user@contoso.com",
                new Account(
                    "1000",
                    "alice.user@contoso.com",
                    "Alice User",
                    "USD",
                    "2022-01-01",
                    "5000",
                    null
                )
            },
            {
                "bob.user@contoso.com",
                new Account(
                    "1010",
                    "bob.user@contoso.com",
                    "Bob User",
                    "EUR",
                    "2022-01-01",
                    "10000",
                    null
                )
            },
            {
                "charlie.user@contoso.com",
                new Account(
                    "1020",
                    "charlie.user@contoso.com",
                    "Charlie User",
                    "EUR",
                    "2022-01-01",
                    "3000",
                    null
                )
            }
        };
    }

    /// <summary>
    /// Retrieves the list of accounts associated with a specific username.
    /// </summary>
    /// <param name="userName">The username of the user.</param>
    /// <returns>A task representing the asynchronous operation, containing the list of accounts.</returns>
    public Task<List<Account>> GetAccountsByUserNameAsync(string userName)
    {
        if (string.IsNullOrEmpty(userName))
            return Task.FromResult(new List<Account>());

        return Task.FromResult(_accounts.TryGetValue(userName, out var account)
            ? new List<Account> { account }
            : new List<Account>());
    }
}