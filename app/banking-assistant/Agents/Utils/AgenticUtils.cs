﻿using Microsoft.SemanticKernel.Plugins.OpenApi;
using Microsoft.SemanticKernel;
using System.Reflection;

public class AgenticUtils
{
    //protected readonly IToolsExecutionCache _toolsExecutionCache;
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
    public static async void AddOpenAPIPlugin(Kernel kernel, string apiName, string pluginName, string apiUrl)
    {
        var stream = GetAPIYaml(apiName);

        var uri = new Uri(apiUrl);

#pragma warning disable SKEXP0040 // Experimental API 
        OpenApiFunctionExecutionParameters parameters = new OpenApiFunctionExecutionParameters(serverUrlOverride: uri);

        KernelPlugin plugin = await OpenApiKernelPluginFactory.CreateFromOpenApiAsync(pluginName, stream!, parameters);

        kernel.Plugins.Add(plugin);
    }
}

