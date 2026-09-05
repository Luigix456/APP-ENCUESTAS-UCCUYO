using AcademicSurveySystem.Domain.Common;

namespace AcademicSurveySystem.Domain.Surveys.Entities;

public sealed class SurveySection
{
    private readonly List<SurveyQuestion> _questions = [];

    private SurveySection()
    {
        Title = null!;
    }

    public SurveySection(
        Guid id,
        Guid surveyId,
        string title,
        string? description,
        int order,
        DateTimeOffset createdAtUtc)
    {
        if (id == Guid.Empty)
        {
            throw new DomainException("SurveySection id is required.");
        }

        if (surveyId == Guid.Empty)
        {
            throw new DomainException("SurveyId is required.");
        }

        EnsureOrder(order);
        EnsureUtc(createdAtUtc, nameof(createdAtUtc));

        Id = id;
        SurveyId = surveyId;
        Title = NormalizeRequiredText(title, nameof(Title), 200);
        Description = NormalizeOptionalText(description, nameof(Description), 1000);
        Order = order;
        IsActive = true;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid SurveyId { get; private set; }
    public string Title { get; private set; }
    public string? Description { get; private set; }
    public int Order { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public Survey Survey { get; private set; } = null!;
    public IReadOnlyCollection<SurveyQuestion> Questions => _questions.AsReadOnly();

    public void Update(
        string title,
        string? description,
        int order,
        DateTimeOffset updatedAtUtc)
    {
        EnsureOrder(order);
        EnsureUtc(updatedAtUtc, nameof(updatedAtUtc));

        Title = NormalizeRequiredText(title, nameof(Title), 200);
        Description = NormalizeOptionalText(description, nameof(Description), 1000);
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

    public void AddQuestion(SurveyQuestion question, DateTimeOffset updatedAtUtc)
    {
        if (question.SurveySectionId != Id)
        {
            throw new DomainException("Question belongs to a different section.");
        }

        if (_questions.Any(item => item.Order == question.Order))
        {
            throw new DomainException("Question order cannot be duplicated.");
        }

        EnsureUtc(updatedAtUtc, nameof(updatedAtUtc));

        _questions.Add(question);
        UpdatedAtUtc = updatedAtUtc;
    }

    public void DeactivateQuestion(Guid questionId, DateTimeOffset updatedAtUtc)
    {
        EnsureUtc(updatedAtUtc, nameof(updatedAtUtc));

        var question = _questions.SingleOrDefault(item => item.Id == questionId);

        if (question is null)
        {
            throw new DomainException("Question was not found.");
        }

        question.Deactivate(updatedAtUtc);
        UpdatedAtUtc = updatedAtUtc;
    }

    private void ChangeActiveState(bool isActive, DateTimeOffset updatedAtUtc)
    {
        EnsureUtc(updatedAtUtc, nameof(updatedAtUtc));

        IsActive = isActive;
        UpdatedAtUtc = updatedAtUtc;
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

    private static void EnsureUtc(DateTimeOffset value, string fieldName)
    {
        if (value.Offset != TimeSpan.Zero)
        {
            throw new DomainException($"{fieldName} must be UTC.");
        }
    }
}
