using AgentContextOS.DTOs;
using AgentContextOS.Services;

namespace AgentContextOS.Endpoints;

public static class SyncEndpoints
{
    public static WebApplication MapSyncEndpoints(this WebApplication app)
    {
        app.MapPost("/sync/git", async (
            HttpContext httpContext,
            IGitIngestionService gitService,
            CancellationToken ct) =>
        {
            var projectPath = httpContext.Request.Headers["X-Project-Path"].FirstOrDefault()
                ?? Directory.GetCurrentDirectory();

            var count = await gitService.SyncRepositoryAsync(projectPath, ct);

            return Results.Ok(ApiResponse<object>.Ok(new
            {
                IngestedCommits = count,
                ProjectPath = projectPath,
                SyncedAt = DateTimeOffset.UtcNow
            }, $"Synced {count} new commits"));
        })
        .WithName("SyncGit")
        .WithTags("Sync")
        .Produces<ApiResponse<object>>(200);

        return app;
    }
}
