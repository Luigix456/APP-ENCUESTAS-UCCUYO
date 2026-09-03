using AcademicSurveySystem.Application.Common.Results;
using AcademicSurveySystem.Domain.Academic.Enums;

namespace AcademicSurveySystem.Application.Academic.Subjects;

public sealed record UpdateSubjectRequest(
    string? Name,
    int? Year,
    string? Period)
{
    public IReadOnlyCollection<ApplicationError> Validate()
    {
        var errors = new List<ApplicationError>();

        if (string.IsNullOrWhiteSpace(Name))
        {
            errors.Add(new ApplicationError("Subject.NameRequired", "Name is required."));
        }

        if (Year is null)
        {
            errors.Add(new ApplicationError("Subject.YearRequired", "Year is required."));
        }
        else if (Year is < 1 or > 10)
        {
            errors.Add(new ApplicationError("Subject.YearInvalid", "Year must be between 1 and 10."));
        }

        if (string.IsNullOrWhiteSpace(Period))
        {
            errors.Add(new ApplicationError("Subject.PeriodRequired", "Period is required."));
        }
        else if (!Enum.TryParse<SubjectPeriod>(Period.Trim(), ignoreCase: true, out _))
        {
            errors.Add(new ApplicationError("Subject.PeriodInvalid", "Period is invalid."));
        }

        return errors;
    }
}
