export class ApiClientError extends Error {
  public readonly status: number;
  public readonly code: string | null;

  public constructor(status: number, message: string, code: string | null = null) {
    super(message);
    this.name = 'ApiClientError';
    this.status = status;
    this.code = code;
  }
}

interface ApiRequestOptions {
  method?: string;
  body?: unknown;
  token?: string;
  onUnauthorized?: () => void;
  signal?: AbortSignal;
  accept?: string;
}

export interface ApiBinaryResponse {
  blob: Blob;
  contentDisposition: string | null;
  contentType: string | null;
}

interface ApiErrorPayload {
  message?: string;
  title?: string;
  errors?: string[] | Array<{ code?: string; message?: string }>;
}

export async function apiRequest<T>(
  path: string,
  { method = 'GET', body, token, onUnauthorized, signal, accept = 'application/json' }: ApiRequestOptions = {}
): Promise<T> {
  const headers: HeadersInit = {
    Accept: accept
  };

  if (body !== undefined) {
    headers['Content-Type'] = 'application/json';
  }

  if (token) {
    headers.Authorization = `Bearer ${token}`;
  }

  const response = await fetch(path, {
    method,
    headers,
    signal,
    body: body === undefined ? undefined : JSON.stringify(body)
  });

  if (!response.ok) {
    if (response.status === 401 && token) {
      onUnauthorized?.();
    }

    throw await createApiClientError(response);
  }

  if (response.status === 204) {
    return null as T;
  }

  const text = await response.text();
  return text ? (JSON.parse(text) as T) : (null as T);
}

export async function apiRequestBlob(
  path: string,
  { method = 'GET', body, token, onUnauthorized, signal, accept = 'application/octet-stream' }: ApiRequestOptions = {}
): Promise<ApiBinaryResponse> {
  const headers: HeadersInit = {
    Accept: accept
  };

  if (body !== undefined) {
    headers['Content-Type'] = 'application/json';
  }

  if (token) {
    headers.Authorization = `Bearer ${token}`;
  }

  const response = await fetch(path, {
    method,
    headers,
    signal,
    body: body === undefined ? undefined : JSON.stringify(body)
  });

  if (!response.ok) {
    if (response.status === 401 && token) {
      onUnauthorized?.();
    }

    throw await createApiClientError(response);
  }

  return {
    blob: await response.blob(),
    contentDisposition: response.headers.get('Content-Disposition'),
    contentType: response.headers.get('Content-Type')
  };
}

async function createApiClientError(response: Response): Promise<ApiClientError> {
  const payload = await readErrorPayload(response);
  const firstError = Array.isArray(payload?.errors) ? payload.errors[0] : null;
  const firstErrorMessage =
    typeof firstError === 'string'
      ? firstError
      : typeof firstError?.message === 'string'
        ? firstError.message
        : null;
  const firstErrorCode = typeof firstError === 'string' ? null : firstError?.code ?? null;
  const message =
    firstErrorMessage ??
    payload?.message ??
    payload?.title ??
    `La API devolvio HTTP ${response.status}.`;

  return new ApiClientError(response.status, message, firstErrorCode);
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
