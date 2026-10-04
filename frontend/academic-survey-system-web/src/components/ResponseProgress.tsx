interface ResponseProgressProps {
  responseCount: number;
  expectedRespondentCount: number | null;
  remainingCount: number | null;
  participationPercentage: number | null;
  compact?: boolean;
  sessionResponseCount?: number;
}

export function ResponseProgress({
  responseCount,
  expectedRespondentCount,
  remainingCount,
  participationPercentage,
  compact = false,
  sessionResponseCount
}: ResponseProgressProps) {
  const hasExpectedCount = typeof expectedRespondentCount === 'number' && expectedRespondentCount > 0;
  const normalizedPercentage = hasExpectedCount
    ? clampPercentage(participationPercentage ?? (responseCount / expectedRespondentCount) * 100)
    : null;
  const safeRemainingCount = hasExpectedCount
    ? Math.max(0, remainingCount ?? expectedRespondentCount - responseCount)
    : null;
  const percentageLabel = normalizedPercentage === null ? null : `${formatDecimal(normalizedPercentage)} %`;

  if (!hasExpectedCount) {
    return (
      <section className={`response-progress ${compact ? 'response-progress--compact' : ''}`}>
        <div className="response-progress__main">
          <span>Respuestas recibidas</span>
          <strong>{responseCount}</strong>
        </div>
        <p>Esta encuesta no tiene una cantidad esperada de alumnos asociada.</p>
        {typeof sessionResponseCount === 'number' ? (
          <small>En esta sesión: {sessionResponseCount}</small>
        ) : null}
      </section>
    );
  }

  return (
    <section
      aria-label={`Progreso de respuestas: ${responseCount} de ${expectedRespondentCount}`}
      className={`response-progress ${compact ? 'response-progress--compact' : ''} ${
        safeRemainingCount === 0 ? 'response-progress--complete' : ''
      }`}
    >
      <div className="response-progress__main">
        <span>Respuestas recibidas</span>
        <strong>
          {responseCount} / {expectedRespondentCount}
        </strong>
      </div>
      <div
        aria-label="Participación"
        aria-valuemax={100}
        aria-valuemin={0}
        aria-valuenow={Math.round(normalizedPercentage ?? 0)}
        className="response-progress__bar"
        role="progressbar"
      >
        <span style={{ width: `${normalizedPercentage ?? 0}%` }} />
      </div>
      <div className="response-progress__details">
        <span>{percentageLabel} de participación</span>
        <span>
          {safeRemainingCount === 0
            ? 'Se recibieron todas las respuestas previstas.'
            : `Faltan ${safeRemainingCount} respuestas`}
        </span>
        {typeof sessionResponseCount === 'number' ? (
          <span>En esta sesión: {sessionResponseCount}</span>
        ) : null}
      </div>
      {safeRemainingCount === 0 ? (
        <div className="response-progress__complete" role="status">
          <strong>Cupo completo</strong>
          <span>Se recibieron todas las respuestas previstas.</span>
        </div>
      ) : null}
    </section>
  );
}

export function formatParticipation(value: number | null | undefined): string {
  return typeof value === 'number' ? `${formatDecimal(value)} %` : 'No disponible';
}

function clampPercentage(value: number): number {
  if (!Number.isFinite(value)) {
    return 0;
  }

  return Math.min(100, Math.max(0, value));
}

function formatDecimal(value: number): string {
  return new Intl.NumberFormat('es-AR', {
    maximumFractionDigits: 2
  }).format(value);
}
