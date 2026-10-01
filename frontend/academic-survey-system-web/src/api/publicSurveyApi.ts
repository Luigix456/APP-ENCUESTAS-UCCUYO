import type {
  PublicSurveySessionDto,
  SubmitSurveyResponseRequest
} from '../types/publicSurvey';

interface ApiErrorPayload {
  errors?: Array<{
    code?: string;
    message?: string;
  }> | Record<string, string[]>;
  code?: string;
  detail?: string;
  extensions?: {
    code?: string;
    [key: string]: unknown;
  };
  message?: string;
  status?: number;
  title?: string;
  type?: string;
}

export class ApiError extends Error {
  public readonly status: number;
  public readonly code: string | null;
  public readonly title: string | null;
  public readonly detail: string | null;
  public readonly details: string[];

  public constructor(
    status: number,
    message: string,
    code: string | null,
    title: string | null,
    detail: string | null,
    details: string[]
  ) {
    super(message);
    this.name = 'ApiError';
    this.status = status;
    this.code = code;
    this.title = title;
    this.detail = detail;
    this.details = details;
  }
}

export async function getPublicSurvey(accessCode: string): Promise<PublicSurveySessionDto> {
  const response = await fetch(`/api/public/survey-sessions/${encodeURIComponent(accessCode)}`, {
    headers: {
      Accept: 'application/json'
    }
  });

  return readJsonResponse<PublicSurveySessionDto>(response);
}

export async function submitSurveyResponse(
  accessCode: string,
  request: SubmitSurveyResponseRequest
): Promise<void> {
  const response = await fetch(`/api/public/survey-sessions/${encodeURIComponent(accessCode)}/responses`, {
    method: 'POST',
    headers: {
      Accept: 'application/json',
      'Content-Type': 'application/json'
    },
    body: JSON.stringify(request)
  });

  await readOptionalJsonResponse(response);
}

async function readJsonResponse<T>(response: Response): Promise<T> {
  if (!response.ok) {
    throw await createApiError(response);
  }

  return response.json() as Promise<T>;
}

async function readOptionalJsonResponse(response: Response): Promise<unknown | null> {
  if (!response.ok) {
    throw await createApiError(response);
  }

  if (response.status === 204) {
    return null;
  }

  const text = await response.text();
  return text ? (JSON.parse(text) as unknown) : null;
}

async function createApiError(response: Response): Promise<ApiError> {
  const payload = await readErrorPayload(response);
  const errors = normalizeErrors(payload?.errors);
  const firstError = errors[0];
  const details = errors
    .map((error) => error.message)
    .filter((message): message is string => Boolean(message?.trim()));
  const detail = payload?.detail ?? null;
  const title = payload?.title ?? null;
  const code = firstError?.code ?? payload?.code ?? payload?.extensions?.code ?? null;
  const message =
    firstError?.message ??
    payload?.message ??
    detail ??
    title ??
    `La API devolvio HTTP ${response.status}.`;

  return new ApiError(response.status, message, code, title, detail, details);
}

function normalizeErrors(
  errors: ApiErrorPayload['errors']
): Array<{ code?: string; message?: string }> {
  if (!errors) {
    return [];
  }

  if (Array.isArray(errors)) {
    return errors;
  }

  return Object.entries(errors).flatMap(([code, messages]) =>
    messages.map((message) => ({
      code,
      message
    }))
  );
}

async function readErrorPayload(response: Response): Promise<ApiErrorPayload | null> {
  const text = await response.text();

  if (!text) {
    return null;
  }

  try {
    return JSON.parse(text) as ApiErrorPayload;
  } catch {
    return { message: text };
  }
}
