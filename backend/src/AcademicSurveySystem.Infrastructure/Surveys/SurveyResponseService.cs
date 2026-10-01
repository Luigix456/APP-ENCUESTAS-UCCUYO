using AcademicSurveySystem.Application.Common.Results;
using AcademicSurveySystem.Application.Surveys.Responses;
using AcademicSurveySystem.Domain.Common;
using AcademicSurveySystem.Domain.Surveys.Entities;
using AcademicSurveySystem.Domain.Surveys.Enums;
using AcademicSurveySystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AcademicSurveySystem.Infrastructure.Surveys;

public sealed class SurveyResponseService : ISurveyResponseService
{
    private readonly ApplicationDbContext _dbContext;

    public SurveyResponseService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ApplicationResult<SurveyResponseSubmissionDto>> SubmitResponseAsync(
        string accessCode,
        SubmitSurveyResponseRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(accessCode))
        {
            return ApplicationResult<SurveyResponseSubmissionDto>.NotFound("Survey session was not found.");
        }

        var session = await QuerySessionForSubmission()
            .SingleOrDefaultAsync(
                item => item.AccessCode == accessCode.Trim(),
                cancellationToken);

        if (session is null)
        {
            return ApplicationResult<SurveyResponseSubmissionDto>.NotFound("Survey session was not found.");
        }

        var now = DateTimeOffset.UtcNow;

        if (session.Status == SurveySessionStatus.Open && session.ExpiresAtUtc <= now)
        {
            try
            {
                session.Expire(now);
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (DomainException)
            {
                return CreateNotAvailableResult();
            }
            catch (DbUpdateException)
            {
                return ApplicationResult<SurveyResponseSubmissionDto>.Failure(
                    "The survey session could not be updated.");
            }

            return CreateNotAvailableResult();
        }

        var availabilityErrors = SurveyResponseValidator.ValidateSessionForSubmission(session, now);

        if (availabilityErrors.Count > 0)
        {
            return ApplicationResult<SurveyResponseSubmissionDto>.Validation(availabilityErrors);
        }

        var survey = session.SurveyAssignment.Survey;
        var validationErrors = SurveyResponseValidator.Validate(request, survey);

        if (validationErrors.Count > 0)
        {
            return ApplicationResult<SurveyResponseSubmissionDto>.Validation(validationErrors);
        }

        var persistedAtUtc = DateTimeOffset.UtcNow;
        var lastAvailabilityCheck = SurveyResponseValidator.ValidateSessionForSubmission(
            session,
            persistedAtUtc);

        if (lastAvailabilityCheck.Count > 0)
        {
            return ApplicationResult<SurveyResponseSubmissionDto>.Validation(lastAvailabilityCheck);
        }

        var response = BuildResponse(session, request, persistedAtUtc);

        _dbContext.SurveyResponses.Add(response);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DomainException exception)
        {
            return ApplicationResult<SurveyResponseSubmissionDto>.Validation([
                new ApplicationError("SurveyResponse.Validation", exception.Message)
            ]);
        }
        catch (DbUpdateException)
        {
            return ApplicationResult<SurveyResponseSubmissionDto>.Failure(
                "The survey response could not be saved.");
        }

        return ApplicationResult<SurveyResponseSubmissionDto>.Success(
            new SurveyResponseSubmissionDto(response.Id, response.SubmittedAtUtc));
    }

    private IQueryable<SurveySession> QuerySessionForSubmission()
    {
        return _dbContext.SurveySessions
            .Include(session => session.SurveyAssignment)
                .ThenInclude(assignment => assignment.Survey)
                    .ThenInclude(survey => survey.Sections)
                        .ThenInclude(section => section.Questions)
                            .ThenInclude(question => question.Options)
            .Include(session => session.SurveyAssignment)
                .ThenInclude(assignment => assignment.Survey)
                    .ThenInclude(survey => survey.Sections)
                        .ThenInclude(section => section.Questions)
                            .ThenInclude(question => question.MatrixRows);
    }

    private static SurveyResponse BuildResponse(
        SurveySession session,
        SubmitSurveyResponseRequest request,
        DateTimeOffset submittedAtUtc)
    {
        var survey = session.SurveyAssignment.Survey;
        var activeQuestions = survey.Sections
            .Where(section => section.IsActive)
            .SelectMany(section => section.Questions)
            .Where(question => question.IsActive)
            .ToDictionary(question => question.Id);

        var response = new SurveyResponse(
            Guid.NewGuid(),
            session.Id,
            survey.Id,
            submittedAtUtc);

        foreach (var answerRequest in request.Answers ?? Array.Empty<SubmitSurveyAnswerRequest>())
        {
            var question = activeQuestions[answerRequest.QuestionId];
            var answer = new SurveyAnswer(
                Guid.NewGuid(),
                response.Id,
                question.Id,
                GetTextValue(question, answerRequest),
                question.Type == SurveyQuestionType.RatingScale ? answerRequest.NumericValue : null,
                answerRequest.Comment,
                GetOtherText(question, answerRequest));

            if (question.Type is SurveyQuestionType.SingleChoice or SurveyQuestionType.MultipleChoice)
            {
                foreach (var optionId in answerRequest.OptionIds ?? Array.Empty<Guid>())
                {
                    answer.AddSelectedOption(new SurveyAnswerOption(answer.Id, optionId));
                }
            }

            if (question.Type == SurveyQuestionType.MatrixSingleChoice)
            {
                foreach (var matrixAnswer in answerRequest.MatrixAnswers
                    ?? Array.Empty<SubmitSurveyMatrixAnswerRequest>())
                {
                    answer.AddMatrixAnswer(new SurveyMatrixAnswer(
                        answer.Id,
                        matrixAnswer.RowId,
                        matrixAnswer.OptionId));
                }
            }

            response.AddAnswer(answer);
        }

        return response;
    }

    private static string? GetTextValue(
        SurveyQuestion question,
        SubmitSurveyAnswerRequest answerRequest)
    {
        return question.Type is SurveyQuestionType.ShortText or SurveyQuestionType.LongText
            ? answerRequest.TextValue
            : null;
    }

    private static string? GetOtherText(
        SurveyQuestion question,
        SubmitSurveyAnswerRequest answerRequest)
    {
        return question.Type is SurveyQuestionType.SingleChoice or SurveyQuestionType.MultipleChoice
            ? answerRequest.OtherText
            : null;
    }

    private static ApplicationResult<SurveyResponseSubmissionDto> CreateNotAvailableResult()
    {
        return ApplicationResult<SurveyResponseSubmissionDto>.Validation([
            new ApplicationError("SurveySession.NotAvailable", "Survey session is not available.")
        ]);
    }
}
