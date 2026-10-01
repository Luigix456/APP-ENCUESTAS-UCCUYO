import { useEffect, useMemo, useState, type FormEvent } from 'react';
import { QRCodeSVG } from 'qrcode.react';
import { ApiClientError } from '../../api/apiClient';
import { getSurveyAssignments } from '../../api/surveyAssignmentsApi';
import {
  closeSurveySession,
  createSurveySession,
  getSurveySession,
  getSurveySessions,
  openSurveySession
} from '../../api/surveySessionsApi';
import { useAuth } from '../../auth/AuthProvider';
import { PaginationControls, usePagination } from '../../components/Pagination';
import { useAcademicContext } from '../academic-context/AcademicContextProvider';
import type {
  CreateSurveySessionRequest,
  SurveyAssignmentDto,
  SurveySessionDto,
  SurveySessionStatus
} from '../../types/surveyOperations';

const MANAGE_SESSIONS_PERMISSION = 'surveys.sessions.manage';
const SESSION_DURATION_HOURS = 2;

type LoadState = 'loading' | 'ready' | 'error';

interface SessionFormState {
  surveyAssignmentId: string;
  title: string;
  location: string;
  expiresAtLocal: string;
}

interface SessionFiltersState {
  subjectId: string;
  teacherId: string;
  status: '' | SurveySessionStatus;
}

