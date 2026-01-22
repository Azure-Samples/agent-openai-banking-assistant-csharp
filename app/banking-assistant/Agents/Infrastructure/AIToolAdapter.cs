using System.Reflection;
using System.Text;
using System.Text.Json.Nodes;
using Azure.AI.Agents.Persistent;
using YamlDotNet.Serialization;

namespace BankingAssistant.Agents.Infrastructure;

/// <summary>
/// Adapter class for converting various tool formats (MCP, OpenAPI, custom tools) to AITools.
/// </summary>
public static class AIToolAdapter
{
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
	/// Converts an OpenAPI specification to individual AITools, one for each operation.
	/// </summary>
	/// <param name="openApiStream">The stream containing the OpenAPI YAML specification.</param>
	/// <param name="apiName">The name/identifier for the OpenAPI specification.</param>
	/// <param name="apiUrl">The base URL for the API operations.</param>
	/// <param name="logger">Optional logger for diagnostic information.</param>
	/// <returns>A list of AITools, one for each operation in the OpenAPI specification.</returns>
	public static async Task<IList<AITool>> ConvertOpenApiOperationsToToolsAsync(
		Stream openApiStream,
		string apiName,
		string apiUrl,
		ILogger? logger = null,
		CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(openApiStream, nameof(openApiStream));
		ArgumentException.ThrowIfNullOrWhiteSpace(apiName, nameof(apiName));
		ArgumentException.ThrowIfNullOrWhiteSpace(apiUrl, nameof(apiUrl));

		logger?.LogInformation("Parsing OpenAPI specification {ApiName} to extract individual operations", apiName);

		try
		{
			// Read the stream as text
			using var memoryStream = new MemoryStream();
			await openApiStream.CopyToAsync(memoryStream, 81920, cancellationToken);
			var yamlText = Encoding.UTF8.GetString(memoryStream.ToArray());

			// Convert YAML to JSON for easier parsing
			var jsonText = ConvertYamlToJson(yamlText);
			using var jsonDoc = JsonDocument.Parse(jsonText);
			var root = jsonDoc.RootElement;

			var aiTools = new List<AITool>();

			// Extract paths and operations
			if (root.TryGetProperty("paths", out var pathsElement))
			{
				foreach (var pathProperty in pathsElement.EnumerateObject())
				{
					var path = pathProperty.Name;
					logger?.LogInformation("Processing path: {Path}", path);

					// Iterate through HTTP methods (get, post, put, delete, patch, options, head)
					foreach (var methodProperty in pathProperty.Value.EnumerateObject())
					{
						var httpMethod = methodProperty.Name.ToLowerInvariant();

						// Skip non-operation properties like "parameters", "servers", etc.
						if (!new[] { "get", "post", "put", "delete", "patch", "options", "head" }.Contains(httpMethod))
							continue;

						var operation = methodProperty.Value;

						// Extract operation details
						var operationId = operation.TryGetProperty("operationId", out var opId)
							? opId.GetString()
							: $"{httpMethod}_{path.Replace("/", "_").TrimStart('_')}";

						var description = operation.TryGetProperty("summary", out var summary)
							? summary.GetString()
							: operation.TryGetProperty("description", out var desc)
								? desc.GetString()
								: $"{httpMethod.ToUpper()} {path}";

						logger?.LogInformation("Creating tool for operation: {OperationId} ({HttpMethod} {Path})", operationId, httpMethod, path);

						// Build path item object using JsonNode for proper serialization
						var pathItemNode = new JsonObject();

						// Add the operation (which includes parameters, responses, etc.)
						var operationNode = JsonNode.Parse(operation.GetRawText());
						pathItemNode[httpMethod] = operationNode;

						// Add path-level parameters if they exist
						if (pathProperty.Value.TryGetProperty("parameters", out var pathParams))
						{
							var parametersNode = JsonNode.Parse(pathParams.GetRawText());
							pathItemNode["parameters"] = parametersNode;
						}

						// Create a minimal OpenAPI spec for this single operation using JsonNode
						var operationSpecNode = new JsonObject
						{
							{ "openapi", root.TryGetProperty("openapi", out var versionEl) ? versionEl.GetString() ?? "3.0.3" : "3.0.3" },
							{ "info", new JsonObject
							{
								{ "title", operationId },
                                { "description", description },
								{ "version", "v1.0.0" }
							}},
							{ "servers", new JsonArray
							{
								new JsonObject { { "url", apiUrl } }
							}},
							{ "auth", new JsonArray() },
							{ "paths", new JsonObject
							{
								{ path, pathItemNode }
							}}
						};

						// Add components if they exist
						if (root.TryGetProperty("components", out var componentsEl))
						{
							var componentsNode = JsonNode.Parse(componentsEl.GetRawText());
							operationSpecNode["components"] = componentsNode;
						}

						var operationSpecJson = operationSpecNode.ToJsonString();
						var binaryData = new BinaryData(Encoding.UTF8.GetBytes(operationSpecJson));

						var openApiToolDef = new OpenApiToolDefinition(
							name: operationId,
							description: description,
							spec: binaryData,
							openApiAuthentication: new OpenApiAnonymousAuthDetails()
						);

						var aiTool = openApiToolDef.AsAITool();						
						aiTools.Add(aiTool);
						logger?.LogDebug("Created AITool '{OperationId}' for {HttpMethod} {Path}", operationId, httpMethod, path);
					}
				}
			}

			logger?.LogInformation("Extracted {ToolCount} operations from OpenAPI specification {ApiName}", aiTools.Count, apiName);
			return aiTools;
		}
		catch (Exception ex)
		{
			logger?.LogError(ex, "Error parsing OpenAPI operations from {ApiName}", apiName);
			throw;
		}
	}

	/// <summary>
	/// Converts a YAML string to JSON format.
	/// OpenAPI tools expect JSON format, so YAML specs need to be converted first.
	/// </summary>
	/// <param name="yamlContent">The YAML content to convert.</param>
	/// <returns>The JSON representation of the YAML content.</returns>
	private static string ConvertYamlToJson(string yamlContent)
	{
		// Use a deserializer without naming conventions to preserve original YAML structure
		// This is important for OpenAPI specs which may have specific parameter names
		var deserializer = new DeserializerBuilder()
			.Build();

		var data = deserializer.Deserialize<dynamic>(yamlContent);
		var json = JsonSerializer.Serialize(data);

		return json;
	}
}