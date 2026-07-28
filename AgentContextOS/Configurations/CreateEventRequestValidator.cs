using FluentValidation;
using AgentContextOS.DTOs;

namespace AgentContextOS.Configurations;

public sealed class CreateEventRequestValidator : AbstractValidator<CreateEventRequestDto>
{
    public CreateEventRequestValidator()
    {
        RuleFor(x => x.Content)
            .NotEmpty()
            .WithMessage("Content is required.")
            .MaximumLength(50_000)
            .WithMessage("Content must not exceed 50,000 characters.");

        RuleFor(x => x.Type)
            .IsInEnum()
            .WithMessage("Type must be Chat, Commit, or Error.");
    }
}
