import { apiRequest } from './apiClient';
import type {
  ResultsAssignmentFilters,
  QuestionHistoryDto,
  SurveyAssignmentResultListItemDto,
  SurveyHistoryDto,
  SurveyQuestionResultsDto,
  SurveyResultsSummaryDto
} from '../types/results';

type UnauthorizedHandler = () => void;

export function getResultAssignments(
  accessToken: string,
  onUnauthorized: UnauthorizedHandler,
  filters: Partial<ResultsAssignmentFilters> = {},
  signal?: AbortSignal
): Promise<SurveyAssignmentResultListItemDto[]> {
  const searchParams = new URLSearchParams();

  appendOptionalFilter(searchParams, 'surveyId', filters.surveyId);
  appendOptionalFilter(searchParams, 'careerId', filters.careerId);
  appendOptionalFilter(searchParams, 'subjectId', filters.subjectId);
  appendOptionalFilter(searchParams, 'academicCycleId', filters.academicCycleId);
  appendOptionalFilter(searchParams, 'teacherId', filters.teacherId);

  const query = searchParams.toString();

  return apiRequest<SurveyAssignmentResultListItemDto[]>(
    `/api/results/survey-assignments${query ? `?${query}` : ''}`,
    {
      token: accessToken,
      onUnauthorized,
      signal
    }
  );
}

export function getSurveyAssignmentResultSummary(
  surveyAssignmentId: string,
  accessToken: string,
  onUnauthorized: UnauthorizedHandler,
  signal?: AbortSignal
): Promise<SurveyResultsSummaryDto> {
  return apiRequest<SurveyResultsSummaryDto>(
    `/api/results/survey-assignments/${encodeURIComponent(surveyAssignmentId)}/summary`,
    {
      token: accessToken,
      onUnauthorized,
      signal
    }
  );
}

export function getSurveyAssignmentQuestionResults(
  surveyAssignmentId: string,
  accessToken: string,
  onUnauthorized: UnauthorizedHandler,
  signal?: AbortSignal
): Promise<SurveyQuestionResultsDto[]> {
  return apiRequest<SurveyQuestionResultsDto[]>(
    `/api/results/survey-assignments/${encodeURIComponent(surveyAssignmentId)}/questions`,
    {
      token: accessToken,
      onUnauthorized,
      signal
    }
  );
}

export function getSurveyAssignmentQuestionResult(
  surveyAssignmentId: string,
  questionId: string,
  accessToken: string,
  onUnauthorized: UnauthorizedHandler,
  signal?: AbortSignal
): Promise<SurveyQuestionResultsDto> {
  return apiRequest<SurveyQuestionResultsDto>(
    `/api/results/survey-assignments/${encodeURIComponent(
      surveyAssignmentId
    )}/questions/${encodeURIComponent(questionId)}`,
    {
      token: accessToken,
      onUnauthorized,
      signal
    }
  );
}

export function getSurveySessionResultSummary(
  surveySessionId: string,
  accessToken: string,
  onUnauthorized: UnauthorizedHandler,
  signal?: AbortSignal
): Promise<SurveyResultsSummaryDto> {
  return apiRequest<SurveyResultsSummaryDto>(
    `/api/results/survey-sessions/${encodeURIComponent(surveySessionId)}/summary`,
    {
      token: accessToken,
      onUnauthorized,
      signal
    }
  );
}

export function getSurveyHistory(
  accessToken: string,
  onUnauthorized: UnauthorizedHandler,
  query: {
    careerId: string;
    subjectId: string;
    teacherId: string;
    surveyVersionGroupId: string;
  },
  signal?: AbortSignal
): Promise<SurveyHistoryDto> {
  const searchParams = new URLSearchParams(query);

  return apiRequest<SurveyHistoryDto>(`/api/results/history?${searchParams.toString()}`, {
    token: accessToken,
    onUnauthorized,
    signal
  });
}

export function getQuestionHistory(
  accessToken: string,
  onUnauthorized: UnauthorizedHandler,
  questionLineageId: string,
  query: {
    careerId: string;
    subjectId: string;
    teacherId: string;
    surveyVersionGroupId: string;
  },
  signal?: AbortSignal
): Promise<QuestionHistoryDto> {
  const searchParams = new URLSearchParams(query);

  return apiRequest<QuestionHistoryDto>(
    `/api/results/history/questions/${encodeURIComponent(questionLineageId)}?${searchParams.toString()}`,
    {
      token: accessToken,
      onUnauthorized,
      signal
    }
  );
}

function appendOptionalFilter(searchParams: URLSearchParams, key: string, value?: string) {
  if (value) {
    searchParams.set(key, value);
  }
}
