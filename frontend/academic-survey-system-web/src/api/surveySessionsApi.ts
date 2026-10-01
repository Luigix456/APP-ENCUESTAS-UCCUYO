import { apiRequest } from './apiClient';
import type { CreateSurveySessionRequest, SurveySessionDto } from '../types/surveyOperations';

export function getSurveySessions(
  accessToken: string,
  onUnauthorized: () => void
): Promise<SurveySessionDto[]> {
  return apiRequest<SurveySessionDto[]>('/api/survey-sessions?includeInactive=false', {
    token: accessToken,
    onUnauthorized
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
