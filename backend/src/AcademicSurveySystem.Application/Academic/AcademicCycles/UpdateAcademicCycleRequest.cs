using AcademicSurveySystem.Application.Common.Results;
using AcademicSurveySystem.Domain.Academic.Enums;

namespace AcademicSurveySystem.Application.Academic.AcademicCycles;

public sealed record UpdateAcademicCycleRequest(
    string? Period,
    DateOnly? StartDate,
    DateOnly? EndDate)
{
    public IReadOnlyCollection<ApplicationError> Validate()
    {
        var errors = new List<ApplicationError>();

        if (string.IsNullOrWhiteSpace(Period))
        {
            errors.Add(new ApplicationError("AcademicCycle.PeriodRequired", "Period is required."));
        }
        else if (!Enum.TryParse<AcademicCyclePeriod>(Period.Trim(), ignoreCase: true, out _))
        {
            errors.Add(new ApplicationError("AcademicCycle.PeriodInvalid", "Period is invalid."));
        }

        if (StartDate is null)
        {
            errors.Add(new ApplicationError("AcademicCycle.StartDateRequired", "StartDate is required."));
        }

        if (EndDate is null)
        {
            errors.Add(new ApplicationError("AcademicCycle.EndDateRequired", "EndDate is required."));
        }

        if (StartDate is not null && EndDate is not null && StartDate > EndDate)
        {
            errors.Add(new ApplicationError(
                "AcademicCycle.DateRangeInvalid",
                "StartDate must be less than or equal to EndDate."));
        }

        return errors;
    }
}
