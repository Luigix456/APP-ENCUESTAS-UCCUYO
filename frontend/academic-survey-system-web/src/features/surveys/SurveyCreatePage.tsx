import { useState, type FormEvent } from 'react';
import { useNavigate } from 'react-router-dom';
import { createSurvey } from '../../api/surveysApi';
import { useAuth } from '../../auth/AuthProvider';
import type { SurveyTarget } from '../../types/surveys';
import { SURVEY_TARGETS } from '../../types/surveys';
import {
  formatSurveyTarget,
  getFriendlySurveyError,
  MANAGE_SURVEY_TEMPLATES_PERMISSION,
  PermissionDeniedPanel,
  trimmedOrNull
} from './surveyUi';

interface SurveyCreateFormState {
  title: string;
  description: string;
  target: SurveyTarget;
  isAnonymous: boolean;
}

const initialForm: SurveyCreateFormState = {
  title: '',
  description: '',
  target: 'Student',
  isAnonymous: true
};

export function SurveyCreatePage() {
  const auth = useAuth();
  const navigate = useNavigate();
  const [form, setForm] = useState<SurveyCreateFormState>(initialForm);
  const [formError, setFormError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const accessToken = auth.accessToken;
  const canManageTemplates = auth.hasPermission(MANAGE_SURVEY_TEMPLATES_PERMISSION);

  if (!canManageTemplates) {
    return <PermissionDeniedPanel />;
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    if (!accessToken) {
      return;
    }

    if (!form.title.trim()) {
      setFormError('Ingresá el título de la encuesta.');
      return;
    }

    setIsSubmitting(true);
    setFormError(null);

    try {
      const createdSurvey = await createSurvey(
        {
          title: form.title.trim(),
          description: trimmedOrNull(form.description),
          target: form.target,
          isAnonymous: form.isAnonymous
        },
        accessToken,
        auth.logout
      );

      navigate(`/app/surveys/${createdSurvey.id}/edit`, { replace: true });
    } catch (error) {
      setFormError(getFriendlySurveyError(error, 'No fue posible crear la encuesta.'));
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <section className="app-content surveys-page">
      <header className="surveys-header">
        <div>
          <p className="eyebrow">Nueva plantilla</p>
          <h2>Crear encuesta</h2>
          <p>La encuesta se crea en estado borrador para poder editar su estructura.</p>
        </div>
      </header>

      <form className="survey-admin-form" noValidate onSubmit={handleSubmit}>
        <label>
          <span>Título</span>
          <input
            className="text-input"
            maxLength={200}
            onChange={(event) => {
              setForm((current) => ({ ...current, title: event.target.value }));
              setFormError(null);
            }}
            required
            type="text"
            value={form.title}
          />
        </label>

        <label>
          <span>Descripción</span>
          <textarea
            className="text-area"
            maxLength={1000}
            onChange={(event) => {
              setForm((current) => ({ ...current, description: event.target.value }));
              setFormError(null);
            }}
            rows={4}
            value={form.description}
          />
        </label>

        <label>
          <span>Audiencia</span>
          <select
            className="text-input"
            onChange={(event) =>
              setForm((current) => ({ ...current, target: event.target.value as SurveyTarget }))
            }
            value={form.target}
          >
            {SURVEY_TARGETS.map((target) => (
              <option key={target} value={target}>
                {formatSurveyTarget(target)}
              </option>
            ))}
          </select>
        </label>

        <label className="checkbox-field">
          <input
            checked={form.isAnonymous}
            onChange={(event) => setForm((current) => ({ ...current, isAnonymous: event.target.checked }))}
            type="checkbox"
          />
          <span>Respuestas anónimas</span>
        </label>

        {formError ? (
          <p className="submit-error" role="alert">
            {formError}
          </p>
        ) : null}

        <div className="form-actions">
          <button className="primary-button" disabled={isSubmitting} type="submit">
            {isSubmitting ? 'Creando...' : 'Crear borrador'}
          </button>
          <button className="secondary-button" onClick={() => navigate('/app/surveys')} type="button">
            Cancelar
          </button>
        </div>
      </form>
    </section>
  );
}
