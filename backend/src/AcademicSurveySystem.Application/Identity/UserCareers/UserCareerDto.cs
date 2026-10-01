namespace AcademicSurveySystem.Application.Identity.UserCareers;

public sealed record UserCareerDto(
    Guid CareerId,
    string CareerCode,
    string CareerName,
    bool IsActive,
    DateTimeOffset AssignedAtUtc);
