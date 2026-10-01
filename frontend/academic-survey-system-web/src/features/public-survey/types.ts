import type {
  PublicSurveyQuestionDto,
  SubmitSurveyAnswerRequest
} from '../../types/publicSurvey';

export interface QuestionAnswerDraft {
  optionIds: string[];
  textValue: string;
  numericValue: number | null;
  comment: string;
  matrixAnswers: Record<string, string>;
  otherSelected: boolean;
  otherText: string;
}

export type AnswerState = Record<string, QuestionAnswerDraft>;

export type ValidationErrors = Record<string, string>;

export interface QuestionInputProps {
  question: PublicSurveyQuestionDto;
  value: QuestionAnswerDraft;
  error?: string;
  onChange: (nextValue: Partial<QuestionAnswerDraft>) => void;
}

export function createEmptyAnswer(): QuestionAnswerDraft {
  return {
    optionIds: [],
    textValue: '',
    numericValue: null,
    comment: '',
    matrixAnswers: {},
    otherSelected: false,
    otherText: ''
  };
}

export function validateQuestion(
  question: PublicSurveyQuestionDto,
  answer: QuestionAnswerDraft
): string | null {
  const otherText = answer.otherText.trim();

  if (question.type === 'RatingScale' && !hasValidRatingScale(question)) {
    return 'Esta pregunta no tiene una escala configurada.';
  }

  if (question.allowsOtherOption && answer.otherSelected && !otherText) {
    return 'Ingresá una respuesta para la opción Otro.';
  }

  if (!question.isRequired) {
    return null;
  }

  if (!hasPrimaryAnswer(question, answer)) {
    return 'Esta pregunta es obligatoria.';
  }

  return null;
}

export function toSubmitAnswer(
  question: PublicSurveyQuestionDto,
  answer: QuestionAnswerDraft
): SubmitSurveyAnswerRequest | null {
  if (!hasPrimaryAnswer(question, answer)) {
    return null;
  }

  const comment = question.allowsComment ? trimmedOrNull(answer.comment) : null;

  switch (question.type) {
    case 'SingleChoice':
      return {
        questionId: question.id,
        optionIds: answer.otherSelected ? [] : answer.optionIds.slice(0, 1),
        textValue: null,
        numericValue: null,
        comment,
        matrixAnswers: null,
        otherText: answer.otherSelected ? trimmedOrNull(answer.otherText) : null
      };
    case 'MultipleChoice':
      return {
        questionId: question.id,
        optionIds: answer.optionIds,
        textValue: null,
        numericValue: null,
        comment,
        matrixAnswers: null,
        otherText: answer.otherSelected ? trimmedOrNull(answer.otherText) : null
      };
    case 'ShortText':
    case 'LongText':
      return {
        questionId: question.id,
        optionIds: null,
        textValue: answer.textValue.trim(),
        numericValue: null,
        comment,
        matrixAnswers: null,
        otherText: null
      };
    case 'RatingScale':
      return {
        questionId: question.id,
        optionIds: null,
        textValue: null,
        numericValue: answer.numericValue,
        comment,
        matrixAnswers: null,
        otherText: null
      };
    case 'MatrixSingleChoice':
      return {
        questionId: question.id,
        optionIds: null,
        textValue: null,
        numericValue: null,
        comment,
        matrixAnswers: question.matrixRows.map((row) => ({
          rowId: row.id,
          optionId: answer.matrixAnswers[row.id]
        })),
        otherText: null
      };
  }
}

export function hasPrimaryAnswer(
  question: PublicSurveyQuestionDto,
  answer: QuestionAnswerDraft
): boolean {
  switch (question.type) {
    case 'SingleChoice':
      return answer.optionIds.length > 0 || (answer.otherSelected && Boolean(answer.otherText.trim()));
    case 'MultipleChoice':
      return answer.optionIds.length > 0 || (answer.otherSelected && Boolean(answer.otherText.trim()));
    case 'ShortText':
    case 'LongText':
      return Boolean(answer.textValue.trim());
    case 'RatingScale':
      return answer.numericValue !== null;
    case 'MatrixSingleChoice':
      return (
        question.matrixRows.length > 0 &&
        question.matrixRows.every((row) => Boolean(answer.matrixAnswers[row.id]))
      );
  }
}

function hasValidRatingScale(question: PublicSurveyQuestionDto): boolean {
  return (
    question.ratingMin !== null &&
    question.ratingMax !== null &&
    question.ratingMin <= question.ratingMax
  );
}

function trimmedOrNull(value: string): string | null {
  const trimmed = value.trim();
  return trimmed ? trimmed : null;
}
