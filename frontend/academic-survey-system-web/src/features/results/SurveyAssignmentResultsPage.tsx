import { useEffect, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import {
  getSurveyAssignmentQuestionResults,
  getSurveyAssignmentResultSummary
} from '../../api/resultsApi';
import { useAuth } from '../../auth/AuthProvider';
import { ResponseProgress, formatParticipation } from '../../components/ResponseProgress';
import type { SurveyQuestionResultsDto, SurveyResultsSummaryDto } from '../../types/results';
import { QuestionResultCard } from './components/QuestionResultCard';
import {
  formatAcademicCycleParts,
  formatDateTimeOrEmpty,
  getFriendlyResultsError,
  hasResultsPermission,
  ResultsPermissionPanel
} from './resultsUi';

type LoadState = 'loading' | 'ready' | 'error';

export function SurveyAssignmentResultsPage() {
  const { surveyAssignmentId } = useParams();
  const auth = useAuth();
  const accessToken = auth.accessToken;
  const canReadResults = hasResultsPermission(auth.hasPermission);
  const [summary, setSummary] = useState<SurveyResultsSummaryDto | null>(null);
  const [questions, setQuestions] = useState<SurveyQuestionResultsDto[]>([]);
  const [loadState, setLoadState] = useState<LoadState>('loading');
  const [pageError, setPageError] = useState<string | null>(null);

  useEffect(() => {
    if (!canReadResults || !accessToken || !surveyAssignmentId) {
      setLoadState('ready');
      return;
    }

    const controller = new AbortController();

    setLoadState('loading');
    setPageError(null);

    getSurveyAssignmentResultSummary(
      surveyAssignmentId,
      accessToken,
      auth.logout,
      controller.signal
    )
      .then(async (nextSummary) => {
        const nextQuestions = nextSummary.detailedResultsAvailable
          ? await getSurveyAssignmentQuestionResults(
              surveyAssignmentId,
              accessToken,
              auth.logout,
              controller.signal
            )
          : [];

        setSummary(nextSummary);
        setQuestions([...nextQuestions].sort((left, right) => left.order - right.order));
        setLoadState('ready');
      })
      .catch((error: unknown) => {
        if (isAbortError(error)) {
          return;
        }

        setPageError(getFriendlyResultsError(error, 'No fue posible conectarse con el sistema.'));
        setLoadState('error');
      });

    return () => {
      controller.abort();
    };
  }, [accessToken, auth.logout, canReadResults, surveyAssignmentId]);

  if (!canReadResults) {
    return <ResultsPermissionPanel />;
  }

  if (!surveyAssignmentId) {
    return (
      <section className="app-content" role="alert">
        <h2>Los resultados solicitados no fueron encontrados.</h2>
      </section>
    );
  }

  if (loadState === 'loading') {
    return (
      <section className="app-content" aria-live="polite">
        <p>Cargando resultados de la evaluación...</p>
      </section>
    );
  }

  if (loadState === 'error') {
    return (
      <section className="app-content" role="alert">
        <p className="eyebrow">Resultados</p>
        <h2>No pudimos cargar los resultados</h2>
        <p>{pageError}</p>
        <Link className="secondary-link-button" to="/app/results">
          Volver a resultados
        </Link>
      </section>
    );
  }

  if (!summary) {
    return (
      <section className="app-content" role="alert">
        <h2>Los resultados solicitados no fueron encontrados.</h2>
      </section>
    );
  }

  return (
    <section className="app-content results-detail-page">
      <header className="results-header">
        <div>
          <p className="eyebrow">Resultados</p>
          <h2>{summary.surveyTitle}</h2>
          <p>
            {summary.subjectName} · {summary.careerName} · {formatAcademicCycleParts(
              summary.academicCycleYear,
              summary.academicCyclePeriod
            )}
          </p>
        </div>
        <div className="results-header__actions">
          <Link
            className="primary-link-button"
            to={`/app/results/assignments/${encodeURIComponent(summary.surveyAssignmentId)}/report`}
          >
            Generar informe
          </Link>
          <Link className="secondary-link-button" to="/app/results">
            Volver
          </Link>
        </div>
      </header>

      <dl className="result-context-meta">
        <div>
          <dt>Docente</dt>
          <dd>{summary.teacherFullName}</dd>
        </div>
        <div>
          <dt>Respuestas</dt>
          <dd>{summary.totalResponses}</dd>
        </div>
        <div>
          <dt>Sesiones</dt>
          <dd>{summary.totalSessions}</dd>
        </div>
        <div>
          <dt>Primera respuesta</dt>
          <dd>{formatDateTimeOrEmpty(summary.firstSubmittedAtUtc)}</dd>
        </div>
        <div>
          <dt>Última respuesta</dt>
          <dd>{formatDateTimeOrEmpty(summary.lastSubmittedAtUtc)}</dd>
        </div>
      </dl>

      <div className="metric-grid" aria-label="Resumen de resultados">
        <MetricCard label="Respuestas" value={String(summary.totalResponses)} />
        <MetricCard label="Alumnos esperados" value={summary.expectedRespondentCount?.toString() ?? 'No aplica'} />
        <MetricCard label="Participación" value={formatParticipation(summary.participationPercentage)} />
        <MetricCard label="Pendientes" value={summary.remainingCount?.toString() ?? 'No aplica'} />
        <MetricCard label="Sesiones" value={String(summary.totalSessions)} />
        <MetricCard label="Primera respuesta" value={formatDateTimeOrEmpty(summary.firstSubmittedAtUtc)} />
        <MetricCard label="Última respuesta" value={formatDateTimeOrEmpty(summary.lastSubmittedAtUtc)} />
      </div>

      <ResponseProgress
        compact
        expectedRespondentCount={summary.expectedRespondentCount}
        participationPercentage={summary.participationPercentage}
        remainingCount={summary.remainingCount}
        responseCount={summary.totalResponses}
      />

      {summary.totalResponses === 0 ? (
        <div className="empty-detail">
          <h3>Esta evaluación todavía no tiene respuestas.</h3>
          <p>El contexto existe y puede consultarse cuando se registren respuestas.</p>
        </div>
      ) : null}

      {summary.totalResponses > 0 && !summary.detailedResultsAvailable ? (
        <section className="privacy-threshold-panel">
          <p className="eyebrow">Resultados protegidos</p>
          <h3>Hay {summary.totalResponses} respuestas.</h3>
          <p>
            Se necesitan {summary.minimumResponsesRequired} para mostrar resultados detallados y proteger el anonimato.
          </p>
          <p>
            Faltan {summary.responsesNeededToUnlock} respuestas. No se muestran respuestas individuales ni
            distribuciones todavía.
          </p>
        </section>
      ) : null}

      {summary.detailedResultsAvailable ? (
        <div className="question-result-list">
          {questions.map((question) => (
            <QuestionResultCard key={question.questionId} question={question} />
          ))}
        </div>
      ) : null}
    </section>
  );
}

function MetricCard({ label, value }: { label: string; value: string }) {
  return (
    <article className="metric-card">
      <span>{label}</span>
      <strong>{value}</strong>
    </article>
  );
}

function isAbortError(error: unknown): boolean {
  return error instanceof DOMException && error.name === 'AbortError';
}
