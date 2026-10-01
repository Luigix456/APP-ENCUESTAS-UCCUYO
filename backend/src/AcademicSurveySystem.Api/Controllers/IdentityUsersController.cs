using System.IdentityModel.Tokens.Jwt;
using AcademicSurveySystem.Api.Authorization;
using AcademicSurveySystem.Application.Common.Results;
using AcademicSurveySystem.Application.Identity.UserCareers;
using AcademicSurveySystem.Application.Identity.UserManagement;
using AcademicSurveySystem.Application.Identity.UserPasswordReset;
using AcademicSurveySystem.Infrastructure.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AcademicSurveySystem.Api.Controllers;

/// <summary>
/// Administra asignaciones de alcance académico para usuarios.
/// </summary>
[ApiController]
[Authorize]
[Route("api/identity/users")]
public sealed class IdentityUsersController : ControllerBase
{
    private const string ReadUsersPermission = "identity.users.read";
    private const string CreateUsersPermission = "identity.users.create";
    private const string UpdateUsersPermission = "identity.users.update";
    private const string AssignRolesPermission = "identity.users.assign_roles";

    private readonly IUserManagementService _userManagementService;
    private readonly IUserCareerService _userCareerService;
    private readonly IUserPasswordResetService _passwordResetService;

    public IdentityUsersController(
        IUserManagementService userManagementService,
        IUserCareerService userCareerService,
        IUserPasswordResetService passwordResetService)
    {
        _userManagementService = userManagementService;
        _userCareerService = userCareerService;
        _passwordResetService = passwordResetService;
    }

