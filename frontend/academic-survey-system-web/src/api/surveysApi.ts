import { apiRequest } from './apiClient';
import type {
  CreateSurveyMatrixRowRequest,
  CreateSurveyQuestionOptionRequest,
  CreateSurveyQuestionRequest,
  CreateSurveyRequest,
  CreateSurveySectionRequest,
  SurveyDetailDto,
  SurveyEditableVersionDto,
  SurveyFilters,
  SurveyMatrixRowDto,
  SurveyQuestionDto,
  SurveyQuestionOptionDto,
  SurveySectionDto,
  SurveySummaryDto,
  UpdateSurveyMatrixRowRequest,
  UpdateSurveyQuestionOptionRequest,
  UpdateSurveyQuestionRequest,
  UpdateSurveyRequest,
  UpdateSurveySectionRequest
} from '../types/surveys';

type UnauthorizedHandler = () => void;

export function getSurveys(
  filters: SurveyFilters,
  accessToken: string,
  onUnauthorized: UnauthorizedHandler
): Promise<SurveySummaryDto[]> {
  const searchParams = new URLSearchParams({
    includeInactive: String(filters.includeInactive)
  });

  if (filters.status) {
    searchParams.set('status', filters.status);
  }

  if (filters.target) {
    searchParams.set('target', filters.target);
  }

  return apiRequest<SurveySummaryDto[]>(`/api/surveys?${searchParams.toString()}`, {
    token: accessToken,
    onUnauthorized
  });
}

export function getSurvey(
  surveyId: string,
  accessToken: string,
  onUnauthorized: UnauthorizedHandler
): Promise<SurveyDetailDto> {
  return apiRequest<SurveyDetailDto>(`/api/surveys/${encodeURIComponent(surveyId)}`, {
    token: accessToken,
    onUnauthorized
  });
}

export function createSurvey(
  request: CreateSurveyRequest,
  accessToken: string,
  onUnauthorized: UnauthorizedHandler
): Promise<SurveyDetailDto> {
  return apiRequest<SurveyDetailDto>('/api/surveys', {
    method: 'POST',
    body: request,
    token: accessToken,
    onUnauthorized
  });
}

export function getOrCreateEditableSurveyVersion(
  surveyId: string,
  accessToken: string,
  onUnauthorized: UnauthorizedHandler
): Promise<SurveyEditableVersionDto> {
  return apiRequest<SurveyEditableVersionDto>(
    `/api/surveys/${encodeURIComponent(surveyId)}/editable-version`,
    {
      method: 'POST',
      token: accessToken,
      onUnauthorized
    }
  );
}

export function updateSurvey(
  surveyId: string,
  request: UpdateSurveyRequest,
  accessToken: string,
  onUnauthorized: UnauthorizedHandler
): Promise<null> {
  return apiRequest<null>(`/api/surveys/${encodeURIComponent(surveyId)}`, {
    method: 'PUT',
    body: request,
    token: accessToken,
    onUnauthorized
  });
}

export function publishSurvey(
  surveyId: string,
  accessToken: string,
  onUnauthorized: UnauthorizedHandler
): Promise<null> {
  return patchSurveyCommand(surveyId, 'publish', accessToken, onUnauthorized);
}

export function archiveSurvey(
  surveyId: string,
  accessToken: string,
  onUnauthorized: UnauthorizedHandler
): Promise<null> {
  return patchSurveyCommand(surveyId, 'archive', accessToken, onUnauthorized);
}

export function activateSurvey(
  surveyId: string,
  accessToken: string,
  onUnauthorized: UnauthorizedHandler
): Promise<null> {
  return patchSurveyCommand(surveyId, 'activate', accessToken, onUnauthorized);
}

export function deactivateSurvey(
  surveyId: string,
  accessToken: string,
  onUnauthorized: UnauthorizedHandler
): Promise<null> {
  return patchSurveyCommand(surveyId, 'deactivate', accessToken, onUnauthorized);
}

