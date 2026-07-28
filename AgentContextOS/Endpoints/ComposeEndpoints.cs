using AgentContextOS.DTOs;
using AgentContextOS.Services;

namespace AgentContextOS.Endpoints;

public static class ComposeEndpoints
{
    public static void MapComposeEndpoints(this WebApplication app)
    {
        app.MapPost("/compose", HandleComposeAsync)
            .WithName("Compose")
            .WithTags("Compose")
            .Produces<ApiResponse<ComposeResponseDto>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest);
    }

    private static async Task<IResult> HandleComposeAsync(
        ComposeRequestDto request,
        IContextComposerService composerService,
        IProjectHashService projectHashService,
        HttpContext httpContext,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Prompt))
            return Results.BadRequest(ApiResponse<object>.Fail("Prompt is required"));

        // Resolve project path: request body > header > fallback
        var projectPath = request.ProjectPath
            ?? httpContext.Request.Headers["X-Project-Path"].FirstOrDefault()
            ?? "/default";

        var projectHash = projectHashService.ComputeHash(projectPath);

        var result = await composerService.ComposeAsync(request.Prompt, projectHash, ct);

        return Results.Ok(ApiResponse<ComposeResponseDto>.Ok(result));
    }
}
