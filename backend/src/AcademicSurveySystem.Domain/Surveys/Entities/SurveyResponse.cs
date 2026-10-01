using AcademicSurveySystem.Domain.Common;

namespace AcademicSurveySystem.Domain.Surveys.Entities;

public sealed class SurveyResponse
{
    private readonly List<SurveyAnswer> _answers = [];

    private SurveyResponse()
    {
    }

    public SurveyResponse(
        Guid id,
        Guid surveySessionId,
        Guid surveyId,
        DateTimeOffset submittedAtUtc)
    {
        EnsureRequired(id, nameof(Id));
        EnsureRequired(surveySessionId, nameof(SurveySessionId));
        EnsureRequired(surveyId, nameof(SurveyId));
        EnsureUtc(submittedAtUtc, nameof(submittedAtUtc));

        Id = id;
        SurveySessionId = surveySessionId;
        SurveyId = surveyId;
        SubmittedAtUtc = submittedAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid SurveySessionId { get; private set; }
    public Guid SurveyId { get; private set; }
    public DateTimeOffset SubmittedAtUtc { get; private set; }
    public SurveySession SurveySession { get; private set; } = null!;
    public Survey Survey { get; private set; } = null!;
    public IReadOnlyCollection<SurveyAnswer> Answers => _answers.AsReadOnly();

    public void AddAnswer(SurveyAnswer answer)
    {
        if (answer.SurveyResponseId != Id)
        {
            throw new DomainException("Answer belongs to a different survey response.");
        }

        if (_answers.Any(item => item.SurveyQuestionId == answer.SurveyQuestionId))
        {
            throw new DomainException("Question answer cannot be duplicated.");
        }

        _answers.Add(answer);
    }

    private static void EnsureRequired(Guid value, string fieldName)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException($"{fieldName} is required.");
        }
    }

    private static void EnsureUtc(DateTimeOffset value, string fieldName)
    {
        if (value.Offset != TimeSpan.Zero)
        {
            throw new DomainException($"{fieldName} must be UTC.");
        }
    }
}
