export type SurveyQuestionType =
  | 'SingleChoice'
  | 'MultipleChoice'
  | 'ShortText'
  | 'LongText'
  | 'RatingScale'
  | 'MatrixSingleChoice';

export interface PublicSurveySessionDto {
  sessionId: string;
  accessCode: string;
  expiresAtUtc: string;
  surveyId: string;
  surveyTitle: string;
  surveyDescription: string | null;
  surveyTarget: string;
  careerName: string;
  subjectName: string;
  academicCycleYear: number;
  academicCyclePeriod: string;
  teacherFullName: string;
  teachingRole: string;
  sections: PublicSurveySectionDto[];
}

export interface PublicSurveySectionDto {
  id: string;
  title: string;
  description: string | null;
  order: number;
  questions: PublicSurveyQuestionDto[];
}

export interface PublicSurveyQuestionDto {
  id: string;
  text: string;
  type: SurveyQuestionType;
  isRequired: boolean;
  allowsComment: boolean;
  allowsOtherOption: boolean;
  order: number;
  options: PublicSurveyQuestionOptionDto[];
  matrixRows: PublicSurveyMatrixRowDto[];
  ratingMin: number | null;
  ratingMax: number | null;
}

export interface PublicSurveyQuestionOptionDto {
  id: string;
  text: string;
  value: string;
  order: number;
}

export interface PublicSurveyMatrixRowDto {
  id: string;
  text: string;
  order: number;
}

export interface SubmitSurveyResponseRequest {
  answers: SubmitSurveyAnswerRequest[];
}

export interface SubmitSurveyAnswerRequest {
  questionId: string;
  optionIds: string[] | null;
  textValue: string | null;
  numericValue: number | null;
  comment: string | null;
  matrixAnswers: SubmitSurveyMatrixAnswerRequest[] | null;
  otherText: string | null;
}

export interface SubmitSurveyMatrixAnswerRequest {
  rowId: string;
  optionId: string;
}
