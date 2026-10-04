using Ehr.Application.Abstractions.Coding;
using Ehr.Application.Abstractions.Clinical;
using Ehr.Application.Abstractions.Persistence;
using Ehr.Application.Abstractions.Pipeline;
using Ehr.Application.Abstractions.Processing;
using Ehr.Application.Abstractions.Repositories;
using Ehr.Application.DTOs.AI;
using Ehr.Application.DTOs.Pipeline;
using Ehr.Application.Utilities;
using Ehr.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Ehr.Application.Services;

public sealed class EncounterAiPipelineService(
    IEncounterRepository encounters,
    IConversationProcessingService conversation,
    ISoapGenerationService soapGeneration,
    IMedicalCodingService medicalCoding,
    IEncounterPipelinePersistence pipelinePersistence,
    ILogger<EncounterAiPipelineService> log) : IEncounterAiPipelineService
{
    public async Task<EncounterPipelineResultDto> RunAsync(
        EncounterPipelineRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.RawConversation))
        {
            throw new ArgumentException("Raw conversation is required.", nameof(request));
        }

        var encounter = await encounters.GetWithPatientAsync(
            request.TenantId, request.EncounterId, cancellationToken);
        if (encounter is null)
        {
            throw new KeyNotFoundException("Encounter not found for tenant.");
        }

        if (encounter.Patient is null)
        {
            throw new InvalidOperationException("Encounter is missing a patient link.");
        }

        var cleaned = conversation.CleanTranscript(request.RawConversation);
        if (string.IsNullOrEmpty(cleaned))
        {
            throw new InvalidOperationException("Transcript is empty after cleaning.");
        }

        var soapInput = ToSoapWorkflowInput(encounter, encounter.Patient, cleaned);
        var soap = await soapGeneration.GenerateAsync(
            soapInput,
            cleanTranscriptFirst: false,
            cancellationToken);

        var codingInput = new MedicalCodingWorkflowInput
        {
            VisitTypeDisplay = ClinicalLabels.VisitType(encounter.Type),
            ChiefComplaint = encounter.ChiefComplaint,
            ServiceDate = encounter.ServiceDate,
            ClinicalSoapDocument = ToClinicalNoteBlock(soap),
            ProceduresNarrative = "Not supplied in this pipeline (link procedures in a later iteration).",
            PrescriptionsNarrative = "Not supplied in this pipeline (link eRx in a later iteration).",
        };

        var coding = await medicalCoding.SuggestCodesAsync(codingInput, cancellationToken);

        var save = await pipelinePersistence.PersistAsync(
            request.TenantId,
            request.EncounterId,
            soap,
            coding,
            request.RawConversation,
            cancellationToken);

        log.LogInformation(
            "Pipeline completed for encounter {EncounterId}, SOAP {SoapNoteId}, code links {Links}",
            request.EncounterId, save.SoapNoteId, save.MedicalCodeLinksCreated);

        return new EncounterPipelineResultDto
        {
            EncounterId = request.EncounterId,
            SoapNoteId = save.SoapNoteId,
            CleanedTranscript = cleaned,
            Soap = ToSnapshot(soap),
            Coding = ToSnapshot(coding),
            MedicalCodeLinksCreated = save.MedicalCodeLinksCreated,
            MedicalCodesCatalogEntriesCreated = save.MedicalCodesCreated,
        };
    }

    private static SoapWorkflowInput ToSoapWorkflowInput(
        Encounter e,
        Patient p,
        string cleanedTranscript) => new()
    {
        PatientLastName = p.LastName,
        PatientFirstName = p.FirstName,
        Age = PatientAgeCalculator.Years(p.DateOfBirth, e.ServiceDate),
        GenderDisplay = ClinicalLabels.FormatGender(p.Gender),
        DateOfBirth = p.DateOfBirth,
        ServiceDate = e.ServiceDate,
        VisitTypeDisplay = ClinicalLabels.VisitType(e.Type),
        ChiefComplaint = e.ChiefComplaint,
        VisitNotes = e.VisitNotes,
        Transcript = cleanedTranscript,
    };

    private static string ToClinicalNoteBlock(SoapGenerationResult s) =>
        $"""
         Subjective: {s.Subjective}

         Objective: {s.Objective}

         Assessment: {s.Assessment}

         Plan: {s.Plan}
         """;

    private static SoapSnapshotDto ToSnapshot(SoapGenerationResult s) => new()
    {
        Subjective = s.Subjective,
        Objective = s.Objective,
        Assessment = s.Assessment,
        Plan = s.Plan,
        ChiefComplaint = s.ChiefComplaint,
    };

    private static MedicalCodingSnapshotDto ToSnapshot(MedicalCodingResult c)
    {
        var lines = c.LineItems
            .Select(
                l => new MedicalCodingLineSnapshotDto
                {
                    CptCode = l.CptCode,
                    Icd10Codes = l.Icd10Codes.ToList(),
                    Quantity = l.Quantity,
                    Modifier1 = l.Modifier1,
                    Modifier2 = l.Modifier2,
                    Reasoning = l.Reasoning,
                })
            .ToList();
        return new MedicalCodingSnapshotDto
        {
            PrimaryIcd10 = c.PrimaryIcd10,
            LineItems = lines,
            AdditionalNotes = c.AdditionalNotes,
        };
    }
}
