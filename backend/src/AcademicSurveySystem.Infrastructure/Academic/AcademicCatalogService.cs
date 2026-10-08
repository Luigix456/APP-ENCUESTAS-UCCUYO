using AcademicSurveySystem.Application.Academic;
using AcademicSurveySystem.Application.Academic.AcademicUnits;
using AcademicSurveySystem.Application.Academic.Attention;
using AcademicSurveySystem.Application.Academic.AcademicCycles;
using AcademicSurveySystem.Application.Academic.Careers;
using AcademicSurveySystem.Application.Academic.Common;
using AcademicSurveySystem.Application.Academic.Subjects;
using AcademicSurveySystem.Application.Academic.SubjectEnrollments;
using AcademicSurveySystem.Application.Academic.Teachers;
using AcademicSurveySystem.Application.Academic.TeacherSubjectAssignments;
using AcademicSurveySystem.Application.Audit;
using AcademicSurveySystem.Application.Common.Results;
using ClosedXML.Excel;
using CsvHelper;
using CsvHelper.Configuration;
using AcademicSurveySystem.Domain.Academic.Entities;
using AcademicSurveySystem.Domain.Academic.Enums;
using AcademicSurveySystem.Domain.Common;
using AcademicSurveySystem.Domain.Surveys.Enums;
using AcademicSurveySystem.Infrastructure.Audit;
using AcademicSurveySystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Npgsql;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace AcademicSurveySystem.Infrastructure.Academic;

public sealed class AcademicCatalogService : IAcademicCatalogService
{
    private const int AcademicCycleEndingSoonDays = 30;
    private const string SubjectEnrollmentImportWorksheetName = "Matrículas";
    private const string SubjectEnrollmentImportStatusCreate = "Create";
    private const string SubjectEnrollmentImportStatusUpdate = "Update";
    private const string SubjectEnrollmentImportStatusUnchanged = "Unchanged";
    private const string SubjectEnrollmentImportStatusError = "Error";
    private static readonly Regex SubjectCodeRegex = new("^[a-z0-9._-]+$", RegexOptions.Compiled);

    private readonly ApplicationDbContext _dbContext;
    private readonly IAuditWriter _auditWriter;
    private readonly SubjectEnrollmentImportOptions _subjectEnrollmentImportOptions;

    public AcademicCatalogService(
        ApplicationDbContext dbContext,
        IAuditWriter? auditWriter = null,
        IOptions<SubjectEnrollmentImportOptions>? subjectEnrollmentImportOptions = null)
    {
        _dbContext = dbContext;
        _auditWriter = auditWriter ?? NoOpAuditWriter.Instance;
        _subjectEnrollmentImportOptions = subjectEnrollmentImportOptions?.Value ?? new SubjectEnrollmentImportOptions();
    }

    public async Task<ApplicationResult<IReadOnlyCollection<AcademicUnitDto>>> GetAcademicUnitsAsync(
        bool includeInactive,
        CancellationToken cancellationToken)
    {
        var units = await _dbContext.AcademicUnits
            .AsNoTracking()
            .Where(unit => includeInactive || unit.IsActive)
            .OrderBy(unit => unit.Name)
            .Select(unit => MapAcademicUnit(unit))
            .ToArrayAsync(cancellationToken);

        return ApplicationResult<IReadOnlyCollection<AcademicUnitDto>>.Success(units);
    }

    public async Task<ApplicationResult<AcademicUnitDto>> GetAcademicUnitByIdAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var unit = await _dbContext.AcademicUnits
            .AsNoTracking()
            .Where(item => item.Id == id)
            .Select(item => MapAcademicUnit(item))
            .SingleOrDefaultAsync(cancellationToken);

