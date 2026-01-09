namespace BankingAssistant.Extensions;

/// <summary>
/// Extension methods for IServiceCollection to add Azure services and agent infrastructure.
/// </summary>
public static class ServicesExtensions
{
    /// <summary>
    /// Adds all Azure services and agent infrastructure to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddAzureServices(this IServiceCollection services, IConfiguration configuration)
    {
        var tenantId = configuration["AzureAd:TenantId"];
        var credentialOptions = new DefaultAzureCredentialOptions();
        
        // Only set TenantId if it's provided and not null
        if (!string.IsNullOrEmpty(tenantId))
        {
            credentialOptions.TenantId = tenantId;
        }
        
        var credential = new DefaultAzureCredential(credentialOptions);
        
        // Register Azure Blob Service Client via the Azure Clients builder.
        services.AddSingleton<BlobServiceClient>(provider =>
        {
            var accountName = configuration["Storage:AccountName"];
            var storageEndpoint = $"https://{accountName}.blob.core.windows.net";
            Console.WriteLine($"BlobServiceClient: {storageEndpoint}");
            var blobServiceClient = new BlobServiceClient(
                new Uri(storageEndpoint),
                credential);
            return blobServiceClient;
        });

        // Register BlobStorageProxy as IBlobStorage.
        services.AddSingleton<IBlobStorage>(provider =>
        {
            var blobServiceClient = provider.GetRequiredService<BlobServiceClient>();
            var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger<BlobStorageProxy>();
            return new BlobStorageProxy(blobServiceClient, logger, configuration);
        });

        // Register DocumentIntelligenceClient.
        services.AddSingleton<DocumentIntelligenceClient>(provider =>
        {
            var endpoint = configuration["DocumentIntelligence:Endpoint"]
                ?? throw new InvalidOperationException("DocumentIntelligence:Endpoint is not configured");
            return new DocumentIntelligenceClient(new Uri(endpoint), credential);
        });

        // Register DocumentIntelligenceProxy as IDocumentScanner.
        services.AddSingleton<IDocumentScanner, DocumentIntelligenceProxy>();

        // Register IChatClient for Azure OpenAI
        services.AddSingleton<IChatClient>(provider =>
        {
            return ChatClientInitialization.CreateFromConfiguration(configuration);
        });

        services.AddSingleton<IUserService, LoggedUserService>();

        // Register Microsoft Agent Framework infrastructure
        services.AddSingleton<AgentFactory>();
        
        // Register Agent Orchestration Service
        services.AddSingleton<AgentOrchestrationService>();

        return services;
    }
}

