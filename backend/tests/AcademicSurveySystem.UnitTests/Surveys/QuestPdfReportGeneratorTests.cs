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
        var report = CreateReport(totalResponses: 28, expectedRespondentCount: 35, remainingCount: 7, participationPercentage: 80m);

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

    [Fact]
    public void BuildSummaryMetrics_WithStudentExpectedCount_IncludesExpectedResponsesParticipationAndRemaining()
    {
        var report = CreateReport(
            totalResponses: 28,
            expectedRespondentCount: 35,
            remainingCount: 7,
            participationPercentage: 80m);

        var metrics = ToMetricDictionary(QuestPdfReportGenerator.BuildSummaryMetrics(report));

        Assert.Equal("35", metrics["Alumnos inscriptos"]);
        Assert.Equal("28", metrics["Respuestas recibidas"]);
        Assert.Equal("80 %", metrics["Participación"]);
        Assert.Equal("7", metrics["Pendientes"]);
    }

    [Fact]
    public void BuildSummaryMetrics_FormatsDecimalParticipationUsingSpanishStyle()
    {
        var report = CreateReport(
            totalResponses: 2,
            expectedRespondentCount: 3,
            remainingCount: 1,
            participationPercentage: 66.67m);

        var metrics = ToMetricDictionary(QuestPdfReportGenerator.BuildSummaryMetrics(report));

        Assert.Equal("66,67 %", metrics["Participación"]);
    }

    [Fact]
    public async Task GenerateSurveyAssignmentReportAsync_WithZeroResponsesAndExpectedCount_KeepsValidPdf()
    {
        var generator = new QuestPdfReportGenerator();
        var report = CreateReport(
            totalResponses: 0,
            expectedRespondentCount: 35,
            remainingCount: 35,
            participationPercentage: 0m);

        var metrics = ToMetricDictionary(QuestPdfReportGenerator.BuildSummaryMetrics(report));
        var result = await generator.GenerateSurveyAssignmentReportAsync(report, CancellationToken.None);

        Assert.Equal("35", metrics["Alumnos inscriptos"]);
        Assert.Equal("0", metrics["Respuestas recibidas"]);
        Assert.Equal("0 %", metrics["Participación"]);
        Assert.Equal("35", metrics["Pendientes"]);
        Assert.Equal(ApplicationResultStatus.Success, result.Status);
        Assert.NotEmpty(result.Value!.Content);
        Assert.Equal("%PDF"u8.ToArray(), result.Value.Content[..4]);
    }

    [Fact]
    public void BuildSummaryMetrics_WithFullParticipation_ShowsOneHundredPercentAndZeroRemaining()
    {
        var report = CreateReport(
            totalResponses: 35,
            expectedRespondentCount: 35,
            remainingCount: 0,
            participationPercentage: 100m);

        var metrics = ToMetricDictionary(QuestPdfReportGenerator.BuildSummaryMetrics(report));

        Assert.Equal("100 %", metrics["Participación"]);
        Assert.Equal("0", metrics["Pendientes"]);
    }

    [Fact]
    public void BuildSummaryMetrics_WithNullExpectedCount_OnlyShowsResponses()
    {
        var report = CreateReport(
            totalResponses: 28,
            expectedRespondentCount: null,
            remainingCount: null,
            participationPercentage: null);

        var metrics = QuestPdfReportGenerator.BuildSummaryMetrics(report).ToArray();

        var metric = Assert.Single(metrics);
        Assert.Equal("Respuestas recibidas", metric.Label);
        Assert.Equal("28", metric.Value);
        Assert.DoesNotContain(metrics, item => item.Label == "Alumnos inscriptos");
        Assert.DoesNotContain(metrics, item => item.Label == "Participación");
        Assert.DoesNotContain(metrics, item => item.Label == "Pendientes");
    }

    [Fact]
    public void BuildSummaryMetrics_UsesHistoricalAssignmentSnapshot()
    {
        const int currentSubjectEnrollmentOutsideReport = 40;
        var report = CreateReport(
            totalResponses: 28,
            expectedRespondentCount: 35,
            remainingCount: 7,
            participationPercentage: 80m);

        var metrics = ToMetricDictionary(QuestPdfReportGenerator.BuildSummaryMetrics(report));

        Assert.Equal("35", metrics["Alumnos inscriptos"]);
        Assert.NotEqual(
            currentSubjectEnrollmentOutsideReport.ToString(System.Globalization.CultureInfo.InvariantCulture),
            metrics["Alumnos inscriptos"]);
    }

    private static SurveyReportDto CreateReport(
        int totalResponses,
        int? expectedRespondentCount,
        int? remainingCount,
        decimal? participationPercentage)
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
            totalResponses,
            1,
            totalResponses == 0 ? null : new DateTimeOffset(2026, 10, 1, 13, 0, 0, TimeSpan.Zero),
            totalResponses == 0 ? null : new DateTimeOffset(2026, 10, 1, 13, 0, 0, TimeSpan.Zero),
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
            ],
            expectedRespondentCount,
            remainingCount,
            participationPercentage);
    }

    private static IReadOnlyDictionary<string, string> ToMetricDictionary(
        IReadOnlyList<QuestPdfReportGenerator.ReportSummaryMetric> metrics)
    {
        return metrics.ToDictionary(metric => metric.Label, metric => metric.Value);
    }
}
