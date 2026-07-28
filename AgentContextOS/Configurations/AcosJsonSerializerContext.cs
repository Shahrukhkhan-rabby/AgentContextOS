using System.Text.Json.Serialization;
using AgentContextOS.DTOs;
using AgentContextOS.Models;

namespace AgentContextOS.Configurations;

[JsonSerializable(typeof(ApiResponse<object>))]
[JsonSerializable(typeof(ApiResponse<EventDto>))]
[JsonSerializable(typeof(ApiResponse<List<ContextQueryResultDto>>))]
[JsonSerializable(typeof(ApiResponse<ComposeResponseDto>))]
[JsonSerializable(typeof(CreateEventRequestDto))]
[JsonSerializable(typeof(ComposeRequestDto))]
[JsonSerializable(typeof(EventDto))]
[JsonSerializable(typeof(ContextQueryResultDto))]
[JsonSerializable(typeof(ComposeResponseDto))]
[JsonSerializable(typeof(ApiFieldError))]
[JsonSerializable(typeof(ContextEventType))]
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
public sealed partial class AcosJsonSerializerContext : JsonSerializerContext;
