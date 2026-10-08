import { useEffect, useMemo, useState } from 'react';
import { Link, useSearchParams } from 'react-router-dom';
import { getQuestionHistory, getSurveyHistory } from '../../api/resultsApi';
import { useAuth } from '../../auth/AuthProvider';
import { formatParticipation } from '../../components/ResponseProgress';
import type { QuestionHistoryDto, SurveyHistoryDto, SurveyHistoryPointDto } from '../../types/results';
import {
  formatAverage,
  formatQuestionType,
  getFriendlyResultsError,
  hasResultsPermission,
  ResultsPermissionPanel
} from './resultsUi';

type LoadState = 'loading' | 'ready' | 'error';

export function SurveyHistoryPage() {
  const auth = useAuth();
  const [searchParams] = useSearchParams();
  const accessToken = auth.accessToken;
  const canReadResults = hasResultsPermission(auth.hasPermission);
  const query = {
    careerId: searchParams.get('careerId') ?? '',
    subjectId: searchParams.get('subjectId') ?? '',
    teacherId: searchParams.get('teacherId') ?? '',
    surveyVersionGroupId: searchParams.get('surveyVersionGroupId') ?? ''
  };
  const [state, setState] = useState<LoadState>('loading');
  const [history, setHistory] = useState<SurveyHistoryDto | null>(null);
  const [questionHistory, setQuestionHistory] = useState<QuestionHistoryDto | null>(null);
  const [selectedQuestionLineageId, setSelectedQuestionLineageId] = useState('');
  const [questionState, setQuestionState] = useState<LoadState>('ready');
  const [questionError, setQuestionError] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const isQueryComplete = Object.values(query).every(Boolean);

  useEffect(() => {
    if (!canReadResults || !accessToken || !isQueryComplete) {
      setState('ready');
      setHistory(null);
      return;
    }

    const controller = new AbortController();
    setState('loading');
    setError(null);
    setHistory(null);
    setSelectedQuestionLineageId('');
    setQuestionHistory(null);
    setQuestionState('ready');
    setQuestionError(null);

    getSurveyHistory(accessToken, auth.logout, query, controller.signal)
      .then((nextHistory) => {
        setHistory(nextHistory);
        setSelectedQuestionLineageId(nextHistory.questions[0]?.questionLineageId ?? '');
        setState('ready');
      })
      .catch((loadError: unknown) => {
        if (isAbortError(loadError)) return;
        setError(getFriendlyResultsError(loadError, 'No fue posible cargar la evolución histórica.'));
        setState('error');
      });

    return () => controller.abort();
  }, [
    accessToken,
    auth.logout,
    canReadResults,
    isQueryComplete,
    query.careerId,
    query.subjectId,
    query.teacherId,
    query.surveyVersionGroupId
  ]);

  useEffect(() => {
    if (!canReadResults || !accessToken || !isQueryComplete || !selectedQuestionLineageId) {
      setQuestionHistory(null);
      setQuestionState('ready');
      setQuestionError(null);
      return;
    }

    const controller = new AbortController();
    setQuestionHistory(null);
    setQuestionState('loading');
    setQuestionError(null);

    getQuestionHistory(accessToken, auth.logout, selectedQuestionLineageId, query, controller.signal)
      .then((nextQuestionHistory) => {
        setQuestionHistory(nextQuestionHistory);
        setQuestionState('ready');
      })
      .catch((loadError: unknown) => {
        if (isAbortError(loadError)) return;
        setQuestionHistory(null);
        setQuestionError(getFriendlyResultsError(loadError, 'No fue posible cargar la comparación de esta pregunta.'));
        setQuestionState('error');
      });

    return () => controller.abort();
  }, [
    accessToken,
    auth.logout,
    canReadResults,
    isQueryComplete,
    query.careerId,
    query.subjectId,
    query.teacherId,
    query.surveyVersionGroupId,
    selectedQuestionLineageId
  ]);

  const summary = useMemo(() => buildHistorySummary(history?.points ?? []), [history]);

  if (!canReadResults) {
    return <ResultsPermissionPanel />;
  }

  if (!isQueryComplete) {
    return (
      <section className="app-content empty-detail">
        <h3>No hay contexto suficiente.</h3>
        <p>Volvé a resultados y abrí la evolución desde una tarjeta con carrera, materia, docente y encuesta.</p>
        <Link className="secondary-link-button" to="/app/results">Volver a resultados</Link>
      </section>
    );
  }

  return (
    <section className="app-content results-history-page">
      <header className="results-header">
        <div>
          <p className="eyebrow">Evolución histórica</p>
          <h2>{history?.subjectName ?? 'Histórico de resultados'}</h2>
          {history ? (
            <p>{history.teacherName} · {history.surveyTitle}</p>
          ) : null}
        </div>
        <Link className="secondary-link-button" to="/app/results">Volver a resultados</Link>
      </header>

      {state === 'loading' ? <p aria-live="polite">Cargando evolución...</p> : null}
      {state === 'error' ? <div className="empty-detail" role="alert"><h3>No pudimos cargar la evolución</h3><p>{error}</p></div> : null}

      {state === 'ready' && history ? (
        <>
          <div className="history-summary-grid" aria-label="Resumen de evolución">
            <HistoryMetric label="Ciclos disponibles" value={String(new Set(history.points.map((point) => point.academicCycleId)).size)} />
            <HistoryMetric label="Última participación" value={summary.latestParticipationLabel} />
            <HistoryMetric label="Cambio respecto del registro anterior" value={summary.changeLabel} />
          </div>

          <section className="history-panel">
            <h3>Participación por asignación</h3>
            <ParticipationHistoryChart points={history.points} />
            <HistoryPointsTable points={history.points} />
          </section>

          <section className="history-panel">
            <label className="history-question-select">
              <span>Comparar una pregunta</span>
              <select
                className="text-input"
                onChange={(event) => setSelectedQuestionLineageId(event.target.value)}
                value={selectedQuestionLineageId}
              >
                {history.questions.map((question) => (
                  <option key={question.questionLineageId} value={question.questionLineageId}>
                    {question.latestQuestionText}
                  </option>
                ))}
              </select>
            </label>
            {questionState === 'loading' ? <p aria-live="polite">Cargando comparación...</p> : null}
            {questionState === 'error' ? (
              <div className="inline-message" role="alert">
                {questionError}
              </div>
            ) : null}
            {questionState === 'ready' && questionHistory ? <QuestionHistoryPanel history={questionHistory} /> : null}
            {questionState === 'ready' && !questionHistory && !selectedQuestionLineageId ? <p>No hay preguntas comparables disponibles.</p> : null}
          </section>
        </>
      ) : null}
    </section>
  );
}

