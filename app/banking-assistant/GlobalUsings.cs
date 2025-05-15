
global using System.Text.Json;

global using Azure;
global using Azure.Identity;
global using Azure.Storage.Blobs;
global using Azure.Storage.Blobs.Models;
global using Azure.AI.OpenAI;
global using Azure.AI.DocumentIntelligence;


global using Microsoft.SemanticKernel;
global using Microsoft.SemanticKernel.Agents;
global using Microsoft.SemanticKernel.Agents.Chat;
global using Microsoft.SemanticKernel.ChatCompletion;
global using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
global using Microsoft.AspNetCore.Mvc;
global using Microsoft.Extensions.Options;

global using ModelContextProtocol.Client;
global using ModelContextProtocol.Protocol.Transport;

global using agent_openai_banking_assistant_csharp.Interfaces;
