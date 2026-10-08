using AcademicSurveySystem.Api.Authorization;
using AcademicSurveySystem.Application.Common.Results;
using AcademicSurveySystem.Application.Surveys.Assignments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AcademicSurveySystem.Api.Controllers.Surveys;

/// <summary>
/// Administra asignaciones de plantillas de encuestas publicadas a contextos académicos.
/// </summary>
[ApiController]
[Authorize]
[Route("api/survey-assignments")]
public sealed class SurveyAssignmentsController : ControllerBase
{
    private const string ReadPermission = "surveys.templates.read";
    private const string ManagePermission = "surveys.templates.manage";

    private readonly ISurveyAssignmentService _surveyAssignmentService;

    public SurveyAssignmentsController(ISurveyAssignmentService surveyAssignmentService)
    {
        _surveyAssignmentService = surveyAssignmentService;
    }

    /// <summary>
    /// Lista asignaciones de encuestas.
    /// </summary>
    /// <remarks>Requiere permiso de lectura: surveys.templates.read.</remarks>
    [HttpGet]
    [RequirePermission(ReadPermission)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAll(
        [FromQuery] bool includeInactive = false,
        [FromQuery] Guid? surveyId = null,
        [FromQuery] Guid? careerId = null,
        [FromQuery] Guid? subjectId = null,
        [FromQuery] Guid? academicCycleId = null,
        [FromQuery] Guid? teacherId = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _surveyAssignmentService.GetAssignmentsAsync(
            includeInactive,
            surveyId,
            careerId,
            subjectId,
            academicCycleId,
            teacherId,
            cancellationToken);

        return ToActionResult(result);
    }

    /// <summary>
    /// Obtiene una asignación de encuesta por identificador.
    /// </summary>
    /// <remarks>Requiere permiso de lectura: surveys.templates.read.</remarks>
    [HttpGet("{id:guid}")]
    [RequirePermission(ReadPermission)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _surveyAssignmentService.GetAssignmentByIdAsync(id, cancellationToken);

        return ToActionResult(result);
    }

    /// <summary>
    /// Crea una asignación de encuesta para un contexto académico.
    /// </summary>
    /// <remarks>Requiere permiso de escritura: surveys.templates.manage.</remarks>
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
        CreateSurveyAssignmentRequest? request,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return BadRequest(CreateErrorResponse([
                new ApplicationError("Request.Required", "Request body is required.")
            ]));
        }

        var result = await _surveyAssignmentService.CreateAssignmentAsync(request, cancellationToken);

        if (result.Status == ApplicationResultStatus.Success)
        {
            return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value);
        }

        return ToActionResult(result);
    }

    /// <summary>
    /// Crea varias asignaciones de encuesta para la misma carrera, materia y ciclo.
    /// </summary>
    /// <remarks>Requiere permiso de escritura: surveys.templates.manage.</remarks>
    [HttpPost("batch")]
    [RequirePermission(ManagePermission)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateBatch(
        CreateSurveyAssignmentBatchRequest? request,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return BadRequest(CreateErrorResponse([
                new ApplicationError("Request.Required", "Request body is required.")
            ]));
        }

        var result = await _surveyAssignmentService.CreateAssignmentsAsync(request, cancellationToken);

        if (result.Status == ApplicationResultStatus.Success)
        {
            return StatusCode(StatusCodes.Status201Created, result.Value);
        }

        return ToActionResult(result);
    }

    /// <summary>
    /// Activa una asignación de encuesta.
    /// </summary>
    /// <remarks>Requiere permiso de escritura: surveys.templates.manage.</remarks>
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
        var result = await _surveyAssignmentService.ActivateAssignmentAsync(id, cancellationToken);

        return ToActionResult(result);
    }

    /// <summary>
    /// Desactiva una asignación de encuesta.
    /// </summary>
    /// <remarks>Requiere permiso de escritura: surveys.templates.manage.</remarks>
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
        var result = await _surveyAssignmentService.DeactivateAssignmentAsync(id, cancellationToken);

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
