import type { ReactNode } from 'react';
import { Navigate, useLocation } from 'react-router-dom';
import { useAuth } from './AuthProvider';

interface ProtectedRouteProps {
  children: ReactNode;
  requiredPermission?: string;
}

export function ProtectedRoute({ children, requiredPermission }: ProtectedRouteProps) {
  const auth = useAuth();
  const location = useLocation();

  if (auth.isLoading) {
    return <AuthLoadingScreen />;
  }

  if (!auth.isAuthenticated) {
    return <Navigate replace state={{ from: location }} to="/login" />;
  }

  if (requiredPermission && !auth.hasPermission(requiredPermission)) {
    return <Navigate replace to="/app" />;
  }

  return children;
}

export function AuthLoadingScreen() {
  return (
    <main className="survey-shell">
      <section className="status-panel" aria-live="polite">
        <p>Verificando sesión...</p>
      </section>
    </main>
  );
}
