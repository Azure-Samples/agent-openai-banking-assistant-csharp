using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.Identity.Web;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using Azure.Monitor.OpenTelemetry.Exporter;
using FluentValidation;
using BankingAssistant.Agents;

namespace BankingAssistant.Extensions;

/// <summary>
/// Extension methods for IServiceCollection to add Azure services and agent infrastructure.
/// </summary>
public static class ServicesExtensions
{
    /// <summary>
    /// Adds the OpenTelemetry configuration extension method.
    /// </summary>
    public static IServiceCollection AddOpenTelemetryConfiguration(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var appInsightsActiveString = configuration.GetValue<string>("ApplicationInsights:Active") ?? "false";
        var appInsightsActive = bool.TryParse(appInsightsActiveString, out var result) && result;

        var resourceBuilder = ResourceBuilder
            .CreateDefault()
            .AddService("BankingAssistant");

        // Add HTTP logging service for capturing request/response details
        services.AddHttpLogging(options =>
        {
            options.LoggingFields = HttpLoggingFields.All;
            options.RequestHeaders.Add("Authorization");
            options.ResponseHeaders.Add("Content-Type");
            options.ResponseHeaders.Add("Content-Length");
            options.MediaTypeOptions.AddText("application/json");
        });

        if (appInsightsActive)
        {
            var appInsightsConnectionString = configuration.GetValue<string>("ApplicationInsights:ConnectionString");

            if (!string.IsNullOrEmpty(appInsightsConnectionString))
            {
                // Configure Traces
                services.AddOpenTelemetry()
                    .WithTracing(tracing => tracing
                        .SetResourceBuilder(resourceBuilder)
                        .AddSource("BankingAssistant")
                        .AddSource("BankingAssistant.Tools.TransactionTool")
                        .AddSource("BankingAssistant.Tools.InvoiceScanTool")
                        .AddSource("BankingAssistant.LLM.Calls")
                        .AddSource("*Microsoft.Extensions.AI")
                        .AddSource("*Microsoft.Extensions.Agents*")
                        .AddAspNetCoreInstrumentation(options =>
                        {
                            options.RecordException = true;
                            options.EnrichWithHttpRequest = (activity, request) =>
                            {
                                activity.SetTag("http.request.body.size", request.ContentLength);
                            };
                            options.EnrichWithHttpResponse = (activity, response) =>
                            {
                                activity.SetTag("http.response.body.size", response.ContentLength);
                            };
                        })
                        .AddAzureMonitorTraceExporter(options =>
                            options.ConnectionString = appInsightsConnectionString));

                // Configure Metrics
                services.AddOpenTelemetry()
                    .WithMetrics(metrics => metrics
                        .SetResourceBuilder(resourceBuilder)
                        .AddMeter("BankingAssistant")
                        .AddMeter("*Microsoft.Agents.AI")
                        .AddAspNetCoreInstrumentation()
                        .AddAzureMonitorMetricExporter(options =>
                            options.ConnectionString = appInsightsConnectionString));
            }
        }
        else if (environment.IsDevelopment())
        {
            // For development without Application Insights, export to Aspire Dashboard
            services.AddOpenTelemetry()
                .WithTracing(tracing => tracing
                    .SetResourceBuilder(resourceBuilder)
                    .AddSource("BankingAssistant")
                    .AddSource("*Microsoft.Extensions.AI")
                    .AddSource("*Microsoft.Extensions.Agents*")
                    .AddAspNetCoreInstrumentation(options =>
                    {
                        options.RecordException = true;
                        options.EnrichWithHttpRequest = (activity, request) =>
                        {
                            activity.SetTag("http.request.body.size", request.ContentLength);
                        };
                        options.EnrichWithHttpResponse = (activity, response) =>
                        {
                            activity.SetTag("http.response.body.size", response.ContentLength);
                        };
                    })
                    .AddOtlpExporter(options => options.Endpoint = new Uri("http://localhost:4317")))
                .WithMetrics(metrics => metrics
                    .SetResourceBuilder(resourceBuilder)
                    .AddMeter("BankingAssistant")
                    .AddMeter("*Microsoft.Agents.AI")
                    .AddAspNetCoreInstrumentation()
                    .AddOtlpExporter(options => options.Endpoint = new Uri("http://localhost:4317")));
        }

        return services;
    }

