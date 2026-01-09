using Microsoft.Extensions.DependencyInjection;

namespace TransactionsApi.Extensions;

/// <summary>
/// Extension methods for configuring transaction services.
/// </summary>
public static class ServicesExtensions
{
    /// <summary>
    /// Adds transaction-related services to the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<ITransactionService, TransactionService>();
        return services;
    }
}
