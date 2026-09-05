using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace AcademicSurveySystem.Api.OpenApi;

public sealed class HealthCheckDocumentFilter : IDocumentFilter
{
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        swaggerDoc.Paths["/api/health"] = new OpenApiPathItem
        {
            Operations =
            {
                [OperationType.Get] = new OpenApiOperation
                {
                    Tags = [new OpenApiTag { Name = "Health" }],
                    Summary = "Consulta el estado de salud de la API.",
                    Description = "Devuelve el estado general del servicio y de sus checks de infraestructura.",
                    Responses = new OpenApiResponses
                    {
                        ["200"] = new OpenApiResponse { Description = "La API y sus checks están saludables." },
                        ["503"] = new OpenApiResponse { Description = "La API responde, pero uno o más checks no están saludables." }
                    }
                }
            }
        };
    }
}
