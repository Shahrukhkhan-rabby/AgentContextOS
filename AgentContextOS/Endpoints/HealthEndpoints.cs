using AgentContextOS.DTOs;

namespace AgentContextOS.Endpoints;

public static class HealthEndpoints
{
    public static WebApplication MapHealthEndpoints(this WebApplication app)
    {
        app.MapGet("/health", () =>
        {
            var response = ApiResponse<object>.Ok(new
            {
                Service = "AgentContextOS",
                Version = "1.0.0",
                Timestamp = DateTimeOffset.UtcNow
            });

            return Results.Ok(response);
        })
        .WithName("HealthCheck")
        .WithTags("Health");

        return app;
    }
}
