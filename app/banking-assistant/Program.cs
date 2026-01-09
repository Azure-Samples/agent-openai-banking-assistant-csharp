using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Identity.Web;
using Serilog;
using Scalar.AspNetCore;

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
    .Enrich.WithProperty("Application", "BankingAssistant")
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}");

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

try
{
    Log.Information("Starting Banking Assistant application");

    var configuration = builder.Configuration;
    var allKeys = configuration.AsEnumerable();
    foreach (var kvp in allKeys)
    {
        Console.WriteLine($"Key: {kvp.Key}, Value: {kvp.Value}");
    }

    // @TODO: Temporary. Fix later.
    builder.Services.AddCors(options => options.AddPolicy("allowSpecificOrigins", policy => policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

    // Configure authentication using Azure AD
    if (builder.Environment.IsDevelopment())
    {
        // In development, use the fake authentication scheme.
        builder.Services.AddAuthentication("Fake")
            .AddScheme<AuthenticationSchemeOptions, FakeAuthenticationHandler>("Fake", options => { });
    }
    else
    {
        // In production or staging, use JWT Bearer authentication via Azure AD.
        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));
    }

    builder.Services.AddAuthorization();

    // Use the custom extension method to register azure services.
    builder.Services.AddAzureServices(configuration);

    builder.Services.AddControllers();
    builder.Services.AddOpenApi();

    var app = builder.Build();

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
        app.MapScalarApiReference();
        app.UseCors("allowSpecificOrigins");
    }

    // app.UseHttpsRedirection();

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
