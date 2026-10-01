export type SurveyStatus = 'Draft' | 'Published' | 'Archived';

export type SurveyTarget = 'Student' | 'Teacher' | 'Institutional';

export type SurveyQuestionType =
  | 'SingleChoice'
  | 'MultipleChoice'
  | 'ShortText'
  | 'LongText'
  | 'RatingScale'
  | 'MatrixSingleChoice';

export interface SurveySummaryDto {
  id: string;
  versionGroupId: string;
  versionNumber: number;
  basedOnSurveyId: string | null;
  title: string;
  description: string | null;
  target: SurveyTarget;
  status: SurveyStatus;
  isAnonymous: boolean;
  isActive: boolean;
  sectionCount: number;
  questionCount: number;
  createdAtUtc: string;
  updatedAtUtc: string;
}

export interface SurveyDetailDto {
  id: string;
  createdByUserId: string;
  versionGroupId: string;
  versionNumber: number;
  basedOnSurveyId: string | null;
  title: string;
  description: string | null;
  target: SurveyTarget;
  status: SurveyStatus;
  isAnonymous: boolean;
  isActive: boolean;
  sections: SurveySectionDto[];
  createdAtUtc: string;
  updatedAtUtc: string;
}

export interface SurveyEditableVersionDto {
  survey: SurveyDetailDto;
  createdNewVersion: boolean;
  sourceSurveyId: string;
  versionGroupId: string;
  versionNumber: number;
}

export interface SurveySectionDto {
  id: string;
  title: string;
  description: string | null;
  order: number;
  isActive: boolean;
  questions: SurveyQuestionDto[];
}

export interface SurveyQuestionDto {
  id: string;
  text: string;
  type: SurveyQuestionType;
  isRequired: boolean;
  allowsComment: boolean;
  allowsOtherOption: boolean;
  order: number;
  isActive: boolean;
  options: SurveyQuestionOptionDto[];
  matrixRows: SurveyMatrixRowDto[];
  ratingMin: number | null;
  ratingMax: number | null;
}

export interface SurveyQuestionOptionDto {
  id: string;
  text: string;
  value: string;
  order: number;
  isActive: boolean;
}

export interface SurveyMatrixRowDto {
  id: string;
  text: string;
  order: number;
  isActive: boolean;
}

export interface SurveyFilters {
  includeInactive: boolean;
  status: SurveyStatus | '';
  target: SurveyTarget | '';
}

export interface CreateSurveyRequest {
  title: string;
  description: string | null;
  target: SurveyTarget;
  isAnonymous: boolean;
}

export type UpdateSurveyRequest = CreateSurveyRequest;

export interface CreateSurveySectionRequest {
  title: string;
  description: string | null;
  order: number;
}

export type UpdateSurveySectionRequest = CreateSurveySectionRequest;

export interface CreateSurveyQuestionRequest {
  text: string;
  type: SurveyQuestionType;
  isRequired: boolean;
  allowsComment: boolean;
  allowsOtherOption: boolean;
  order: number;
  ratingMin: number | null;
  ratingMax: number | null;
}

export type UpdateSurveyQuestionRequest = CreateSurveyQuestionRequest;

export interface CreateSurveyQuestionOptionRequest {
  text: string;
  value: string;
  order: number;
}

export type UpdateSurveyQuestionOptionRequest = CreateSurveyQuestionOptionRequest;

export interface CreateSurveyMatrixRowRequest {
  text: string;
  order: number;
}

export type UpdateSurveyMatrixRowRequest = CreateSurveyMatrixRowRequest;

export const SURVEY_STATUSES: SurveyStatus[] = ['Draft', 'Published', 'Archived'];

export const SURVEY_TARGETS: SurveyTarget[] = ['Student', 'Teacher', 'Institutional'];

export const SURVEY_QUESTION_TYPES: SurveyQuestionType[] = [
  'SingleChoice',
  'MultipleChoice',
  'ShortText',
  'LongText',
  'RatingScale',
  'MatrixSingleChoice'
];

export const SURVEY_STATUS_LABELS: Record<SurveyStatus, string> = {
  Draft: 'Borrador',
  Published: 'Publicada',
  Archived: 'Archivada'
};

export const SURVEY_TARGET_LABELS: Record<SurveyTarget, string> = {
  Student: 'Estudiantes',
  Teacher: 'Docentes',
  Institutional: 'Institucional'
};

export const SURVEY_QUESTION_TYPE_LABELS: Record<SurveyQuestionType, string> = {
  SingleChoice: 'Opción única',
  MultipleChoice: 'Opción múltiple',
  ShortText: 'Texto corto',
  LongText: 'Texto largo',
  RatingScale: 'Escala de valoración',
  MatrixSingleChoice: 'Matriz de opción única'
};
