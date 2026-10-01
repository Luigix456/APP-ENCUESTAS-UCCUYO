using AcademicSurveySystem.Application.Common.Results;

namespace AcademicSurveySystem.Application.Identity.UserCareers;

public sealed record UpdateUserCareersRequest(
    IReadOnlyCollection<Guid>? CareerIds)
{
    public IReadOnlyCollection<ApplicationError> Validate()
    {
        var errors = new List<ApplicationError>();

        if (CareerIds is null)
        {
            errors.Add(new ApplicationError(
                "UserCareer.CareerIdsRequired",
                "CareerIds are required."));

            return errors;
        }

        if (CareerIds.Any(careerId => careerId == Guid.Empty))
        {
            errors.Add(new ApplicationError(
                "UserCareer.CareerIdRequired",
                "CareerId is required."));
        }

        return errors;
    }
}
