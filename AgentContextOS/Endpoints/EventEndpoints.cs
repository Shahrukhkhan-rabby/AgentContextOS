using AgentContextOS.DTOs;
using AgentContextOS.Services;
using FluentValidation;

namespace AgentContextOS.Endpoints;

public static class EventEndpoints
{
    public static WebApplication MapEventEndpoints(this WebApplication app)
    {
        app.MapPost("/events", async (
            CreateEventRequestDto request,
            IValidator<CreateEventRequestDto> validator,
            IEventService eventService,
            HttpContext httpContext,
            CancellationToken ct) =>
        {
            var validation = await validator.ValidateAsync(request, ct);
            if (!validation.IsValid)
            {
                var errors = validation.Errors
                    .Select(e => new ApiFieldError(e.PropertyName, e.ErrorMessage));
                return Results.BadRequest(
                    ApiResponse<EventDto>.Fail("Validation failed", 400, errors));
            }

            var projectPath = httpContext.Request.Headers["X-Project-Path"].FirstOrDefault();
            var dto = await eventService.IngestAsync(request, projectPath, ct);

            return Results.Created($"/events/{dto.Id}",
                ApiResponse<EventDto>.Created(dto, "Event ingested successfully"));
        })
        .WithName("IngestEvent")
        .WithTags("Events")
        .Accepts<CreateEventRequestDto>("application/json")
        .Produces<ApiResponse<EventDto>>(201)
        .Produces<ApiResponse<EventDto>>(400);

        return app;
    }
}
