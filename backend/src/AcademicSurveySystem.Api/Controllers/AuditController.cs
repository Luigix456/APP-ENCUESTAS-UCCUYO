using AcademicSurveySystem.Api.Authorization;
using AcademicSurveySystem.Application.Audit;
using AcademicSurveySystem.Application.Common.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AcademicSurveySystem.Api.Controllers;

/// <summary>
/// Consulta registros de auditoría append-only del sistema.
/// </summary>
[ApiController]
[Authorize]
[Route("api/audit")]
public sealed class AuditController : ControllerBase
{
    private const string ReadPermission = "audit.read";
    private readonly IAuditQueryService _auditQueryService;

    public AuditController(IAuditQueryService auditQueryService)
    {
        _auditQueryService = auditQueryService;
    }

    /// <summary>
    /// Lista acciones administrativas y operativas auditadas.
    /// </summary>
    [HttpGet]
    [RequirePermission(ReadPermission)]
    [ProducesResponseType(typeof(AuditEntriesPageDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAll(
        [FromQuery] DateTimeOffset? fromUtc = null,
        [FromQuery] DateTimeOffset? toUtc = null,
        [FromQuery] Guid? actorUserId = null,
        [FromQuery] string? module = null,
        [FromQuery] string? action = null,
        [FromQuery] string? entityType = null,
        [FromQuery] string? search = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken cancellationToken = default)
    {
        var result = await _auditQueryService.GetEntriesAsync(
            new AuditEntryFilter(
                fromUtc,
                toUtc,
                actorUserId,
                module,
                action,
                entityType,
                search,
                page,
                pageSize),
            cancellationToken);

        return ToActionResult(result);
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
