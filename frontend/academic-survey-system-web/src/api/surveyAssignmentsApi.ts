import { apiRequest } from './apiClient';
import type {
  CreateSurveyAssignmentRequest,
  SurveyAssignmentDto,
  SurveyAssignmentFilters
} from '../types/surveyAssignments';

type UnauthorizedHandler = () => void;

const defaultFilters: SurveyAssignmentFilters = {
  includeInactive: false,
  surveyId: '',
  careerId: '',
  subjectId: '',
  academicCycleId: '',
  teacherId: ''
};

export function getSurveyAssignments(
  accessToken: string,
  onUnauthorized: UnauthorizedHandler,
  filters: Partial<SurveyAssignmentFilters> = {}
): Promise<SurveyAssignmentDto[]> {
  const nextFilters = {
    ...defaultFilters,
    ...filters
  };
  const searchParams = new URLSearchParams({
    includeInactive: String(nextFilters.includeInactive)
  });

  if (nextFilters.surveyId) {
    searchParams.set('surveyId', nextFilters.surveyId);
  }

  if (nextFilters.careerId) {
    searchParams.set('careerId', nextFilters.careerId);
  }

  if (nextFilters.subjectId) {
    searchParams.set('subjectId', nextFilters.subjectId);
  }

  if (nextFilters.academicCycleId) {
    searchParams.set('academicCycleId', nextFilters.academicCycleId);
  }

  if (nextFilters.teacherId) {
    searchParams.set('teacherId', nextFilters.teacherId);
  }

  return apiRequest<SurveyAssignmentDto[]>(`/api/survey-assignments?${searchParams.toString()}`, {
    token: accessToken,
    onUnauthorized
  });
}

export function getSurveyAssignment(
  id: string,
  accessToken: string,
  onUnauthorized: UnauthorizedHandler
): Promise<SurveyAssignmentDto> {
  return apiRequest<SurveyAssignmentDto>(`/api/survey-assignments/${encodeURIComponent(id)}`, {
    token: accessToken,
    onUnauthorized
  });
}

export function createSurveyAssignment(
  request: CreateSurveyAssignmentRequest,
  accessToken: string,
  onUnauthorized: UnauthorizedHandler
): Promise<SurveyAssignmentDto> {
  return apiRequest<SurveyAssignmentDto>('/api/survey-assignments', {
    method: 'POST',
    body: request,
    token: accessToken,
    onUnauthorized
  });
}

export function activateSurveyAssignment(
  id: string,
  accessToken: string,
  onUnauthorized: UnauthorizedHandler
): Promise<null> {
  return patchAssignmentState(id, 'activate', accessToken, onUnauthorized);
}

export function deactivateSurveyAssignment(
  id: string,
  accessToken: string,
  onUnauthorized: UnauthorizedHandler
): Promise<null> {
  return patchAssignmentState(id, 'deactivate', accessToken, onUnauthorized);
}

function patchAssignmentState(
  id: string,
  action: 'activate' | 'deactivate',
  accessToken: string,
  onUnauthorized: UnauthorizedHandler
): Promise<null> {
  return apiRequest<null>(`/api/survey-assignments/${encodeURIComponent(id)}/${action}`, {
    method: 'PATCH',
    token: accessToken,
    onUnauthorized
  });
}