function HistoryMetric({ label, value }: { label: string; value: string }) {
  return <div className="metric-card"><span>{label}</span><strong>{value}</strong></div>;
}

function ParticipationHistoryChart({ points }: { points: SurveyHistoryPointDto[] }) {
  const chartPoints = points.filter((point) => point.participationPercentage !== null);
  const width = 640;
  const height = 220;
  const padding = 34;
  const usableWidth = width - padding * 2;
  const usableHeight = height - padding * 2;
  const coordinates = chartPoints.map((point, index) => {
    const x = chartPoints.length <= 1 ? width / 2 : padding + (usableWidth * index) / (chartPoints.length - 1);
    const y = padding + usableHeight - ((point.participationPercentage ?? 0) / 100) * usableHeight;
    return { point, x, y };
  });
  const path = coordinates.map((item, index) => `${index === 0 ? 'M' : 'L'} ${item.x} ${item.y}`).join(' ');

  if (chartPoints.length === 0) {
    return <p>Participación no disponible porque las asignaciones no tienen matrícula esperada.</p>;
  }

  return (
    <figure className="history-chart">
      <svg aria-label="Gráfico de participación histórica de 0 a 100 por ciento" role="img" viewBox={`0 0 ${width} ${height}`}>
        <line x1={padding} x2={padding} y1={padding} y2={height - padding} />
        <line x1={padding} x2={width - padding} y1={height - padding} y2={height - padding} />
        {[0, 25, 50, 75, 100].map((tick) => {
          const y = padding + usableHeight - (tick / 100) * usableHeight;
          return <g key={tick}><line className="history-chart-grid" x1={padding} x2={width - padding} y1={y} y2={y} /><text x={4} y={y + 4}>{tick}%</text></g>;
        })}
        {path ? <path className="history-chart-line" d={path} fill="none" /> : null}
        {coordinates.map(({ point, x, y }) => (
          <g key={point.surveyAssignmentId}>
            <circle cx={x} cy={y} r="5" />
            <text x={x} y={height - 8} textAnchor="middle">{point.academicCycleYear} v{point.surveyVersionNumber}</text>
          </g>
        ))}
      </svg>
      <figcaption>La tabla siguiente contiene los valores exactos. Si hay varias asignaciones en un ciclo, se muestran separadas.</figcaption>
    </figure>
  );
}

