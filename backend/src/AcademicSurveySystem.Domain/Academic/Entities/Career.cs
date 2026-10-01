using System.Text.RegularExpressions;
using AcademicSurveySystem.Domain.Academic.Enums;
using AcademicSurveySystem.Domain.Common;
using AcademicSurveySystem.Domain.Identity.Entities;

namespace AcademicSurveySystem.Domain.Academic.Entities;

public sealed partial class Career
{
    private readonly List<Subject> _subjects = [];
    private readonly List<UserCareer> _userCareers = [];

    private Career()
    {
        Code = null!;
        Name = null!;
    }

    public Career(
        Guid id,
        string code,
        string name,
        CareerType type,
        DateTimeOffset createdAtUtc)
    {
        if (id == Guid.Empty)
        {
            throw new DomainException("Career id is required.");
        }

        EnsureUtc(createdAtUtc, nameof(createdAtUtc));
        EnsureDefined(type, nameof(Type));

        Id = id;
        Code = NormalizeCode(code, nameof(Code));
        Name = NormalizeRequiredText(name, nameof(Name), 200);
        Type = type;
        IsActive = true;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }
    public string Code { get; private set; }
    public string Name { get; private set; }
    public CareerType Type { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public IReadOnlyCollection<Subject> Subjects => _subjects.AsReadOnly();
    public IReadOnlyCollection<UserCareer> UserCareers => _userCareers.AsReadOnly();

    public void UpdateName(string name, DateTimeOffset updatedAtUtc)
    {
        EnsureUtc(updatedAtUtc, nameof(updatedAtUtc));

        Name = NormalizeRequiredText(name, nameof(Name), 200);
        UpdatedAtUtc = updatedAtUtc;
    }

    public void UpdateType(CareerType type, DateTimeOffset updatedAtUtc)
    {
        EnsureUtc(updatedAtUtc, nameof(updatedAtUtc));
        EnsureDefined(type, nameof(Type));

        Type = type;
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

    private static string NormalizeCode(string value, string fieldName)
    {
        var normalized = NormalizeRequiredText(value, fieldName, 50).ToLowerInvariant();

        if (!CodeRegex().IsMatch(normalized))
        {
            throw new DomainException($"{fieldName} format is invalid.");
        }

        return normalized;
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

    private static void EnsureDefined(CareerType value, string fieldName)
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

    [GeneratedRegex("^[a-z0-9._-]+$")]
    private static partial Regex CodeRegex();
}
