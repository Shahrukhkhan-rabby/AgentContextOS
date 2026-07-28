namespace AgentContextOS.Models;

public sealed class ContextEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public ContextEventType Type { get; set; }

    public required string Content { get; set; }

    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>JSON-serialized metadata bag (author, sha, file paths, etc.).</summary>
    public string? Metadata { get; set; }

    /// <summary>Deterministic SHA-256 hash of the repository root path for project isolation.</summary>
    public required string ProjectHash { get; set; }

    /// <summary>Vector embedding stored as little-endian float BLOB for sqlite-vec compatibility.</summary>
    public byte[]? Embedding { get; set; }
}
