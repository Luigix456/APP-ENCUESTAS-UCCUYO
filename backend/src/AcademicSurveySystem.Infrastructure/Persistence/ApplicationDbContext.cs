using AcademicSurveySystem.Domain.Academic.Entities;
using AcademicSurveySystem.Domain.Identity.Entities;
using AcademicSurveySystem.Domain.Surveys.Entities;
using Microsoft.EntityFrameworkCore;

namespace AcademicSurveySystem.Infrastructure.Persistence;

public sealed class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<UserCareer> UserCareers => Set<UserCareer>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<AcademicUnit> AcademicUnits => Set<AcademicUnit>();
    public DbSet<Career> Careers => Set<Career>();
    public DbSet<Subject> Subjects => Set<Subject>();
    public DbSet<SubjectEnrollment> SubjectEnrollments => Set<SubjectEnrollment>();
    public DbSet<Teacher> Teachers => Set<Teacher>();
    public DbSet<AcademicCycle> AcademicCycles => Set<AcademicCycle>();
    public DbSet<TeacherSubjectAssignment> TeacherSubjectAssignments =>
        Set<TeacherSubjectAssignment>();
    public DbSet<Survey> Surveys => Set<Survey>();
    public DbSet<SurveySection> SurveySections => Set<SurveySection>();
    public DbSet<SurveyQuestion> SurveyQuestions => Set<SurveyQuestion>();
    public DbSet<SurveyQuestionOption> SurveyQuestionOptions => Set<SurveyQuestionOption>();
    public DbSet<SurveyMatrixRow> SurveyMatrixRows => Set<SurveyMatrixRow>();
    public DbSet<SurveyAssignment> SurveyAssignments => Set<SurveyAssignment>();
    public DbSet<SurveySession> SurveySessions => Set<SurveySession>();
    public DbSet<SurveyResponse> SurveyResponses => Set<SurveyResponse>();
    public DbSet<SurveyAnswer> SurveyAnswers => Set<SurveyAnswer>();
    public DbSet<SurveyAnswerOption> SurveyAnswerOptions => Set<SurveyAnswerOption>();
    public DbSet<SurveyMatrixAnswer> SurveyMatrixAnswers => Set<SurveyMatrixAnswer>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
