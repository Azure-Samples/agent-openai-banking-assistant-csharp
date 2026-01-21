using Serilog;

namespace BankingAssistant.Extensions;

/// <summary>
/// Extension methods for WebApplicationBuilder to configure application startup and services.
/// </summary>
public static class WebApplicationBuilderExtensions
{
    /// <summary>
    /// Configures Serilog logging based on application configuration and environment.
    /// </summary>
    /// <param name="builder">The web application builder.</param>
    public static void ConfigureSerilog(this WebApplicationBuilder builder)
    {
        var loggerConfig = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .Enrich.WithProcessId()
            .Enrich.WithThreadId()
            .Enrich.WithProperty("Application", "BankingAssistant")
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}",
                standardErrorFromLevel: null)
            .WriteTo.Debug(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}");

        var appInsightsActiveString = builder.Configuration.GetValue<string>("ApplicationInsights:Active") ?? "false";
        var appInsightsActive = bool.TryParse(appInsightsActiveString, out var result) && result;

        if (appInsightsActive)
        {
            builder.Services.AddApplicationInsightsTelemetry();
            var appInsightsConnectionString = builder.Configuration.GetValue<string>("ApplicationInsights:ConnectionString");

            if (!string.IsNullOrEmpty(appInsightsConnectionString))
            {
                loggerConfig.WriteTo.ApplicationInsights(appInsightsConnectionString, TelemetryConverter.Traces);
            }
        }

        Log.Logger = loggerConfig.CreateLogger();
        builder.Host.UseSerilog();
    }
}
