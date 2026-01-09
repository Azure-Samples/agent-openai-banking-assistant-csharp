global using Microsoft.AspNetCore.Mvc;
global using System.Text.Json;

global using Azure;
global using Azure.Identity;
global using Azure.Storage.Blobs;
global using Azure.Storage.Blobs.Models;
global using Azure.AI.OpenAI;
global using Azure.AI.DocumentIntelligence;

global using Microsoft.Agents.AI;
global using Microsoft.Agents.AI.Workflows;
global using Microsoft.Extensions.AI;

global using ModelContextProtocol.Client;
global using ModelContextProtocol.Protocol.Transport;

global using BankingAssistant.Interfaces;
global using BankingAssistant.Models;
global using BankingAssistant.Services;
global using BankingAssistant.Proxy;
global using BankingAssistant.Configurations;
global using BankingAssistant.Extensions;
global using BankingAssistant.Agents.Infrastructure;
global using BankingAssistant.Agents.Orchestration;
global using BankingAssistant.Agents.Utils;
global using BankingAssistant.Agents.Tools;
