using Ehr.Application.Abstractions.Pipeline;
using Ehr.Application.Abstractions.Repositories;
using Ehr.Application.DTOs.Encounters;
using Ehr.Application.DTOs.Pipeline;
using Ehr.Application.Options;
using Ehr.Application.Utilities;
using Ehr.Domain.Entities;
using Ehr.Domain.Enums;
using Microsoft.Extensions.Options;

namespace Ehr.Application.Services;

public sealed class EncounterService(
    IEncounterRepository encounters,
    IPatientRepository patients,
    IEncounterAiPipelineService encounterPipeline,
    IOptions<DocumentationOptions> documentationOptions) : IEncounterService
{
    public async Task<EncounterResponse?> GetByIdAsync(
        Guid tenantId,
        Guid encounterId,
        CancellationToken cancellationToken = default)
    {
        var entity = await encounters.GetByIdAsync(tenantId, encounterId, cancellationToken);
        if (entity is null)
        {
            return null;
        }

        return new EncounterResponse
        {
            Id = entity.Id,
            TenantId = entity.TenantId,
            PatientId = entity.PatientId,
            FacilityId = entity.FacilityId,
            AttendingProviderUserId = entity.AttendingProviderUserId,
            ServiceDate = entity.ServiceDate,
            Status = entity.Status,
            Type = entity.Type,
            ChiefComplaint = entity.ChiefComplaint,
        };
    }

    public async Task<IReadOnlyList<EncounterHistoryItemDto>> ListHistoryForPatientAsync(
        Guid tenantId,
        Guid patientId,
        CancellationToken cancellationToken = default)
    {
        var patient = await patients.GetByIdAsync(tenantId, patientId, cancellationToken);
        if (patient is null)
        {
            throw new KeyNotFoundException("Patient not found.");
        }

        return await encounters.ListHistoryByPatientAsync(tenantId, patientId, cancellationToken);
    }

    public async Task<EncounterPipelineResultDto> CreateVisitAndDocumentAsync(
        Guid tenantId,
        Guid patientId,
        CreatePatientVisitRequest request,
        CancellationToken cancellationToken = default)
    {
        var patient = await patients.GetByIdAsync(tenantId, patientId, cancellationToken);
        if (patient is null)
        {
            throw new KeyNotFoundException("Patient not found.");
        }

        var attendingId = documentationOptions.Value.DefaultAttendingProviderUserId;
        if (attendingId == Guid.Empty)
        {
            throw new InvalidOperationException(
                "Documentation:DefaultAttendingProviderUserId is not configured.");
        }

        var occurredAt = request.OccurredAt;
        var title = string.IsNullOrWhiteSpace(request.Title)
            ? EncounterTitleGenerator.FromConversation(request.RawConversation)
            : request.Title.Trim();

        var now = DateTimeOffset.UtcNow;
        var encounter = new Encounter
        {
            Id = Guid.NewGuid(),
            TenantId = patient.TenantId,
            PatientId = patient.Id,
            FacilityId = patient.FacilityId,
            AttendingProviderUserId = attendingId,
            ServiceDate = DateOnly.FromDateTime(occurredAt.LocalDateTime),
            ScheduledTime = TimeOnly.FromDateTime(occurredAt.LocalDateTime),
            Status = EncounterStatus.InProgress,
            Type = EncounterType.FollowUp,
            ChiefComplaint = title,
            CreatedAt = now,
            UpdatedAt = now,
        };

        await encounters.AddAsync(encounter, cancellationToken);

        return await encounterPipeline.RunAsync(
            new EncounterPipelineRequest
            {
                TenantId = patient.TenantId,
                EncounterId = encounter.Id,
                RawConversation = request.RawConversation,
            },
            cancellationToken);
    }
}
