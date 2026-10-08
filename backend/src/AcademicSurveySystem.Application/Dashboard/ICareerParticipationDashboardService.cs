using AcademicSurveySystem.Application.Common.Results;

namespace AcademicSurveySystem.Application.Dashboard;

public interface ICareerParticipationDashboardService
{
    Task<ApplicationResult<CareerParticipationDashboardDto>> GetCareerParticipationAsync(
        Guid careerId,
        Guid academicCycleId,
        CancellationToken cancellationToken);
}