export function SurveySessionsPage({ contextual = false }: { contextual?: boolean }) {
  const auth = useAuth();
  const academicContext = useAcademicContext();
  const [assignments, setAssignments] = useState<SurveyAssignmentDto[]>([]);
  const [sessions, setSessions] = useState<SurveySessionDto[]>([]);
  const [selectedSession, setSelectedSession] = useState<SurveySessionDto | null>(null);
  const [loadState, setLoadState] = useState<LoadState>('loading');
  const [pageError, setPageError] = useState<string | null>(null);
  const [formError, setFormError] = useState<string | null>(null);
  const [actionError, setActionError] = useState<string | null>(null);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);
  const [isCreating, setIsCreating] = useState(false);
  const [activeAction, setActiveAction] = useState<'open' | 'close' | 'refresh' | null>(null);
  const [isFormVisible, setIsFormVisible] = useState(false);
  const [form, setForm] = useState<SessionFormState>(() => createInitialForm());
  const [filters, setFilters] = useState<SessionFiltersState>({
    subjectId: '',
    teacherId: '',
    status: ''
  });

  const accessToken = auth.accessToken;
  const canManageSessions = auth.hasPermission(MANAGE_SESSIONS_PERMISSION);

  useEffect(() => {
    if (!contextual) {
      return;
    }

    setFilters({ subjectId: '', teacherId: '', status: '' });
    setSelectedSession(null);
    setIsFormVisible(false);
    setForm(createInitialForm());
  }, [academicContext.academicCycleId, academicContext.careerId, contextual]);

  useEffect(() => {
    if (
      !canManageSessions ||
      !accessToken ||
      (contextual && (!academicContext.careerId || !academicContext.academicCycleId))
    ) {
      setLoadState('ready');
      setAssignments([]);
      setSessions([]);
      setSelectedSession(null);
      return;
    }

    const abortController = new AbortController();
    void loadData(accessToken, abortController.signal);
    return () => abortController.abort();
  }, [
    accessToken,
    academicContext.academicCycleId,
    academicContext.careerId,
    canManageSessions,
    contextual,
    filters.status,
    filters.subjectId,
    filters.teacherId
  ]);

  const sortedSessions = useMemo(() => {
    return [...sessions].sort(compareSessions);
  }, [sessions]);
  const sessionPagination = usePagination(sortedSessions, 8);
  const sessionSubjectOptions = useMemo(() => uniqueSessionOptions(sessions, (session) => ({
    id: session.subjectId,
    label: session.subjectName
  })), [sessions]);
  const sessionTeacherOptions = useMemo(() => uniqueSessionOptions(sessions, (session) => ({
    id: session.teacherId,
    label: session.teacherFullName
  })), [sessions]);

  const selectedAssignment = assignments.find((assignment) => assignment.id === form.surveyAssignmentId);
  const publicSurveyUrl = selectedSession?.accessCode
    ? `${window.location.origin}/survey/${encodeURIComponent(selectedSession.accessCode)}`
    : null;

  if (!canManageSessions) {
    return (
      <section className="app-content access-denied-panel">
        <p className="eyebrow">Sin acceso</p>
        <h2>No tenés permisos para gestionar sesiones de encuesta.</h2>
        <p>Solicitá el permiso {MANAGE_SESSIONS_PERMISSION} a la administración del sistema.</p>
      </section>
    );
  }

  if (contextual && !academicContext.selectedCareer) {
    return (
      <section className="app-content empty-detail">
        <h3>Seleccioná una carrera.</h3>
        <p>Elegí una carrera en el panel lateral para gestionar sus sesiones.</p>
      </section>
    );
  }

  if (contextual && !academicContext.selectedAcademicCycle) {
    return (
      <section className="app-content empty-detail">
        <h3>Seleccioná un ciclo lectivo.</h3>
        <p>Elegí un ciclo lectivo para ver o crear sesiones de encuesta.</p>
      </section>
    );
  }

  async function loadData(token: string, signal?: AbortSignal) {
    setLoadState('loading');
    setPageError(null);

    try {
      const sessionQueryFilters = contextual
        ? {
            careerId: academicContext.careerId,
            academicCycleId: academicContext.academicCycleId,
            subjectId: filtersStateValue(filters.subjectId),
            teacherId: filtersStateValue(filters.teacherId),
            status: filtersStateValue(filters.status)
          }
        : {};
      const [nextAssignments, nextSessions] = await Promise.all([
        getSurveyAssignments(token, auth.logout, {
          includeInactive: false,
          careerId: contextual ? academicContext.careerId : undefined,
          academicCycleId: contextual ? academicContext.academicCycleId : undefined
        }, signal),
        getSurveySessions(token, auth.logout, sessionQueryFilters, signal)
      ]);
      const activeAssignments = nextAssignments.filter((assignment) => assignment.isActive);

      setAssignments(activeAssignments);
      setSessions(nextSessions);
      setSelectedSession((current) => findNextSelectedSession(current, nextSessions));
      setForm((current) => {
        const currentAssignment = activeAssignments.find(
          (assignment) => assignment.id === current.surveyAssignmentId
        );
        const nextAssignment = currentAssignment ?? activeAssignments[0];

        return {
          ...current,
          surveyAssignmentId: nextAssignment?.id ?? '',
          title: currentAssignment ? current.title : createSuggestedTitle(nextAssignment)
        };
      });
      setLoadState('ready');
    } catch (error) {
      if (signal?.aborted) {
        return;
      }

      setPageError(getFriendlyError(error));
      setLoadState('error');
    }
  }

  async function refreshSelectedSession(sessionId = selectedSession?.id) {
    if (!accessToken || !sessionId) {
      return;
    }

    setActiveAction('refresh');
    setActionError(null);

    try {
      const refreshedSession = await getSurveySession(sessionId, accessToken, auth.logout);
      upsertSession(refreshedSession);
      setSelectedSession(refreshedSession);
      setSuccessMessage('Sesión actualizada.');
    } catch (error) {
      setActionError(getFriendlyError(error));
    } finally {
      setActiveAction(null);
    }
  }

  async function handleCreateSession(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    if (!accessToken) {
      return;
    }

    const validationError = validateForm(form);

    if (validationError) {
      setFormError(validationError);
      return;
    }

    setIsCreating(true);
    setFormError(null);
    setActionError(null);
    setSuccessMessage(null);

    try {
      const request = buildCreateRequest(form);
      const createdSession = await createSurveySession(request, accessToken, auth.logout);
      upsertSession(createdSession);
      setSelectedSession(createdSession);
      setIsFormVisible(false);
      setSuccessMessage('Sesión creada. Podés abrirla cuando el aula esté lista.');
      setForm(createInitialForm(form.surveyAssignmentId, selectedAssignment));
    } catch (error) {
      setFormError(getFriendlyError(error));
    } finally {
      setIsCreating(false);
    }
  }

  async function handleOpenSession() {
    if (!accessToken || !selectedSession) {
      return;
    }

    setActiveAction('open');
    setActionError(null);
    setSuccessMessage(null);

    try {
      await openSurveySession(selectedSession.id, accessToken, auth.logout);
      const refreshedSession = await getSurveySession(selectedSession.id, accessToken, auth.logout);
      upsertSession(refreshedSession);
      setSelectedSession(refreshedSession);
      setSuccessMessage('Sesión abierta.');
    } catch (error) {
      setActionError(getFriendlyError(error));
    } finally {
      setActiveAction(null);
    }
  }

  async function handleCloseSession() {
    if (!accessToken || !selectedSession) {
      return;
    }

    const confirmed = window.confirm(
      'Al cerrar la sesión, el código QR dejará de aceptar respuestas. ¿Deseás continuar?'
    );

    if (!confirmed) {
      return;
    }

    setActiveAction('close');
    setActionError(null);
    setSuccessMessage(null);

    try {
      await closeSurveySession(selectedSession.id, accessToken, auth.logout);
      const refreshedSession = await getSurveySession(selectedSession.id, accessToken, auth.logout);
      upsertSession(refreshedSession);
      setSelectedSession(refreshedSession);
      setSuccessMessage('Sesión cerrada. Esta sesión ya no acepta respuestas.');
    } catch (error) {
      setActionError(getFriendlyError(error));
    } finally {
      setActiveAction(null);
    }
  }

  function upsertSession(nextSession: SurveySessionDto) {
    setSessions((current) => {
      const exists = current.some((session) => session.id === nextSession.id);

      return exists
        ? current.map((session) => (session.id === nextSession.id ? nextSession : session))
        : [nextSession, ...current];
    });
  }

  function handleAssignmentChange(assignmentId: string) {
    const assignment = assignments.find((item) => item.id === assignmentId);

    setForm((current) => ({
      ...current,
      surveyAssignmentId: assignmentId,
      title: current.title.trim() ? current.title : createSuggestedTitle(assignment)
    }));
    setFormError(null);
  }

  async function handleCopyLink() {
    if (!publicSurveyUrl) {
      return;
    }

    try {
      await navigator.clipboard.writeText(publicSurveyUrl);
      setSuccessMessage('Enlace copiado.');
    } catch {
      setActionError('No fue posible copiar el enlace.');
    }
  }

  if (loadState === 'loading') {
    return (
      <section className="app-content" aria-live="polite">
        <p>Cargando sesiones...</p>
      </section>
    );
  }

  if (loadState === 'error') {
    return (
      <section className="app-content" role="alert">
        <p className="eyebrow">Sesiones de encuesta</p>
        <h2>No pudimos cargar las sesiones</h2>
        <p>{pageError ?? 'No fue posible conectarse con el sistema.'}</p>
        <button
          className="secondary-button"
          onClick={() => accessToken && void loadData(accessToken)}
          type="button"
        >
          Reintentar
        </button>
      </section>
    );
  }

  return (
    <section className="app-content sessions-page">
      <header className="sessions-header">
        <div>
          <p className="eyebrow">{contextual ? 'Carrera seleccionada' : 'Operación de aula'}</p>
          <h2>Sesiones de encuesta</h2>
          <p>
            {contextual
              ? 'Gestioná sesiones temporales para la carrera y ciclo lectivo seleccionados.'
              : 'Gestioná sesiones temporales y compartí el QR con estudiantes.'}
          </p>
        </div>
        <div className="sessions-actions">
          <button
            className="secondary-button"
            disabled={!accessToken}
            onClick={() => accessToken && void loadData(accessToken)}
            type="button"
          >
            Actualizar listado
          </button>
          <button className="primary-button" onClick={() => setIsFormVisible(true)} type="button">
            Nueva sesión
          </button>
        </div>
      </header>

      {contextual ? (
        <div className="surveys-filters" aria-label="Filtros de sesiones">
          <label>
            <span>Materia</span>
            <select
              className="text-input"
              onChange={(event) => setFilters((current) => ({ ...current, subjectId: event.target.value }))}
              value={filters.subjectId}
            >
              <option value="">Todas</option>
              {sessionSubjectOptions.map((option) => (
                <option key={option.id} value={option.id}>
                  {option.label}
                </option>
              ))}
            </select>
          </label>
          <label>
            <span>Docente</span>
            <select
              className="text-input"
              onChange={(event) => setFilters((current) => ({ ...current, teacherId: event.target.value }))}
              value={filters.teacherId}
            >
              <option value="">Todos</option>
              {sessionTeacherOptions.map((option) => (
                <option key={option.id} value={option.id}>
                  {option.label}
                </option>
              ))}
            </select>
          </label>
          <label>
            <span>Estado</span>
            <select
              className="text-input"
              onChange={(event) =>
                setFilters((current) => ({ ...current, status: event.target.value as SessionFiltersState['status'] }))
              }
              value={filters.status}
            >
              <option value="">Todos</option>
              <option value="Created">Creada</option>
              <option value="Open">Abierta</option>
              <option value="Closed">Cerrada</option>
              <option value="Expired">Expirada</option>
              <option value="Cancelled">Cancelada</option>
            </select>
          </label>
          <button
            className="secondary-button"
            onClick={() => setFilters({ subjectId: '', teacherId: '', status: '' })}
            type="button"
          >
            Limpiar filtros
          </button>
        </div>
      ) : null}

      {successMessage ? (
        <div className="success-message" role="status">
          {successMessage}
        </div>
      ) : null}

      {isFormVisible ? (
        <form className="session-form" noValidate onSubmit={handleCreateSession}>
          <header>
            <h3>Nueva sesión</h3>
            <button
              className="secondary-button"
              onClick={() => {
                setIsFormVisible(false);
                setFormError(null);
              }}
              type="button"
            >
              Cancelar
            </button>
          </header>

          <label>
            <span>Asignación de encuesta</span>
            <select
              className="text-input"
              onChange={(event) => handleAssignmentChange(event.target.value)}
              required
              value={form.surveyAssignmentId}
            >
              <option value="">Seleccionar asignación</option>
              {assignments.map((assignment) => (
                <option key={assignment.id} value={assignment.id}>
                  {formatAssignmentOption(assignment)}
                </option>
              ))}
            </select>
          </label>

          <label>
            <span>Título de sesión</span>
            <input
              className="text-input"
              maxLength={200}
              onChange={(event) => {
                setForm((current) => ({ ...current, title: event.target.value }));
                setFormError(null);
              }}
              type="text"
              value={form.title}
            />
          </label>

          <label>
            <span>Ubicación / aula</span>
            <input
              className="text-input"
              maxLength={200}
              onChange={(event) => {
                setForm((current) => ({ ...current, location: event.target.value }));
                setFormError(null);
              }}
              placeholder="Aula 3"
              type="text"
              value={form.location}
            />
          </label>

          <label>
            <span>Vencimiento</span>
            <input
              className="text-input"
              onChange={(event) => {
                setForm((current) => ({ ...current, expiresAtLocal: event.target.value }));
                setFormError(null);
              }}
              required
              type="datetime-local"
              value={form.expiresAtLocal}
            />
          </label>

          {formError ? (
            <p className="submit-error" role="alert">
              {formError}
            </p>
          ) : null}

          <button className="primary-button" disabled={isCreating} type="submit">
            {isCreating ? 'Creando...' : 'Crear sesión'}
          </button>
        </form>
      ) : null}

      <div className="sessions-workspace">
        <div className="sessions-list" aria-label="Sesiones recientes">
          <h3>Sesiones recientes</h3>
          {sortedSessions.length === 0 ? (
            <p>
              {contextual
                ? 'No hay sesiones disponibles para esta carrera y ciclo lectivo.'
                : 'No hay sesiones disponibles.'}
            </p>
          ) : (
            <>
              <PaginationControls
                firstItem={sessionPagination.firstItem}
                itemLabel="sesiones"
                lastItem={sessionPagination.lastItem}
                onPageChange={sessionPagination.setPage}
                onPageSizeChange={sessionPagination.setPageSize}
                page={sessionPagination.page}
                pageSize={sessionPagination.pageSize}
                pageSizeOptions={[5, 8, 12]}
                totalItems={sessionPagination.totalItems}
                totalPages={sessionPagination.totalPages}
              />
              {sessionPagination.items.map((session) => (
                <button
                  className={`session-list-item ${
                    selectedSession?.id === session.id ? 'session-list-item--active' : ''
                  }`}
                  key={session.id}
                  onClick={() => {
                    setSelectedSession(session);
                    setActionError(null);
                    setSuccessMessage(null);
                  }}
                  type="button"
                >
                  <span>{session.title || session.surveyTitle}</span>
                  <small>{session.subjectName}</small>
                  <StatusBadge status={session.status} />
                </button>
              ))}
            </>
          )}
        </div>

        <div className="session-detail">
          {selectedSession ? (
            <>
              <SessionSummary session={selectedSession} />

              {actionError ? (
                <p className="submit-error" role="alert">
                  {actionError}
                </p>
              ) : null}

              <div className="session-command-bar">
                <button
                  className="secondary-button"
                  disabled={activeAction !== null}
                  onClick={() => void refreshSelectedSession()}
                  type="button"
                >
                  {activeAction === 'refresh' ? 'Actualizando...' : 'Actualizar'}
                </button>

                {selectedSession.status === 'Created' ? (
                  <button
                    className="primary-button"
                    disabled={activeAction !== null}
                    onClick={() => void handleOpenSession()}
                    type="button"
                  >
                    {activeAction === 'open' ? 'Abriendo...' : 'Abrir sesión'}
                  </button>
                ) : null}

                {selectedSession.status === 'Open' ? (
                  <button
                    className="danger-button"
                    disabled={activeAction !== null}
                    onClick={() => void handleCloseSession()}
                    type="button"
                  >
                    {activeAction === 'close' ? 'Cerrando...' : 'Cerrar sesión'}
                  </button>
                ) : null}
              </div>

              {selectedSession.status === 'Created' ? (
                <div className="inline-message">
                  Abrí la sesión para habilitar el enlace público y mostrar el QR a estudiantes.
                </div>
              ) : null}

              {selectedSession.status === 'Open' && publicSurveyUrl ? (
                <QrPanel
                  onCopy={() => void handleCopyLink()}
                  session={selectedSession}
                  surveyUrl={publicSurveyUrl}
                />
              ) : null}

              {selectedSession.status === 'Closed' ? (
                <div className="inline-message">Esta sesión ya no acepta respuestas.</div>
              ) : null}
            </>
          ) : (
            <div className="empty-detail">
              <h3>Seleccioná una sesión</h3>
              <p>Creá una nueva sesión o elegí una existente para ver el detalle operativo.</p>
            </div>
          )}
        </div>
      </div>
    </section>
  );
}

