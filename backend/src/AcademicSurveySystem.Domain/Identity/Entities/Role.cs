using System.Text.RegularExpressions;
using AcademicSurveySystem.Domain.Common;

namespace AcademicSurveySystem.Domain.Identity.Entities;

public sealed partial class Role
{
    private readonly List<UserRole> _userRoles = [];
    private readonly List<RolePermission> _rolePermissions = [];

    private Role()
    {
        Code = null!;
        Name = null!;
    }

    public Role(
        Guid id,
        string code,
        string name,
        string? description,
        bool isSystem,
        DateTimeOffset createdAtUtc)
    {
        if (id == Guid.Empty)
        {
            throw new DomainException("Role id is required.");
        }

        EnsureUtc(createdAtUtc, nameof(createdAtUtc));

        Id = id;
        Code = NormalizeCode(code);
        Name = NormalizeRequiredText(name, nameof(Name), 150);
        Description = NormalizeOptionalText(description, nameof(Description), 500);
        IsSystem = isSystem;
        IsActive = true;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }
    public string Code { get; private set; }
    public string Name { get; private set; }
    public string? Description { get; private set; }
    public bool IsSystem { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public IReadOnlyCollection<UserRole> UserRoles => _userRoles.AsReadOnly();
    public IReadOnlyCollection<RolePermission> RolePermissions => _rolePermissions.AsReadOnly();

    public void UpdateDetails(string name, string? description, DateTimeOffset updatedAtUtc)
    {
        EnsureUtc(updatedAtUtc, nameof(updatedAtUtc));

        Name = NormalizeRequiredText(name, nameof(Name), 150);
        Description = NormalizeOptionalText(description, nameof(Description), 500);
        UpdatedAtUtc = updatedAtUtc;
    }

    public void Activate(DateTimeOffset updatedAtUtc)
    {
        EnsureUtc(updatedAtUtc, nameof(updatedAtUtc));

        IsActive = true;
        UpdatedAtUtc = updatedAtUtc;
    }

    public void Deactivate(DateTimeOffset updatedAtUtc)
    {
        EnsureUtc(updatedAtUtc, nameof(updatedAtUtc));

        IsActive = false;
        UpdatedAtUtc = updatedAtUtc;
    }

    public void GrantPermission(Guid permissionId, DateTimeOffset grantedAtUtc)
    {
        EnsureUtc(grantedAtUtc, nameof(grantedAtUtc));

        if (permissionId == Guid.Empty)
        {
            throw new DomainException("PermissionId is required.");
        }

        if (_rolePermissions.Any(rolePermission => rolePermission.PermissionId == permissionId))
        {
            return;
        }

        _rolePermissions.Add(new RolePermission(Id, permissionId, grantedAtUtc));
    }

    public void RevokePermission(Guid permissionId)
    {
        if (permissionId == Guid.Empty)
        {
            throw new DomainException("PermissionId is required.");
        }

        var rolePermission = _rolePermissions.FirstOrDefault(item => item.PermissionId == permissionId);

        if (rolePermission is not null)
        {
            _rolePermissions.Remove(rolePermission);
        }
    }

    private static string NormalizeCode(string code)
    {
        var normalized = NormalizeRequiredText(code, nameof(Code), 100).ToLowerInvariant();

        if (!RoleCodeRegex().IsMatch(normalized))
        {
            throw new DomainException("Role code format is invalid.");
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

    [GeneratedRegex("^[a-z0-9._-]+$")]
    private static partial Regex RoleCodeRegex();
}
