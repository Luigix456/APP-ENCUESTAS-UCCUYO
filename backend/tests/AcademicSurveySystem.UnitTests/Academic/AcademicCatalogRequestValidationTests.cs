using AcademicSurveySystem.Application.Academic.AcademicUnits;
using AcademicSurveySystem.Application.Academic.AcademicCycles;
using AcademicSurveySystem.Application.Academic.Careers;
using AcademicSurveySystem.Application.Academic.Subjects;
using AcademicSurveySystem.Application.Academic.Teachers;
using AcademicSurveySystem.Application.Academic.TeacherSubjectAssignments;

namespace AcademicSurveySystem.UnitTests.Academic;

public sealed class AcademicCatalogRequestValidationTests
{
    [Fact]
    public void CreateCareerRequest_RejectsEmptyCode()
    {
        var errors = new CreateCareerRequest(Guid.NewGuid(), " ", "Sistemas", "Undergraduate").Validate();

        Assert.Contains(errors, error => error.Code == "Career.CodeRequired");
    }

    [Fact]
    public void CreateCareerRequest_RejectsMissingAcademicUnitId()
    {
        var errors = new CreateCareerRequest(null, "sis", "Sistemas", "Undergraduate").Validate();

        Assert.Contains(errors, error => error.Code == "Career.AcademicUnitIdRequired");
    }

    [Fact]
    public void CreateCareerRequest_RejectsEmptyName()
    {
        var errors = new CreateCareerRequest(Guid.NewGuid(), "sis", " ", "Undergraduate").Validate();

        Assert.Contains(errors, error => error.Code == "Career.NameRequired");
    }

    [Fact]
    public void CreateCareerRequest_RejectsInvalidType()
    {
        var errors = new CreateCareerRequest(Guid.NewGuid(), "sis", "Sistemas", "Invalid").Validate();

        Assert.Contains(errors, error => error.Code == "Career.TypeInvalid");
    }

    [Fact]
    public void CreateAcademicUnitRequest_RejectsEmptyCode()
    {
        var errors = new CreateAcademicUnitRequest(" ", "Facultad").Validate();

        Assert.Contains(errors, error => error.Code == "AcademicUnit.CodeRequired");
    }

    [Fact]
    public void UpdateAcademicUnitRequest_RejectsEmptyName()
    {
        var errors = new UpdateAcademicUnitRequest(" ").Validate();

        Assert.Contains(errors, error => error.Code == "AcademicUnit.NameRequired");
    }

    [Fact]
    public void UpdateCareerRequest_RejectsMissingType()
    {
        var errors = new UpdateCareerRequest("Sistemas", null).Validate();

        Assert.Contains(errors, error => error.Code == "Career.TypeRequired");
    }

    [Fact]
    public void CreateAcademicCycleRequest_RejectsMissingYear()
    {
        var errors = CreateCycleRequest(year: null).Validate();

        Assert.Contains(errors, error => error.Code == "AcademicCycle.YearRequired");
    }

    [Fact]
    public void CreateAcademicCycleRequest_RejectsYearOutOfRange()
    {
        var errors = CreateCycleRequest(year: 1999).Validate();

        Assert.Contains(errors, error => error.Code == "AcademicCycle.YearInvalid");
    }

    [Fact]
    public void CreateAcademicCycleRequest_RejectsInvalidPeriod()
    {
        var errors = CreateCycleRequest(period: "Invalid").Validate();

        Assert.Contains(errors, error => error.Code == "AcademicCycle.PeriodInvalid");
    }

    [Fact]
    public void CreateAcademicCycleRequest_RejectsMissingDates()
    {
        var errors = new CreateAcademicCycleRequest(2026, "Annual", null, null).Validate();

        Assert.Contains(errors, error => error.Code == "AcademicCycle.StartDateRequired");
        Assert.Contains(errors, error => error.Code == "AcademicCycle.EndDateRequired");
    }

