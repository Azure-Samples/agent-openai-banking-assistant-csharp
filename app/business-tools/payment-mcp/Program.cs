
var builder = WebApplication.CreateBuilder(args);
builder.Configuration
    .AddJsonFile("appsettings.json", optional: true)
    .AddEnvironmentVariables("DOTNET_");

// Add services to the container.
builder.Services.AddSingleton<ILoggerFactory>(LoggerFactory.Create(builder => builder.AddConsole()));

builder.Services.AddServices();

//Add MCP Server and register tool classes
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

app.MapMcp("/mcp");

app.Run();
