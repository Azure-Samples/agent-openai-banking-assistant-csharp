using System.Reflection;
using System.Text;
using Azure.AI.Agents.Persistent;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace BankingAssistant.Agents.Infrastructure;

/// <summary>
/// Adapter class for converting various tool formats (MCP, OpenAPI, custom tools) to AITools.
/// </summary>
public static class AIToolAdapter
{
    /// <summary>
    /// Converts an OpenAPI YAML specification to an AITool using OpenApiToolDefinition.
    /// </summary>
    /// <param name="openApiStream">The stream containing the OpenAPI YAML specification.</param>
    /// <param name="apiName">The name/identifier for the OpenAPI specification.</param>
    /// <param name="logger">Optional logger for diagnostic information.</param>
    /// <returns>An AITool that represents the OpenAPI specification.</returns>
    public static async Task<AITool> ConvertOpenApiToToolAsync(
        Stream openApiStream,
        string apiName,
        ILogger? logger = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(openApiStream, nameof(openApiStream));
        ArgumentException.ThrowIfNullOrWhiteSpace(apiName, nameof(apiName));

        logger?.LogInformation("Converting OpenAPI specification {ApiName} to AITool", apiName);

        try
        {
            // Read the entire stream as BinaryData asynchronously
            using var memoryStream = new MemoryStream();
            await openApiStream.CopyToAsync(memoryStream, 81920, cancellationToken);
            var specContent = memoryStream.ToArray();
            var specText = Encoding.UTF8.GetString(specContent);

            logger?.LogInformation("Loaded OpenAPI specification {ApiName}: {SpecContent}", apiName, specText);

            // Convert YAML to JSON
            var jsonSpec = ConvertYamlToJson(specText);
            var binaryData = new BinaryData(Encoding.UTF8.GetBytes(jsonSpec));

            // Create OpenAPI tool definition using anonymous authentication
            // (can be extended to support API keys, OAuth, etc.)
            var openApiToolDef = new OpenApiToolDefinition(
                name: apiName,
                description: $"Provides access to {apiName} operations via OpenAPI specification",
                spec: binaryData,
                openApiAuthentication: new OpenApiAnonymousAuthDetails()
            );

            logger?.LogInformation("Created AITool for OpenAPI {ApiName} ({BytesRead} bytes)", apiName, binaryData.ToMemory().Length);

            return openApiToolDef.AsAITool();
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Error converting OpenAPI specification {ApiName} to tool", apiName);
            throw;
        }
    }

    /// <summary>
    /// Converts a custom tool instance's methods to AITools.
    /// </summary>
    /// <typeparam name="TTool">The type of the custom tool.</typeparam>
    /// <param name="toolInstance">The tool instance containing methods to convert.</param>
    /// <param name="logger">Optional logger for diagnostic information.</param>
    /// <returns>A list of AITools created from the tool's public methods.</returns>
    public static IList<AITool> ConvertCustomToolToAITools<TTool>(
        TTool toolInstance,
        ILogger? logger = null) where TTool : class
    {
        ArgumentNullException.ThrowIfNull(toolInstance, nameof(toolInstance));

        var aiTools = new List<AITool>();
        var toolType = typeof(TTool);

        var methods = toolType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

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
                var aiTool = AIFunctionFactory.Create(
                    method: method,
                    target: toolInstance,
                    name: method.Name,
                    description: description
                );

                aiTools.Add(aiTool);
                logger?.LogDebug("Created AITool for method {MethodName}", method.Name);
            }
            catch (Exception ex)
            {
                logger?.LogWarning(ex, "Could not create AITool for method {MethodName}", method.Name);
            }
        }

        logger?.LogInformation("Created {ToolCount} AITools from custom tool {ToolType}", aiTools.Count, toolType.Name);

        return aiTools;
    }

    /// <summary>
    /// Converts a YAML string to JSON format.
    /// OpenAPI tools expect JSON format, so YAML specs need to be converted first.
    /// </summary>
    /// <param name="yamlContent">The YAML content to convert.</param>
    /// <returns>The JSON representation of the YAML content.</returns>
    private static string ConvertYamlToJson(string yamlContent)
    {
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();

        var data = deserializer.Deserialize<dynamic>(yamlContent);        
        var json = JsonSerializer.Serialize(data);

        return json;
    }
}