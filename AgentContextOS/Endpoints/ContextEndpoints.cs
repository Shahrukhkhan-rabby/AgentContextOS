using AgentContextOS.DTOs;
using AgentContextOS.Services;

namespace AgentContextOS.Endpoints;

public static class ContextEndpoints
{
    public static WebApplication MapContextEndpoints(this WebApplication app)
    {
        app.MapGet("/context", async (
            string query,
            int? limit,
            IContextRetrievalService retrievalService,
            IProjectHashService projectHashService,
            HttpContext httpContext,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return Results.BadRequest(
                    ApiResponse<List<ContextQueryResultDto>>.Fail("Query parameter is required", 400));
            }

            var projectPath = httpContext.Request.Headers["X-Project-Path"].FirstOrDefault();
            var projectHash = projectHashService.ComputeHash(projectPath);

            var results = await retrievalService.RetrieveAsync(query, projectHash, limit ?? 10, ct);

            return Results.Ok(ApiResponse<List<ContextQueryResultDto>>.Ok(
                results,
                $"Retrieved {results.Count} context fragments"));
        })
        .WithName("QueryContext")
        .WithTags("Context")
        .Produces<ApiResponse<List<ContextQueryResultDto>>>(200)
        .Produces<ApiResponse<List<ContextQueryResultDto>>>(400);

        return app;
    }
}
