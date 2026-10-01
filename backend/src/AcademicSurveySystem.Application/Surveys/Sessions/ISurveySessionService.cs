using AcademicSurveySystem.Application.Common.Results;

namespace AcademicSurveySystem.Application.Surveys.Sessions;

public interface ISurveySessionService
{
    Task<ApplicationResult<IReadOnlyCollection<SurveySessionDto>>> GetSessionsAsync(
        bool includeInactive,
        string? status,
        Guid? surveyAssignmentId,
        string? accessCode,
        CancellationToken cancellationToken);

    Task<ApplicationResult<SurveySessionDto>> GetSessionByIdAsync(
        Guid id,
        CancellationToken cancellationToken);

    Task<ApplicationResult<SurveySessionDto>> CreateSessionAsync(
        CreateSurveySessionRequest request,
        Guid createdByUserId,
        CancellationToken cancellationToken);

    Task<ApplicationResult> UpdateSessionAsync(
        Guid id,
        UpdateSurveySessionRequest request,
        CancellationToken cancellationToken);

    Task<ApplicationResult> OpenSessionAsync(
        Guid id,
        CancellationToken cancellationToken);

    Task<ApplicationResult> CloseSessionAsync(
        Guid id,
        CancellationToken cancellationToken);

    Task<ApplicationResult> CancelSessionAsync(
        Guid id,
        CancellationToken cancellationToken);

    Task<ApplicationResult> ActivateSessionAsync(
        Guid id,
        CancellationToken cancellationToken);

    Task<ApplicationResult> DeactivateSessionAsync(
        Guid id,
        CancellationToken cancellationToken);

    Task<ApplicationResult<PublicSurveySessionDto>> GetPublicSessionByAccessCodeAsync(
        string accessCode,
        CancellationToken cancellationToken);
}
