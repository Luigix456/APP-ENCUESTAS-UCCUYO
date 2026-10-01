import { apiRequest } from './apiClient';
import type { CreateSurveySessionRequest, SurveySessionDto } from '../types/surveyOperations';

export interface SurveySessionFilters {
  includeInactive?: boolean;
  status?: string;
  surveyAssignmentId?: string;
  accessCode?: string;
  careerId?: string;
  academicCycleId?: string;
  subjectId?: string;
  teacherId?: string;
}

export function getSurveySessions(
  accessToken: string,
  onUnauthorized: () => void,
  filters: SurveySessionFilters = {},
  signal?: AbortSignal
): Promise<SurveySessionDto[]> {
  const searchParams = new URLSearchParams({
    includeInactive: String(filters.includeInactive ?? false)
  });

  appendOptionalFilter(searchParams, 'status', filters.status);
  appendOptionalFilter(searchParams, 'surveyAssignmentId', filters.surveyAssignmentId);
  appendOptionalFilter(searchParams, 'accessCode', filters.accessCode);
  appendOptionalFilter(searchParams, 'careerId', filters.careerId);
  appendOptionalFilter(searchParams, 'academicCycleId', filters.academicCycleId);
  appendOptionalFilter(searchParams, 'subjectId', filters.subjectId);
  appendOptionalFilter(searchParams, 'teacherId', filters.teacherId);

  return apiRequest<SurveySessionDto[]>(`/api/survey-sessions?${searchParams.toString()}`, {
    token: accessToken,
    onUnauthorized,
    signal
  });
}

export function getSurveySession(
  id: string,
  accessToken: string,
  onUnauthorized: () => void
): Promise<SurveySessionDto> {
  return apiRequest<SurveySessionDto>(`/api/survey-sessions/${encodeURIComponent(id)}`, {
    token: accessToken,
    onUnauthorized
  });
}

export function createSurveySession(
  request: CreateSurveySessionRequest,
  accessToken: string,
  onUnauthorized: () => void
): Promise<SurveySessionDto> {
  return apiRequest<SurveySessionDto>('/api/survey-sessions', {
    method: 'POST',
    body: request,
    token: accessToken,
    onUnauthorized
  });
}

export function openSurveySession(
  id: string,
  accessToken: string,
  onUnauthorized: () => void
): Promise<null> {
  return apiRequest<null>(`/api/survey-sessions/${encodeURIComponent(id)}/open`, {
    method: 'PATCH',
    token: accessToken,
    onUnauthorized
  });
}

export function closeSurveySession(
  id: string,
  accessToken: string,
  onUnauthorized: () => void
): Promise<null> {
  return apiRequest<null>(`/api/survey-sessions/${encodeURIComponent(id)}/close`, {
    method: 'PATCH',
    token: accessToken,
    onUnauthorized
  });
}

function appendOptionalFilter(searchParams: URLSearchParams, key: string, value?: string) {
  if (value) {
    searchParams.set(key, value);
  }
}
