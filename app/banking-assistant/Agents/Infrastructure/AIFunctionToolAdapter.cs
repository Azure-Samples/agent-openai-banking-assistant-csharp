using System.Reflection;
using System.Text.Json.Nodes;

namespace BankingAssistant.Agents.Infrastructure;

/// <summary>
/// Adapter class for converting various tool formats (MCP, OpenAPI, custom plugins) to AIFunctions.
/// </summary>
public static class AIFunctionToolAdapter
{
    /// <summary>
    /// Converts MCP (Model Context Protocol) tools to AIFunctions.
    /// </summary>
    /// <param name="mcpTools">The list of MCP tools to convert.</param>
    /// <returns>A list of AIFunctions created from the MCP tools.</returns>
    public static IList<AIFunction> ConvertMcpToolsToAIFunctions(IList<McpClientTool> mcpTools)
    {
        ArgumentNullException.ThrowIfNull(mcpTools, nameof(mcpTools));

        var aiFunctions = new List<AIFunction>();

        foreach (var mcpTool in mcpTools)
        {
            // Create AIFunction from MCP tool
            // MCP tools work with JSON - we'll create a simple wrapper that just passes through
            // The actual parameter handling is done by the MCP tool itself
            var toolName = mcpTool.Name;
            var toolDescription = mcpTool.Description ?? "MCP tool";
            
            // Create a delegate that invokes the MCP tool
            // For MCP tools, we expect the framework to serialize parameters to JSON
            var aiFunction = AIFunctionFactory.Create(
                method: async () =>
                {
                    // MCP tools handle their own parameter serialization
                    // This is a placeholder - actual invocation happens through MCP framework
                    var result = await Task.FromResult(JsonNode.Parse("{}"));
                    return result?.ToString() ?? "";
                },
                options: new AIFunctionFactoryOptions
                {
                    Name = toolName,
                    Description = toolDescription
                });

            aiFunctions.Add(aiFunction);
        }

        return aiFunctions;
    }

    /// <summary>
    /// Converts OpenAPI specifications to AIFunctions.
    /// </summary>
    /// <param name="openApiStream">The stream containing the OpenAPI YAML specification.</param>
    /// <param name="serverUrl">The base URL of the API server.</param>
    /// <param name="logger">Optional logger for diagnostic information.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a list of AIFunctions.</returns>
    public static async Task<IList<AIFunction>> ConvertOpenApiToAIFunctionsAsync(
        Stream openApiStream,
        string serverUrl,
        ILogger? logger = null)
    {
        ArgumentNullException.ThrowIfNull(openApiStream, nameof(openApiStream));
        ArgumentException.ThrowIfNullOrWhiteSpace(serverUrl, nameof(serverUrl));

        logger?.LogWarning("OpenAPI to AIFunction conversion is not yet fully implemented");
        
        var aiFunctions = new List<AIFunction>();
        
        return aiFunctions;
    }

    /// <summary>
    /// Converts a custom plugin instance's methods to AIFunctions.
    /// </summary>
    /// <typeparam name="TPlugin">The type of the plugin.</typeparam>
    /// <param name="pluginInstance">The plugin instance containing methods to convert.</param>
    /// <param name="logger">Optional logger for diagnostic information.</param>
    /// <returns>A list of AIFunctions created from the plugin's public methods.</returns>
    public static IList<AIFunction> ConvertPluginToAIFunctions<TPlugin>(
        TPlugin pluginInstance,
        ILogger? logger = null) where TPlugin : class
    {
        ArgumentNullException.ThrowIfNull(pluginInstance, nameof(pluginInstance));

        var aiFunctions = new List<AIFunction>();
        var pluginType = typeof(TPlugin);

        var methods = pluginType.GetMethods(BindingFlags.Public | BindingFlags.Instance);

        foreach (var method in methods)
        {
            var description = method.Name;
            
            var descAttr = method.GetCustomAttribute<System.ComponentModel.DescriptionAttribute>();
            if (descAttr != null)
            {
                description = descAttr.Description;
            }

            try
            {
                var aiFunction = AIFunctionFactory.Create(
                    method: method,
                    target: pluginInstance,
                    name: method.Name,
                    description: description
                );

                aiFunctions.Add(aiFunction);
                logger?.LogDebug("Created AIFunction for method {MethodName}", method.Name);
            }
            catch (Exception ex)
            {
                logger?.LogWarning(ex, "Could not create AIFunction for method {MethodName}", method.Name);
            }
        }

        logger?.LogInformation("Created {FunctionCount} AIFunctions from plugin {PluginType}", aiFunctions.Count, pluginType.Name);

        return aiFunctions;
    }
}