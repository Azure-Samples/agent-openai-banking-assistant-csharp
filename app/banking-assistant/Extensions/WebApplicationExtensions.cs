using Scalar.AspNetCore;
using Microsoft.AspNetCore.HttpLogging;

namespace BankingAssistant.Extensions;

/// <summary>
/// Extension methods for WebApplication to configure HTTP request pipeline.
/// </summary>
public static class WebApplicationExtensions
{
    /// <summary>
    /// Configures the HTTP request pipeline with authentication, CORS, and OpenAPI documentation.
    /// </summary>
    /// <param name="app">The web application.</param>
    /// <returns>The web application for chaining.</returns>
    public static WebApplication UseApplicationPipeline(this WebApplication app)
    {
        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
            app.MapScalarApiReference();
            app.UseCors("allowSpecificOrigins");
        }

        // Enable HTTP logging for capturing request/response details
        app.UseHttpLogging();

        // app.UseHttpsRedirection();

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();

        return app;
    }
}
