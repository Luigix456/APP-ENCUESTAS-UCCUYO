using AcademicSurveySystem.Application.Academic;
using AcademicSurveySystem.Application.Academic.AcademicCycles;
using AcademicSurveySystem.Application.Academic.Careers;
using AcademicSurveySystem.Application.Academic.Common;
using AcademicSurveySystem.Application.Academic.Subjects;
using AcademicSurveySystem.Application.Academic.Teachers;
using AcademicSurveySystem.Application.Academic.TeacherSubjectAssignments;
using AcademicSurveySystem.Application.Common.Results;
using AcademicSurveySystem.Domain.Academic.Entities;
using AcademicSurveySystem.Domain.Academic.Enums;
using AcademicSurveySystem.Domain.Common;
using AcademicSurveySystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace AcademicSurveySystem.Infrastructure.Academic;

public sealed class AcademicCatalogService : IAcademicCatalogService
{
    private readonly ApplicationDbContext _dbContext;

    public AcademicCatalogService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ApplicationResult<IReadOnlyCollection<CareerDto>>> GetCareersAsync(
        bool includeInactive,
        CancellationToken cancellationToken)
    {
        var careers = await _dbContext.Careers
            .AsNoTracking()
            .Where(career => includeInactive || career.IsActive)
            .OrderBy(career => career.Name)
            .Select(career => MapCareer(career))
            .ToArrayAsync(cancellationToken);

        return ApplicationResult<IReadOnlyCollection<CareerDto>>.Success(careers);
    }

    public async Task<ApplicationResult<CareerDto>> GetCareerByIdAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var career = await _dbContext.Careers
            .AsNoTracking()
            .Where(item => item.Id == id)
            .Select(item => MapCareer(item))
            .SingleOrDefaultAsync(cancellationToken);

