using AcademicSurveySystem.Domain.Common;

namespace AcademicSurveySystem.Domain.Surveys.Entities;

public sealed class SurveyMatrixAnswer
{
    private SurveyMatrixAnswer()
    {
    }

    public SurveyMatrixAnswer(
        Guid surveyAnswerId,
        Guid surveyMatrixRowId,
        Guid surveyQuestionOptionId)
    {
        EnsureRequired(surveyAnswerId, nameof(SurveyAnswerId));
        EnsureRequired(surveyMatrixRowId, nameof(SurveyMatrixRowId));
        EnsureRequired(surveyQuestionOptionId, nameof(SurveyQuestionOptionId));

        SurveyAnswerId = surveyAnswerId;
        SurveyMatrixRowId = surveyMatrixRowId;
        SurveyQuestionOptionId = surveyQuestionOptionId;
    }

    public Guid SurveyAnswerId { get; private set; }
    public Guid SurveyMatrixRowId { get; private set; }
    public Guid SurveyQuestionOptionId { get; private set; }
    public SurveyAnswer SurveyAnswer { get; private set; } = null!;
    public SurveyMatrixRow SurveyMatrixRow { get; private set; } = null!;
    public SurveyQuestionOption SurveyQuestionOption { get; private set; } = null!;

    private static void EnsureRequired(Guid value, string fieldName)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException($"{fieldName} is required.");
        }
    }
}
