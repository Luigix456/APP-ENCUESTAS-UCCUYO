using AcademicSurveySystem.Application.Common.Results;
using AcademicSurveySystem.Application.Surveys;
using AcademicSurveySystem.Application.Surveys.Dtos;
using AcademicSurveySystem.Application.Surveys.Requests;
using AcademicSurveySystem.Domain.Common;
using AcademicSurveySystem.Domain.Surveys.Entities;
using AcademicSurveySystem.Domain.Surveys.Enums;
using AcademicSurveySystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace AcademicSurveySystem.Infrastructure.Surveys;

public sealed class SurveyTemplateService : ISurveyTemplateService
{
    private readonly ApplicationDbContext _dbContext;

    public SurveyTemplateService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ApplicationResult<IReadOnlyCollection<SurveySummaryDto>>> GetSurveysAsync(
        bool includeInactive,
        string? status,
        string? target,
        CancellationToken cancellationToken)
    {
        if (!TryParseOptionalEnum<SurveyStatus>(status, "Survey.StatusInvalid", out var parsedStatus, out var statusError))
        {
            return ApplicationResult<IReadOnlyCollection<SurveySummaryDto>>.Validation([statusError]);
        }

        if (!TryParseOptionalEnum<SurveyTarget>(target, "Survey.TargetInvalid", out var parsedTarget, out var targetError))
        {
            return ApplicationResult<IReadOnlyCollection<SurveySummaryDto>>.Validation([targetError]);
        }

        var surveys = await _dbContext.Surveys
            .AsNoTracking()
            .Where(survey => includeInactive || survey.IsActive)
            .Where(survey => parsedStatus == null || survey.Status == parsedStatus.Value)
            .Where(survey => parsedTarget == null || survey.Target == parsedTarget.Value)
            .OrderByDescending(survey => survey.UpdatedAtUtc)
            .Select(survey => new SurveySummaryDto(
                survey.Id,
                survey.Title,
                survey.Description,
                survey.Target.ToString(),
                survey.Status.ToString(),
                survey.IsAnonymous,
                survey.IsActive,
                survey.Sections.Count,
                survey.Sections.SelectMany(section => section.Questions).Count(),
                survey.CreatedAtUtc,
                survey.UpdatedAtUtc))
            .ToArrayAsync(cancellationToken);

        return ApplicationResult<IReadOnlyCollection<SurveySummaryDto>>.Success(surveys);
    }

    public async Task<ApplicationResult<SurveyDetailDto>> GetSurveyByIdAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var survey = await LoadSurveyForReadAsync(id, cancellationToken);

