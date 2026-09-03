using AcademicSurveySystem.Domain.Identity;
using Microsoft.AspNetCore.Authorization;

namespace AcademicSurveySystem.Api.Authorization;

public static class PermissionAuthorizationExtensions
{
    public const string PolicyPrefix = "Permission:";

    public static void AddPermissionPolicies(this AuthorizationOptions options)
    {
        foreach (var permission in IdentityCatalog.Permissions)
        {
            options.AddPolicy(
                GetPolicyName(permission.Code),
                policy => policy
                    .RequireAuthenticatedUser()
                    .AddRequirements(new PermissionRequirement(permission.Code)));
        }
    }

    public static string GetPolicyName(string permission) =>
        $"{PolicyPrefix}{permission}";
}
