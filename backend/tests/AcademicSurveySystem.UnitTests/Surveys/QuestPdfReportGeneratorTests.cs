using AcademicSurveySystem.Application.Common.Results;
using AcademicSurveySystem.Application.Surveys.Reports;
using AcademicSurveySystem.Application.Surveys.Results;
using AcademicSurveySystem.Infrastructure.Reports;

namespace AcademicSurveySystem.UnitTests.Surveys;

public sealed class QuestPdfReportGeneratorTests
{
    [Fact]
    public async Task GenerateSurveyAssignmentReportAsync_ReturnsPdfBytesAndSafeFileName()
    {
        var generator = new QuestPdfReportGenerator();
        var report = CreateReport();

        var result = await generator.GenerateSurveyAssignmentReportAsync(
            report,
            CancellationToken.None);

        Assert.Equal(ApplicationResultStatus.Success, result.Status);
        Assert.Equal("application/pdf", result.Value!.ContentType);
        Assert.EndsWith(".pdf", result.Value.FileName);
        Assert.DoesNotContain("/", result.Value.FileName);
        Assert.DoesNotContain("\\", result.Value.FileName);
        Assert.True(result.Value.Content.Length > 4);
        Assert.Equal("%PDF"u8.ToArray(), result.Value.Content[..4]);
    }

    private static SurveyReportDto CreateReport()
    {
        return new SurveyReportDto(
            new ReportInstitutionDto(
                "Universidad Católica de Cuyo",
                "Sistema Web de Gestión de Encuestas Académicas",
                "Facultad de Ciencias Económicas y Empresariales"),
            new DateTimeOffset(2026, 10, 1, 12, 0, 0, TimeSpan.Zero),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Encuesta / Docente",
            2,
            Guid.NewGuid(),
            "Contador Público",
            Guid.NewGuid(),
            "Programación I / Laboratorio",
            Guid.NewGuid(),
            2026,
            "Annual",
            Guid.NewGuid(),
            "Ada / Lovelace",
            "Titular",
            1,
            1,
            new DateTimeOffset(2026, 10, 1, 13, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 10, 1, 13, 0, 0, TimeSpan.Zero),
            [
                new SurveyQuestionResultsDto(
                    Guid.NewGuid(),
                    "¿Cómo evalúa la claridad?",
                    "SingleChoice",
                    1,
                    1,
                    new SurveyChoiceResultsDto(
                        [
                            new SurveyOptionDistributionDto(Guid.NewGuid(), "Buena", "good", 1, 100),
                            new SurveyOptionDistributionDto(Guid.NewGuid(), "Regular", "regular", 0, 0)
                        ]),
                    null,
                    null,
                    null,
                    []),
                new SurveyQuestionResultsDto(
                    Guid.NewGuid(),
                    "Comentarios",
                    "LongText",
                    2,
                    1,
                    null,
                    new SurveyTextResultsDto(1, ["Respuesta abierta extensa que debe continuar correctamente en el PDF."]),
                    null,
                    null,
                    [new SurveyQuestionCommentDto(Guid.NewGuid(), "Comentario adicional anónimo.")])
            ]);
    }
}
