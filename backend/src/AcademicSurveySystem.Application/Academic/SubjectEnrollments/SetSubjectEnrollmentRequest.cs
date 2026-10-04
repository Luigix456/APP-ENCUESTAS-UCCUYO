using AcademicSurveySystem.Application.Common.Results;

namespace AcademicSurveySystem.Application.Academic.SubjectEnrollments;

public sealed record SetSubjectEnrollmentRequest(int? EnrolledStudentCount)
{
    public IReadOnlyCollection<ApplicationError> Validate()
    {
        var errors = new List<ApplicationError>();

        if (EnrolledStudentCount is null)
        {
            errors.Add(new ApplicationError(
                "SubjectEnrollment.EnrolledStudentCountRequired",
                "EnrolledStudentCount is required."));
        }
        else if (EnrolledStudentCount <= 0)
        {
            errors.Add(new ApplicationError(
                "SubjectEnrollment.EnrolledStudentCountInvalid",
                "EnrolledStudentCount must be greater than zero."));
        }

        return errors;
    }
}
