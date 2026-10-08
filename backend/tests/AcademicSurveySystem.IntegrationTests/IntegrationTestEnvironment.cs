using System.Runtime.CompilerServices;

namespace AcademicSurveySystem.IntegrationTests;

internal static class IntegrationTestEnvironment
{
    [ModuleInitializer]
    internal static void Configure()
    {
        Environment.SetEnvironmentVariable(
            "ConnectionStrings__DefaultConnection",
            Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? "Host=localhost;Port=5432;Database=academic_survey_db;Username=postgres");
    }
}
