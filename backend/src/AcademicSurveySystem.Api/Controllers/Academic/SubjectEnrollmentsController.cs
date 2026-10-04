using AcademicSurveySystem.Api.Authorization;
using AcademicSurveySystem.Application.Academic;
using AcademicSurveySystem.Application.Academic.SubjectEnrollments;
using AcademicSurveySystem.Application.Common.Results;
using Microsoft.AspNetCore.Mvc;

namespace AcademicSurveySystem.Api.Controllers.Academic;

/// <summary>
/// Administra matrículas de materias por ciclo lectivo.
/// </summary>
[ApiController]
[Route("api/academic")]
public sealed class SubjectEnrollmentsController : ControllerBase
{
    private const string ReadPermission = "academic.catalog.read";
    private const string ManagePermission = "academic.catalog.manage";

    private readonly IAcademicCatalogService _academicCatalogService;

    public SubjectEnrollmentsController(IAcademicCatalogService academicCatalogService)
    {
        _academicCatalogService = academicCatalogService;
    }

    /// <summary>
    /// Lista matrículas de materias por ciclo lectivo.
    /// </summary>
    /// <remarks>Requiere permiso de lectura: academic.catalog.read.</remarks>
    [HttpGet("subject-enrollments")]
    [RequirePermission(ReadPermission)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAll(
        [FromQuery] Guid? subjectId = null,
        [FromQuery] Guid? academicCycleId = null,
        [FromQuery] Guid? careerId = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _academicCatalogService.GetSubjectEnrollmentsAsync(
            subjectId,
            academicCycleId,
            careerId,
            cancellationToken);

        return ToActionResult(result);
    }

    /// <summary>
    /// Obtiene la matrícula de una materia para un ciclo lectivo.
    /// </summary>
    /// <remarks>Requiere permiso de lectura: academic.catalog.read.</remarks>
    [HttpGet("subjects/{subjectId:guid}/enrollment")]
    [RequirePermission(ReadPermission)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetBySubjectAndCycle(
        Guid subjectId,
        [FromQuery] Guid academicCycleId,
        CancellationToken cancellationToken = default)
    {
        var result = await _academicCatalogService.GetSubjectEnrollmentAsync(
            subjectId,
            academicCycleId,
            cancellationToken);

        return ToActionResult(result);
    }

    /// <summary>
    /// Crea o actualiza la matrícula de una materia para un ciclo lectivo.
    /// </summary>
    /// <remarks>Requiere permiso de escritura: academic.catalog.manage.</remarks>
    [HttpPut("subjects/{subjectId:guid}/enrollment/{academicCycleId:guid}")]
    [RequirePermission(ManagePermission)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Set(
        Guid subjectId,
        Guid academicCycleId,
        SetSubjectEnrollmentRequest? request,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            return BadRequest(CreateErrorResponse([
                new ApplicationError("Request.Required", "Request body is required.")
            ]));
        }

        var result = await _academicCatalogService.SetSubjectEnrollmentAsync(
            subjectId,
            academicCycleId,
            request,
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
