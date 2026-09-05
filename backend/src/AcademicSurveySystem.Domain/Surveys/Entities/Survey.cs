using AcademicSurveySystem.Domain.Common;
using AcademicSurveySystem.Domain.Surveys.Enums;

namespace AcademicSurveySystem.Domain.Surveys.Entities;

public sealed class Survey
{
    private readonly List<SurveySection> _sections = [];

    private Survey()
    {
        Title = null!;
    }

    public Survey(
        Guid id,
        Guid createdByUserId,
        string title,
        string? description,
        SurveyTarget target,
        DateTimeOffset createdAtUtc)
    {
        if (id == Guid.Empty)
        {
            throw new DomainException("Survey id is required.");
        }

        if (createdByUserId == Guid.Empty)
        {
            throw new DomainException("CreatedByUserId is required.");
        }

        EnsureDefined(target, nameof(Target));
        EnsureUtc(createdAtUtc, nameof(createdAtUtc));

        Id = id;
        CreatedByUserId = createdByUserId;
        Title = NormalizeRequiredText(title, nameof(Title), 200);
        Description = NormalizeOptionalText(description, nameof(Description), 1000);
        Target = target;
        Status = SurveyStatus.Draft;
        IsAnonymous = true;
        IsActive = true;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid CreatedByUserId { get; private set; }
    public string Title { get; private set; }
    public string? Description { get; private set; }
    public SurveyTarget Target { get; private set; }
    public SurveyStatus Status { get; private set; }
    public bool IsAnonymous { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public IReadOnlyCollection<SurveySection> Sections => _sections.AsReadOnly();

    public void Update(
        string title,
        string? description,
        SurveyTarget target,
        bool isAnonymous,
        DateTimeOffset updatedAtUtc)
    {
        EnsureDefined(target, nameof(Target));
        EnsureUtc(updatedAtUtc, nameof(updatedAtUtc));

        Title = NormalizeRequiredText(title, nameof(Title), 200);
        Description = NormalizeOptionalText(description, nameof(Description), 1000);
        Target = target;
        IsAnonymous = isAnonymous;
        UpdatedAtUtc = updatedAtUtc;
    }

    public void Publish(DateTimeOffset updatedAtUtc)
    {
        EnsureCanPublish();
        ChangeStatus(SurveyStatus.Published, updatedAtUtc);
    }

    public void Archive(DateTimeOffset updatedAtUtc)
    {
        ChangeStatus(SurveyStatus.Archived, updatedAtUtc);
    }

    public void Activate(DateTimeOffset updatedAtUtc)
    {
        ChangeActiveState(true, updatedAtUtc);
    }

    public void Deactivate(DateTimeOffset updatedAtUtc)
    {
        ChangeActiveState(false, updatedAtUtc);
    }

    public void AddSection(SurveySection section, DateTimeOffset updatedAtUtc)
    {
        if (section.SurveyId != Id)
        {
            throw new DomainException("Section belongs to a different survey.");
        }

        if (_sections.Any(item => item.Order == section.Order))
        {
            throw new DomainException("Section order cannot be duplicated.");
        }

        EnsureUtc(updatedAtUtc, nameof(updatedAtUtc));

        _sections.Add(section);
        UpdatedAtUtc = updatedAtUtc;
    }

    public void DeactivateSection(Guid sectionId, DateTimeOffset updatedAtUtc)
    {
        EnsureUtc(updatedAtUtc, nameof(updatedAtUtc));

        var section = _sections.SingleOrDefault(item => item.Id == sectionId);

        if (section is null)
        {
            throw new DomainException("Section was not found.");
        }

        section.Deactivate(updatedAtUtc);
        UpdatedAtUtc = updatedAtUtc;
    }

    private void ChangeStatus(SurveyStatus status, DateTimeOffset updatedAtUtc)
    {
        EnsureDefined(status, nameof(Status));
        EnsureUtc(updatedAtUtc, nameof(updatedAtUtc));

        Status = status;
        UpdatedAtUtc = updatedAtUtc;
    }

    private void ChangeActiveState(bool isActive, DateTimeOffset updatedAtUtc)
    {
        EnsureUtc(updatedAtUtc, nameof(updatedAtUtc));

        IsActive = isActive;
        UpdatedAtUtc = updatedAtUtc;
    }

    private void EnsureCanPublish()
    {
        if (Status == SurveyStatus.Archived)
        {
            throw new DomainException("Archived surveys cannot be published.");
        }

        var activeSections = _sections.Where(section => section.IsActive).ToArray();

        if (activeSections.Length == 0)
        {
            throw new DomainException("Survey must have at least one active section.");
        }

        foreach (var section in activeSections)
        {
            var activeQuestions = section.Questions.Where(question => question.IsActive).ToArray();

            if (activeQuestions.Length == 0)
            {
                throw new DomainException("Every active section must have at least one active question.");
            }

            foreach (var question in activeQuestions)
            {
                EnsureQuestionCanBePublished(question);
            }
        }
    }

    private static void EnsureQuestionCanBePublished(SurveyQuestion question)
    {
        var activeOptionsCount = question.Options.Count(option => option.IsActive);

        if (question.Type is SurveyQuestionType.SingleChoice or SurveyQuestionType.MultipleChoice
            && activeOptionsCount < 2)
        {
            throw new DomainException("Choice questions must have at least two active options.");
        }

        if (question.Type == SurveyQuestionType.MatrixSingleChoice)
        {
            if (activeOptionsCount < 2)
            {
                throw new DomainException("Matrix single choice questions must have at least two active options.");
            }

            if (!question.MatrixRows.Any(matrixRow => matrixRow.IsActive))
            {
                throw new DomainException("Matrix single choice questions must have at least one active row.");
            }
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

    private static void EnsureDefined<TEnum>(TEnum value, string fieldName)
        where TEnum : struct, Enum
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
