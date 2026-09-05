using AcademicSurveySystem.Application.Academic;
using AcademicSurveySystem.Application.Common.Security;
using AcademicSurveySystem.Application.Common.Authentication;
using AcademicSurveySystem.Application.Identity.Authentication;
using AcademicSurveySystem.Application.Identity.InitialAdministrator;
using AcademicSurveySystem.Application.Surveys;
using AcademicSurveySystem.Infrastructure.Academic;
using AcademicSurveySystem.Infrastructure.Authentication;
using AcademicSurveySystem.Infrastructure.Identity;
using AcademicSurveySystem.Infrastructure.Identity.Authentication;
using AcademicSurveySystem.Infrastructure.Persistence;
using AcademicSurveySystem.Infrastructure.Security;
using AcademicSurveySystem.Infrastructure.Surveys;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AcademicSurveySystem.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        bool requireJwtOptions = true)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Connection string 'DefaultConnection' is not configured.");
        }

        services
            .AddOptions<PasswordHashingOptions>()
            .Configure(options =>
            {
                var configuredWorkFactor = configuration["Security:PasswordHashing:WorkFactor"];

                if (int.TryParse(configuredWorkFactor, out var workFactor))
                {
                    options.WorkFactor = workFactor;
                }
            })
            .Validate(
                options => options.WorkFactor is >= PasswordHashingOptions.MinimumWorkFactor
                    and <= PasswordHashingOptions.MaximumWorkFactor,
                $"Password hashing WorkFactor must be between {PasswordHashingOptions.MinimumWorkFactor} and {PasswordHashingOptions.MaximumWorkFactor}.")
            .ValidateOnStart();

        services
            .AddOptions<InitialAdministratorOptions>()
            .Configure(options =>
            {
                options.FirstName = configuration["InitialAdmin:FirstName"];
                options.LastName = configuration["InitialAdmin:LastName"];
                options.Email = configuration["InitialAdmin:Email"];
                options.Password = configuration["InitialAdmin:Password"];
            });

        var jwtOptionsBuilder = services
            .AddOptions<JwtOptions>()
            .Configure(options =>
            {
                options.Issuer = configuration["Jwt:Issuer"] ?? options.Issuer;
                options.Audience = configuration["Jwt:Audience"] ?? options.Audience;
                options.SigningKey = configuration["Jwt:SigningKey"] ?? options.SigningKey;

                var configuredExpiration = configuration["Jwt:AccessTokenExpirationMinutes"];

                if (int.TryParse(configuredExpiration, out var expirationMinutes))
                {
                    options.AccessTokenExpirationMinutes = expirationMinutes;
                }
            });

        if (requireJwtOptions)
        {
            jwtOptionsBuilder
                .Validate(
                    options =>
                        !string.IsNullOrWhiteSpace(options.Issuer)
                        && !string.IsNullOrWhiteSpace(options.Audience)
                        && !string.IsNullOrWhiteSpace(options.SigningKey)
                        && options.SigningKey.Length >= JwtOptions.MinimumSigningKeyLength
                        && options.AccessTokenExpirationMinutes is >= JwtOptions.MinimumAccessTokenExpirationMinutes
                            and <= JwtOptions.MaximumAccessTokenExpirationMinutes,
                    $"Jwt configuration is invalid. Issuer, Audience and SigningKey are required; SigningKey must be at least {JwtOptions.MinimumSigningKeyLength} characters; AccessTokenExpirationMinutes must be between {JwtOptions.MinimumAccessTokenExpirationMinutes} and {JwtOptions.MaximumAccessTokenExpirationMinutes}.")
                .ValidateOnStart();
        }

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<IAuthenticationUserStore, EfAuthenticationUserStore>();
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<IAcademicCatalogService, AcademicCatalogService>();
        services.AddScoped<ISurveyTemplateService, SurveyTemplateService>();
        services.AddScoped<IInitialAdministratorStore, EfInitialAdministratorStore>();
        services.AddScoped<IInitialAdministratorBootstrapper, InitialAdministratorBootstrapper>();

        services
            .AddHealthChecks()
            .AddDbContextCheck<ApplicationDbContext>("ApplicationDbContext");

        return services;
    }
}
