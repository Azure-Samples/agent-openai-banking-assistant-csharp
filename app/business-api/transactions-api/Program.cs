
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddSingleton<ILoggerFactory>(LoggerFactory.Create(builder => builder.AddConsole()));

builder.Services.AddServices(builder.Configuration);

//Add MCP Server and register tool classes
builder.Services
    .AddMcpServer(options =>
    {
        options.ServerInfo = new()
        {
            Name = "Transaction Tool Server",
            Version = "1.0.0",
        };
    })
    .WithHttpTransport()
    .WithTools<TransactionTool>();

var app = builder.Build();

app.MapMcp();

app.Run();
