using System.IdentityModel.Tokens.Jwt;
using AcademicSurveySystem.Api.Authorization;
using AcademicSurveySystem.Application.Common.Results;
using AcademicSurveySystem.Application.Surveys;
using AcademicSurveySystem.Application.Surveys.Requests;
using AcademicSurveySystem.Infrastructure.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AcademicSurveySystem.Api.Controllers.Surveys;

/// <summary>
/// Administra plantillas de encuestas, secciones, preguntas, opciones y filas de matriz.
/// </summary>
[ApiController]
[Authorize]
[Route("api/surveys")]
public sealed class SurveysController : ControllerBase
{
    private const string ReadPermission = "surveys.templates.read";
    private const string ManagePermission = "surveys.templates.manage";

    private readonly ISurveyTemplateService _surveyTemplateService;

    public SurveysController(ISurveyTemplateService surveyTemplateService)
    {
        _surveyTemplateService = surveyTemplateService;
    }

    /// <summary>
    /// Lista plantillas de encuestas.
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
        [FromQuery] string? status = null,
        [FromQuery] string? target = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _surveyTemplateService.GetSurveysAsync(
            includeInactive,
            status,
            target,
            cancellationToken);

        return ToActionResult(result);
    }

    /// <summary>
    /// Obtiene el detalle de una plantilla de encuesta.
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
        var result = await _surveyTemplateService.GetSurveyByIdAsync(id, cancellationToken);

        return ToActionResult(result);
    }

    /// <summary>
    /// Crea una plantilla de encuesta.
    /// </summary>
    /// <remarks>Requiere permiso de escritura: surveys.templates.manage.</remarks>
    [HttpPost]
    [RequirePermission(ManagePermission)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Create(
        CreateSurveyRequest? request,
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

        var result = await _surveyTemplateService.CreateSurveyAsync(userId, request, cancellationToken);

        if (result.Status == ApplicationResultStatus.Success)
        {
            return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value);
        }

        return ToActionResult(result);
    }

    /// <summary>
    /// Actualiza una plantilla de encuesta.
    /// </summary>
    /// <remarks>Requiere permiso de escritura: surveys.templates.manage.</remarks>
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
        UpdateSurveyRequest? request,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return BadRequest(CreateErrorResponse([
                new ApplicationError("Request.Required", "Request body is required.")
            ]));
        }

        var result = await _surveyTemplateService.UpdateSurveyAsync(id, request, cancellationToken);

        return ToActionResult(result);
    }

    /// <summary>
    /// Publica una plantilla de encuesta.
    /// </summary>
    /// <remarks>Requiere permiso de escritura: surveys.templates.manage.</remarks>
    [HttpPatch("{id:guid}/publish")]
    [RequirePermission(ManagePermission)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Publish(Guid id, CancellationToken cancellationToken)
    {
        var result = await _surveyTemplateService.PublishSurveyAsync(id, cancellationToken);

        return ToActionResult(result);
    }

    /// <summary>
    /// Archiva una plantilla de encuesta.
    /// </summary>
    /// <remarks>Requiere permiso de escritura: surveys.templates.manage.</remarks>
    [HttpPatch("{id:guid}/archive")]
    [RequirePermission(ManagePermission)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Archive(Guid id, CancellationToken cancellationToken)
    {
        var result = await _surveyTemplateService.ArchiveSurveyAsync(id, cancellationToken);

        return ToActionResult(result);
    }

    /// <summary>
    /// Activa una plantilla de encuesta.
    /// </summary>
    /// <remarks>Requiere permiso de escritura: surveys.templates.manage.</remarks>
    [HttpPatch("{id:guid}/activate")]
    [RequirePermission(ManagePermission)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Activate(Guid id, CancellationToken cancellationToken)
    {
        var result = await _surveyTemplateService.ActivateSurveyAsync(id, cancellationToken);

        return ToActionResult(result);
    }

    /// <summary>
    /// Desactiva una plantilla de encuesta.
    /// </summary>
    /// <remarks>Requiere permiso de escritura: surveys.templates.manage.</remarks>
    [HttpPatch("{id:guid}/deactivate")]
    [RequirePermission(ManagePermission)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        var result = await _surveyTemplateService.DeactivateSurveyAsync(id, cancellationToken);

        return ToActionResult(result);
    }

    /// <summary>
    /// Agrega una sección a una plantilla de encuesta.
    /// </summary>
    /// <remarks>Requiere permiso de escritura: surveys.templates.manage.</remarks>
    [HttpPost("{surveyId:guid}/sections")]
    [RequirePermission(ManagePermission)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> AddSection(
        Guid surveyId,
        CreateSurveySectionRequest? request,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return BadRequest(CreateErrorResponse([
                new ApplicationError("Request.Required", "Request body is required.")
            ]));
        }

        var result = await _surveyTemplateService.AddSectionAsync(surveyId, request, cancellationToken);

        if (result.Status == ApplicationResultStatus.Success)
        {
            return CreatedAtAction(nameof(GetById), new { id = surveyId }, result.Value);
        }

        return ToActionResult(result);
    }

    /// <summary>
    /// Actualiza una sección de una plantilla de encuesta.
    /// </summary>
    /// <remarks>Requiere permiso de escritura: surveys.templates.manage.</remarks>
    [HttpPut("{surveyId:guid}/sections/{sectionId:guid}")]
    [RequirePermission(ManagePermission)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateSection(
        Guid surveyId,
        Guid sectionId,
        UpdateSurveySectionRequest? request,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return BadRequest(CreateErrorResponse([
                new ApplicationError("Request.Required", "Request body is required.")
            ]));
        }

        var result = await _surveyTemplateService.UpdateSectionAsync(
            surveyId,
            sectionId,
            request,
            cancellationToken);

        return ToActionResult(result);
    }

    /// <summary>
    /// Activa una sección de una plantilla de encuesta.
    /// </summary>
    /// <remarks>Requiere permiso de escritura: surveys.templates.manage.</remarks>
    [HttpPatch("{surveyId:guid}/sections/{sectionId:guid}/activate")]
    [RequirePermission(ManagePermission)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ActivateSection(
        Guid surveyId,
        Guid sectionId,
        CancellationToken cancellationToken)
    {
        var result = await _surveyTemplateService.ActivateSectionAsync(surveyId, sectionId, cancellationToken);

        return ToActionResult(result);
    }

    /// <summary>
    /// Desactiva una sección de una plantilla de encuesta.
    /// </summary>
    /// <remarks>Requiere permiso de escritura: surveys.templates.manage.</remarks>
    [HttpPatch("{surveyId:guid}/sections/{sectionId:guid}/deactivate")]
    [RequirePermission(ManagePermission)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeactivateSection(
        Guid surveyId,
        Guid sectionId,
        CancellationToken cancellationToken)
    {
        var result = await _surveyTemplateService.DeactivateSectionAsync(surveyId, sectionId, cancellationToken);

        return ToActionResult(result);
    }

    /// <summary>
    /// Agrega una pregunta a una sección.
    /// </summary>
    /// <remarks>Requiere permiso de escritura: surveys.templates.manage.</remarks>
    [HttpPost("{surveyId:guid}/sections/{sectionId:guid}/questions")]
    [RequirePermission(ManagePermission)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> AddQuestion(
        Guid surveyId,
        Guid sectionId,
        CreateSurveyQuestionRequest? request,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return BadRequest(CreateErrorResponse([
                new ApplicationError("Request.Required", "Request body is required.")
            ]));
        }

        var result = await _surveyTemplateService.AddQuestionAsync(
            surveyId,
            sectionId,
            request,
            cancellationToken);

        if (result.Status == ApplicationResultStatus.Success)
        {
            return CreatedAtAction(nameof(GetById), new { id = surveyId }, result.Value);
        }

        return ToActionResult(result);
    }

    /// <summary>
    /// Actualiza una pregunta de una sección.
    /// </summary>
    /// <remarks>Requiere permiso de escritura: surveys.templates.manage.</remarks>
    [HttpPut("{surveyId:guid}/sections/{sectionId:guid}/questions/{questionId:guid}")]
    [RequirePermission(ManagePermission)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateQuestion(
        Guid surveyId,
        Guid sectionId,
        Guid questionId,
        UpdateSurveyQuestionRequest? request,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return BadRequest(CreateErrorResponse([
                new ApplicationError("Request.Required", "Request body is required.")
            ]));
        }

        var result = await _surveyTemplateService.UpdateQuestionAsync(
            surveyId,
            sectionId,
            questionId,
            request,
            cancellationToken);

        return ToActionResult(result);
    }

    /// <summary>
    /// Activa una pregunta de una sección.
    /// </summary>
    /// <remarks>Requiere permiso de escritura: surveys.templates.manage.</remarks>
    [HttpPatch("{surveyId:guid}/sections/{sectionId:guid}/questions/{questionId:guid}/activate")]
    [RequirePermission(ManagePermission)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ActivateQuestion(
        Guid surveyId,
        Guid sectionId,
        Guid questionId,
        CancellationToken cancellationToken)
    {
        var result = await _surveyTemplateService.ActivateQuestionAsync(
            surveyId,
            sectionId,
            questionId,
            cancellationToken);

        return ToActionResult(result);
    }

    /// <summary>
    /// Desactiva una pregunta de una sección.
    /// </summary>
    /// <remarks>Requiere permiso de escritura: surveys.templates.manage.</remarks>
    [HttpPatch("{surveyId:guid}/sections/{sectionId:guid}/questions/{questionId:guid}/deactivate")]
    [RequirePermission(ManagePermission)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeactivateQuestion(
        Guid surveyId,
        Guid sectionId,
        Guid questionId,
        CancellationToken cancellationToken)
    {
        var result = await _surveyTemplateService.DeactivateQuestionAsync(
            surveyId,
            sectionId,
            questionId,
            cancellationToken);

        return ToActionResult(result);
    }

    /// <summary>
    /// Agrega una opción a una pregunta de selección o matriz.
    /// </summary>
    /// <remarks>Requiere permiso de escritura: surveys.templates.manage.</remarks>
    [HttpPost("{surveyId:guid}/sections/{sectionId:guid}/questions/{questionId:guid}/options")]
    [RequirePermission(ManagePermission)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> AddOption(
        Guid surveyId,
        Guid sectionId,
        Guid questionId,
        CreateSurveyQuestionOptionRequest? request,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return BadRequest(CreateErrorResponse([
                new ApplicationError("Request.Required", "Request body is required.")
            ]));
        }

        var result = await _surveyTemplateService.AddOptionAsync(
            surveyId,
            sectionId,
            questionId,
            request,
            cancellationToken);

        if (result.Status == ApplicationResultStatus.Success)
        {
            return CreatedAtAction(nameof(GetById), new { id = surveyId }, result.Value);
        }

        return ToActionResult(result);
    }

    /// <summary>
    /// Activa una opción de pregunta.
    /// </summary>
    /// <remarks>Requiere permiso de escritura: surveys.templates.manage.</remarks>
    [HttpPatch(
        "{surveyId:guid}/sections/{sectionId:guid}/questions/{questionId:guid}/options/{optionId:guid}/activate")]
    [RequirePermission(ManagePermission)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ActivateOption(
        Guid surveyId,
        Guid sectionId,
        Guid questionId,
        Guid optionId,
        CancellationToken cancellationToken)
    {
        var result = await _surveyTemplateService.ActivateOptionAsync(
            surveyId,
            sectionId,
            questionId,
            optionId,
            cancellationToken);

        return ToActionResult(result);
    }

    /// <summary>
    /// Desactiva una opción de pregunta.
    /// </summary>
    /// <remarks>Requiere permiso de escritura: surveys.templates.manage.</remarks>
    [HttpPatch(
        "{surveyId:guid}/sections/{sectionId:guid}/questions/{questionId:guid}/options/{optionId:guid}/deactivate")]
    [RequirePermission(ManagePermission)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeactivateOption(
        Guid surveyId,
        Guid sectionId,
        Guid questionId,
        Guid optionId,
        CancellationToken cancellationToken)
    {
        var result = await _surveyTemplateService.DeactivateOptionAsync(
            surveyId,
            sectionId,
            questionId,
            optionId,
            cancellationToken);

        return ToActionResult(result);
    }

    /// <summary>
    /// Agrega una fila de matriz a una pregunta de matriz.
    /// </summary>
    /// <remarks>Requiere permiso de escritura: surveys.templates.manage.</remarks>
    [HttpPost("{surveyId:guid}/sections/{sectionId:guid}/questions/{questionId:guid}/matrix-rows")]
    [RequirePermission(ManagePermission)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> AddMatrixRow(
        Guid surveyId,
        Guid sectionId,
        Guid questionId,
        CreateSurveyMatrixRowRequest? request,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return BadRequest(CreateErrorResponse([
                new ApplicationError("Request.Required", "Request body is required.")
            ]));
        }

        var result = await _surveyTemplateService.AddMatrixRowAsync(
            surveyId,
            sectionId,
            questionId,
            request,
            cancellationToken);

        if (result.Status == ApplicationResultStatus.Success)
        {
            return CreatedAtAction(nameof(GetById), new { id = surveyId }, result.Value);
        }

        return ToActionResult(result);
    }

    /// <summary>
    /// Activa una fila de matriz.
    /// </summary>
    /// <remarks>Requiere permiso de escritura: surveys.templates.manage.</remarks>
    [HttpPatch(
        "{surveyId:guid}/sections/{sectionId:guid}/questions/{questionId:guid}/matrix-rows/{rowId:guid}/activate")]
    [RequirePermission(ManagePermission)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ActivateMatrixRow(
        Guid surveyId,
        Guid sectionId,
        Guid questionId,
        Guid rowId,
        CancellationToken cancellationToken)
    {
        var result = await _surveyTemplateService.ActivateMatrixRowAsync(
            surveyId,
            sectionId,
            questionId,
            rowId,
            cancellationToken);

        return ToActionResult(result);
    }

    /// <summary>
    /// Desactiva una fila de matriz.
    /// </summary>
    /// <remarks>Requiere permiso de escritura: surveys.templates.manage.</remarks>
    [HttpPatch(
        "{surveyId:guid}/sections/{sectionId:guid}/questions/{questionId:guid}/matrix-rows/{rowId:guid}/deactivate")]
    [RequirePermission(ManagePermission)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeactivateMatrixRow(
        Guid surveyId,
        Guid sectionId,
        Guid questionId,
        Guid rowId,
        CancellationToken cancellationToken)
    {
        var result = await _surveyTemplateService.DeactivateMatrixRowAsync(
            surveyId,
            sectionId,
            questionId,
            rowId,
            cancellationToken);

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