export function addSurveySection(
  surveyId: string,
  request: CreateSurveySectionRequest,
  accessToken: string,
  onUnauthorized: UnauthorizedHandler
): Promise<SurveyDetailDto> {
  return apiRequest<SurveyDetailDto>(`/api/surveys/${encodeURIComponent(surveyId)}/sections`, {
    method: 'POST',
    body: request,
    token: accessToken,
    onUnauthorized
  });
}

export function updateSurveySection(
  surveyId: string,
  sectionId: string,
  request: UpdateSurveySectionRequest,
  accessToken: string,
  onUnauthorized: UnauthorizedHandler
): Promise<null> {
  return apiRequest<null>(
    `/api/surveys/${encodeURIComponent(surveyId)}/sections/${encodeURIComponent(sectionId)}`,
    {
      method: 'PUT',
      body: request,
      token: accessToken,
      onUnauthorized
    }
  );
}

export function setSurveySectionActive(
  surveyId: string,
  sectionId: string,
  isActive: boolean,
  accessToken: string,
  onUnauthorized: UnauthorizedHandler
): Promise<null> {
  return patchNestedCommand(
    `/api/surveys/${encodeURIComponent(surveyId)}/sections/${encodeURIComponent(sectionId)}`,
    isActive,
    accessToken,
    onUnauthorized
  );
}

export function addSurveyQuestion(
  surveyId: string,
  sectionId: string,
  request: CreateSurveyQuestionRequest,
  accessToken: string,
  onUnauthorized: UnauthorizedHandler
): Promise<SurveyDetailDto> {
  return apiRequest<SurveyDetailDto>(
    `/api/surveys/${encodeURIComponent(surveyId)}/sections/${encodeURIComponent(sectionId)}/questions`,
    {
      method: 'POST',
      body: request,
      token: accessToken,
      onUnauthorized
    }
  );
}

export function updateSurveyQuestion(
  surveyId: string,
  sectionId: string,
  questionId: string,
  request: UpdateSurveyQuestionRequest,
  accessToken: string,
  onUnauthorized: UnauthorizedHandler
): Promise<null> {
  return apiRequest<null>(
    `/api/surveys/${encodeURIComponent(surveyId)}/sections/${encodeURIComponent(sectionId)}/questions/${encodeURIComponent(questionId)}`,
    {
      method: 'PUT',
      body: request,
      token: accessToken,
      onUnauthorized
    }
  );
}

export function setSurveyQuestionActive(
  surveyId: string,
  sectionId: string,
  questionId: string,
  isActive: boolean,
  accessToken: string,
  onUnauthorized: UnauthorizedHandler
): Promise<null> {
  return patchNestedCommand(
    `/api/surveys/${encodeURIComponent(surveyId)}/sections/${encodeURIComponent(sectionId)}/questions/${encodeURIComponent(questionId)}`,
    isActive,
    accessToken,
    onUnauthorized
  );
}

export function addSurveyQuestionOption(
  surveyId: string,
  sectionId: string,
  questionId: string,
  request: CreateSurveyQuestionOptionRequest,
  accessToken: string,
  onUnauthorized: UnauthorizedHandler
): Promise<SurveyDetailDto> {
  return apiRequest<SurveyDetailDto>(
    `/api/surveys/${encodeURIComponent(surveyId)}/sections/${encodeURIComponent(sectionId)}/questions/${encodeURIComponent(questionId)}/options`,
    {
      method: 'POST',
      body: request,
      token: accessToken,
      onUnauthorized
    }
  );
}

export function updateSurveyQuestionOption(
  surveyId: string,
  sectionId: string,
  questionId: string,
  optionId: string,
  request: UpdateSurveyQuestionOptionRequest,
  accessToken: string,
  onUnauthorized: UnauthorizedHandler
): Promise<SurveyDetailDto | null> {
  return apiRequest<SurveyDetailDto | null>(
    `/api/surveys/${encodeURIComponent(surveyId)}/sections/${encodeURIComponent(sectionId)}/questions/${encodeURIComponent(questionId)}/options/${encodeURIComponent(optionId)}`,
    {
      method: 'PUT',
      body: request,
      token: accessToken,
      onUnauthorized
    }
  );
}

