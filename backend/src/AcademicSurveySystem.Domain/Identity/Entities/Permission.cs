using System.Text.RegularExpressions;
using AcademicSurveySystem.Domain.Common;

namespace AcademicSurveySystem.Domain.Identity.Entities;

public sealed partial class Permission
{
    private readonly List<RolePermission> _rolePermissions = [];

    private Permission()
    {
        Code = null!;
        Name = null!;
        Module = null!;
    }

    public Permission(
        Guid id,
        string code,
        string name,
        string module,
        string? description,
        DateTimeOffset createdAtUtc)
    {
        if (id == Guid.Empty)
        {
            throw new DomainException("Permission id is required.");
        }

        EnsureUtc(createdAtUtc, nameof(createdAtUtc));

        Id = id;
        Code = NormalizeCode(code);
        Name = NormalizeRequiredText(name, nameof(Name), 150);
        Module = NormalizeModule(module);
        Description = NormalizeOptionalText(description, nameof(Description), 500);
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }
    public string Code { get; private set; }
    public string Name { get; private set; }
    public string Module { get; private set; }
    public string? Description { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public IReadOnlyCollection<RolePermission> RolePermissions => _rolePermissions.AsReadOnly();

    public void UpdateDetails(
        string name,
        string module,
        string? description,
        DateTimeOffset updatedAtUtc)
    {
        EnsureUtc(updatedAtUtc, nameof(updatedAtUtc));

        Name = NormalizeRequiredText(name, nameof(Name), 150);
        Module = NormalizeModule(module);
        Description = NormalizeOptionalText(description, nameof(Description), 500);
        UpdatedAtUtc = updatedAtUtc;
    }

    private static string NormalizeCode(string code)
    {
        var normalized = NormalizeRequiredText(code, nameof(Code), 150).ToLowerInvariant();

        if (!PermissionCodeRegex().IsMatch(normalized))
        {
            throw new DomainException("Permission code format is invalid.");
        }

        return normalized;
    }

    private static string NormalizeModule(string module)
    {
        return NormalizeRequiredText(module, nameof(Module), 100).ToLowerInvariant();
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

    [GeneratedRegex("^[a-z0-9]+([._-][a-z0-9]+)*$")]
    private static partial Regex PermissionCodeRegex();
}
