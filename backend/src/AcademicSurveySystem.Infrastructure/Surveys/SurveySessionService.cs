using AcademicSurveySystem.Application.Common.Results;
using AcademicSurveySystem.Application.Surveys.Responses;
using AcademicSurveySystem.Application.Surveys.Sessions;
using AcademicSurveySystem.Domain.Common;
using AcademicSurveySystem.Domain.Surveys.Entities;
using AcademicSurveySystem.Domain.Surveys.Enums;
using AcademicSurveySystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace AcademicSurveySystem.Infrastructure.Surveys;

public sealed class SurveySessionService : ISurveySessionService
{
    private const int MaxAccessCodeGenerationAttempts = 10;
    private const string UniqueViolationSqlState = "23505";

    private readonly ApplicationDbContext _dbContext;
    private readonly ISurveySessionAccessCodeGenerator _accessCodeGenerator;

    public SurveySessionService(
        ApplicationDbContext dbContext,
        ISurveySessionAccessCodeGenerator accessCodeGenerator)
    {
        _dbContext = dbContext;
        _accessCodeGenerator = accessCodeGenerator;
    }

    public async Task<ApplicationResult<IReadOnlyCollection<SurveySessionDto>>> GetSessionsAsync(
        bool includeInactive,
        string? status,
        Guid? surveyAssignmentId,
        string? accessCode,
        Guid? careerId,
        Guid? academicCycleId,
        Guid? subjectId,
        Guid? teacherId,
        CancellationToken cancellationToken)
    {
        SurveySessionStatus? parsedStatus = null;

        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!Enum.TryParse<SurveySessionStatus>(status, ignoreCase: true, out var value)
                || !Enum.IsDefined(value))
            {
                return ApplicationResult<IReadOnlyCollection<SurveySessionDto>>.Validation([
                    new ApplicationError("SurveySession.StatusInvalid", "Status is invalid.")
                ]);
            }

