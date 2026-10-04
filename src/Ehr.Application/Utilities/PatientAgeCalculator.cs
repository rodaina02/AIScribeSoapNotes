namespace Ehr.Application.Utilities;

public static class PatientAgeCalculator
{
    public static int Years(DateOnly dateOfBirth, DateOnly asOfDate)
    {
        var years = asOfDate.Year - dateOfBirth.Year;
        if (dateOfBirth > asOfDate.AddYears(-years))
        {
            years--;
        }

        return Math.Max(0, years);
    }
}
