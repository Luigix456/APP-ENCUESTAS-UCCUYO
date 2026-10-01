import { apiRequest, apiRequestBlob, type ApiBinaryResponse } from './apiClient';
import type { SurveyReportDto } from '../types/reports';

type UnauthorizedHandler = () => void;

export function getSurveyAssignmentReport(
  surveyAssignmentId: string,
  accessToken: string,
  onUnauthorized: UnauthorizedHandler,
  signal?: AbortSignal
): Promise<SurveyReportDto> {
  return apiRequest<SurveyReportDto>(
    `/api/reports/survey-assignments/${encodeURIComponent(surveyAssignmentId)}`,
    {
      token: accessToken,
      onUnauthorized,
      signal
    }
  );
}

export function downloadSurveyAssignmentReportPdf(
  surveyAssignmentId: string,
  accessToken: string,
  onUnauthorized: UnauthorizedHandler,
  signal?: AbortSignal
): Promise<ApiBinaryResponse> {
  return apiRequestBlob(
    `/api/reports/survey-assignments/${encodeURIComponent(surveyAssignmentId)}/pdf`,
    {
      token: accessToken,
      onUnauthorized,
      signal,
      accept: 'application/pdf'
    }
  );
}
