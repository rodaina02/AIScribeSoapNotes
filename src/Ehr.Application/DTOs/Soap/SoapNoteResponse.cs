namespace Ehr.Application.DTOs.Soap;

public sealed class SoapNoteResponse
{
    public required Guid Id { get; init; }
    public required Guid TenantId { get; init; }
    public required Guid EncounterId { get; init; }
    public required string Subjective { get; init; }
    public required string Objective { get; init; }
    public required string Assessment { get; init; }
    public required string Plan { get; init; }
    public string? ChiefComplaintNarrative { get; init; }
    public required bool IsSigned { get; init; }
}
