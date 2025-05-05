var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// @TODO: Temporary. Fix later.
builder.Services.AddCors(options => options.AddPolicy("allowSpecificOrigins", policy => policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

// Add services to the container.
builder.Services.AddSingleton<ILoggerFactory>(LoggerFactory.Create(builder => builder.AddConsole()));

builder.Services.AddServices(builder.Configuration);

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseCors("allowSpecificOrigins");
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
