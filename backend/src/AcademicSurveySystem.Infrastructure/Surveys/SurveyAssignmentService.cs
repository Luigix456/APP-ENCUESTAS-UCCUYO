using System.Linq.Expressions;
using AcademicSurveySystem.Application.Common.Results;
using AcademicSurveySystem.Application.Surveys.Assignments;
using AcademicSurveySystem.Domain.Common;
using AcademicSurveySystem.Domain.Surveys.Entities;
using AcademicSurveySystem.Domain.Surveys.Enums;
using AcademicSurveySystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace AcademicSurveySystem.Infrastructure.Surveys;

public sealed class SurveyAssignmentService : ISurveyAssignmentService
{
    private const string UniqueViolationSqlState = "23505";

    private readonly ApplicationDbContext _dbContext;

    public SurveyAssignmentService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ApplicationResult<IReadOnlyCollection<SurveyAssignmentDto>>> GetAssignmentsAsync(
        bool includeInactive,
        Guid? surveyId,
        Guid? careerId,
        Guid? subjectId,
        Guid? academicCycleId,
        Guid? teacherId,
        CancellationToken cancellationToken)
    {
        var assignments = await _dbContext.SurveyAssignments
            .AsNoTracking()
            .Where(assignment => includeInactive || assignment.IsActive)
            .Where(assignment => surveyId == null || assignment.SurveyId == surveyId.Value)
            .Where(assignment => careerId == null || assignment.CareerId == careerId.Value)
            .Where(assignment => subjectId == null || assignment.SubjectId == subjectId.Value)
            .Where(assignment => academicCycleId == null || assignment.AcademicCycleId == academicCycleId.Value)
            .Where(assignment => teacherId == null || assignment.TeacherSubjectAssignment.TeacherId == teacherId.Value)
            .OrderByDescending(assignment => assignment.AcademicCycle.Year)
            .ThenBy(assignment => assignment.Career.Name)
            .ThenBy(assignment => assignment.Subject.Name)
            .ThenBy(assignment =>
                assignment.TeacherSubjectAssignment.Teacher.FirstName
                + " "
                + assignment.TeacherSubjectAssignment.Teacher.LastName)
            .Select(MapDtoExpression())
            .ToArrayAsync(cancellationToken);

        return ApplicationResult<IReadOnlyCollection<SurveyAssignmentDto>>.Success(assignments);
    }

    public async Task<ApplicationResult<SurveyAssignmentDto>> GetAssignmentByIdAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var assignment = await _dbContext.SurveyAssignments
            .AsNoTracking()
            .Where(item => item.Id == id)
            .Select(MapDtoExpression())
            .SingleOrDefaultAsync(cancellationToken);

