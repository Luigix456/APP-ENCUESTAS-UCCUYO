import { apiRequest } from './apiClient';
import type {
  CreateUserRequest,
  ResetUserPasswordRequest,
  RoleDto,
  UpdateUserCareersRequest,
  UpdateUserRequest,
  UpdateUserRolesRequest,
  UserCareerDto,
  UserDto
} from '../types/users';

type UnauthorizedHandler = () => void;

interface IdentityRequestOptions {
  accessToken: string;
  onUnauthorized: UnauthorizedHandler;
  signal?: AbortSignal;
}

export function getUsers({
  accessToken,
  onUnauthorized,
  signal,
  includeInactive = true
}: IdentityRequestOptions & { includeInactive?: boolean }): Promise<UserDto[]> {
  return apiRequest<UserDto[]>(`/api/identity/users?includeInactive=${includeInactive}`, {
    token: accessToken,
    onUnauthorized,
    signal
  });
}

export function getUser(
  userId: string,
  { accessToken, onUnauthorized, signal }: IdentityRequestOptions
): Promise<UserDto> {
  return apiRequest<UserDto>(`/api/identity/users/${encodeURIComponent(userId)}`, {
    token: accessToken,
    onUnauthorized,
    signal
  });
}

export function createUser(
  request: CreateUserRequest,
  { accessToken, onUnauthorized }: IdentityRequestOptions
): Promise<UserDto> {
  return apiRequest<UserDto>('/api/identity/users', {
    method: 'POST',
    body: request,
    token: accessToken,
    onUnauthorized
  });
}

export function updateUser(
  userId: string,
  request: UpdateUserRequest,
  { accessToken, onUnauthorized }: IdentityRequestOptions
): Promise<null> {
  return apiRequest<null>(`/api/identity/users/${encodeURIComponent(userId)}`, {
    method: 'PUT',
    body: request,
    token: accessToken,
    onUnauthorized
  });
}

export function activateUser(userId: string, options: IdentityRequestOptions): Promise<null> {
  return patchUserCommand(userId, 'activate', options);
}

export function deactivateUser(userId: string, options: IdentityRequestOptions): Promise<null> {
  return patchUserCommand(userId, 'deactivate', options);
}

export function deleteUser(
  userId: string,
  { accessToken, onUnauthorized }: IdentityRequestOptions
): Promise<null> {
  return apiRequest<null>(`/api/identity/users/${encodeURIComponent(userId)}`, {
    method: 'DELETE',
    token: accessToken,
    onUnauthorized
  });
}

export function resetUserPassword(
  userId: string,
  request: ResetUserPasswordRequest,
  { accessToken, onUnauthorized }: IdentityRequestOptions
): Promise<null> {
  return apiRequest<null>(`/api/identity/users/${encodeURIComponent(userId)}/password`, {
    method: 'PUT',
    body: request,
    token: accessToken,
    onUnauthorized
  });
}

export function getRoles({
  accessToken,
  onUnauthorized,
  signal
}: IdentityRequestOptions): Promise<RoleDto[]> {
  return apiRequest<RoleDto[]>('/api/identity/roles', {
    token: accessToken,
    onUnauthorized,
    signal
  });
}

export function replaceUserRoles(
  userId: string,
  request: UpdateUserRolesRequest,
  { accessToken, onUnauthorized }: IdentityRequestOptions
): Promise<null> {
  return apiRequest<null>(`/api/identity/users/${encodeURIComponent(userId)}/roles`, {
    method: 'PUT',
    body: request,
    token: accessToken,
    onUnauthorized
  });
}

export function getUserCareers(
  userId: string,
  { accessToken, onUnauthorized, signal }: IdentityRequestOptions
): Promise<UserCareerDto[]> {
  return apiRequest<UserCareerDto[]>(`/api/identity/users/${encodeURIComponent(userId)}/careers`, {
    token: accessToken,
    onUnauthorized,
    signal
  });
}

export function replaceUserCareers(
  userId: string,
  request: UpdateUserCareersRequest,
  { accessToken, onUnauthorized }: IdentityRequestOptions
): Promise<null> {
  return apiRequest<null>(`/api/identity/users/${encodeURIComponent(userId)}/careers`, {
    method: 'PUT',
    body: request,
    token: accessToken,
    onUnauthorized
  });
}

function patchUserCommand(
  userId: string,
  command: 'activate' | 'deactivate',
  { accessToken, onUnauthorized }: IdentityRequestOptions
): Promise<null> {
  return apiRequest<null>(`/api/identity/users/${encodeURIComponent(userId)}/${command}`, {
    method: 'PATCH',
    token: accessToken,
    onUnauthorized
  });
}
