import type { ReactNode } from 'react';
import { Navigate, useLocation } from 'react-router-dom';
import { useAuth } from './AuthProvider';

interface ProtectedRouteProps {
  children: ReactNode;
}

export function ProtectedRoute({ children }: ProtectedRouteProps) {
  const auth = useAuth();
  const location = useLocation();

  if (auth.isLoading) {
    return <AuthLoadingScreen />;
  }

  if (!auth.isAuthenticated) {
    return <Navigate replace state={{ from: location }} to="/login" />;
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
