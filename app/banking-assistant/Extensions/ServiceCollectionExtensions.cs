public static class ServicesExtensions
{
    public static IServiceCollection AddAzureServices(this IServiceCollection services, IConfiguration configuration)
    {
        var tenantId = configuration["AzureAd:TenantId"];
        var credentialOptions = new DefaultAzureCredentialOptions();
        
        // Only set TenantId if it's provided and not null
        if (!string.IsNullOrEmpty(tenantId))
        {
            credentialOptions.TenantId = tenantId;
        }
        
        // Register Azure Blob Service Client via the Azure Clients builder.
        services.AddSingleton<BlobServiceClient>(provider =>
        {
            var credential = new DefaultAzureCredential(credentialOptions);
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
            var endpoint = configuration["DocumentIntelligence:Endpoint"];
            var credential = new DefaultAzureCredential(credentialOptions);
            return new DocumentIntelligenceClient(new Uri(endpoint), credential);
        });

        // Register DocumentIntelligenceProxy as IDocumentScanner.
        services.AddSingleton<IDocumentScanner, DocumentIntelligenceProxy>();

        // Register Azure OpenAI Kernel.
        services.AddKernel().AddAzureOpenAIChatCompletion(
            deploymentName: configuration["AzureOpenAI:Deployment"],
            endpoint: configuration["AzureOpenAI:Endpoint"],
            credentials: new DefaultAzureCredential(credentialOptions)
        );

        services.AddSingleton<IUserService, LoggedUserService>();

        // Register Agent Router
        services.AddTransient<IAgentRouter, AgentRouter>();
        
        // Register Intent Extractor Agent
        services.AddSingleton<IIntentExtractorAgent, IntentExtractorAgent>();
        // Register Account Agent
        services.AddSingleton<IAccountAgent, AccountAgent>();
        // Register Payment Agent
        services.AddSingleton<IPaymentAgent, PaymentAgent>();
        // Register Transactions Reporting Agent
        services.AddSingleton<ITransactionsReportingAgent, TransactionsReportingAgent>();

        return services;
    }
}

