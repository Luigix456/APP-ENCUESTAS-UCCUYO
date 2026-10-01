using AcademicSurveySystem.Application.Common.Results;
using AcademicSurveySystem.Application.Surveys.Dtos;
using AcademicSurveySystem.Application.Surveys.Requests;

namespace AcademicSurveySystem.Application.Surveys;

public interface ISurveyTemplateService
{
    Task<ApplicationResult<IReadOnlyCollection<SurveySummaryDto>>> GetSurveysAsync(
        bool includeInactive,
        string? status,
        string? target,
        CancellationToken cancellationToken);

    Task<ApplicationResult<SurveyDetailDto>> GetSurveyByIdAsync(
        Guid id,
        CancellationToken cancellationToken);

    Task<ApplicationResult<SurveyDetailDto>> CreateSurveyAsync(
        Guid createdByUserId,
        CreateSurveyRequest request,
        CancellationToken cancellationToken);

    Task<ApplicationResult<SurveyEditableVersionDto>> GetOrCreateEditableVersionAsync(
        Guid surveyId,
        Guid currentUserId,
        CancellationToken cancellationToken);

    Task<ApplicationResult> UpdateSurveyAsync(
        Guid id,
        UpdateSurveyRequest request,
        CancellationToken cancellationToken);

    Task<ApplicationResult> PublishSurveyAsync(Guid id, CancellationToken cancellationToken);

    Task<ApplicationResult> ArchiveSurveyAsync(Guid id, CancellationToken cancellationToken);

    Task<ApplicationResult> ActivateSurveyAsync(Guid id, CancellationToken cancellationToken);

    Task<ApplicationResult> DeactivateSurveyAsync(Guid id, CancellationToken cancellationToken);

    Task<ApplicationResult<SurveyDetailDto>> AddSectionAsync(
        Guid surveyId,
        CreateSurveySectionRequest request,
        CancellationToken cancellationToken);

    Task<ApplicationResult> UpdateSectionAsync(
        Guid surveyId,
        Guid sectionId,
        UpdateSurveySectionRequest request,
        CancellationToken cancellationToken);

    Task<ApplicationResult> ActivateSectionAsync(Guid surveyId, Guid sectionId, CancellationToken cancellationToken);

    Task<ApplicationResult> DeactivateSectionAsync(Guid surveyId, Guid sectionId, CancellationToken cancellationToken);

    Task<ApplicationResult<SurveyDetailDto>> AddQuestionAsync(
        Guid surveyId,
        Guid sectionId,
        CreateSurveyQuestionRequest request,
        CancellationToken cancellationToken);

    Task<ApplicationResult> UpdateQuestionAsync(
        Guid surveyId,
        Guid sectionId,
        Guid questionId,
        UpdateSurveyQuestionRequest request,
        CancellationToken cancellationToken);

    Task<ApplicationResult> ActivateQuestionAsync(
        Guid surveyId,
        Guid sectionId,
        Guid questionId,
        CancellationToken cancellationToken);

    Task<ApplicationResult> DeactivateQuestionAsync(
        Guid surveyId,
        Guid sectionId,
        Guid questionId,
        CancellationToken cancellationToken);

    Task<ApplicationResult<SurveyDetailDto>> AddOptionAsync(
        Guid surveyId,
        Guid sectionId,
        Guid questionId,
        CreateSurveyQuestionOptionRequest request,
        CancellationToken cancellationToken);

    Task<ApplicationResult<SurveyDetailDto>> UpdateOptionAsync(
        Guid surveyId,
        Guid sectionId,
        Guid questionId,
        Guid optionId,
        UpdateSurveyQuestionOptionRequest request,
        CancellationToken cancellationToken);

    Task<ApplicationResult> ActivateOptionAsync(
        Guid surveyId,
        Guid sectionId,
        Guid questionId,
        Guid optionId,
        CancellationToken cancellationToken);

    Task<ApplicationResult> DeactivateOptionAsync(
        Guid surveyId,
        Guid sectionId,
        Guid questionId,
        Guid optionId,
        CancellationToken cancellationToken);

    Task<ApplicationResult<SurveyDetailDto>> AddMatrixRowAsync(
        Guid surveyId,
        Guid sectionId,
        Guid questionId,
        CreateSurveyMatrixRowRequest request,
        CancellationToken cancellationToken);

    Task<ApplicationResult<SurveyDetailDto>> UpdateMatrixRowAsync(
        Guid surveyId,
        Guid sectionId,
        Guid questionId,
        Guid rowId,
        UpdateSurveyMatrixRowRequest request,
        CancellationToken cancellationToken);

    Task<ApplicationResult> ActivateMatrixRowAsync(
        Guid surveyId,
        Guid sectionId,
        Guid questionId,
        Guid rowId,
        CancellationToken cancellationToken);

    Task<ApplicationResult> DeactivateMatrixRowAsync(
        Guid surveyId,
        Guid sectionId,
        Guid questionId,
        Guid rowId,
        CancellationToken cancellationToken);
}
