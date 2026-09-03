using System.Net.Mail;
using AcademicSurveySystem.Domain.Common;

namespace AcademicSurveySystem.Domain.Academic.Entities;

public sealed class Teacher
{
    private readonly List<TeacherSubjectAssignment> _teacherSubjectAssignments = [];

    private Teacher()
    {
        FirstName = null!;
        LastName = null!;
    }

    public Teacher(
        Guid id,
        string firstName,
        string lastName,
        string? email,
        DateTimeOffset createdAtUtc)
    {
        if (id == Guid.Empty)
        {
            throw new DomainException("Teacher id is required.");
        }

        EnsureUtc(createdAtUtc, nameof(createdAtUtc));

        Id = id;
        FirstName = NormalizeRequiredText(firstName, nameof(FirstName), 100);
        LastName = NormalizeRequiredText(lastName, nameof(LastName), 100);
        SetEmail(email);
        IsActive = true;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }
    public string FirstName { get; private set; }
    public string LastName { get; private set; }
    public string? Email { get; private set; }
    public string? NormalizedEmail { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public IReadOnlyCollection<TeacherSubjectAssignment> TeacherSubjectAssignments =>
        _teacherSubjectAssignments.AsReadOnly();

    public void UpdateName(
        string firstName,
        string lastName,
        DateTimeOffset updatedAtUtc)
    {
        EnsureUtc(updatedAtUtc, nameof(updatedAtUtc));

        FirstName = NormalizeRequiredText(firstName, nameof(FirstName), 100);
        LastName = NormalizeRequiredText(lastName, nameof(LastName), 100);
        UpdatedAtUtc = updatedAtUtc;
    }

    public void ChangeEmail(string email, DateTimeOffset updatedAtUtc)
    {
        EnsureUtc(updatedAtUtc, nameof(updatedAtUtc));

        SetEmail(email);
        UpdatedAtUtc = updatedAtUtc;
    }

    public void RemoveEmail(DateTimeOffset updatedAtUtc)
    {
        EnsureUtc(updatedAtUtc, nameof(updatedAtUtc));

        Email = null;
        NormalizedEmail = null;
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

    private void SetEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            Email = null;
            NormalizedEmail = null;
            return;
        }

        var normalized = NormalizeRequiredText(email, nameof(Email), 320).ToLowerInvariant();

        if (!IsValidEmail(normalized))
        {
            throw new DomainException("Email format is invalid.");
        }

        Email = normalized;
        NormalizedEmail = normalized.ToUpperInvariant();
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var address = new MailAddress(email);
            return address.Address == email;
        }
        catch (FormatException)
        {
            return false;
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
