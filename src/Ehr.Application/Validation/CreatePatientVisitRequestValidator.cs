using Ehr.Application.DTOs.Encounters;
using FluentValidation;

namespace Ehr.Application.Validation;

public sealed class CreatePatientVisitRequestValidator : AbstractValidator<CreatePatientVisitRequest>
{
    public CreatePatientVisitRequestValidator()
    {
        RuleFor(x => x.Title)
            .MaximumLength(200)
            .When(x => !string.IsNullOrWhiteSpace(x.Title));

        RuleFor(x => x.OccurredAt)
            .Must(d => d != default)
            .WithMessage("Visit date and time is required.");

        RuleFor(x => x.RawConversation)
            .NotEmpty()
            .WithMessage("A conversation transcript is required.")
            .MaximumLength(200_000);
    }
}