function HistoryPointsTable({ points }: { points: SurveyHistoryPointDto[] }) {
  return (
    <div className="history-table">
      <table>
        <thead><tr><th>Ciclo</th><th>Versión</th><th>Respuestas</th><th>Participación</th><th>Detalle</th></tr></thead>
        <tbody>
          {points.map((point) => (
            <tr key={point.surveyAssignmentId}>
              <td>{point.academicCycleName}</td>
              <td>v{point.surveyVersionNumber}</td>
              <td>{point.expectedRespondentCount ? `${point.responseCount} / ${point.expectedRespondentCount}` : `${point.responseCount} respuestas`}</td>
              <td>{point.participationPercentage === null ? 'No disponible' : formatParticipation(point.participationPercentage)}</td>
              <td>{point.detailedResultsAvailable ? 'Disponible' : 'Resultados protegidos'}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

function QuestionHistoryPanel({ history }: { history: QuestionHistoryDto }) {
  return (
    <div className="question-history-panel">
      <div>
        <h3>{history.latestQuestionText}</h3>
        <p>{formatQuestionType(history.questionType)}</p>
        {history.questionTextsChanged ? <p className="form-helper">El texto de esta pregunta cambió entre versiones.</p> : null}
      </div>
      {!history.comparisonSupported ? (
        <p className="inline-message">{history.unsupportedReason}</p>
      ) : null}
      <div className="question-history-list">
        {history.points.map((point) => (
          <article className="question-history-card" key={`${point.surveyAssignmentId}-${point.questionId}`}>
            <header><strong>{point.academicCycleName} · v{point.surveyVersionNumber}</strong><span>{point.responseCount} respuestas</span></header>
            {!point.detailedResultsAvailable ? (
              <p>Resultados protegidos. Faltan {point.responsesNeededToUnlock} respuestas para alcanzar el mínimo de {point.minimumResponsesRequired}.</p>
            ) : point.averageRating !== null ? (
              <div>
                <strong>{formatAverage(point.averageRating)} / {point.maxRating}</strong>
                {point.ratingScaleChanged ? <small>Escala modificada entre versiones.</small> : null}
              </div>
            ) : point.distribution ? (
              <div className="result-bars">
                {point.distribution.map((item) => (
                  <div className="history-bar" key={item.label}>
                    <span>{item.label}</span>
                    <strong>{item.count} · {formatParticipation(item.percentage)}</strong>
                    <i style={{ width: `${item.percentage}%` }} />
                  </div>
                ))}
              </div>
            ) : (
              <p>Sin distribución disponible.</p>
            )}
          </article>
        ))}
      </div>
    </div>
  );
}

function buildHistorySummary(points: SurveyHistoryPointDto[]) {
  const comparable = points.filter((point) => point.participationPercentage !== null);
  const latest = comparable.at(-1);
  const previous = comparable.at(-2);
  const change = latest && previous
    ? latest.participationPercentage! - previous.participationPercentage!
    : null;

  return {
    latestParticipationLabel: latest ? formatParticipation(latest.participationPercentage) : 'No disponible',
    changeLabel: change === null ? 'No disponible' : formatPercentagePointChange(change)
  };
}

function formatPercentagePointChange(value: number): string {
  return `${new Intl.NumberFormat('es-AR', {
    maximumFractionDigits: 2,
    signDisplay: 'always'
  }).format(value)} pp`;
}

function isAbortError(error: unknown): boolean {
  return error instanceof DOMException && error.name === 'AbortError';
}