function SessionSummary({ session }: { session: SurveySessionDto }) {
  return (
    <article className="session-summary">
      <header>
        <div>
          <p className="eyebrow">{session.subjectName}</p>
          <h3>{session.title || session.surveyTitle}</h3>
        </div>
        <StatusBadge status={session.status} />
      </header>

      <dl className="session-meta">
        <div>
          <dt>Materia</dt>
          <dd>{session.subjectName}</dd>
        </div>
        <div>
          <dt>Docente</dt>
          <dd>{session.teacherFullName}</dd>
        </div>
        <div>
          <dt>Carrera</dt>
          <dd>{session.careerName}</dd>
        </div>
        <div>
          <dt>Ubicación</dt>
          <dd>{session.location || 'Sin ubicación'}</dd>
        </div>
        <div>
          <dt>Vencimiento</dt>
          <dd>{formatDateTime(session.expiresAtUtc)}</dd>
        </div>
        <div>
          <dt>Ciclo</dt>
          <dd>
            {session.academicCycleYear} · {formatPeriod(session.academicCyclePeriod)}
          </dd>
        </div>
      </dl>
    </article>
  );
}

function QrPanel({
  onCopy,
  session,
  surveyUrl
}: {
  onCopy: () => void;
  session: SurveySessionDto;
  surveyUrl: string;
}) {
  return (
    <section className="qr-panel" aria-label="QR de acceso público">
      <div className="qr-panel__code">
        <QRCodeSVG
          bgColor="#ffffff"
          fgColor="#122033"
          level="M"
          marginSize={2}
          size={288}
          value={surveyUrl}
        />
      </div>
      <div className="qr-panel__content">
        <p className="eyebrow">Sesión abierta</p>
        <h3>{session.title || session.surveyTitle}</h3>
        <p>{session.subjectName} · {session.teacherFullName}</p>
        <label>
          <span>Enlace para estudiantes</span>
          <input className="text-input" readOnly type="text" value={surveyUrl} />
        </label>
        <div className="qr-actions">
          <button className="secondary-button" onClick={onCopy} type="button">
            Copiar enlace
          </button>
          <a className="secondary-link-button" href={surveyUrl} rel="noreferrer" target="_blank">
            Abrir encuesta
          </a>
        </div>
      </div>
    </section>
  );
}

