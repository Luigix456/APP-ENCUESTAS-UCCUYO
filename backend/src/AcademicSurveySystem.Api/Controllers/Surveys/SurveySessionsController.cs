using System.IdentityModel.Tokens.Jwt;
using AcademicSurveySystem.Api.Authorization;
using AcademicSurveySystem.Application.Common.Results;
using AcademicSurveySystem.Application.Surveys.Sessions;
using AcademicSurveySystem.Infrastructure.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AcademicSurveySystem.Api.Controllers.Surveys;

/// <summary>
/// Administers temporary survey sessions associated with academic survey assignments.
/// </summary>
[ApiController]
[Authorize]
[Route("api/survey-sessions")]
public sealed class SurveySessionsController : ControllerBase
{
    private const string ManagePermission = "surveys.sessions.manage";

    private readonly ISurveySessionService _surveySessionService;

    public SurveySessionsController(ISurveySessionService surveySessionService)
    {
        _surveySessionService = surveySessionService;
    }

    /// <summary>
    /// Lists temporary survey sessions.
    /// </summary>
    [HttpGet]
    [RequirePermission(ManagePermission)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAll(
        [FromQuery] bool includeInactive = false,
        [FromQuery] string? status = null,
        [FromQuery] Guid? surveyAssignmentId = null,
        [FromQuery] string? accessCode = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _surveySessionService.GetSessionsAsync(
            includeInactive,
            status,
            surveyAssignmentId,
            accessCode,
            cancellationToken);

        return ToActionResult(result);
    }

    /// <summary>
    /// Gets a temporary survey session by identifier.
    /// </summary>
    [HttpGet("{id:guid}")]
    [RequirePermission(ManagePermission)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _surveySessionService.GetSessionByIdAsync(id, cancellationToken);

        return ToActionResult(result);
    }

    /// <summary>
    /// Creates a temporary survey session and generates its access code.
    /// </summary>
    [HttpPost]
    [RequirePermission(ManagePermission)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Create(
        CreateSurveySessionRequest? request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        if (request is null)
        {
            return BadRequest(CreateErrorResponse([
                new ApplicationError("Request.Required", "Request body is required.")
            ]));
        }

        var result = await _surveySessionService.CreateSessionAsync(
            request,
            userId,
            cancellationToken);

        if (result.Status == ApplicationResultStatus.Success)
        {
            return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value);
        }

        return ToActionResult(result);
    }

    /// <summary>
    /// Updates title, location and expiration of a temporary survey session.
    /// </summary>
    [HttpPut("{id:guid}")]
    [RequirePermission(ManagePermission)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Update(
        Guid id,
        UpdateSurveySessionRequest? request,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return BadRequest(CreateErrorResponse([
                new ApplicationError("Request.Required", "Request body is required.")
            ]));
        }

        var result = await _surveySessionService.UpdateSessionAsync(id, request, cancellationToken);

        return ToActionResult(result);
    }

    /// <summary>
    /// Opens a temporary survey session.
    /// </summary>
    [HttpPatch("{id:guid}/open")]
    [RequirePermission(ManagePermission)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Open(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _surveySessionService.OpenSessionAsync(id, cancellationToken);

        return ToActionResult(result);
    }

    /// <summary>
    /// Closes a temporary survey session and invalidates public access.
    /// </summary>
    [HttpPatch("{id:guid}/close")]
    [RequirePermission(ManagePermission)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Close(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _surveySessionService.CloseSessionAsync(id, cancellationToken);

        return ToActionResult(result);
    }

    /// <summary>
    /// Cancels a temporary survey session and invalidates public access.
    /// </summary>
    [HttpPatch("{id:guid}/cancel")]
    [RequirePermission(ManagePermission)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Cancel(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _surveySessionService.CancelSessionAsync(id, cancellationToken);

        return ToActionResult(result);
    }

    /// <summary>
    /// Activates a temporary survey session.
    /// </summary>
    [HttpPatch("{id:guid}/activate")]
    [RequirePermission(ManagePermission)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Activate(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _surveySessionService.ActivateSessionAsync(id, cancellationToken);

        return ToActionResult(result);
    }

    /// <summary>
    /// Deactivates a temporary survey session and invalidates public access.
    /// </summary>
    [HttpPatch("{id:guid}/deactivate")]
    [RequirePermission(ManagePermission)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Deactivate(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _surveySessionService.DeactivateSessionAsync(id, cancellationToken);

        return ToActionResult(result);
    }

    private bool TryGetUserId(out Guid userId)
    {
        var userIdValue = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
            ?? User.FindFirst(JwtTokenGenerator.NameIdentifierClaimType)?.Value;

        return Guid.TryParse(userIdValue, out userId);
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

    private IActionResult ToActionResult(ApplicationResult result)
    {
        return result.Status switch
        {
            ApplicationResultStatus.Success => NoContent(),
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