            parsedStatus = value;
        }

        var sessions = await QuerySessions()
            .AsNoTracking()
            .Where(session => includeInactive || session.IsActive)
            .Where(session => parsedStatus == null || session.Status == parsedStatus.Value)
            .Where(session => surveyAssignmentId == null || session.SurveyAssignmentId == surveyAssignmentId.Value)
            .Where(session => string.IsNullOrWhiteSpace(accessCode) || session.AccessCode == accessCode.Trim())
            .Where(session => careerId == null || session.SurveyAssignment.CareerId == careerId.Value)
            .Where(session => academicCycleId == null || session.SurveyAssignment.AcademicCycleId == academicCycleId.Value)
            .Where(session => subjectId == null || session.SurveyAssignment.SubjectId == subjectId.Value)
            .Where(session => teacherId == null
                || session.SurveyAssignment.TeacherSubjectAssignment!.TeacherId == teacherId.Value)
            .OrderByDescending(session => session.ExpiresAtUtc)
            .ThenByDescending(session => session.UpdatedAtUtc)
            .ToArrayAsync(cancellationToken);

        var progressByAssignment = await LoadProgressByAssignmentAsync(
            sessions.Select(session => session.SurveyAssignmentId).Distinct().ToArray(),
            cancellationToken);
        var responseCountBySession = await LoadResponseCountBySessionAsync(
            sessions.Select(session => session.Id).ToArray(),
            cancellationToken);

        return ApplicationResult<IReadOnlyCollection<SurveySessionDto>>.Success(
            sessions.Select(session => MapDto(
                session,
                responseCountBySession.GetValueOrDefault(session.Id),
                progressByAssignment.GetValueOrDefault(session.SurveyAssignmentId))).ToArray());
    }

    public async Task<ApplicationResult<SurveySessionDto>> GetSessionByIdAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var session = await QuerySessions()
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (session is null)
        {
            return ApplicationResult<SurveySessionDto>.NotFound("Survey session was not found.");
        }

        var sessionResponseCount = await _dbContext.SurveyResponses
            .AsNoTracking()
            .CountAsync(response => response.SurveySessionId == session.Id, cancellationToken);
        var assignmentResponseCount = await _dbContext.SurveyResponses
            .AsNoTracking()
            .CountAsync(
                response => response.SurveySession.SurveyAssignmentId == session.SurveyAssignmentId,
                cancellationToken);
        var progress = SurveyResponseProgressCalculator.Build(
            session.SurveyAssignmentId,
            session.SurveyAssignment.ExpectedRespondentCount,
            assignmentResponseCount);

        return ApplicationResult<SurveySessionDto>.Success(MapDto(session, sessionResponseCount, progress));
    }

    public async Task<ApplicationResult<SurveySessionDto>> CreateSessionAsync(
        CreateSurveySessionRequest request,
        Guid createdByUserId,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var validationErrors = request.Validate(now).ToList();

        if (createdByUserId == Guid.Empty)
        {
            validationErrors.Add(new ApplicationError(
                "SurveySession.CreatedByUserIdRequired",
                "CreatedByUserId is required."));
        }

        if (validationErrors.Count > 0)
        {
            return ApplicationResult<SurveySessionDto>.Validation(validationErrors);
        }

        var relationshipValidation = await ValidateAssignmentCanBeUsedAsync(
            request.SurveyAssignmentId,
            cancellationToken);

        if (relationshipValidation.Status != ApplicationResultStatus.Success)
        {
            return ToSessionDtoResult(relationshipValidation);
        }

        var accessCodeResult = await GenerateUniqueAccessCodeAsync(cancellationToken);

        if (accessCodeResult.Status != ApplicationResultStatus.Success)
        {
            return ToSessionDtoResult(accessCodeResult);
        }

        var session = new SurveySession(
            Guid.NewGuid(),
            request.SurveyAssignmentId,
            createdByUserId,
            accessCodeResult.Value!,
            request.Title,
            request.Location,
            request.ExpiresAtUtc,
            now);

        _dbContext.SurveySessions.Add(session);

        var saveResult = await SaveChangesAsync(
            "The survey session could not be saved.",
            cancellationToken);

        if (saveResult.Status != ApplicationResultStatus.Success)
        {
            return ToSessionDtoResult(saveResult);
        }

        return await GetSessionByIdAsync(session.Id, cancellationToken);
    }

    public async Task<ApplicationResult> UpdateSessionAsync(
        Guid id,
        UpdateSurveySessionRequest request,
        CancellationToken cancellationToken)
    {
        var validationErrors = request.Validate();

        if (validationErrors.Count > 0)
        {
            return ApplicationResult.Validation(validationErrors);
        }

        return await ChangeSessionAsync(
            id,
            session => session.UpdateDetails(
                request.Title,
                request.Location,
                request.ExpiresAtUtc,
                DateTimeOffset.UtcNow),
            "The survey session could not be updated.",
            cancellationToken);
    }

    public async Task<ApplicationResult> OpenSessionAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var session = await _dbContext.SurveySessions
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (session is null)
        {
            return ApplicationResult.NotFound("Survey session was not found.");
        }

        var relationshipValidation = await ValidateAssignmentCanBeUsedAsync(
            session.SurveyAssignmentId,
            cancellationToken);

        if (relationshipValidation.Status != ApplicationResultStatus.Success)
        {
            return relationshipValidation;
        }

        return await ApplySessionChangeAsync(
            () => session.Open(DateTimeOffset.UtcNow),
            "The survey session could not be opened.",
            cancellationToken);
    }

    public Task<ApplicationResult> CloseSessionAsync(
        Guid id,
        CancellationToken cancellationToken) =>
        ChangeSessionAsync(
            id,
            session => session.Close(DateTimeOffset.UtcNow),
            "The survey session could not be closed.",
            cancellationToken);

    public Task<ApplicationResult> CancelSessionAsync(
        Guid id,
        CancellationToken cancellationToken) =>
        ChangeSessionAsync(
            id,
            session => session.Cancel(DateTimeOffset.UtcNow),
            "The survey session could not be cancelled.",
            cancellationToken);

    public Task<ApplicationResult> ActivateSessionAsync(
        Guid id,
        CancellationToken cancellationToken) =>
        ChangeSessionAsync(
            id,
            session => session.Activate(DateTimeOffset.UtcNow),
            "The survey session could not be activated.",
            cancellationToken);

    public Task<ApplicationResult> DeactivateSessionAsync(
        Guid id,
        CancellationToken cancellationToken) =>
        ChangeSessionAsync(
            id,
            session => session.Deactivate(DateTimeOffset.UtcNow),
            "The survey session could not be deactivated.",
            cancellationToken);

    public async Task<ApplicationResult<PublicSurveySessionDto>> GetPublicSessionByAccessCodeAsync(
        string accessCode,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(accessCode))
        {
            return ApplicationResult<PublicSurveySessionDto>.NotFound("Survey session was not found.");
        }

        var normalizedAccessCode = accessCode.Trim();
        var session = await QuerySessionsWithSurveyDefinition()
            .SingleOrDefaultAsync(item => item.AccessCode == normalizedAccessCode, cancellationToken);

        if (session is null)
        {
            return ApplicationResult<PublicSurveySessionDto>.NotFound("Survey session was not found.");
        }

        var now = DateTimeOffset.UtcNow;

        if (session.Status == SurveySessionStatus.Open && session.ExpiresAtUtc <= now)
        {
            try
            {
                session.Expire(now);
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (DomainException)
            {
                return ApplicationResult<PublicSurveySessionDto>.Validation([
                    new ApplicationError("SurveySession.NotAvailable", "Survey session is not available.")
                ]);
            }
            catch (DbUpdateException)
            {
                return ApplicationResult<PublicSurveySessionDto>.Failure(
                    "The survey session could not be updated.");
            }
        }

        if (!session.IsAvailable(now))
        {
            return ApplicationResult<PublicSurveySessionDto>.Validation([
                new ApplicationError("SurveySession.NotAvailable", "Survey session is not available.")
            ]);
        }

        return ApplicationResult<PublicSurveySessionDto>.Success(MapPublicDto(session));
    }

    private IQueryable<SurveySession> QuerySessions()
    {
        return _dbContext.SurveySessions
            .Include(session => session.SurveyAssignment)
                .ThenInclude(assignment => assignment.Survey)
            .Include(session => session.SurveyAssignment)
                .ThenInclude(assignment => assignment.Career)
            .Include(session => session.SurveyAssignment)
                .ThenInclude(assignment => assignment.Subject)
            .Include(session => session.SurveyAssignment)
                .ThenInclude(assignment => assignment.AcademicCycle)
            .Include(session => session.SurveyAssignment)
                .ThenInclude(assignment => assignment.TeacherSubjectAssignment)
                    .ThenInclude(teacherSubjectAssignment => teacherSubjectAssignment.Teacher);
    }

    private IQueryable<SurveySession> QuerySessionsWithSurveyDefinition()
    {
        return QuerySessions()
            .Include(session => session.SurveyAssignment)
                .ThenInclude(assignment => assignment.Survey)
                    .ThenInclude(survey => survey.Sections)
                        .ThenInclude(section => section.Questions)
                            .ThenInclude(question => question.Options)
            .Include(session => session.SurveyAssignment)
                .ThenInclude(assignment => assignment.Survey)
                    .ThenInclude(survey => survey.Sections)
                        .ThenInclude(section => section.Questions)
                            .ThenInclude(question => question.MatrixRows);
    }

    private async Task<ApplicationResult> ChangeSessionAsync(
        Guid id,
        Action<SurveySession> change,
        string failureMessage,
        CancellationToken cancellationToken)
    {
        var session = await _dbContext.SurveySessions
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (session is null)
        {
            return ApplicationResult.NotFound("Survey session was not found.");
        }

        return await ApplySessionChangeAsync(
            () => change(session),
            failureMessage,
            cancellationToken);
    }

    private async Task<ApplicationResult> ApplySessionChangeAsync(
        Action change,
        string failureMessage,
        CancellationToken cancellationToken)
    {
        try
        {
            change();
            await _dbContext.SaveChangesAsync(cancellationToken);

            return ApplicationResult.Success();
        }
        catch (DomainException exception)
        {
            return ApplicationResult.Validation([
                new ApplicationError("SurveySession.Validation", exception.Message)
            ]);
        }
        catch (DbUpdateException)
        {
            return ApplicationResult.Failure(failureMessage);
        }
    }

    private async Task<ApplicationResult> ValidateAssignmentCanBeUsedAsync(
        Guid surveyAssignmentId,
        CancellationToken cancellationToken)
    {
        var assignment = await _dbContext.SurveyAssignments
            .AsNoTracking()
            .Where(item => item.Id == surveyAssignmentId)
            .Select(item => new
            {
                item.IsActive,
                SurveyIsActive = item.Survey.IsActive,
                item.Survey.Status,
                CareerIsActive = item.Career.IsActive,
                SubjectIsActive = item.Subject.IsActive,
                SubjectCareerId = item.Subject.CareerId,
                AcademicCycleIsActive = item.AcademicCycle.IsActive,
                TeacherSubjectAssignmentIsActive = item.TeacherSubjectAssignment.IsActive,
                TeacherSubjectAssignmentSubjectId = item.TeacherSubjectAssignment.SubjectId,
                TeacherSubjectAssignmentAcademicCycleId = item.TeacherSubjectAssignment.AcademicCycleId,
                TeacherIsActive = item.TeacherSubjectAssignment.Teacher.IsActive,
                item.CareerId,
                item.SubjectId,
                item.AcademicCycleId
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (assignment is null)
        {
            return ApplicationResult.NotFound("Survey assignment was not found.");
        }

        var errors = new List<ApplicationError>();

        if (!assignment.IsActive)
        {
            errors.Add(new ApplicationError("SurveySession.AssignmentInactive", "Survey assignment must be active."));
        }

        if (!assignment.SurveyIsActive)
        {
            errors.Add(new ApplicationError("SurveySession.SurveyInactive", "Survey must be active."));
        }

        if (assignment.Status != SurveyStatus.Published)
        {
            errors.Add(new ApplicationError("SurveySession.SurveyNotPublished", "Survey must be published."));
        }

        if (!assignment.CareerIsActive)
        {
            errors.Add(new ApplicationError("SurveySession.CareerInactive", "Career must be active."));
        }

        if (!assignment.SubjectIsActive)
        {
            errors.Add(new ApplicationError("SurveySession.SubjectInactive", "Subject must be active."));
        }

        if (assignment.SubjectCareerId != assignment.CareerId)
        {
            errors.Add(new ApplicationError(
                "SurveySession.SubjectCareerMismatch",
                "Subject must belong to the selected career."));
        }

        if (!assignment.AcademicCycleIsActive)
        {
            errors.Add(new ApplicationError(
                "SurveySession.AcademicCycleInactive",
                "Academic cycle must be active."));
        }

        if (!assignment.TeacherSubjectAssignmentIsActive)
        {
            errors.Add(new ApplicationError(
                "SurveySession.TeacherSubjectAssignmentInactive",
                "Teacher-subject assignment must be active."));
        }

        if (assignment.TeacherSubjectAssignmentSubjectId != assignment.SubjectId)
        {
            errors.Add(new ApplicationError(
                "SurveySession.TeacherSubjectAssignmentSubjectMismatch",
                "Teacher-subject assignment must belong to the selected subject."));
        }

        if (assignment.TeacherSubjectAssignmentAcademicCycleId != assignment.AcademicCycleId)
        {
            errors.Add(new ApplicationError(
                "SurveySession.TeacherSubjectAssignmentCycleMismatch",
                "Teacher-subject assignment must belong to the selected academic cycle."));
        }

        if (!assignment.TeacherIsActive)
        {
            errors.Add(new ApplicationError("SurveySession.TeacherInactive", "Teacher must be active."));
        }

        return errors.Count == 0
            ? ApplicationResult.Success()
            : ApplicationResult.Validation(errors);
    }

    private async Task<ApplicationResult<string>> GenerateUniqueAccessCodeAsync(
        CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < MaxAccessCodeGenerationAttempts; attempt++)
        {
            var accessCode = _accessCodeGenerator.Generate();
            var exists = await _dbContext.SurveySessions
                .AsNoTracking()
                .AnyAsync(session => session.AccessCode == accessCode, cancellationToken);

            if (!exists)
            {
                return ApplicationResult<string>.Success(accessCode);
            }
        }

        return ApplicationResult<string>.Conflict(
            "A unique survey session access code could not be generated.");
    }

    private async Task<ApplicationResult> SaveChangesAsync(
        string failureMessage,
        CancellationToken cancellationToken)
    {
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);

            return ApplicationResult.Success();
        }
        catch (DomainException exception)
        {
            return ApplicationResult.Validation([
                new ApplicationError("SurveySession.Validation", exception.Message)
            ]);
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
        {
            return ApplicationResult.Conflict("Survey session access code already exists.");
        }
        catch (DbUpdateException)
        {
            return ApplicationResult.Failure(failureMessage);
        }
    }

    private static bool IsUniqueViolation(DbUpdateException exception)
    {
        return exception.InnerException is PostgresException postgresException
            && postgresException.SqlState == UniqueViolationSqlState;
    }

    private async Task<Dictionary<Guid, SurveyResponseProgressDto>> LoadProgressByAssignmentAsync(
        IReadOnlyCollection<Guid> surveyAssignmentIds,
        CancellationToken cancellationToken)
    {
        if (surveyAssignmentIds.Count == 0)
        {
            return [];
        }

        var responseCounts = await _dbContext.SurveyResponses
            .AsNoTracking()
            .Where(response => surveyAssignmentIds.Contains(response.SurveySession.SurveyAssignmentId))
            .GroupBy(response => response.SurveySession.SurveyAssignmentId)
            .Select(group => new
            {
                SurveyAssignmentId = group.Key,
                ResponseCount = group.Count()
            })
            .ToDictionaryAsync(
                item => item.SurveyAssignmentId,
                item => item.ResponseCount,
                cancellationToken);

        return await _dbContext.SurveyAssignments
            .AsNoTracking()
            .Where(assignment => surveyAssignmentIds.Contains(assignment.Id))
            .Select(assignment => new
            {
                assignment.Id,
                assignment.ExpectedRespondentCount
            })
            .ToDictionaryAsync(
                item => item.Id,
                item => SurveyResponseProgressCalculator.Build(
                    item.Id,
                    item.ExpectedRespondentCount,
                    responseCounts.GetValueOrDefault(item.Id)),
                cancellationToken);
    }

    private Task<Dictionary<Guid, int>> LoadResponseCountBySessionAsync(
        IReadOnlyCollection<Guid> surveySessionIds,
        CancellationToken cancellationToken)
    {
        if (surveySessionIds.Count == 0)
        {
            return Task.FromResult(new Dictionary<Guid, int>());
        }

        return _dbContext.SurveyResponses
            .AsNoTracking()
            .Where(response => surveySessionIds.Contains(response.SurveySessionId))
            .GroupBy(response => response.SurveySessionId)
            .Select(group => new
            {
                SurveySessionId = group.Key,
                ResponseCount = group.Count()
            })
            .ToDictionaryAsync(
                item => item.SurveySessionId,
                item => item.ResponseCount,
                cancellationToken);
    }

    private static SurveySessionDto MapDto(
        SurveySession session,
        int sessionResponseCount,
        SurveyResponseProgressDto? progress)
    {
        var assignment = session.SurveyAssignment;
        var teacher = assignment.TeacherSubjectAssignment.Teacher;
        progress ??= SurveyResponseProgressCalculator.Build(
            assignment.Id,
            assignment.ExpectedRespondentCount,
            0);

        return new SurveySessionDto(
            session.Id,
            session.SurveyAssignmentId,
            session.AccessCode,
            BuildPublicPath(session.AccessCode),
            null,
            session.Title,
            session.Location,
            session.Status.ToString(),
            session.ExpiresAtUtc,
            session.OpenedAtUtc,
            session.ClosedAtUtc,
            session.IsActive,
            assignment.SurveyId,
            assignment.Survey.Title,
            assignment.CareerId,
            assignment.Career.Name,
            assignment.SubjectId,
            assignment.Subject.Name,
            assignment.AcademicCycleId,
            assignment.AcademicCycle.Year,
            assignment.AcademicCycle.Period.ToString(),
            teacher.Id,
            teacher.FirstName + " " + teacher.LastName,
            assignment.TeacherSubjectAssignment.TeachingRole,
            session.CreatedByUserId,
            session.CreatedAtUtc,
            session.UpdatedAtUtc,
            sessionResponseCount,
            progress.ResponseCount,
            progress.ExpectedRespondentCount,
            progress.RemainingCount,
            progress.ParticipationPercentage);
    }

    private static PublicSurveySessionDto MapPublicDto(SurveySession session)
    {
        var assignment = session.SurveyAssignment;
        var survey = assignment.Survey;
        var teacher = assignment.TeacherSubjectAssignment.Teacher;

        var sections = survey.Sections
            .Where(section => section.IsActive)
            .OrderBy(section => section.Order)
            .Select(section => new PublicSurveySectionDto(
                section.Id,
                section.Title,
                section.Description,
                section.Order,
                section.Questions
                    .Where(question => question.IsActive)
                    .OrderBy(question => question.Order)
                    .Select(question => new PublicSurveyQuestionDto(
                        question.Id,
                        question.Text,
                        question.Type.ToString(),
                        question.IsRequired,
                        question.AllowsComment,
                        question.AllowsOtherOption,
                        question.Order,
                        question.Options
                            .Where(option => option.IsActive)
                            .OrderBy(option => option.Order)
                            .Select(option => new PublicSurveyQuestionOptionDto(
                                option.Id,
                                option.Text,
                                option.Value,
                                option.Order))
                            .ToArray(),
                        question.MatrixRows
                            .Where(matrixRow => matrixRow.IsActive)
                            .OrderBy(matrixRow => matrixRow.Order)
                            .Select(matrixRow => new PublicSurveyMatrixRowDto(
                                matrixRow.Id,
                                matrixRow.Text,
                                matrixRow.Order))
                            .ToArray(),
                        question.RatingMin,
                        question.RatingMax))
                    .ToArray()))
            .ToArray();

        return new PublicSurveySessionDto(
            session.Id,
            session.AccessCode,
            session.ExpiresAtUtc,
            survey.Id,
            survey.Title,
            survey.Description,
            survey.Target.ToString(),
            assignment.Career.Name,
            assignment.Subject.Name,
            assignment.AcademicCycle.Year,
            assignment.AcademicCycle.Period.ToString(),
            teacher.FirstName + " " + teacher.LastName,
            assignment.TeacherSubjectAssignment.TeachingRole,
            sections);
    }

    private static string BuildPublicPath(string accessCode)
    {
        return $"/api/public/survey-sessions/{Uri.EscapeDataString(accessCode)}";
    }

    private static ApplicationResult<SurveySessionDto> ToSessionDtoResult(ApplicationResult result)
    {
        return result.Status switch
        {
            ApplicationResultStatus.Validation => ApplicationResult<SurveySessionDto>.Validation(result.Errors),
            ApplicationResultStatus.NotFound => ApplicationResult<SurveySessionDto>.NotFound(
                result.Errors.First().Message),
            ApplicationResultStatus.Conflict => ApplicationResult<SurveySessionDto>.Conflict(
                result.Errors.First().Message),
            _ => ApplicationResult<SurveySessionDto>.Failure(result.Errors.First().Message)
        };
    }
}
