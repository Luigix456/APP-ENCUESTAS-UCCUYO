using System.IdentityModel.Tokens.Jwt;
using AcademicSurveySystem.Application.Common.Authentication;
using AcademicSurveySystem.Application.Identity.Authentication;
using AcademicSurveySystem.Infrastructure.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AcademicSurveySystem.Api.Controllers;

/// <summary>
/// Endpoints de autenticación JWT y consulta del usuario actual.
/// </summary>
[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private const string InvalidCredentialsMessage = "Las credenciales son inválidas.";
    private const string DisabledUserMessage = "El usuario no se encuentra habilitado para acceder.";

    private readonly IAuthenticationService _authenticationService;

    public AuthController(IAuthenticationService authenticationService)
    {
        _authenticationService = authenticationService;
    }

    /// <summary>
    /// Inicia sesión y emite un token JWT.
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Login(
        LoginRequest? request,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return BadRequest(new
            {
                message = "La solicitud de inicio de sesión es inválida.",
                errors = new[] { "Request body is required." }
            });
        }

        var result = await _authenticationService.LoginAsync(request, cancellationToken);

        if (result.Succeeded)
        {
            return Ok(result.Response);
        }

        return result.FailureReason switch
        {
            AuthenticationFailureReason.InvalidRequest => BadRequest(new
            {
                message = "La solicitud de inicio de sesión es inválida.",
                errors = result.Errors
            }),
            AuthenticationFailureReason.InvalidCredentials => Unauthorized(new
            {
                message = InvalidCredentialsMessage
            }),
            AuthenticationFailureReason.UserInactive or AuthenticationFailureReason.UserBlocked => StatusCode(
                StatusCodes.Status403Forbidden,
                new
                {
                    message = DisabledUserMessage
                }),
            _ => StatusCode(
                StatusCodes.Status500InternalServerError,
                new
                {
                    message = "No se pudo completar el inicio de sesión."
                })
        };
    }

    /// <summary>
    /// Devuelve los datos del usuario autenticado desde el token JWT.
    /// </summary>
    /// <remarks>Requiere JWT Bearer válido.</remarks>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult Me()
    {
        var userIdValue = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
            ?? User.FindFirst(JwtTokenGenerator.NameIdentifierClaimType)?.Value;
        var email = User.FindFirst(JwtRegisteredClaimNames.Email)?.Value;
        var firstName = User.FindFirst(JwtRegisteredClaimNames.GivenName)?.Value;
        var lastName = User.FindFirst(JwtRegisteredClaimNames.FamilyName)?.Value;

        if (!Guid.TryParse(userIdValue, out var userId)
            || string.IsNullOrWhiteSpace(email)
            || string.IsNullOrWhiteSpace(firstName)
            || string.IsNullOrWhiteSpace(lastName))
        {
            return Unauthorized();
        }

        var user = new AuthenticatedUser(
            userId,
            firstName,
            lastName,
            email,
            User.FindAll(JwtTokenGenerator.RoleClaimType)
                .Select(claim => claim.Value)
                .Distinct(StringComparer.Ordinal)
                .Order(StringComparer.Ordinal)
                .ToArray(),
            User.FindAll(JwtTokenGenerator.PermissionClaimType)
                .Select(claim => claim.Value)
                .Distinct(StringComparer.Ordinal)
                .Order(StringComparer.Ordinal)
                .ToArray());

        return Ok(user);
    }
}
