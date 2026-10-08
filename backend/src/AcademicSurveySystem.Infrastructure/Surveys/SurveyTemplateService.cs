using AcademicSurveySystem.Application.Common.Results;
using AcademicSurveySystem.Application.Audit;
using AcademicSurveySystem.Application.Surveys;
using AcademicSurveySystem.Application.Surveys.Dtos;
using AcademicSurveySystem.Application.Surveys.Requests;
using AcademicSurveySystem.Domain.Common;
using AcademicSurveySystem.Domain.Surveys.Entities;
using AcademicSurveySystem.Domain.Surveys.Enums;
using AcademicSurveySystem.Infrastructure.Audit;
using AcademicSurveySystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace AcademicSurveySystem.Infrastructure.Surveys;

public sealed class SurveyTemplateService : ISurveyTemplateService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IAuditWriter _auditWriter;

    public SurveyTemplateService(
        ApplicationDbContext dbContext,
        IAuditWriter? auditWriter = null)
    {
        _dbContext = dbContext;
        _auditWriter = auditWriter ?? NoOpAuditWriter.Instance;
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
                survey.UpdatedAtUtc,
                survey.VersionGroupId,
                survey.VersionNumber,
                survey.BasedOnSurveyId))
            .ToArrayAsync(cancellationToken);

        return ApplicationResult<IReadOnlyCollection<SurveySummaryDto>>.Success(surveys);
    }

    public async Task<ApplicationResult<SurveyDetailDto>> GetSurveyByIdAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var survey = await _dbContext.Surveys
            .AsNoTracking()
            .Where(survey => survey.Id == id)
            .Select(survey => new SurveyDetailDto(
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
                    .Select(section => new SurveySectionDto(
                        section.Id,
                        section.Title,
                        section.Description,
                        section.Order,
                        section.IsActive,
                        section.Questions
                            .OrderBy(question => question.Order)
                            .Select(question => new SurveyQuestionDto(
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
                                    .ToArray(),
                                question.RatingMin,
                                question.RatingMax))
                            .ToArray()))
                    .ToArray(),
                survey.CreatedAtUtc,
                survey.UpdatedAtUtc,
                survey.VersionGroupId,
                survey.VersionNumber,
                survey.BasedOnSurveyId))
            .SingleOrDefaultAsync(cancellationToken);

        return survey is null
            ? ApplicationResult<SurveyDetailDto>.NotFound("Survey was not found.")
            : ApplicationResult<SurveyDetailDto>.Success(survey);
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
            await WriteSurveyAuditAsync(
                "surveys.survey.created",
                "Survey",
                survey.Id,
                $"Creó la plantilla de encuesta {survey.Title}.",
                new
                {
                    survey.Target,
                    survey.VersionNumber,
                    survey.VersionGroupId
                },
                cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return ApplicationResult<SurveyDetailDto>.Success(MapDetail(survey));
        }
        catch (DomainException exception)
        {
            return ApplicationResult<SurveyDetailDto>.Validation([
                CreateValidationError("Survey.Validation", exception)
            ]);
        }
        catch (DbUpdateException)
        {
            return ApplicationResult<SurveyDetailDto>.Failure("The survey could not be saved.");
        }
    }

    public async Task<ApplicationResult<SurveyEditableVersionDto>> GetOrCreateEditableVersionAsync(
        Guid surveyId,
        Guid currentUserId,
        CancellationToken cancellationToken)
    {
        if (currentUserId == Guid.Empty)
        {
            return ApplicationResult<SurveyEditableVersionDto>.Validation([
                new ApplicationError("Survey.CurrentUserRequired", "Current user is required.")
            ]);
        }

        var source = await LoadSurveyForReadAsync(surveyId, cancellationToken);

        if (source is null)
        {
            return ApplicationResult<SurveyEditableVersionDto>.NotFound("Survey was not found.");
        }

        if (source.Status == SurveyStatus.Draft)
        {
            return ApplicationResult<SurveyEditableVersionDto>.Success(
                CreateEditableVersionDto(source, createdNewVersion: false, sourceSurveyId: source.Id));
        }

        var existingDraft = await LoadDraftVersionForReadAsync(source.VersionGroupId, cancellationToken);

        if (existingDraft is not null)
        {
            return ApplicationResult<SurveyEditableVersionDto>.Success(
                CreateEditableVersionDto(existingDraft, createdNewVersion: false, sourceSurveyId: source.Id));
        }

        var nextVersionNumber = await _dbContext.Surveys
            .Where(survey => survey.VersionGroupId == source.VersionGroupId)
            .MaxAsync(survey => (int?)survey.VersionNumber, cancellationToken) ?? 0;
        nextVersionNumber++;

        try
        {
            var now = DateTimeOffset.UtcNow;
            var clone = CloneEditableVersion(source, currentUserId, nextVersionNumber, now);
            _dbContext.Surveys.Add(clone);
            await WriteSurveyAuditAsync(
                "surveys.survey.version_created",
                "Survey",
                clone.Id,
                $"Creó una versión editable de la plantilla {source.Title}.",
                new
                {
                    sourceSurveyId = source.Id,
                    clone.VersionGroupId,
                    clone.VersionNumber
                },
                cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            var created = await LoadSurveyForReadAsync(clone.Id, cancellationToken);

            return ApplicationResult<SurveyEditableVersionDto>.Success(
                CreateEditableVersionDto(created!, createdNewVersion: true, sourceSurveyId: source.Id));
        }
        catch (DomainException exception)
        {
            return ApplicationResult<SurveyEditableVersionDto>.Validation([
                CreateValidationError("Survey.VersionInvalid", exception)
            ]);
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
        {
            var draft = await LoadDraftVersionForReadAsync(source.VersionGroupId, cancellationToken);

            return draft is null
                ? ApplicationResult<SurveyEditableVersionDto>.Conflict("Survey editable version could not be created.")
                : ApplicationResult<SurveyEditableVersionDto>.Success(
                    CreateEditableVersionDto(draft, createdNewVersion: false, sourceSurveyId: source.Id));
        }
        catch (DbUpdateException)
        {
            return ApplicationResult<SurveyEditableVersionDto>.Failure(
                "Survey editable version could not be created.");
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
            var oldTitle = survey.Title;
            var oldDescription = survey.Description;
            var oldTarget = survey.Target.ToString();
            var oldIsAnonymous = survey.IsAnonymous;
            survey.Update(
                request.Title!,
                request.Description,
                target,
                request.IsAnonymous,
                DateTimeOffset.UtcNow);

            await WriteSurveyAuditAsync(
                "surveys.survey.updated",
                "Survey",
                survey.Id,
                $"Actualizó la plantilla de encuesta {survey.Title}.",
                new { changedFields = BuildChangedFields(
                    (nameof(survey.Title), oldTitle, survey.Title),
                    (nameof(survey.Description), oldDescription, survey.Description),
                    (nameof(survey.Target), oldTarget, survey.Target.ToString()),
                    (nameof(survey.IsAnonymous), oldIsAnonymous, survey.IsAnonymous)) },
                cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return ApplicationResult.Success();
        }
        catch (DomainException exception)
        {
            return ApplicationResult.Validation([CreateValidationError("Survey.Validation", exception)]);
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
            var oldStatus = survey.Status.ToString();
            survey.Publish(DateTimeOffset.UtcNow);
            await WriteSurveyAuditAsync(
                "surveys.survey.published",
                "Survey",
                survey.Id,
                $"Publicó la plantilla de encuesta {survey.Title}.",
                new { oldStatus, newStatus = survey.Status.ToString(), survey.VersionNumber },
                cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return ApplicationResult.Success();
        }
        catch (DomainException exception)
        {
            return ApplicationResult.Validation([CreateValidationError("Survey.PublishInvalid", exception)]);
        }
        catch (DbUpdateException)
        {
            return ApplicationResult.Failure("The survey could not be saved.");
        }
    }

    public Task<ApplicationResult> ArchiveSurveyAsync(Guid id, CancellationToken cancellationToken) =>
        ChangeSurveyStateAsync(
            id,
            stateChange: (survey, now) => survey.Archive(now),
            auditAction: "surveys.survey.archived",
            auditDescription: survey => $"Archivó la plantilla de encuesta {survey.Title}.",
            cancellationToken);

    public Task<ApplicationResult> ActivateSurveyAsync(Guid id, CancellationToken cancellationToken) =>
        ChangeSurveyStateAsync(
            id,
            stateChange: (survey, now) => survey.Activate(now),
            auditAction: "surveys.survey.activated",
            auditDescription: survey => $"Activó la plantilla de encuesta {survey.Title}.",
            cancellationToken);

    public Task<ApplicationResult> DeactivateSurveyAsync(Guid id, CancellationToken cancellationToken) =>
        ChangeSurveyStateAsync(
            id,
            stateChange: (survey, now) => survey.Deactivate(now),
            auditAction: "surveys.survey.deactivated",
            auditDescription: survey => $"Desactivó la plantilla de encuesta {survey.Title}.",
            cancellationToken);

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

        var notEditableResult = EnsureSurveyStructureCanBeModifiedForDetail(survey);

        if (notEditableResult is not null)
        {
            return notEditableResult;
        }

        var order = request.Order!.Value;

        if (survey.Sections.Any(section => section.Order == order))
        {
            return ApplicationResult<SurveyDetailDto>.Conflict("A section with the same order already exists.");
        }

        try
        {
            var now = DateTimeOffset.UtcNow;
            var section = new SurveySection(
                Guid.NewGuid(),
                survey.Id,
                request.Title!,
                request.Description,
                order,
                now);

            survey.AddSection(section, now);
            _dbContext.SurveySections.Add(section);
            await _dbContext.SaveChangesAsync(cancellationToken);

            var updatedSurvey = await LoadSurveyForReadAsync(surveyId, cancellationToken);

            return updatedSurvey is null
                ? ApplicationResult<SurveyDetailDto>.NotFound("Survey was not found.")
                : ApplicationResult<SurveyDetailDto>.Success(MapDetail(updatedSurvey));
        }
        catch (DomainException exception)
        {
            return ApplicationResult<SurveyDetailDto>.Validation([
                CreateValidationError("SurveySection.Validation", exception)
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

        var notEditableResult = EnsureSurveyStructureCanBeModified(survey!);

        if (notEditableResult is not null)
        {
            return notEditableResult;
        }

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
            return ApplicationResult.Validation([CreateValidationError("SurveySection.Validation", exception)]);
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

        var notEditableResult = EnsureSurveyStructureCanBeModifiedForDetail(survey!);

        if (notEditableResult is not null)
        {
            return notEditableResult;
        }

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
            var now = DateTimeOffset.UtcNow;
            var question = new SurveyQuestion(
                Guid.NewGuid(),
                section.Id,
                request.Text!,
                type,
                request.IsRequired,
                request.AllowsComment,
                request.AllowsOtherOption,
                order,
                now,
                request.RatingMin,
                request.RatingMax);

            section.AddQuestion(question, now);
            _dbContext.SurveyQuestions.Add(question);
            await _dbContext.SaveChangesAsync(cancellationToken);

            var updatedSurvey = await LoadSurveyForReadAsync(surveyId, cancellationToken);

            return updatedSurvey is null
                ? ApplicationResult<SurveyDetailDto>.NotFound("Survey was not found.")
                : ApplicationResult<SurveyDetailDto>.Success(MapDetail(updatedSurvey));
        }
        catch (DomainException exception)
        {
            return ApplicationResult<SurveyDetailDto>.Validation([
                CreateValidationError("SurveyQuestion.Validation", exception)
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

        var notEditableResult = EnsureSurveyStructureCanBeModified(survey);

        if (notEditableResult is not null)
        {
            return notEditableResult;
        }

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
                DateTimeOffset.UtcNow,
                request.RatingMin,
                request.RatingMax);

            await _dbContext.SaveChangesAsync(cancellationToken);
            return ApplicationResult.Success();
        }
        catch (DomainException exception)
        {
            return ApplicationResult.Validation([CreateValidationError("SurveyQuestion.Validation", exception)]);
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

        var notEditableResult = EnsureSurveyStructureCanBeModifiedForDetail(survey!);

        if (notEditableResult is not null)
        {
            return notEditableResult;
        }

        var order = request.Order!.Value;

        if (question.Options.Any(option => option.Order == order))
        {
            return ApplicationResult<SurveyDetailDto>.Conflict("An option with the same order already exists.");
        }

        try
        {
            var now = DateTimeOffset.UtcNow;
            var option = new SurveyQuestionOption(
                Guid.NewGuid(),
                question.Id,
                request.Text!,
                request.Value!,
                order,
                now);

            question.AddOption(option, now);
            _dbContext.SurveyQuestionOptions.Add(option);
            await _dbContext.SaveChangesAsync(cancellationToken);

            var updatedSurvey = await LoadSurveyForReadAsync(surveyId, cancellationToken);

            return updatedSurvey is null
                ? ApplicationResult<SurveyDetailDto>.NotFound("Survey was not found.")
                : ApplicationResult<SurveyDetailDto>.Success(MapDetail(updatedSurvey));
        }
        catch (DomainException exception)
        {
            return ApplicationResult<SurveyDetailDto>.Validation([
                CreateValidationError("SurveyQuestionOption.Validation", exception)
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

    public async Task<ApplicationResult<SurveyDetailDto>> UpdateOptionAsync(
        Guid surveyId,
        Guid sectionId,
        Guid questionId,
        Guid optionId,
        UpdateSurveyQuestionOptionRequest request,
        CancellationToken cancellationToken)
    {
        var validationErrors = request.Validate();

        if (validationErrors.Count > 0)
        {
            return ApplicationResult<SurveyDetailDto>.Validation(validationErrors);
        }

        var survey = await LoadSurveyAggregateAsync(surveyId, cancellationToken);
        var optionResult = FindOption(survey, sectionId, questionId, optionId);

        if (optionResult.Status != ApplicationResultStatus.Success)
        {
            return ApplicationResult<SurveyDetailDto>.NotFound(optionResult.Errors.First().Message);
        }

        var question = survey!.Sections
            .Single(section => section.Id == sectionId)
            .Questions
            .Single(question => question.Id == questionId);

        var notEditableResult = EnsureSurveyStructureCanBeModifiedForDetail(survey);

        if (notEditableResult is not null)
        {
            return notEditableResult;
        }

        var order = request.Order!.Value;

        if (question.Options.Any(option => option.Id != optionId && option.Order == order))
        {
            return ApplicationResult<SurveyDetailDto>.Conflict("An option with the same order already exists.");
        }

        try
        {
            question.UpdateOption(
                optionId,
                request.Text!,
                request.Value!,
                order,
                DateTimeOffset.UtcNow);

            await _dbContext.SaveChangesAsync(cancellationToken);

            var updatedSurvey = await LoadSurveyForReadAsync(surveyId, cancellationToken);

            return updatedSurvey is null
                ? ApplicationResult<SurveyDetailDto>.NotFound("Survey was not found.")
                : ApplicationResult<SurveyDetailDto>.Success(MapDetail(updatedSurvey));
        }
        catch (DomainException exception)
        {
            return ApplicationResult<SurveyDetailDto>.Validation([
                CreateValidationError("SurveyQuestionOption.Validation", exception)
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

        var notEditableResult = EnsureSurveyStructureCanBeModifiedForDetail(survey!);

        if (notEditableResult is not null)
        {
            return notEditableResult;
        }

        var order = request.Order!.Value;

        if (question.MatrixRows.Any(row => row.Order == order))
        {
            return ApplicationResult<SurveyDetailDto>.Conflict("A matrix row with the same order already exists.");
        }

        try
        {
            var now = DateTimeOffset.UtcNow;
            var matrixRow = new SurveyMatrixRow(
                Guid.NewGuid(),
                question.Id,
                request.Text!,
                order,
                now);

            question.AddMatrixRow(matrixRow, now);
            _dbContext.SurveyMatrixRows.Add(matrixRow);
            await _dbContext.SaveChangesAsync(cancellationToken);

            var updatedSurvey = await LoadSurveyForReadAsync(surveyId, cancellationToken);

            return updatedSurvey is null
                ? ApplicationResult<SurveyDetailDto>.NotFound("Survey was not found.")
                : ApplicationResult<SurveyDetailDto>.Success(MapDetail(updatedSurvey));
        }
        catch (DomainException exception)
        {
            return ApplicationResult<SurveyDetailDto>.Validation([
                CreateValidationError("SurveyMatrixRow.Validation", exception)
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

    public async Task<ApplicationResult<SurveyDetailDto>> UpdateMatrixRowAsync(
        Guid surveyId,
        Guid sectionId,
        Guid questionId,
        Guid rowId,
        UpdateSurveyMatrixRowRequest request,
        CancellationToken cancellationToken)
    {
        var validationErrors = request.Validate();

        if (validationErrors.Count > 0)
        {
            return ApplicationResult<SurveyDetailDto>.Validation(validationErrors);
        }

        var survey = await LoadSurveyAggregateAsync(surveyId, cancellationToken);
        var rowResult = FindMatrixRow(survey, sectionId, questionId, rowId);

        if (rowResult.Status != ApplicationResultStatus.Success)
        {
            return ApplicationResult<SurveyDetailDto>.NotFound(rowResult.Errors.First().Message);
        }

        var question = survey!.Sections
            .Single(section => section.Id == sectionId)
            .Questions
            .Single(question => question.Id == questionId);

        var notEditableResult = EnsureSurveyStructureCanBeModifiedForDetail(survey);

        if (notEditableResult is not null)
        {
            return notEditableResult;
        }

        var order = request.Order!.Value;

        if (question.MatrixRows.Any(row => row.Id != rowId && row.Order == order))
        {
            return ApplicationResult<SurveyDetailDto>.Conflict("A matrix row with the same order already exists.");
        }

        try
        {
            question.UpdateMatrixRow(
                rowId,
                request.Text!,
                order,
                DateTimeOffset.UtcNow);

            await _dbContext.SaveChangesAsync(cancellationToken);

            var updatedSurvey = await LoadSurveyForReadAsync(surveyId, cancellationToken);

            return updatedSurvey is null
                ? ApplicationResult<SurveyDetailDto>.NotFound("Survey was not found.")
                : ApplicationResult<SurveyDetailDto>.Success(MapDetail(updatedSurvey));
        }
        catch (DomainException exception)
        {
            return ApplicationResult<SurveyDetailDto>.Validation([
                CreateValidationError("SurveyMatrixRow.Validation", exception)
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
        string auditAction,
        Func<Survey, string> auditDescription,
        CancellationToken cancellationToken)
    {
        var survey = await _dbContext.Surveys.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (survey is null)
        {
            return ApplicationResult.NotFound("Survey was not found.");
        }

        try
        {
            var oldStatus = survey.Status.ToString();
            var oldIsActive = survey.IsActive;
            stateChange(survey, DateTimeOffset.UtcNow);
            await WriteSurveyAuditAsync(
                auditAction,
                "Survey",
                survey.Id,
                auditDescription(survey),
                new
                {
                    oldStatus,
                    newStatus = survey.Status.ToString(),
                    oldIsActive,
                    newIsActive = survey.IsActive
                },
                cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return ApplicationResult.Success();
        }
        catch (DomainException exception)
        {
            return ApplicationResult.Validation([CreateValidationError("Survey.Validation", exception)]);
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

        var notEditableResult = EnsureSurveyStructureCanBeModified(survey!);

        if (notEditableResult is not null)
        {
            return notEditableResult;
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

        var notEditableResult = EnsureSurveyStructureCanBeModified(survey!);

        if (notEditableResult is not null)
        {
            return notEditableResult;
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

        var notEditableResult = EnsureSurveyStructureCanBeModified(survey!);

        if (notEditableResult is not null)
        {
            return notEditableResult;
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

        var notEditableResult = EnsureSurveyStructureCanBeModified(survey!);

        if (notEditableResult is not null)
        {
            return notEditableResult;
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

    private static ApplicationResult? EnsureSurveyStructureCanBeModified(Survey survey)
    {
        try
        {
            survey.EnsureStructureCanBeModified();
            return null;
        }
        catch (DomainException exception)
        {
            return ApplicationResult.Validation([CreateValidationError("Survey.Validation", exception)]);
        }
    }

    private static ApplicationResult<SurveyDetailDto>? EnsureSurveyStructureCanBeModifiedForDetail(Survey survey)
    {
        try
        {
            survey.EnsureStructureCanBeModified();
            return null;
        }
        catch (DomainException exception)
        {
            return ApplicationResult<SurveyDetailDto>.Validation([
                CreateValidationError("Survey.Validation", exception)
            ]);
        }
    }

    private static ApplicationError CreateValidationError(string fallbackCode, DomainException exception)
    {
        return new ApplicationError(exception.Code ?? fallbackCode, exception.Message);
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

    private Task<Survey?> LoadDraftVersionForReadAsync(
        Guid versionGroupId,
        CancellationToken cancellationToken)
    {
        return _dbContext.Surveys
            .AsNoTracking()
            .Include(survey => survey.Sections)
                .ThenInclude(section => section.Questions)
                    .ThenInclude(question => question.Options)
            .Include(survey => survey.Sections)
                .ThenInclude(section => section.Questions)
                    .ThenInclude(question => question.MatrixRows)
            .Where(survey => survey.VersionGroupId == versionGroupId)
            .Where(survey => survey.Status == SurveyStatus.Draft)
            .OrderByDescending(survey => survey.VersionNumber)
            .SingleOrDefaultAsync(cancellationToken);
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
            survey.UpdatedAtUtc,
            survey.VersionGroupId,
            survey.VersionNumber,
            survey.BasedOnSurveyId);
    }

    private static SurveyEditableVersionDto CreateEditableVersionDto(
        Survey survey,
        bool createdNewVersion,
        Guid sourceSurveyId)
    {
        return new SurveyEditableVersionDto(
            MapDetail(survey),
            createdNewVersion,
            sourceSurveyId,
            survey.VersionGroupId,
            survey.VersionNumber);
    }

    private static Survey CloneEditableVersion(
        Survey source,
        Guid currentUserId,
        int versionNumber,
        DateTimeOffset now)
    {
        var clone = new Survey(
            Guid.NewGuid(),
            currentUserId,
            source.Title,
            source.Description,
            source.Target,
            now);

        clone.SetVersionMetadata(source.VersionGroupId, versionNumber, source.Id);
        clone.Update(source.Title, source.Description, source.Target, source.IsAnonymous, now);

        if (!source.IsActive)
        {
            clone.Deactivate(now);
        }

        foreach (var sourceSection in source.Sections.OrderBy(section => section.Order))
        {
            var clonedSection = new SurveySection(
                Guid.NewGuid(),
                clone.Id,
                sourceSection.Title,
                sourceSection.Description,
                sourceSection.Order,
                now);

            if (!sourceSection.IsActive)
            {
                clonedSection.Deactivate(now);
            }

            foreach (var sourceQuestion in sourceSection.Questions.OrderBy(question => question.Order))
            {
                var clonedQuestion = new SurveyQuestion(
                    Guid.NewGuid(),
                    clonedSection.Id,
                    sourceQuestion.Text,
                    sourceQuestion.Type,
                    sourceQuestion.IsRequired,
                    sourceQuestion.AllowsComment,
                    sourceQuestion.AllowsOtherOption,
                    sourceQuestion.Order,
                    now,
                    sourceQuestion.RatingMin,
                    sourceQuestion.RatingMax,
                    sourceQuestion.QuestionLineageId);

                if (!sourceQuestion.IsActive)
                {
                    clonedQuestion.Deactivate(now);
                }

                foreach (var sourceOption in sourceQuestion.Options.OrderBy(option => option.Order))
                {
                    var clonedOption = new SurveyQuestionOption(
                        Guid.NewGuid(),
                        clonedQuestion.Id,
                        sourceOption.Text,
                        sourceOption.Value,
                        sourceOption.Order,
                        now);

                    if (!sourceOption.IsActive)
                    {
                        clonedOption.Deactivate(now);
                    }

                    clonedQuestion.AddClonedOption(clonedOption, now);
                }

                foreach (var sourceMatrixRow in sourceQuestion.MatrixRows.OrderBy(row => row.Order))
                {
                    var clonedMatrixRow = new SurveyMatrixRow(
                        Guid.NewGuid(),
                        clonedQuestion.Id,
                        sourceMatrixRow.Text,
                        sourceMatrixRow.Order,
                        now);

                    if (!sourceMatrixRow.IsActive)
                    {
                        clonedMatrixRow.Deactivate(now);
                    }

                    clonedQuestion.AddClonedMatrixRow(clonedMatrixRow, now);
                }

                clonedSection.AddQuestion(clonedQuestion, now);
            }

            clone.AddSection(clonedSection, now);
        }

        return clone;
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
                .ToArray(),
            question.RatingMin,
            question.RatingMax);
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

    private Task WriteSurveyAuditAsync(
        string action,
        string entityType,
        Guid entityId,
        string description,
        object? metadata,
        CancellationToken cancellationToken)
    {
        return _auditWriter.WriteAsync(
            action,
            "survey_templates",
            entityType,
            entityId,
            description,
            metadata,
            cancellationToken);
    }

    private static IReadOnlyCollection<object> BuildChangedFields(
        params (string Field, object? OldValue, object? NewValue)[] fields)
    {
        return fields
            .Where(field => !Equals(field.OldValue, field.NewValue))
            .Select(field => new
            {
                field.Field,
                OldValue = field.OldValue,
                NewValue = field.NewValue
            })
            .ToArray();
    }

    private static bool IsUniqueViolation(DbUpdateException exception)
    {
        return exception.InnerException is PostgresException postgresException
            && postgresException.SqlState == PostgresErrorCodes.UniqueViolation;
    }
}
