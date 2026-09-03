using AcademicSurveySystem.Api.Authorization;
using AcademicSurveySystem.Application.Academic;
using AcademicSurveySystem.Application.Academic.Teachers;
using AcademicSurveySystem.Application.Common.Results;
using Microsoft.AspNetCore.Mvc;

namespace AcademicSurveySystem.Api.Controllers.Academic;

[ApiController]
[Route("api/academic/teachers")]
public sealed class TeachersController : ControllerBase
{
    private const string ReadPermission = "academic.catalog.read";
    private const string ManagePermission = "academic.catalog.manage";

    private readonly IAcademicCatalogService _academicCatalogService;

    public TeachersController(IAcademicCatalogService academicCatalogService)
    {
        _academicCatalogService = academicCatalogService;
    }

    [HttpGet]
    [RequirePermission(ReadPermission)]
    public async Task<IActionResult> GetAll(
        [FromQuery] bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        var result = await _academicCatalogService.GetTeachersAsync(includeInactive, cancellationToken);

        return ToActionResult(result);
    }

    [HttpGet("{id:guid}")]
    [RequirePermission(ReadPermission)]
    public async Task<IActionResult> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _academicCatalogService.GetTeacherByIdAsync(id, cancellationToken);

        return ToActionResult(result);
    }

    [HttpPost]
    [RequirePermission(ManagePermission)]
    public async Task<IActionResult> Create(
        CreateTeacherRequest? request,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return BadRequest(CreateErrorResponse([
                new ApplicationError("Request.Required", "Request body is required.")
            ]));
        }

        var result = await _academicCatalogService.CreateTeacherAsync(request, cancellationToken);

        if (result.Status == ApplicationResultStatus.Success)
        {
            return CreatedAtAction(
                nameof(GetById),
                new { id = result.Value!.Id },
                result.Value);
        }

        return ToActionResult(result);
    }

    [HttpPut("{id:guid}")]
    [RequirePermission(ManagePermission)]
    public async Task<IActionResult> Update(
        Guid id,
        UpdateTeacherRequest? request,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return BadRequest(CreateErrorResponse([
                new ApplicationError("Request.Required", "Request body is required.")
            ]));
        }

        var result = await _academicCatalogService.UpdateTeacherAsync(id, request, cancellationToken);

        return ToActionResult(result);
    }

    [HttpPatch("{id:guid}/activate")]
    [RequirePermission(ManagePermission)]
    public async Task<IActionResult> Activate(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _academicCatalogService.ActivateTeacherAsync(id, cancellationToken);

        return ToActionResult(result);
    }

    [HttpPatch("{id:guid}/deactivate")]
    [RequirePermission(ManagePermission)]
    public async Task<IActionResult> Deactivate(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _academicCatalogService.DeactivateTeacherAsync(id, cancellationToken);

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
