using Serilog;

try
{
	var builder = WebApplication.CreateBuilder(args);

	builder.Configuration
        .AddJsonFile("appsettings.json", optional: true)
        .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true);

    if (builder.Environment.IsDevelopment())
    {
        builder.Configuration.AddUserSecrets<Program>();
    }

    builder.Configuration.AddEnvironmentVariables("DOTNET_");

    // Configure OpenTelemetry
    builder.Services.AddOpenTelemetryConfiguration(builder.Configuration, builder.Environment);

    // Configure Serilog
    builder.ConfigureSerilog();

    Log.Information("Starting Banking Assistant application");

    var configuration = builder.Configuration;
    var allKeys = configuration.AsEnumerable();
    var logger = Log.Logger;

    foreach (var kvp in allKeys)
    {
        logger.Information("Configuration - Key: {Key}, Value: {Value}", kvp.Key, kvp.Value);
    }

    // Configure services
    builder.Services.AddAuthenticationConfiguration(builder.Configuration, builder.Environment);
    builder.Services.AddValidationAndCors();
    builder.Services.AddApiDocumentation();
    builder.Services.AddAzureServices(builder.Configuration, builder.Environment);

    var app = builder.Build();

    // Configure HTTP request pipeline
    app.UseApplicationPipeline();

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