    [Fact]
    public void UpdateAcademicCycleRequest_RejectsInvalidDateRange()
    {
        var errors = new UpdateAcademicCycleRequest(
            "Annual",
            new DateOnly(2026, 12, 15),
            new DateOnly(2026, 3, 1)).Validate();

        Assert.Contains(errors, error => error.Code == "AcademicCycle.DateRangeInvalid");
    }

    [Fact]
    public void CreateSubjectRequest_RejectsMissingCareerId()
    {
        var errors = new CreateSubjectRequest(
            null,
            "programacion-i",
            "Programacion I",
            1,
            "FirstSemester").Validate();

        Assert.Contains(errors, error => error.Code == "Subject.CareerIdRequired");
    }

    [Fact]
    public void CreateSubjectRequest_RejectsYearOutOfRange()
    {
        var errors = CreateSubjectRequest(year: 11).Validate();

        Assert.Contains(errors, error => error.Code == "Subject.YearInvalid");
    }

    [Fact]
    public void UpdateSubjectRequest_RejectsInvalidPeriod()
    {
        var errors = new UpdateSubjectRequest("Programacion I", 1, "Invalid").Validate();

        Assert.Contains(errors, error => error.Code == "Subject.PeriodInvalid");
    }

    [Fact]
    public void CreateTeacherRequest_RejectsMissingNames()
    {
        var errors = new CreateTeacherRequest(" ", null, null).Validate();

        Assert.Contains(errors, error => error.Code == "Teacher.FirstNameRequired");
        Assert.Contains(errors, error => error.Code == "Teacher.LastNameRequired");
    }

    [Fact]
    public void CreateTeacherRequest_RejectsInvalidEmail()
    {
        var errors = new CreateTeacherRequest("Juan", "Perez", "invalid-email").Validate();

        Assert.Contains(errors, error => error.Code == "Teacher.EmailInvalid");
    }

    [Fact]
    public void UpdateTeacherRequest_AllowsRemovingEmail()
    {
        var errors = new UpdateTeacherRequest("Juan", "Perez", " ").Validate();

        Assert.DoesNotContain(errors, error => error.Code == "Teacher.EmailInvalid");
    }

    [Fact]
    public void CreateTeacherSubjectAssignmentRequest_RejectsMissingIds()
    {
        var errors = new CreateTeacherSubjectAssignmentRequest(null, Guid.Empty, null, "Titular")
            .Validate();

        Assert.Contains(errors, error => error.Code == "TeacherSubjectAssignment.TeacherIdRequired");
        Assert.Contains(errors, error => error.Code == "TeacherSubjectAssignment.SubjectIdRequired");
        Assert.Contains(errors, error => error.Code == "TeacherSubjectAssignment.AcademicCycleIdRequired");
    }

    [Fact]
    public void UpdateTeacherSubjectAssignmentRequest_RejectsMissingTeachingRole()
    {
        var errors = new UpdateTeacherSubjectAssignmentRequest(" ").Validate();

        Assert.Contains(errors, error => error.Code == "TeacherSubjectAssignment.TeachingRoleRequired");
    }

    private static CreateAcademicCycleRequest CreateCycleRequest(
        int? year = 2026,
        string? period = "Annual",
        DateOnly? startDate = null,
        DateOnly? endDate = null)
    {
        return new CreateAcademicCycleRequest(
            year,
            period,
            startDate ?? new DateOnly(2026, 3, 1),
            endDate ?? new DateOnly(2026, 12, 15));
    }

    private static CreateSubjectRequest CreateSubjectRequest(
        Guid? careerId = null,
        string? code = "programacion-i",
        string? name = "Programacion I",
        int? year = 1,
        string? period = "FirstSemester")
    {
        return new CreateSubjectRequest(
            careerId ?? Guid.NewGuid(),
            code,
            name,
            year,
            period);
    }
}