    /// <summary>
    /// Configures authentication based on the hosting environment.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <param name="environment">The hosting environment.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddAuthenticationConfiguration(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        if (environment.IsDevelopment())
        {
            // In development, use the fake authentication scheme.
            services.AddAuthentication("Fake")
                .AddScheme<AuthenticationSchemeOptions, FakeAuthenticationHandler>("Fake", options => { });
        }
        else
        {
            // In production or staging, use JWT Bearer authentication via Azure AD.
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddMicrosoftIdentityWebApi(configuration.GetSection("AzureAd"));
        }

        services.AddAuthorization();

        return services;
    }

    /// <summary>
    /// Adds CORS and validation support to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddValidationAndCors(this IServiceCollection services)
    {
        // @TODO: Temporary. Fix later.
        services.AddCors(options => options.AddPolicy("allowSpecificOrigins", 
            policy => policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

        // Register FluentValidation
        services.AddValidatorsFromAssemblyContaining<Program>();

        return services;
    }

    /// <summary>
    /// Adds OpenAPI documentation support.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddApiDocumentation(this IServiceCollection services)
    {
        services.AddOpenApi();
        return services;
    }

    /// <summary>
    /// Adds all Azure services and agent infrastructure to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddAzureServices(this IServiceCollection services, IConfiguration configuration, IHostEnvironment? environment = null)
    {
        // Add Controllers support
        services.AddControllers();
        
        var tenantId = configuration["AzureAd:TenantId"];
        var credentialOptions = new DefaultAzureCredentialOptions();
        
        // Only set TenantId if it's provided and not null
        if (!string.IsNullOrEmpty(tenantId))
        {
            credentialOptions.TenantId = tenantId;
        }
        
        var credential = new DefaultAzureCredential(credentialOptions);
        
        // Register Azure Blob Service Client via the Azure Clients builder.
        services.AddSingleton(provider =>
        {
            var accountName = configuration["Storage:AccountName"];
            var storageEndpoint = $"https://{accountName}.blob.core.windows.net";
            var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger("BlobServiceClient");
            
            logger.LogInformation("Initializing BlobServiceClient: {StorageEndpoint}", storageEndpoint);
            var blobServiceClient = new BlobServiceClient(new Uri(storageEndpoint), credential);
            
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
        services.AddSingleton(provider =>
        {
            var endpoint = configuration["DocumentIntelligence:Endpoint"]
                ?? throw new InvalidOperationException("DocumentIntelligence:Endpoint is not configured");
            
            return new DocumentIntelligenceClient(new Uri(endpoint), credential);
        });

        // Register DocumentIntelligenceProxy as IDocumentScanner.
        services.AddSingleton<IDocumentScanner, DocumentIntelligenceProxy>();
		
        // Register User Service
		services.AddSingleton<IUserService, LoggedUserService>();

        // Register HttpClient factory for tools
        services.AddHttpClient();
		
		// Register IChatClient as singleton using the AzureOpenAIClient
		services.AddSingleton<IChatClient>(provider =>
		{
			var endpoint = configuration["AzureOpenAI:Endpoint"]
				?? throw new InvalidOperationException("AzureOpenAI:Endpoint is not configured");

			var deployment = configuration["AzureOpenAI:Deployment"]
				?? throw new InvalidOperationException("AzureOpenAI:Deployment is not configured");

			var azureOpenAIClient = new AzureOpenAIClient(new Uri(endpoint), credential);
			var chatClient = azureOpenAIClient
				.GetChatClient(deployment)
				.AsIChatClient()
				.AsBuilder()
				.UseOpenTelemetry(sourceName: "BankingAssistant", configure: (cfg) => cfg.EnableSensitiveData = true)
				.Build();

			return chatClient;
		});

		// Register Microsoft Agent Framework infrastructure
		services.AddSingleton<AgentFactory>();
        
        // Register Agent Managers
        services.AddScoped<IAccountAgentManager, AccountAgentManager>();
        services.AddScoped<IPaymentAgentManager, PaymentAgentManager>();
        services.AddScoped<ITransactionAgentManager, TransactionAgentManager>();
        
        // Register Agent Orchestration Service
        services.AddScoped<AgentOrchestrationService>();

        return services;
    }
}

