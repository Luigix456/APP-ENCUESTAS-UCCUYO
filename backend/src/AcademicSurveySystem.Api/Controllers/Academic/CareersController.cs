using AcademicSurveySystem.Api.Authorization;
using AcademicSurveySystem.Application.Academic;
using AcademicSurveySystem.Application.Academic.Careers;
using AcademicSurveySystem.Application.Common.Results;
using Microsoft.AspNetCore.Mvc;

namespace AcademicSurveySystem.Api.Controllers.Academic;

/// <summary>
/// Administra carreras, cursos y trayectos académicos.
/// </summary>
[ApiController]
[Route("api/academic/careers")]
public sealed class CareersController : ControllerBase
{
    private const string ReadPermission = "academic.catalog.read";
    private const string ManagePermission = "academic.catalog.manage";

    private readonly IAcademicCatalogService _academicCatalogService;

    public CareersController(IAcademicCatalogService academicCatalogService)
    {
        _academicCatalogService = academicCatalogService;
    }

    /// <summary>
    /// Lista carreras del catálogo académico.
    /// </summary>
    /// <remarks>Requiere permiso de lectura: academic.catalog.read.</remarks>
    [HttpGet]
    [RequirePermission(ReadPermission)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAll(
        [FromQuery] bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        var result = await _academicCatalogService.GetCareersAsync(includeInactive, cancellationToken);

        return ToActionResult(result);
    }

    /// <summary>
    /// Obtiene una carrera por identificador.
    /// </summary>
    /// <remarks>Requiere permiso de lectura: academic.catalog.read.</remarks>
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
        var result = await _academicCatalogService.GetCareerByIdAsync(id, cancellationToken);

        return ToActionResult(result);
    }

    /// <summary>
    /// Crea una carrera.
    /// </summary>
    /// <remarks>Requiere permiso de escritura: academic.catalog.manage.</remarks>
    [HttpPost]
    [RequirePermission(ManagePermission)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Create(
        CreateCareerRequest? request,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return BadRequest(CreateErrorResponse([
                new ApplicationError("Request.Required", "Request body is required.")
            ]));
        }

        var result = await _academicCatalogService.CreateCareerAsync(request, cancellationToken);

        if (result.Status == ApplicationResultStatus.Success)
        {
            return CreatedAtAction(
                nameof(GetById),
                new { id = result.Value!.Id },
                result.Value);
        }

        return ToActionResult(result);
    }

    /// <summary>
    /// Actualiza una carrera.
    /// </summary>
    /// <remarks>Requiere permiso de escritura: academic.catalog.manage.</remarks>
    [HttpPut("{id:guid}")]
    [RequirePermission(ManagePermission)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Update(
        Guid id,
        UpdateCareerRequest? request,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return BadRequest(CreateErrorResponse([
                new ApplicationError("Request.Required", "Request body is required.")
            ]));
        }

        var result = await _academicCatalogService.UpdateCareerAsync(id, request, cancellationToken);

        return ToActionResult(result);
    }

    /// <summary>
    /// Activa una carrera.
    /// </summary>
    /// <remarks>Requiere permiso de escritura: academic.catalog.manage.</remarks>
    [HttpPatch("{id:guid}/activate")]
    [RequirePermission(ManagePermission)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Activate(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _academicCatalogService.ActivateCareerAsync(id, cancellationToken);

        return ToActionResult(result);
    }

    /// <summary>
    /// Desactiva una carrera.
    /// </summary>
    /// <remarks>Requiere permiso de escritura: academic.catalog.manage.</remarks>
    [HttpPatch("{id:guid}/deactivate")]
    [RequirePermission(ManagePermission)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Deactivate(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _academicCatalogService.DeactivateCareerAsync(id, cancellationToken);

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
