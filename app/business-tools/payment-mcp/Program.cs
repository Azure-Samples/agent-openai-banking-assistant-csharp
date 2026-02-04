using Serilog;
using Serilog.Events;
using Scalar.AspNetCore;
using OpenTelemetry.Trace;
using OpenTelemetry.Resources;
using Azure.Monitor.OpenTelemetry.Exporter;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .AddJsonFile("appsettings.json", optional: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true);

if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddUserSecrets<Program>();
}

builder.Configuration.AddEnvironmentVariables("DOTNET_");

var appInsightsActiveString = builder.Configuration.GetValue<string>("ApplicationInsights:Active") ?? "false";
var appInsightsActive = bool.TryParse(appInsightsActiveString, out var result) && result;

// Configure Serilog
var loggerConfig = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithProcessId()
    .Enrich.WithThreadId()
    .Enrich.WithProperty("Application", "PaymentMcp")
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}");

if (appInsightsActive)
{
    var appInsightsConnectionString = builder.Configuration.GetValue<string>("ApplicationInsights:ConnectionString");
    
    if (!string.IsNullOrEmpty(appInsightsConnectionString))
    {
        loggerConfig.WriteTo.ApplicationInsights(appInsightsConnectionString, TelemetryConverter.Traces);
    }
}

Log.Logger = loggerConfig.CreateLogger();

builder.Host.UseSerilog();

// Configure OpenTelemetry for ActivitySource export
if (appInsightsActive)
{
    var appInsightsConnectionString = builder.Configuration.GetValue<string>("ApplicationInsights:ConnectionString");
    if (!string.IsNullOrEmpty(appInsightsConnectionString))
    {
        var resourceBuilder = ResourceBuilder
            .CreateDefault()
            .AddService("PaymentMcp");

        builder.Services.AddOpenTelemetry()
            .WithTracing(tracing => tracing
                .SetResourceBuilder(resourceBuilder)
                .AddSource("PaymentMcp.Tools.PaymentTool")
                .AddAspNetCoreInstrumentation()
                .AddAzureMonitorTraceExporter(options =>
                    options.ConnectionString = appInsightsConnectionString));
    }
}

try
{
    Log.Information("Starting Payment MCP Server");

// Add services to the container.
builder.Services.AddServices();

builder.Services.AddOpenApi();

// Add MCP Server and register tool classes
builder.Services
    .AddMcpServer(options =>
    {
        options.ServerInfo = new()
        {
            Name = "Payment Tool Server",
            Version = "1.0.0",
        };
    })
    .WithHttpTransport()
    .WithTools<PaymentTool>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapMcp("/mcp");

app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Payment MCP Server terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
