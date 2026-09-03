using AcademicSurveySystem.Application.Common.Results;

namespace AcademicSurveySystem.Application.Academic.TeacherSubjectAssignments;

public sealed record UpdateTeacherSubjectAssignmentRequest(string? TeachingRole)
{
    public IReadOnlyCollection<ApplicationError> Validate()
    {
        var errors = new List<ApplicationError>();

        if (string.IsNullOrWhiteSpace(TeachingRole))
        {
            errors.Add(new ApplicationError(
                "TeacherSubjectAssignment.TeachingRoleRequired",
                "TeachingRole is required."));
        }

        return errors;
    }
}
