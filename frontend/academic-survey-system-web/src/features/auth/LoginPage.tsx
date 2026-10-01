import { useState, type FormEvent } from 'react';
import { Navigate, useLocation, useNavigate } from 'react-router-dom';
import { ApiClientError } from '../../api/apiClient';
import { AuthLoadingScreen } from '../../auth/ProtectedRoute';
import { useAuth } from '../../auth/AuthProvider';

interface LocationState {
  from?: {
    pathname?: string;
  };
}

export function LoginPage() {
  const auth = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  if (auth.isLoading) {
    return <AuthLoadingScreen />;
  }

  if (auth.isAuthenticated) {
    return <Navigate replace to="/app" />;
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError(null);
    setIsSubmitting(true);

    try {
      await auth.login(email.trim(), password);
      setPassword('');
      const locationState = location.state as LocationState | null;
      navigate(locationState?.from?.pathname ?? '/app', { replace: true });
    } catch (loginError) {
      setError(getLoginErrorMessage(loginError));
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <main className="auth-shell">
      <section className="login-card">
        <p className="eyebrow">Sistema Web de Gestión de Encuestas Académicas</p>
        <h1>Ingresar</h1>
        <p>Accedé al área privada con tu cuenta institucional.</p>

        <form className="login-form" noValidate onSubmit={handleSubmit}>
          <label>
            <span>Correo electrónico</span>
            <input
              autoComplete="email"
              className="text-input"
              inputMode="email"
              name="email"
              onChange={(event) => {
                setEmail(event.target.value);
                setError(null);
              }}
              required
              type="email"
              value={email}
            />
          </label>

          <label>
            <span>Contraseña</span>
            <input
              autoComplete="current-password"
              className="text-input"
              name="password"
              onChange={(event) => {
                setPassword(event.target.value);
                setError(null);
              }}
              required
              type="password"
              value={password}
            />
          </label>

          {error ? (
            <p className="submit-error" role="alert">
              {error}
            </p>
          ) : null}

          <button className="primary-button" disabled={isSubmitting} type="submit">
            {isSubmitting ? 'Ingresando...' : 'Ingresar'}
          </button>
        </form>
      </section>
    </main>
  );
}

function getLoginErrorMessage(error: unknown): string {
  if (error instanceof ApiClientError) {
    if (error.status === 401) {
      return 'Correo electrónico o contraseña incorrectos.';
    }

    if (error.status === 403) {
      return 'El usuario no tiene acceso habilitado.';
    }
  }

  return 'No fue posible conectarse con el sistema. Intentá nuevamente.';
}