        return assignment is null
            ? ApplicationResult<SurveyAssignmentDto>.NotFound("Survey assignment was not found.")
            : ApplicationResult<SurveyAssignmentDto>.Success(assignment);
    }

    public async Task<ApplicationResult<SurveyAssignmentDto>> CreateAssignmentAsync(
        CreateSurveyAssignmentRequest request,
        CancellationToken cancellationToken)
    {
        var validationErrors = request.Validate();

        if (validationErrors.Count > 0)
        {
            return ApplicationResult<SurveyAssignmentDto>.Validation(validationErrors);
        }

        var relationshipValidation = await ValidateRelatedEntitiesAsync(request, cancellationToken);

        if (relationshipValidation.Status != ApplicationResultStatus.Success)
        {
            return relationshipValidation.Status switch
            {
                ApplicationResultStatus.NotFound => ApplicationResult<SurveyAssignmentDto>.NotFound(
                    relationshipValidation.Errors.First().Message),
                _ => ApplicationResult<SurveyAssignmentDto>.Validation(relationshipValidation.Errors)
            };
        }

        var expectedRespondentCount = relationshipValidation.Value;

        var duplicateExists = await _dbContext.SurveyAssignments
            .AsNoTracking()
            .AnyAsync(
                assignment =>
                    assignment.SurveyId == request.SurveyId
                    && assignment.CareerId == request.CareerId
                    && assignment.SubjectId == request.SubjectId
                    && assignment.AcademicCycleId == request.AcademicCycleId
                    && assignment.TeacherSubjectAssignmentId == request.TeacherSubjectAssignmentId,
                cancellationToken);

        if (duplicateExists)
        {
            return ApplicationResult<SurveyAssignmentDto>.Conflict(
                "A survey assignment for the same academic context already exists.");
        }

        var now = DateTimeOffset.UtcNow;
        var assignment = new SurveyAssignment(
            Guid.NewGuid(),
            request.SurveyId,
            request.CareerId,
            request.SubjectId,
            request.AcademicCycleId,
            request.TeacherSubjectAssignmentId,
            expectedRespondentCount,
            now);

        _dbContext.SurveyAssignments.Add(assignment);

        var saveResult = await SaveChangesAsync(
            "The survey assignment could not be saved.",
            cancellationToken);

        if (saveResult.Status != ApplicationResultStatus.Success)
        {
            return saveResult.Status switch
            {
                ApplicationResultStatus.Validation => ApplicationResult<SurveyAssignmentDto>.Validation(
                    saveResult.Errors),
                ApplicationResultStatus.Conflict => ApplicationResult<SurveyAssignmentDto>.Conflict(
                    saveResult.Errors.First().Message),
                _ => ApplicationResult<SurveyAssignmentDto>.Failure(saveResult.Errors.First().Message)
            };
        }

        return await GetAssignmentByIdAsync(assignment.Id, cancellationToken);
    }

    public Task<ApplicationResult> ActivateAssignmentAsync(
        Guid id,
        CancellationToken cancellationToken) =>
        ChangeAssignmentStateAsync(id, isActive: true, cancellationToken);

    public Task<ApplicationResult> DeactivateAssignmentAsync(
        Guid id,
        CancellationToken cancellationToken) =>
        ChangeAssignmentStateAsync(id, isActive: false, cancellationToken);

    private async Task<ApplicationResult<int?>> ValidateRelatedEntitiesAsync(
        CreateSurveyAssignmentRequest request,
        CancellationToken cancellationToken)
    {
        var survey = await _dbContext.Surveys
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == request.SurveyId, cancellationToken);

        if (survey is null)
        {
            return ApplicationResult<int?>.NotFound("Survey was not found.");
        }

        if (!survey.IsActive)
        {
            return ApplicationResult<int?>.Validation([
                new ApplicationError("SurveyAssignment.SurveyInactive", "Survey must be active.")
            ]);
        }

        if (survey.Status != SurveyStatus.Published)
        {
            return ApplicationResult<int?>.Validation([
                new ApplicationError("SurveyAssignment.SurveyNotPublished", "Survey must be published.")
            ]);
        }

        var career = await _dbContext.Careers
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == request.CareerId, cancellationToken);

        if (career is null)
        {
            return ApplicationResult<int?>.NotFound("Career was not found.");
        }

        if (!career.IsActive)
        {
            return ApplicationResult<int?>.Validation([
                new ApplicationError("SurveyAssignment.CareerInactive", "Career must be active.")
            ]);
        }

        var subject = await _dbContext.Subjects
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == request.SubjectId, cancellationToken);

        if (subject is null)
        {
            return ApplicationResult<int?>.NotFound("Subject was not found.");
        }

        if (!subject.IsActive)
        {
            return ApplicationResult<int?>.Validation([
                new ApplicationError("SurveyAssignment.SubjectInactive", "Subject must be active.")
            ]);
        }

        if (subject.CareerId != request.CareerId)
        {
            return ApplicationResult<int?>.Validation([
                new ApplicationError(
                    "SurveyAssignment.SubjectCareerMismatch",
                    "Subject must belong to the selected career.")
            ]);
        }

        var academicCycle = await _dbContext.AcademicCycles
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == request.AcademicCycleId, cancellationToken);

        if (academicCycle is null)
        {
            return ApplicationResult<int?>.NotFound("Academic cycle was not found.");
        }

        if (!academicCycle.IsActive)
        {
            return ApplicationResult<int?>.Validation([
                new ApplicationError("SurveyAssignment.AcademicCycleInactive", "Academic cycle must be active.")
            ]);
        }

        var teacherSubjectAssignment = await _dbContext.TeacherSubjectAssignments
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == request.TeacherSubjectAssignmentId, cancellationToken);

        if (teacherSubjectAssignment is null)
        {
            return ApplicationResult<int?>.NotFound("Teacher-subject assignment was not found.");
        }

        if (!teacherSubjectAssignment.IsActive)
        {
            return ApplicationResult<int?>.Validation([
                new ApplicationError(
                    "SurveyAssignment.TeacherSubjectAssignmentInactive",
                    "Teacher-subject assignment must be active.")
            ]);
        }

        if (teacherSubjectAssignment.SubjectId != request.SubjectId)
        {
            return ApplicationResult<int?>.Validation([
                new ApplicationError(
                    "SurveyAssignment.TeacherSubjectAssignmentSubjectMismatch",
                    "Teacher-subject assignment must belong to the selected subject.")
            ]);
        }

        if (teacherSubjectAssignment.AcademicCycleId != request.AcademicCycleId)
        {
            return ApplicationResult<int?>.Validation([
                new ApplicationError(
                    "SurveyAssignment.TeacherSubjectAssignmentCycleMismatch",
                    "Teacher-subject assignment must belong to the selected academic cycle.")
            ]);
        }

        if (survey.Target == SurveyTarget.Student)
        {
            var enrollment = await _dbContext.SubjectEnrollments
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    item => item.SubjectId == request.SubjectId
                        && item.AcademicCycleId == request.AcademicCycleId,
                    cancellationToken);

            if (enrollment is null)
            {
                return ApplicationResult<int?>.Validation([
                    new ApplicationError(
                        "Subject.EnrollmentRequired",
                        "Debe indicar la cantidad de alumnos inscriptos en la materia para el ciclo lectivo seleccionado.")
                ]);
            }

            return ApplicationResult<int?>.Success(enrollment.EnrolledStudentCount);
        }

        return ApplicationResult<int?>.Success(null);
    }

    private async Task<ApplicationResult> ChangeAssignmentStateAsync(
        Guid id,
        bool isActive,
        CancellationToken cancellationToken)
    {
        var assignment = await _dbContext.SurveyAssignments
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (assignment is null)
        {
            return ApplicationResult.NotFound("Survey assignment was not found.");
        }

        try
        {
            if (isActive)
            {
                assignment.Activate(DateTimeOffset.UtcNow);
            }
            else
            {
                assignment.Deactivate(DateTimeOffset.UtcNow);
            }

            await _dbContext.SaveChangesAsync(cancellationToken);

            return ApplicationResult.Success();
        }
        catch (DomainException exception)
        {
            return ApplicationResult.Validation([
                new ApplicationError("SurveyAssignment.Validation", exception.Message)
            ]);
        }
        catch (DbUpdateException)
        {
            return ApplicationResult.Failure("The survey assignment could not be saved.");
        }
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
                new ApplicationError("SurveyAssignment.Validation", exception.Message)
            ]);
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
        {
            return ApplicationResult.Conflict(
                "A survey assignment for the same academic context already exists.");
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

    private static Expression<Func<SurveyAssignment, SurveyAssignmentDto>> MapDtoExpression()
    {
        return assignment => new SurveyAssignmentDto(
            assignment.Id,
            assignment.SurveyId,
            assignment.Survey.Title,
            assignment.Survey.Status.ToString(),
            assignment.CareerId,
            assignment.Career.Name,
            assignment.SubjectId,
            assignment.Subject.Name,
            assignment.AcademicCycleId,
            assignment.AcademicCycle.Year,
            assignment.AcademicCycle.Period.ToString(),
            assignment.TeacherSubjectAssignmentId,
            assignment.TeacherSubjectAssignment.TeacherId,
            assignment.TeacherSubjectAssignment.Teacher.FirstName
                + " "
                + assignment.TeacherSubjectAssignment.Teacher.LastName,
            assignment.TeacherSubjectAssignment.TeachingRole,
            assignment.IsActive,
            assignment.CreatedAtUtc,
            assignment.UpdatedAtUtc,
            assignment.ExpectedRespondentCount);
    }
}
