using Microsoft.SemanticKernel.Plugins.OpenApi;
using System.Reflection;

internal class AgenticUtils
{
    /// <summary>
    /// Retrieves the embedded YAML file stream for the specified API name.
    /// </summary>
    /// <param name="apiName">The name of the API whose YAML file is to be retrieved.</param>
    /// <returns>A <see cref="Stream"/> containing the YAML file content.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the resource is not found or an error occurs while reading the file.</exception>
    private static Stream? GetAPIYaml(string apiName)
    {
        try
        {
            // Get the current assembly
            Assembly assembly = Assembly.GetExecutingAssembly();

            // Define the full resource name (namespace + file name)
            var resourceNames = assembly.GetManifestResourceNames();

            // Find the resource that matches the apiName parameter
            var resourceName = resourceNames
                .FirstOrDefault(name => name.EndsWith(string.Concat(apiName, ".yaml"), StringComparison.OrdinalIgnoreCase));

            if (resourceName == null)
            {
                // If no resource is found, throw an exception
                throw new InvalidOperationException($"Resource '{apiName}.yaml' not found.");
            }

            // Return the stream for the found resource
            return assembly.GetManifestResourceStream(resourceName);
        }
        catch (Exception ex)
        {
            // Handle any error (e.g., file not found, IO issues, etc.)
            throw new InvalidOperationException("Error reading the embedded YAML file.", ex);
        }
    }

    /// <summary>
    /// Adds an OpenAPI plugin to the specified kernel.
    /// </summary>
    /// <param name="kernel">The kernel to which the plugin will be added.</param>
    /// <param name="apiName">The name of the API for which the plugin is created.</param>
    /// <param name="pluginName">The name of the plugin.</param>
    /// <param name="apiUrl">The URL of the API server.</param>
    public static async void AddOpenAPIPlugin(Kernel kernel, string apiName, string pluginName, string apiUrl)
    {
        var stream = GetAPIYaml(apiName);

        var uri = new Uri(apiUrl);

#pragma warning disable SKEXP0040 // Experimental API 
        OpenApiFunctionExecutionParameters parameters = new OpenApiFunctionExecutionParameters(serverUrlOverride: uri);

        KernelPlugin plugin = await OpenApiKernelPluginFactory.CreateFromOpenApiAsync(pluginName, stream!, parameters);

        kernel.Plugins.Add(plugin);
    }

    /// <summary>
    /// Adds an MCP server plugin and retrieves the list of tools provided by the server.
    /// </summary>
    /// <param name="clientName">The name of the MCP client.</param>
    /// <param name="pluginName">The name of the plugin.</param>
    /// <param name="apiUrl">The URL of the MCP server.</param>
    /// <param name="useStreamableHttp">Indicates whether to use streamable HTTP.  Default false will use SSE </param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a list of <see cref="McpClientTool"/>.</returns>
    public static async Task<IList<McpClientTool>> AddMcpServerPluginAsync(string clientName, string pluginName, string apiUrl, bool useStreamableHttp = false)
    {
        // Create a new MCP client using the SseClientTransport
        var mcpClient = await McpClientFactory.CreateAsync(
                new SseClientTransport(
                    new SseClientTransportOptions() { Endpoint = new Uri(apiUrl), Name = clientName, UseStreamableHttp  = useStreamableHttp }));

        // Retrieve and display the list provided by the MCP server
        return await mcpClient.ListToolsAsync();

    }

}

