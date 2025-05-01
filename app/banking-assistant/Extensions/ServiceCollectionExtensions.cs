public static class ServicesExtensions
{
    public static IServiceCollection AddAzureServices(this IServiceCollection services, , IConfiguration configuration)
    {

        // Register Azure Blob Service Client via the Azure Clients builder.
        services.AddSingleton<BlobServiceClient>(provider =>
        {
            var credential = new DefaultAzureCredential();
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
            var apiKey = configuration["DocumentIntelligence:ApiKey"];
            var credential = new DefaultAzureCredential();
            return new DocumentIntelligenceClient(new Uri(endpoint), credential);
        });

        // Register DocumentIntelligenceProxy as IDocumentScanner.
        services.AddSingleton<IDocumentScanner, DocumentIntelligenceProxy>();

        // Register OpenAIClient.
        services.AddSingleton<AzureOpenAIClient>(provider =>
        {
            var endpoint = configuration["AzureOpenAI:Endpoint"];
            var apiKey = configuration["AzureOpenAI:ApiKey"];
            return new AzureOpenAIClient(new Uri(endpoint), new AzureKeyCredential(apiKey));
        });

        services.AddSingleton<Kernel>(provider =>
        {
            var deploymentName = configuration[key: "AzureOpenAI:Deployment"];
            var endpoint = configuration[key: "AzureOpenAI:Endpoint"];
            var apiKey = configuration[key: "AzureOpenAI:ApiKey"];
            var credential = new DefaultAzureCredential();

            IKernelBuilder kernelBuilder = Kernel.CreateBuilder();
            kernelBuilder.AddAzureOpenAIChatCompletion(deploymentName: deploymentName, endpoint: endpoint, credentials: credential);
            return kernelBuilder.Build();
        });

        services.AddSingleton<IUserService, LoggedUserService>();

        services.AddSingleton<AgentRouter>(provider =>
        {
            var kernel = provider.GetRequiredService<Kernel>();
            var documentScanner = provider.GetRequiredService<IDocumentScanner>();
            var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
            var userService = provider.GetRequiredService<IUserService>();
            return new AgentRouter(kernel, configuration, documentScanner, loggerFactory, userService);
        });

        return services;
    }
}

