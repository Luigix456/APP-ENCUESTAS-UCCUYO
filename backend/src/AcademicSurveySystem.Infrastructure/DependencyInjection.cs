using AcademicSurveySystem.Application.Academic;
using AcademicSurveySystem.Application.Academic.SubjectEnrollments;
using AcademicSurveySystem.Application.Audit;
using AcademicSurveySystem.Application.Common.Security;
using AcademicSurveySystem.Application.Common.Authentication;
using AcademicSurveySystem.Application.Dashboard;
using AcademicSurveySystem.Application.Identity.AdminPasswordReset;
using AcademicSurveySystem.Application.Identity.Authentication;
using AcademicSurveySystem.Application.Identity.InitialAdministrator;
using AcademicSurveySystem.Application.Identity.UserCareers;
using AcademicSurveySystem.Application.Identity.UserManagement;
using AcademicSurveySystem.Application.Identity.UserPasswordReset;
using AcademicSurveySystem.Application.Surveys;
using AcademicSurveySystem.Application.Surveys.Assignments;
using AcademicSurveySystem.Application.Surveys.Responses;
using AcademicSurveySystem.Application.Surveys.Reports;
using AcademicSurveySystem.Application.Surveys.Results;
using AcademicSurveySystem.Application.Surveys.Sessions;
using AcademicSurveySystem.Infrastructure.Academic;
using AcademicSurveySystem.Infrastructure.Academic.Seeding;
using AcademicSurveySystem.Infrastructure.Audit;
using AcademicSurveySystem.Infrastructure.Authentication;
using AcademicSurveySystem.Infrastructure.Dashboard;
using AcademicSurveySystem.Infrastructure.Identity;
using AcademicSurveySystem.Infrastructure.Identity.Authentication;
using AcademicSurveySystem.Infrastructure.Persistence;
using AcademicSurveySystem.Infrastructure.Reports;
using AcademicSurveySystem.Infrastructure.Security;
using AcademicSurveySystem.Infrastructure.Surveys;
using AcademicSurveySystem.Infrastructure.Surveys.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using QuestPDF.Infrastructure;

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

        services
            .AddOptions<AdminPasswordResetOptions>()
            .Configure(options =>
            {
                options.Email = configuration["AdminPasswordReset:Email"];
                options.NewPassword = configuration["AdminPasswordReset:NewPassword"];
            });

        services
            .AddOptions<ReportOptions>()
            .Configure(options =>
            {
                options.InstitutionName = configuration["Reports:InstitutionName"] ?? options.InstitutionName;
                options.SystemName = configuration["Reports:SystemName"] ?? options.SystemName;
                options.FacultyName = configuration["Reports:FacultyName"] ?? options.FacultyName;
            });

        services
            .AddOptions<ResultsPrivacyOptions>()
            .Configure(options =>
            {
                var configuredMinimum = configuration["ResultsPrivacy:MinimumResponsesForDetailedResults"];

                if (int.TryParse(configuredMinimum, out var minimum))
                {
                    options.MinimumResponsesForDetailedResults = minimum;
                }
            })
            .Validate(
                options => options.IsValid(),
                $"ResultsPrivacy MinimumResponsesForDetailedResults must be greater than or equal to {ResultsPrivacyOptions.MinimumAllowedResponses}.")
            .ValidateOnStart();

        services
            .AddOptions<DashboardOptions>()
            .Configure(options =>
            {
                var configuredThreshold = configuration["Dashboard:LowParticipationThresholdPercentage"];

                if (decimal.TryParse(configuredThreshold, out var threshold))
                {
                    options.LowParticipationThresholdPercentage = threshold;
                }
            })
            .Validate(
                options => options.IsValid(),
                "Dashboard LowParticipationThresholdPercentage must be greater than 0 and less than or equal to 100.")
            .ValidateOnStart();

        services
            .AddOptions<SubjectEnrollmentImportOptions>()
            .Configure(options =>
            {
                var configuredMaxFileSize = configuration["Imports:SubjectEnrollmentMaxFileSizeBytes"];
                var configuredMaxRows = configuration["Imports:SubjectEnrollmentMaxRows"];

                if (long.TryParse(configuredMaxFileSize, out var maxFileSizeBytes))
                {
                    options.MaxFileSizeBytes = maxFileSizeBytes;
                }

                if (int.TryParse(configuredMaxRows, out var maxRows))
                {
                    options.MaxRows = maxRows;
                }
            })
            .Validate(
                options => options.IsValid(),
                "Subject enrollment import options must define positive MaxFileSizeBytes and MaxRows values.")
            .ValidateOnStart();

        QuestPDF.Settings.License = LicenseType.Community;

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
        services.AddScoped<IAdminPasswordResetStore, EfAdminPasswordResetStore>();
        services.AddScoped<IAdminPasswordResetService, AdminPasswordResetService>();
        services.AddScoped<IAcademicCatalogService, AcademicCatalogService>();
        services.AddScoped<UccuyoAcademicCatalogSeeder>();
        services.AddScoped<IAuditActorAccessor, CurrentAuditActorAccessor>();
        services.AddScoped<IAuditActorProvider>(provider =>
            provider.GetRequiredService<IAuditActorAccessor>());
        services.AddScoped<IAuditWriter, AuditWriter>();
        services.AddScoped<IAuditQueryService, AuditQueryService>();
        services.AddScoped<ISurveyTemplateService, SurveyTemplateService>();
        services.AddScoped<ISurveyAssignmentService, SurveyAssignmentService>();
        services.AddSingleton<ISurveySessionAccessCodeGenerator, SurveySessionAccessCodeGenerator>();
        services.AddScoped<ISurveySessionService, SurveySessionService>();
        services.AddScoped<ISurveyResponseProgressPublisher, NoOpSurveyResponseProgressPublisher>();
        services.AddScoped<ISurveyResponseService, SurveyResponseService>();
        services.AddScoped<ISurveyResultsService, SurveyResultsService>();
        services.AddScoped<IResultsAccessService, ResultsAccessService>();
        services.AddScoped<ICareerParticipationDashboardService, CareerParticipationDashboardService>();
        services.AddScoped<IReportService, ReportService>();
        services.AddScoped<IPdfReportGenerator, QuestPdfReportGenerator>();
        services.AddScoped<IUserCareerService, UserCareerService>();
        services.AddScoped<IUserManagementService, UserManagementService>();
        services.AddScoped<IUserPasswordResetService, UserPasswordResetService>();
        services.AddScoped<IInitialAdministratorStore, EfInitialAdministratorStore>();
        services.AddScoped<IInitialAdministratorBootstrapper, InitialAdministratorBootstrapper>();

        services
            .AddHealthChecks()
            .AddDbContextCheck<ApplicationDbContext>("ApplicationDbContext");

        return services;
    }
}
