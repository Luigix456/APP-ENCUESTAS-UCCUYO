using AcademicSurveySystem.Application.Common.Security;
using AcademicSurveySystem.Application.Identity.InitialAdministrator;
using AcademicSurveySystem.Application.Identity.UserPasswordReset;
using AcademicSurveySystem.Domain.Common;
using AcademicSurveySystem.Domain.Identity.Entities;
using AcademicSurveySystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AcademicSurveySystem.Infrastructure.Identity;

public sealed class UserPasswordResetService : IUserPasswordResetService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<UserPasswordResetService> _logger;

    public UserPasswordResetService(
        ApplicationDbContext dbContext,
        IPasswordHasher passwordHasher,
        ILogger<UserPasswordResetService> logger)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    public async Task<UserPasswordResetResult> ResetAsync(
        string? email,
        string? newPassword,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return UserPasswordResetResult.Failed(
                UserPasswordResetStatus.InvalidRequest,
                "Email is required.");
        }

        var invalidPasswordResult = ValidateNewPassword(newPassword);
        if (invalidPasswordResult is not null)
        {
            return invalidPasswordResult;
        }

        var normalizedEmail = email.Trim().ToUpperInvariant();
        var user = await _dbContext.Users
            .SingleOrDefaultAsync(
                item => item.NormalizedEmail == normalizedEmail,
                cancellationToken);

        if (user is null)
        {
            return UserPasswordResetResult.Failed(
                UserPasswordResetStatus.UserNotFound,
                "Password reset failed. User was not found.");
        }

        return await ResetUserPasswordAsync(user, newPassword!, cancellationToken);
    }

    public async Task<UserPasswordResetResult> ResetByUserIdAsync(
        Guid userId,
        string? newPassword,
        CancellationToken cancellationToken = default)
    {
        var invalidPasswordResult = ValidateNewPassword(newPassword);
        if (invalidPasswordResult is not null)
        {
            return invalidPasswordResult;
        }

        var user = await _dbContext.Users
            .SingleOrDefaultAsync(
                item => item.Id == userId,
                cancellationToken);

        if (user is null)
        {
            return UserPasswordResetResult.Failed(
                UserPasswordResetStatus.UserNotFound,
                "Password reset failed. User was not found.");
        }

        return await ResetUserPasswordAsync(user, newPassword!, cancellationToken);
    }

    private async Task<UserPasswordResetResult> ResetUserPasswordAsync(
        User user,
        string newPassword,
        CancellationToken cancellationToken)
    {
        try
        {
            var passwordHash = _passwordHasher.Hash(newPassword);
            user.ChangePasswordHash(passwordHash, DateTimeOffset.UtcNow);

            await _dbContext.SaveChangesAsync(cancellationToken);

            return UserPasswordResetResult.Updated();
        }
        catch (DomainException exception)
        {
            _logger.LogWarning(
                "User password reset validation failed with error type {ErrorType}.",
                exception.GetType().Name);

            return UserPasswordResetResult.Failed(
                UserPasswordResetStatus.Failed,
                "Password reset failed. User data is invalid.");
        }
        catch (Exception exception)
        {
            _logger.LogError(
                "User password reset failed with error type {ErrorType}.",
                exception.GetType().Name);

            return UserPasswordResetResult.Failed(
                UserPasswordResetStatus.Failed,
                "Password reset failed. No credentials were written to output.");
        }
    }

    private static UserPasswordResetResult? ValidateNewPassword(string? newPassword)
    {
        var passwordValidation = InitialAdministratorPasswordPolicy.Validate(newPassword);

        return passwordValidation.IsValid
            ? null
            : UserPasswordResetResult.Failed(
                UserPasswordResetStatus.InvalidPassword,
                "Password is invalid. " + string.Join(' ', passwordValidation.Errors));
    }
}
