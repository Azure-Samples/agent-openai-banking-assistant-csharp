using System.Reflection;

namespace BankingAssistant.Agents.Infrastructure;

/// <summary>
/// Helper class for registering and converting tools from various sources (OpenAPI, custom plugins) to AIFunctions.
/// </summary>
public static class ToolRegistrationHelper
{
    /// <summary>
    /// Loads an OpenAPI specification and converts each operation to individual AITools.
    /// </summary>
    /// <param name="apiName">The name of the API (used to locate the embedded YAML file).</param>
    /// <param name="apiUrl">The base URL of the API.</param>
    /// <param name="logger">Optional logger for diagnostic information.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a list of AITools, one for each operation.</returns>
    public static async Task<IList<AITool>> GetOpenApiToolsAsync(
        string apiName,
        string apiUrl,
        ILogger? logger = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiName, nameof(apiName));
        ArgumentException.ThrowIfNullOrWhiteSpace(apiUrl, nameof(apiUrl));

        logger?.LogInformation("Loading OpenAPI specification for {ApiName}", apiName);

        try
        {
            var stream = GetEmbeddedApiYaml(apiName) ?? throw new InvalidOperationException($"Could not find embedded YAML for {apiName}");
            var tools = await AIToolAdapter.ConvertOpenApiOperationsToToolsAsync(stream, apiName, apiUrl, logger);
            logger?.LogInformation("Created {ToolCount} AITools from OpenAPI specification {ApiName}", tools.Count, apiName);
            return tools;
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Error loading OpenAPI specification for {ApiName}", apiName);
            throw;
        }
    }

    /// <summary>
    /// Converts a custom plugin instance to a list of AITools.
    /// </summary>
    /// <typeparam name="TTool">The type of the tool.</typeparam>
    /// <param name="toolInstance">The plugin instance containing methods to convert.</param>
    /// <param name="logger">Optional logger for diagnostic information.</param>
    /// <returns>A list of AITools created from the plugin's methods.</returns>
    public static IList<AITool> GetCustomTools<TTool>(
        TTool toolInstance,
        ILogger? logger = null) where TTool : class
    {
        ArgumentNullException.ThrowIfNull(toolInstance, nameof(toolInstance));

        logger?.LogInformation("Creating AITools from plugin type {PluginType}", typeof(TTool).Name);

        return AIToolAdapter.ConvertCustomToolToAITools(toolInstance, logger);
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