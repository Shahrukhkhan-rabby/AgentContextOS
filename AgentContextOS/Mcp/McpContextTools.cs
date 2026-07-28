using System.ComponentModel;
using AgentContextOS.DTOs;
using AgentContextOS.Models;
using AgentContextOS.Services;
using ModelContextProtocol.Server;

namespace AgentContextOS.Mcp;

[McpServerToolType]
public class McpContextTools(
    IEventService eventService,
    IContextComposerService composerService,
    IProjectHashService projectHashService)
{
    [McpServerTool, Description(
        "Saves the AI agent's session summary, decisions, and outcomes into permanent engineering memory. " +
        "Call this after completing any significant task.")]
    public async Task<string> RecordSession(
        [Description("High-level summary of what was accomplished in this session")] string summary,
        [Description("Key decisions made (e.g. 'Use SQLite for persistence', 'Chose append-only model')")] string[] decisions,
        [Description("Absolute path to the project repository root")] string projectPath,
        CancellationToken cancellationToken = default)
    {
        var content = $"Summary: {summary}\nDecisions: {string.Join(", ", decisions)}";

        await eventService.IngestAsync(
            new CreateEventRequestDto(ContextEventType.Chat, content),
            projectPath,
            cancellationToken);

        return "Context successfully archived to AgentContextOS.";
    }

    [McpServerTool, Description(
        "Retrieves relevant past decisions, recent commits, and known errors to enrich the current prompt with engineering memory. " +
        "Call this before starting any significant task.")]
    public async Task<string> GetMemory(
        [Description("The current user query or task description to retrieve context for")] string userQuery,
        [Description("Absolute path to the project repository root")] string projectPath,
        CancellationToken cancellationToken = default)
    {
        var projectHash = projectHashService.ComputeHash(projectPath);
        var result = await composerService.ComposeAsync(userQuery, projectHash, cancellationToken);
        return result.EnrichedPrompt;
    }
}
