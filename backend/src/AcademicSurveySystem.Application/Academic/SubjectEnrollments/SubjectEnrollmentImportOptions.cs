namespace AcademicSurveySystem.Application.Academic.SubjectEnrollments;

public sealed class SubjectEnrollmentImportOptions
{
    public const long DefaultMaxFileSizeBytes = 5 * 1024 * 1024;
    public const int DefaultMaxRows = 2000;

    public long MaxFileSizeBytes { get; set; } = DefaultMaxFileSizeBytes;
    public int MaxRows { get; set; } = DefaultMaxRows;

    public bool IsValid() => MaxFileSizeBytes > 0 && MaxRows > 0;
}
