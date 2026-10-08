using System.IdentityModel.Tokens.Jwt;
using AcademicSurveySystem.Application.Common.Results;
using AcademicSurveySystem.Application.Dashboard;
using AcademicSurveySystem.Application.Surveys.Results;
using AcademicSurveySystem.Infrastructure.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AcademicSurveySystem.Api.Controllers;

/// <summary>
/// Consulta métricas agregadas para tableros académicos.
/// </summary>
[ApiController]
[Authorize]
[Route("api/dashboard")]
public sealed class DashboardController : ControllerBase
{
    private const string ReadAllPermission = "results.read_all";
    private const string ReadCareerPermission = "results.read_career";

    private readonly ICareerParticipationDashboardService _dashboardService;
    private readonly IResultsAccessService _resultsAccessService;

    public DashboardController(
        ICareerParticipationDashboardService dashboardService,
        IResultsAccessService resultsAccessService)
    {
        _dashboardService = dashboardService;
        _resultsAccessService = resultsAccessService;
    }

    /// <summary>
    /// Obtiene el resumen de participación de una carrera para un ciclo académico.
    /// </summary>
    [HttpGet("careers/{careerId:guid}")]
    [ProducesResponseType(typeof(CareerParticipationDashboardDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetCareerParticipation(
        Guid careerId,
        [FromQuery] Guid academicCycleId,
        CancellationToken cancellationToken)
    {
        if (academicCycleId == Guid.Empty)
        {
            return BadRequest(CreateErrorResponse([
                new ApplicationError("Dashboard.AcademicCycleRequired", "AcademicCycleId is required.")
            ]));
        }

        if (!TryGetUserId(out var userId))
        {
            return Forbid();
        }

        var accessScope = await _resultsAccessService.GetDiscoveryScopeAsync(
            userId,
            HasPermission(ReadAllPermission),
            HasPermission(ReadCareerPermission),
            cancellationToken);

        if (!CanReadCareer(accessScope, careerId))
        {
            return Forbid();
        }

        var result = await _dashboardService.GetCareerParticipationAsync(
            careerId,
            academicCycleId,
            cancellationToken);

        return ToActionResult(result);
    }

    private static bool CanReadCareer(ResultsAccessScope accessScope, Guid careerId)
    {
        return accessScope.Type switch
        {
            ResultsAccessScopeType.All => true,
            ResultsAccessScopeType.Career => accessScope.CareerIds.Contains(careerId),
            _ => false
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
