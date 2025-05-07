using Microsoft.Extensions.DependencyInjection;

public static class ServicesExtensions
{
    public static IServiceCollection AddServices(this IServiceCollection services)
    {
        services.AddSingleton<IPaymentService>(provider =>
        {
            var configuration = provider.GetRequiredService<IConfiguration>();
            var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
            var httpClient = new HttpClient();
            var transactionsApiUrl = configuration["BackendAPIs:TransactionsApiUrl"];
            return new PaymentService(loggerFactory.CreateLogger<PaymentService>(), httpClient, transactionsApiUrl);
        });
        return services;
    }
}
