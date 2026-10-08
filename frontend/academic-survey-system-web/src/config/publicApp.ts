const configuredPublicAppUrl = import.meta.env.VITE_PUBLIC_APP_URL?.trim();

export function getPublicAppBaseUrl(): string {
  const candidate = configuredPublicAppUrl || window.location.origin;

  try {
    const url = new URL(candidate);

    if (url.protocol !== 'http:' && url.protocol !== 'https:') {
      return window.location.origin;
    }

    return url.origin;
  } catch {
    return window.location.origin;
  }
}

export function buildPublicSurveyUrl(accessCode: string): string {
  return `${getPublicAppBaseUrl()}/survey/${encodeURIComponent(accessCode)}`;
}

export function isLoopbackUrl(value: string): boolean {
  try {
    const hostname = new URL(value).hostname.toLowerCase();
    return hostname === 'localhost' || hostname === '127.0.0.1' || hostname === '::1' || hostname === '[::1]';
  } catch {
    return false;
  }
}
