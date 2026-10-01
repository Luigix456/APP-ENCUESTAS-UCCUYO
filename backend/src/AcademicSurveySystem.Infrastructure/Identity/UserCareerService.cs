using AcademicSurveySystem.Application.Common.Results;
using AcademicSurveySystem.Application.Identity.UserCareers;
using AcademicSurveySystem.Domain.Identity.Entities;
using AcademicSurveySystem.Domain.Identity.Enums;
using AcademicSurveySystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AcademicSurveySystem.Infrastructure.Identity;

public sealed class UserCareerService : IUserCareerService
{
    private readonly ApplicationDbContext _dbContext;

    public UserCareerService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ApplicationResult<IReadOnlyCollection<UserCareerDto>>> GetUserCareersAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var userExists = await _dbContext.Users
            .AsNoTracking()
            .AnyAsync(user => user.Id == userId, cancellationToken);

        if (!userExists)
        {
            return ApplicationResult<IReadOnlyCollection<UserCareerDto>>.NotFound("User was not found.");
        }

        var careers = await QueryUserCareers(userId)
            .ToArrayAsync(cancellationToken);

        return ApplicationResult<IReadOnlyCollection<UserCareerDto>>.Success(careers);
    }

    public async Task<ApplicationResult<IReadOnlyCollection<UserCareerDto>>> ReplaceUserCareersAsync(
        Guid userId,
        UpdateUserCareersRequest request,
        CancellationToken cancellationToken)
    {
        var validationErrors = request.Validate();

        if (validationErrors.Count > 0)
        {
            return ApplicationResult<IReadOnlyCollection<UserCareerDto>>.Validation(validationErrors);
        }

        var user = await _dbContext.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == userId, cancellationToken);

        if (user is null)
        {
            return ApplicationResult<IReadOnlyCollection<UserCareerDto>>.NotFound("User was not found.");
        }

        if (user.Status != UserStatus.Active)
        {
            return ApplicationResult<IReadOnlyCollection<UserCareerDto>>.Validation([
                new ApplicationError("UserCareer.UserInactive", "User must be active.")
            ]);
        }

        var requestedCareerIds = (request.CareerIds ?? Array.Empty<Guid>())
            .Distinct()
            .ToArray();

        var careers = await _dbContext.Careers
            .Where(career => requestedCareerIds.Contains(career.Id))
            .Select(career => new
            {
                career.Id,
                career.IsActive
            })
            .ToArrayAsync(cancellationToken);

        var missingCareerIds = requestedCareerIds
            .Except(careers.Select(career => career.Id))
            .ToArray();

        if (missingCareerIds.Length > 0)
        {
            return ApplicationResult<IReadOnlyCollection<UserCareerDto>>.NotFound("Career was not found.");
        }

        if (careers.Any(career => !career.IsActive))
        {
            return ApplicationResult<IReadOnlyCollection<UserCareerDto>>.Validation([
                new ApplicationError("UserCareer.CareerInactive", "Career must be active.")
            ]);
        }

        var currentUserCareers = await _dbContext.UserCareers
            .Where(userCareer => userCareer.UserId == userId)
            .ToArrayAsync(cancellationToken);
        var currentCareerIds = currentUserCareers
            .Select(userCareer => userCareer.CareerId)
            .ToHashSet();
        var requestedCareerIdSet = requestedCareerIds.ToHashSet();

        var userCareersToRemove = currentUserCareers
            .Where(userCareer => !requestedCareerIdSet.Contains(userCareer.CareerId))
            .ToArray();

        _dbContext.UserCareers.RemoveRange(userCareersToRemove);

        var now = DateTimeOffset.UtcNow;

        foreach (var careerId in requestedCareerIds.Except(currentCareerIds))
        {
            _dbContext.UserCareers.Add(new UserCareer(userId, careerId, now));
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        var updatedCareers = await QueryUserCareers(userId)
            .ToArrayAsync(cancellationToken);

        return ApplicationResult<IReadOnlyCollection<UserCareerDto>>.Success(updatedCareers);
    }

    private IQueryable<UserCareerDto> QueryUserCareers(Guid userId)
    {
        return _dbContext.UserCareers
            .AsNoTracking()
            .Where(userCareer => userCareer.UserId == userId)
            .OrderBy(userCareer => userCareer.Career.Name)
            .Select(userCareer => new UserCareerDto(
                userCareer.CareerId,
                userCareer.Career.Code,
                userCareer.Career.Name,
                userCareer.Career.IsActive,
                userCareer.AssignedAtUtc));
    }
}