function StatusBadge({ status }: { status: SurveySessionStatus }) {
  return <span className={`status-badge status-badge--${status.toLowerCase()}`}>{translateStatus(status)}</span>;
}

function createInitialForm(
  surveyAssignmentId = '',
  assignment?: SurveyAssignmentDto
): SessionFormState {
  return {
    surveyAssignmentId,
    title: createSuggestedTitle(assignment),
    location: '',
    expiresAtLocal: toDateTimeLocalValue(addHours(new Date(), SESSION_DURATION_HOURS))
  };
}

function buildCreateRequest(form: SessionFormState): CreateSurveySessionRequest {
  return {
    surveyAssignmentId: form.surveyAssignmentId,
    title: trimmedOrNull(form.title),
    location: trimmedOrNull(form.location),
    expiresAtUtc: new Date(form.expiresAtLocal).toISOString()
  };
}

function validateForm(form: SessionFormState): string | null {
  if (!form.surveyAssignmentId) {
    return 'Seleccioná una asignación de encuesta.';
  }

  if (!form.expiresAtLocal) {
    return 'Indicá el vencimiento de la sesión.';
  }

  const expiresAt = new Date(form.expiresAtLocal);

  if (Number.isNaN(expiresAt.getTime())) {
    return 'El vencimiento no es válido.';
  }

  if (expiresAt <= new Date()) {
    return 'El vencimiento debe ser una fecha futura.';
  }

  if (expiresAt > addHours(new Date(), 24)) {
    return 'La sesión no puede durar más de 24 horas.';
  }

  return null;
}

