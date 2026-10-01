namespace AcademicSurveySystem.Application.Surveys.Sessions;

/// <summary>
/// Represents the public view of an open survey session.
/// </summary>
public sealed record PublicSurveySessionDto(
    Guid SessionId,
    string AccessCode,
    DateTimeOffset ExpiresAtUtc,
    Guid SurveyId,
    string SurveyTitle,
    string? SurveyDescription,
    string SurveyTarget,
    string CareerName,
    string SubjectName,
    int AcademicCycleYear,
    string AcademicCyclePeriod,
    string TeacherFullName,
    string TeachingRole,
    IReadOnlyCollection<PublicSurveySectionDto> Sections);

public sealed record PublicSurveySectionDto(
    Guid Id,
    string Title,
    string? Description,
    int Order,
    IReadOnlyCollection<PublicSurveyQuestionDto> Questions);

public sealed record PublicSurveyQuestionDto(
    Guid Id,
    string Text,
    string Type,
    bool IsRequired,
    bool AllowsComment,
    bool AllowsOtherOption,
    int Order,
    IReadOnlyCollection<PublicSurveyQuestionOptionDto> Options,
    IReadOnlyCollection<PublicSurveyMatrixRowDto> MatrixRows,
    int? RatingMin = null,
    int? RatingMax = null);

public sealed record PublicSurveyQuestionOptionDto(
    Guid Id,
    string Text,
    string Value,
    int Order);

public sealed record PublicSurveyMatrixRowDto(
    Guid Id,
    string Text,
    int Order);
