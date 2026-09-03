namespace AcademicSurveySystem.Application.Common.Security;

public enum PasswordVerificationResult
{
    Failed = 0,
    Success = 1,
    SuccessRehashNeeded = 2
}