function compareSessions(left: SurveySessionDto, right: SurveySessionDto): number {
  const statusDifference = getStatusPriority(left.status) - getStatusPriority(right.status);

  if (statusDifference !== 0) {
    return statusDifference;
  }

  return Date.parse(right.createdAtUtc) - Date.parse(left.createdAtUtc);
}

function getStatusPriority(status: SurveySessionStatus): number {
  switch (status) {
    case 'Open':
      return 0;
    case 'Created':
      return 1;
    case 'Closed':
      return 2;
    case 'Expired':
      return 3;
    case 'Cancelled':
      return 4;
  }
}

function findNextSelectedSession(
  current: SurveySessionDto | null,
  sessions: SurveySessionDto[]
): SurveySessionDto | null {
  if (current) {
    return sessions.find((session) => session.id === current.id) ?? null;
  }

  return [...sessions].sort(compareSessions)[0] ?? null;
}

function formatAssignmentOption(assignment: SurveyAssignmentDto): string {
  return `${assignment.subjectName} · ${assignment.teacherFullName} · ${assignment.careerName} · ${assignment.academicCycleYear} ${formatPeriod(assignment.academicCyclePeriod)}`;
}

function createSuggestedTitle(assignment?: SurveyAssignmentDto): string {
  return assignment ? `Evaluación ${assignment.subjectName}` : '';
}

