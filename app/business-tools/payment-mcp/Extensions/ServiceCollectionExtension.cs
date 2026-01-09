namespace PaymentMcp.Extensions;

/// <summary>
/// Extension methods for configuring payment services.
/// </summary>
public static class ServicesExtensions
{
    /// <summary>
    /// Adds payment-related services to the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddServices(this IServiceCollection services)
    {
        services.AddSingleton<IPaymentService>(provider =>
        {
            var configuration = provider.GetRequiredService<IConfiguration>();
            var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
            var httpClient = new HttpClient();
            var transactionsApiUrl = configuration["BackendAPIs:TransactionsApiUrl"] ?? throw new InvalidOperationException("BackendAPIs:TransactionsApiUrl configuration is missing");
            return new PaymentService(loggerFactory.CreateLogger<PaymentService>(), httpClient, transactionsApiUrl);
        });
        return services;
    }
}
