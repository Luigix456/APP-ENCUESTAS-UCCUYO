import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
  type ReactNode
} from 'react';
import { getCurrentUser, loginUser } from './authApi';
import { clearAuthSession, getStoredAuthSession, saveAuthSession } from './authStorage';
import type { AuthSession, AuthenticatedUser } from './types';

type AuthStatus = 'checking' | 'authenticated' | 'anonymous';

interface AuthContextValue {
  user: AuthenticatedUser | null;
  accessToken: string | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  login: (email: string, password: string) => Promise<void>;
  logout: () => void;
  hasRole: (role: string) => boolean;
  hasPermission: (permission: string) => boolean;
}

interface AuthState {
  status: AuthStatus;
  user: AuthenticatedUser | null;
  session: AuthSession | null;
}

const AuthContext = createContext<AuthContextValue | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [state, setState] = useState<AuthState>({
    status: 'checking',
    user: null,
    session: null
  });

  const logout = useCallback(() => {
    clearAuthSession();
    setState({
      status: 'anonymous',
      user: null,
      session: null
    });
  }, []);

  useEffect(() => {
    let isCurrent = true;
    const storedSession = getStoredAuthSession();

    if (!storedSession) {
      setState({
        status: 'anonymous',
        user: null,
        session: null
      });
      return;
    }

    getCurrentUser(storedSession.accessToken, logout)
      .then((user) => {
        if (!isCurrent) {
          return;
        }

        setState({
          status: 'authenticated',
          user,
          session: storedSession
        });
      })
      .catch(() => {
        if (!isCurrent) {
          return;
        }

        clearAuthSession();
        setState({
          status: 'anonymous',
          user: null,
          session: null
        });
      });

    return () => {
      isCurrent = false;
    };
  }, [logout]);

  const login = useCallback(async (email: string, password: string) => {
    const response = await loginUser({
      email,
      password
    });
    const nextSession = {
      accessToken: response.accessToken,
      expiresAtUtc: response.expiresAtUtc
    };

    saveAuthSession(nextSession);
    setState({
      status: 'authenticated',
      user: response.user,
      session: nextSession
    });
  }, []);

  const value = useMemo<AuthContextValue>(
    () => ({
      user: state.user,
      accessToken: state.session?.accessToken ?? null,
      isAuthenticated: state.status === 'authenticated',
      isLoading: state.status === 'checking',
      login,
      logout,
      hasRole: (role: string) => state.user?.roles.includes(role) ?? false,
      hasPermission: (permission: string) => state.user?.permissions.includes(permission) ?? false
    }),
    [login, logout, state]
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthContextValue {
  const context = useContext(AuthContext);

  if (!context) {
    throw new Error('useAuth debe usarse dentro de AuthProvider.');
  }

  return context;
}
