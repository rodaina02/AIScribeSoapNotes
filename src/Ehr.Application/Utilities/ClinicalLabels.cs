using Ehr.Domain.Enums;

namespace Ehr.Application.Utilities;

public static class ClinicalLabels
{
    public static string FormatGender(Gender g) => g switch
    {
        Gender.Male => "Male",
        Gender.Female => "Female",
        Gender.Other => "Other",
        _ => "Unknown",
    };

    public static string VisitType(EncounterType t) => t switch
    {
        EncounterType.InitialConsult => "Initial Consultation",
        EncounterType.FollowUp => "Follow-up",
        EncounterType.Procedure => "Procedure Visit",
        EncounterType.ReEvaluation => "Re-evaluation",
        _ => t.ToString(),
    };
}
