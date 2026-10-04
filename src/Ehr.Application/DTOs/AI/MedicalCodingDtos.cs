namespace Ehr.Application.DTOs.AI;

public sealed class MedicalCodingRequest
{
    public required string VisitType { get; init; }
    public required string ChiefComplaint { get; init; }
    public required string ServiceDate { get; init; }
    public string FacilityType { get; init; } = "Skilled Nursing Facility (SNF)";
    public string ClinicalNoteSoap { get; init; } = "No clinical note available";
    public string ProceduresNarrative { get; init; } = "None";
    public string PrescriptionsNarrative { get; init; } = "None";
}

public sealed class MedicalCodingResult
{
    public IReadOnlyList<MedicalCodingLineItem> LineItems { get; init; } = [];
    public required string PrimaryIcd10 { get; init; }
    public string AdditionalNotes { get; init; } = string.Empty;
}

public sealed class MedicalCodingLineItem
{
    public required string CptCode { get; init; }
    public IReadOnlyList<string> Icd10Codes { get; init; } = [];
    public int Quantity { get; init; } = 1;
    public string Modifier1 { get; init; } = string.Empty;
    public string Modifier2 { get; init; } = string.Empty;
    public string Reasoning { get; init; } = string.Empty;
}

public sealed class MedicalCodingWorkflowInput
{
    public required string VisitTypeDisplay { get; init; }
    public string? ChiefComplaint { get; init; }
    public required DateOnly ServiceDate { get; init; }
    public string? ClinicalSoapDocument { get; init; }
    public string? ProceduresNarrative { get; init; }
    public string? PrescriptionsNarrative { get; init; }
}
