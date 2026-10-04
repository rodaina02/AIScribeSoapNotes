namespace Ehr.Domain.Enums;

/// <summary>How a code is used on the encounter (primary diagnosis, line item, etc.).</summary>
public enum EncounterMedicalCodeRole
{
    PrimaryDiagnosis = 0,
    SecondaryDiagnosis = 1,
    Procedure = 2,
    Supporting = 3
}
