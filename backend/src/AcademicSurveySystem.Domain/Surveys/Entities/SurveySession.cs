using AcademicSurveySystem.Domain.Common;
using AcademicSurveySystem.Domain.Surveys.Enums;

namespace AcademicSurveySystem.Domain.Surveys.Entities;

public sealed class SurveySession
{
    private SurveySession()
    {
        AccessCode = null!;
    }

    public SurveySession(
        Guid id,
        Guid surveyAssignmentId,
        Guid createdByUserId,
        string accessCode,
        string? title,
        string? location,
        DateTimeOffset expiresAtUtc,
        DateTimeOffset createdAtUtc)
    {
        EnsureRequired(id, nameof(Id));
        EnsureRequired(surveyAssignmentId, nameof(SurveyAssignmentId));
        EnsureRequired(createdByUserId, nameof(CreatedByUserId));
        EnsureUtc(expiresAtUtc, nameof(expiresAtUtc));
        EnsureUtc(createdAtUtc, nameof(createdAtUtc));
        EnsureExpiresAfterCreated(expiresAtUtc, createdAtUtc);

        Id = id;
        SurveyAssignmentId = surveyAssignmentId;
        CreatedByUserId = createdByUserId;
        AccessCode = NormalizeRequiredText(accessCode, nameof(AccessCode), 128);
        Title = NormalizeOptionalText(title, nameof(Title), 200);
        Location = NormalizeOptionalText(location, nameof(Location), 200);
        Status = SurveySessionStatus.Created;
        ExpiresAtUtc = expiresAtUtc;
        IsActive = true;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid SurveyAssignmentId { get; private set; }
    public Guid CreatedByUserId { get; private set; }
    public string AccessCode { get; private set; }
    public string? Title { get; private set; }
    public string? Location { get; private set; }
    public SurveySessionStatus Status { get; private set; }
    public DateTimeOffset ExpiresAtUtc { get; private set; }
    public DateTimeOffset? OpenedAtUtc { get; private set; }
    public DateTimeOffset? ClosedAtUtc { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public SurveyAssignment SurveyAssignment { get; private set; } = null!;

    public void UpdateDetails(
        string? title,
        string? location,
        DateTimeOffset expiresAtUtc,
        DateTimeOffset updatedAtUtc)
    {
        EnsureCanUpdate();
        EnsureUtc(expiresAtUtc, nameof(expiresAtUtc));
        EnsureUtc(updatedAtUtc, nameof(updatedAtUtc));
        EnsureExpiresAfterCreated(expiresAtUtc, CreatedAtUtc);

        Title = NormalizeOptionalText(title, nameof(Title), 200);
        Location = NormalizeOptionalText(location, nameof(Location), 200);
        ExpiresAtUtc = expiresAtUtc;
        UpdatedAtUtc = updatedAtUtc;
    }

    public void Open(DateTimeOffset openedAtUtc)
    {
        EnsureUtc(openedAtUtc, nameof(openedAtUtc));

        if (Status != SurveySessionStatus.Created)
        {
            throw new DomainException("Only created sessions can be opened.");
        }

        if (!IsActive)
        {
            throw new DomainException("Inactive sessions cannot be opened.");
        }

        if (ExpiresAtUtc <= openedAtUtc)
        {
            throw new DomainException("Expired sessions cannot be opened.");
        }

        Status = SurveySessionStatus.Open;
        OpenedAtUtc = openedAtUtc;
        UpdatedAtUtc = openedAtUtc;
    }

    public void Close(DateTimeOffset closedAtUtc)
    {
        EnsureUtc(closedAtUtc, nameof(closedAtUtc));

        if (Status is not SurveySessionStatus.Open and not SurveySessionStatus.Created)
        {
            throw new DomainException("Only created or open sessions can be closed.");
        }

        Status = SurveySessionStatus.Closed;
        ClosedAtUtc = closedAtUtc;
        UpdatedAtUtc = closedAtUtc;
    }

    public void Cancel(DateTimeOffset cancelledAtUtc)
    {
        EnsureUtc(cancelledAtUtc, nameof(cancelledAtUtc));

        if (Status is SurveySessionStatus.Closed or SurveySessionStatus.Expired)
        {
            throw new DomainException("Closed or expired sessions cannot be cancelled.");
        }

        Status = SurveySessionStatus.Cancelled;
        UpdatedAtUtc = cancelledAtUtc;
    }

    public void Expire(DateTimeOffset expiredAtUtc)
    {
        EnsureUtc(expiredAtUtc, nameof(expiredAtUtc));

        if (Status != SurveySessionStatus.Open || ExpiresAtUtc > expiredAtUtc)
        {
            throw new DomainException("Only open sessions past their expiration can be expired.");
        }

        Status = SurveySessionStatus.Expired;
        UpdatedAtUtc = expiredAtUtc;
    }

    public void Activate(DateTimeOffset updatedAtUtc)
    {
        ChangeActiveState(true, updatedAtUtc);
    }

    public void Deactivate(DateTimeOffset updatedAtUtc)
    {
        ChangeActiveState(false, updatedAtUtc);
    }

    public bool IsAvailable(DateTimeOffset nowUtc)
    {
        EnsureUtc(nowUtc, nameof(nowUtc));

        return IsActive
            && Status == SurveySessionStatus.Open
            && ExpiresAtUtc > nowUtc;
    }

    private void ChangeActiveState(bool isActive, DateTimeOffset updatedAtUtc)
    {
        EnsureUtc(updatedAtUtc, nameof(updatedAtUtc));

        IsActive = isActive;
        UpdatedAtUtc = updatedAtUtc;
    }

    private void EnsureCanUpdate()
    {
        if (Status is SurveySessionStatus.Closed
            or SurveySessionStatus.Cancelled
            or SurveySessionStatus.Expired)
        {
            throw new DomainException("Closed, cancelled or expired sessions cannot be updated.");
        }
    }

    private static void EnsureRequired(Guid value, string fieldName)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException($"{fieldName} is required.");
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

    private static void EnsureExpiresAfterCreated(
        DateTimeOffset expiresAtUtc,
        DateTimeOffset createdAtUtc)
    {
        if (expiresAtUtc <= createdAtUtc)
        {
            throw new DomainException("ExpiresAtUtc must be greater than CreatedAtUtc.");
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
