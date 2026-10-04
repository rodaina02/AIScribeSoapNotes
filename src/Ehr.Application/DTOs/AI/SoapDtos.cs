namespace Ehr.Application.DTOs.AI;

/// <summary>Payload sent to <see cref="Abstractions.AI.IAIService"/> when generating a SOAP note.</summary>
public sealed class SoapGenerationRequest
{
    public required string PatientName { get; init; }
    public required int Age { get; init; }
    public required string Gender { get; init; }
    public required string DateOfBirth { get; init; }
    public required string VisitDate { get; init; }
    public required string VisitType { get; init; }
    public string FacilityType { get; init; } = "Skilled Nursing Facility (SNF)";
    public string ChiefComplaint { get; init; } = "Not specified";
    public string VisitNotes { get; init; } = "None";
    /// <summary>When null/empty, provider uses the manual-visit path (no transcript).</summary>
    public string? Transcript { get; init; }
    public string ProceduresNarrative { get; init; } = "No procedures documented";
    public string PrescriptionsNarrative { get; init; } = "No prescriptions written";
    public string PreviousVisitsNarrative { get; init; } = "N/A (Initial visit)";
}

public sealed class SoapGenerationResult
{
    public required string Subjective { get; init; }
    public required string Objective { get; init; }
    public required string Assessment { get; init; }
    public required string Plan { get; init; }
    public string? ChiefComplaint { get; init; }
}

/// <summary>Input for building a <see cref="SoapGenerationRequest"/> in the app layer (visit + patient + optional add-ons).</summary>
public sealed class SoapWorkflowInput
{
    public required string PatientLastName { get; init; }
    public required string PatientFirstName { get; init; }
    public required int Age { get; init; }
    public required string GenderDisplay { get; init; }
    public required DateOnly DateOfBirth { get; init; }
    public required DateOnly ServiceDate { get; init; }
    public required string VisitTypeDisplay { get; init; }
    public string? ChiefComplaint { get; init; }
    public string? VisitNotes { get; init; }
    public string? Transcript { get; init; }
    public string? ProceduresNarrative { get; init; }
    public string? PrescriptionsNarrative { get; init; }
    public string? PreviousVisitsNarrative { get; init; }
}
