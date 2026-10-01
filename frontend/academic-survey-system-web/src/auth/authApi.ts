import { apiRequest } from '../api/apiClient';
import type { AuthenticatedUser, LoginRequest, LoginResponse } from './types';

export function loginUser(request: LoginRequest): Promise<LoginResponse> {
  return apiRequest<LoginResponse>('/api/auth/login', {
    method: 'POST',
    body: request
  });
}

export function getCurrentUser(
  accessToken: string,
  onUnauthorized?: () => void
): Promise<AuthenticatedUser> {
  return apiRequest<AuthenticatedUser>('/api/auth/me', {
    token: accessToken,
    onUnauthorized
  });
}
