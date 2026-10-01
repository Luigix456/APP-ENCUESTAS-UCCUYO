using System.Reflection;
using AcademicSurveySystem.Api.Controllers.Surveys;
using AcademicSurveySystem.Application.Common.Results;
using AcademicSurveySystem.Application.Surveys;
using AcademicSurveySystem.Application.Surveys.Dtos;
using AcademicSurveySystem.Application.Surveys.Requests;
using AcademicSurveySystem.Domain.Surveys.Entities;
using AcademicSurveySystem.Domain.Surveys.Enums;
using AcademicSurveySystem.Infrastructure.Surveys;
using Microsoft.AspNetCore.Mvc;

namespace AcademicSurveySystem.UnitTests.Surveys;

public sealed class SurveyTemplateDetailTests
{
    private static readonly DateTimeOffset CreatedAtUtc =
        new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static readonly DateTimeOffset UpdatedAtUtc =
        new(2026, 1, 2, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void ServiceDetailMapping_ReturnsSurveyDetailDtoWithNestedCollections()
    {
        var createdByUserId = Guid.NewGuid();
        var survey = new Survey(
            Guid.NewGuid(),
            createdByUserId,
            "Encuesta docente",
            "Descripcion",
            SurveyTarget.Student,
            CreatedAtUtc);

        var sectionId = Guid.NewGuid();
        var section = new SurveySection(
            sectionId,
            survey.Id,
            "Desempeno docente",
            "Preguntas sobre claridad",
            order: 1,
            CreatedAtUtc);

        var matrixQuestion = new SurveyQuestion(
            Guid.NewGuid(),
            section.Id,
            "Evalua la claridad del docente",
            SurveyQuestionType.MatrixSingleChoice,
            isRequired: true,
            allowsComment: true,
            allowsOtherOption: false,
            order: 2,
            CreatedAtUtc);

        matrixQuestion.AddOption(
            new SurveyQuestionOption(Guid.NewGuid(), matrixQuestion.Id, "Bueno", "good", order: 2, CreatedAtUtc),
            UpdatedAtUtc);
        matrixQuestion.AddOption(
            new SurveyQuestionOption(Guid.NewGuid(), matrixQuestion.Id, "Excelente", "excellent", order: 1, CreatedAtUtc),
            UpdatedAtUtc);
        matrixQuestion.AddMatrixRow(
            new SurveyMatrixRow(Guid.NewGuid(), matrixQuestion.Id, "Organizacion", order: 2, CreatedAtUtc),
            UpdatedAtUtc);
        matrixQuestion.AddMatrixRow(
            new SurveyMatrixRow(Guid.NewGuid(), matrixQuestion.Id, "Claridad", order: 1, CreatedAtUtc),
            UpdatedAtUtc);

        var textQuestion = new SurveyQuestion(
            Guid.NewGuid(),
            section.Id,
            "Comentario general",
            SurveyQuestionType.ShortText,
            isRequired: false,
            allowsComment: false,
            allowsOtherOption: false,
            order: 1,
            CreatedAtUtc);

        var ratingQuestion = new SurveyQuestion(
            Guid.NewGuid(),
            section.Id,
            "Califique",
            SurveyQuestionType.RatingScale,
            isRequired: true,
            allowsComment: false,
            allowsOtherOption: false,
            order: 3,
            CreatedAtUtc,
            ratingMin: 1,
            ratingMax: 5);

        section.AddQuestion(matrixQuestion, UpdatedAtUtc);
        section.AddQuestion(textQuestion, UpdatedAtUtc);
        section.AddQuestion(ratingQuestion, UpdatedAtUtc);
        survey.AddSection(section, UpdatedAtUtc);

        var dto = MapDetail(survey);

        Assert.Equal(survey.Id, dto.Id);
        Assert.Equal(createdByUserId, dto.CreatedByUserId);
        var dtoSection = Assert.Single(dto.Sections);
        Assert.Equal(sectionId, dtoSection.Id);

        Assert.Equal(
            [textQuestion.Id, matrixQuestion.Id, ratingQuestion.Id],
            dtoSection.Questions.Select(question => question.Id).ToArray());

        var dtoMatrixQuestion = dtoSection.Questions.Single(question => question.Id == matrixQuestion.Id);
        Assert.Equal(["Excelente", "Bueno"], dtoMatrixQuestion.Options.Select(option => option.Text).ToArray());
        Assert.Equal(["Claridad", "Organizacion"], dtoMatrixQuestion.MatrixRows.Select(row => row.Text).ToArray());
        var dtoRatingQuestion = dtoSection.Questions.Single(question => question.Id == ratingQuestion.Id);
        Assert.Equal(1, dtoRatingQuestion.RatingMin);
        Assert.Equal(5, dtoRatingQuestion.RatingMax);
    }

    [Fact]
    public async Task ControllerGetById_ReturnsSurveyDetailDto()
    {
        var detail = new SurveyDetailDto(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Encuesta docente",
            "Descripcion",
            "Student",
            "Draft",
            IsAnonymous: true,
            IsActive: true,
            [
                new SurveySectionDto(
                    Guid.NewGuid(),
                    "Desempeno docente",
                    "Preguntas sobre claridad",
                    Order: 1,
                    IsActive: true,
                    [
                        new SurveyQuestionDto(
                            Guid.NewGuid(),
                            "Evalua la claridad del docente",
                            "SingleChoice",
                            IsRequired: true,
                            AllowsComment: true,
                            AllowsOtherOption: false,
                            Order: 1,
                            IsActive: true,
                            [
                                new SurveyQuestionOptionDto(
                                    Guid.NewGuid(),
                                    "Excelente",
                                    "excellent",
                                    Order: 1,
                                    IsActive: true)
                            ],
                            [
                                new SurveyMatrixRowDto(
                                    Guid.NewGuid(),
                                    "Claridad",
                                    Order: 1,
                                    IsActive: true)
                            ])
                    ])
            ],
            CreatedAtUtc,
            UpdatedAtUtc);

        var controller = new SurveysController(new FakeSurveyTemplateService
        {
            SurveyByIdResult = ApplicationResult<SurveyDetailDto>.Success(detail)
        });

        var result = await controller.GetById(detail.Id, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var dto = Assert.IsType<SurveyDetailDto>(ok.Value);
        Assert.Equal(detail.CreatedByUserId, dto.CreatedByUserId);
        Assert.Single(dto.Sections);
        Assert.NotNull(dto.Sections.Single().Questions);
    }

    [Fact]
    public async Task ControllerGetAll_StillReturnsSurveySummaryDto()
    {
        var summary = new SurveySummaryDto(
            Guid.NewGuid(),
            "Encuesta docente",
            "Descripcion",
            "Student",
            "Draft",
            IsAnonymous: true,
            IsActive: true,
            SectionCount: 1,
            QuestionCount: 0,
            CreatedAtUtc,
            UpdatedAtUtc);

        var controller = new SurveysController(new FakeSurveyTemplateService
        {
            SurveysResult = ApplicationResult<IReadOnlyCollection<SurveySummaryDto>>.Success([summary])
        });

        var result = await controller.GetAll(cancellationToken: CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var summaries = Assert.IsAssignableFrom<IReadOnlyCollection<SurveySummaryDto>>(ok.Value);
        Assert.IsType<SurveySummaryDto>(Assert.Single(summaries));
    }

    [Fact]
    public async Task ControllerUpdateOption_ReturnsSurveyDetailDto()
    {
        var surveyId = Guid.NewGuid();
        var sectionId = Guid.NewGuid();
        var questionId = Guid.NewGuid();
        var optionId = Guid.NewGuid();
        var detail = CreateDetailWithOption(surveyId, sectionId, questionId, optionId);

        var controller = new SurveysController(new FakeSurveyTemplateService
        {
            UpdateOptionResult = ApplicationResult<SurveyDetailDto>.Success(detail)
        });

        var result = await controller.UpdateOption(
            surveyId,
            sectionId,
            questionId,
            optionId,
            new UpdateSurveyQuestionOptionRequest("Excelente", "excellent", 1),
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var dto = Assert.IsType<SurveyDetailDto>(ok.Value);
        Assert.Equal(optionId, dto.Sections.Single().Questions.Single().Options.Single().Id);
    }

    [Fact]
    public async Task ControllerUpdateMatrixRow_ReturnsSurveyDetailDto()
    {
        var surveyId = Guid.NewGuid();
        var sectionId = Guid.NewGuid();
        var questionId = Guid.NewGuid();
        var rowId = Guid.NewGuid();
        var detail = CreateDetailWithMatrixRow(surveyId, sectionId, questionId, rowId);

        var controller = new SurveysController(new FakeSurveyTemplateService
        {
            UpdateMatrixRowResult = ApplicationResult<SurveyDetailDto>.Success(detail)
        });

        var result = await controller.UpdateMatrixRow(
            surveyId,
            sectionId,
            questionId,
            rowId,
            new UpdateSurveyMatrixRowRequest("Claridad", 1),
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var dto = Assert.IsType<SurveyDetailDto>(ok.Value);
        Assert.Equal(rowId, dto.Sections.Single().Questions.Single().MatrixRows.Single().Id);
    }

    private static SurveyDetailDto MapDetail(Survey survey)
    {
        var mapDetail = typeof(SurveyTemplateService)
            .GetMethod("MapDetail", BindingFlags.NonPublic | BindingFlags.Static);

        Assert.NotNull(mapDetail);
        return Assert.IsType<SurveyDetailDto>(mapDetail.Invoke(null, [survey]));
    }

    private static SurveyDetailDto CreateDetailWithOption(
        Guid surveyId,
        Guid sectionId,
        Guid questionId,
        Guid optionId)
    {
        return new SurveyDetailDto(
            surveyId,
            Guid.NewGuid(),
            "Encuesta docente",
            null,
            "Student",
            "Draft",
            IsAnonymous: true,
            IsActive: true,
            [
                new SurveySectionDto(
                    sectionId,
                    "Seccion",
                    null,
                    Order: 1,
                    IsActive: true,
                    [
                        new SurveyQuestionDto(
                            questionId,
                            "Pregunta",
                            "SingleChoice",
                            IsRequired: true,
                            AllowsComment: false,
                            AllowsOtherOption: false,
                            Order: 1,
                            IsActive: true,
                            [new SurveyQuestionOptionDto(optionId, "Excelente", "excellent", 1, IsActive: true)],
                            [])
                    ])
            ],
            CreatedAtUtc,
            UpdatedAtUtc);
    }

    private static SurveyDetailDto CreateDetailWithMatrixRow(
        Guid surveyId,
        Guid sectionId,
        Guid questionId,
        Guid rowId)
    {
        return new SurveyDetailDto(
            surveyId,
            Guid.NewGuid(),
            "Encuesta docente",
            null,
            "Student",
            "Draft",
            IsAnonymous: true,
            IsActive: true,
            [
                new SurveySectionDto(
                    sectionId,
                    "Seccion",
                    null,
                    Order: 1,
                    IsActive: true,
                    [
                        new SurveyQuestionDto(
                            questionId,
                            "Pregunta",
                            "MatrixSingleChoice",
                            IsRequired: true,
                            AllowsComment: false,
                            AllowsOtherOption: false,
                            Order: 1,
                            IsActive: true,
                            [],
                            [new SurveyMatrixRowDto(rowId, "Claridad", 1, IsActive: true)])
                    ])
            ],
            CreatedAtUtc,
            UpdatedAtUtc);
    }

    private sealed class FakeSurveyTemplateService : ISurveyTemplateService
    {
        public ApplicationResult<IReadOnlyCollection<SurveySummaryDto>> SurveysResult { get; init; } =
            ApplicationResult<IReadOnlyCollection<SurveySummaryDto>>.Success([]);

        public ApplicationResult<SurveyDetailDto> SurveyByIdResult { get; init; } =
            ApplicationResult<SurveyDetailDto>.NotFound("Survey was not found.");

        public ApplicationResult<SurveyDetailDto> UpdateOptionResult { get; init; } =
            ApplicationResult<SurveyDetailDto>.NotFound("Survey question option was not found.");

        public ApplicationResult<SurveyDetailDto> UpdateMatrixRowResult { get; init; } =
            ApplicationResult<SurveyDetailDto>.NotFound("Survey matrix row was not found.");

        public Task<ApplicationResult<IReadOnlyCollection<SurveySummaryDto>>> GetSurveysAsync(
            bool includeInactive,
            string? status,
            string? target,
            CancellationToken cancellationToken) =>
            Task.FromResult(SurveysResult);

        public Task<ApplicationResult<SurveyDetailDto>> GetSurveyByIdAsync(
            Guid id,
            CancellationToken cancellationToken) =>
            Task.FromResult(SurveyByIdResult);

        public Task<ApplicationResult<SurveyDetailDto>> CreateSurveyAsync(
            Guid createdByUserId,
            CreateSurveyRequest request,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<ApplicationResult<SurveyEditableVersionDto>> GetOrCreateEditableVersionAsync(
            Guid surveyId,
            Guid currentUserId,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<ApplicationResult> UpdateSurveyAsync(
            Guid id,
            UpdateSurveyRequest request,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<ApplicationResult> PublishSurveyAsync(Guid id, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<ApplicationResult> ArchiveSurveyAsync(Guid id, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<ApplicationResult> ActivateSurveyAsync(Guid id, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<ApplicationResult> DeactivateSurveyAsync(Guid id, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<ApplicationResult<SurveyDetailDto>> AddSectionAsync(
            Guid surveyId,
            CreateSurveySectionRequest request,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<ApplicationResult> UpdateSectionAsync(
            Guid surveyId,
            Guid sectionId,
            UpdateSurveySectionRequest request,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<ApplicationResult> ActivateSectionAsync(
            Guid surveyId,
            Guid sectionId,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<ApplicationResult> DeactivateSectionAsync(
            Guid surveyId,
            Guid sectionId,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<ApplicationResult<SurveyDetailDto>> AddQuestionAsync(
            Guid surveyId,
            Guid sectionId,
            CreateSurveyQuestionRequest request,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<ApplicationResult> UpdateQuestionAsync(
            Guid surveyId,
            Guid sectionId,
            Guid questionId,
            UpdateSurveyQuestionRequest request,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<ApplicationResult> ActivateQuestionAsync(
            Guid surveyId,
            Guid sectionId,
            Guid questionId,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<ApplicationResult> DeactivateQuestionAsync(
            Guid surveyId,
            Guid sectionId,
            Guid questionId,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<ApplicationResult<SurveyDetailDto>> AddOptionAsync(
            Guid surveyId,
            Guid sectionId,
            Guid questionId,
            CreateSurveyQuestionOptionRequest request,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<ApplicationResult<SurveyDetailDto>> UpdateOptionAsync(
            Guid surveyId,
            Guid sectionId,
            Guid questionId,
            Guid optionId,
            UpdateSurveyQuestionOptionRequest request,
            CancellationToken cancellationToken) =>
            Task.FromResult(UpdateOptionResult);

        public Task<ApplicationResult> ActivateOptionAsync(
            Guid surveyId,
            Guid sectionId,
            Guid questionId,
            Guid optionId,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<ApplicationResult> DeactivateOptionAsync(
            Guid surveyId,
            Guid sectionId,
            Guid questionId,
            Guid optionId,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<ApplicationResult<SurveyDetailDto>> AddMatrixRowAsync(
            Guid surveyId,
            Guid sectionId,
            Guid questionId,
            CreateSurveyMatrixRowRequest request,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<ApplicationResult<SurveyDetailDto>> UpdateMatrixRowAsync(
            Guid surveyId,
            Guid sectionId,
            Guid questionId,
            Guid rowId,
            UpdateSurveyMatrixRowRequest request,
            CancellationToken cancellationToken) =>
            Task.FromResult(UpdateMatrixRowResult);

        public Task<ApplicationResult> ActivateMatrixRowAsync(
            Guid surveyId,
            Guid sectionId,
            Guid questionId,
            Guid rowId,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<ApplicationResult> DeactivateMatrixRowAsync(
            Guid surveyId,
            Guid sectionId,
            Guid questionId,
            Guid rowId,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }
}