        return career is null
            ? ApplicationResult<CareerDto>.NotFound("Career was not found.")
            : ApplicationResult<CareerDto>.Success(career);
    }

    public async Task<ApplicationResult<CareerDto>> CreateCareerAsync(
        CreateCareerRequest request,
        CancellationToken cancellationToken)
    {
        var validationErrors = request.Validate();

        if (validationErrors.Count > 0)
        {
            return ApplicationResult<CareerDto>.Validation(validationErrors);
        }

        var normalizedCode = request.Code!.Trim().ToLowerInvariant();

        if (await _dbContext.Careers.AnyAsync(career => career.Code == normalizedCode, cancellationToken))
        {
            return ApplicationResult<CareerDto>.Conflict("A career with the same code already exists.");
        }

        if (!Enum.TryParse<CareerType>(request.Type!.Trim(), ignoreCase: true, out var type))
        {
            return ApplicationResult<CareerDto>.Validation([
                new ApplicationError("Career.TypeInvalid", "Type is invalid.")
            ]);
        }

        try
        {
            var career = new Career(
                Guid.NewGuid(),
                request.Code,
                request.Name!,
                type,
                DateTimeOffset.UtcNow);

            _dbContext.Careers.Add(career);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return ApplicationResult<CareerDto>.Success(MapCareer(career));
        }
        catch (DomainException exception)
        {
            return ApplicationResult<CareerDto>.Validation([
                new ApplicationError("Career.Validation", exception.Message)
            ]);
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
        {
            return ApplicationResult<CareerDto>.Conflict("A career with the same code already exists.");
        }
        catch (DbUpdateException)
        {
            return ApplicationResult<CareerDto>.Failure("The career could not be saved.");
        }
    }

    public async Task<ApplicationResult> UpdateCareerAsync(
        Guid id,
        UpdateCareerRequest request,
        CancellationToken cancellationToken)
    {
        var validationErrors = request.Validate();

        if (validationErrors.Count > 0)
        {
            return ApplicationResult.Validation(validationErrors);
        }

        var career = await _dbContext.Careers.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (career is null)
        {
            return ApplicationResult.NotFound("Career was not found.");
        }

        if (!Enum.TryParse<CareerType>(request.Type!.Trim(), ignoreCase: true, out var type))
        {
            return ApplicationResult.Validation([
                new ApplicationError("Career.TypeInvalid", "Type is invalid.")
            ]);
        }

        try
        {
            var now = DateTimeOffset.UtcNow;
            career.UpdateName(request.Name!, now);
            career.UpdateType(type, now);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return ApplicationResult.Success();
        }
        catch (DomainException exception)
        {
            return ApplicationResult.Validation([
                new ApplicationError("Career.Validation", exception.Message)
            ]);
        }
        catch (DbUpdateException)
        {
            return ApplicationResult.Failure("The career could not be saved.");
        }
    }

    public Task<ApplicationResult> ActivateCareerAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        return ChangeCareerStateAsync(id, activate: true, cancellationToken);
    }

    public Task<ApplicationResult> DeactivateCareerAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        return ChangeCareerStateAsync(id, activate: false, cancellationToken);
    }

    public async Task<ApplicationResult<IReadOnlyCollection<AcademicCycleDto>>> GetAcademicCyclesAsync(
        bool includeInactive,
        CancellationToken cancellationToken)
    {
        var cycles = await _dbContext.AcademicCycles
            .AsNoTracking()
            .Where(cycle => includeInactive || cycle.IsActive)
            .OrderByDescending(cycle => cycle.Year)
            .ThenBy(cycle => cycle.Period)
            .Select(cycle => MapAcademicCycle(cycle))
            .ToArrayAsync(cancellationToken);

        return ApplicationResult<IReadOnlyCollection<AcademicCycleDto>>.Success(cycles);
    }

    public async Task<ApplicationResult<AcademicCycleDto>> GetAcademicCycleByIdAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var cycle = await _dbContext.AcademicCycles
            .AsNoTracking()
            .Where(item => item.Id == id)
            .Select(item => MapAcademicCycle(item))
            .SingleOrDefaultAsync(cancellationToken);

        return cycle is null
            ? ApplicationResult<AcademicCycleDto>.NotFound("Academic cycle was not found.")
            : ApplicationResult<AcademicCycleDto>.Success(cycle);
    }

    public async Task<ApplicationResult<AcademicCycleDto>> CreateAcademicCycleAsync(
        CreateAcademicCycleRequest request,
        CancellationToken cancellationToken)
    {
        var validationErrors = request.Validate();

        if (validationErrors.Count > 0)
        {
            return ApplicationResult<AcademicCycleDto>.Validation(validationErrors);
        }

        if (!Enum.TryParse<AcademicCyclePeriod>(request.Period!.Trim(), ignoreCase: true, out var period))
        {
            return ApplicationResult<AcademicCycleDto>.Validation([
                new ApplicationError("AcademicCycle.PeriodInvalid", "Period is invalid.")
            ]);
        }

        if (await _dbContext.AcademicCycles.AnyAsync(
            cycle => cycle.Year == request.Year!.Value && cycle.Period == period,
            cancellationToken))
        {
            return ApplicationResult<AcademicCycleDto>.Conflict(
                "An academic cycle with the same year and period already exists.");
        }

        try
        {
            var cycle = new AcademicCycle(
                Guid.NewGuid(),
                request.Year!.Value,
                period,
                request.StartDate!.Value,
                request.EndDate!.Value,
                DateTimeOffset.UtcNow);

            _dbContext.AcademicCycles.Add(cycle);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return ApplicationResult<AcademicCycleDto>.Success(MapAcademicCycle(cycle));
        }
        catch (DomainException exception)
        {
            return ApplicationResult<AcademicCycleDto>.Validation([
                new ApplicationError("AcademicCycle.Validation", exception.Message)
            ]);
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
        {
            return ApplicationResult<AcademicCycleDto>.Conflict(
                "An academic cycle with the same year and period already exists.");
        }
        catch (DbUpdateException)
        {
            return ApplicationResult<AcademicCycleDto>.Failure("The academic cycle could not be saved.");
        }
    }

    public async Task<ApplicationResult> UpdateAcademicCycleAsync(
        Guid id,
        UpdateAcademicCycleRequest request,
        CancellationToken cancellationToken)
    {
        var validationErrors = request.Validate();

        if (validationErrors.Count > 0)
        {
            return ApplicationResult.Validation(validationErrors);
        }

        var cycle = await _dbContext.AcademicCycles.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (cycle is null)
        {
            return ApplicationResult.NotFound("Academic cycle was not found.");
        }

        if (!Enum.TryParse<AcademicCyclePeriod>(request.Period!.Trim(), ignoreCase: true, out var period))
        {
            return ApplicationResult.Validation([
                new ApplicationError("AcademicCycle.PeriodInvalid", "Period is invalid.")
            ]);
        }

        if (await _dbContext.AcademicCycles.AnyAsync(
            item => item.Id != id && item.Year == cycle.Year && item.Period == period,
            cancellationToken))
        {
            return ApplicationResult.Conflict(
                "An academic cycle with the same year and period already exists.");
        }

        try
        {
            cycle.UpdatePeriodAndDates(
                period,
                request.StartDate!.Value,
                request.EndDate!.Value,
                DateTimeOffset.UtcNow);

            await _dbContext.SaveChangesAsync(cancellationToken);

            return ApplicationResult.Success();
        }
        catch (DomainException exception)
        {
            return ApplicationResult.Validation([
                new ApplicationError("AcademicCycle.Validation", exception.Message)
            ]);
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
        {
            return ApplicationResult.Conflict(
                "An academic cycle with the same year and period already exists.");
        }
        catch (DbUpdateException)
        {
            return ApplicationResult.Failure("The academic cycle could not be saved.");
        }
    }

    public Task<ApplicationResult> ActivateAcademicCycleAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        return ChangeAcademicCycleStateAsync(id, activate: true, cancellationToken);
    }

    public Task<ApplicationResult> DeactivateAcademicCycleAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        return ChangeAcademicCycleStateAsync(id, activate: false, cancellationToken);
    }

    public async Task<ApplicationResult<IReadOnlyCollection<SubjectDto>>> GetSubjectsAsync(
        bool includeInactive,
        Guid? careerId,
        CancellationToken cancellationToken)
    {
        var subjects = await _dbContext.Subjects
            .AsNoTracking()
            .Where(subject => includeInactive || subject.IsActive)
            .Where(subject => careerId == null || subject.CareerId == careerId)
            .OrderBy(subject => subject.Career.Name)
            .ThenBy(subject => subject.Year)
            .ThenBy(subject => subject.Name)
            .Select(subject => new SubjectDto(
                subject.Id,
                subject.CareerId,
                subject.Career.Name,
                subject.Code,
                subject.Name,
                subject.Year,
                subject.Period.ToString(),
                subject.IsActive,
                subject.CreatedAtUtc,
                subject.UpdatedAtUtc))
            .ToArrayAsync(cancellationToken);

        return ApplicationResult<IReadOnlyCollection<SubjectDto>>.Success(subjects);
    }

    public async Task<ApplicationResult<SubjectDto>> GetSubjectByIdAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var subject = await _dbContext.Subjects
            .AsNoTracking()
            .Where(item => item.Id == id)
            .Select(item => new SubjectDto(
                item.Id,
                item.CareerId,
                item.Career.Name,
                item.Code,
                item.Name,
                item.Year,
                item.Period.ToString(),
                item.IsActive,
                item.CreatedAtUtc,
                item.UpdatedAtUtc))
            .SingleOrDefaultAsync(cancellationToken);

        return subject is null
            ? ApplicationResult<SubjectDto>.NotFound("Subject was not found.")
            : ApplicationResult<SubjectDto>.Success(subject);
    }

    public async Task<ApplicationResult<SubjectDto>> CreateSubjectAsync(
        CreateSubjectRequest request,
        CancellationToken cancellationToken)
    {
        var validationErrors = request.Validate();

        if (validationErrors.Count > 0)
        {
            return ApplicationResult<SubjectDto>.Validation(validationErrors);
        }

        var careerId = request.CareerId!.Value;

        var career = await _dbContext.Careers
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == careerId, cancellationToken);

        if (career is null)
        {
            return ApplicationResult<SubjectDto>.NotFound("Career was not found.");
        }

        var normalizedCode = request.Code!.Trim().ToLowerInvariant();

        if (await _dbContext.Subjects.AnyAsync(
            subject => subject.CareerId == careerId && subject.Code == normalizedCode,
            cancellationToken))
        {
            return ApplicationResult<SubjectDto>.Conflict(
                "A subject with the same code already exists for this career.");
        }

        if (!Enum.TryParse<SubjectPeriod>(request.Period!.Trim(), ignoreCase: true, out var period))
        {
            return ApplicationResult<SubjectDto>.Validation([
                new ApplicationError("Subject.PeriodInvalid", "Period is invalid.")
            ]);
        }

        try
        {
            var subject = new Subject(
                Guid.NewGuid(),
                careerId,
                request.Code,
                request.Name!,
                request.Year!.Value,
                period,
                DateTimeOffset.UtcNow);

            _dbContext.Subjects.Add(subject);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return ApplicationResult<SubjectDto>.Success(MapSubject(subject, career.Name));
        }
        catch (DomainException exception)
        {
            return ApplicationResult<SubjectDto>.Validation([
                new ApplicationError("Subject.Validation", exception.Message)
            ]);
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
        {
            return ApplicationResult<SubjectDto>.Conflict(
                "A subject with the same code already exists for this career.");
        }
        catch (DbUpdateException)
        {
            return ApplicationResult<SubjectDto>.Failure("The subject could not be saved.");
        }
    }

    public async Task<ApplicationResult> UpdateSubjectAsync(
        Guid id,
        UpdateSubjectRequest request,
        CancellationToken cancellationToken)
    {
        var validationErrors = request.Validate();

        if (validationErrors.Count > 0)
        {
            return ApplicationResult.Validation(validationErrors);
        }

        var subject = await _dbContext.Subjects.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (subject is null)
        {
            return ApplicationResult.NotFound("Subject was not found.");
        }

        if (!Enum.TryParse<SubjectPeriod>(request.Period!.Trim(), ignoreCase: true, out var period))
        {
            return ApplicationResult.Validation([
                new ApplicationError("Subject.PeriodInvalid", "Period is invalid.")
            ]);
        }

        try
        {
            var now = DateTimeOffset.UtcNow;
            subject.UpdateName(request.Name!, now);
            subject.UpdateYearAndPeriod(request.Year!.Value, period, now);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return ApplicationResult.Success();
        }
        catch (DomainException exception)
        {
            return ApplicationResult.Validation([
                new ApplicationError("Subject.Validation", exception.Message)
            ]);
        }
        catch (DbUpdateException)
        {
            return ApplicationResult.Failure("The subject could not be saved.");
        }
    }

    public Task<ApplicationResult> ActivateSubjectAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        return ChangeSubjectStateAsync(id, activate: true, cancellationToken);
    }

    public Task<ApplicationResult> DeactivateSubjectAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        return ChangeSubjectStateAsync(id, activate: false, cancellationToken);
    }

    public async Task<ApplicationResult<IReadOnlyCollection<TeacherDto>>> GetTeachersAsync(
        bool includeInactive,
        CancellationToken cancellationToken)
    {
        var teachers = await _dbContext.Teachers
            .AsNoTracking()
            .Where(teacher => includeInactive || teacher.IsActive)
            .OrderBy(teacher => teacher.LastName)
            .ThenBy(teacher => teacher.FirstName)
            .Select(teacher => MapTeacher(teacher))
            .ToArrayAsync(cancellationToken);

        return ApplicationResult<IReadOnlyCollection<TeacherDto>>.Success(teachers);
    }

    public async Task<ApplicationResult<TeacherDto>> GetTeacherByIdAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var teacher = await _dbContext.Teachers
            .AsNoTracking()
            .Where(item => item.Id == id)
            .Select(item => MapTeacher(item))
            .SingleOrDefaultAsync(cancellationToken);

        return teacher is null
            ? ApplicationResult<TeacherDto>.NotFound("Teacher was not found.")
            : ApplicationResult<TeacherDto>.Success(teacher);
    }

    public async Task<ApplicationResult<TeacherDto>> CreateTeacherAsync(
        CreateTeacherRequest request,
        CancellationToken cancellationToken)
    {
        var validationErrors = request.Validate();

        if (validationErrors.Count > 0)
        {
            return ApplicationResult<TeacherDto>.Validation(validationErrors);
        }

        var normalizedEmail = NormalizeEmailForComparison(request.Email);

        if (normalizedEmail is not null && await _dbContext.Teachers.AnyAsync(
            teacher => teacher.NormalizedEmail == normalizedEmail,
            cancellationToken))
        {
            return ApplicationResult<TeacherDto>.Conflict("A teacher with the same email already exists.");
        }

        try
        {
            var teacher = new Teacher(
                Guid.NewGuid(),
                request.FirstName!,
                request.LastName!,
                request.Email,
                DateTimeOffset.UtcNow);

            _dbContext.Teachers.Add(teacher);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return ApplicationResult<TeacherDto>.Success(MapTeacher(teacher));
        }
        catch (DomainException exception)
        {
            return ApplicationResult<TeacherDto>.Validation([
                new ApplicationError("Teacher.Validation", exception.Message)
            ]);
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
        {
            return ApplicationResult<TeacherDto>.Conflict("A teacher with the same email already exists.");
        }
        catch (DbUpdateException)
        {
            return ApplicationResult<TeacherDto>.Failure("The teacher could not be saved.");
        }
    }

    public async Task<ApplicationResult> UpdateTeacherAsync(
        Guid id,
        UpdateTeacherRequest request,
        CancellationToken cancellationToken)
    {
        var validationErrors = request.Validate();

        if (validationErrors.Count > 0)
        {
            return ApplicationResult.Validation(validationErrors);
        }

        var teacher = await _dbContext.Teachers.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (teacher is null)
        {
            return ApplicationResult.NotFound("Teacher was not found.");
        }

        var normalizedEmail = NormalizeEmailForComparison(request.Email);

        if (normalizedEmail is not null && await _dbContext.Teachers.AnyAsync(
            item => item.Id != id && item.NormalizedEmail == normalizedEmail,
            cancellationToken))
        {
            return ApplicationResult.Conflict("A teacher with the same email already exists.");
        }

        try
        {
            var now = DateTimeOffset.UtcNow;
            teacher.UpdateName(request.FirstName!, request.LastName!, now);

            if (string.IsNullOrWhiteSpace(request.Email))
            {
                teacher.RemoveEmail(now);
            }
            else
            {
                teacher.ChangeEmail(request.Email, now);
            }

            await _dbContext.SaveChangesAsync(cancellationToken);

            return ApplicationResult.Success();
        }
        catch (DomainException exception)
        {
            return ApplicationResult.Validation([
                new ApplicationError("Teacher.Validation", exception.Message)
            ]);
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
        {
            return ApplicationResult.Conflict("A teacher with the same email already exists.");
        }
        catch (DbUpdateException)
        {
            return ApplicationResult.Failure("The teacher could not be saved.");
        }
    }

    public Task<ApplicationResult> ActivateTeacherAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        return ChangeTeacherStateAsync(id, activate: true, cancellationToken);
    }

    public Task<ApplicationResult> DeactivateTeacherAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        return ChangeTeacherStateAsync(id, activate: false, cancellationToken);
    }

    public async Task<ApplicationResult<IReadOnlyCollection<TeacherSubjectAssignmentDto>>>
        GetTeacherSubjectAssignmentsAsync(
            bool includeInactive,
            Guid? teacherId,
            Guid? subjectId,
            Guid? academicCycleId,
            CancellationToken cancellationToken)
    {
        var assignments = await _dbContext.TeacherSubjectAssignments
            .AsNoTracking()
            .Where(assignment => includeInactive || assignment.IsActive)
            .Where(assignment => teacherId == null || assignment.TeacherId == teacherId)
            .Where(assignment => subjectId == null || assignment.SubjectId == subjectId)
            .Where(assignment => academicCycleId == null || assignment.AcademicCycleId == academicCycleId)
            .OrderBy(assignment => assignment.Teacher.LastName)
            .ThenBy(assignment => assignment.Teacher.FirstName)
            .ThenBy(assignment => assignment.Subject.Career.Name)
            .ThenBy(assignment => assignment.Subject.Year)
            .ThenBy(assignment => assignment.Subject.Name)
            .ThenByDescending(assignment => assignment.AcademicCycle.Year)
            .ThenBy(assignment => assignment.AcademicCycle.Period)
            .Select(assignment => new TeacherSubjectAssignmentDto(
                assignment.Id,
                assignment.TeacherId,
                assignment.Teacher.FirstName + " " + assignment.Teacher.LastName,
                assignment.SubjectId,
                assignment.Subject.Name,
                assignment.Subject.CareerId,
                assignment.Subject.Career.Name,
                assignment.AcademicCycleId,
                assignment.AcademicCycle.Year,
                assignment.AcademicCycle.Period.ToString(),
                assignment.TeachingRole,
                assignment.IsActive,
                assignment.CreatedAtUtc,
                assignment.UpdatedAtUtc))
            .ToArrayAsync(cancellationToken);

        return ApplicationResult<IReadOnlyCollection<TeacherSubjectAssignmentDto>>.Success(assignments);
    }

    public async Task<ApplicationResult<TeacherSubjectAssignmentDto>> GetTeacherSubjectAssignmentByIdAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var assignment = await _dbContext.TeacherSubjectAssignments
            .AsNoTracking()
            .Where(item => item.Id == id)
            .Select(item => new TeacherSubjectAssignmentDto(
                item.Id,
                item.TeacherId,
                item.Teacher.FirstName + " " + item.Teacher.LastName,
                item.SubjectId,
                item.Subject.Name,
                item.Subject.CareerId,
                item.Subject.Career.Name,
                item.AcademicCycleId,
                item.AcademicCycle.Year,
                item.AcademicCycle.Period.ToString(),
                item.TeachingRole,
                item.IsActive,
                item.CreatedAtUtc,
                item.UpdatedAtUtc))
            .SingleOrDefaultAsync(cancellationToken);

        return assignment is null
            ? ApplicationResult<TeacherSubjectAssignmentDto>.NotFound(
                "Teacher subject assignment was not found.")
            : ApplicationResult<TeacherSubjectAssignmentDto>.Success(assignment);
    }

    public async Task<ApplicationResult<TeacherSubjectAssignmentDto>> CreateTeacherSubjectAssignmentAsync(
        CreateTeacherSubjectAssignmentRequest request,
        CancellationToken cancellationToken)
    {
        var validationErrors = request.Validate();

        if (validationErrors.Count > 0)
        {
            return ApplicationResult<TeacherSubjectAssignmentDto>.Validation(validationErrors);
        }

        var teacherId = request.TeacherId!.Value;
        var subjectId = request.SubjectId!.Value;
        var academicCycleId = request.AcademicCycleId!.Value;

        var teacher = await _dbContext.Teachers
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == teacherId, cancellationToken);

        if (teacher is null)
        {
            return ApplicationResult<TeacherSubjectAssignmentDto>.NotFound("Teacher was not found.");
        }

        var subject = await _dbContext.Subjects
            .AsNoTracking()
            .Include(item => item.Career)
            .SingleOrDefaultAsync(item => item.Id == subjectId, cancellationToken);

        if (subject is null)
        {
            return ApplicationResult<TeacherSubjectAssignmentDto>.NotFound("Subject was not found.");
        }

        var academicCycle = await _dbContext.AcademicCycles
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == academicCycleId, cancellationToken);

        if (academicCycle is null)
        {
            return ApplicationResult<TeacherSubjectAssignmentDto>.NotFound("Academic cycle was not found.");
        }

        if (await _dbContext.TeacherSubjectAssignments.AnyAsync(
            item => item.TeacherId == teacherId
                && item.SubjectId == subjectId
                && item.AcademicCycleId == academicCycleId,
            cancellationToken))
        {
            return ApplicationResult<TeacherSubjectAssignmentDto>.Conflict(
                "A teacher subject assignment with the same teacher, subject and academic cycle already exists.");
        }

        try
        {
            var assignment = new TeacherSubjectAssignment(
                Guid.NewGuid(),
                teacherId,
                subjectId,
                academicCycleId,
                request.TeachingRole!,
                DateTimeOffset.UtcNow);

            _dbContext.TeacherSubjectAssignments.Add(assignment);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return ApplicationResult<TeacherSubjectAssignmentDto>.Success(
                MapTeacherSubjectAssignment(assignment, teacher, subject, academicCycle));
        }
        catch (DomainException exception)
        {
            return ApplicationResult<TeacherSubjectAssignmentDto>.Validation([
                new ApplicationError("TeacherSubjectAssignment.Validation", exception.Message)
            ]);
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
        {
            return ApplicationResult<TeacherSubjectAssignmentDto>.Conflict(
                "A teacher subject assignment with the same teacher, subject and academic cycle already exists.");
        }
        catch (DbUpdateException)
        {
            return ApplicationResult<TeacherSubjectAssignmentDto>.Failure(
                "The teacher subject assignment could not be saved.");
        }
    }

    public async Task<ApplicationResult> UpdateTeacherSubjectAssignmentAsync(
        Guid id,
        UpdateTeacherSubjectAssignmentRequest request,
        CancellationToken cancellationToken)
    {
        var validationErrors = request.Validate();

        if (validationErrors.Count > 0)
        {
            return ApplicationResult.Validation(validationErrors);
        }

        var assignment = await _dbContext.TeacherSubjectAssignments
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (assignment is null)
        {
            return ApplicationResult.NotFound("Teacher subject assignment was not found.");
        }

        try
        {
            assignment.UpdateTeachingRole(request.TeachingRole!, DateTimeOffset.UtcNow);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return ApplicationResult.Success();
        }
        catch (DomainException exception)
        {
            return ApplicationResult.Validation([
                new ApplicationError("TeacherSubjectAssignment.Validation", exception.Message)
            ]);
        }
        catch (DbUpdateException)
        {
            return ApplicationResult.Failure("The teacher subject assignment could not be saved.");
        }
    }

    public Task<ApplicationResult> ActivateTeacherSubjectAssignmentAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        return ChangeTeacherSubjectAssignmentStateAsync(id, activate: true, cancellationToken);
    }

    public Task<ApplicationResult> DeactivateTeacherSubjectAssignmentAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        return ChangeTeacherSubjectAssignmentStateAsync(id, activate: false, cancellationToken);
    }

    private async Task<ApplicationResult> ChangeCareerStateAsync(
        Guid id,
        bool activate,
        CancellationToken cancellationToken)
    {
        var career = await _dbContext.Careers.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (career is null)
        {
            return ApplicationResult.NotFound("Career was not found.");
        }

        var now = DateTimeOffset.UtcNow;

        if (activate)
        {
            career.Activate(now);
        }
        else
        {
            career.Deactivate(now);
        }

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);

            return ApplicationResult.Success();
        }
        catch (DbUpdateException)
        {
            return ApplicationResult.Failure("The career could not be saved.");
        }
    }

    private async Task<ApplicationResult> ChangeAcademicCycleStateAsync(
        Guid id,
        bool activate,
        CancellationToken cancellationToken)
    {
        var cycle = await _dbContext.AcademicCycles.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (cycle is null)
        {
            return ApplicationResult.NotFound("Academic cycle was not found.");
        }

        var now = DateTimeOffset.UtcNow;

        if (activate)
        {
            cycle.Activate(now);
        }
        else
        {
            cycle.Deactivate(now);
        }

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);

            return ApplicationResult.Success();
        }
        catch (DbUpdateException)
        {
            return ApplicationResult.Failure("The academic cycle could not be saved.");
        }
    }

    private async Task<ApplicationResult> ChangeSubjectStateAsync(
        Guid id,
        bool activate,
        CancellationToken cancellationToken)
    {
        var subject = await _dbContext.Subjects.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (subject is null)
        {
            return ApplicationResult.NotFound("Subject was not found.");
        }

        var now = DateTimeOffset.UtcNow;

        if (activate)
        {
            subject.Activate(now);
        }
        else
        {
            subject.Deactivate(now);
        }

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);

            return ApplicationResult.Success();
        }
        catch (DbUpdateException)
        {
            return ApplicationResult.Failure("The subject could not be saved.");
        }
    }

    private async Task<ApplicationResult> ChangeTeacherStateAsync(
        Guid id,
        bool activate,
        CancellationToken cancellationToken)
    {
        var teacher = await _dbContext.Teachers.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (teacher is null)
        {
            return ApplicationResult.NotFound("Teacher was not found.");
        }

        var now = DateTimeOffset.UtcNow;

        if (activate)
        {
            teacher.Activate(now);
        }
        else
        {
            teacher.Deactivate(now);
        }

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);

            return ApplicationResult.Success();
        }
        catch (DbUpdateException)
        {
            return ApplicationResult.Failure("The teacher could not be saved.");
        }
    }

    private async Task<ApplicationResult> ChangeTeacherSubjectAssignmentStateAsync(
        Guid id,
        bool activate,
        CancellationToken cancellationToken)
    {
        var assignment = await _dbContext.TeacherSubjectAssignments
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (assignment is null)
        {
            return ApplicationResult.NotFound("Teacher subject assignment was not found.");
        }

        var now = DateTimeOffset.UtcNow;

        if (activate)
        {
            assignment.Activate(now);
        }
        else
        {
            assignment.Deactivate(now);
        }

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);

            return ApplicationResult.Success();
        }
        catch (DbUpdateException)
        {
            return ApplicationResult.Failure("The teacher subject assignment could not be saved.");
        }
    }

    private static CareerDto MapCareer(Career career)
    {
        return new CareerDto(
            career.Id,
            career.Code,
            career.Name,
            career.Type.ToString(),
            career.IsActive,
            career.CreatedAtUtc,
            career.UpdatedAtUtc);
    }

    private static AcademicCycleDto MapAcademicCycle(AcademicCycle cycle)
    {
        return new AcademicCycleDto(
            cycle.Id,
            cycle.Year,
            cycle.Period.ToString(),
            cycle.StartDate,
            cycle.EndDate,
            cycle.IsActive,
            cycle.CreatedAtUtc,
            cycle.UpdatedAtUtc);
    }

    private static SubjectDto MapSubject(Subject subject, string careerName)
    {
        return new SubjectDto(
            subject.Id,
            subject.CareerId,
            careerName,
            subject.Code,
            subject.Name,
            subject.Year,
            subject.Period.ToString(),
            subject.IsActive,
            subject.CreatedAtUtc,
            subject.UpdatedAtUtc);
    }

    private static TeacherDto MapTeacher(Teacher teacher)
    {
        return new TeacherDto(
            teacher.Id,
            teacher.FirstName,
            teacher.LastName,
            teacher.Email,
            teacher.IsActive,
            teacher.CreatedAtUtc,
            teacher.UpdatedAtUtc);
    }

    private static TeacherSubjectAssignmentDto MapTeacherSubjectAssignment(
        TeacherSubjectAssignment assignment,
        Teacher teacher,
        Subject subject,
        AcademicCycle academicCycle)
    {
        return new TeacherSubjectAssignmentDto(
            assignment.Id,
            assignment.TeacherId,
            $"{teacher.FirstName} {teacher.LastName}",
            assignment.SubjectId,
            subject.Name,
            subject.CareerId,
            subject.Career.Name,
            assignment.AcademicCycleId,
            academicCycle.Year,
            academicCycle.Period.ToString(),
            assignment.TeachingRole,
            assignment.IsActive,
            assignment.CreatedAtUtc,
            assignment.UpdatedAtUtc);
    }

    private static string? NormalizeEmailForComparison(string? email)
    {
        return string.IsNullOrWhiteSpace(email)
            ? null
            : email.Trim().ToUpperInvariant();
    }

    private static bool IsUniqueViolation(DbUpdateException exception)
    {
        return exception.InnerException is PostgresException postgresException
            && postgresException.SqlState == PostgresErrorCodes.UniqueViolation;
    }
}
