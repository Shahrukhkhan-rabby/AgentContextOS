namespace AgentContextOS.DTOs;

public sealed record ComposeRequestDto(
    string Prompt,
    string? ProjectPath = null);

public sealed record ComposeResponseDto(
    string EnrichedPrompt,
    int FragmentCount,
    int TokenCount);
