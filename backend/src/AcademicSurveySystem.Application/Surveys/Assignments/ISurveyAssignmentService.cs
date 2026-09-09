using AcademicSurveySystem.Application.Common.Results;

namespace AcademicSurveySystem.Application.Surveys.Assignments;

public interface ISurveyAssignmentService
{
    Task<ApplicationResult<IReadOnlyCollection<SurveyAssignmentDto>>> GetAssignmentsAsync(
        bool includeInactive,
        Guid? surveyId,
        Guid? careerId,
        Guid? subjectId,
        Guid? academicCycleId,
        Guid? teacherId,
        CancellationToken cancellationToken);

    Task<ApplicationResult<SurveyAssignmentDto>> GetAssignmentByIdAsync(
        Guid id,
        CancellationToken cancellationToken);

    Task<ApplicationResult<SurveyAssignmentDto>> CreateAssignmentAsync(
        CreateSurveyAssignmentRequest request,
        CancellationToken cancellationToken);

    Task<ApplicationResult> ActivateAssignmentAsync(
        Guid id,
        CancellationToken cancellationToken);

    Task<ApplicationResult> DeactivateAssignmentAsync(
        Guid id,
        CancellationToken cancellationToken);
}
