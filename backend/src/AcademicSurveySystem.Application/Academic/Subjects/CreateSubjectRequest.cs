using AcademicSurveySystem.Application.Common.Results;
using AcademicSurveySystem.Domain.Academic.Enums;

namespace AcademicSurveySystem.Application.Academic.Subjects;

public sealed record CreateSubjectRequest(
    Guid? CareerId,
    string? Code,
    string? Name,
    int? Year,
    string? Period)
{
    public IReadOnlyCollection<ApplicationError> Validate()
    {
        var errors = new List<ApplicationError>();

        if (CareerId is null || CareerId == Guid.Empty)
        {
            errors.Add(new ApplicationError("Subject.CareerIdRequired", "CareerId is required."));
        }

        if (string.IsNullOrWhiteSpace(Code))
        {
            errors.Add(new ApplicationError("Subject.CodeRequired", "Code is required."));
        }

        if (string.IsNullOrWhiteSpace(Name))
        {
            errors.Add(new ApplicationError("Subject.NameRequired", "Name is required."));
        }

        ValidateYear(Year, errors);
        ValidatePeriod(Period, errors);

        return errors;
    }

    private static void ValidateYear(int? year, ICollection<ApplicationError> errors)
    {
        if (year is null)
        {
            errors.Add(new ApplicationError("Subject.YearRequired", "Year is required."));
            return;
        }

        if (year is < 1 or > 10)
        {
            errors.Add(new ApplicationError("Subject.YearInvalid", "Year must be between 1 and 10."));
        }
    }

    private static void ValidatePeriod(string? period, ICollection<ApplicationError> errors)
    {
        if (string.IsNullOrWhiteSpace(period))
        {
            errors.Add(new ApplicationError("Subject.PeriodRequired", "Period is required."));
            return;
        }

        if (!Enum.TryParse<SubjectPeriod>(period.Trim(), ignoreCase: true, out _))
        {
            errors.Add(new ApplicationError("Subject.PeriodInvalid", "Period is invalid."));
        }
    }
}
