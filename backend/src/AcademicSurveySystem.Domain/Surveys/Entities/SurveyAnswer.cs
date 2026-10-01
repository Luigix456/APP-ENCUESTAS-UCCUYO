using AcademicSurveySystem.Domain.Common;

namespace AcademicSurveySystem.Domain.Surveys.Entities;

public sealed class SurveyAnswer
{
    private readonly List<SurveyAnswerOption> _selectedOptions = [];
    private readonly List<SurveyMatrixAnswer> _matrixAnswers = [];

    private SurveyAnswer()
    {
    }

    public SurveyAnswer(
        Guid id,
        Guid surveyResponseId,
        Guid surveyQuestionId,
        string? textValue,
        int? numericValue,
        string? comment,
        string? otherText = null)
    {
        EnsureRequired(id, nameof(Id));
        EnsureRequired(surveyResponseId, nameof(SurveyResponseId));
        EnsureRequired(surveyQuestionId, nameof(SurveyQuestionId));

        Id = id;
        SurveyResponseId = surveyResponseId;
        SurveyQuestionId = surveyQuestionId;
        TextValue = NormalizeOptionalText(textValue, nameof(TextValue), 4000);
        NumericValue = numericValue;
        Comment = NormalizeOptionalText(comment, nameof(Comment), 1000);
        OtherText = NormalizeOptionalText(otherText, nameof(OtherText), 1000);
    }

    public Guid Id { get; private set; }
    public Guid SurveyResponseId { get; private set; }
    public Guid SurveyQuestionId { get; private set; }
    public string? TextValue { get; private set; }
    public int? NumericValue { get; private set; }
    public string? Comment { get; private set; }
    public string? OtherText { get; private set; }
    public SurveyResponse SurveyResponse { get; private set; } = null!;
    public SurveyQuestion SurveyQuestion { get; private set; } = null!;
    public IReadOnlyCollection<SurveyAnswerOption> SelectedOptions => _selectedOptions.AsReadOnly();
    public IReadOnlyCollection<SurveyMatrixAnswer> MatrixAnswers => _matrixAnswers.AsReadOnly();

    public void AddSelectedOption(SurveyAnswerOption selectedOption)
    {
        if (selectedOption.SurveyAnswerId != Id)
        {
            throw new DomainException("Selected option belongs to a different answer.");
        }

        if (_selectedOptions.Any(item => item.SurveyQuestionOptionId == selectedOption.SurveyQuestionOptionId))
        {
            throw new DomainException("Selected option cannot be duplicated.");
        }

        _selectedOptions.Add(selectedOption);
    }

    public void AddMatrixAnswer(SurveyMatrixAnswer matrixAnswer)
    {
        if (matrixAnswer.SurveyAnswerId != Id)
        {
            throw new DomainException("Matrix answer belongs to a different answer.");
        }

        if (_matrixAnswers.Any(item => item.SurveyMatrixRowId == matrixAnswer.SurveyMatrixRowId))
        {
            throw new DomainException("Matrix row answer cannot be duplicated.");
        }

        _matrixAnswers.Add(matrixAnswer);
    }

    private static void EnsureRequired(Guid value, string fieldName)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException($"{fieldName} is required.");
        }
    }

    private static string? NormalizeOptionalText(string? value, string fieldName, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();

        if (normalized.Length > maxLength)
        {
            throw new DomainException($"{fieldName} must be {maxLength} characters or fewer.");
        }

        return normalized;
    }
}