    /// <summary>
    /// Lista usuarios administrativos.
    /// </summary>
    [HttpGet]
    [RequirePermission(ReadUsersPermission)]
    [ProducesResponseType(typeof(IReadOnlyCollection<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAll(
        [FromQuery] bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        var result = await _userManagementService.GetUsersAsync(
            includeInactive,
            cancellationToken);

        return ToActionResult(result);
    }

    /// <summary>
    /// Obtiene un usuario administrativo por identificador.
    /// </summary>
    [HttpGet("{userId:guid}")]
    [RequirePermission(ReadUsersPermission)]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetById(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var result = await _userManagementService.GetUserByIdAsync(
            userId,
            cancellationToken);

        return ToActionResult(result);
    }

    /// <summary>
    /// Crea un usuario administrativo.
    /// </summary>
    [HttpPost]
    [RequirePermission(CreateUsersPermission)]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Create(
        CreateUserRequest? request,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return BadRequest(CreateErrorResponse([
                new ApplicationError("Request.Required", "Request body is required.")
            ]));
        }

        var result = await _userManagementService.CreateUserAsync(
            request,
            cancellationToken);

        if (result.Status == ApplicationResultStatus.Success)
        {
            return CreatedAtAction(
                nameof(GetById),
                new { userId = result.Value!.Id },
                result.Value);
        }

        return ToActionResult(result);
    }

    /// <summary>
    /// Actualiza datos básicos de un usuario administrativo.
    /// </summary>
    [HttpPut("{userId:guid}")]
    [RequirePermission(UpdateUsersPermission)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Update(
        Guid userId,
        UpdateUserRequest? request,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return BadRequest(CreateErrorResponse([
                new ApplicationError("Request.Required", "Request body is required.")
            ]));
        }

        var result = await _userManagementService.UpdateUserAsync(
            userId,
            request,
            cancellationToken);

        return ToActionResult(result);
    }

    /// <summary>
    /// Activa un usuario administrativo.
    /// </summary>
    [HttpPatch("{userId:guid}/activate")]
    [RequirePermission(UpdateUsersPermission)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Activate(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var result = await _userManagementService.ActivateUserAsync(
            userId,
            cancellationToken);

        return ToActionResult(result);
    }

    /// <summary>
    /// Desactiva un usuario administrativo.
    /// </summary>
    [HttpPatch("{userId:guid}/deactivate")]
    [RequirePermission(UpdateUsersPermission)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Deactivate(
        Guid userId,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
        {
            return Forbid();
        }

        var result = await _userManagementService.DeactivateUserAsync(
            userId,
            currentUserId,
            cancellationToken);

        return ToActionResult(result);
    }

    /// <summary>
    /// Da de baja operativamente un usuario administrativo sin eliminar su historial.
    /// </summary>
    [HttpDelete("{userId:guid}")]
    [RequirePermission(UpdateUsersPermission)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Delete(
        Guid userId,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
        {
            return Forbid();
        }

        var result = await _userManagementService.DeactivateUserAsync(
            userId,
            currentUserId,
            cancellationToken);

        return ToActionResult(result);
    }

    /// <summary>
    /// Restablece la contraseña de un usuario administrativo existente.
    /// </summary>
    [HttpPut("{userId:guid}/password")]
    [RequirePermission(UpdateUsersPermission)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ResetPassword(
        Guid userId,
        ResetUserPasswordRequest? request,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return BadRequest(CreateErrorResponse([
                new ApplicationError("Request.Required", "Request body is required.")
            ]));
        }

        var result = await _passwordResetService.ResetByUserIdAsync(
            userId,
            request.NewPassword,
            cancellationToken);

        return ToActionResult(result);
    }

    /// <summary>
    /// Reemplaza completamente los roles asignados a un usuario.
    /// </summary>
    [HttpPut("{userId:guid}/roles")]
    [RequirePermission(AssignRolesPermission)]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ReplaceRoles(
        Guid userId,
        UpdateUserRolesRequest? request,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return BadRequest(CreateErrorResponse([
                new ApplicationError("Request.Required", "Request body is required.")
            ]));
        }

        var result = await _userManagementService.ReplaceUserRolesAsync(
            userId,
            request,
            cancellationToken);

        return ToActionResult(result);
    }

    /// <summary>
    /// Lista las carreras asignadas a un usuario.
    /// </summary>
    [HttpGet("{userId:guid}/careers")]
    [RequirePermission(ReadUsersPermission)]
    [ProducesResponseType(typeof(IReadOnlyCollection<UserCareerDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetCareers(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var result = await _userCareerService.GetUserCareersAsync(userId, cancellationToken);

        return ToActionResult(result);
    }

    /// <summary>
    /// Reemplaza de forma completa las carreras asignadas a un usuario.
    /// </summary>
    [HttpPut("{userId:guid}/careers")]
    [RequirePermission(UpdateUsersPermission)]
    [ProducesResponseType(typeof(IReadOnlyCollection<UserCareerDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ReplaceCareers(
        Guid userId,
        UpdateUserCareersRequest? request,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return BadRequest(CreateErrorResponse([
                new ApplicationError("Request.Required", "Request body is required.")
            ]));
        }

        var result = await _userCareerService.ReplaceUserCareersAsync(
            userId,
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

    private IActionResult ToActionResult(UserPasswordResetResult result)
    {
        return result.Status switch
        {
            UserPasswordResetStatus.Updated => NoContent(),
            UserPasswordResetStatus.InvalidRequest => BadRequest(CreateErrorResponse([
                new ApplicationError("Request.Invalid", result.Message)
            ])),
            UserPasswordResetStatus.InvalidPassword => BadRequest(CreateErrorResponse([
                new ApplicationError("Identity.InvalidPassword", result.Message)
            ])),
            UserPasswordResetStatus.UserNotFound => NotFound(CreateErrorResponse([
                new ApplicationError("NotFound", result.Message)
            ])),
            _ => StatusCode(StatusCodes.Status500InternalServerError, CreateErrorResponse([
                new ApplicationError("Failure", result.Message)
            ]))
        };
    }

    private bool TryGetCurrentUserId(out Guid userId)
    {
        var userIdValue = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
            ?? User.FindFirst(JwtTokenGenerator.NameIdentifierClaimType)?.Value;

        return Guid.TryParse(userIdValue, out userId);
    }

    private static object CreateErrorResponse(IReadOnlyCollection<ApplicationError> errors)
    {
        return new
        {
            errors
        };
    }
}
