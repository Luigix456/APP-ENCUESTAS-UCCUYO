using System.Text.Json;
using AcademicSurveySystem.Infrastructure;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

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

app.Run();

public partial class Program
{
}
