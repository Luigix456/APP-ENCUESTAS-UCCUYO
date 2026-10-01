using System.IdentityModel.Tokens.Jwt;
using AcademicSurveySystem.Application.Common.Results;
using AcademicSurveySystem.Application.Surveys.Reports;
using AcademicSurveySystem.Application.Surveys.Results;
using AcademicSurveySystem.Infrastructure.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AcademicSurveySystem.Api.Controllers;

/// <summary>
/// Genera modelos y archivos de informes institucionales de resultados.
/// </summary>
[ApiController]
[Authorize]
[Route("api/reports")]
public sealed class ReportsController : ControllerBase
{
    private const string ReadAllPermission = "results.read_all";
    private const string ReadCareerPermission = "results.read_career";
    private const string ExportReportsPermission = "reports.export";

    private readonly IReportService _reportService;
    private readonly IPdfReportGenerator _pdfReportGenerator;
    private readonly IResultsAccessService _resultsAccessService;

    public ReportsController(
        IReportService reportService,
        IPdfReportGenerator pdfReportGenerator,
        IResultsAccessService resultsAccessService)
    {
        _reportService = reportService;
        _pdfReportGenerator = pdfReportGenerator;
        _resultsAccessService = resultsAccessService;
    }

    /// <summary>
    /// Obtiene el modelo de vista previa del informe de una asignación de encuesta.
    /// Requiere results.read_all o results.read_career dentro del alcance de carreras del usuario.
    /// </summary>
    [HttpGet("survey-assignments/{surveyAssignmentId:guid}")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(SurveyReportDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetSurveyAssignmentReport(
        Guid surveyAssignmentId,
        CancellationToken cancellationToken)
    {
        var authorizationResult = await AuthorizeSurveyAssignmentResultsAsync(
            surveyAssignmentId,
            cancellationToken);

        if (authorizationResult is not null)
        {
            return authorizationResult;
        }

        var result = await _reportService.BuildSurveyAssignmentReportAsync(
            surveyAssignmentId,
            cancellationToken);

        SetNoStoreCacheHeaders();
        return ToActionResult(result);
    }

    /// <summary>
    /// Exporta en PDF el informe de una asignación de encuesta.
    /// Requiere reports.export y además results.read_all o results.read_career dentro del alcance de carreras del usuario.
    /// </summary>
    [HttpGet("survey-assignments/{surveyAssignmentId:guid}/pdf")]
    [Produces("application/pdf")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK, "application/pdf")]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetSurveyAssignmentReportPdf(
        Guid surveyAssignmentId,
        CancellationToken cancellationToken)
    {
        if (!HasPermission(ExportReportsPermission))
        {
            return Forbid();
        }

        var authorizationResult = await AuthorizeSurveyAssignmentResultsAsync(
            surveyAssignmentId,
            cancellationToken);

        if (authorizationResult is not null)
        {
            return authorizationResult;
        }

        var reportResult = await _reportService.BuildSurveyAssignmentReportAsync(
            surveyAssignmentId,
            cancellationToken);

        if (!reportResult.Succeeded || reportResult.Value is null)
        {
            SetNoStoreCacheHeaders();
            return ToActionResult(reportResult);
        }

        var fileResult = await _pdfReportGenerator.GenerateSurveyAssignmentReportAsync(
            reportResult.Value,
            cancellationToken);

        SetNoStoreCacheHeaders();

        if (!fileResult.Succeeded || fileResult.Value is null)
        {
            return ToActionResult(fileResult);
        }

        return File(
            fileResult.Value.Content,
            fileResult.Value.ContentType,
            fileResult.Value.FileName);
    }

    private async Task<IActionResult?> AuthorizeSurveyAssignmentResultsAsync(
        Guid surveyAssignmentId,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Forbid();
        }

        var decision = await _resultsAccessService.AuthorizeSurveyAssignmentAsync(
            userId,
            HasPermission(ReadAllPermission),
            HasPermission(ReadCareerPermission),
            surveyAssignmentId,
            cancellationToken);

        return ToAuthorizationResult(decision);
    }

    private IActionResult? ToAuthorizationResult(ResultsAccessDecision decision)
    {
        return decision.Status switch
        {
            ResultsAccessDecisionStatus.Allowed => null,
            ResultsAccessDecisionStatus.SurveyAssignmentNotFound => NotFound(CreateErrorResponse([
                new ApplicationError("Results.SurveyAssignmentNotFound", "Survey assignment was not found.")
            ])),
            _ => Forbid()
        };
    }

    private bool TryGetUserId(out Guid userId)
    {
        var userIdValue = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
            ?? User.FindFirst(JwtTokenGenerator.NameIdentifierClaimType)?.Value;

        return Guid.TryParse(userIdValue, out userId);
    }

    private bool HasPermission(string permission)
    {
        return User.HasClaim(JwtTokenGenerator.PermissionClaimType, permission);
    }

    private IActionResult ToActionResult<T>(ApplicationResult<T> result)
    {
        return result.Status switch
        {
            ApplicationResultStatus.Success => Ok(result.Value),
            ApplicationResultStatus.Validation => BadRequest(CreateErrorResponse(result.Errors)),
            ApplicationResultStatus.NotFound => NotFound(CreateErrorResponse(result.Errors)),
            ApplicationResultStatus.Conflict => Conflict(CreateErrorResponse(result.Errors)),
            _ => StatusCode(StatusCodes.Status500InternalServerError, CreateErrorResponse(result.Errors))
        };
    }

    private void SetNoStoreCacheHeaders()
    {
        Response.Headers.CacheControl = "private, no-store";
        Response.Headers.Pragma = "no-cache";
    }

    private static object CreateErrorResponse(IReadOnlyCollection<ApplicationError> errors)
    {
        return new
        {
            errors
        };
    }
}
