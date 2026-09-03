using Microsoft.AspNetCore.Authorization;

namespace AcademicSurveySystem.Api.Authorization;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class RequirePermissionAttribute : AuthorizeAttribute
{
    public RequirePermissionAttribute(string permission)
    {
        Policy = PermissionAuthorizationExtensions.GetPolicyName(permission);
    }
}
