using System.Text;
using AgentContextOS.DTOs;
using AgentContextOS.Models;

namespace AgentContextOS.Services;

public interface IContextComposerService
{
    Task<ComposeResponseDto> ComposeAsync(string prompt, string projectHash, CancellationToken ct = default);
}

public sealed class ContextComposerService(
    IContextRetrievalService retrievalService,
    ITokenBudgetService tokenBudgetService,
    ILogger<ContextComposerService> logger) : IContextComposerService
{
    public async Task<ComposeResponseDto> ComposeAsync(
        string prompt, string projectHash, CancellationToken ct = default)
    {
        var fragments = await retrievalService.RetrieveAsync(prompt, projectHash, limit: 20, ct: ct);

        // Iteratively trim lowest-score fragments until the brief fits within token budget
        while (true)
        {
            var brief = BuildBrief(prompt, fragments);
            var tokenCount = tokenBudgetService.CountTokens(brief);

            if (tokenCount <= tokenBudgetService.Budget || fragments.Count == 0)
            {
                logger.LogInformation(
                    "Composed brief: {FragmentCount} fragments, {TokenCount} tokens",
                    fragments.Count, tokenCount);

                return new ComposeResponseDto(brief, fragments.Count, tokenCount);
            }

            // Remove the lowest-scored fragment and retry
            var minScore = fragments.Min(f => f.Score);
            var toRemove = fragments.First(f => f.Score == minScore);
            fragments.Remove(toRemove);
        }
    }

    private static string BuildBrief(string prompt, List<ContextQueryResultDto> fragments)
    {
        var commits = fragments.Where(f => f.Type == ContextEventType.Commit).ToList();
        var chats = fragments.Where(f => f.Type == ContextEventType.Chat).ToList();
        var errors = fragments.Where(f => f.Type == ContextEventType.Error).ToList();

        var sb = new StringBuilder();
        sb.AppendLine("# 🧠 Engineering Context Brief");
        sb.AppendLine();

        if (commits.Count > 0)
        {
            sb.AppendLine("## 📝 Recent Commits");
            foreach (var c in commits)
                sb.AppendLine($"- [{c.Timestamp:yyyy-MM-dd HH:mm}] {c.Content}");
            sb.AppendLine();
        }

        if (chats.Count > 0)
        {
            sb.AppendLine("## 💬 Chat History");
            foreach (var c in chats)
                sb.AppendLine($"- [{c.Timestamp:yyyy-MM-dd HH:mm}] {c.Content}");
            sb.AppendLine();
        }

        if (errors.Count > 0)
        {
            sb.AppendLine("## ⚠️ Known Errors");
            foreach (var e in errors)
                sb.AppendLine($"- [{e.Timestamp:yyyy-MM-dd HH:mm}] {e.Content}");
            sb.AppendLine();
        }

        sb.AppendLine("## 🔍 Original Query");
        sb.AppendLine(prompt);
        sb.AppendLine();

        return sb.ToString();
    }
}
