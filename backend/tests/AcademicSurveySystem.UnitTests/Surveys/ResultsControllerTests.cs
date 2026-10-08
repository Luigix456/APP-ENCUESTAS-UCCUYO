using System.Security.Claims;
using AcademicSurveySystem.Api.Controllers;
using AcademicSurveySystem.Application.Common.Results;
using AcademicSurveySystem.Application.Surveys.Results;
using AcademicSurveySystem.Infrastructure.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AcademicSurveySystem.UnitTests.Surveys;

public sealed class ResultsControllerTests
{
    [Fact]
    public async Task Summary_WithReadAllPermission_ReturnsOk()
    {
        var summary = CreateSummary();
        var service = new FakeSurveyResultsService
        {
            SummaryResult = ApplicationResult<SurveyResultsSummaryDto>.Success(summary)
        };
        var controller = CreateController(
            service,
            new FakeResultsAccessService(),
            "results.read_all");

        var result = await controller.GetSurveyAssignmentSummary(
            summary.SurveyAssignmentId,
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(summary, ok.Value);
    }

    [Fact]
    public async Task Summary_WithReadCareerPermissionAndAssociatedCareer_ReturnsOk()
    {
        var summary = CreateSummary();
        var service = new FakeSurveyResultsService
        {
            SummaryResult = ApplicationResult<SurveyResultsSummaryDto>.Success(summary)
        };
        var controller = CreateController(
            service,
            new FakeResultsAccessService(),
            "results.read_career");

        var result = await controller.GetSurveyAssignmentSummary(
            summary.SurveyAssignmentId,
            CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Summary_WithReadCareerPermissionAndNoAssociatedCareer_ReturnsForbidden()
    {
        var controller = CreateController(
            new FakeSurveyResultsService(),
            new FakeResultsAccessService(ResultsAccessDecision.Forbidden()),
            "results.read_career");

        var result = await controller.GetSurveyAssignmentSummary(
            Guid.NewGuid(),
            CancellationToken.None);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task Summary_WithoutResultsPermission_ReturnsForbidden()
    {
        var controller = CreateController(
            new FakeSurveyResultsService(),
            new FakeResultsAccessService(ResultsAccessDecision.Forbidden()));

        var result = await controller.GetSurveyAssignmentSummary(
            Guid.NewGuid(),
            CancellationToken.None);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task GetSurveyAssignments_WithReadCareerPermission_ReturnsOk()
    {
        var assignments = new[]
        {
            CreateAssignmentResult()
        };
        var service = new FakeSurveyResultsService
        {
            AssignmentResults = ApplicationResult<IReadOnlyCollection<SurveyAssignmentResultListItemDto>>.Success(
                assignments)
        };
        var controller = CreateController(
            service,
            new FakeResultsAccessService(),
            "results.read_career");

        var result = await controller.GetSurveyAssignments(cancellationToken: CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(assignments, ok.Value);
    }

    [Fact]
    public async Task GetSurveyAssignments_WithoutResultsPermission_ReturnsForbidden()
    {
        var controller = CreateController(
            new FakeSurveyResultsService(),
            new FakeResultsAccessService());

        var result = await controller.GetSurveyAssignments(cancellationToken: CancellationToken.None);

        Assert.IsType<ForbidResult>(result);
    }

    private static ResultsController CreateController(
        ISurveyResultsService service,
        IResultsAccessService accessService,
        string? permission = null)
    {
        var claims = new List<Claim>
        {
            new(JwtTokenGenerator.NameIdentifierClaimType, Guid.NewGuid().ToString())
        };

        if (permission is not null)
        {
            claims.Add(new Claim(JwtTokenGenerator.PermissionClaimType, permission));
        }

        var identity = new ClaimsIdentity(claims, authenticationType: "Test");
        var principal = new ClaimsPrincipal(identity);

        return new ResultsController(service, accessService)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = principal
                }
            }
        };
    }

    private static SurveyResultsSummaryDto CreateSummary()
    {
        return new SurveyResultsSummaryDto(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Encuesta",
            Guid.NewGuid(),
            "Carrera",
            Guid.NewGuid(),
            "Materia",
            Guid.NewGuid(),
            2026,
            "FirstSemester",
            Guid.NewGuid(),
            "Docente",
            1,
            1,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow);
    }

    private static SurveyAssignmentResultListItemDto CreateAssignmentResult()
    {
        return new SurveyAssignmentResultListItemDto(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Encuesta",
            Guid.NewGuid(),
            "Carrera",
            Guid.NewGuid(),
            "Materia",
            Guid.NewGuid(),
            2026,
            "Annual",
            Guid.NewGuid(),
            "Docente",
            "Titular",
            IsActive: true,
            TotalSessions: 1,
            TotalResponses: 1,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow);
    }

    private sealed class FakeSurveyResultsService : ISurveyResultsService
    {
        public ApplicationResult<SurveyResultsSummaryDto> SummaryResult { get; init; } =
            ApplicationResult<SurveyResultsSummaryDto>.NotFound("Not found.");

        public ApplicationResult<IReadOnlyCollection<SurveyAssignmentResultListItemDto>> AssignmentResults { get; init; } =
            ApplicationResult<IReadOnlyCollection<SurveyAssignmentResultListItemDto>>.Success([]);

        public Task<ApplicationResult<IReadOnlyCollection<SurveyAssignmentResultListItemDto>>> GetSurveyAssignmentResultsAsync(
            ResultsAccessScope accessScope,
            SurveyAssignmentResultsFilter filter,
            CancellationToken cancellationToken) =>
            Task.FromResult(AssignmentResults);

        public Task<ApplicationResult<SurveyResultsSummaryDto>> GetSurveyAssignmentSummaryAsync(
            Guid surveyAssignmentId,
            CancellationToken cancellationToken) =>
            Task.FromResult(SummaryResult);

        public Task<ApplicationResult<IReadOnlyCollection<SurveyQuestionResultsDto>>> GetSurveyAssignmentQuestionResultsAsync(
            Guid surveyAssignmentId,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<ApplicationResult<SurveyQuestionResultsDto>> GetSurveyAssignmentQuestionResultAsync(
            Guid surveyAssignmentId,
            Guid questionId,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<ApplicationResult<SurveyResultsSummaryDto>> GetSurveySessionSummaryAsync(
            Guid surveySessionId,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<ApplicationResult<SurveyHistoryDto>> GetSurveyHistoryAsync(
            ResultsAccessScope accessScope,
            SurveyHistoryQuery query,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<ApplicationResult<QuestionHistoryDto>> GetQuestionHistoryAsync(
            ResultsAccessScope accessScope,
            Guid questionLineageId,
            SurveyHistoryQuery query,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    private sealed class FakeResultsAccessService : IResultsAccessService
    {
        private readonly ResultsAccessDecision _decision;

        public FakeResultsAccessService()
            : this(ResultsAccessDecision.Allowed())
        {
        }

        public FakeResultsAccessService(ResultsAccessDecision decision)
        {
            _decision = decision;
        }

        public Task<ResultsAccessScope> GetDiscoveryScopeAsync(
            Guid userId,
            bool hasReadAll,
            bool hasReadCareer,
            CancellationToken cancellationToken)
        {
            if (hasReadAll)
            {
                return Task.FromResult(ResultsAccessScope.All());
            }

            if (hasReadCareer)
            {
                return Task.FromResult(ResultsAccessScope.Career([Guid.NewGuid()]));
            }

            return Task.FromResult(ResultsAccessScope.Forbidden());
        }

        public Task<ResultsAccessDecision> AuthorizeSurveyAssignmentAsync(
            Guid userId,
            bool hasReadAll,
            bool hasReadCareer,
            Guid surveyAssignmentId,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(hasReadAll || hasReadCareer
                ? _decision
                : ResultsAccessDecision.Forbidden());
        }

        public Task<ResultsAccessDecision> AuthorizeSurveySessionAsync(
            Guid userId,
            bool hasReadAll,
            bool hasReadCareer,
            Guid surveySessionId,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(hasReadAll || hasReadCareer
                ? _decision
                : ResultsAccessDecision.Forbidden());
        }
    }
}
