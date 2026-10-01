using System.Net.Mail;
using AcademicSurveySystem.Domain.Common;
using AcademicSurveySystem.Domain.Identity.Enums;

namespace AcademicSurveySystem.Domain.Identity.Entities;

public sealed class User
{
    private readonly List<UserRole> _userRoles = [];
    private readonly List<UserCareer> _userCareers = [];

    private User()
    {
        FirstName = null!;
        LastName = null!;
        Email = null!;
        NormalizedEmail = null!;
        PasswordHash = null!;
    }

    public User(
        Guid id,
        string firstName,
        string lastName,
        string email,
        string passwordHash,
        DateTimeOffset createdAtUtc)
    {
        if (id == Guid.Empty)
        {
            throw new DomainException("User id is required.");
        }

        EnsureUtc(createdAtUtc, nameof(createdAtUtc));

        Id = id;
        FirstName = NormalizeRequiredText(firstName, nameof(FirstName), 100);
        LastName = NormalizeRequiredText(lastName, nameof(LastName), 100);
        Email = null!;
        NormalizedEmail = null!;
        SetEmail(email);
        PasswordHash = NormalizeRequiredText(passwordHash, nameof(PasswordHash), 512);
        Status = UserStatus.Active;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }
    public string FirstName { get; private set; }
    public string LastName { get; private set; }
    public string Email { get; private set; }
    public string NormalizedEmail { get; private set; }
    public string PasswordHash { get; private set; }
    public UserStatus Status { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public IReadOnlyCollection<UserRole> UserRoles => _userRoles.AsReadOnly();
    public IReadOnlyCollection<UserCareer> UserCareers => _userCareers.AsReadOnly();

    public void UpdateName(string firstName, string lastName, DateTimeOffset updatedAtUtc)
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

    public void ChangePasswordHash(string passwordHash, DateTimeOffset updatedAtUtc)
    {
        EnsureUtc(updatedAtUtc, nameof(updatedAtUtc));

        PasswordHash = NormalizeRequiredText(passwordHash, nameof(PasswordHash), 512);
        UpdatedAtUtc = updatedAtUtc;
    }

    public void Activate(DateTimeOffset updatedAtUtc)
    {
        ChangeStatus(UserStatus.Active, updatedAtUtc);
    }

    public void Deactivate(DateTimeOffset updatedAtUtc)
    {
        ChangeStatus(UserStatus.Inactive, updatedAtUtc);
    }

    public void Block(DateTimeOffset updatedAtUtc)
    {
        ChangeStatus(UserStatus.Blocked, updatedAtUtc);
    }

    public void AssignRole(Guid roleId, DateTimeOffset assignedAtUtc)
    {
        EnsureUtc(assignedAtUtc, nameof(assignedAtUtc));

        if (roleId == Guid.Empty)
        {
            throw new DomainException("RoleId is required.");
        }

        if (_userRoles.Any(userRole => userRole.RoleId == roleId))
        {
            return;
        }

        _userRoles.Add(new UserRole(Id, roleId, assignedAtUtc));
    }

    public void RemoveRole(Guid roleId)
    {
        if (roleId == Guid.Empty)
        {
            throw new DomainException("RoleId is required.");
        }

        var userRole = _userRoles.FirstOrDefault(item => item.RoleId == roleId);

        if (userRole is not null)
        {
            _userRoles.Remove(userRole);
        }
    }

    public void AssignCareer(Guid careerId, DateTimeOffset assignedAtUtc)
    {
        EnsureUtc(assignedAtUtc, nameof(assignedAtUtc));

        if (careerId == Guid.Empty)
        {
            throw new DomainException("CareerId is required.");
        }

        if (_userCareers.Any(userCareer => userCareer.CareerId == careerId))
        {
            return;
        }

        _userCareers.Add(new UserCareer(Id, careerId, assignedAtUtc));
    }

    public void RemoveCareer(Guid careerId)
    {
        if (careerId == Guid.Empty)
        {
            throw new DomainException("CareerId is required.");
        }

        var userCareer = _userCareers.FirstOrDefault(item => item.CareerId == careerId);

        if (userCareer is not null)
        {
            _userCareers.Remove(userCareer);
        }
    }

    private void ChangeStatus(UserStatus status, DateTimeOffset updatedAtUtc)
    {
        EnsureUtc(updatedAtUtc, nameof(updatedAtUtc));

        Status = status;
        UpdatedAtUtc = updatedAtUtc;
    }

    private void SetEmail(string email)
    {
        var cleanedEmail = NormalizeRequiredText(email, nameof(Email), 320).ToLowerInvariant();

        if (!IsValidEmail(cleanedEmail))
        {
            throw new DomainException("Email format is invalid.");
        }

        Email = cleanedEmail;
        NormalizedEmail = cleanedEmail.ToUpperInvariant();
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
