using AcademicSurveySystem.Application.Common.Results;

namespace AcademicSurveySystem.Application.Surveys.Assignments;

public sealed record CreateSurveyAssignmentBatchRequest(
    Guid SurveyId,
    Guid CareerId,
    Guid SubjectId,
    Guid AcademicCycleId,
    IReadOnlyCollection<Guid> TeacherSubjectAssignmentIds)
{
    public IReadOnlyCollection<ApplicationError> Validate()
    {
        var errors = new List<ApplicationError>();

        AddRequiredGuidError(SurveyId, "SurveyAssignment.SurveyIdRequired", "SurveyId is required.", errors);
        AddRequiredGuidError(CareerId, "SurveyAssignment.CareerIdRequired", "CareerId is required.", errors);
        AddRequiredGuidError(SubjectId, "SurveyAssignment.SubjectIdRequired", "SubjectId is required.", errors);
        AddRequiredGuidError(
            AcademicCycleId,
            "SurveyAssignment.AcademicCycleIdRequired",
            "AcademicCycleId is required.",
            errors);

        if (TeacherSubjectAssignmentIds.Count == 0)
        {
            errors.Add(new ApplicationError(
                "SurveyAssignment.TeacherSubjectAssignmentIdsRequired",
                "At least one teacher-subject assignment is required."));
        }

        if (TeacherSubjectAssignmentIds.Any(id => id == Guid.Empty))
        {
            errors.Add(new ApplicationError(
                "SurveyAssignment.TeacherSubjectAssignmentIdRequired",
                "TeacherSubjectAssignmentId is required."));
        }

        if (TeacherSubjectAssignmentIds.Distinct().Count() != TeacherSubjectAssignmentIds.Count)
        {
            errors.Add(new ApplicationError(
                "SurveyAssignment.TeacherSubjectAssignmentDuplicated",
                "Teacher-subject assignments must be unique."));
        }

        return errors;
    }

    public CreateSurveyAssignmentRequest ToSingleRequest(Guid teacherSubjectAssignmentId) =>
        new(SurveyId, CareerId, SubjectId, AcademicCycleId, teacherSubjectAssignmentId);

    private static void AddRequiredGuidError(
        Guid value,
        string code,
        string message,
        ICollection<ApplicationError> errors)
    {
        if (value == Guid.Empty)
        {
            errors.Add(new ApplicationError(code, message));
        }
    }
}
