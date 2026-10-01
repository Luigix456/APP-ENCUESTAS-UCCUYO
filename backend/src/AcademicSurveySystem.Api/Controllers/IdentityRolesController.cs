using AcademicSurveySystem.Api.Authorization;
using AcademicSurveySystem.Application.Common.Results;
using AcademicSurveySystem.Application.Identity.UserManagement;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AcademicSurveySystem.Api.Controllers;

/// <summary>
/// Consulta roles administrativos disponibles.
/// </summary>
[ApiController]
[Authorize]
[Route("api/identity/roles")]
public sealed class IdentityRolesController : ControllerBase
{
    private const string ReadRolesPermission = "identity.roles.read";

    private readonly IUserManagementService _userManagementService;

    public IdentityRolesController(IUserManagementService userManagementService)
    {
        _userManagementService = userManagementService;
    }

    /// <summary>
    /// Lista roles disponibles para asignación administrativa.
    /// </summary>
    [HttpGet]
    [RequirePermission(ReadRolesPermission)]
    [ProducesResponseType(typeof(IReadOnlyCollection<RoleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await _userManagementService.GetRolesAsync(cancellationToken);

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
