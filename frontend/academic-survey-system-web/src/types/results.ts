export interface SurveyAssignmentResultListItemDto {
  surveyAssignmentId: string;
  surveyId: string;
  surveyVersionGroupId: string;
  surveyVersionNumber: number;
  surveyTitle: string;
  careerId: string;
  careerName: string;
  subjectId: string;
  subjectName: string;
  academicCycleId: string;
  academicCycleYear: number;
  academicCyclePeriod: string;
  teacherId: string;
  teacherFullName: string;
  teachingRole: string;
  isActive: boolean;
  totalSessions: number;
  totalResponses: number;
  firstSubmittedAtUtc: string | null;
  lastSubmittedAtUtc: string | null;
  expectedRespondentCount: number | null;
  participationPercentage: number | null;
}

export interface SurveyHistoryDto {
  careerId: string;
  careerName: string;
  subjectId: string;
  subjectName: string;
  teacherId: string;
  teacherName: string;
  surveyVersionGroupId: string;
  surveyTitle: string;
  points: SurveyHistoryPointDto[];
  questions: SurveyHistoryQuestionDto[];
}

export interface SurveyHistoryPointDto {
  surveyAssignmentId: string;
  academicCycleId: string;
  academicCycleName: string;
  academicCycleYear: number;
  surveyId: string;
  surveyVersionNumber: number;
  expectedRespondentCount: number | null;
  responseCount: number;
  remainingCount: number | null;
  participationPercentage: number | null;
  sessionCount: number;
  detailedResultsAvailable: boolean;
}

export interface SurveyHistoryQuestionDto {
  questionLineageId: string;
  latestQuestionText: string;
  questionTextsChanged: boolean;
  questionType: string;
  comparisonSupported: boolean;
  unsupportedReason: string | null;
}

export interface QuestionHistoryDto {
  questionLineageId: string;
  latestQuestionText: string;
  questionTextsChanged: boolean;
  questionType: string;
  comparisonSupported: boolean;
  unsupportedReason: string | null;
  points: QuestionHistoryPointDto[];
}

export interface QuestionHistoryPointDto {
  surveyAssignmentId: string;
  academicCycleId: string;
  academicCycleName: string;
  academicCycleYear: number;
  surveyVersionNumber: number;
  questionId: string;
  questionText: string;
  responseCount: number;
  detailedResultsAvailable: boolean;
  minimumResponsesRequired: number;
  responsesNeededToUnlock: number;
  distribution: QuestionHistoryOptionDistributionDto[] | null;
  averageRating: number | null;
  minRating: number | null;
  maxRating: number | null;
  ratingScaleChanged: boolean;
}

export interface QuestionHistoryOptionDistributionDto {
  label: string;
  count: number;
  percentage: number;
}

export interface ResultsAssignmentFilters {
  surveyId: string;
  careerId: string;
  subjectId: string;
  academicCycleId: string;
  teacherId: string;
}

export interface SurveyResultsSummaryDto {
  surveyAssignmentId: string;
  surveyId: string;
  surveyTitle: string;
  careerId: string;
  careerName: string;
  subjectId: string;
  subjectName: string;
  academicCycleId: string;
  academicCycleYear: number;
  academicCyclePeriod: string;
  teacherId: string;
  teacherFullName: string;
  totalResponses: number;
  totalSessions: number;
  firstSubmittedAtUtc: string | null;
  lastSubmittedAtUtc: string | null;
  expectedRespondentCount: number | null;
  remainingCount: number | null;
  participationPercentage: number | null;
  detailedResultsAvailable: boolean;
  minimumResponsesRequired: number;
  responsesNeededToUnlock: number;
}

export type SurveyQuestionResultType =
  | 'SingleChoice'
  | 'MultipleChoice'
  | 'ShortText'
  | 'LongText'
  | 'RatingScale'
  | 'MatrixSingleChoice'
  | string;

export interface SurveyQuestionResultsDto {
  questionId: string;
  text: string;
  type: SurveyQuestionResultType;
  order: number;
  responseCount: number;
  choice: SurveyChoiceResultsDto | null;
  textValues: SurveyTextResultsDto | null;
  rating: SurveyRatingResultsDto | null;
  matrix: SurveyMatrixResultsDto | null;
  comments: SurveyQuestionCommentDto[];
}

export interface SurveyChoiceResultsDto {
  options: SurveyOptionDistributionDto[];
  other: SurveyOtherOptionResultsDto | null;
}

export interface SurveyOtherOptionResultsDto {
  count: number;
  percentage: number;
  values: string[];
}

export interface SurveyOptionDistributionDto {
  optionId: string;
  text: string;
  value: string;
  count: number;
  percentage: number;
}

export interface SurveyTextResultsDto {
  responseCount: number;
  values: string[];
}

export interface SurveyRatingResultsDto {
  responseCount: number;
  average: number | null;
  minimumObserved: number | null;
  maximumObserved: number | null;
  distribution: SurveyRatingDistributionDto[];
  configuredMinimum: number | null;
  configuredMaximum: number | null;
}

export interface SurveyRatingDistributionDto {
  value: number;
  count: number;
  percentage: number;
}

export interface SurveyMatrixResultsDto {
  rows: SurveyMatrixRowResultsDto[];
}

export interface SurveyMatrixRowResultsDto {
  rowId: string;
  rowText: string;
  totalResponses: number;
  options: SurveyOptionDistributionDto[];
}

export interface SurveyQuestionCommentDto {
  answerId: string;
  comment: string;
}
