import { useCallback, useEffect, useMemo, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { ApiClientError } from '../../api/apiClient';
import {
  downloadSurveyAssignmentReportPdf,
  getSurveyAssignmentReport
} from '../../api/reportsApi';
import { useAuth } from '../../auth/AuthProvider';
import { formatParticipation } from '../../components/ResponseProgress';
import type {
  SurveyChoiceResultsDto,
  SurveyMatrixResultsDto,
  SurveyQuestionCommentDto,
  SurveyQuestionResultsDto,
  SurveyRatingResultsDto,
  SurveyTextResultsDto
} from '../../types/results';
import type { SurveyReportDto } from '../../types/reports';
import { DonutChart } from './components/DonutChart';
import { HorizontalBar } from './components/HorizontalBar';
import {
  formatAcademicCycleParts,
  formatAverage,
  formatDateTimeOrEmpty,
  formatQuestionType,
  hasResultsPermission,
  ResultsPermissionPanel
} from './resultsUi';

const REPORTS_EXPORT_PERMISSION = 'reports.export';
const DEFAULT_REPORT_FILENAME = 'informe-resultados.pdf';

type LoadState = 'loading' | 'ready' | 'error';

export function SurveyAssignmentReportPage() {
  const { surveyAssignmentId } = useParams();
  const auth = useAuth();
  const accessToken = auth.accessToken;
  const canReadResults = hasResultsPermission(auth.hasPermission);
  const canExportReport = auth.hasPermission(REPORTS_EXPORT_PERMISSION);
  const [report, setReport] = useState<SurveyReportDto | null>(null);
  const [loadState, setLoadState] = useState<LoadState>('loading');
  const [pageError, setPageError] = useState<string | null>(null);
  const [exportError, setExportError] = useState<string | null>(null);
  const [isExporting, setIsExporting] = useState(false);

  useEffect(() => {
    if (!canReadResults || !accessToken || !surveyAssignmentId) {
      setLoadState('ready');
      return;
    }

    const controller = new AbortController();

    setLoadState('loading');
    setPageError(null);

    getSurveyAssignmentReport(surveyAssignmentId, accessToken, auth.logout, controller.signal)
      .then((nextReport) => {
        setReport(nextReport);
        setLoadState('ready');
      })
      .catch((error: unknown) => {
        if (isAbortError(error)) {
          return;
        }

        setPageError(getReportLoadError(error));
        setLoadState('error');
      });

    return () => {
      controller.abort();
    };
  }, [accessToken, auth.logout, canReadResults, surveyAssignmentId]);

  const sortedQuestions = useMemo(
    () => [...(report?.questions ?? [])].sort((left, right) => left.order - right.order),
    [report?.questions]
  );

  const handlePrint = useCallback(() => {
    window.print();
  }, []);

  const handleExportPdf = useCallback(async () => {
    if (!accessToken || !surveyAssignmentId || isExporting) {
      return;
    }

    const controller = new AbortController();

    setIsExporting(true);
    setExportError(null);

    try {
      const response = await downloadSurveyAssignmentReportPdf(
        surveyAssignmentId,
        accessToken,
        auth.logout,
        controller.signal
      );
      const filename =
        getFilenameFromContentDisposition(response.contentDisposition) ?? DEFAULT_REPORT_FILENAME;
      const url = URL.createObjectURL(response.blob);
      const link = document.createElement('a');

      link.href = url;
      link.download = filename;
      document.body.appendChild(link);
      link.click();
      link.remove();
      window.setTimeout(() => URL.revokeObjectURL(url), 0);
    } catch (error: unknown) {
      if (!isAbortError(error)) {
        setExportError(getReportExportError(error));
      }
    } finally {
      setIsExporting(false);
    }
  }, [accessToken, auth.logout, isExporting, surveyAssignmentId]);

  if (!canReadResults) {
    return <ResultsPermissionPanel />;
  }

  if (!surveyAssignmentId) {
    return (
      <section className="app-content" role="alert">
        <h2>El informe solicitado no fue encontrado.</h2>
      </section>
    );
  }

  if (loadState === 'loading') {
    return (
      <section className="app-content" aria-live="polite">
        <p>Cargando informe...</p>
      </section>
    );
  }

  if (loadState === 'error') {
    return (
      <section className="app-content" role="alert">
        <p className="eyebrow">Informe</p>
        <h2>No pudimos cargar el informe</h2>
        <p>{pageError}</p>
        <Link className="secondary-link-button" to="/app/results">
          Volver a resultados
        </Link>
      </section>
    );
  }

  if (!report) {
    return (
      <section className="app-content" role="alert">
        <h2>El informe solicitado no fue encontrado.</h2>
      </section>
    );
  }

  return (
    <section className="app-content report-page">
      <nav aria-label="Ruta de navegación" className="report-breadcrumb">
        <Link to="/app/results">Resultados</Link>
        <span>/</span>
        <span>Informe</span>
      </nav>

      <header className="report-actions">
        <Link
          className="secondary-link-button report-back-link"
          to={`/app/results/assignments/${encodeURIComponent(surveyAssignmentId)}`}
        >
          Volver a resultados
        </Link>
        <div>
          <button className="secondary-button" onClick={handlePrint} type="button">
            Imprimir
          </button>
          {canExportReport ? (
            <button
              className="primary-button"
              disabled={isExporting}
              onClick={handleExportPdf}
              type="button"
            >
              {isExporting ? 'Exportando...' : 'Exportar PDF'}
            </button>
          ) : null}
        </div>
      </header>

      {exportError ? (
        <p className="submit-error" role="alert">
          {exportError}
        </p>
      ) : null}

      <article className="report-sheet">
        <header className="report-sheet__header">
          <div>
            <p className="eyebrow">{report.institution.systemName}</p>
            <h2>{report.institution.institutionName}</h2>
            {report.institution.facultyName ? <p>{report.institution.facultyName}</p> : null}
          </div>
          <div>
            <strong>Informe de resultados</strong>
            <span>Generado: {formatDateTimeOrEmpty(report.generatedAtUtc)}</span>
          </div>
        </header>

        <section className="report-title-block">
          <h1>{report.surveyTitle}</h1>
          <p>Versión {report.surveyVersionNumber}</p>
        </section>

        <dl className="report-context-grid">
          <ReportMetaItem label="Carrera" value={report.careerName} />
          <ReportMetaItem label="Materia" value={report.subjectName} />
          <ReportMetaItem label="Docente" value={report.teacherFullName} />
          <ReportMetaItem label="Rol docente" value={report.teachingRole} />
          <ReportMetaItem
            label="Ciclo académico"
            value={formatAcademicCycleParts(report.academicCycleYear, report.academicCyclePeriod)}
          />
        </dl>

        <section className="report-summary-grid" aria-label="Resumen del informe">
          <ReportSummaryCard label="Alumnos inscriptos" value={report.expectedRespondentCount?.toString() ?? 'No aplica'} />
          <ReportSummaryCard label="Respuestas" value={String(report.totalResponses)} />
          <ReportSummaryCard label="Participación" value={formatParticipation(report.participationPercentage)} />
          <ReportSummaryCard label="Pendientes" value={report.remainingCount?.toString() ?? 'No aplica'} />
          <ReportSummaryCard label="Sesiones" value={String(report.totalSessions)} />
          <ReportSummaryCard
            label="Primera respuesta"
            value={formatDateTimeOrEmpty(report.firstSubmittedAtUtc)}
          />
          <ReportSummaryCard
            label="Última respuesta"
            value={formatDateTimeOrEmpty(report.lastSubmittedAtUtc)}
          />
        </section>

        {report.totalResponses === 0 ? (
          <section className="report-empty-state">
            <h3>Esta evaluación todavía no tiene respuestas.</h3>
            <p>El informe queda disponible para consulta cuando se registren respuestas.</p>
          </section>
        ) : null}

        {report.totalResponses > 0 && !report.detailedResultsAvailable ? (
          <section className="report-empty-state">
            <h3>Resultados protegidos</h3>
            <p>
              Los resultados detallados estarán disponibles cuando se alcance el mínimo de{' '}
              {report.minimumResponsesRequired} respuestas requerido para proteger el anonimato.
            </p>
            <p>
              Hay {report.totalResponses} respuestas. Faltan {report.responsesNeededToUnlock}.
            </p>
          </section>
        ) : null}

        {report.detailedResultsAvailable ? (
          <section className="report-question-list" aria-label="Resultados por pregunta">
            {sortedQuestions.map((question) => (
              <ReportQuestion key={question.questionId} question={question} />
            ))}
          </section>
        ) : null}
      </article>
    </section>
  );
}

function ReportQuestion({ question }: { question: SurveyQuestionResultsDto }) {
  return (
    <section className="report-question">
      <header>
        <div>
          <p className="eyebrow">{formatQuestionType(question.type)}</p>
          <h3>{question.text}</h3>
        </div>
        <span>{question.responseCount} respuestas</span>
      </header>

      {renderReportQuestion(question)}

      {question.comments.length > 0 ? (
        <ReportComments comments={question.comments} title="Comentarios adicionales" />
      ) : null}
    </section>
  );
}

function renderReportQuestion(question: SurveyQuestionResultsDto) {
  switch (question.type) {
    case 'SingleChoice':
      return <ReportSingleChoice choice={question.choice} questionText={question.text} />;
    case 'MultipleChoice':
      return <ReportChoiceBars choice={question.choice} includeOtherValues />;
    case 'ShortText':
      return <ReportTextValues textValues={question.textValues} title="Respuestas abiertas" />;
    case 'LongText':
      return <ReportTextValues textValues={question.textValues} title="Respuestas" />;
    case 'RatingScale':
      return <ReportRating rating={question.rating} />;
    case 'MatrixSingleChoice':
      return <ReportMatrix matrix={question.matrix} />;
    default:
      return <p>Tipo de pregunta no reconocido.</p>;
  }
}

function ReportSingleChoice({
  choice,
  questionText
}: {
  choice: SurveyChoiceResultsDto | null;
  questionText: string;
}) {
  if (!choice) {
    return <p>No hay datos de opciones para esta pregunta.</p>;
  }

  const entries = [
    ...choice.options.map((option) => ({
      count: option.count,
      label: option.text,
      percentage: option.percentage
    })),
    ...(choice.other
      ? [
          {
            count: choice.other.count,
            label: 'Otro',
            percentage: choice.other.percentage
          }
        ]
      : [])
  ];

  return (
    <div className="report-choice-layout">
      <DonutChart entries={entries} title={`Distribución de respuestas para ${questionText}`} />
      <ReportChoiceBars choice={choice} includeOtherValues />
    </div>
  );
}

function ReportChoiceBars({
  choice,
  includeOtherValues
}: {
  choice: SurveyChoiceResultsDto | null;
  includeOtherValues?: boolean;
}) {
  if (!choice) {
    return <p>No hay datos de opciones para esta pregunta.</p>;
  }

  return (
    <div className="result-subsection">
      <div className="result-bars">
        {choice.options.map((option) => (
          <HorizontalBar
            count={option.count}
            key={option.optionId}
            label={option.text}
            percentage={option.percentage}
          />
        ))}
        {choice.other ? (
          <HorizontalBar count={choice.other.count} label="Otro" percentage={choice.other.percentage} />
        ) : null}
      </div>

      {includeOtherValues && choice.other ? (
        <ReportTextList emptyText="Sin respuestas en Otro." title="Respuestas en Otro" values={choice.other.values} />
      ) : null}
    </div>
  );
}

function ReportTextValues({
  textValues,
  title
}: {
  textValues: SurveyTextResultsDto | null;
  title: string;
}) {
  if (!textValues) {
    return <p>Sin respuestas de texto.</p>;
  }

  return (
    <ReportTextList
      emptyText="Sin respuestas de texto."
      title={`${title} (${textValues.responseCount})`}
      values={textValues.values}
    />
  );
}

function ReportTextList({
  emptyText,
  title,
  values
}: {
  emptyText: string;
  title: string;
  values: string[];
}) {
  return (
    <section className="text-response-section">
      <h4>{title}</h4>
      {values.length > 0 ? (
        <div className="text-response-list">
          {values.map((value, index) => (
            <blockquote className="text-response" key={`${value}-${index}`}>
              {value}
            </blockquote>
          ))}
        </div>
      ) : (
        <p>{emptyText}</p>
      )}
    </section>
  );
}

function ReportComments({
  comments,
  title
}: {
  comments: SurveyQuestionCommentDto[];
  title: string;
}) {
  return (
    <section className="comments-section">
      <h4>{title}</h4>
      <div className="text-response-list">
        {comments.map((comment, index) => (
          <blockquote className="text-response" key={`${comment.comment}-${index}`}>
            {comment.comment}
          </blockquote>
        ))}
      </div>
    </section>
  );
}

function ReportRating({ rating }: { rating: SurveyRatingResultsDto | null }) {
  if (!rating) {
    return <p>No hay datos de escala para esta pregunta.</p>;
  }

  const denominator = rating.configuredMaximum ?? rating.maximumObserved;

  return (
    <div className="rating-result">
      <div className="rating-result__summary">
        <div>
          <span>Promedio</span>
          <strong>
            {formatAverage(rating.average)}
            {denominator ? ` / ${denominator}` : ''}
          </strong>
        </div>
        <div>
          <span>Respuestas</span>
          <strong>{rating.responseCount}</strong>
        </div>
        <div>
          <span>Mínimo observado</span>
          <strong>{rating.minimumObserved ?? 'Sin datos'}</strong>
        </div>
        <div>
          <span>Máximo observado</span>
          <strong>{rating.maximumObserved ?? 'Sin datos'}</strong>
        </div>
      </div>

      <div className="result-bars">
        {rating.distribution.map((item) => (
          <HorizontalBar
            count={item.count}
            key={item.value}
            label={String(item.value)}
            percentage={item.percentage}
          />
        ))}
      </div>
    </div>
  );
}

function ReportMatrix({ matrix }: { matrix: SurveyMatrixResultsDto | null }) {
  if (!matrix) {
    return <p>No hay datos de matriz para esta pregunta.</p>;
  }

  if (matrix.rows.length === 0) {
    return <p>Sin filas de matriz.</p>;
  }

  return (
    <div className="matrix-result">
      {matrix.rows.map((row) => (
        <section className="matrix-result__row report-chart-row" key={row.rowId}>
          <header>
            <h4>{row.rowText}</h4>
            <span>{row.totalResponses} respuestas</span>
          </header>
          <div className="result-bars">
            {row.options.map((option) => (
              <HorizontalBar
                count={option.count}
                key={option.optionId}
                label={option.text}
                percentage={option.percentage}
              />
            ))}
          </div>
        </section>
      ))}
    </div>
  );
}

function ReportMetaItem({ label, value }: { label: string; value: string }) {
  return (
    <div>
      <dt>{label}</dt>
      <dd>{value}</dd>
    </div>
  );
}

function ReportSummaryCard({ label, value }: { label: string; value: string }) {
  return (
    <article className="report-summary-card">
      <span>{label}</span>
      <strong>{value}</strong>
    </article>
  );
}

function getReportLoadError(error: unknown): string {
  if (error instanceof ApiClientError) {
    if (error.status === 401) {
      return 'La sesión de usuario venció. Volvé a iniciar sesión.';
    }

    if (error.status === 403) {
      return 'No tenés permisos para consultar este informe.';
    }

    if (error.status === 404) {
      return 'El informe solicitado no fue encontrado.';
    }
  }

  return 'No fue posible cargar el informe.';
}

function getReportExportError(error: unknown): string {
  if (error instanceof ApiClientError) {
    if (error.status === 401) {
      return 'La sesión de usuario venció. Volvé a iniciar sesión.';
    }

    if (error.status === 403) {
      return 'No tenés permisos para exportar este informe.';
    }
  }

  return 'No fue posible generar el PDF.';
}

function getFilenameFromContentDisposition(contentDisposition: string | null): string | null {
  if (!contentDisposition) {
    return null;
  }

  const encodedMatch = /filename\*=UTF-8''([^;]+)/i.exec(contentDisposition);
  if (encodedMatch?.[1]) {
    return decodeURIComponent(encodedMatch[1].trim());
  }

  const quotedMatch = /filename="([^"]+)"/i.exec(contentDisposition);
  if (quotedMatch?.[1]) {
    return quotedMatch[1].trim();
  }

  const plainMatch = /filename=([^;]+)/i.exec(contentDisposition);
  return plainMatch?.[1]?.trim() ?? null;
}

function isAbortError(error: unknown): boolean {
  return error instanceof DOMException && error.name === 'AbortError';
}
