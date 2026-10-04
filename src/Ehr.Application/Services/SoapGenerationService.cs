using System.Globalization;
using Ehr.Application.Abstractions.AI;
using Ehr.Application.Abstractions.Clinical;
using Ehr.Application.Abstractions.Processing;
using Ehr.Application.DTOs.AI;
using Microsoft.Extensions.Logging;

namespace Ehr.Application.Services;

public sealed class SoapGenerationService(
    IAIService ai,
    IConversationProcessingService conversation,
    ILogger<SoapGenerationService> log) : ISoapGenerationService
{
    public async Task<SoapGenerationResult> GenerateAsync(
        SoapWorkflowInput input,
        bool cleanTranscriptFirst = true,
        CancellationToken cancellationToken = default)
    {
        var transcript = input.Transcript;
        if (cleanTranscriptFirst && !string.IsNullOrEmpty(transcript))
        {
            transcript = conversation.CleanTranscript(transcript);
        }
        else if (string.IsNullOrWhiteSpace(transcript))
        {
            transcript = null;
        }

        var request = new SoapGenerationRequest
        {
            PatientName = $"{input.PatientFirstName} {input.PatientLastName}".Trim(),
            Age = input.Age,
            Gender = input.GenderDisplay,
            DateOfBirth = input.DateOfBirth.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            VisitDate = input.ServiceDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            VisitType = input.VisitTypeDisplay,
            ChiefComplaint = input.ChiefComplaint ?? "Not specified",
            VisitNotes = string.IsNullOrWhiteSpace(input.VisitNotes) ? "None" : input.VisitNotes!,
            Transcript = transcript,
            ProceduresNarrative = string.IsNullOrWhiteSpace(input.ProceduresNarrative)
                ? "No procedures documented"
                : input.ProceduresNarrative!,
            PrescriptionsNarrative = string.IsNullOrWhiteSpace(input.PrescriptionsNarrative)
                ? "No prescriptions written"
                : input.PrescriptionsNarrative!,
            PreviousVisitsNarrative = string.IsNullOrWhiteSpace(input.PreviousVisitsNarrative)
                ? (IsInitialVisitName(input.VisitTypeDisplay) ? "N/A (Initial visit)" : "No previous visits on record")
                : input.PreviousVisitsNarrative!,
        };

        log.LogInformation(
            "SOAP generation: transcript path = {WithTranscript}",
            transcript is { Length: > 0 });

        return await ai.GenerateSoapAsync(request, cancellationToken);
    }

    private static bool IsInitialVisitName(string type)
    {
        return type.Contains("Initial", StringComparison.OrdinalIgnoreCase);
    }
}
