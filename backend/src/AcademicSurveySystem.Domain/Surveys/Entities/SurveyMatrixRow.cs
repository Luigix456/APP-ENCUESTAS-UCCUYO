using AcademicSurveySystem.Domain.Common;

namespace AcademicSurveySystem.Domain.Surveys.Entities;

public sealed class SurveyMatrixRow
{
    private SurveyMatrixRow()
    {
        Text = null!;
    }

    public SurveyMatrixRow(
        Guid id,
        Guid surveyQuestionId,
        string text,
        int order,
        DateTimeOffset createdAtUtc)
    {
        if (id == Guid.Empty)
        {
            throw new DomainException("SurveyMatrixRow id is required.");
        }

        if (surveyQuestionId == Guid.Empty)
        {
            throw new DomainException("SurveyQuestionId is required.");
        }

        EnsureOrder(order);
        EnsureUtc(createdAtUtc, nameof(createdAtUtc));

        Id = id;
        SurveyQuestionId = surveyQuestionId;
        Text = NormalizeRequiredText(text, nameof(Text), 500);
        Order = order;
        IsActive = true;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid SurveyQuestionId { get; private set; }
    public string Text { get; private set; }
    public int Order { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public SurveyQuestion SurveyQuestion { get; private set; } = null!;

    public void Update(
        string text,
        int order,
        DateTimeOffset updatedAtUtc)
    {
        EnsureOrder(order);
        EnsureUtc(updatedAtUtc, nameof(updatedAtUtc));

        Text = NormalizeRequiredText(text, nameof(Text), 500);
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

    private static void EnsureUtc(DateTimeOffset value, string fieldName)
    {
        if (value.Offset != TimeSpan.Zero)
        {
            throw new DomainException($"{fieldName} must be UTC.");
        }
    }
}
