namespace AccountMcp.Extensions;

/// <summary>
/// Extension methods for configuring account services.
/// </summary>
public static class ServicesExtensions
{
    /// <summary>
    /// Adds account-related services to the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IAccountService, AccountService>();

        services.AddSingleton<IUserService, UserService>();

        return services;
    }
}
