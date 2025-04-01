﻿using Azure.Core;
using Azure.Identity;

public class CredentialConfiguration
{
    private ILogger _logger;
    public CredentialConfiguration(ILogger logger)
    {
        _logger = logger;
    }
    public TokenCredential? getCredential(WebApplicationBuilder builder)
    {
        TokenCredential credential = null;

        if (builder.Environment.IsProduction() || builder.Environment.IsStaging())
        {
            string? clientId = builder.Configuration["UserAssignedClientId"];
            if (string.IsNullOrEmpty(clientId))
            {
                _logger.LogInformation("Did not find valid clientId in Configuration using default MIC.");
                credential = new ManagedIdentityCredential();
            }
            else
            {
                credential = new ManagedIdentityCredential(
                ManagedIdentityId.FromUserAssignedClientId(clientId));
            }
        }
        else
        {
            // local development environment
            credential = new ChainedTokenCredential(
                new VisualStudioCredential(),
                new AzureCliCredential(),
                new AzurePowerShellCredential());
        }
        return credential;
    }
}
