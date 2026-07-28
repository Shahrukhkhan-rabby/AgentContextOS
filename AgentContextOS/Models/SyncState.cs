namespace AgentContextOS.Models;

/// <summary>
/// Tracks the last-synced Git commit per project to avoid re-ingestion.
/// </summary>
public sealed class SyncState
{
    public required string ProjectHash { get; set; }

    public string? LastCommitSha { get; set; }

    public DateTimeOffset LastSyncedAt { get; set; } = DateTimeOffset.UtcNow;
}