        return survey is null
            ? ApplicationResult<SurveyDetailDto>.NotFound("Survey was not found.")
            : ApplicationResult<SurveyDetailDto>.Success(MapDetail(survey));
    }

    public async Task<ApplicationResult<SurveyDetailDto>> CreateSurveyAsync(
        Guid createdByUserId,
        CreateSurveyRequest request,
        CancellationToken cancellationToken)
    {
        var validationErrors = request.Validate();

        if (validationErrors.Count > 0)
        {
            return ApplicationResult<SurveyDetailDto>.Validation(validationErrors);
        }

        if (!Enum.TryParse<SurveyTarget>(request.Target!.Trim(), ignoreCase: true, out var target))
        {
            return ApplicationResult<SurveyDetailDto>.Validation([
                new ApplicationError("Survey.TargetInvalid", "Target is invalid.")
            ]);
        }

        try
        {
            var survey = new Survey(
                Guid.NewGuid(),
                createdByUserId,
                request.Title!,
                request.Description,
                target,
                DateTimeOffset.UtcNow);

            if (!request.IsAnonymous)
            {
                survey.Update(
                    request.Title!,
                    request.Description,
                    target,
                    isAnonymous: false,
                    DateTimeOffset.UtcNow);
            }

            _dbContext.Surveys.Add(survey);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return ApplicationResult<SurveyDetailDto>.Success(MapDetail(survey));
        }
        catch (DomainException exception)
        {
            return ApplicationResult<SurveyDetailDto>.Validation([
                new ApplicationError("Survey.Validation", exception.Message)
            ]);
        }
        catch (DbUpdateException)
        {
            return ApplicationResult<SurveyDetailDto>.Failure("The survey could not be saved.");
        }
    }

    public async Task<ApplicationResult> UpdateSurveyAsync(
        Guid id,
        UpdateSurveyRequest request,
        CancellationToken cancellationToken)
    {
        var validationErrors = request.Validate();

        if (validationErrors.Count > 0)
        {
            return ApplicationResult.Validation(validationErrors);
        }

        var survey = await _dbContext.Surveys.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (survey is null)
        {
            return ApplicationResult.NotFound("Survey was not found.");
        }

        if (!Enum.TryParse<SurveyTarget>(request.Target!.Trim(), ignoreCase: true, out var target))
        {
            return ApplicationResult.Validation([
                new ApplicationError("Survey.TargetInvalid", "Target is invalid.")
            ]);
        }

        try
        {
            survey.Update(
                request.Title!,
                request.Description,
                target,
                request.IsAnonymous,
                DateTimeOffset.UtcNow);

            await _dbContext.SaveChangesAsync(cancellationToken);
            return ApplicationResult.Success();
        }
        catch (DomainException exception)
        {
            return ApplicationResult.Validation([new ApplicationError("Survey.Validation", exception.Message)]);
        }
        catch (DbUpdateException)
        {
            return ApplicationResult.Failure("The survey could not be saved.");
        }
    }

    public async Task<ApplicationResult> PublishSurveyAsync(Guid id, CancellationToken cancellationToken)
    {
        var survey = await LoadSurveyAggregateAsync(id, cancellationToken);

        if (survey is null)
        {
            return ApplicationResult.NotFound("Survey was not found.");
        }

        try
        {
            survey.Publish(DateTimeOffset.UtcNow);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return ApplicationResult.Success();
        }
        catch (DomainException exception)
        {
            return ApplicationResult.Validation([new ApplicationError("Survey.PublishInvalid", exception.Message)]);
        }
        catch (DbUpdateException)
        {
            return ApplicationResult.Failure("The survey could not be saved.");
        }
    }

    public Task<ApplicationResult> ArchiveSurveyAsync(Guid id, CancellationToken cancellationToken) =>
        ChangeSurveyStateAsync(id, stateChange: (survey, now) => survey.Archive(now), cancellationToken);

    public Task<ApplicationResult> ActivateSurveyAsync(Guid id, CancellationToken cancellationToken) =>
        ChangeSurveyStateAsync(id, stateChange: (survey, now) => survey.Activate(now), cancellationToken);

    public Task<ApplicationResult> DeactivateSurveyAsync(Guid id, CancellationToken cancellationToken) =>
        ChangeSurveyStateAsync(id, stateChange: (survey, now) => survey.Deactivate(now), cancellationToken);

    public async Task<ApplicationResult<SurveyDetailDto>> AddSectionAsync(
        Guid surveyId,
        CreateSurveySectionRequest request,
        CancellationToken cancellationToken)
    {
        var validationErrors = request.Validate();

        if (validationErrors.Count > 0)
        {
            return ApplicationResult<SurveyDetailDto>.Validation(validationErrors);
        }

        var survey = await LoadSurveyAggregateAsync(surveyId, cancellationToken);

        if (survey is null)
        {
            return ApplicationResult<SurveyDetailDto>.NotFound("Survey was not found.");
        }

        var order = request.Order!.Value;

        if (survey.Sections.Any(section => section.Order == order))
        {
            return ApplicationResult<SurveyDetailDto>.Conflict("A section with the same order already exists.");
        }

        try
        {
            var section = new SurveySection(
                Guid.NewGuid(),
                surveyId,
                request.Title!,
                request.Description,
                order,
                DateTimeOffset.UtcNow);

            survey.AddSection(section, DateTimeOffset.UtcNow);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return ApplicationResult<SurveyDetailDto>.Success(MapDetail(survey));
        }
        catch (DomainException exception)
        {
            return ApplicationResult<SurveyDetailDto>.Validation([
                new ApplicationError("SurveySection.Validation", exception.Message)
            ]);
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
        {
            return ApplicationResult<SurveyDetailDto>.Conflict("A section with the same order already exists.");
        }
        catch (DbUpdateException)
        {
            return ApplicationResult<SurveyDetailDto>.Failure("The section could not be saved.");
        }
    }

    public async Task<ApplicationResult> UpdateSectionAsync(
        Guid surveyId,
        Guid sectionId,
        UpdateSurveySectionRequest request,
        CancellationToken cancellationToken)
    {
        var validationErrors = request.Validate();

        if (validationErrors.Count > 0)
        {
            return ApplicationResult.Validation(validationErrors);
        }

        var survey = await LoadSurveyAggregateAsync(surveyId, cancellationToken);
        var sectionResult = FindSection(survey, sectionId);

        if (sectionResult.Status != ApplicationResultStatus.Success)
        {
            return sectionResult;
        }

        var section = sectionResult.Value!;

        var order = request.Order!.Value;

        if (survey!.Sections.Any(item => item.Id != sectionId && item.Order == order))
        {
            return ApplicationResult.Conflict("A section with the same order already exists.");
        }

        try
        {
            section.Update(request.Title!, request.Description, order, DateTimeOffset.UtcNow);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return ApplicationResult.Success();
        }
        catch (DomainException exception)
        {
            return ApplicationResult.Validation([new ApplicationError("SurveySection.Validation", exception.Message)]);
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
        {
            return ApplicationResult.Conflict("A section with the same order already exists.");
        }
        catch (DbUpdateException)
        {
            return ApplicationResult.Failure("The section could not be saved.");
        }
    }

    public Task<ApplicationResult> ActivateSectionAsync(
        Guid surveyId,
        Guid sectionId,
        CancellationToken cancellationToken) =>
        ChangeSectionStateAsync(surveyId, sectionId, activate: true, cancellationToken);

    public Task<ApplicationResult> DeactivateSectionAsync(
        Guid surveyId,
        Guid sectionId,
        CancellationToken cancellationToken) =>
        ChangeSectionStateAsync(surveyId, sectionId, activate: false, cancellationToken);

    public async Task<ApplicationResult<SurveyDetailDto>> AddQuestionAsync(
        Guid surveyId,
        Guid sectionId,
        CreateSurveyQuestionRequest request,
        CancellationToken cancellationToken)
    {
        var validationErrors = request.Validate();

        if (validationErrors.Count > 0)
        {
            return ApplicationResult<SurveyDetailDto>.Validation(validationErrors);
        }

        var survey = await LoadSurveyAggregateAsync(surveyId, cancellationToken);
        var sectionResult = FindSection(survey, sectionId);

        if (sectionResult.Status != ApplicationResultStatus.Success)
        {
            return ApplicationResult<SurveyDetailDto>.NotFound(sectionResult.Errors.First().Message);
        }

        var section = sectionResult.Value!;

        var order = request.Order!.Value;

        if (section.Questions.Any(question => question.Order == order))
        {
            return ApplicationResult<SurveyDetailDto>.Conflict("A question with the same order already exists.");
        }

        if (!Enum.TryParse<SurveyQuestionType>(request.Type!.Trim(), ignoreCase: true, out var type))
        {
            return ApplicationResult<SurveyDetailDto>.Validation([
                new ApplicationError("SurveyQuestion.TypeInvalid", "Type is invalid.")
            ]);
        }

        try
        {
            var question = new SurveyQuestion(
                Guid.NewGuid(),
                sectionId,
                request.Text!,
                type,
                request.IsRequired,
                request.AllowsComment,
                request.AllowsOtherOption,
                order,
                DateTimeOffset.UtcNow);

            section.AddQuestion(question, DateTimeOffset.UtcNow);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return ApplicationResult<SurveyDetailDto>.Success(MapDetail(survey!));
        }
        catch (DomainException exception)
        {
            return ApplicationResult<SurveyDetailDto>.Validation([
                new ApplicationError("SurveyQuestion.Validation", exception.Message)
            ]);
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
        {
            return ApplicationResult<SurveyDetailDto>.Conflict("A question with the same order already exists.");
        }
        catch (DbUpdateException)
        {
            return ApplicationResult<SurveyDetailDto>.Failure("The question could not be saved.");
        }
    }

    public async Task<ApplicationResult> UpdateQuestionAsync(
        Guid surveyId,
        Guid sectionId,
        Guid questionId,
        UpdateSurveyQuestionRequest request,
        CancellationToken cancellationToken)
    {
        var validationErrors = request.Validate();

        if (validationErrors.Count > 0)
        {
            return ApplicationResult.Validation(validationErrors);
        }

        var survey = await LoadSurveyAggregateAsync(surveyId, cancellationToken);
        var questionResult = FindQuestion(survey, sectionId, questionId);

        if (questionResult.Status != ApplicationResultStatus.Success)
        {
            return questionResult;
        }

        var section = survey!.Sections.Single(item => item.Id == sectionId);
        var question = questionResult.Value!;

        var order = request.Order!.Value;

        if (section.Questions.Any(item => item.Id != questionId && item.Order == order))
        {
            return ApplicationResult.Conflict("A question with the same order already exists.");
        }

        if (!Enum.TryParse<SurveyQuestionType>(request.Type!.Trim(), ignoreCase: true, out var type))
        {
            return ApplicationResult.Validation([
                new ApplicationError("SurveyQuestion.TypeInvalid", "Type is invalid.")
            ]);
        }

        try
        {
            question.Update(
                request.Text!,
                type,
                request.IsRequired,
                request.AllowsComment,
                request.AllowsOtherOption,
                order,
                DateTimeOffset.UtcNow);

            await _dbContext.SaveChangesAsync(cancellationToken);
            return ApplicationResult.Success();
        }
        catch (DomainException exception)
        {
            return ApplicationResult.Validation([new ApplicationError("SurveyQuestion.Validation", exception.Message)]);
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
        {
            return ApplicationResult.Conflict("A question with the same order already exists.");
        }
        catch (DbUpdateException)
        {
            return ApplicationResult.Failure("The question could not be saved.");
        }
    }

    public Task<ApplicationResult> ActivateQuestionAsync(
        Guid surveyId,
        Guid sectionId,
        Guid questionId,
        CancellationToken cancellationToken) =>
        ChangeQuestionStateAsync(surveyId, sectionId, questionId, activate: true, cancellationToken);

    public Task<ApplicationResult> DeactivateQuestionAsync(
        Guid surveyId,
        Guid sectionId,
        Guid questionId,
        CancellationToken cancellationToken) =>
        ChangeQuestionStateAsync(surveyId, sectionId, questionId, activate: false, cancellationToken);

    public async Task<ApplicationResult<SurveyDetailDto>> AddOptionAsync(
        Guid surveyId,
        Guid sectionId,
        Guid questionId,
        CreateSurveyQuestionOptionRequest request,
        CancellationToken cancellationToken)
    {
        var validationErrors = request.Validate();

        if (validationErrors.Count > 0)
        {
            return ApplicationResult<SurveyDetailDto>.Validation(validationErrors);
        }

        var survey = await LoadSurveyAggregateAsync(surveyId, cancellationToken);
        var questionResult = FindQuestion(survey, sectionId, questionId);

        if (questionResult.Status != ApplicationResultStatus.Success)
        {
            return ApplicationResult<SurveyDetailDto>.NotFound(questionResult.Errors.First().Message);
        }

        var question = questionResult.Value!;

        var order = request.Order!.Value;

        if (question.Options.Any(option => option.Order == order))
        {
            return ApplicationResult<SurveyDetailDto>.Conflict("An option with the same order already exists.");
        }

        try
        {
            var option = new SurveyQuestionOption(
                Guid.NewGuid(),
                questionId,
                request.Text!,
                request.Value!,
                order,
                DateTimeOffset.UtcNow);

            question.AddOption(option, DateTimeOffset.UtcNow);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return ApplicationResult<SurveyDetailDto>.Success(MapDetail(survey!));
        }
        catch (DomainException exception)
        {
            return ApplicationResult<SurveyDetailDto>.Validation([
                new ApplicationError("SurveyQuestionOption.Validation", exception.Message)
            ]);
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
        {
            return ApplicationResult<SurveyDetailDto>.Conflict("An option with the same order already exists.");
        }
        catch (DbUpdateException)
        {
            return ApplicationResult<SurveyDetailDto>.Failure("The option could not be saved.");
        }
    }

    public Task<ApplicationResult> ActivateOptionAsync(
        Guid surveyId,
        Guid sectionId,
        Guid questionId,
        Guid optionId,
        CancellationToken cancellationToken) =>
        ChangeOptionStateAsync(surveyId, sectionId, questionId, optionId, activate: true, cancellationToken);

    public Task<ApplicationResult> DeactivateOptionAsync(
        Guid surveyId,
        Guid sectionId,
        Guid questionId,
        Guid optionId,
        CancellationToken cancellationToken) =>
        ChangeOptionStateAsync(surveyId, sectionId, questionId, optionId, activate: false, cancellationToken);

    public async Task<ApplicationResult<SurveyDetailDto>> AddMatrixRowAsync(
        Guid surveyId,
        Guid sectionId,
        Guid questionId,
        CreateSurveyMatrixRowRequest request,
        CancellationToken cancellationToken)
    {
        var validationErrors = request.Validate();

        if (validationErrors.Count > 0)
        {
            return ApplicationResult<SurveyDetailDto>.Validation(validationErrors);
        }

        var survey = await LoadSurveyAggregateAsync(surveyId, cancellationToken);
        var questionResult = FindQuestion(survey, sectionId, questionId);

        if (questionResult.Status != ApplicationResultStatus.Success)
        {
            return ApplicationResult<SurveyDetailDto>.NotFound(questionResult.Errors.First().Message);
        }

        var question = questionResult.Value!;

        var order = request.Order!.Value;

        if (question.MatrixRows.Any(row => row.Order == order))
        {
            return ApplicationResult<SurveyDetailDto>.Conflict("A matrix row with the same order already exists.");
        }

        try
        {
            var matrixRow = new SurveyMatrixRow(
                Guid.NewGuid(),
                questionId,
                request.Text!,
                order,
                DateTimeOffset.UtcNow);

            question.AddMatrixRow(matrixRow, DateTimeOffset.UtcNow);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return ApplicationResult<SurveyDetailDto>.Success(MapDetail(survey!));
        }
        catch (DomainException exception)
        {
            return ApplicationResult<SurveyDetailDto>.Validation([
                new ApplicationError("SurveyMatrixRow.Validation", exception.Message)
            ]);
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
        {
            return ApplicationResult<SurveyDetailDto>.Conflict("A matrix row with the same order already exists.");
        }
        catch (DbUpdateException)
        {
            return ApplicationResult<SurveyDetailDto>.Failure("The matrix row could not be saved.");
        }
    }

    public Task<ApplicationResult> ActivateMatrixRowAsync(
        Guid surveyId,
        Guid sectionId,
        Guid questionId,
        Guid rowId,
        CancellationToken cancellationToken) =>
        ChangeMatrixRowStateAsync(surveyId, sectionId, questionId, rowId, activate: true, cancellationToken);

    public Task<ApplicationResult> DeactivateMatrixRowAsync(
        Guid surveyId,
        Guid sectionId,
        Guid questionId,
        Guid rowId,
        CancellationToken cancellationToken) =>
        ChangeMatrixRowStateAsync(surveyId, sectionId, questionId, rowId, activate: false, cancellationToken);

    private async Task<ApplicationResult> ChangeSurveyStateAsync(
        Guid id,
        Action<Survey, DateTimeOffset> stateChange,
        CancellationToken cancellationToken)
    {
        var survey = await _dbContext.Surveys.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (survey is null)
        {
            return ApplicationResult.NotFound("Survey was not found.");
        }

        try
        {
            stateChange(survey, DateTimeOffset.UtcNow);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return ApplicationResult.Success();
        }
        catch (DomainException exception)
        {
            return ApplicationResult.Validation([new ApplicationError("Survey.Validation", exception.Message)]);
        }
        catch (DbUpdateException)
        {
            return ApplicationResult.Failure("The survey could not be saved.");
        }
    }

    private async Task<ApplicationResult> ChangeSectionStateAsync(
        Guid surveyId,
        Guid sectionId,
        bool activate,
        CancellationToken cancellationToken)
    {
        var survey = await LoadSurveyAggregateAsync(surveyId, cancellationToken);
        var sectionResult = FindSection(survey, sectionId);

        if (sectionResult.Status != ApplicationResultStatus.Success)
        {
            return sectionResult;
        }

        var now = DateTimeOffset.UtcNow;

        if (activate)
        {
            sectionResult.Value!.Activate(now);
        }
        else
        {
            sectionResult.Value!.Deactivate(now);
        }

        return await SaveChangesAsync("The section could not be saved.", cancellationToken);
    }

    private async Task<ApplicationResult> ChangeQuestionStateAsync(
        Guid surveyId,
        Guid sectionId,
        Guid questionId,
        bool activate,
        CancellationToken cancellationToken)
    {
        var survey = await LoadSurveyAggregateAsync(surveyId, cancellationToken);
        var questionResult = FindQuestion(survey, sectionId, questionId);

        if (questionResult.Status != ApplicationResultStatus.Success)
        {
            return questionResult;
        }

        var now = DateTimeOffset.UtcNow;

        if (activate)
        {
            questionResult.Value!.Activate(now);
        }
        else
        {
            questionResult.Value!.Deactivate(now);
        }

        return await SaveChangesAsync("The question could not be saved.", cancellationToken);
    }

    private async Task<ApplicationResult> ChangeOptionStateAsync(
        Guid surveyId,
        Guid sectionId,
        Guid questionId,
        Guid optionId,
        bool activate,
        CancellationToken cancellationToken)
    {
        var survey = await LoadSurveyAggregateAsync(surveyId, cancellationToken);
        var optionResult = FindOption(survey, sectionId, questionId, optionId);

        if (optionResult.Status != ApplicationResultStatus.Success)
        {
            return optionResult;
        }

        var now = DateTimeOffset.UtcNow;

        if (activate)
        {
            optionResult.Value!.Activate(now);
        }
        else
        {
            optionResult.Value!.Deactivate(now);
        }

        return await SaveChangesAsync("The option could not be saved.", cancellationToken);
    }

    private async Task<ApplicationResult> ChangeMatrixRowStateAsync(
        Guid surveyId,
        Guid sectionId,
        Guid questionId,
        Guid rowId,
        bool activate,
        CancellationToken cancellationToken)
    {
        var survey = await LoadSurveyAggregateAsync(surveyId, cancellationToken);
        var rowResult = FindMatrixRow(survey, sectionId, questionId, rowId);

        if (rowResult.Status != ApplicationResultStatus.Success)
        {
            return rowResult;
        }

        var now = DateTimeOffset.UtcNow;

        if (activate)
        {
            rowResult.Value!.Activate(now);
        }
        else
        {
            rowResult.Value!.Deactivate(now);
        }

        return await SaveChangesAsync("The matrix row could not be saved.", cancellationToken);
    }

    private async Task<ApplicationResult> SaveChangesAsync(
        string failureMessage,
        CancellationToken cancellationToken)
    {
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            return ApplicationResult.Success();
        }
        catch (DbUpdateException)
        {
            return ApplicationResult.Failure(failureMessage);
        }
    }

    private Task<Survey?> LoadSurveyForReadAsync(Guid id, CancellationToken cancellationToken)
    {
        return _dbContext.Surveys
            .AsNoTracking()
            .Include(survey => survey.Sections)
                .ThenInclude(section => section.Questions)
                    .ThenInclude(question => question.Options)
            .Include(survey => survey.Sections)
                .ThenInclude(section => section.Questions)
                    .ThenInclude(question => question.MatrixRows)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
    }

    private Task<Survey?> LoadSurveyAggregateAsync(Guid id, CancellationToken cancellationToken)
    {
        return _dbContext.Surveys
            .Include(survey => survey.Sections)
                .ThenInclude(section => section.Questions)
                    .ThenInclude(question => question.Options)
            .Include(survey => survey.Sections)
                .ThenInclude(section => section.Questions)
                    .ThenInclude(question => question.MatrixRows)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
    }

    private static ApplicationResult<SurveySection> FindSection(Survey? survey, Guid sectionId)
    {
        if (survey is null)
        {
            return ApplicationResult<SurveySection>.NotFound("Survey was not found.");
        }

        var section = survey.Sections.SingleOrDefault(item => item.Id == sectionId);

        return section is null
            ? ApplicationResult<SurveySection>.NotFound("Survey section was not found.")
            : ApplicationResult<SurveySection>.Success(section);
    }

    private static ApplicationResult<SurveyQuestion> FindQuestion(
        Survey? survey,
        Guid sectionId,
        Guid questionId)
    {
        var sectionResult = FindSection(survey, sectionId);

        if (sectionResult.Status != ApplicationResultStatus.Success)
        {
            return ApplicationResult<SurveyQuestion>.NotFound(sectionResult.Errors.First().Message);
        }

        var question = sectionResult.Value!.Questions.SingleOrDefault(item => item.Id == questionId);

        return question is null
            ? ApplicationResult<SurveyQuestion>.NotFound("Survey question was not found.")
            : ApplicationResult<SurveyQuestion>.Success(question);
    }

    private static ApplicationResult<SurveyQuestionOption> FindOption(
        Survey? survey,
        Guid sectionId,
        Guid questionId,
        Guid optionId)
    {
        var questionResult = FindQuestion(survey, sectionId, questionId);

        if (questionResult.Status != ApplicationResultStatus.Success)
        {
            return ApplicationResult<SurveyQuestionOption>.NotFound(questionResult.Errors.First().Message);
        }

        var option = questionResult.Value!.Options.SingleOrDefault(item => item.Id == optionId);

        return option is null
            ? ApplicationResult<SurveyQuestionOption>.NotFound("Survey question option was not found.")
            : ApplicationResult<SurveyQuestionOption>.Success(option);
    }

    private static ApplicationResult<SurveyMatrixRow> FindMatrixRow(
        Survey? survey,
        Guid sectionId,
        Guid questionId,
        Guid rowId)
    {
        var questionResult = FindQuestion(survey, sectionId, questionId);

        if (questionResult.Status != ApplicationResultStatus.Success)
        {
            return ApplicationResult<SurveyMatrixRow>.NotFound(questionResult.Errors.First().Message);
        }

        var row = questionResult.Value!.MatrixRows.SingleOrDefault(item => item.Id == rowId);

        return row is null
            ? ApplicationResult<SurveyMatrixRow>.NotFound("Survey matrix row was not found.")
            : ApplicationResult<SurveyMatrixRow>.Success(row);
    }

    private static SurveyDetailDto MapDetail(Survey survey)
    {
        return new SurveyDetailDto(
            survey.Id,
            survey.CreatedByUserId,
            survey.Title,
            survey.Description,
            survey.Target.ToString(),
            survey.Status.ToString(),
            survey.IsAnonymous,
            survey.IsActive,
            survey.Sections
                .OrderBy(section => section.Order)
                .Select(MapSection)
                .ToArray(),
            survey.CreatedAtUtc,
            survey.UpdatedAtUtc);
    }

    private static SurveySectionDto MapSection(SurveySection section)
    {
        return new SurveySectionDto(
            section.Id,
            section.Title,
            section.Description,
            section.Order,
            section.IsActive,
            section.Questions
                .OrderBy(question => question.Order)
                .Select(MapQuestion)
                .ToArray());
    }

    private static SurveyQuestionDto MapQuestion(SurveyQuestion question)
    {
        return new SurveyQuestionDto(
            question.Id,
            question.Text,
            question.Type.ToString(),
            question.IsRequired,
            question.AllowsComment,
            question.AllowsOtherOption,
            question.Order,
            question.IsActive,
            question.Options
                .OrderBy(option => option.Order)
                .Select(option => new SurveyQuestionOptionDto(
                    option.Id,
                    option.Text,
                    option.Value,
                    option.Order,
                    option.IsActive))
                .ToArray(),
            question.MatrixRows
                .OrderBy(row => row.Order)
                .Select(row => new SurveyMatrixRowDto(
                    row.Id,
                    row.Text,
                    row.Order,
                    row.IsActive))
                .ToArray());
    }

    private static bool TryParseOptionalEnum<TEnum>(
        string? value,
        string errorCode,
        out TEnum? parsedValue,
        out ApplicationError error)
        where TEnum : struct, Enum
    {
        parsedValue = null;
        error = new ApplicationError(errorCode, $"{typeof(TEnum).Name} is invalid.");

        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        if (!Enum.TryParse<TEnum>(value.Trim(), ignoreCase: true, out var parsed))
        {
            return false;
        }

        parsedValue = parsed;
        return true;
    }

    private static bool IsUniqueViolation(DbUpdateException exception)
    {
        return exception.InnerException is PostgresException postgresException
            && postgresException.SqlState == PostgresErrorCodes.UniqueViolation;
    }
}
