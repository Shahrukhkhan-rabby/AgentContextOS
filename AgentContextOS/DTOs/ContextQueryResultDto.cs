using AgentContextOS.Models;

namespace AgentContextOS.DTOs;

public sealed record ContextQueryResultDto(
    Guid Id,
    ContextEventType Type,
    string Content,
    DateTimeOffset Timestamp,
    string? Metadata,
    double Score);