function translateStatus(status: SurveySessionStatus): string {
  switch (status) {
    case 'Created':
      return 'Creada';
    case 'Open':
      return 'Abierta';
    case 'Closed':
      return 'Cerrada';
    case 'Cancelled':
      return 'Cancelada';
    case 'Expired':
      return 'Expirada';
  }
}

function getFriendlyError(error: unknown): string {
  if (error instanceof ApiClientError) {
    if (error.status === 401) {
      return 'La sesión de usuario venció. Volvé a iniciar sesión.';
    }

    if (error.status === 403) {
      return 'No tenés permisos para gestionar sesiones de encuesta.';
    }

    if (error.status === 404) {
      return 'La sesión de encuesta no fue encontrada.';
    }

    if (error.status === 400) {
      return getValidationMessage(error.code);
    }
  }

  return 'No fue posible conectarse con el sistema.';
}

function getValidationMessage(code: string | null): string {
  switch (code) {
    case 'SurveySession.SurveyAssignmentIdRequired':
      return 'Seleccioná una asignación de encuesta.';
    case 'SurveySession.ExpiresAtUtcMustBeUtc':
      return 'El vencimiento debe enviarse en UTC.';
    case 'SurveySession.ExpiresAtUtcMustBeFuture':
      return 'El vencimiento debe ser una fecha futura.';
    case 'SurveySession.ExpiresAtUtcTooFar':
      return 'La sesión no puede durar más de 24 horas.';
    case 'SurveySession.TitleTooLong':
      return 'El título debe tener 200 caracteres o menos.';
    case 'SurveySession.LocationTooLong':
      return 'La ubicación debe tener 200 caracteres o menos.';
    default:
      return 'No fue posible procesar la solicitud. Revisá los datos e intentá nuevamente.';
  }
}

