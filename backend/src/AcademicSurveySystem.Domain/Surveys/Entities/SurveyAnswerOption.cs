using AcademicSurveySystem.Domain.Common;

namespace AcademicSurveySystem.Domain.Surveys.Entities;

public sealed class SurveyAnswerOption
{
    private SurveyAnswerOption()
    {
    }

    public SurveyAnswerOption(
        Guid surveyAnswerId,
        Guid surveyQuestionOptionId)
    {
        EnsureRequired(surveyAnswerId, nameof(SurveyAnswerId));
        EnsureRequired(surveyQuestionOptionId, nameof(SurveyQuestionOptionId));

        SurveyAnswerId = surveyAnswerId;
        SurveyQuestionOptionId = surveyQuestionOptionId;
    }

    public Guid SurveyAnswerId { get; private set; }
    public Guid SurveyQuestionOptionId { get; private set; }
    public SurveyAnswer SurveyAnswer { get; private set; } = null!;
    public SurveyQuestionOption SurveyQuestionOption { get; private set; } = null!;

    private static void EnsureRequired(Guid value, string fieldName)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException($"{fieldName} is required.");
        }
    }
}
