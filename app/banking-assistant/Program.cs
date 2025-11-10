using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Identity.Web;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .AddJsonFile("appsettings.json", optional: true)
    .AddEnvironmentVariables("DOTNET_");

var appInsightsActive = builder.Configuration.GetValue<bool>("ApplicationInsights:Active");
if (appInsightsActive)
{
    builder.Services.AddApplicationInsightsTelemetry();
}

var configuration = builder.Configuration;
var allKeys = configuration.AsEnumerable();
foreach (var kvp in allKeys)
{
    Console.WriteLine($"Key: {kvp.Key}, Value: {kvp.Value}");
}

// @TODO: Temporary. Fix later.
builder.Services.AddCors(options => options.AddPolicy("allowSpecificOrigins", policy => policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

// Bind Backend Services uri settings
builder.Services.AddOptions<BackendApisOption>()
        .Bind(builder.Configuration.GetSection("BackendAPIs"))
        .ValidateDataAnnotations()
        .ValidateOnStart();

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

builder.Services.AddSingleton<ILoggerFactory>(LoggerFactory.Create(builder => builder.AddConsole()));

// Use the custom extension method to register azure services.
builder.Services.AddAzureServices(configuration);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseCors("allowSpecificOrigins");
}

// app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
