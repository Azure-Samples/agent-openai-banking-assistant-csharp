using Serilog;
using Serilog.Events;
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
    .Enrich.WithProperty("Application", "TransactionsApi")
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

try
{
    Log.Information("Starting Transactions API");

// Add services to the container.
// @TODO: Temporary. Fix later.
builder.Services.AddCors(options => options.AddPolicy("allowSpecificOrigins", policy => policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

// Add services to the container.
builder.Services.AddServices(builder.Configuration);

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
    app.UseCors("allowSpecificOrigins");
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Transactions API terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
