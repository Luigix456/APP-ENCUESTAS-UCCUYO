using AcademicSurveySystem.Application.Academic.AcademicUnits;
using AcademicSurveySystem.Application.Academic.Attention;
using AcademicSurveySystem.Application.Academic.Careers;
using AcademicSurveySystem.Application.Academic.SubjectEnrollments;
using AcademicSurveySystem.Application.Audit;
using AcademicSurveySystem.Application.Common.Results;
using AcademicSurveySystem.Domain.Academic.Entities;
using AcademicSurveySystem.Domain.Academic.Enums;
using AcademicSurveySystem.Domain.Surveys.Entities;
using AcademicSurveySystem.Domain.Surveys.Enums;
using AcademicSurveySystem.Infrastructure.Academic;
using AcademicSurveySystem.Infrastructure.Persistence;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Text;

namespace AcademicSurveySystem.UnitTests.Academic;

public sealed class AcademicCatalogServiceTests
{
    private static readonly DateTimeOffset CreatedAtUtc =
        new(2026, 10, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task CreateAcademicUnitAsync_CreatesActiveAcademicUnit()
    {
        using var context = CreateContext();
        var service = new AcademicCatalogService(context);

        var result = await service.CreateAcademicUnitAsync(
            new CreateAcademicUnitRequest("ECONOMICAS", "Facultad de Ciencias Economicas y Empresariales"),
            CancellationToken.None);

        Assert.Equal(ApplicationResultStatus.Success, result.Status);
        Assert.Equal("economicas", result.Value!.Code);
        Assert.Equal("Facultad de Ciencias Economicas y Empresariales", result.Value.Name);
        Assert.True(result.Value.IsActive);
    }

    [Fact]
    public async Task CreateCareerAsync_ReturnsCareerWithAcademicUnitFields()
    {
        using var context = CreateContext();
        var unit = SeedAcademicUnit(context, "economicas", "Facultad de Ciencias Economicas");
        var service = new AcademicCatalogService(context);

        var result = await service.CreateCareerAsync(
            new CreateCareerRequest(unit.Id, "contador", "Contador Publico", "Undergraduate"),
            CancellationToken.None);

        Assert.Equal(ApplicationResultStatus.Success, result.Status);
        Assert.Equal(unit.Id, result.Value!.AcademicUnitId);
        Assert.Equal(unit.Code, result.Value.AcademicUnitCode);
        Assert.Equal(unit.Name, result.Value.AcademicUnitName);
    }

    [Fact]
    public async Task GetCareersAsync_FiltersByAcademicUnitId()
    {
        using var context = CreateContext();
        var unitA = SeedAcademicUnit(context, "unidad-a", "Unidad A");
        var unitB = SeedAcademicUnit(context, "unidad-b", "Unidad B");
        context.Careers.AddRange(
            new Career(Guid.NewGuid(), unitA.Id, "career-a", "Career A", CareerType.Undergraduate, CreatedAtUtc),
            new Career(Guid.NewGuid(), unitB.Id, "career-b", "Career B", CareerType.Undergraduate, CreatedAtUtc));
        await context.SaveChangesAsync();
        var service = new AcademicCatalogService(context);

        var result = await service.GetCareersAsync(
            includeInactive: false,
            academicUnitId: unitA.Id,
            CancellationToken.None);

        var career = Assert.Single(result.Value!);
        Assert.Equal("career-a", career.Code);
        Assert.Equal(unitA.Id, career.AcademicUnitId);
    }

    [Fact]
    public async Task CreateCareerAsync_RejectsMissingAcademicUnit()
    {
        using var context = CreateContext();
        var service = new AcademicCatalogService(context);

        var result = await service.CreateCareerAsync(
            new CreateCareerRequest(Guid.NewGuid(), "contador", "Contador Publico", "Undergraduate"),
            CancellationToken.None);

        Assert.Equal(ApplicationResultStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task CreateCareerAsync_RejectsInactiveAcademicUnit()
    {
        using var context = CreateContext();
        var unit = SeedAcademicUnit(context, "economicas", "Facultad de Ciencias Economicas");
        unit.Deactivate(CreatedAtUtc.AddMinutes(1));
        await context.SaveChangesAsync();
        var service = new AcademicCatalogService(context);

        var result = await service.CreateCareerAsync(
            new CreateCareerRequest(unit.Id, "contador", "Contador Publico", "Undergraduate"),
            CancellationToken.None);

        Assert.Equal(ApplicationResultStatus.Validation, result.Status);
        Assert.Contains(result.Errors, error => error.Code == "Career.AcademicUnitInactive");
    }

    [Fact]
    public async Task GetCareerTeachersAsync_ReturnsUniqueTeachersForCareerAndAcademicCycle()
    {
        using var context = CreateContext();
        var unit = SeedAcademicUnit(context, "economicas", "Facultad de Ciencias Economicas");
        var career = new Career(Guid.NewGuid(), unit.Id, "contador", "Contador Publico", CareerType.Undergraduate, CreatedAtUtc);
        var otherCareer = new Career(Guid.NewGuid(), unit.Id, "sistemas", "Sistemas", CareerType.Undergraduate, CreatedAtUtc);
        var subjectA = new Subject(Guid.NewGuid(), career.Id, "contabilidad", "Contabilidad", 1, SubjectPeriod.Annual, CreatedAtUtc);
        var subjectB = new Subject(Guid.NewGuid(), career.Id, "costos", "Costos", 1, SubjectPeriod.Annual, CreatedAtUtc);
        var otherSubject = new Subject(Guid.NewGuid(), otherCareer.Id, "programacion", "Programacion", 1, SubjectPeriod.Annual, CreatedAtUtc);
        var cycle = new AcademicCycle(
            Guid.NewGuid(),
            2026,
            AcademicCyclePeriod.Annual,
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 12, 31),
            CreatedAtUtc);
        var otherCycle = new AcademicCycle(
            Guid.NewGuid(),
            2027,
            AcademicCyclePeriod.Annual,
            new DateOnly(2027, 1, 1),
            new DateOnly(2027, 12, 31),
            CreatedAtUtc);
        var teacher = new Teacher(Guid.NewGuid(), "Ada", "Lovelace", "ada@example.com", CreatedAtUtc);
        var otherTeacher = new Teacher(Guid.NewGuid(), "Grace", "Hopper", "grace@example.com", CreatedAtUtc);
        var unassignedTeacher = new Teacher(Guid.NewGuid(), "No", "Assignment", "no.assignment@example.com", CreatedAtUtc);
        context.AddRange(
            career,
            otherCareer,
            subjectA,
            subjectB,
            otherSubject,
            cycle,
            otherCycle,
            teacher,
            otherTeacher,
            unassignedTeacher,
            new TeacherSubjectAssignment(Guid.NewGuid(), teacher.Id, subjectA.Id, cycle.Id, "Titular", CreatedAtUtc),
            new TeacherSubjectAssignment(Guid.NewGuid(), teacher.Id, subjectB.Id, cycle.Id, "Adjunto", CreatedAtUtc),
            new TeacherSubjectAssignment(Guid.NewGuid(), teacher.Id, otherSubject.Id, cycle.Id, "Titular", CreatedAtUtc),
            new TeacherSubjectAssignment(Guid.NewGuid(), otherTeacher.Id, subjectA.Id, otherCycle.Id, "Titular", CreatedAtUtc),
            new TeacherSubjectAssignment(Guid.NewGuid(), otherTeacher.Id, otherSubject.Id, cycle.Id, "Titular", CreatedAtUtc));
        await context.SaveChangesAsync();
        var service = new AcademicCatalogService(context);

        var result = await service.GetCareerTeachersAsync(
            career.Id,
            cycle.Id,
            includeInactive: false,
            CancellationToken.None);

        var teacherResult = Assert.Single(result.Value!);
        Assert.Equal(teacher.Id, teacherResult.Id);
        Assert.DoesNotContain(result.Value!, item => item.Id == unassignedTeacher.Id);

        var otherCareerResult = await service.GetCareerTeachersAsync(
            otherCareer.Id,
            cycle.Id,
            includeInactive: false,
            CancellationToken.None);

        Assert.Contains(otherCareerResult.Value!, item => item.Id == teacher.Id);
        Assert.DoesNotContain(otherCareerResult.Value!, item => item.Id == unassignedTeacher.Id);
    }

    [Fact]
    public async Task SetSubjectEnrollmentAsync_CreatesEnrollmentForSubjectAndCycle()
    {
        using var context = CreateContext();
        var academic = SeedAcademicContext(context);
        var service = new AcademicCatalogService(context);

        var result = await service.SetSubjectEnrollmentAsync(
            academic.Subject.Id,
            academic.Cycle.Id,
            new SetSubjectEnrollmentRequest(35),
            CancellationToken.None);

        Assert.Equal(ApplicationResultStatus.Success, result.Status);
        Assert.Equal(35, result.Value!.EnrolledStudentCount);
        Assert.Equal(academic.Subject.Id, result.Value.SubjectId);
        Assert.Equal(academic.Cycle.Id, result.Value.AcademicCycleId);
        Assert.Equal(academic.Career.Id, result.Value.CareerId);
    }

    [Fact]
    public async Task SetSubjectEnrollmentAsync_UpdatesExistingEnrollmentWithoutDuplicating()
    {
        using var context = CreateContext();
        var academic = SeedAcademicContext(context);
        var service = new AcademicCatalogService(context);

        await service.SetSubjectEnrollmentAsync(
            academic.Subject.Id,
            academic.Cycle.Id,
            new SetSubjectEnrollmentRequest(35),
            CancellationToken.None);
        var update = await service.SetSubjectEnrollmentAsync(
            academic.Subject.Id,
            academic.Cycle.Id,
            new SetSubjectEnrollmentRequest(42),
            CancellationToken.None);

        Assert.Equal(ApplicationResultStatus.Success, update.Status);
        Assert.Equal(42, update.Value!.EnrolledStudentCount);
        Assert.Equal(1, await context.SubjectEnrollments.CountAsync());
    }

    [Fact]
    public async Task SetSubjectEnrollmentAsync_RejectsNonPositiveCount()
    {
        using var context = CreateContext();
        var academic = SeedAcademicContext(context);
        var service = new AcademicCatalogService(context);

        var result = await service.SetSubjectEnrollmentAsync(
            academic.Subject.Id,
            academic.Cycle.Id,
            new SetSubjectEnrollmentRequest(0),
            CancellationToken.None);

        Assert.Equal(ApplicationResultStatus.Validation, result.Status);
        Assert.Contains(result.Errors, error => error.Code == "SubjectEnrollment.EnrolledStudentCountInvalid");
    }

    [Fact]
    public async Task SetSubjectEnrollmentAsync_RejectsMissingSubjectOrCycle()
    {
        using var context = CreateContext();
        var academic = SeedAcademicContext(context);
        var service = new AcademicCatalogService(context);

        var missingSubject = await service.SetSubjectEnrollmentAsync(
            Guid.NewGuid(),
            academic.Cycle.Id,
            new SetSubjectEnrollmentRequest(20),
            CancellationToken.None);
        var missingCycle = await service.SetSubjectEnrollmentAsync(
            academic.Subject.Id,
            Guid.NewGuid(),
            new SetSubjectEnrollmentRequest(20),
            CancellationToken.None);

        Assert.Equal(ApplicationResultStatus.NotFound, missingSubject.Status);
        Assert.Equal(ApplicationResultStatus.NotFound, missingCycle.Status);
    }

    [Fact]
    public async Task GetSubjectEnrollmentsAsync_FiltersByCareer()
    {
        using var context = CreateContext();
        var academic = SeedAcademicContext(context);
        var other = SeedAcademicContext(context, "other-unit", "other-career", "other-subject", 2027);
        context.SubjectEnrollments.AddRange(
            new SubjectEnrollment(Guid.NewGuid(), academic.Subject.Id, academic.Cycle.Id, 35, CreatedAtUtc),
            new SubjectEnrollment(Guid.NewGuid(), other.Subject.Id, other.Cycle.Id, 12, CreatedAtUtc));
        await context.SaveChangesAsync();
        var service = new AcademicCatalogService(context);

        var result = await service.GetSubjectEnrollmentsAsync(
            subjectId: null,
            academicCycleId: null,
            careerId: academic.Career.Id,
            CancellationToken.None);

        var enrollment = Assert.Single(result.Value!);
        Assert.Equal(academic.Subject.Id, enrollment.SubjectId);
        Assert.Equal(35, enrollment.EnrolledStudentCount);
    }

    [Fact]
    public async Task PreviewSubjectEnrollmentImportAsync_ReturnsPreviewWithoutSaving()
    {
        using var context = CreateContext();
        var academic = SeedAcademicContext(context);
        var service = new AcademicCatalogService(context);
        var file = CreateCsvImportFile(
            "CodigoMateria;Materia;AlumnosInscriptos\r\n" +
            "CONTABILIDAD;Materia contabilidad;35\r\n");

        var result = await service.PreviewSubjectEnrollmentImportAsync(
            academic.Career.Id,
            academic.Cycle.Id,
            file,
            CancellationToken.None);

        Assert.Equal(ApplicationResultStatus.Success, result.Status);
        Assert.Equal(1, result.Value!.CreateRows);
        Assert.Equal(0, await context.SubjectEnrollments.CountAsync());
    }

    [Fact]
    public async Task PreviewSubjectEnrollmentImportAsync_DetectsDuplicateNameMismatchAndInvalidCount()
    {
        using var context = CreateContext();
        var academic = SeedAcademicContext(context);
        context.Subjects.AddRange(
            new Subject(Guid.NewGuid(), academic.Career.Id, "costos", "Materia costos", 1, SubjectPeriod.Annual, CreatedAtUtc),
            new Subject(Guid.NewGuid(), academic.Career.Id, "algebra", "Materia algebra", 1, SubjectPeriod.Annual, CreatedAtUtc));
        await context.SaveChangesAsync();
        var service = new AcademicCatalogService(context);
        var file = CreateCsvImportFile(
            "CodigoMateria,Materia,AlumnosInscriptos\r\n" +
            "contabilidad,Materia contabilidad,35\r\n" +
            "CONTABILIDAD,Materia contabilidad,40\r\n" +
            "otro,Materia inexistente,10\r\n" +
            "costos,Nombre incorrecto,15\r\n" +
            "algebra,Materia algebra,0\r\n");

        var result = await service.PreviewSubjectEnrollmentImportAsync(
            academic.Career.Id,
            academic.Cycle.Id,
            file,
            CancellationToken.None);

        Assert.Equal(ApplicationResultStatus.Success, result.Status);
        Assert.Equal(5, result.Value!.ErrorRows);
        Assert.Contains(result.Value.Rows, row => row.ErrorCode == "SubjectEnrollmentImport.DuplicateSubjectCode");
        Assert.Contains(result.Value.Rows, row => row.ErrorCode == "SubjectEnrollmentImport.SubjectNotFound");
        Assert.Contains(result.Value.Rows, row => row.ErrorCode == "SubjectNameMismatch");
        Assert.Contains(result.Value.Rows, row => row.ErrorCode == "SubjectEnrollmentImport.EnrolledStudentCountInvalid");
    }

    [Fact]
    public async Task PreviewSubjectEnrollmentImportAsync_ReadsXlsxRows()
    {
        using var context = CreateContext();
        var academic = SeedAcademicContext(context);
        var service = new AcademicCatalogService(context);
        var file = CreateXlsxImportFile(("contabilidad", "Materia contabilidad", "35"));

        var result = await service.PreviewSubjectEnrollmentImportAsync(
            academic.Career.Id,
            academic.Cycle.Id,
            file,
            CancellationToken.None);

        Assert.Equal(ApplicationResultStatus.Success, result.Status);
        Assert.Equal(1, result.Value!.CreateRows);
        Assert.Equal("contabilidad", Assert.Single(result.Value.Rows).SubjectCode);
    }

    [Fact]
    public async Task ImportSubjectEnrollmentsAsync_CreatesUpdatesLeavesUnchangedAndWritesSingleAuditEntry()
    {
        using var context = CreateContext();
        var academic = SeedAcademicContext(context);
        var subjectToCreate = new Subject(Guid.NewGuid(), academic.Career.Id, "costos", "Materia costos", 1, SubjectPeriod.Annual, CreatedAtUtc);
        var subjectUnchanged = new Subject(Guid.NewGuid(), academic.Career.Id, "algebra", "Materia algebra", 1, SubjectPeriod.Annual, CreatedAtUtc);
        var existingEnrollment = new SubjectEnrollment(Guid.NewGuid(), academic.Subject.Id, academic.Cycle.Id, 35, CreatedAtUtc);
        var unchangedEnrollment = new SubjectEnrollment(Guid.NewGuid(), subjectUnchanged.Id, academic.Cycle.Id, 40, CreatedAtUtc);
        context.AddRange(subjectToCreate, subjectUnchanged, existingEnrollment, unchangedEnrollment);
        await context.SaveChangesAsync();
        var auditWriter = new RecordingAuditWriter();
        var service = new AcademicCatalogService(context, auditWriter);
        var file = CreateCsvImportFile(
            "CodigoMateria,Materia,AlumnosInscriptos\r\n" +
            "contabilidad,Materia contabilidad,42\r\n" +
            "costos,Materia costos,20\r\n" +
            "algebra,Materia algebra,40\r\n");

        var result = await service.ImportSubjectEnrollmentsAsync(
            academic.Career.Id,
            academic.Cycle.Id,
            file,
            CancellationToken.None);

        Assert.Equal(ApplicationResultStatus.Success, result.Status);
        Assert.Equal(1, result.Value!.CreatedCount);
        Assert.Equal(1, result.Value.UpdatedCount);
        Assert.Equal(1, result.Value.UnchangedCount);
        Assert.Equal(42, existingEnrollment.EnrolledStudentCount);
        Assert.Equal(CreatedAtUtc, unchangedEnrollment.UpdatedAtUtc);
        Assert.Equal(3, await context.SubjectEnrollments.CountAsync());
        var auditEntry = Assert.Single(auditWriter.Entries);
        Assert.Equal("academic.enrollment.bulk_imported", auditEntry.Action);
    }

    [Fact]
    public async Task ImportSubjectEnrollmentsAsync_DoesNotWriteAuditWhenAllRowsAreUnchanged()
    {
        using var context = CreateContext();
        var academic = SeedAcademicContext(context);
        context.SubjectEnrollments.Add(
            new SubjectEnrollment(Guid.NewGuid(), academic.Subject.Id, academic.Cycle.Id, 35, CreatedAtUtc));
        await context.SaveChangesAsync();
        var auditWriter = new RecordingAuditWriter();
        var service = new AcademicCatalogService(context, auditWriter);

        var result = await service.ImportSubjectEnrollmentsAsync(
            academic.Career.Id,
            academic.Cycle.Id,
            CreateCsvImportFile("CodigoMateria,Materia,AlumnosInscriptos\r\ncontabilidad,Materia contabilidad,35\r\n"),
            CancellationToken.None);

        Assert.Equal(ApplicationResultStatus.Success, result.Status);
        Assert.Equal(1, result.Value!.UnchangedCount);
        Assert.Empty(auditWriter.Entries);
    }


    [Fact]
    public async Task PreviewSubjectEnrollmentImportAsync_EnforcesFileSizeAndRowLimits()
    {
        using var context = CreateContext();
        var academic = SeedAcademicContext(context);
        var smallFileOptions = Options.Create(new SubjectEnrollmentImportOptions
        {
            MaxFileSizeBytes = 5,
            MaxRows = 2000
        });
        var smallFileService = new AcademicCatalogService(context, subjectEnrollmentImportOptions: smallFileOptions);

        var tooLarge = await smallFileService.PreviewSubjectEnrollmentImportAsync(
            academic.Career.Id,
            academic.Cycle.Id,
            CreateCsvImportFile("CodigoMateria,Materia,AlumnosInscriptos\r\ncontabilidad,Materia contabilidad,35\r\n"),
            CancellationToken.None);

        var rowLimitOptions = Options.Create(new SubjectEnrollmentImportOptions
        {
            MaxFileSizeBytes = SubjectEnrollmentImportOptions.DefaultMaxFileSizeBytes,
            MaxRows = 1
        });
        var rowLimitService = new AcademicCatalogService(context, subjectEnrollmentImportOptions: rowLimitOptions);
        var tooManyRows = await rowLimitService.PreviewSubjectEnrollmentImportAsync(
            academic.Career.Id,
            academic.Cycle.Id,
            CreateCsvImportFile(
                "CodigoMateria,Materia,AlumnosInscriptos\r\n" +
                "contabilidad,Materia contabilidad,35\r\n" +
                "otro,Otra materia,20\r\n"),
            CancellationToken.None);

        Assert.Equal(ApplicationResultStatus.Validation, tooLarge.Status);
        Assert.Contains(tooLarge.Errors, error => error.Code == "SubjectEnrollmentImport.FileTooLarge");
        Assert.Equal(ApplicationResultStatus.Validation, tooManyRows.Status);
        Assert.Contains(tooManyRows.Errors, error => error.Code == "SubjectEnrollmentImport.TooManyRows");
    }

    [Fact]
    public async Task ImportSubjectEnrollmentsAsync_DoesNotModifyExistingSurveyAssignmentSnapshot()
    {
        using var context = CreateContext();
        var academic = SeedAcademicContext(context);
        var surveyV1 = SeedPublishedSurvey(context, SurveyTarget.Student);
        var surveyV2 = SeedPublishedSurvey(context, SurveyTarget.Student);
        var teacher = new Teacher(Guid.NewGuid(), "Ada", "Lovelace", "ada@example.com", CreatedAtUtc);
        var teacherAssignment = new TeacherSubjectAssignment(
            Guid.NewGuid(),
            teacher.Id,
            academic.Subject.Id,
            academic.Cycle.Id,
            "Titular",
            CreatedAtUtc);
        context.SubjectEnrollments.Add(
            new SubjectEnrollment(Guid.NewGuid(), academic.Subject.Id, academic.Cycle.Id, 35, CreatedAtUtc));
        context.AddRange(teacher, teacherAssignment);
        await context.SaveChangesAsync();
        var assignmentService = new Infrastructure.Surveys.SurveyAssignmentService(context);
        var first = await assignmentService.CreateAssignmentAsync(
            new Application.Surveys.Assignments.CreateSurveyAssignmentRequest(
                surveyV1.Id,
                academic.Career.Id,
                academic.Subject.Id,
                academic.Cycle.Id,
                teacherAssignment.Id),
            CancellationToken.None);
        var catalogService = new AcademicCatalogService(context);

        var import = await catalogService.ImportSubjectEnrollmentsAsync(
            academic.Career.Id,
            academic.Cycle.Id,
            CreateCsvImportFile("CodigoMateria,Materia,AlumnosInscriptos\r\ncontabilidad,Materia contabilidad,40\r\n"),
            CancellationToken.None);
        var second = await assignmentService.CreateAssignmentAsync(
            new Application.Surveys.Assignments.CreateSurveyAssignmentRequest(
                surveyV2.Id,
                academic.Career.Id,
                academic.Subject.Id,
                academic.Cycle.Id,
                teacherAssignment.Id),
            CancellationToken.None);

        Assert.Equal(ApplicationResultStatus.Success, import.Status);
        Assert.Equal(35, first.Value!.ExpectedRespondentCount);
        Assert.Equal(40, second.Value!.ExpectedRespondentCount);
    }

    [Fact]
    public async Task GetCareerAttentionAsync_ReturnsCatalogAlertsForMissingEnrollmentAndTeachers()
    {
        using var context = CreateContext();
        var academic = SeedAcademicContext(context);
        var service = new AcademicCatalogService(context);

        var result = await service.GetCareerAttentionAsync(
            academic.Career.Id,
            academic.Cycle.Id,
            new AcademicAttentionPermissions(IncludeSurveyAlerts: false),
            CancellationToken.None);

        Assert.Equal(ApplicationResultStatus.Success, result.Status);
        Assert.Contains(result.Value!.Items, item => item.Code == "Academic.SubjectsWithoutEnrollment");
        Assert.Contains(result.Value.Items, item => item.Code == "Academic.SubjectsWithoutTeachers");
    }

    [Fact]
    public async Task GetCareerAttentionAsync_WithNoSubjects_ReturnsSuccess()
    {
        using var context = CreateContext();
        var academicUnit = new AcademicUnit(
            Guid.NewGuid(),
            "unit",
            "Unidad Academica",
            CreatedAtUtc);
        var career = new Career(
            Guid.NewGuid(),
            academicUnit.Id,
            "career",
            "Carrera",
            CareerType.Undergraduate,
            CreatedAtUtc);
        var cycle = new AcademicCycle(
            Guid.NewGuid(),
            2026,
            AcademicCyclePeriod.Annual,
            new DateOnly(2026, 3, 1),
            new DateOnly(2026, 12, 20),
            CreatedAtUtc);
        context.AddRange(academicUnit, career, cycle);
        await context.SaveChangesAsync();
        var service = new AcademicCatalogService(context);

        var result = await service.GetCareerAttentionAsync(
            career.Id,
            cycle.Id,
            new AcademicAttentionPermissions(IncludeSurveyAlerts: true),
            CancellationToken.None);

        Assert.Equal(ApplicationResultStatus.Success, result.Status);
    }

    [Fact]
    public async Task GetCareerAttentionAsync_WithNoSurveyAssignments_ReturnsSuccess()
    {
        using var context = CreateContext();
        var academic = SeedAcademicContext(context);
        var teacher = new Teacher(Guid.NewGuid(), "Ada", "Lovelace", "ada@example.com", CreatedAtUtc);
        var teacherAssignment = new TeacherSubjectAssignment(
            Guid.NewGuid(),
            teacher.Id,
            academic.Subject.Id,
            academic.Cycle.Id,
            "Titular",
            CreatedAtUtc);
        context.AddRange(
            teacher,
            teacherAssignment,
            new SubjectEnrollment(Guid.NewGuid(), academic.Subject.Id, academic.Cycle.Id, 30, CreatedAtUtc));
        await context.SaveChangesAsync();
        var service = new AcademicCatalogService(context);

        var result = await service.GetCareerAttentionAsync(
            academic.Career.Id,
            academic.Cycle.Id,
            new AcademicAttentionPermissions(IncludeSurveyAlerts: true),
            CancellationToken.None);

        Assert.Equal(ApplicationResultStatus.Success, result.Status);
        Assert.DoesNotContain(result.Value!.Items, item => item.Code == "SurveyAssignment.WithoutResponses");
    }

    [Fact]
    public async Task GetCareerAttentionAsync_OnlyIncludesSurveyAlertsWhenPermissionAllowsIt()
    {
        using var context = CreateContext();
        var academic = SeedAcademicContext(context);
        var teacher = new Teacher(Guid.NewGuid(), "Ada", "Lovelace", "ada@example.com", CreatedAtUtc);
        var teacherAssignment = new TeacherSubjectAssignment(
            Guid.NewGuid(),
            teacher.Id,
            academic.Subject.Id,
            academic.Cycle.Id,
            "Titular",
            CreatedAtUtc);
        var assignedSurvey = SeedPublishedSurvey(context, SurveyTarget.Student);
        var unassignedSurvey = SeedPublishedSurvey(context, SurveyTarget.Teacher);
        context.AddRange(
            teacher,
            teacherAssignment,
            new SubjectEnrollment(Guid.NewGuid(), academic.Subject.Id, academic.Cycle.Id, 35, CreatedAtUtc),
            new SurveyAssignment(
                Guid.NewGuid(),
                assignedSurvey.Id,
                academic.Career.Id,
                academic.Subject.Id,
                academic.Cycle.Id,
                teacherAssignment.Id,
                expectedRespondentCount: null,
                CreatedAtUtc));
        await context.SaveChangesAsync();
        var service = new AcademicCatalogService(context);

        var withoutSurveyPermission = await service.GetCareerAttentionAsync(
            academic.Career.Id,
            academic.Cycle.Id,
            new AcademicAttentionPermissions(IncludeSurveyAlerts: false),
            CancellationToken.None);
        var withSurveyPermission = await service.GetCareerAttentionAsync(
            academic.Career.Id,
            academic.Cycle.Id,
            new AcademicAttentionPermissions(IncludeSurveyAlerts: true),
            CancellationToken.None);

        Assert.DoesNotContain(
            withoutSurveyPermission.Value!.Items,
            item => item.Code.StartsWith("Survey", StringComparison.Ordinal));
        Assert.Contains(
            withSurveyPermission.Value!.Items,
            item => item.Code == "SurveyAssignment.ExpectedEnrollmentMissing");
        Assert.Contains(
            withSurveyPermission.Value.Items,
            item => item.Code == "SurveyAssignment.WithoutResponses");
        Assert.Contains(
            withSurveyPermission.Value.Items,
            item => item.Code == "Survey.PublishedWithoutAssignments" && item.EntityId == unassignedSurvey.Id);
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var context = new ApplicationDbContext(options);
        context.Database.EnsureCreated();

        return context;
    }

    private static AcademicUnit SeedAcademicUnit(
        ApplicationDbContext context,
        string code,
        string name)
    {
        var unit = new AcademicUnit(Guid.NewGuid(), code, name, CreatedAtUtc);
        context.AcademicUnits.Add(unit);
        context.SaveChanges();

        return unit;
    }

    private static AcademicFixture SeedAcademicContext(
        ApplicationDbContext context,
        string academicUnitCode = "economicas",
        string careerCode = "contador",
        string subjectCode = "contabilidad",
        int year = 2026)
    {
        var academicUnit = new AcademicUnit(
            Guid.NewGuid(),
            academicUnitCode,
            $"Unidad {academicUnitCode}",
            CreatedAtUtc);
        var career = new Career(
            Guid.NewGuid(),
            academicUnit.Id,
            careerCode,
            $"Carrera {careerCode}",
            CareerType.Undergraduate,
            CreatedAtUtc);
        var subject = new Subject(
            Guid.NewGuid(),
            career.Id,
            subjectCode,
            $"Materia {subjectCode}",
            1,
            SubjectPeriod.Annual,
            CreatedAtUtc);
        var cycle = new AcademicCycle(
            Guid.NewGuid(),
            year,
            AcademicCyclePeriod.Annual,
            new DateOnly(year, 1, 1),
            new DateOnly(year, 12, 31),
            CreatedAtUtc);

        context.AddRange(academicUnit, career, subject, cycle);
        context.SaveChanges();

        return new AcademicFixture(academicUnit, career, subject, cycle);
    }

    private static Survey SeedPublishedSurvey(
        ApplicationDbContext context,
        SurveyTarget target)
    {
        var survey = new Survey(
            Guid.NewGuid(),
            Guid.NewGuid(),
            $"Encuesta {Guid.NewGuid():N}",
            null,
            target,
            CreatedAtUtc);
        var section = new SurveySection(
            Guid.NewGuid(),
            survey.Id,
            "Seccion",
            null,
            1,
            CreatedAtUtc);
        var question = new SurveyQuestion(
            Guid.NewGuid(),
            section.Id,
            "Pregunta",
            SurveyQuestionType.SingleChoice,
            isRequired: true,
            allowsComment: false,
            allowsOtherOption: false,
            order: 1,
            CreatedAtUtc);
        question.AddOption(
            new SurveyQuestionOption(Guid.NewGuid(), question.Id, "A", "a", 1, CreatedAtUtc),
            CreatedAtUtc);
        question.AddOption(
            new SurveyQuestionOption(Guid.NewGuid(), question.Id, "B", "b", 2, CreatedAtUtc),
            CreatedAtUtc);
        section.AddQuestion(question, CreatedAtUtc);
        survey.AddSection(section, CreatedAtUtc);
        survey.Publish(CreatedAtUtc.AddMinutes(1));

        context.Surveys.Add(survey);
        context.SaveChanges();

        return survey;
    }

    private static SubjectEnrollmentImportFile CreateCsvImportFile(string content)
    {
        var bytes = Encoding.UTF8.GetBytes(content);

        return new SubjectEnrollmentImportFile(
            "matriculas.csv",
            "text/csv",
            bytes.Length,
            new MemoryStream(bytes));
    }

    private static SubjectEnrollmentImportFile CreateXlsxImportFile(
        params (string SubjectCode, string SubjectName, string EnrolledStudentCount)[] rows)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.AddWorksheet("Matrículas");
        worksheet.Cell(1, 1).Value = "Código de materia";
        worksheet.Cell(1, 2).Value = "Materia";
        worksheet.Cell(1, 3).Value = "Alumnos inscriptos";

        for (var index = 0; index < rows.Length; index++)
        {
            worksheet.Cell(index + 2, 1).Value = rows[index].SubjectCode;
            worksheet.Cell(index + 2, 2).Value = rows[index].SubjectName;
            worksheet.Cell(index + 2, 3).Value = rows[index].EnrolledStudentCount;
        }

        var memoryStream = new MemoryStream();
        workbook.SaveAs(memoryStream);
        memoryStream.Position = 0;

        return new SubjectEnrollmentImportFile(
            "matriculas.xlsx",
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            memoryStream.Length,
            memoryStream);
    }

    private sealed record AcademicFixture(
        AcademicUnit AcademicUnit,
        Career Career,
        Subject Subject,
        AcademicCycle Cycle);

    private sealed class RecordingAuditWriter : IAuditWriter
    {
        public List<AuditEntry> Entries { get; } = [];

        public Task WriteAsync(
            string action,
            string module,
            string entityType,
            Guid? entityId,
            string description,
            object? metadata = null,
            CancellationToken cancellationToken = default)
        {
            Entries.Add(new AuditEntry(action, module, entityType, entityId, description, metadata));
            return Task.CompletedTask;
        }

        public Task FlushAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private sealed record AuditEntry(
        string Action,
        string Module,
        string EntityType,
        Guid? EntityId,
        string Description,
        object? Metadata);
}