export function setSurveyQuestionOptionActive(
  surveyId: string,
  sectionId: string,
  questionId: string,
  optionId: string,
  isActive: boolean,
  accessToken: string,
  onUnauthorized: UnauthorizedHandler
): Promise<null> {
  return patchNestedCommand(
    `/api/surveys/${encodeURIComponent(surveyId)}/sections/${encodeURIComponent(sectionId)}/questions/${encodeURIComponent(questionId)}/options/${encodeURIComponent(optionId)}`,
    isActive,
    accessToken,
    onUnauthorized
  );
}

export function addSurveyMatrixRow(
  surveyId: string,
  sectionId: string,
  questionId: string,
  request: CreateSurveyMatrixRowRequest,
  accessToken: string,
  onUnauthorized: UnauthorizedHandler
): Promise<SurveyDetailDto> {
  return apiRequest<SurveyDetailDto>(
    `/api/surveys/${encodeURIComponent(surveyId)}/sections/${encodeURIComponent(sectionId)}/questions/${encodeURIComponent(questionId)}/matrix-rows`,
    {
      method: 'POST',
      body: request,
      token: accessToken,
      onUnauthorized
    }
  );
}

export function updateSurveyMatrixRow(
  surveyId: string,
  sectionId: string,
  questionId: string,
  rowId: string,
  request: UpdateSurveyMatrixRowRequest,
  accessToken: string,
  onUnauthorized: UnauthorizedHandler
): Promise<SurveyDetailDto | null> {
  return apiRequest<SurveyDetailDto | null>(
    `/api/surveys/${encodeURIComponent(surveyId)}/sections/${encodeURIComponent(sectionId)}/questions/${encodeURIComponent(questionId)}/matrix-rows/${encodeURIComponent(rowId)}`,
    {
      method: 'PUT',
      body: request,
      token: accessToken,
      onUnauthorized
    }
  );
}

export function setSurveyMatrixRowActive(
  surveyId: string,
  sectionId: string,
  questionId: string,
  rowId: string,
  isActive: boolean,
  accessToken: string,
  onUnauthorized: UnauthorizedHandler
): Promise<null> {
  return patchNestedCommand(
    `/api/surveys/${encodeURIComponent(surveyId)}/sections/${encodeURIComponent(sectionId)}/questions/${encodeURIComponent(questionId)}/matrix-rows/${encodeURIComponent(rowId)}`,
    isActive,
    accessToken,
    onUnauthorized
  );
}

export function sortSurveyDetail(survey: SurveyDetailDto): SurveyDetailDto {
  return {
    ...survey,
    sections: [...survey.sections]
      .sort(compareByOrder)
      .map((section) => ({
        ...section,
        questions: [...section.questions]
          .sort(compareByOrder)
          .map((question) => ({
            ...question,
            options: [...question.options].sort(compareByOrder),
            matrixRows: [...question.matrixRows].sort(compareByOrder)
          }))
      }))
  };
}

function patchSurveyCommand(
  surveyId: string,
  command: 'publish' | 'archive' | 'activate' | 'deactivate',
  accessToken: string,
  onUnauthorized: UnauthorizedHandler
): Promise<null> {
  return apiRequest<null>(`/api/surveys/${encodeURIComponent(surveyId)}/${command}`, {
    method: 'PATCH',
    token: accessToken,
    onUnauthorized
  });
}

function patchNestedCommand(
  resourcePath: string,
  activate: boolean,
  accessToken: string,
  onUnauthorized: UnauthorizedHandler
): Promise<null> {
  return apiRequest<null>(`${resourcePath}/${activate ? 'activate' : 'deactivate'}`, {
    method: 'PATCH',
    token: accessToken,
    onUnauthorized
  });
}

function compareByOrder<T extends SurveySectionDto | SurveyQuestionDto | SurveyQuestionOptionDto | SurveyMatrixRowDto>(
  left: T,
  right: T
): number {
  return left.order - right.order;
}
