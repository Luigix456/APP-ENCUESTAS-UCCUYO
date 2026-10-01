import type { AuthSession } from './types';

const AUTH_STORAGE_KEY = 'academicSurvey.auth';

export function saveAuthSession(session: AuthSession): void {
  sessionStorage.setItem(AUTH_STORAGE_KEY, JSON.stringify(session));
}

export function getStoredAuthSession(): AuthSession | null {
  const rawValue = sessionStorage.getItem(AUTH_STORAGE_KEY);

  if (!rawValue) {
    return null;
  }

  try {
    const parsedValue = JSON.parse(rawValue) as Partial<AuthSession>;

    if (
      typeof parsedValue.accessToken !== 'string' ||
      typeof parsedValue.expiresAtUtc !== 'string'
    ) {
      clearAuthSession();
      return null;
    }

    const session = {
      accessToken: parsedValue.accessToken,
      expiresAtUtc: parsedValue.expiresAtUtc
    };

    if (isSessionExpired(session)) {
      clearAuthSession();
      return null;
    }

    return session;
  } catch {
    clearAuthSession();
    return null;
  }
}

export function clearAuthSession(): void {
  sessionStorage.removeItem(AUTH_STORAGE_KEY);
}

export function isSessionExpired(session: AuthSession): boolean {
  const expiresAt = Date.parse(session.expiresAtUtc);

  if (Number.isNaN(expiresAt)) {
    return true;
  }

  return expiresAt <= Date.now();
}
