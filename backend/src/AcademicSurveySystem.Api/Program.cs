using System.Text.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using AcademicSurveySystem.Api.Authorization;
using AcademicSurveySystem.Application.Identity.InitialAdministrator;
using AcademicSurveySystem.Infrastructure;
using AcademicSurveySystem.Infrastructure.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;

var bootstrapAdmin = args.Contains("--bootstrap-admin", StringComparer.Ordinal);

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddInfrastructure(builder.Configuration, requireJwtOptions: !bootstrapAdmin);

if (!bootstrapAdmin)
{
    JwtSecurityTokenHandler.DefaultMapInboundClaims = false;

    builder.Services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            var jwtOptions = GetJwtOptions(builder.Configuration);

            options.MapInboundClaims = false;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwtOptions.Issuer,
                ValidateAudience = true,
                ValidAudience = jwtOptions.Audience,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(1),
                NameClaimType = JwtTokenGenerator.NameIdentifierClaimType,
                RoleClaimType = JwtTokenGenerator.RoleClaimType
            };
        });

    builder.Services.AddAuthorization(options => options.AddPermissionPolicies());
    builder.Services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();
}

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

if (!bootstrapAdmin)
{
    app.UseAuthentication();
    app.UseAuthorization();
}

app.MapControllers();
app.MapHealthChecks("/api/health", new HealthCheckOptions
{
    ResponseWriter = async (context, healthReport) =>
    {
        context.Response.ContentType = "application/json";

        var response = new
        {
            status = healthReport.Status.ToString(),
            service = "AcademicSurveySystem.Api",
            checks = healthReport.Entries.Select(entry => new
            {
                name = entry.Key,
                status = entry.Value.Status.ToString()
            })
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
});

if (bootstrapAdmin)
{
    using var scope = app.Services.CreateScope();
    var bootstrapper = scope.ServiceProvider.GetRequiredService<IInitialAdministratorBootstrapper>();
    var result = await bootstrapper.BootstrapAsync();

    Console.WriteLine(result.Status switch
    {
        InitialAdministratorBootstrapStatus.Created =>
            "Initial administrator bootstrap completed: created.",
        InitialAdministratorBootstrapStatus.AlreadyExists =>
            "Initial administrator bootstrap completed: already exists.",
        _ =>
            "Initial administrator bootstrap failed."
    });

    return result.Succeeded ? 0 : 1;
}

app.Run();

return 0;

static JwtOptions GetJwtOptions(IConfiguration configuration)
{
    var jwtOptions = new JwtOptions
    {
        Issuer = configuration["Jwt:Issuer"] ?? "AcademicSurveySystem",
        Audience = configuration["Jwt:Audience"] ?? "AcademicSurveySystem.Web",
        SigningKey = configuration["Jwt:SigningKey"] ?? string.Empty,
        AccessTokenExpirationMinutes = int.TryParse(
            configuration["Jwt:AccessTokenExpirationMinutes"],
            out var expirationMinutes)
            ? expirationMinutes
            : 60
    };

    jwtOptions.Validate();

    return jwtOptions;
}

public partial class Program
{
}
