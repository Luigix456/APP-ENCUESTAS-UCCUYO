export type UserStatus = 'Active' | 'Inactive' | 'Blocked' | string;

export interface RoleDto {
  id: string;
  code: string;
  name: string;
  permissions: string[];
}

export interface UserRoleDto {
  id: string;
  code: string;
  name: string;
}

export interface UserDto {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  status: UserStatus;
  roles: UserRoleDto[];
  createdAtUtc: string;
  updatedAtUtc: string;
}

export interface UserCareerDto {
  careerId: string;
  careerCode: string;
  careerName: string;
  isActive: boolean;
  assignedAtUtc: string;
}

export interface CreateUserRequest {
  firstName: string;
  lastName: string;
  email: string;
  password: string;
  roleIds: string[];
}

export interface UpdateUserRequest {
  firstName: string;
  lastName: string;
  email: string;
}

export interface UpdateUserRolesRequest {
  roleIds: string[];
}

export interface UpdateUserCareersRequest {
  careerIds: string[];
}

export interface ResetUserPasswordRequest {
  newPassword: string;
}

export const USER_STATUS_LABELS: Record<string, string> = {
  Active: 'Activo/a',
  Inactive: 'Inactivo/a',
  Blocked: 'Bloqueado/a'
};
