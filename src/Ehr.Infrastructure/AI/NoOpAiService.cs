using Ehr.Application.Abstractions.AI;
using Ehr.Application.DTOs.AI;
using Microsoft.Extensions.Logging;

namespace Ehr.Infrastructure.AI;

/// <summary>Deterministic stand-in that is not registered in DI. <see cref="OpenAiService"/> is the implementation the API uses.</summary>
public sealed class NoOpAiService(ILogger<NoOpAiService> log) : IAIService
{
    public Task<SoapGenerationResult> GenerateSoapAsync(
        SoapGenerationRequest request,
        CancellationToken cancellationToken = default)
    {
        log.LogWarning("NoOp IAIService.GenerateSoapAsync: configure a real AI provider in DI.");
        var t = string.IsNullOrEmpty(request.Transcript) ? "no transcript" : "transcript provided";
        var s = $"(NoOp placeholder SOAP — {t})";
        return Task.FromResult(
            new SoapGenerationResult
            {
                Subjective = s,
                Objective = s,
                Assessment = s,
                Plan = s,
                ChiefComplaint = string.IsNullOrWhiteSpace(request.ChiefComplaint) ||
                    string.Equals(request.ChiefComplaint, "Not specified", StringComparison.Ordinal)
                    ? $"Visit on {request.VisitDate} — place chief complaint here"
                    : request.ChiefComplaint,
            });
    }

    public Task<MedicalCodingResult> ExtractCodesAsync(
        MedicalCodingRequest request,
        CancellationToken cancellationToken = default)
    {
        log.LogWarning("NoOp IAIService.ExtractCodesAsync: configure a real AI provider in DI.");
        return Task.FromResult(
            new MedicalCodingResult
            {
                LineItems = [],
                PrimaryIcd10 = "N/A",
                AdditionalNotes = "NoOp IAIService: no codes generated. This type is not registered; OpenAiService is.",
            });
    }
}
