export interface AuthenticatedUser {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  roles: string[];
  permissions: string[];
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface LoginResponse {
  accessToken: string;
  tokenType: string;
  expiresAtUtc: string;
  user: AuthenticatedUser;
}

export interface AuthSession {
  accessToken: string;
  expiresAtUtc: string;
}
