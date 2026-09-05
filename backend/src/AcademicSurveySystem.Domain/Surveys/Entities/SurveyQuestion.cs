using AcademicSurveySystem.Domain.Common;
using AcademicSurveySystem.Domain.Surveys.Enums;

namespace AcademicSurveySystem.Domain.Surveys.Entities;

public sealed class SurveyQuestion
{
    private readonly List<SurveyQuestionOption> _options = [];
    private readonly List<SurveyMatrixRow> _matrixRows = [];

    private SurveyQuestion()
    {
        Text = null!;
    }

    public SurveyQuestion(
        Guid id,
        Guid surveySectionId,
        string text,
        SurveyQuestionType type,
        bool isRequired,
        bool allowsComment,
        bool allowsOtherOption,
        int order,
        DateTimeOffset createdAtUtc)
    {
        if (id == Guid.Empty)
        {
            throw new DomainException("SurveyQuestion id is required.");
        }

        if (surveySectionId == Guid.Empty)
        {
            throw new DomainException("SurveySectionId is required.");
        }

        EnsureDefined(type, nameof(Type));
        EnsureOrder(order);
        EnsureUtc(createdAtUtc, nameof(createdAtUtc));
        EnsureAllowsOtherOption(type, allowsOtherOption);

        Id = id;
        SurveySectionId = surveySectionId;
        Text = NormalizeRequiredText(text, nameof(Text), 1000);
        Type = type;
        IsRequired = isRequired;
        AllowsComment = allowsComment;
        AllowsOtherOption = allowsOtherOption;
        Order = order;
        IsActive = true;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid SurveySectionId { get; private set; }
    public string Text { get; private set; }
    public SurveyQuestionType Type { get; private set; }
    public bool IsRequired { get; private set; }
    public bool AllowsComment { get; private set; }
    public bool AllowsOtherOption { get; private set; }
    public int Order { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public SurveySection SurveySection { get; private set; } = null!;
    public IReadOnlyCollection<SurveyQuestionOption> Options => _options.AsReadOnly();
    public IReadOnlyCollection<SurveyMatrixRow> MatrixRows => _matrixRows.AsReadOnly();

    public void Update(
        string text,
        SurveyQuestionType type,
        bool isRequired,
        bool allowsComment,
        bool allowsOtherOption,
        int order,
        DateTimeOffset updatedAtUtc)
    {
        EnsureDefined(type, nameof(Type));
        EnsureOrder(order);
        EnsureUtc(updatedAtUtc, nameof(updatedAtUtc));
        EnsureAllowsOtherOption(type, allowsOtherOption);
        EnsureCanUseType(type);

        Text = NormalizeRequiredText(text, nameof(Text), 1000);
        Type = type;
        IsRequired = isRequired;
        AllowsComment = allowsComment;
        AllowsOtherOption = allowsOtherOption;
        Order = order;
        UpdatedAtUtc = updatedAtUtc;
    }

    public void Activate(DateTimeOffset updatedAtUtc)
    {
        ChangeActiveState(true, updatedAtUtc);
    }

    public void Deactivate(DateTimeOffset updatedAtUtc)
    {
        ChangeActiveState(false, updatedAtUtc);
    }

    public void AddOption(SurveyQuestionOption option, DateTimeOffset updatedAtUtc)
    {
        if (option.SurveyQuestionId != Id)
        {
            throw new DomainException("Option belongs to a different question.");
        }

        if (!CanHaveOptions(Type))
        {
            throw new DomainException("Question type does not allow manual options.");
        }

        if (_options.Any(item => item.Order == option.Order))
        {
            throw new DomainException("Option order cannot be duplicated.");
        }

        EnsureUtc(updatedAtUtc, nameof(updatedAtUtc));

        _options.Add(option);
        UpdatedAtUtc = updatedAtUtc;
    }

    public void DeactivateOption(Guid optionId, DateTimeOffset updatedAtUtc)
    {
        EnsureUtc(updatedAtUtc, nameof(updatedAtUtc));

        var option = _options.SingleOrDefault(item => item.Id == optionId);

        if (option is null)
        {
            throw new DomainException("Option was not found.");
        }

        option.Deactivate(updatedAtUtc);
        UpdatedAtUtc = updatedAtUtc;
    }

    public void AddMatrixRow(SurveyMatrixRow matrixRow, DateTimeOffset updatedAtUtc)
    {
        if (matrixRow.SurveyQuestionId != Id)
        {
            throw new DomainException("Matrix row belongs to a different question.");
        }

        if (Type != SurveyQuestionType.MatrixSingleChoice)
        {
            throw new DomainException("Only matrix single choice questions allow matrix rows.");
        }

        if (_matrixRows.Any(item => item.Order == matrixRow.Order))
        {
            throw new DomainException("Matrix row order cannot be duplicated.");
        }

        EnsureUtc(updatedAtUtc, nameof(updatedAtUtc));

        _matrixRows.Add(matrixRow);
        UpdatedAtUtc = updatedAtUtc;
    }

    public void DeactivateMatrixRow(Guid matrixRowId, DateTimeOffset updatedAtUtc)
    {
        EnsureUtc(updatedAtUtc, nameof(updatedAtUtc));

        var matrixRow = _matrixRows.SingleOrDefault(item => item.Id == matrixRowId);

        if (matrixRow is null)
        {
            throw new DomainException("Matrix row was not found.");
        }

        matrixRow.Deactivate(updatedAtUtc);
        UpdatedAtUtc = updatedAtUtc;
    }

    private void ChangeActiveState(bool isActive, DateTimeOffset updatedAtUtc)
    {
        EnsureUtc(updatedAtUtc, nameof(updatedAtUtc));

        IsActive = isActive;
        UpdatedAtUtc = updatedAtUtc;
    }

    private void EnsureCanUseType(SurveyQuestionType type)
    {
        if (!CanHaveOptions(type) && _options.Any(item => item.IsActive))
        {
            throw new DomainException("Question type does not allow active manual options.");
        }

        if (type != SurveyQuestionType.MatrixSingleChoice && _matrixRows.Any(item => item.IsActive))
        {
            throw new DomainException("Question type does not allow active matrix rows.");
        }
    }

    private static bool CanHaveOptions(SurveyQuestionType type)
    {
        return type is SurveyQuestionType.SingleChoice
            or SurveyQuestionType.MultipleChoice
            or SurveyQuestionType.MatrixSingleChoice;
    }

    private static void EnsureAllowsOtherOption(
        SurveyQuestionType type,
        bool allowsOtherOption)
    {
        if (allowsOtherOption
            && type is not SurveyQuestionType.SingleChoice
                and not SurveyQuestionType.MultipleChoice)
        {
            throw new DomainException(
                "AllowsOtherOption is only valid for single choice or multiple choice questions.");
        }
    }

    private static void EnsureOrder(int order)
    {
        if (order <= 0)
        {
            throw new DomainException("Order must be greater than zero.");
        }
    }

    private static string NormalizeRequiredText(string value, string fieldName, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException($"{fieldName} is required.");
        }

        var normalized = value.Trim();

        if (normalized.Length > maxLength)
        {
            throw new DomainException($"{fieldName} must be {maxLength} characters or fewer.");
        }

        return normalized;
    }

    private static void EnsureDefined(SurveyQuestionType value, string fieldName)
    {
        if (!Enum.IsDefined(value))
        {
            throw new DomainException($"{fieldName} is invalid.");
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
