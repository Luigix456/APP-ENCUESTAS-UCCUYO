using AcademicSurveySystem.Domain.Academic.Entities;
using AcademicSurveySystem.Domain.Common;

namespace AcademicSurveySystem.UnitTests.Academic;

public sealed class TeacherTests
{
    private static readonly DateTimeOffset CreatedAtUtc =
        new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset UpdatedAtUtc =
        new(2026, 1, 2, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Constructor_CreatesTeacherWithEmail_WhenValuesAreValid()
    {
        var teacher = CreateTeacher();

        Assert.Equal("Ada", teacher.FirstName);
        Assert.Equal("Lovelace", teacher.LastName);
        Assert.Equal("ada@institucion.edu.ar", teacher.Email);
        Assert.Equal("ADA@INSTITUCION.EDU.AR", teacher.NormalizedEmail);
        Assert.True(teacher.IsActive);
    }

    [Fact]
    public void Constructor_CreatesTeacherWithoutEmail()
    {
        var teacher = CreateTeacher(email: null);

        Assert.Null(teacher.Email);
        Assert.Null(teacher.NormalizedEmail);
    }

    [Fact]
    public void ChangeEmail_NormalizesEmail()
    {
        var teacher = CreateTeacher(email: null);

        teacher.ChangeEmail(" GRACE@INSTITUCION.EDU.AR ", UpdatedAtUtc);

        Assert.Equal("grace@institucion.edu.ar", teacher.Email);
        Assert.Equal("GRACE@INSTITUCION.EDU.AR", teacher.NormalizedEmail);
    }

    [Fact]
    public void Constructor_RejectsInvalidEmail()
    {
        Assert.Throws<DomainException>(() => CreateTeacher(email: "invalid-email"));
    }

    [Fact]
    public void RemoveEmail_ClearsEmailValues()
    {
        var teacher = CreateTeacher();

        teacher.RemoveEmail(UpdatedAtUtc);

        Assert.Null(teacher.Email);
        Assert.Null(teacher.NormalizedEmail);
        Assert.Equal(UpdatedAtUtc, teacher.UpdatedAtUtc);
    }

    [Fact]
    public void Constructor_RejectsEmptyName()
    {
        Assert.Throws<DomainException>(() => CreateTeacher(firstName: " "));
    }

    [Fact]
    public void ActivateAndDeactivate_UpdateState()
    {
        var teacher = CreateTeacher();

        teacher.Deactivate(UpdatedAtUtc);
        Assert.False(teacher.IsActive);

        teacher.Activate(UpdatedAtUtc.AddDays(1));
        Assert.True(teacher.IsActive);
    }

    private static Teacher CreateTeacher(
        string firstName = "Ada",
        string lastName = "Lovelace",
        string? email = "ada@institucion.edu.ar")
    {
        return new Teacher(
            Guid.NewGuid(),
            firstName,
            lastName,
            email,
            CreatedAtUtc);
    }
}
