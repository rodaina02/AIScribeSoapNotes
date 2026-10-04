using System.Globalization;
using Ehr.Application.Abstractions.AI;
using Ehr.Application.Abstractions.Coding;
using Ehr.Application.DTOs.AI;
using Microsoft.Extensions.Logging;

namespace Ehr.Application.Services;

public sealed class MedicalCodingService(IAIService ai, ILogger<MedicalCodingService> log) : IMedicalCodingService
{
    public async Task<MedicalCodingResult> SuggestCodesAsync(
        MedicalCodingWorkflowInput input,
        CancellationToken cancellationToken = default)
    {
        var serviceDate = input.ServiceDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var request = new MedicalCodingRequest
        {
            VisitType = input.VisitTypeDisplay,
            ChiefComplaint = string.IsNullOrWhiteSpace(input.ChiefComplaint)
                ? "Not specified"
                : input.ChiefComplaint!,
            ServiceDate = serviceDate,
            ClinicalNoteSoap = string.IsNullOrWhiteSpace(input.ClinicalSoapDocument)
                ? "No clinical note available"
                : input.ClinicalSoapDocument!,
            ProceduresNarrative = string.IsNullOrWhiteSpace(input.ProceduresNarrative) ? "None" : input.ProceduresNarrative!,
            PrescriptionsNarrative = string.IsNullOrWhiteSpace(input.PrescriptionsNarrative) ? "None" : input.PrescriptionsNarrative!,
        };

        log.LogInformation("Medical coding suggestion for service date {ServiceDate}", serviceDate);
        return await ai.ExtractCodesAsync(request, cancellationToken);
    }
}
