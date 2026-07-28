using AgentContextOS.Data;
using AgentContextOS.Endpoints;
using AgentContextOS.Middlewares;

namespace AgentContextOS.Extensions;

public static class WebApplicationExtensions
{
    public static async Task InitializeDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AcosDbContext>();
        await db.Database.EnsureCreatedAsync();

        try
        {
            db.InitializeVectorExtension();
            app.Logger.LogInformation("sqlite-vec extension loaded and vector table initialized");
        }
        catch (Exception ex)
        {
            app.Logger.LogWarning(ex, "sqlite-vec extension could not be loaded — vector search disabled");
        }
    }

    public static WebApplication MapAcosEndpoints(this WebApplication app)
    {
        app.MapHealthEndpoints();
        app.MapEventEndpoints();
        app.MapSyncEndpoints();
        app.MapContextEndpoints();
        app.MapComposeEndpoints();
        app.MapMcp("/mcp");

        return app;
    }

    public static WebApplication UseAcosPipeline(this WebApplication app)
    {
        app.UseMiddleware<GlobalExceptionMiddleware>();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        return app;
    }
}
