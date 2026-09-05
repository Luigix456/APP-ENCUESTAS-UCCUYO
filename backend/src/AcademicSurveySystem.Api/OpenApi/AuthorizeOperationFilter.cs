using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace AcademicSurveySystem.Api.OpenApi;

public sealed class AuthorizeOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var controllerActionDescriptor = context.ApiDescription.ActionDescriptor as ControllerActionDescriptor;

        var endpointMetadata = context.ApiDescription.ActionDescriptor.EndpointMetadata;

        var hasAllowAnonymous = endpointMetadata.OfType<IAllowAnonymous>().Any()
            || controllerActionDescriptor?.ControllerTypeInfo
                .GetCustomAttributes(typeof(AllowAnonymousAttribute), inherit: true)
                .Any() == true
            || controllerActionDescriptor?.MethodInfo
                .GetCustomAttributes(typeof(AllowAnonymousAttribute), inherit: true)
                .Any() == true;

        if (hasAllowAnonymous)
        {
            return;
        }

        var hasAuthorize = endpointMetadata.OfType<IAuthorizeData>().Any()
            || controllerActionDescriptor?.ControllerTypeInfo
                .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
                .Any() == true
            || controllerActionDescriptor?.MethodInfo
                .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
                .Any() == true;

        if (!hasAuthorize)
        {
            return;
        }

        operation.Security ??= [];
        operation.Security.Add(new OpenApiSecurityRequirement
        {
            [
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                }
            ] = []
        });
    }
}
