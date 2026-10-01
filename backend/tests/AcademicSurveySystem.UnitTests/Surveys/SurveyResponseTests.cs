using System.Reflection;
using AcademicSurveySystem.Application.Surveys.Responses;
using AcademicSurveySystem.Application.Surveys.Sessions;
using AcademicSurveySystem.Domain.Common;
using AcademicSurveySystem.Domain.Surveys.Entities;
using AcademicSurveySystem.Domain.Surveys.Enums;
using AcademicSurveySystem.Infrastructure.Surveys;

namespace AcademicSurveySystem.UnitTests.Surveys;

public sealed class SurveyResponseTests
{
    private static readonly DateTimeOffset CreatedAtUtc =
        new(2026, 9, 22, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void OpenSession_WithValidResponse_PassesValidation()
    {
        var fixture = CreateFixture();
        var session = CreateSession(fixture.Survey, published: true);
        var request = CreateSingleChoiceRequest(fixture.SingleChoiceQuestion.Id, fixture.SingleChoiceOptionA.Id);

        var availabilityErrors = SurveyResponseValidator.ValidateSessionForSubmission(
            session,
            CreatedAtUtc.AddMinutes(2));
        var answerErrors = SurveyResponseValidator.Validate(request, fixture.Survey);

        Assert.Empty(availabilityErrors);
        Assert.Empty(answerErrors);
    }

    [Fact]
    public void ClosedSession_RejectsResponse()
    {
        var fixture = CreateFixture();
        var session = CreateSession(fixture.Survey, published: true);
        session.Close(CreatedAtUtc.AddMinutes(2));

        var errors = SurveyResponseValidator.ValidateSessionForSubmission(
            session,
            CreatedAtUtc.AddMinutes(3));

        Assert.Contains(errors, error => error.Code == "SurveySession.NotAvailable");
    }

    [Fact]
    public void ExpiredSession_RejectsResponse()
    {
        var fixture = CreateFixture();
        var session = CreateSession(fixture.Survey, published: true, expiresAtUtc: CreatedAtUtc.AddMinutes(3));
        session.Expire(CreatedAtUtc.AddMinutes(3));

        var errors = SurveyResponseValidator.ValidateSessionForSubmission(
            session,
            CreatedAtUtc.AddMinutes(4));

        Assert.Contains(errors, error => error.Code == "SurveySession.NotAvailable");
    }

    [Fact]
    public void DraftSurvey_RejectsResponse()
    {
        var fixture = CreateFixture();
        var session = CreateSession(fixture.Survey, published: false);

        var errors = SurveyResponseValidator.ValidateSessionForSubmission(
            session,
            CreatedAtUtc.AddMinutes(2));

        Assert.Contains(errors, error => error.Code == "SurveyResponse.SurveyNotPublished");
    }

    [Fact]
    public void RequiredQuestionMissing_Fails()
    {
        var fixture = CreateFixture();
        var request = new SubmitSurveyResponseRequest([]);

        var errors = SurveyResponseValidator.Validate(request, fixture.Survey);

        Assert.Contains(errors, error => error.Code == "SurveyResponse.RequiredQuestionMissing");
    }

    [Fact]
    public void QuestionFromAnotherSurvey_Fails()
    {
        var fixture = CreateFixture();
        var otherFixture = CreateFixture();
        var request = CreateSingleChoiceRequest(
            otherFixture.SingleChoiceQuestion.Id,
            otherFixture.SingleChoiceOptionA.Id);

        var errors = SurveyResponseValidator.Validate(request, fixture.Survey);

        Assert.Contains(errors, error => error.Code == "SurveyResponse.InvalidQuestion");
    }

    [Fact]
    public void OptionFromAnotherQuestion_Fails()
    {
        var fixture = CreateFixture();
        var request = CreateSingleChoiceRequest(
            fixture.SingleChoiceQuestion.Id,
            fixture.MultipleChoiceOptionA.Id);

        var errors = SurveyResponseValidator.Validate(request, fixture.Survey);

        Assert.Contains(errors, error => error.Code == "SurveyResponse.InvalidOption");
    }

    [Fact]
    public void InactiveOption_Fails()
    {
        var fixture = CreateFixture();
        fixture.SingleChoiceOptionA.Deactivate(CreatedAtUtc.AddMinutes(1));
        var request = CreateSingleChoiceRequest(fixture.SingleChoiceQuestion.Id, fixture.SingleChoiceOptionA.Id);

        var errors = SurveyResponseValidator.Validate(request, fixture.Survey);

        Assert.Contains(errors, error => error.Code == "SurveyResponse.InvalidOption");
    }

    [Fact]
    public void InactiveQuestion_Fails()
    {
        var fixture = CreateFixture(singleChoiceRequired: false);
        fixture.SingleChoiceQuestion.Deactivate(CreatedAtUtc.AddMinutes(1));
        var request = CreateSingleChoiceRequest(fixture.SingleChoiceQuestion.Id, fixture.SingleChoiceOptionA.Id);

        var errors = SurveyResponseValidator.Validate(request, fixture.Survey);

        Assert.Contains(errors, error => error.Code == "SurveyResponse.InvalidQuestion");
    }

    [Fact]
    public void DuplicateQuestion_Fails()
    {
        var fixture = CreateFixture();
        var request = new SubmitSurveyResponseRequest([
            new SubmitSurveyAnswerRequest(
                fixture.SingleChoiceQuestion.Id,
                [fixture.SingleChoiceOptionA.Id],
                null,
                null,
                null,
                null),
            new SubmitSurveyAnswerRequest(
                fixture.SingleChoiceQuestion.Id,
                [fixture.SingleChoiceOptionB.Id],
                null,
                null,
                null,
                null)
        ]);

        var errors = SurveyResponseValidator.Validate(request, fixture.Survey);

        Assert.Contains(errors, error => error.Code == "SurveyResponse.DuplicateQuestion");
    }

    [Fact]
    public void SingleChoice_WithMultipleOptions_Fails()
    {
        var fixture = CreateFixture();
        var request = new SubmitSurveyResponseRequest([
            new SubmitSurveyAnswerRequest(
                fixture.SingleChoiceQuestion.Id,
                [fixture.SingleChoiceOptionA.Id, fixture.SingleChoiceOptionB.Id],
                null,
                null,
                null,
                null)
        ]);

        var errors = SurveyResponseValidator.Validate(request, fixture.Survey);

        Assert.Contains(errors, error => error.Code == "SurveyResponse.InvalidAnswer");
    }

    [Fact]
    public void SingleChoice_WithOnlyOtherText_PassesWhenAllowed()
    {
        var fixture = CreateFixture(allowsOtherOption: true);
        var request = new SubmitSurveyResponseRequest([
            new SubmitSurveyAnswerRequest(
                fixture.SingleChoiceQuestion.Id,
                [],
                null,
                null,
                null,
                null,
                OtherText: "Otra alternativa")
        ]);

        var errors = SurveyResponseValidator.Validate(request, fixture.Survey);

        Assert.Empty(errors);
    }

    [Fact]
    public void SingleChoice_WithOptionAndOtherText_Fails()
    {
        var fixture = CreateFixture(allowsOtherOption: true);
        var request = new SubmitSurveyResponseRequest([
            new SubmitSurveyAnswerRequest(
                fixture.SingleChoiceQuestion.Id,
                [fixture.SingleChoiceOptionA.Id],
                null,
                null,
                null,
                null,
                OtherText: "Otra alternativa")
        ]);

        var errors = SurveyResponseValidator.Validate(request, fixture.Survey);

        Assert.Contains(errors, error => error.Code == "SurveyResponse.InvalidAnswer");
    }

    [Fact]
    public void SingleChoice_WithWhitespaceOtherTextAndNoOption_Fails()
    {
        var fixture = CreateFixture(allowsOtherOption: true);
        var request = new SubmitSurveyResponseRequest([
            new SubmitSurveyAnswerRequest(
                fixture.SingleChoiceQuestion.Id,
                [],
                null,
                null,
                null,
                null,
                OtherText: "   ")
        ]);

        var errors = SurveyResponseValidator.Validate(request, fixture.Survey);

        Assert.Contains(errors, error => error.Code == "SurveyResponse.InvalidAnswer");
    }

    [Fact]
    public void MultipleChoice_WithValidOptions_Passes()
    {
        var fixture = CreateFixture(singleChoiceRequired: false);
        var request = new SubmitSurveyResponseRequest([
            new SubmitSurveyAnswerRequest(
                fixture.MultipleChoiceQuestion.Id,
                [fixture.MultipleChoiceOptionA.Id, fixture.MultipleChoiceOptionB.Id],
                null,
                null,
                null,
                null)
        ]);

        var errors = SurveyResponseValidator.Validate(request, fixture.Survey);

        Assert.Empty(errors);
    }

    [Fact]
    public void MultipleChoice_WithOnlyOtherText_PassesWhenAllowed()
    {
        var fixture = CreateFixture(singleChoiceRequired: false, allowsOtherOption: true);
        var request = new SubmitSurveyResponseRequest([
            new SubmitSurveyAnswerRequest(
                fixture.MultipleChoiceQuestion.Id,
                [],
                null,
                null,
                null,
                null,
                OtherText: "Otra alternativa")
        ]);

        var errors = SurveyResponseValidator.Validate(request, fixture.Survey);

        Assert.Empty(errors);
    }

    [Fact]
    public void MultipleChoice_WithOptionsAndOtherText_PassesWhenAllowed()
    {
        var fixture = CreateFixture(singleChoiceRequired: false, allowsOtherOption: true);
        var request = new SubmitSurveyResponseRequest([
            new SubmitSurveyAnswerRequest(
                fixture.MultipleChoiceQuestion.Id,
                [fixture.MultipleChoiceOptionA.Id],
                null,
                null,
                "Comentario general",
                null,
                OtherText: "Otra alternativa")
        ]);

        var errors = SurveyResponseValidator.Validate(request, fixture.Survey);

        Assert.Empty(errors);
    }

    [Fact]
    public void ChoiceQuestion_WithOtherTextWhenNotAllowed_Fails()
    {
        var fixture = CreateFixture(singleChoiceRequired: false);
        var request = new SubmitSurveyResponseRequest([
            new SubmitSurveyAnswerRequest(
                fixture.MultipleChoiceQuestion.Id,
                [fixture.MultipleChoiceOptionA.Id],
                null,
                null,
                null,
                null,
                OtherText: "Otra alternativa")
        ]);

        var errors = SurveyResponseValidator.Validate(request, fixture.Survey);

        Assert.Contains(errors, error => error.Code == "SurveyResponse.OtherTextNotAllowed");
    }

    [Fact]
    public void NonChoiceQuestion_WithOtherText_Fails()
    {
        var fixture = CreateFixture(singleChoiceRequired: false);
        var request = new SubmitSurveyResponseRequest([
            new SubmitSurveyAnswerRequest(
                fixture.RatingQuestion.Id,
                null,
                null,
                3,
                null,
                null,
                OtherText: "Otra alternativa")
        ]);

        var errors = SurveyResponseValidator.Validate(request, fixture.Survey);

        Assert.Contains(errors, error => error.Code == "SurveyResponse.OtherTextNotAllowed");
    }

    [Fact]
    public void TextValueInSelectionQuestion_Fails()
    {
        var fixture = CreateFixture();
        var request = new SubmitSurveyResponseRequest([
            new SubmitSurveyAnswerRequest(
                fixture.SingleChoiceQuestion.Id,
                [fixture.SingleChoiceOptionA.Id],
                "texto invalido",
                null,
                null,
                null)
        ]);

        var errors = SurveyResponseValidator.Validate(request, fixture.Survey);

        Assert.Contains(errors, error => error.Code == "SurveyResponse.InvalidAnswer");
    }

    [Fact]
    public void Comment_WhenNotAllowed_Fails()
    {
        var fixture = CreateFixture();
        var request = new SubmitSurveyResponseRequest([
            new SubmitSurveyAnswerRequest(
                fixture.SingleChoiceQuestion.Id,
                [fixture.SingleChoiceOptionA.Id],
                null,
                null,
                "comentario",
                null)
        ]);

        var errors = SurveyResponseValidator.Validate(request, fixture.Survey);

        Assert.Contains(errors, error => error.Code == "SurveyResponse.CommentNotAllowed");
    }

    [Fact]
    public void MatrixSingleChoice_WithValidRowsAndOptions_Passes()
    {
        var fixture = CreateFixture(singleChoiceRequired: false);
        var request = new SubmitSurveyResponseRequest([
            new SubmitSurveyAnswerRequest(
                fixture.MatrixQuestion.Id,
                null,
                null,
                null,
                null,
                [
                    new SubmitSurveyMatrixAnswerRequest(fixture.MatrixRowA.Id, fixture.MatrixOptionA.Id),
                    new SubmitSurveyMatrixAnswerRequest(fixture.MatrixRowB.Id, fixture.MatrixOptionB.Id)
                ])
        ]);

        var errors = SurveyResponseValidator.Validate(request, fixture.Survey);

        Assert.Empty(errors);
    }

    [Fact]
    public void MatrixSingleChoice_WithInvalidRowOrOption_Fails()
    {
        var fixture = CreateFixture(singleChoiceRequired: false);
        var request = new SubmitSurveyResponseRequest([
            new SubmitSurveyAnswerRequest(
                fixture.MatrixQuestion.Id,
                null,
                null,
                null,
                null,
                [
                    new SubmitSurveyMatrixAnswerRequest(Guid.NewGuid(), Guid.NewGuid())
                ])
        ]);

        var errors = SurveyResponseValidator.Validate(request, fixture.Survey);

        Assert.Contains(errors, error => error.Code == "SurveyResponse.InvalidMatrixRow");
        Assert.Contains(errors, error => error.Code == "SurveyResponse.InvalidOption");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(5)]
    public void RatingScale_WithValueInsideConfiguredRange_Passes(int value)
    {
        var fixture = CreateFixture(singleChoiceRequired: false);
        var request = new SubmitSurveyResponseRequest([
            new SubmitSurveyAnswerRequest(
                fixture.RatingQuestion.Id,
                null,
                null,
                value,
                null,
                null)
        ]);

        var errors = SurveyResponseValidator.Validate(request, fixture.Survey);

        Assert.Empty(errors);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public void RatingScale_WithValueOutsideConfiguredRange_Fails(int value)
    {
        var fixture = CreateFixture(singleChoiceRequired: false);
        var request = new SubmitSurveyResponseRequest([
            new SubmitSurveyAnswerRequest(
                fixture.RatingQuestion.Id,
                null,
                null,
                value,
                null,
                null)
        ]);

        var errors = SurveyResponseValidator.Validate(request, fixture.Survey);

        Assert.Contains(errors, error => error.Code == "SurveyResponse.InvalidRatingValue");
    }

    [Fact]
    public void RatingScale_LegacyQuestionWithoutBounds_StillAllowsPositiveValues()
    {
        var fixture = CreateFixture(singleChoiceRequired: false);
        SetPrivateProperty<SurveyQuestion, int?>(fixture.RatingQuestion, nameof(SurveyQuestion.RatingMin), null);
        SetPrivateProperty<SurveyQuestion, int?>(fixture.RatingQuestion, nameof(SurveyQuestion.RatingMax), null);
        var request = new SubmitSurveyResponseRequest([
            new SubmitSurveyAnswerRequest(
                fixture.RatingQuestion.Id,
                null,
                null,
                3,
                null,
                null)
        ]);

        var errors = SurveyResponseValidator.Validate(request, fixture.Survey);

        Assert.Empty(errors);
    }

    [Fact]
    public void SurveyResponse_PersistsAnswersOptionsAndMatrixRowsInAggregate()
    {
        var response = new SurveyResponse(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            CreatedAtUtc);

        var choiceAnswer = new SurveyAnswer(
            Guid.NewGuid(),
            response.Id,
            Guid.NewGuid(),
            null,
            null,
            null);
        choiceAnswer.AddSelectedOption(new SurveyAnswerOption(choiceAnswer.Id, Guid.NewGuid()));
        response.AddAnswer(choiceAnswer);

        var matrixAnswer = new SurveyAnswer(
            Guid.NewGuid(),
            response.Id,
            Guid.NewGuid(),
            null,
            null,
            null);
        matrixAnswer.AddMatrixAnswer(new SurveyMatrixAnswer(
            matrixAnswer.Id,
            Guid.NewGuid(),
            Guid.NewGuid()));
        response.AddAnswer(matrixAnswer);

        Assert.Equal(2, response.Answers.Count);
        Assert.Single(choiceAnswer.SelectedOptions);
        Assert.Single(matrixAnswer.MatrixAnswers);
    }

    [Fact]
    public void SurveyAnswer_NormalizesOtherText()
    {
        var answer = new SurveyAnswer(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            null,
            null,
            otherText: "  Otra alternativa  ");

        Assert.Equal("Otra alternativa", answer.OtherText);
    }

    [Fact]
    public void SurveyAnswer_StoresWhitespaceOtherTextAsNull()
    {
        var answer = new SurveyAnswer(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            null,
            null,
            otherText: "   ");

        Assert.Null(answer.OtherText);
    }

    [Fact]
    public void PublicSurveyQuestionDto_IncludesIsRequired()
    {
        var dto = new PublicSurveyQuestionDto(
            Guid.NewGuid(),
            "Pregunta",
            "SingleChoice",
            IsRequired: true,
            AllowsComment: false,
            AllowsOtherOption: false,
            Order: 1,
            [],
            []);

        Assert.True(dto.IsRequired);
    }

    [Fact]
    public void PublicSurveyQuestionDto_IncludesRatingBounds()
    {
        var dto = new PublicSurveyQuestionDto(
            Guid.NewGuid(),
            "Califique",
            "RatingScale",
            IsRequired: true,
            AllowsComment: false,
            AllowsOtherOption: false,
            Order: 1,
            [],
            [],
            RatingMin: 1,
            RatingMax: 5);

        Assert.Equal(1, dto.RatingMin);
        Assert.Equal(5, dto.RatingMax);
    }

    private static SubmitSurveyResponseRequest CreateSingleChoiceRequest(Guid questionId, Guid optionId)
    {
        return new SubmitSurveyResponseRequest([
            new SubmitSurveyAnswerRequest(
                questionId,
                [optionId],
                null,
                null,
                null,
                null)
        ]);
    }

    private static SurveySession CreateSession(
        Survey survey,
        bool published,
        DateTimeOffset? expiresAtUtc = null)
    {
        if (published && survey.Status == SurveyStatus.Draft)
        {
            survey.Publish(CreatedAtUtc.AddMinutes(1));
        }

        var assignment = new SurveyAssignment(
            Guid.NewGuid(),
            survey.Id,
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            CreatedAtUtc);
        SetPrivateProperty(assignment, nameof(SurveyAssignment.Survey), survey);

        var session = new SurveySession(
            Guid.NewGuid(),
            assignment.Id,
            Guid.NewGuid(),
            "access-code",
            "Title",
            "Room",
            expiresAtUtc ?? CreatedAtUtc.AddHours(2),
            CreatedAtUtc);
        SetPrivateProperty(session, nameof(SurveySession.SurveyAssignment), assignment);
        session.Open(CreatedAtUtc.AddMinutes(1));

        return session;
    }

    private static SurveyFixture CreateFixture(
        bool singleChoiceRequired = true,
        bool allowsOtherOption = false)
    {
        var survey = new Survey(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Encuesta",
            "Descripcion",
            SurveyTarget.Student,
            CreatedAtUtc);

        var section = new SurveySection(
            Guid.NewGuid(),
            survey.Id,
            "Seccion",
            null,
            order: 1,
            CreatedAtUtc);

        var singleChoiceQuestion = new SurveyQuestion(
            Guid.NewGuid(),
            section.Id,
            "Pregunta single",
            SurveyQuestionType.SingleChoice,
            isRequired: singleChoiceRequired,
            allowsComment: false,
            allowsOtherOption,
            order: 1,
            CreatedAtUtc);
        var singleChoiceOptionA = new SurveyQuestionOption(
            Guid.NewGuid(),
            singleChoiceQuestion.Id,
            "A",
            "a",
            order: 1,
            CreatedAtUtc);
        var singleChoiceOptionB = new SurveyQuestionOption(
            Guid.NewGuid(),
            singleChoiceQuestion.Id,
            "B",
            "b",
            order: 2,
            CreatedAtUtc);
        singleChoiceQuestion.AddOption(singleChoiceOptionA, CreatedAtUtc.AddMinutes(1));
        singleChoiceQuestion.AddOption(singleChoiceOptionB, CreatedAtUtc.AddMinutes(1));

        var multipleChoiceQuestion = new SurveyQuestion(
            Guid.NewGuid(),
            section.Id,
            "Pregunta multiple",
            SurveyQuestionType.MultipleChoice,
            isRequired: false,
            allowsComment: true,
            allowsOtherOption,
            order: 2,
            CreatedAtUtc);
        var multipleChoiceOptionA = new SurveyQuestionOption(
            Guid.NewGuid(),
            multipleChoiceQuestion.Id,
            "M1",
            "m1",
            order: 1,
            CreatedAtUtc);
        var multipleChoiceOptionB = new SurveyQuestionOption(
            Guid.NewGuid(),
            multipleChoiceQuestion.Id,
            "M2",
            "m2",
            order: 2,
            CreatedAtUtc);
        multipleChoiceQuestion.AddOption(multipleChoiceOptionA, CreatedAtUtc.AddMinutes(1));
        multipleChoiceQuestion.AddOption(multipleChoiceOptionB, CreatedAtUtc.AddMinutes(1));

        var matrixQuestion = new SurveyQuestion(
            Guid.NewGuid(),
            section.Id,
            "Pregunta matriz",
            SurveyQuestionType.MatrixSingleChoice,
            isRequired: false,
            allowsComment: false,
            allowsOtherOption: false,
            order: 3,
            CreatedAtUtc);
        var matrixOptionA = new SurveyQuestionOption(
            Guid.NewGuid(),
            matrixQuestion.Id,
            "Bueno",
            "good",
            order: 1,
            CreatedAtUtc);
        var matrixOptionB = new SurveyQuestionOption(
            Guid.NewGuid(),
            matrixQuestion.Id,
            "Excelente",
            "excellent",
            order: 2,
            CreatedAtUtc);
        var matrixRowA = new SurveyMatrixRow(
            Guid.NewGuid(),
            matrixQuestion.Id,
            "Claridad",
            order: 1,
            CreatedAtUtc);
        var matrixRowB = new SurveyMatrixRow(
            Guid.NewGuid(),
            matrixQuestion.Id,
            "Organizacion",
            order: 2,
            CreatedAtUtc);
        matrixQuestion.AddOption(matrixOptionA, CreatedAtUtc.AddMinutes(1));
        matrixQuestion.AddOption(matrixOptionB, CreatedAtUtc.AddMinutes(1));
        matrixQuestion.AddMatrixRow(matrixRowA, CreatedAtUtc.AddMinutes(1));
        matrixQuestion.AddMatrixRow(matrixRowB, CreatedAtUtc.AddMinutes(1));

        var ratingQuestion = new SurveyQuestion(
            Guid.NewGuid(),
            section.Id,
            "Pregunta rating",
            SurveyQuestionType.RatingScale,
            isRequired: false,
            allowsComment: false,
            allowsOtherOption: false,
            order: 4,
            CreatedAtUtc,
            ratingMin: 1,
            ratingMax: 5);

        section.AddQuestion(singleChoiceQuestion, CreatedAtUtc.AddMinutes(1));
        section.AddQuestion(multipleChoiceQuestion, CreatedAtUtc.AddMinutes(1));
        section.AddQuestion(matrixQuestion, CreatedAtUtc.AddMinutes(1));
        section.AddQuestion(ratingQuestion, CreatedAtUtc.AddMinutes(1));
        survey.AddSection(section, CreatedAtUtc.AddMinutes(1));

        return new SurveyFixture(
            survey,
            singleChoiceQuestion,
            singleChoiceOptionA,
            singleChoiceOptionB,
            multipleChoiceQuestion,
            multipleChoiceOptionA,
            multipleChoiceOptionB,
            matrixQuestion,
            matrixOptionA,
            matrixOptionB,
            matrixRowA,
            matrixRowB,
            ratingQuestion);
    }

    private static void SetPrivateProperty<TTarget, TValue>(
        TTarget target,
        string propertyName,
        TValue value)
        where TTarget : notnull
    {
        var property = typeof(TTarget).GetProperty(
            propertyName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        if (property is null)
        {
            throw new InvalidOperationException($"{propertyName} was not found.");
        }

        property.SetValue(target, value);
    }

    private sealed record SurveyFixture(
        Survey Survey,
        SurveyQuestion SingleChoiceQuestion,
        SurveyQuestionOption SingleChoiceOptionA,
        SurveyQuestionOption SingleChoiceOptionB,
        SurveyQuestion MultipleChoiceQuestion,
        SurveyQuestionOption MultipleChoiceOptionA,
        SurveyQuestionOption MultipleChoiceOptionB,
        SurveyQuestion MatrixQuestion,
        SurveyQuestionOption MatrixOptionA,
        SurveyQuestionOption MatrixOptionB,
        SurveyMatrixRow MatrixRowA,
        SurveyMatrixRow MatrixRowB,
        SurveyQuestion RatingQuestion);
}
