using AcademicSurveySystem.Application.Common.Results;

namespace AcademicSurveySystem.Application.Identity.UserCareers;

public interface IUserCareerService
{
    Task<ApplicationResult<IReadOnlyCollection<UserCareerDto>>> GetUserCareersAsync(
        Guid userId,
        CancellationToken cancellationToken);

    Task<ApplicationResult<IReadOnlyCollection<UserCareerDto>>> ReplaceUserCareersAsync(
        Guid userId,
        UpdateUserCareersRequest request,
        CancellationToken cancellationToken);
}
