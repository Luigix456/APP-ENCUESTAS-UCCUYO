using System.IdentityModel.Tokens.Jwt;
using AcademicSurveySystem.Application.Common.Results;
using AcademicSurveySystem.Application.Surveys.Results;
using AcademicSurveySystem.Infrastructure.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AcademicSurveySystem.Api.Controllers;

/// <summary>
/// Consulta resultados agregados de encuestas académicas.
/// </summary>
[ApiController]
[Authorize]
[Route("api/results")]
public sealed class ResultsController : ControllerBase
{
    private const string ReadAllPermission = "results.read_all";
    private const string ReadCareerPermission = "results.read_career";

    private readonly ISurveyResultsService _surveyResultsService;
    private readonly IResultsAccessService _resultsAccessService;

    public ResultsController(
        ISurveyResultsService surveyResultsService,
        IResultsAccessService resultsAccessService)
    {
        _surveyResultsService = surveyResultsService;
        _resultsAccessService = resultsAccessService;
    }

    /// <summary>
    /// Lista asignaciones de encuesta disponibles para consulta de resultados.
    /// </summary>
    [HttpGet("survey-assignments")]
    [ProducesResponseType(typeof(IReadOnlyCollection<SurveyAssignmentResultListItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetSurveyAssignments(
        [FromQuery] Guid? surveyId = null,
        [FromQuery] Guid? careerId = null,
        [FromQuery] Guid? subjectId = null,
        [FromQuery] Guid? academicCycleId = null,
        [FromQuery] Guid? teacherId = null,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetUserId(out var userId))
        {
            return Forbid();
        }

        var accessScope = await _resultsAccessService.GetDiscoveryScopeAsync(
            userId,
            HasPermission(ReadAllPermission),
            HasPermission(ReadCareerPermission),
            cancellationToken);

        if (!accessScope.IsAllowed)
        {
            return Forbid();
        }

        var result = await _surveyResultsService.GetSurveyAssignmentResultsAsync(
            accessScope,
            new SurveyAssignmentResultsFilter(
                surveyId,
                careerId,
                subjectId,
                academicCycleId,
                teacherId),
            cancellationToken);

        return ToActionResult(result);
    }

    /// <summary>
    /// Obtiene la evolución histórica general de participación para una familia de encuesta.
    /// </summary>
    [HttpGet("history")]
    [ProducesResponseType(typeof(SurveyHistoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetHistory(
        [FromQuery] Guid careerId,
        [FromQuery] Guid subjectId,
        [FromQuery] Guid teacherId,
        [FromQuery] Guid surveyVersionGroupId,
        CancellationToken cancellationToken = default)
    {
        var accessScope = await GetResultsAccessScopeAsync(cancellationToken);

        if (accessScope is null)
        {
            return Forbid();
        }

        var result = await _surveyResultsService.GetSurveyHistoryAsync(
            accessScope,
            new SurveyHistoryQuery(careerId, subjectId, teacherId, surveyVersionGroupId),
            cancellationToken);

        return ToActionResult(result);
    }

    /// <summary>
    /// Obtiene la comparación histórica de una pregunta por lineage.
    /// </summary>
    [HttpGet("history/questions/{questionLineageId:guid}")]
    [ProducesResponseType(typeof(QuestionHistoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetQuestionHistory(
        Guid questionLineageId,
        [FromQuery] Guid careerId,
        [FromQuery] Guid subjectId,
        [FromQuery] Guid teacherId,
        [FromQuery] Guid surveyVersionGroupId,
        CancellationToken cancellationToken = default)
    {
        var accessScope = await GetResultsAccessScopeAsync(cancellationToken);

        if (accessScope is null)
        {
            return Forbid();
        }

        var result = await _surveyResultsService.GetQuestionHistoryAsync(
            accessScope,
            questionLineageId,
            new SurveyHistoryQuery(careerId, subjectId, teacherId, surveyVersionGroupId),
            cancellationToken);

        return ToActionResult(result);
    }

    /// <summary>
    /// Obtiene el resumen general de resultados de una asignación de encuesta.
    /// </summary>
    [HttpGet("survey-assignments/{surveyAssignmentId:guid}/summary")]
    [ProducesResponseType(typeof(SurveyResultsSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetSurveyAssignmentSummary(
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

        var result = await _surveyResultsService.GetSurveyAssignmentSummaryAsync(
            surveyAssignmentId,
            cancellationToken);

        return ToActionResult(result);
    }

    /// <summary>
    /// Obtiene resultados agregados de todas las preguntas de una asignación de encuesta.
    /// </summary>
    [HttpGet("survey-assignments/{surveyAssignmentId:guid}/questions")]
    [ProducesResponseType(typeof(IReadOnlyCollection<SurveyQuestionResultsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetSurveyAssignmentQuestions(
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

        var result = await _surveyResultsService.GetSurveyAssignmentQuestionResultsAsync(
            surveyAssignmentId,
            cancellationToken);

        return ToActionResult(result);
    }

    /// <summary>
    /// Obtiene el resultado agregado de una pregunta concreta.
    /// </summary>
    [HttpGet("survey-assignments/{surveyAssignmentId:guid}/questions/{questionId:guid}")]
    [ProducesResponseType(typeof(SurveyQuestionResultsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetSurveyAssignmentQuestion(
        Guid surveyAssignmentId,
        Guid questionId,
        CancellationToken cancellationToken)
    {
        var authorizationResult = await AuthorizeSurveyAssignmentResultsAsync(
            surveyAssignmentId,
            cancellationToken);

        if (authorizationResult is not null)
        {
            return authorizationResult;
        }

        var result = await _surveyResultsService.GetSurveyAssignmentQuestionResultAsync(
            surveyAssignmentId,
            questionId,
            cancellationToken);

        return ToActionResult(result);
    }

    /// <summary>
    /// Obtiene el resumen de resultados de una sesión concreta.
    /// </summary>
    [HttpGet("survey-sessions/{surveySessionId:guid}/summary")]
    [ProducesResponseType(typeof(SurveyResultsSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetSurveySessionSummary(
        Guid surveySessionId,
        CancellationToken cancellationToken)
    {
        var authorizationResult = await AuthorizeSurveySessionResultsAsync(
            surveySessionId,
            cancellationToken);

        if (authorizationResult is not null)
        {
            return authorizationResult;
        }

        var result = await _surveyResultsService.GetSurveySessionSummaryAsync(
            surveySessionId,
            cancellationToken);

        return ToActionResult(result);
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

    private async Task<ResultsAccessScope?> GetResultsAccessScopeAsync(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return null;
        }

        var accessScope = await _resultsAccessService.GetDiscoveryScopeAsync(
            userId,
            HasPermission(ReadAllPermission),
            HasPermission(ReadCareerPermission),
            cancellationToken);

        return accessScope.IsAllowed ? accessScope : null;
    }

    private async Task<IActionResult?> AuthorizeSurveySessionResultsAsync(
        Guid surveySessionId,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Forbid();
        }

        var decision = await _resultsAccessService.AuthorizeSurveySessionAsync(
            userId,
            HasPermission(ReadAllPermission),
            HasPermission(ReadCareerPermission),
            surveySessionId,
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
            ResultsAccessDecisionStatus.SurveySessionNotFound => NotFound(CreateErrorResponse([
                new ApplicationError("Results.SurveySessionNotFound", "Survey session was not found.")
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

    private static object CreateErrorResponse(IReadOnlyCollection<ApplicationError> errors)
    {
        return new
        {
            errors
        };
    }
}
