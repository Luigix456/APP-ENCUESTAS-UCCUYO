using AcademicSurveySystem.Application.Academic.AcademicUnits;
using AcademicSurveySystem.Application.Academic.Careers;
using AcademicSurveySystem.Application.Common.Results;
using AcademicSurveySystem.Domain.Academic.Entities;
using AcademicSurveySystem.Domain.Academic.Enums;
using AcademicSurveySystem.Infrastructure.Academic;
using AcademicSurveySystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

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
}