        return unit is null
            ? ApplicationResult<AcademicUnitDto>.NotFound("Academic unit was not found.")
            : ApplicationResult<AcademicUnitDto>.Success(unit);
    }

    public async Task<ApplicationResult<AcademicUnitDto>> CreateAcademicUnitAsync(
        CreateAcademicUnitRequest request,
        CancellationToken cancellationToken)
    {
        var validationErrors = request.Validate();

        if (validationErrors.Count > 0)
        {
            return ApplicationResult<AcademicUnitDto>.Validation(validationErrors);
        }

        var normalizedCode = request.Code!.Trim().ToLowerInvariant();

        if (await _dbContext.AcademicUnits.AnyAsync(unit => unit.Code == normalizedCode, cancellationToken))
        {
            return ApplicationResult<AcademicUnitDto>.Conflict(
                "An academic unit with the same code already exists.");
        }

        try
        {
            var unit = new AcademicUnit(
                Guid.NewGuid(),
                request.Code,
                request.Name!,
                DateTimeOffset.UtcNow);

            _dbContext.AcademicUnits.Add(unit);
            await WriteAcademicAuditAsync(
                "academic.unit.created",
                "AcademicUnit",
                unit.Id,
                $"Creó la unidad académica {unit.Code}.",
                new { unit.Code },
                cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return ApplicationResult<AcademicUnitDto>.Success(MapAcademicUnit(unit));
        }
        catch (DomainException exception)
        {
            return ApplicationResult<AcademicUnitDto>.Validation([
                new ApplicationError("AcademicUnit.Validation", exception.Message)
            ]);
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
        {
            return ApplicationResult<AcademicUnitDto>.Conflict(
                "An academic unit with the same code already exists.");
        }
        catch (DbUpdateException)
        {
            return ApplicationResult<AcademicUnitDto>.Failure("The academic unit could not be saved.");
        }
    }

    public async Task<ApplicationResult> UpdateAcademicUnitAsync(
        Guid id,
        UpdateAcademicUnitRequest request,
        CancellationToken cancellationToken)
    {
        var validationErrors = request.Validate();

        if (validationErrors.Count > 0)
        {
            return ApplicationResult.Validation(validationErrors);
        }

        var unit = await _dbContext.AcademicUnits.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (unit is null)
        {
            return ApplicationResult.NotFound("Academic unit was not found.");
        }

        try
        {
            var oldName = unit.Name;
            unit.UpdateName(request.Name!, DateTimeOffset.UtcNow);
            await WriteAcademicAuditAsync(
                "academic.unit.updated",
                "AcademicUnit",
                unit.Id,
                $"Actualizó la unidad académica {unit.Code}.",
                new { changedFields = BuildChangedFields((nameof(unit.Name), oldName, unit.Name)) },
                cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return ApplicationResult.Success();
        }
        catch (DomainException exception)
        {
            return ApplicationResult.Validation([
                new ApplicationError("AcademicUnit.Validation", exception.Message)
            ]);
        }
        catch (DbUpdateException)
        {
            return ApplicationResult.Failure("The academic unit could not be saved.");
        }
    }

    public Task<ApplicationResult> ActivateAcademicUnitAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        return ChangeAcademicUnitStateAsync(id, activate: true, cancellationToken);
    }

    public Task<ApplicationResult> DeactivateAcademicUnitAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        return ChangeAcademicUnitStateAsync(id, activate: false, cancellationToken);
    }

    public async Task<ApplicationResult<IReadOnlyCollection<CareerDto>>> GetCareersAsync(
        bool includeInactive,
        Guid? academicUnitId,
        CancellationToken cancellationToken)
    {
        var careers = await _dbContext.Careers
            .AsNoTracking()
            .Where(career => includeInactive || career.IsActive)
            .Where(career => academicUnitId == null || career.AcademicUnitId == academicUnitId.Value)
            .OrderBy(career => career.Name)
            .Select(career => new CareerDto(
                career.Id,
                career.AcademicUnitId,
                career.AcademicUnit.Code,
                career.AcademicUnit.Name,
                career.Code,
                career.Name,
                career.Type.ToString(),
                career.IsActive,
                career.CreatedAtUtc,
                career.UpdatedAtUtc))
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
            .Select(item => new CareerDto(
                item.Id,
                item.AcademicUnitId,
                item.AcademicUnit.Code,
                item.AcademicUnit.Name,
                item.Code,
                item.Name,
                item.Type.ToString(),
                item.IsActive,
                item.CreatedAtUtc,
                item.UpdatedAtUtc))
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

        var academicUnit = await _dbContext.AcademicUnits
            .AsNoTracking()
            .SingleOrDefaultAsync(unit => unit.Id == request.AcademicUnitId!.Value, cancellationToken);

        if (academicUnit is null)
        {
            return ApplicationResult<CareerDto>.NotFound("Academic unit was not found.");
        }

        if (!academicUnit.IsActive)
        {
            return ApplicationResult<CareerDto>.Validation([
                new ApplicationError("Career.AcademicUnitInactive", "Academic unit is inactive.")
            ]);
        }

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
                request.AcademicUnitId!.Value,
                request.Code,
                request.Name!,
                type,
                DateTimeOffset.UtcNow);

            _dbContext.Careers.Add(career);
            await WriteAcademicAuditAsync(
                "academic.career.created",
                "Career",
                career.Id,
                $"Creó la carrera {career.Code}.",
                new { career.Code, academicUnitCode = academicUnit.Code },
                cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return await GetCareerByIdAsync(career.Id, cancellationToken);
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
            var oldName = career.Name;
            var oldType = career.Type.ToString();
            career.UpdateName(request.Name!, now);
            career.UpdateType(type, now);
            await WriteAcademicAuditAsync(
                "academic.career.updated",
                "Career",
                career.Id,
                $"Actualizó la carrera {career.Code}.",
                new { changedFields = BuildChangedFields(
                    (nameof(career.Name), oldName, career.Name),
                    (nameof(career.Type), oldType, career.Type.ToString())) },
                cancellationToken);
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

    public async Task<ApplicationResult<IReadOnlyCollection<TeacherDto>>> GetCareerTeachersAsync(
        Guid careerId,
        Guid? academicCycleId,
        bool includeInactive,
        CancellationToken cancellationToken)
    {
        var careerExists = await _dbContext.Careers
            .AsNoTracking()
            .AnyAsync(career => career.Id == careerId, cancellationToken);

        if (!careerExists)
        {
            return ApplicationResult<IReadOnlyCollection<TeacherDto>>.NotFound("Career was not found.");
        }

        var teachers = await _dbContext.TeacherSubjectAssignments
            .AsNoTracking()
            .Where(assignment => includeInactive || assignment.IsActive)
            .Where(assignment => includeInactive || assignment.Teacher.IsActive)
            .Where(assignment => assignment.Subject.CareerId == careerId)
            .Where(assignment => academicCycleId == null || assignment.AcademicCycleId == academicCycleId.Value)
            .GroupBy(assignment => new
            {
                assignment.Teacher.Id,
                assignment.Teacher.FirstName,
                assignment.Teacher.LastName,
                assignment.Teacher.Email,
                assignment.Teacher.IsActive,
                assignment.Teacher.CreatedAtUtc,
                assignment.Teacher.UpdatedAtUtc
            })
            .OrderBy(group => group.Key.LastName)
            .ThenBy(group => group.Key.FirstName)
            .Select(group => new TeacherDto(
                group.Key.Id,
                group.Key.FirstName,
                group.Key.LastName,
                group.Key.Email,
                group.Key.IsActive,
                group.Key.CreatedAtUtc,
                group.Key.UpdatedAtUtc))
            .ToArrayAsync(cancellationToken);

        return ApplicationResult<IReadOnlyCollection<TeacherDto>>.Success(teachers);
    }

    public async Task<ApplicationResult<AcademicAttentionDto>> GetCareerAttentionAsync(
        Guid careerId,
        Guid academicCycleId,
        AcademicAttentionPermissions permissions,
        CancellationToken cancellationToken)
    {
        var careerExists = await _dbContext.Careers
            .AsNoTracking()
            .AnyAsync(career => career.Id == careerId, cancellationToken);

        if (!careerExists)
        {
            return ApplicationResult<AcademicAttentionDto>.NotFound("Career was not found.");
        }

        var academicCycle = await _dbContext.AcademicCycles
            .AsNoTracking()
            .Where(cycle => cycle.Id == academicCycleId)
            .Select(cycle => new
            {
                cycle.Id,
                cycle.EndDate
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (academicCycle is null)
        {
            return ApplicationResult<AcademicAttentionDto>.NotFound("Academic cycle was not found.");
        }

        var items = new List<AcademicAttentionItemDto>();
        var subjects = await _dbContext.Subjects
            .AsNoTracking()
            .Where(subject => subject.CareerId == careerId)
            .Where(subject => subject.IsActive)
            .Select(subject => new AttentionSubject(subject.Id, subject.Name))
            .OrderBy(subject => subject.Name)
            .ToArrayAsync(cancellationToken);

        if (subjects.Length > 0)
        {
            var subjectIds = subjects.Select(subject => subject.Id).ToArray();
            var enrolledSubjectIds = await _dbContext.SubjectEnrollments
                .AsNoTracking()
                .Where(enrollment => enrollment.AcademicCycleId == academicCycleId)
                .Where(enrollment => subjectIds.Contains(enrollment.SubjectId))
                .Select(enrollment => enrollment.SubjectId)
                .Distinct()
                .ToArrayAsync(cancellationToken);
            var assignedSubjectIds = await _dbContext.TeacherSubjectAssignments
                .AsNoTracking()
                .Where(assignment => assignment.AcademicCycleId == academicCycleId)
                .Where(assignment => assignment.IsActive)
                .Where(assignment => subjectIds.Contains(assignment.SubjectId))
                .Select(assignment => assignment.SubjectId)
                .Distinct()
                .ToArrayAsync(cancellationToken);
            var enrolledSubjectSet = enrolledSubjectIds.ToHashSet();
            var assignedSubjectSet = assignedSubjectIds.ToHashSet();
            var subjectsWithoutEnrollment = subjects
                .Where(subject => !enrolledSubjectSet.Contains(subject.Id))
                .ToArray();
            var subjectsWithoutTeachers = subjects
                .Where(subject => !assignedSubjectSet.Contains(subject.Id))
                .ToArray();

            if (subjectsWithoutEnrollment.Length > 0)
            {
                items.Add(CreateCountAlert(
                    "Academic.SubjectsWithoutEnrollment",
                    "warning",
                    "Materias sin matrícula cargada",
                    FormatSubjectCountDescription(
                        subjectsWithoutEnrollment,
                        "materia no tiene matrícula cargada para el ciclo seleccionado.",
                        "materias no tienen matrícula cargada para el ciclo seleccionado."),
                    "Subject",
                    subjectsWithoutEnrollment.Length == 1 ? subjectsWithoutEnrollment[0].Id : null,
                    "ReviewSubjectEnrollments",
                    subjectsWithoutEnrollment.Length));
            }

            if (subjectsWithoutTeachers.Length > 0)
            {
                items.Add(CreateCountAlert(
                    "Academic.SubjectsWithoutTeachers",
                    "warning",
                    "Materias sin docentes asignados",
                    FormatSubjectCountDescription(
                        subjectsWithoutTeachers,
                        "materia no tiene docentes asignados para el ciclo seleccionado.",
                        "materias no tienen docentes asignados para el ciclo seleccionado."),
                    "Subject",
                    subjectsWithoutTeachers.Length == 1 ? subjectsWithoutTeachers[0].Id : null,
                    "AssignSubjectTeacher",
                    subjectsWithoutTeachers.Length));
            }
        }

        if (permissions.IncludeSurveyAlerts)
        {
            await AddSurveyAttentionItemsAsync(careerId, academicCycleId, items, cancellationToken);
        }

        AddAcademicCycleEndingSoonAlert(academicCycle.EndDate, academicCycle.Id, items);

        return ApplicationResult<AcademicAttentionDto>.Success(new AcademicAttentionDto(items));
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
            await WriteAcademicAuditAsync(
                "academic.cycle.created",
                "AcademicCycle",
                cycle.Id,
                $"Creó el ciclo académico {cycle.Year} {cycle.Period}.",
                new { cycle.Year, Period = cycle.Period.ToString() },
                cancellationToken);
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
            var oldPeriod = cycle.Period.ToString();
            var oldStartDate = cycle.StartDate;
            var oldEndDate = cycle.EndDate;
            cycle.UpdatePeriodAndDates(
                period,
                request.StartDate!.Value,
                request.EndDate!.Value,
                DateTimeOffset.UtcNow);

            await WriteAcademicAuditAsync(
                "academic.cycle.updated",
                "AcademicCycle",
                cycle.Id,
                $"Actualizó el ciclo académico {cycle.Year} {cycle.Period}.",
                new { changedFields = BuildChangedFields(
                    (nameof(cycle.Period), oldPeriod, cycle.Period.ToString()),
                    (nameof(cycle.StartDate), oldStartDate, cycle.StartDate),
                    (nameof(cycle.EndDate), oldEndDate, cycle.EndDate)) },
                cancellationToken);
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
            await WriteAcademicAuditAsync(
                "academic.subject.created",
                "Subject",
                subject.Id,
                $"Creó la materia {subject.Code}.",
                new { subject.Code, careerCode = career.Code },
                cancellationToken);
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
            var oldName = subject.Name;
            var oldYear = subject.Year;
            var oldPeriod = subject.Period.ToString();
            subject.UpdateName(request.Name!, now);
            subject.UpdateYearAndPeriod(request.Year!.Value, period, now);
            await WriteAcademicAuditAsync(
                "academic.subject.updated",
                "Subject",
                subject.Id,
                $"Actualizó la materia {subject.Code}.",
                new { changedFields = BuildChangedFields(
                    (nameof(subject.Name), oldName, subject.Name),
                    (nameof(subject.Year), oldYear, subject.Year),
                    (nameof(subject.Period), oldPeriod, subject.Period.ToString())) },
                cancellationToken);
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

    public async Task<ApplicationResult<IReadOnlyCollection<SubjectEnrollmentDto>>> GetSubjectEnrollmentsAsync(
        Guid? subjectId,
        Guid? academicCycleId,
        Guid? careerId,
        CancellationToken cancellationToken)
    {
        var enrollments = await _dbContext.SubjectEnrollments
            .AsNoTracking()
            .Include(enrollment => enrollment.Subject)
                .ThenInclude(subject => subject.Career)
            .Include(enrollment => enrollment.AcademicCycle)
            .Where(enrollment => subjectId == null || enrollment.SubjectId == subjectId.Value)
            .Where(enrollment => academicCycleId == null || enrollment.AcademicCycleId == academicCycleId.Value)
            .Where(enrollment => careerId == null || enrollment.Subject.CareerId == careerId.Value)
            .OrderBy(enrollment => enrollment.Subject.Career.Name)
            .ThenBy(enrollment => enrollment.Subject.Year)
            .ThenBy(enrollment => enrollment.Subject.Name)
            .ThenByDescending(enrollment => enrollment.AcademicCycle.Year)
            .ThenBy(enrollment => enrollment.AcademicCycle.Period)
            .Select(enrollment => MapSubjectEnrollment(enrollment))
            .ToArrayAsync(cancellationToken);

        return ApplicationResult<IReadOnlyCollection<SubjectEnrollmentDto>>.Success(enrollments);
    }

    public async Task<ApplicationResult<SubjectEnrollmentDto>> GetSubjectEnrollmentAsync(
        Guid subjectId,
        Guid academicCycleId,
        CancellationToken cancellationToken)
    {
        var enrollment = await _dbContext.SubjectEnrollments
            .AsNoTracking()
            .Include(item => item.Subject)
                .ThenInclude(subject => subject.Career)
            .Include(item => item.AcademicCycle)
            .Where(item => item.SubjectId == subjectId && item.AcademicCycleId == academicCycleId)
            .Select(item => MapSubjectEnrollment(item))
            .SingleOrDefaultAsync(cancellationToken);

        return enrollment is null
            ? ApplicationResult<SubjectEnrollmentDto>.NotFound("Subject enrollment was not found.")
            : ApplicationResult<SubjectEnrollmentDto>.Success(enrollment);
    }

    public async Task<ApplicationResult<SubjectEnrollmentDto>> SetSubjectEnrollmentAsync(
        Guid subjectId,
        Guid academicCycleId,
        SetSubjectEnrollmentRequest request,
        CancellationToken cancellationToken)
    {
        var validationErrors = request.Validate();

        if (subjectId == Guid.Empty)
        {
            validationErrors = validationErrors
                .Append(new ApplicationError("SubjectEnrollment.SubjectIdRequired", "SubjectId is required."))
                .ToArray();
        }

        if (academicCycleId == Guid.Empty)
        {
            validationErrors = validationErrors
                .Append(new ApplicationError(
                    "SubjectEnrollment.AcademicCycleIdRequired",
                    "AcademicCycleId is required."))
                .ToArray();
        }

        if (validationErrors.Count > 0)
        {
            return ApplicationResult<SubjectEnrollmentDto>.Validation(validationErrors);
        }

        var subjectExists = await _dbContext.Subjects
            .AsNoTracking()
            .AnyAsync(subject => subject.Id == subjectId, cancellationToken);

        if (!subjectExists)
        {
            return ApplicationResult<SubjectEnrollmentDto>.NotFound("Subject was not found.");
        }

        var academicCycleExists = await _dbContext.AcademicCycles
            .AsNoTracking()
            .AnyAsync(cycle => cycle.Id == academicCycleId, cancellationToken);

        if (!academicCycleExists)
        {
            return ApplicationResult<SubjectEnrollmentDto>.NotFound("Academic cycle was not found.");
        }

        var enrollment = await _dbContext.SubjectEnrollments
            .SingleOrDefaultAsync(
                item => item.SubjectId == subjectId && item.AcademicCycleId == academicCycleId,
                cancellationToken);
        var enrollmentWasCreated = enrollment is null;
        var oldEnrolledStudentCount = enrollment?.EnrolledStudentCount;

        try
        {
            if (enrollment is null)
            {
                enrollment = new SubjectEnrollment(
                    Guid.NewGuid(),
                    subjectId,
                    academicCycleId,
                    request.EnrolledStudentCount!.Value,
                    DateTimeOffset.UtcNow);
                _dbContext.SubjectEnrollments.Add(enrollment);
            }
            else
            {
                enrollment.UpdateEnrolledStudentCount(
                    request.EnrolledStudentCount!.Value,
                    DateTimeOffset.UtcNow);
            }

            await WriteAcademicAuditAsync(
                enrollmentWasCreated
                    ? "academic.enrollment.created"
                    : "academic.enrollment.updated",
                "SubjectEnrollment",
                enrollment.Id,
                "Actualizó la matrícula de la materia.",
                new
                {
                    subjectId,
                    academicCycleId,
                    oldCount = oldEnrolledStudentCount,
                    newCount = enrollment.EnrolledStudentCount
                },
                cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DomainException exception)
        {
            return ApplicationResult<SubjectEnrollmentDto>.Validation([
                new ApplicationError("SubjectEnrollment.Validation", exception.Message)
            ]);
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
        {
            return ApplicationResult<SubjectEnrollmentDto>.Conflict(
                "A subject enrollment for the same subject and academic cycle already exists.");
        }
        catch (DbUpdateException)
        {
            return ApplicationResult<SubjectEnrollmentDto>.Failure("The subject enrollment could not be saved.");
        }

        return await GetSubjectEnrollmentAsync(subjectId, academicCycleId, cancellationToken);
    }

    public async Task<ApplicationResult<SubjectEnrollmentImportTemplateFileDto>> GenerateSubjectEnrollmentImportTemplateAsync(
        Guid careerId,
        Guid academicCycleId,
        string? format,
        CancellationToken cancellationToken)
    {
        var validationErrors = ValidateSubjectEnrollmentImportContextIds(careerId, academicCycleId);

        if (validationErrors.Count > 0)
        {
            return ApplicationResult<SubjectEnrollmentImportTemplateFileDto>.Validation(validationErrors);
        }

        var normalizedFormat = NormalizeTemplateFormat(format);

        if (normalizedFormat is null)
        {
            return ApplicationResult<SubjectEnrollmentImportTemplateFileDto>.Validation([
                new ApplicationError("SubjectEnrollmentImport.FormatInvalid", "Format must be xlsx or csv.")
            ]);
        }

        var contextResult = await LoadSubjectEnrollmentImportContextAsync(
            careerId,
            academicCycleId,
            cancellationToken);

        if (contextResult.Status != ApplicationResultStatus.Success)
        {
            return contextResult.Status == ApplicationResultStatus.NotFound
                ? ApplicationResult<SubjectEnrollmentImportTemplateFileDto>.NotFound(contextResult.Errors.First().Message)
                : ApplicationResult<SubjectEnrollmentImportTemplateFileDto>.Validation(contextResult.Errors);
        }

        var context = contextResult.Value!;
        var rows = context.Subjects
            .Where(subject => subject.IsActive)
            .OrderBy(subject => subject.Year)
            .ThenBy(subject => subject.Name)
            .Select(subject => new SubjectEnrollmentTemplateRow(
                subject.Code,
                subject.Name,
                context.EnrollmentsBySubjectId.TryGetValue(subject.Id, out var currentCount)
                    ? (int?)currentCount
                    : null))
            .ToArray();
        var fileName = $"matriculas-{context.Career.Code}-{context.AcademicCycle.Year}.{normalizedFormat}";

        return normalizedFormat == "csv"
            ? ApplicationResult<SubjectEnrollmentImportTemplateFileDto>.Success(
                new SubjectEnrollmentImportTemplateFileDto(
                    fileName,
                    "text/csv; charset=utf-8",
                    CreateSubjectEnrollmentCsvTemplate(rows)))
            : ApplicationResult<SubjectEnrollmentImportTemplateFileDto>.Success(
                new SubjectEnrollmentImportTemplateFileDto(
                    fileName,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    CreateSubjectEnrollmentXlsxTemplate(rows)));
    }

    public async Task<ApplicationResult<SubjectEnrollmentImportPreviewDto>> PreviewSubjectEnrollmentImportAsync(
        Guid careerId,
        Guid academicCycleId,
        SubjectEnrollmentImportFile file,
        CancellationToken cancellationToken)
    {
        var parseResult = await ParseAndValidateSubjectEnrollmentImportAsync(
            careerId,
            academicCycleId,
            file,
            cancellationToken);

        return parseResult.Status == ApplicationResultStatus.Success
            ? ApplicationResult<SubjectEnrollmentImportPreviewDto>.Success(parseResult.Value!.Preview)
            : parseResult.Status == ApplicationResultStatus.NotFound
                ? ApplicationResult<SubjectEnrollmentImportPreviewDto>.NotFound(parseResult.Errors.First().Message)
                : ApplicationResult<SubjectEnrollmentImportPreviewDto>.Validation(parseResult.Errors);
    }

    public async Task<ApplicationResult<SubjectEnrollmentImportResultDto>> ImportSubjectEnrollmentsAsync(
        Guid careerId,
        Guid academicCycleId,
        SubjectEnrollmentImportFile file,
        CancellationToken cancellationToken)
    {
        var parseResult = await ParseAndValidateSubjectEnrollmentImportAsync(
            careerId,
            academicCycleId,
            file,
            cancellationToken);

        if (parseResult.Status != ApplicationResultStatus.Success)
        {
            return parseResult.Status == ApplicationResultStatus.NotFound
                ? ApplicationResult<SubjectEnrollmentImportResultDto>.NotFound(parseResult.Errors.First().Message)
                : ApplicationResult<SubjectEnrollmentImportResultDto>.Validation(parseResult.Errors);
        }

        var parsedImport = parseResult.Value!;

        if (parsedImport.Preview.ErrorRows > 0)
        {
            return ApplicationResult<SubjectEnrollmentImportResultDto>.Validation(
                parsedImport.Preview.Rows
                    .Where(row => row.Status == SubjectEnrollmentImportStatusError)
                    .Select(row => new ApplicationError(
                        row.ErrorCode ?? "SubjectEnrollmentImport.RowInvalid",
                        $"Fila {row.RowNumber}: {row.ErrorMessage ?? "La fila contiene errores."}"))
                    .ToArray());
        }

        var rowsToCreate = parsedImport.Preview.Rows
            .Where(row => row.Status == SubjectEnrollmentImportStatusCreate)
            .ToArray();
        var rowsToUpdate = parsedImport.Preview.Rows
            .Where(row => row.Status == SubjectEnrollmentImportStatusUpdate)
            .ToArray();
        var subjectIdsToUpdate = rowsToUpdate
            .Select(row => row.SubjectId!.Value)
            .ToArray();
        await using var transaction = _dbContext.Database.IsRelational()
            ? await _dbContext.Database.BeginTransactionAsync(cancellationToken)
            : null;

        try
        {
            var utcNow = DateTimeOffset.UtcNow;
            var existingEnrollments = await _dbContext.SubjectEnrollments
                .Where(enrollment => enrollment.AcademicCycleId == academicCycleId)
                .Where(enrollment => subjectIdsToUpdate.Contains(enrollment.SubjectId))
                .ToDictionaryAsync(enrollment => enrollment.SubjectId, cancellationToken);

            foreach (var row in rowsToCreate)
            {
                _dbContext.SubjectEnrollments.Add(new SubjectEnrollment(
                    Guid.NewGuid(),
                    row.SubjectId!.Value,
                    academicCycleId,
                    row.NewEnrolledStudentCount!.Value,
                    utcNow));
            }

            foreach (var row in rowsToUpdate)
            {
                if (existingEnrollments.TryGetValue(row.SubjectId!.Value, out var enrollment))
                {
                    enrollment.UpdateEnrolledStudentCount(row.NewEnrolledStudentCount!.Value, utcNow);
                }
            }

            if (rowsToCreate.Length + rowsToUpdate.Length > 0)
            {
                await _auditWriter.WriteAsync(
                    "academic.enrollment.bulk_imported",
                    "academic_catalog",
                    "SubjectEnrollment",
                    entityId: null,
                    $"Importó matrículas para {rowsToCreate.Length + rowsToUpdate.Length} materias de {parsedImport.Context.Career.Name} - ciclo {parsedImport.Context.AcademicCycle.Year}.",
                    new
                    {
                        careerId,
                        academicCycleId,
                        createdCount = rowsToCreate.Length,
                        updatedCount = rowsToUpdate.Length,
                        unchangedCount = parsedImport.Preview.UnchangedRows,
                        subjectIds = rowsToCreate
                            .Concat(rowsToUpdate)
                            .Select(row => row.SubjectId!.Value)
                            .Take(50)
                            .ToArray()
                    },
                    cancellationToken);
            }
            await _dbContext.SaveChangesAsync(cancellationToken);

            if (transaction is not null)
            {
                await transaction.CommitAsync(cancellationToken);
            }

            return ApplicationResult<SubjectEnrollmentImportResultDto>.Success(
                new SubjectEnrollmentImportResultDto(
                    rowsToCreate.Length,
                    rowsToUpdate.Length,
                    parsedImport.Preview.UnchangedRows,
                    parsedImport.Preview.ValidRows));
        }
        catch (DomainException exception)
        {
            if (transaction is not null)
            {
                await transaction.RollbackAsync(cancellationToken);
            }

            return ApplicationResult<SubjectEnrollmentImportResultDto>.Validation([
                new ApplicationError("SubjectEnrollmentImport.Validation", exception.Message)
            ]);
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
        {
            if (transaction is not null)
            {
                await transaction.RollbackAsync(cancellationToken);
            }

            return ApplicationResult<SubjectEnrollmentImportResultDto>.Conflict(
                "A subject enrollment for the same subject and academic cycle already exists.");
        }
        catch (DbUpdateException)
        {
            if (transaction is not null)
            {
                await transaction.RollbackAsync(cancellationToken);
            }

            return ApplicationResult<SubjectEnrollmentImportResultDto>.Failure(
                "The subject enrollment import could not be saved.");
        }
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
            await WriteAcademicAuditAsync(
                "academic.teacher.created",
                "Teacher",
                teacher.Id,
                $"Creó el docente {BuildTeacherDisplayName(teacher)}.",
                null,
                cancellationToken);
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
            var oldFirstName = teacher.FirstName;
            var oldLastName = teacher.LastName;
            var oldHasEmail = !string.IsNullOrWhiteSpace(teacher.Email);
            teacher.UpdateName(request.FirstName!, request.LastName!, now);

            if (string.IsNullOrWhiteSpace(request.Email))
            {
                teacher.RemoveEmail(now);
            }
            else
            {
                teacher.ChangeEmail(request.Email, now);
            }

            await WriteAcademicAuditAsync(
                "academic.teacher.updated",
                "Teacher",
                teacher.Id,
                $"Actualizó el docente {BuildTeacherDisplayName(teacher)}.",
                new { changedFields = BuildChangedFields(
                    (nameof(teacher.FirstName), oldFirstName, teacher.FirstName),
                    (nameof(teacher.LastName), oldLastName, teacher.LastName),
                    ("HasEmail", oldHasEmail, !string.IsNullOrWhiteSpace(teacher.Email))) },
                cancellationToken);
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
            Guid? careerId,
            Guid? teacherId,
            Guid? subjectId,
            Guid? academicCycleId,
            CancellationToken cancellationToken)
    {
        var assignments = await _dbContext.TeacherSubjectAssignments
            .AsNoTracking()
            .Where(assignment => includeInactive || assignment.IsActive)
            .Where(assignment => careerId == null || assignment.Subject.CareerId == careerId)
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
            await WriteAcademicAuditAsync(
                "academic.teacher_subject_assignment.created",
                "TeacherSubjectAssignment",
                assignment.Id,
                "Asignó un docente a una materia.",
                new
                {
                    teacherId,
                    subjectId,
                    academicCycleId,
                    assignment.TeachingRole
                },
                cancellationToken);
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
            var oldTeachingRole = assignment.TeachingRole;
            assignment.UpdateTeachingRole(request.TeachingRole!, DateTimeOffset.UtcNow);
            await WriteAcademicAuditAsync(
                "academic.teacher_subject_assignment.updated",
                "TeacherSubjectAssignment",
                assignment.Id,
                "Actualizó la asignación docente-materia.",
                new { changedFields = BuildChangedFields(
                    (nameof(assignment.TeachingRole), oldTeachingRole, assignment.TeachingRole)) },
                cancellationToken);
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
            await WriteAcademicAuditAsync(
                activate ? "academic.career.activated" : "academic.career.deactivated",
                "Career",
                career.Id,
                activate ? $"Activó la carrera {career.Code}." : $"Desactivó la carrera {career.Code}.",
                new { career.Code },
                cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return ApplicationResult.Success();
        }
        catch (DbUpdateException)
        {
            return ApplicationResult.Failure("The career could not be saved.");
        }
    }

    private async Task<ApplicationResult> ChangeAcademicUnitStateAsync(
        Guid id,
        bool activate,
        CancellationToken cancellationToken)
    {
        var unit = await _dbContext.AcademicUnits.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (unit is null)
        {
            return ApplicationResult.NotFound("Academic unit was not found.");
        }

        var now = DateTimeOffset.UtcNow;

        if (activate)
        {
            unit.Activate(now);
        }
        else
        {
            unit.Deactivate(now);
        }

        try
        {
            await WriteAcademicAuditAsync(
                activate ? "academic.unit.activated" : "academic.unit.deactivated",
                "AcademicUnit",
                unit.Id,
                activate ? $"Activó la unidad académica {unit.Code}." : $"Desactivó la unidad académica {unit.Code}.",
                new { unit.Code },
                cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return ApplicationResult.Success();
        }
        catch (DbUpdateException)
        {
            return ApplicationResult.Failure("The academic unit could not be saved.");
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
            await WriteAcademicAuditAsync(
                activate ? "academic.cycle.activated" : "academic.cycle.deactivated",
                "AcademicCycle",
                cycle.Id,
                activate
                    ? $"Activó el ciclo académico {cycle.Year} {cycle.Period}."
                    : $"Desactivó el ciclo académico {cycle.Year} {cycle.Period}.",
                new { cycle.Year, Period = cycle.Period.ToString() },
                cancellationToken);
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
            await WriteAcademicAuditAsync(
                activate ? "academic.subject.activated" : "academic.subject.deactivated",
                "Subject",
                subject.Id,
                activate ? $"Activó la materia {subject.Code}." : $"Desactivó la materia {subject.Code}.",
                new { subject.Code },
                cancellationToken);
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
            await WriteAcademicAuditAsync(
                activate ? "academic.teacher.activated" : "academic.teacher.deactivated",
                "Teacher",
                teacher.Id,
                activate
                    ? $"Activó el docente {BuildTeacherDisplayName(teacher)}."
                    : $"Desactivó el docente {BuildTeacherDisplayName(teacher)}.",
                null,
                cancellationToken);
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
            await WriteAcademicAuditAsync(
                activate
                    ? "academic.teacher_subject_assignment.activated"
                    : "academic.teacher_subject_assignment.deactivated",
                "TeacherSubjectAssignment",
                assignment.Id,
                activate ? "Activó la asignación docente-materia." : "Desactivó la asignación docente-materia.",
                new
                {
                    assignment.TeacherId,
                    assignment.SubjectId,
                    assignment.AcademicCycleId
                },
                cancellationToken);
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
            career.AcademicUnitId,
            career.AcademicUnit.Code,
            career.AcademicUnit.Name,
            career.Code,
            career.Name,
            career.Type.ToString(),
            career.IsActive,
            career.CreatedAtUtc,
            career.UpdatedAtUtc);
    }

    private static AcademicUnitDto MapAcademicUnit(AcademicUnit unit)
    {
        return new AcademicUnitDto(
            unit.Id,
            unit.Code,
            unit.Name,
            unit.IsActive,
            unit.CreatedAtUtc,
            unit.UpdatedAtUtc);
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

    private static SubjectEnrollmentDto MapSubjectEnrollment(SubjectEnrollment enrollment)
    {
        return new SubjectEnrollmentDto(
            enrollment.Id,
            enrollment.SubjectId,
            enrollment.Subject.Name,
            enrollment.Subject.CareerId,
            enrollment.Subject.Career.Name,
            enrollment.AcademicCycleId,
            enrollment.AcademicCycle.Year,
            enrollment.AcademicCycle.Period.ToString(),
            enrollment.EnrolledStudentCount,
            enrollment.CreatedAtUtc,
            enrollment.UpdatedAtUtc);
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

    private async Task<ApplicationResult<ParsedSubjectEnrollmentImport>> ParseAndValidateSubjectEnrollmentImportAsync(
        Guid careerId,
        Guid academicCycleId,
        SubjectEnrollmentImportFile file,
        CancellationToken cancellationToken)
    {
        var validationErrors = ValidateSubjectEnrollmentImportContextIds(careerId, academicCycleId).ToList();

        if (string.IsNullOrWhiteSpace(file.FileName))
        {
            validationErrors.Add(new ApplicationError("SubjectEnrollmentImport.FileNameRequired", "File name is required."));
        }

        if (file.Length <= 0)
        {
            validationErrors.Add(new ApplicationError("SubjectEnrollmentImport.FileEmpty", "File is empty."));
        }

        if (file.Length > _subjectEnrollmentImportOptions.MaxFileSizeBytes)
        {
            validationErrors.Add(new ApplicationError(
                "SubjectEnrollmentImport.FileTooLarge",
                "El archivo supera el tamaño máximo permitido de 5 MB."));
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

        if (extension is not ".csv" and not ".xlsx")
        {
            validationErrors.Add(new ApplicationError(
                "SubjectEnrollmentImport.FileTypeInvalid",
                "Only .csv and .xlsx files are supported."));
        }

        if (validationErrors.Count > 0)
        {
            return ApplicationResult<ParsedSubjectEnrollmentImport>.Validation(validationErrors);
        }

        var contextResult = await LoadSubjectEnrollmentImportContextAsync(
            careerId,
            academicCycleId,
            cancellationToken);

        if (contextResult.Status != ApplicationResultStatus.Success)
        {
            return contextResult.Status == ApplicationResultStatus.NotFound
                ? ApplicationResult<ParsedSubjectEnrollmentImport>.NotFound(contextResult.Errors.First().Message)
                : ApplicationResult<ParsedSubjectEnrollmentImport>.Validation(contextResult.Errors);
        }

        var content = await ReadImportFileContentAsync(file, cancellationToken);
        ApplicationResult<IReadOnlyCollection<SubjectEnrollmentImportRawRow>> rowsResult;

        try
        {
            rowsResult = extension == ".csv"
                ? ReadSubjectEnrollmentCsvRows(content)
                : ReadSubjectEnrollmentXlsxRows(content);
        }
        catch (CsvHelperException)
        {
            rowsResult = ApplicationResult<IReadOnlyCollection<SubjectEnrollmentImportRawRow>>.Validation([
                new ApplicationError("SubjectEnrollmentImport.CsvInvalid", "The CSV file could not be parsed.")
            ]);
        }
        catch (InvalidDataException exception)
        {
            rowsResult = ApplicationResult<IReadOnlyCollection<SubjectEnrollmentImportRawRow>>.Validation([
                new ApplicationError("SubjectEnrollmentImport.FileInvalid", exception.Message)
            ]);
        }

        if (rowsResult.Status != ApplicationResultStatus.Success)
        {
            return ApplicationResult<ParsedSubjectEnrollmentImport>.Validation(rowsResult.Errors);
        }

        if (rowsResult.Value!.Count > _subjectEnrollmentImportOptions.MaxRows)
        {
            return ApplicationResult<ParsedSubjectEnrollmentImport>.Validation([
                new ApplicationError(
                    "SubjectEnrollmentImport.TooManyRows",
                    $"The file exceeds the maximum allowed rows of {_subjectEnrollmentImportOptions.MaxRows}.")
            ]);
        }

        var context = contextResult.Value!;
        var rows = BuildSubjectEnrollmentImportPreviewRows(rowsResult.Value, context);
        var preview = new SubjectEnrollmentImportPreviewDto(
            file.FileName,
            rows.Count,
            rows.Count(row => row.Status != SubjectEnrollmentImportStatusError),
            rows.Count(row => row.Status == SubjectEnrollmentImportStatusCreate),
            rows.Count(row => row.Status == SubjectEnrollmentImportStatusUpdate),
            rows.Count(row => row.Status == SubjectEnrollmentImportStatusUnchanged),
            rows.Count(row => row.Status == SubjectEnrollmentImportStatusError),
            rows);

        return ApplicationResult<ParsedSubjectEnrollmentImport>.Success(
            new ParsedSubjectEnrollmentImport(context, preview));
    }

    private async Task<ApplicationResult<SubjectEnrollmentImportContext>> LoadSubjectEnrollmentImportContextAsync(
        Guid careerId,
        Guid academicCycleId,
        CancellationToken cancellationToken)
    {
        var career = await _dbContext.Careers
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == careerId, cancellationToken);

        if (career is null)
        {
            return ApplicationResult<SubjectEnrollmentImportContext>.NotFound("Career was not found.");
        }

        var academicCycle = await _dbContext.AcademicCycles
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == academicCycleId, cancellationToken);

        if (academicCycle is null)
        {
            return ApplicationResult<SubjectEnrollmentImportContext>.NotFound("Academic cycle was not found.");
        }

        var subjects = await _dbContext.Subjects
            .AsNoTracking()
            .Where(subject => subject.CareerId == careerId)
            .OrderBy(subject => subject.Year)
            .ThenBy(subject => subject.Name)
            .Select(subject => new SubjectEnrollmentImportSubject(
                subject.Id,
                subject.CareerId,
                subject.Code,
                subject.Name,
                subject.Year,
                subject.IsActive))
            .ToArrayAsync(cancellationToken);
        var subjectIds = subjects
            .Select(subject => subject.Id)
            .ToArray();
        var enrollmentsBySubjectId = await _dbContext.SubjectEnrollments
            .AsNoTracking()
            .Where(enrollment => enrollment.AcademicCycleId == academicCycleId)
            .Where(enrollment => subjectIds.Contains(enrollment.SubjectId))
            .ToDictionaryAsync(
                enrollment => enrollment.SubjectId,
                enrollment => enrollment.EnrolledStudentCount,
                cancellationToken);

        return ApplicationResult<SubjectEnrollmentImportContext>.Success(
            new SubjectEnrollmentImportContext(
                career,
                academicCycle,
                subjects.ToDictionary(subject => subject.Code, StringComparer.Ordinal),
                enrollmentsBySubjectId));
    }

    private static async Task<byte[]> ReadImportFileContentAsync(
        SubjectEnrollmentImportFile file,
        CancellationToken cancellationToken)
    {
        await using var memoryStream = new MemoryStream();
        await file.Content.CopyToAsync(memoryStream, cancellationToken);
        return memoryStream.ToArray();
    }

    private static ApplicationResult<IReadOnlyCollection<SubjectEnrollmentImportRawRow>> ReadSubjectEnrollmentCsvRows(
        byte[] content)
    {
        using var memoryStream = new MemoryStream(content);
        using var delimiterReader = new StreamReader(
            memoryStream,
            Encoding.UTF8,
            detectEncodingFromByteOrderMarks: true,
            leaveOpen: true);
        var headerLine = delimiterReader.ReadLine() ?? string.Empty;
        var delimiter = CountOccurrences(headerLine, ';') > CountOccurrences(headerLine, ',') ? ";" : ",";
        memoryStream.Position = 0;
        using var reader = new StreamReader(
            memoryStream,
            Encoding.UTF8,
            detectEncodingFromByteOrderMarks: true,
            leaveOpen: false);
        using var csv = new CsvReader(
            reader,
            new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Delimiter = delimiter,
                BadDataFound = null,
                HeaderValidated = null,
                MissingFieldFound = null,
                TrimOptions = TrimOptions.Trim
            });

        if (!csv.Read() || !csv.ReadHeader())
        {
            return ApplicationResult<IReadOnlyCollection<SubjectEnrollmentImportRawRow>>.Validation([
                new ApplicationError("SubjectEnrollmentImport.HeadersMissing", "The file must include a header row.")
            ]);
        }

        var headerIndex = BuildHeaderIndex(csv.HeaderRecord);
        var headerErrors = ValidateSubjectEnrollmentImportHeaders(headerIndex);

        if (headerErrors.Count > 0)
        {
            return ApplicationResult<IReadOnlyCollection<SubjectEnrollmentImportRawRow>>.Validation(headerErrors);
        }

        var rows = new List<SubjectEnrollmentImportRawRow>();

        while (csv.Read())
        {
            var row = new SubjectEnrollmentImportRawRow(
                csv.Context.Parser?.Row ?? 0,
                GetCsvField(csv, headerIndex["codigomateria"]),
                GetCsvField(csv, headerIndex["materia"]),
                GetCsvField(csv, headerIndex["alumnosinscriptos"]));

            if (!row.IsBlank)
            {
                rows.Add(row);
            }
        }

        return ApplicationResult<IReadOnlyCollection<SubjectEnrollmentImportRawRow>>.Success(rows);
    }

    private static ApplicationResult<IReadOnlyCollection<SubjectEnrollmentImportRawRow>> ReadSubjectEnrollmentXlsxRows(
        byte[] content)
    {
        using var memoryStream = new MemoryStream(content);
        using var workbook = new XLWorkbook(memoryStream);
        var worksheet = workbook.Worksheets
            .FirstOrDefault(item => item.Name.Equals(
                SubjectEnrollmentImportWorksheetName,
                StringComparison.OrdinalIgnoreCase))
            ?? workbook.Worksheets.FirstOrDefault();

        if (worksheet is null)
        {
            return ApplicationResult<IReadOnlyCollection<SubjectEnrollmentImportRawRow>>.Validation([
                new ApplicationError("SubjectEnrollmentImport.WorksheetMissing", "The workbook does not contain worksheets.")
            ]);
        }

        var headerIndex = BuildHeaderIndex(
            Enumerable
                .Range(1, Math.Max(worksheet.LastColumnUsed()?.ColumnNumber() ?? 0, 3))
                .Select(column => worksheet.Cell(1, column).GetString())
                .ToArray());
        var headerErrors = ValidateSubjectEnrollmentImportHeaders(headerIndex);

        if (headerErrors.Count > 0)
        {
            return ApplicationResult<IReadOnlyCollection<SubjectEnrollmentImportRawRow>>.Validation(headerErrors);
        }

        var lastRowNumber = worksheet.LastRowUsed()?.RowNumber() ?? 1;
        var rows = new List<SubjectEnrollmentImportRawRow>();

        for (var rowNumber = 2; rowNumber <= lastRowNumber; rowNumber++)
        {
            var row = new SubjectEnrollmentImportRawRow(
                rowNumber,
                worksheet.Cell(rowNumber, headerIndex["codigomateria"] + 1).GetString(),
                worksheet.Cell(rowNumber, headerIndex["materia"] + 1).GetString(),
                worksheet.Cell(rowNumber, headerIndex["alumnosinscriptos"] + 1).GetString());

            if (!row.IsBlank)
            {
                rows.Add(row);
            }
        }

        return ApplicationResult<IReadOnlyCollection<SubjectEnrollmentImportRawRow>>.Success(rows);
    }

    private static IReadOnlyCollection<ApplicationError> ValidateSubjectEnrollmentImportHeaders(
        IReadOnlyDictionary<string, int> headerIndex)
    {
        var errors = new List<ApplicationError>();

        if (!headerIndex.ContainsKey("codigomateria"))
        {
            errors.Add(new ApplicationError(
                "SubjectEnrollmentImport.SubjectCodeHeaderMissing",
                "Column CodigoMateria is required."));
        }

        if (!headerIndex.ContainsKey("materia"))
        {
            errors.Add(new ApplicationError(
                "SubjectEnrollmentImport.SubjectNameHeaderMissing",
                "Column Materia is required."));
        }

        if (!headerIndex.ContainsKey("alumnosinscriptos"))
        {
            errors.Add(new ApplicationError(
                "SubjectEnrollmentImport.EnrolledStudentCountHeaderMissing",
                "Column AlumnosInscriptos is required."));
        }

        return errors;
    }

    private static IReadOnlyList<SubjectEnrollmentImportPreviewRowDto> BuildSubjectEnrollmentImportPreviewRows(
        IReadOnlyCollection<SubjectEnrollmentImportRawRow> rawRows,
        SubjectEnrollmentImportContext context)
    {
        var normalizedCodeCounts = rawRows
            .Select(row => TryNormalizeSubjectCode(row.SubjectCode, out var normalizedCode, out _)
                ? normalizedCode
                : null)
            .Where(code => code is not null)
            .GroupBy(code => code!, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);
        var rows = new List<SubjectEnrollmentImportPreviewRowDto>(rawRows.Count);

        foreach (var rawRow in rawRows)
        {
            var subjectCode = rawRow.SubjectCode?.Trim() ?? string.Empty;
            var providedSubjectName = string.IsNullOrWhiteSpace(rawRow.SubjectName)
                ? null
                : rawRow.SubjectName.Trim();

            if (!TryNormalizeSubjectCode(rawRow.SubjectCode, out var normalizedCode, out var codeError))
            {
                rows.Add(CreateImportErrorRow(rawRow, subjectCode, providedSubjectName, codeError.Code, codeError.Message));
                continue;
            }

            if (normalizedCodeCounts.TryGetValue(normalizedCode, out var codeOccurrences) && codeOccurrences > 1)
            {
                rows.Add(CreateImportErrorRow(
                    rawRow,
                    subjectCode,
                    providedSubjectName,
                    "SubjectEnrollmentImport.DuplicateSubjectCode",
                    "El código de materia está repetido en el archivo."));
                continue;
            }

            if (!context.SubjectsByCode.TryGetValue(normalizedCode, out var subject))
            {
                rows.Add(CreateImportErrorRow(
                    rawRow,
                    subjectCode,
                    providedSubjectName,
                    "SubjectEnrollmentImport.SubjectNotFound",
                    "No existe una materia activa o inactiva con ese código en la carrera seleccionada."));
                continue;
            }

            if (!subject.IsActive)
            {
                rows.Add(CreateImportErrorRow(
                    rawRow,
                    subjectCode,
                    providedSubjectName,
                    "SubjectEnrollmentImport.SubjectInactive",
                    "La materia está inactiva y no puede recibir matrículas por importación.",
                    subject));
                continue;
            }

            if (!string.IsNullOrWhiteSpace(providedSubjectName)
                && !NormalizeSubjectNameForComparison(providedSubjectName).Equals(
                    NormalizeSubjectNameForComparison(subject.Name),
                    StringComparison.Ordinal))
            {
                rows.Add(CreateImportErrorRow(
                    rawRow,
                    subjectCode,
                    providedSubjectName,
                    "SubjectNameMismatch",
                    "El nombre informado no coincide con la materia encontrada por código.",
                    subject));
                continue;
            }

            if (!TryParseEnrolledStudentCount(rawRow.EnrolledStudentCount, out var enrolledStudentCount, out var countError))
            {
                rows.Add(CreateImportErrorRow(
                    rawRow,
                    subjectCode,
                    providedSubjectName,
                    countError.Code,
                    countError.Message,
                    subject));
                continue;
            }

            var currentCount = context.EnrollmentsBySubjectId.TryGetValue(subject.Id, out var existingCount)
                ? (int?)existingCount
                : null;
            var status = currentCount is null
                ? SubjectEnrollmentImportStatusCreate
                : currentCount.Value == enrolledStudentCount
                    ? SubjectEnrollmentImportStatusUnchanged
                    : SubjectEnrollmentImportStatusUpdate;

            rows.Add(new SubjectEnrollmentImportPreviewRowDto(
                rawRow.RowNumber,
                normalizedCode,
                providedSubjectName,
                subject.Id,
                subject.Name,
                currentCount,
                enrolledStudentCount,
                status,
                ErrorCode: null,
                ErrorMessage: null));
        }

        return rows;
    }

    private static SubjectEnrollmentImportPreviewRowDto CreateImportErrorRow(
        SubjectEnrollmentImportRawRow rawRow,
        string subjectCode,
        string? providedSubjectName,
        string errorCode,
        string errorMessage,
        SubjectEnrollmentImportSubject? subject = null)
    {
        return new SubjectEnrollmentImportPreviewRowDto(
            rawRow.RowNumber,
            subjectCode,
            providedSubjectName,
            subject?.Id,
            subject?.Name,
            CurrentEnrolledStudentCount: null,
            NewEnrolledStudentCount: null,
            SubjectEnrollmentImportStatusError,
            errorCode,
            errorMessage);
    }

    private static IReadOnlyCollection<ApplicationError> ValidateSubjectEnrollmentImportContextIds(
        Guid careerId,
        Guid academicCycleId)
    {
        var errors = new List<ApplicationError>();

        if (careerId == Guid.Empty)
        {
            errors.Add(new ApplicationError(
                "SubjectEnrollmentImport.CareerIdRequired",
                "CareerId is required."));
        }

        if (academicCycleId == Guid.Empty)
        {
            errors.Add(new ApplicationError(
                "SubjectEnrollmentImport.AcademicCycleIdRequired",
                "AcademicCycleId is required."));
        }

        return errors;
    }

    private static string? NormalizeTemplateFormat(string? format)
    {
        var normalized = string.IsNullOrWhiteSpace(format)
            ? "xlsx"
            : format.Trim().ToLowerInvariant();

        return normalized is "xlsx" or "csv" ? normalized : null;
    }

    private static byte[] CreateSubjectEnrollmentCsvTemplate(
        IReadOnlyCollection<SubjectEnrollmentTemplateRow> rows)
    {
        using var memoryStream = new MemoryStream();
        using (var writer = new StreamWriter(memoryStream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true), leaveOpen: true))
        using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
        {
            csv.WriteField("CodigoMateria");
            csv.WriteField("Materia");
            csv.WriteField("AlumnosInscriptos");
            csv.NextRecord();

            foreach (var row in rows)
            {
                csv.WriteField(SanitizeCsvText(row.SubjectCode));
                csv.WriteField(SanitizeCsvText(row.SubjectName));
                csv.WriteField(row.EnrolledStudentCount?.ToString(CultureInfo.InvariantCulture) ?? string.Empty);
                csv.NextRecord();
            }
        }

        return memoryStream.ToArray();
    }

    private static byte[] CreateSubjectEnrollmentXlsxTemplate(
        IReadOnlyCollection<SubjectEnrollmentTemplateRow> rows)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.AddWorksheet(SubjectEnrollmentImportWorksheetName);
        worksheet.Cell(1, 1).Value = "Código de materia";
        worksheet.Cell(1, 2).Value = "Materia";
        worksheet.Cell(1, 3).Value = "Alumnos inscriptos";

        var header = worksheet.Range(1, 1, 1, 3);
        header.Style.Font.Bold = true;
        header.SetAutoFilter();
        worksheet.SheetView.FreezeRows(1);
        worksheet.Column(1).Width = 22;
        worksheet.Column(2).Width = 42;
        worksheet.Column(3).Width = 22;

        var rowNumber = 2;

        foreach (var row in rows)
        {
            worksheet.Cell(rowNumber, 1).Value = row.SubjectCode;
            worksheet.Cell(rowNumber, 2).Value = row.SubjectName;

            if (row.EnrolledStudentCount is not null)
            {
                worksheet.Cell(rowNumber, 3).Value = row.EnrolledStudentCount.Value;
            }

            rowNumber++;
        }

        using var memoryStream = new MemoryStream();
        workbook.SaveAs(memoryStream);
        return memoryStream.ToArray();
    }

    private static Dictionary<string, int> BuildHeaderIndex(string[]? headerRecord)
    {
        var headerIndex = new Dictionary<string, int>(StringComparer.Ordinal);

        if (headerRecord is null)
        {
            return headerIndex;
        }

        for (var index = 0; index < headerRecord.Length; index++)
        {
            var normalizedHeader = NormalizeImportHeader(headerRecord[index]);
            normalizedHeader = normalizedHeader switch
            {
                "codigodemateria" => "codigomateria",
                "alumnosinscriptos" => "alumnosinscriptos",
                _ => normalizedHeader
            };

            if (!string.IsNullOrEmpty(normalizedHeader) && !headerIndex.ContainsKey(normalizedHeader))
            {
                headerIndex[normalizedHeader] = index;
            }
        }

        return headerIndex;
    }

    private static string GetCsvField(CsvReader csv, int index)
    {
        var parser = csv.Context.Parser;

        return parser is not null && index >= 0 && index < parser.Count
            ? csv.GetField(index) ?? string.Empty
            : string.Empty;
    }

    private static bool TryNormalizeSubjectCode(
        string? value,
        out string normalizedCode,
        out ApplicationError error)
    {
        normalizedCode = string.Empty;
        error = new ApplicationError("SubjectEnrollmentImport.SubjectCodeInvalid", "Subject code is invalid.");

        if (string.IsNullOrWhiteSpace(value))
        {
            error = new ApplicationError(
                "SubjectEnrollmentImport.SubjectCodeRequired",
                "CodigoMateria is required.");
            return false;
        }

        normalizedCode = value.Trim().ToLowerInvariant();

        if (normalizedCode.Length > 50 || !SubjectCodeRegex.IsMatch(normalizedCode))
        {
            error = new ApplicationError(
                "SubjectEnrollmentImport.SubjectCodeInvalid",
                "CodigoMateria has an invalid format.");
            return false;
        }

        return true;
    }

    private static bool TryParseEnrolledStudentCount(
        string? value,
        out int enrolledStudentCount,
        out ApplicationError error)
    {
        enrolledStudentCount = 0;
        error = new ApplicationError("SubjectEnrollmentImport.EnrolledStudentCountInvalid", "AlumnosInscriptos is invalid.");

        if (string.IsNullOrWhiteSpace(value))
        {
            error = new ApplicationError(
                "SubjectEnrollmentImport.EnrolledStudentCountRequired",
                "AlumnosInscriptos is required.");
            return false;
        }

        if (!int.TryParse(value.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out enrolledStudentCount))
        {
            error = new ApplicationError(
                "SubjectEnrollmentImport.EnrolledStudentCountNotInteger",
                "AlumnosInscriptos must be an integer.");
            return false;
        }

        if (enrolledStudentCount <= 0)
        {
            error = new ApplicationError(
                "SubjectEnrollmentImport.EnrolledStudentCountInvalid",
                "AlumnosInscriptos must be greater than zero.");
            return false;
        }

        return true;
    }

    private static string NormalizeImportHeader(string? value)
    {
        var normalized = RemoveDiacritics(value ?? string.Empty)
            .Trim()
            .ToLowerInvariant();
        var builder = new StringBuilder(normalized.Length);

        foreach (var character in normalized)
        {
            if (char.IsLetterOrDigit(character))
            {
                builder.Append(character);
            }
        }

        return builder.ToString();
    }

    private static string NormalizeSubjectNameForComparison(string value)
    {
        return Regex.Replace(value.Trim().ToLowerInvariant(), "\\s+", " ");
    }

    private static string RemoveDiacritics(string value)
    {
        var normalized = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);

        foreach (var character in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(character);
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }

    private static string SanitizeCsvText(string value)
    {
        var trimmedStart = value.TrimStart();

        return trimmedStart.StartsWith("=", StringComparison.Ordinal)
            || trimmedStart.StartsWith("+", StringComparison.Ordinal)
            || trimmedStart.StartsWith("-", StringComparison.Ordinal)
            || trimmedStart.StartsWith("@", StringComparison.Ordinal)
            ? $"'{value}"
            : value;
    }

    private static int CountOccurrences(string value, char target)
    {
        return value.Count(character => character == target);
    }

    private static string? NormalizeEmailForComparison(string? email)
    {
        return string.IsNullOrWhiteSpace(email)
            ? null
            : email.Trim().ToUpperInvariant();
    }

    private Task WriteAcademicAuditAsync(
        string action,
        string entityType,
        Guid entityId,
        string description,
        object? metadata,
        CancellationToken cancellationToken)
    {
        return _auditWriter.WriteAsync(
            action,
            "academic_catalog",
            entityType,
            entityId,
            description,
            metadata,
            cancellationToken);
    }

    private static IReadOnlyCollection<object> BuildChangedFields(params (string Field, object? OldValue, object? NewValue)[] fields)
    {
        return fields
            .Where(field => !Equals(field.OldValue, field.NewValue))
            .Select(field => new
            {
                field.Field,
                OldValue = field.OldValue,
                NewValue = field.NewValue
            })
            .ToArray();
    }

    private static string BuildTeacherDisplayName(Teacher teacher)
    {
        return $"{teacher.FirstName} {teacher.LastName}".Trim();
    }

    private static bool IsUniqueViolation(DbUpdateException exception)
    {
        return exception.InnerException is PostgresException postgresException
            && postgresException.SqlState == PostgresErrorCodes.UniqueViolation;
    }

    private async Task AddSurveyAttentionItemsAsync(
        Guid careerId,
        Guid academicCycleId,
        ICollection<AcademicAttentionItemDto> items,
        CancellationToken cancellationToken)
    {
        var studentAssignmentsWithoutExpectedCount = await _dbContext.SurveyAssignments
            .AsNoTracking()
            .Where(assignment => assignment.CareerId == careerId)
            .Where(assignment => assignment.AcademicCycleId == academicCycleId)
            .Where(assignment => assignment.IsActive)
            .Where(assignment => assignment.Survey.Target == SurveyTarget.Student)
            .Where(assignment => assignment.ExpectedRespondentCount == null)
            .Select(assignment => assignment.Id)
            .ToArrayAsync(cancellationToken);

        if (studentAssignmentsWithoutExpectedCount.Length > 0)
        {
            items.Add(CreateCountAlert(
                "SurveyAssignment.ExpectedEnrollmentMissing",
                "warning",
                "Encuestas de estudiantes sin matrícula esperada",
                studentAssignmentsWithoutExpectedCount.Length == 1
                    ? "Una asignación de encuesta a estudiantes no tiene matrícula esperada asociada."
                    : $"{studentAssignmentsWithoutExpectedCount.Length} asignaciones de encuestas a estudiantes no tienen matrícula esperada asociada.",
                "SurveyAssignment",
                studentAssignmentsWithoutExpectedCount.Length == 1 ? studentAssignmentsWithoutExpectedCount[0] : null,
                "ReviewSurveyAssignments",
                studentAssignmentsWithoutExpectedCount.Length));
        }

        var assignmentsWithoutResponses = await _dbContext.SurveyAssignments
            .AsNoTracking()
            .Where(assignment => assignment.CareerId == careerId)
            .Where(assignment => assignment.AcademicCycleId == academicCycleId)
            .Where(assignment => assignment.IsActive)
            .Where(assignment => !_dbContext.SurveyResponses.Any(
                response => response.SurveySession.SurveyAssignmentId == assignment.Id))
            .Select(assignment => assignment.Id)
            .ToArrayAsync(cancellationToken);

        if (assignmentsWithoutResponses.Length > 0)
        {
            items.Add(CreateCountAlert(
                "SurveyAssignment.WithoutResponses",
                "info",
                "Encuestas asignadas sin respuestas",
                assignmentsWithoutResponses.Length == 1
                    ? "Una encuesta asignada todavía no recibió respuestas."
                    : $"{assignmentsWithoutResponses.Length} encuestas asignadas todavía no recibieron respuestas.",
                "SurveyAssignment",
                assignmentsWithoutResponses.Length == 1 ? assignmentsWithoutResponses[0] : null,
                "ViewSurveyResults",
                assignmentsWithoutResponses.Length));
        }

        var publishedSurveysWithoutAssignments = await _dbContext.Surveys
            .AsNoTracking()
            .Where(survey => survey.Status == SurveyStatus.Published)
            .Where(survey => survey.IsActive)
            .Where(survey => !_dbContext.SurveyAssignments.Any(assignment => assignment.SurveyId == survey.Id))
            .Select(survey => survey.Id)
            .ToArrayAsync(cancellationToken);

        if (publishedSurveysWithoutAssignments.Length > 0)
        {
            items.Add(CreateCountAlert(
                "Survey.PublishedWithoutAssignments",
                "info",
                "Plantillas publicadas sin asignaciones",
                publishedSurveysWithoutAssignments.Length == 1
                    ? "Una plantilla publicada todavía no tiene ninguna asignación."
                    : $"{publishedSurveysWithoutAssignments.Length} plantillas publicadas todavía no tienen ninguna asignación.",
                "Survey",
                publishedSurveysWithoutAssignments.Length == 1 ? publishedSurveysWithoutAssignments[0] : null,
                "ViewSurveyTemplates",
                publishedSurveysWithoutAssignments.Length));
        }
    }

    private static void AddAcademicCycleEndingSoonAlert(
        DateOnly endDate,
        Guid academicCycleId,
        ICollection<AcademicAttentionItemDto> items)
    {
        var todayUtc = DateOnly.FromDateTime(DateTime.UtcNow);
        var daysRemaining = endDate.DayNumber - todayUtc.DayNumber;

        if (daysRemaining is < 0 or > AcademicCycleEndingSoonDays)
        {
            return;
        }

        var description = daysRemaining == 0
            ? "El ciclo lectivo finaliza hoy."
            : $"El ciclo lectivo finaliza en {daysRemaining} dias.";

        items.Add(CreateCountAlert(
            "AcademicCycle.EndingSoon",
            "info",
            "Ciclo lectivo próximo a finalizar",
            description,
            "AcademicCycle",
            academicCycleId,
            "ReviewAcademicCycle",
            1));
    }

    private static AcademicAttentionItemDto CreateCountAlert(
        string code,
        string severity,
        string title,
        string description,
        string entityType,
        Guid? entityId,
        string actionCode,
        int count)
    {
        return new AcademicAttentionItemDto(
            code,
            severity,
            title,
            description,
            entityType,
            entityId,
            actionCode,
            count);
    }

    private static string FormatSubjectCountDescription(
        IReadOnlyCollection<AttentionSubject> subjects,
        string singularSuffix,
        string pluralSuffix)
    {
        if (subjects.Count == 1)
        {
            return $"{subjects.Single().Name} {singularSuffix}";
        }

        return $"{subjects.Count} {pluralSuffix}";
    }

    private sealed record ParsedSubjectEnrollmentImport(
        SubjectEnrollmentImportContext Context,
        SubjectEnrollmentImportPreviewDto Preview);

    private sealed record SubjectEnrollmentImportContext(
        Career Career,
        AcademicCycle AcademicCycle,
        IReadOnlyDictionary<string, SubjectEnrollmentImportSubject> SubjectsByCode,
        IReadOnlyDictionary<Guid, int> EnrollmentsBySubjectId)
    {
        public IReadOnlyCollection<SubjectEnrollmentImportSubject> Subjects => SubjectsByCode.Values.ToArray();
    }

    private sealed record SubjectEnrollmentImportSubject(
        Guid Id,
        Guid CareerId,
        string Code,
        string Name,
        int Year,
        bool IsActive);

    private sealed record SubjectEnrollmentImportRawRow(
        int RowNumber,
        string? SubjectCode,
        string? SubjectName,
        string? EnrolledStudentCount)
    {
        public bool IsBlank =>
            string.IsNullOrWhiteSpace(SubjectCode)
            && string.IsNullOrWhiteSpace(SubjectName)
            && string.IsNullOrWhiteSpace(EnrolledStudentCount);
    }

    private sealed record SubjectEnrollmentTemplateRow(
        string SubjectCode,
        string SubjectName,
        int? EnrolledStudentCount);

    private sealed record AttentionSubject(Guid Id, string Name);
}
