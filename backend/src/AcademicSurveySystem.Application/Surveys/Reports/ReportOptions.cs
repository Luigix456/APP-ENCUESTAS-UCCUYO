namespace AcademicSurveySystem.Application.Surveys.Reports;

public sealed class ReportOptions
{
    public const string SectionName = "Reports";

    public string InstitutionName { get; set; } = "Universidad Católica de Cuyo";

    public string SystemName { get; set; } = "Sistema Web de Gestión de Encuestas Académicas";

    public string? FacultyName { get; set; } = "Facultad de Ciencias Económicas y Empresariales";
}