function formatDateTime(value: string): string {
  const date = new Date(value);

  if (Number.isNaN(date.getTime())) {
    return 'Fecha no disponible';
  }

  return new Intl.DateTimeFormat('es-AR', {
    dateStyle: 'short',
    timeStyle: 'short'
  }).format(date);
}

function formatPeriod(period: string): string {
  switch (period) {
    case 'Annual':
      return 'Anual';
    case 'FirstSemester':
      return 'Primer semestre';
    case 'SecondSemester':
      return 'Segundo semestre';
    case 'FirstQuarter':
      return 'Primer cuatrimestre';
    case 'SecondQuarter':
      return 'Segundo cuatrimestre';
    default:
      return period;
  }
}

function toDateTimeLocalValue(date: Date): string {
  const offsetMs = date.getTimezoneOffset() * 60_000;
  return new Date(date.getTime() - offsetMs).toISOString().slice(0, 16);
}

function addHours(date: Date, hours: number): Date {
  return new Date(date.getTime() + hours * 60 * 60 * 1000);
}

function trimmedOrNull(value: string): string | null {
  const trimmed = value.trim();
  return trimmed ? trimmed : null;
}

function filtersStateValue(value: string): string | undefined {
  return value || undefined;
}

function uniqueSessionOptions(
  sessions: SurveySessionDto[],
  selector: (session: SurveySessionDto) => { id: string; label: string }
): Array<{ id: string; label: string }> {
  const options = new Map<string, { id: string; label: string }>();

  sessions.forEach((session) => {
    const option = selector(session);
    options.set(option.id, option);
  });

  return [...options.values()].sort((left, right) => left.label.localeCompare(right.label));
}
