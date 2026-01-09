using System.Reflection;

namespace BankingAssistant.Agents.Infrastructure;

/// <summary>
/// Helper class for registering and converting tools from various sources (MCP, OpenAPI, custom plugins) to AIFunctions.
/// </summary>
public static class ToolRegistrationHelper
{
    /// <summary>
    /// Retrieves tools from an MCP (Model Context Protocol) server and converts them to AIFunctions.
    /// </summary>
    /// <param name="clientName">The name of the MCP client.</param>
    /// <param name="apiUrl">The URL of the MCP server.</param>
    /// <param name="useStreamableHttp">Whether to use streamable HTTP transport.</param>
    /// <param name="logger">Optional logger for diagnostic information.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a list of AIFunctions.</returns>
    public static async Task<IList<AIFunction>> GetMcpToolsAsync(
        string clientName,
        string apiUrl,
        bool useStreamableHttp = false,
        ILogger? logger = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(clientName, nameof(clientName));
        ArgumentException.ThrowIfNullOrWhiteSpace(apiUrl, nameof(apiUrl));

        logger?.LogInformation("Fetching MCP tools from {ApiUrl}", apiUrl);

        try
        {
            var mcpClient = await McpClientFactory.CreateAsync(
                new SseClientTransport(
                    new SseClientTransportOptions()
                    {
                        Endpoint = new Uri(apiUrl),
                        Name = clientName,
                        UseStreamableHttp = useStreamableHttp
                    }));

            var mcpTools = await mcpClient.ListToolsAsync();

            logger?.LogInformation("Retrieved {ToolCount} tools from MCP server", mcpTools.Count);

            return AIFunctionToolAdapter.ConvertMcpToolsToAIFunctions(mcpTools);
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Error fetching MCP tools from {ApiUrl}", apiUrl);
            throw;
        }
    }

    /// <summary>
    /// Loads an OpenAPI specification and converts its operations to AIFunctions.
    /// </summary>
    /// <param name="apiName">The name of the API (used to locate the embedded YAML file).</param>
    /// <param name="apiUrl">The base URL of the API.</param>
    /// <param name="logger">Optional logger for diagnostic information.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a list of AIFunctions.</returns>
    public static async Task<IList<AIFunction>> GetOpenApiToolsAsync(
        string apiName,
        string apiUrl,
        ILogger? logger = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiName, nameof(apiName));
        ArgumentException.ThrowIfNullOrWhiteSpace(apiUrl, nameof(apiUrl));

        logger?.LogInformation("Loading OpenAPI tools for {ApiName} from {ApiUrl}", apiName, apiUrl);

        try
        {
            var stream = GetEmbeddedApiYaml(apiName);

            return stream == null
                ? throw new InvalidOperationException($"Could not find embedded YAML for {apiName}")
                : await AIFunctionToolAdapter.ConvertOpenApiToAIFunctionsAsync(stream, apiUrl, logger);
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Error loading OpenAPI tools for {ApiName}", apiName);
            throw;
        }
    }

    /// <summary>
    /// Converts a custom plugin instance to a list of AIFunctions.
    /// </summary>
    /// <typeparam name="TPlugin">The type of the plugin.</typeparam>
    /// <param name="pluginInstance">The plugin instance containing methods to convert.</param>
    /// <param name="logger">Optional logger for diagnostic information.</param>
    /// <returns>A list of AIFunctions created from the plugin's methods.</returns>
    public static IList<AIFunction> GetCustomPluginTools<TPlugin>(
        TPlugin pluginInstance,
        ILogger? logger = null) where TPlugin : class
    {
        ArgumentNullException.ThrowIfNull(pluginInstance, nameof(pluginInstance));

        logger?.LogInformation("Creating AIFunctions from plugin type {PluginType}", typeof(TPlugin).Name);

        return AIFunctionToolAdapter.ConvertPluginToAIFunctions(pluginInstance, logger);
    }

    /// <summary>
    /// Retrieves an embedded OpenAPI YAML file from the assembly resources.
    /// </summary>
    /// <param name="apiName">The name of the API whose YAML file to retrieve.</param>
    /// <returns>A stream containing the YAML file content.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the YAML resource is not found.</exception>
    private static Stream? GetEmbeddedApiYaml(string apiName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceNames = assembly.GetManifestResourceNames();

        var resourceName = resourceNames
            .FirstOrDefault(name => name.EndsWith($"{apiName}.yaml", StringComparison.OrdinalIgnoreCase));

        return resourceName == null
            ? throw new InvalidOperationException($"Resource {apiName}.yaml not found.")
            : assembly.GetManifestResourceStream(resourceName);
    }
}