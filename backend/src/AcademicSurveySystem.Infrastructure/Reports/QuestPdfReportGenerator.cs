using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using AcademicSurveySystem.Application.Common.Results;
using AcademicSurveySystem.Application.Surveys.Reports;
using AcademicSurveySystem.Application.Surveys.Results;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace AcademicSurveySystem.Infrastructure.Reports;

public sealed class QuestPdfReportGenerator : IPdfReportGenerator
{
    private const string PdfContentType = "application/pdf";
    private static readonly string[] ChartColors =
    [
        "#214f68",
        "#2f806d",
        "#8a6d3b",
        "#6e4f8f",
        "#9d2636",
        "#526071",
        "#3f6f8f",
        "#4f8060"
    ];

    public Task<ApplicationResult<GeneratedReportFileDto>> GenerateSurveyAssignmentReportAsync(
        SurveyReportDto report,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            QuestPDF.Settings.License = LicenseType.Community;
            var document = new SurveyReportDocument(report);
            var content = document.GeneratePdf();
            var fileName = BuildFileName(report);

            return Task.FromResult(
                ApplicationResult<GeneratedReportFileDto>.Success(
                    new GeneratedReportFileDto(fileName, PdfContentType, content)));
        }
        catch (Exception)
        {
            return Task.FromResult(
                ApplicationResult<GeneratedReportFileDto>.Failure("The PDF report could not be generated."));
        }
    }

    private static string BuildFileName(SurveyReportDto report)
    {
        var parts = new[]
        {
            "Informe",
            report.SubjectName,
            report.TeacherFullName,
            report.AcademicCycleYear.ToString(CultureInfo.InvariantCulture),
            $"v{report.SurveyVersionNumber}"
        };

        var fileName = string.Join("_", parts.Select(SanitizeFileNamePart));
        return string.IsNullOrWhiteSpace(fileName)
            ? "Informe.pdf"
            : $"{fileName}.pdf";
    }

    private static string SanitizeFileNamePart(string value)
    {
        var sanitized = Regex.Replace(value.Trim(), "[\\\\/\\r\\n\\t\"':;|?*<>]+", " ");
        sanitized = Regex.Replace(sanitized, @"\s+", "_");
        sanitized = Regex.Replace(sanitized, @"[^\p{L}\p{Nd}_-]+", string.Empty);
        sanitized = sanitized.Trim('_', '-', '.');

        return sanitized.Length > 48 ? sanitized[..48] : sanitized;
    }

    private sealed class SurveyReportDocument : IDocument
    {
        private readonly SurveyReportDto _report;

        public SurveyReportDocument(SurveyReportDto report)
        {
            _report = report;
        }

        public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

        public void Compose(IDocumentContainer container)
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(32);
                page.DefaultTextStyle(text => text.FontSize(9).FontColor("#18212f"));

                page.Header().Element(ComposeHeader);
                page.Content().PaddingVertical(14).Column(column =>
                {
                    column.Spacing(14);
                    column.Item().Element(ComposeTitle);
                    column.Item().Element(ComposeContext);
                    column.Item().Element(ComposeSummary);

                    if (_report.TotalResponses == 0)
                    {
                        column.Item().Element(container =>
                            InfoBox(container, "Esta evaluación todavía no tiene respuestas."));
                    }

                    foreach (var question in _report.Questions.OrderBy(question => question.Order))
                    {
                        column.Item().EnsureSpace(110).Element(container => ComposeQuestion(container, question));
                    }
                });
                page.Footer().Element(ComposeFooter);
            });
        }

        private void ComposeHeader(IContainer container)
        {
            container.BorderBottom(1).BorderColor("#d7e0dc").PaddingBottom(8).Row(row =>
            {
                row.RelativeItem().Column(column =>
                {
                    column.Item().Text(_report.Institution.InstitutionName)
                        .FontSize(13)
                        .Bold()
                        .FontColor("#214f68");
                    if (!string.IsNullOrWhiteSpace(_report.Institution.FacultyName))
                    {
                        column.Item().Text(_report.Institution.FacultyName)
                            .FontSize(9)
                            .FontColor("#526071");
                    }

                    column.Item().Text(_report.Institution.SystemName)
                        .FontSize(9)
                        .FontColor("#526071");
                });

                row.ConstantItem(110).AlignRight().Text("Informe de resultados")
                    .FontSize(10)
                    .Bold()
                    .FontColor("#2f806d");
            });
        }

        private void ComposeTitle(IContainer container)
        {
            container.Column(column =>
            {
                column.Item().Text("Informe de resultados")
                    .FontSize(18)
                    .Bold()
                    .FontColor("#122033");
                column.Item().Text(_report.SurveyTitle)
                    .FontSize(12)
                    .Bold()
                    .FontColor("#214f68");
                column.Item().Text($"Versión {_report.SurveyVersionNumber}")
                    .FontSize(10)
                    .FontColor("#526071");
            });
        }

        private void ComposeContext(IContainer container)
        {
            container.Border(1).BorderColor("#d7e0dc").CornerRadius(4).Padding(10).Element(content =>
                ComposeMetadataGrid(
                    content,
                    [
                        ("Carrera", _report.CareerName),
                        ("Materia", _report.SubjectName),
                        ("Docente", _report.TeacherFullName),
                        ("Rol docente", _report.TeachingRole),
                        ("Ciclo lectivo", $"{_report.AcademicCycleYear} · {_report.AcademicCyclePeriod}"),
                        ("Generado", FormatDateTimeUtc(_report.GeneratedAtUtc))
                    ],
                    columns: 2));
        }

        private void ComposeSummary(IContainer container)
        {
            container.Border(1).BorderColor("#d7e0dc").CornerRadius(4).Padding(10).Column(column =>
            {
                column.Item().Text("Resumen").FontSize(12).Bold().FontColor("#122033");
                column.Item().PaddingTop(6).Element(content =>
                    ComposeMetadataGrid(
                        content,
                        [
                            ("Total de respuestas", _report.TotalResponses.ToString(CultureInfo.InvariantCulture)),
                            ("Total de sesiones", _report.TotalSessions.ToString(CultureInfo.InvariantCulture)),
                            ("Primera respuesta", FormatDateTimeUtc(_report.FirstSubmittedAtUtc)),
                            ("Última respuesta", FormatDateTimeUtc(_report.LastSubmittedAtUtc))
                        ],
                        columns: 2));
            });
        }

        private static void ComposeMetadataGrid(
            IContainer container,
            IReadOnlyList<(string Label, string Value)> items,
            int columns)
        {
            container.Column(column =>
            {
                column.Spacing(7);

                foreach (var rowItems in items.Chunk(columns))
                {
                    column.Item().Row(row =>
                    {
                        foreach (var item in rowItems)
                        {
                            row.RelativeItem().Column(metadata =>
                            {
                                metadata.Item().Text(item.Label).FontSize(7).Bold().FontColor("#526071");
                                metadata.Item().Text(item.Value).FontSize(9).Bold().FontColor("#18212f");
                            });
                        }

                        for (var index = rowItems.Length; index < columns; index++)
                        {
                            row.RelativeItem();
                        }
                    });
                }
            });
        }

        private void ComposeQuestion(IContainer container, SurveyQuestionResultsDto question)
        {
            container.Border(1).BorderColor("#d7e0dc").CornerRadius(4).Padding(10).Column(column =>
            {
                column.Spacing(8);
                column.Item().ShowEntire().Column(header =>
                {
                    header.Item().Text($"{question.Order}. {question.Text}")
                        .FontSize(11)
                        .Bold()
                        .FontColor("#122033");
                    header.Item().Text($"{FormatQuestionType(question.Type)} · {question.ResponseCount} respuestas")
                        .FontSize(8)
                        .FontColor("#526071");
                });

                switch (question.Type)
                {
                    case "SingleChoice":
                        column.Item().Element(item => ComposeSingleChoice(item, question));
                        break;
                    case "MultipleChoice":
                        column.Item().Element(item => ComposeMultipleChoice(item, question));
                        break;
                    case "ShortText":
                        column.Item().Element(item => ComposeTextValues(item, "Respuestas abiertas", question.TextValues?.Values ?? []));
                        break;
                    case "LongText":
                        column.Item().Element(item => ComposeTextValues(item, "Respuestas", question.TextValues?.Values ?? []));
                        break;
                    case "RatingScale":
                        column.Item().Element(item => ComposeRating(item, question));
                        break;
                    case "MatrixSingleChoice":
                        column.Item().Element(item => ComposeMatrix(item, question));
                        break;
                    default:
                        column.Item().Element(item => InfoBox(item, "Tipo de pregunta no reconocido."));
                        break;
                }

                if (question.Type == "LongText" && question.Comments.Count > 0)
                {
                    column.Item().Element(item =>
                        ComposeTextValues(item, "Comentarios adicionales", question.Comments.Select(comment => comment.Comment)));
                }
                else if (question.Type != "LongText" && question.Comments.Count > 0)
                {
                    column.Item().Element(item =>
                        ComposeTextValues(item, "Comentarios", question.Comments.Select(comment => comment.Comment)));
                }
            });
        }

        private static void ComposeSingleChoice(IContainer container, SurveyQuestionResultsDto question)
        {
            var options = GetChoiceEntries(question.Choice);

            container.Column(column =>
            {
                column.Item().Row(row =>
                {
                    row.ConstantItem(150).Height(130).Svg(BuildDonutSvg(options));
                    row.RelativeItem().PaddingLeft(10).Element(item => ComposeOptionTable(item, options));
                });

                if (question.Choice?.Other is not null && question.Choice.Other.Values.Count > 0)
                {
                    column.Item().PaddingTop(8).Element(item =>
                        ComposeTextValues(item, "Textos de Otro", question.Choice.Other.Values));
                }
            });
        }

        private static void ComposeMultipleChoice(IContainer container, SurveyQuestionResultsDto question)
        {
            var options = GetChoiceEntries(question.Choice);

            container.Column(column =>
            {
                column.Item().Element(item => ComposeBarChart(item, options));

                if (question.Choice?.Other is not null && question.Choice.Other.Values.Count > 0)
                {
                    column.Item().PaddingTop(8).Element(item =>
                        ComposeTextValues(item, "Textos de Otro", question.Choice.Other.Values));
                }
            });
        }

        private static void ComposeRating(IContainer container, SurveyQuestionResultsDto question)
        {
            if (question.Rating is null)
            {
                InfoBox(container, "Sin respuestas para esta escala.");
                return;
            }

            var rating = question.Rating;
            var entries = rating.Distribution
                .Select(item => new ChartEntry(
                    item.Value.ToString(CultureInfo.InvariantCulture),
                    item.Count,
                    item.Percentage))
                .ToArray();

            container.Column(column =>
            {
                column.Item().Element(content =>
                    ComposeMetadataGrid(
                        content,
                        [
                            ("Promedio", rating.Average?.ToString("0.##", CultureInfo.InvariantCulture) ?? "-"),
                            ("Mínimo configurado", rating.ConfiguredMinimum?.ToString(CultureInfo.InvariantCulture) ?? "-"),
                            ("Máximo configurado", rating.ConfiguredMaximum?.ToString(CultureInfo.InvariantCulture) ?? "-"),
                            ("Mínimo observado", rating.MinimumObserved?.ToString(CultureInfo.InvariantCulture) ?? "-"),
                            ("Máximo observado", rating.MaximumObserved?.ToString(CultureInfo.InvariantCulture) ?? "-"),
                            ("Respuestas", rating.ResponseCount.ToString(CultureInfo.InvariantCulture))
                        ],
                        columns: 3));
                column.Item().PaddingTop(6).Element(item => ComposeBarChart(item, entries));
            });
        }

        private static void ComposeMatrix(IContainer container, SurveyQuestionResultsDto question)
        {
            if (question.Matrix is null || question.Matrix.Rows.Count == 0)
            {
                InfoBox(container, "Sin filas de matriz para mostrar.");
                return;
            }

            container.Column(column =>
            {
                column.Spacing(8);

                foreach (var rowResult in question.Matrix.Rows)
                {
                    column.Item().PreventPageBreak().Border(1).BorderColor("#e5ebe8").CornerRadius(4).Padding(8).Column(row =>
                    {
                        row.Item().Text(rowResult.RowText).Bold().FontColor("#122033");
                        row.Item().Text($"{rowResult.TotalResponses} respuestas").FontSize(8).FontColor("#526071");
                        row.Item().PaddingTop(4).Element(item => ComposeBarChart(
                            item,
                            rowResult.Options.Select(option => new ChartEntry(option.Text, option.Count, option.Percentage)).ToArray()));
                    });
                }
            });
        }

        private static void ComposeOptionTable(IContainer container, IReadOnlyCollection<ChartEntry> entries)
        {
            container.Column(column =>
            {
                column.Spacing(4);
                foreach (var entry in entries)
                {
                    column.Item().Row(row =>
                    {
                        row.ConstantItem(10).Height(10).Background(entry.Color);
                        row.RelativeItem().PaddingLeft(6).Text(entry.Label).FontSize(8);
                        row.ConstantItem(90).AlignRight().Text($"{entry.Count} · {FormatPercentage(entry.Percentage)}")
                            .FontSize(8)
                            .Bold();
                    });
                }
            });
        }

        private static void ComposeBarChart(IContainer container, IReadOnlyCollection<ChartEntry> entries)
        {
            if (entries.Count == 0)
            {
                InfoBox(container, "Sin datos para graficar.");
                return;
            }

            var max = Math.Max(1, entries.Max(entry => entry.Count));

            container.Column(column =>
            {
                column.Spacing(5);
                foreach (var entry in entries)
                {
                    column.Item().Column(rowColumn =>
                    {
                        rowColumn.Item().Row(row =>
                        {
                            row.RelativeItem().Text(entry.Label).FontSize(8).FontColor("#18212f");
                            row.ConstantItem(95).AlignRight().Text($"{entry.Count} · {FormatPercentage(entry.Percentage)}")
                                .FontSize(8)
                                .Bold();
                        });
                        rowColumn.Item().Height(8).Background("#edf1ef").Row(row =>
                        {
                            row.RelativeItem(Math.Max(entry.Count, 0.001f)).Background(entry.Color);
                            row.RelativeItem(Math.Max(max - entry.Count, 0.001f));
                        });
                    });
                }
            });
        }

        private static void ComposeTextValues(IContainer container, string title, IEnumerable<string> values)
        {
            var visibleValues = values
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .ToArray();

            container.Column(column =>
            {
                column.Spacing(5);
                column.Item().Text(title).FontSize(10).Bold().FontColor("#214f68");

                if (visibleValues.Length == 0)
                {
                    column.Item().Element(item => InfoBox(item, "Sin textos para mostrar."));
                    return;
                }

                foreach (var value in visibleValues)
                {
                    column.Item().BorderLeft(3).BorderColor("#2f806d").PaddingLeft(8).Text(value).FontSize(8);
                }
            });
        }

        private static void InfoBox(IContainer container, string message)
        {
            container.Border(1).BorderColor("#d7e0dc").CornerRadius(4).Padding(8).Background("#fbfcfc")
                .Text(message)
                .FontSize(8)
                .FontColor("#526071");
        }

        private void ComposeFooter(IContainer container)
        {
            container.BorderTop(1).BorderColor("#d7e0dc").PaddingTop(6).Row(row =>
            {
                row.RelativeItem().Text(
                    $"Generado por {_report.Institution.SystemName} · {FormatDateTimeUtc(_report.GeneratedAtUtc)}")
                    .FontSize(7)
                    .FontColor("#526071");
                row.ConstantItem(90).AlignRight().Text(text =>
                {
                    text.DefaultTextStyle(style => style.FontSize(7).FontColor("#526071"));
                    text.Span("Página ");
                    text.CurrentPageNumber();
                    text.Span(" de ");
                    text.TotalPages();
                });
            });
        }

        private static IReadOnlyCollection<ChartEntry> GetChoiceEntries(SurveyChoiceResultsDto? choice)
        {
            if (choice is null)
            {
                return [];
            }

            var entries = choice.Options
                .Select((option, index) => new ChartEntry(
                    option.Text,
                    option.Count,
                    option.Percentage,
                    ChartColors[index % ChartColors.Length]))
                .ToList();

            if (choice.Other is not null)
            {
                entries.Add(new ChartEntry(
                    "Otro",
                    choice.Other.Count,
                    choice.Other.Percentage,
                    ChartColors[entries.Count % ChartColors.Length]));
            }

            return entries;
        }

        private static string BuildDonutSvg(IReadOnlyCollection<ChartEntry> entries)
        {
            const decimal radius = 44m;
            const decimal center = 64m;
            const decimal strokeWidth = 18m;
            var total = entries.Sum(entry => Math.Max(entry.Count, 0));

            if (total == 0)
            {
                return """
                    <svg width="128" height="128" viewBox="0 0 128 128" xmlns="http://www.w3.org/2000/svg">
                      <circle cx="64" cy="64" r="44" fill="none" stroke="#edf1ef" stroke-width="18"/>
                      <text x="64" y="68" text-anchor="middle" font-size="12" fill="#526071">0</text>
                    </svg>
                    """;
            }

            var circumference = 2m * (decimal)Math.PI * radius;
            var offset = 0m;
            var builder = new StringBuilder();
            builder.AppendLine("""<svg width="128" height="128" viewBox="0 0 128 128" xmlns="http://www.w3.org/2000/svg">""");
            builder.AppendLine("""<circle cx="64" cy="64" r="44" fill="none" stroke="#edf1ef" stroke-width="18"/>""");

            foreach (var entry in entries)
            {
                var length = circumference * entry.Count / total;
                var gap = Math.Max(0m, circumference - length);
                builder.Append(CultureInfo.InvariantCulture, $"""
                    <circle cx="{center}" cy="{center}" r="{radius}" fill="none" stroke="{entry.Color}" stroke-width="{strokeWidth}" stroke-dasharray="{length:0.###} {gap:0.###}" stroke-dashoffset="-{offset:0.###}" transform="rotate(-90 {center} {center})"/>
                    """);
                offset += length;
            }

            builder.Append(CultureInfo.InvariantCulture, $"""
                <circle cx="{center}" cy="{center}" r="28" fill="#ffffff"/>
                <text x="{center}" y="68" text-anchor="middle" font-size="14" font-weight="700" fill="#214f68">{total}</text>
                </svg>
                """);

            return builder.ToString();
        }

        private static string FormatQuestionType(string type)
        {
            return type switch
            {
                "SingleChoice" => "Opción única",
                "MultipleChoice" => "Opción múltiple",
                "ShortText" => "Texto corto",
                "LongText" => "Texto largo",
                "RatingScale" => "Escala de valoración",
                "MatrixSingleChoice" => "Matriz de opción única",
                _ => type
            };
        }

        private static string FormatDateTimeUtc(DateTimeOffset? value)
        {
            return value is null
                ? "-"
                : value.Value.UtcDateTime.ToString("yyyy-MM-dd HH:mm 'UTC'", CultureInfo.InvariantCulture);
        }

        private static string FormatPercentage(decimal percentage)
        {
            return $"{percentage.ToString("0.##", CultureInfo.InvariantCulture)}%";
        }
    }

    private sealed record ChartEntry(
        string Label,
        int Count,
        decimal Percentage,
        string Color = "#214f68");
}
