using AgentContextOS.Models;

namespace AgentContextOS.DTOs;

public sealed record CreateEventRequestDto(
    ContextEventType Type,
    string Content,
    string? Metadata = null);

public sealed record EventDto(
    Guid Id,
    ContextEventType Type,
    string Content,
    DateTimeOffset Timestamp,
    string? Metadata,
    string ProjectHash);
