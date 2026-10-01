using AcademicSurveySystem.Application.Common.Results;
using AcademicSurveySystem.Application.Surveys.Responses;
using AcademicSurveySystem.Application.Surveys.Sessions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AcademicSurveySystem.Api.Controllers.Public;

/// <summary>
/// Provides public read access to open survey sessions by access code.
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("api/public/survey-sessions")]
public sealed class PublicSurveySessionsController : ControllerBase
{
    private readonly ISurveySessionService _surveySessionService;
    private readonly ISurveyResponseService _surveyResponseService;

    public PublicSurveySessionsController(
        ISurveySessionService surveySessionService,
        ISurveyResponseService surveyResponseService)
    {
        _surveySessionService = surveySessionService;
        _surveyResponseService = surveyResponseService;
    }

    /// <summary>
    /// Gets the public survey content for an open, active and non-expired session.
    /// </summary>
    [HttpGet("{accessCode}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetByAccessCode(
        string accessCode,
        CancellationToken cancellationToken)
    {
        var result = await _surveySessionService.GetPublicSessionByAccessCodeAsync(
            accessCode,
            cancellationToken);

        return ToActionResult(result);
    }

    /// <summary>
    /// Submits an anonymous response for an open public survey session.
    /// </summary>
    [HttpPost("{accessCode}/responses")]
    [ProducesResponseType(typeof(SurveyResponseSubmissionDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> SubmitResponse(
        string accessCode,
        SubmitSurveyResponseRequest? request,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return BadRequest(CreateErrorResponse([
                new ApplicationError("Request.Required", "Request body is required.")
            ]));
        }

        var result = await _surveyResponseService.SubmitResponseAsync(
            accessCode,
            request,
            cancellationToken);

        return result.Status == ApplicationResultStatus.Success
            ? StatusCode(StatusCodes.Status201Created, result.Value)
            : ToActionResult(result);
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
