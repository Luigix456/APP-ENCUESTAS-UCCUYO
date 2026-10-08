import { useEffect, useMemo, useState, type FormEvent } from 'react';
import { Link } from 'react-router-dom';
import { ConfirmDialog, Modal } from '../../components/ui/Modal';
import { useToast } from '../../components/ui/ToastProvider';
import {
  createTeacher,
  createTeacherSubjectAssignment,
  createSubject,
  deactivateSubject,
  deactivateTeacherSubjectAssignment,
  downloadSubjectEnrollmentImportTemplate,
  getCareerAttention,
  getCareerTeachers,
  getSubjectEnrollments,
  getSubjects,
  getTeachers,
  getTeacherSubjectAssignments,
  importSubjectEnrollments,
  previewSubjectEnrollmentImport,
  setSubjectEnrollment,
  updateSubject,
  updateTeacher
} from '../../api/academicCatalogApi';
import { getCareerParticipationDashboard } from '../../api/dashboardApi';
import { useAuth } from '../../auth/AuthProvider';
import { formatParticipation } from '../../components/ResponseProgress';
import type {
  AcademicCycleDto,
  AcademicAttentionDto,
  AcademicAttentionItemDto,
  CreateTeacherRequest,
  CreateTeacherSubjectAssignmentRequest,
  CreateSubjectRequest,
  SubjectDto,
  SubjectEnrollmentDto,
  SubjectEnrollmentImportPreviewDto,
  SubjectPeriod,
  TeacherDto,
  TeacherSubjectAssignmentDto,
  UpdateSubjectRequest,
  UpdateTeacherRequest
} from '../../types/academicCatalog';
import { SUBJECT_PERIODS } from '../../types/academicCatalog';
import type {
  CareerParticipationDashboardDto,
  CareerParticipationDashboardItemDto
} from '../../types/dashboard';
import { formatAcademicCycle, formatPeriod, getFriendlyCatalogError } from '../academic-catalog/academicCatalogUi';
import { useAcademicContext } from './AcademicContextProvider';

type LoadState = 'idle' | 'loading' | 'ready' | 'error';

const READ_CATALOG = 'academic.catalog.read';
const MANAGE_CATALOG = 'academic.catalog.manage';
const SUBJECT_ENROLLMENT_IMPORT_MAX_FILE_SIZE_BYTES = 5 * 1024 * 1024;

export function CareerOverviewPage() {
  const auth = useAuth();
  const context = useAcademicContext();
  const [dashboard, setDashboard] = useState<CareerParticipationDashboardDto | null>(null);
  const [attentionItems, setAttentionItems] = useState<AcademicAttentionItemDto[]>([]);
  const [attentionError, setAttentionError] = useState<string | null>(null);
  const [dashboardError, setDashboardError] = useState<string | null>(null);
  const [dashboardSort, setDashboardSort] = useState<DashboardSort>('lowest');
  const [state, setState] = useState<LoadState>('idle');
  const [error, setError] = useState<string | null>(null);
  const accessToken = auth.accessToken;
  const canReadCatalog = auth.hasPermission(READ_CATALOG) || auth.hasPermission(MANAGE_CATALOG);
  const canReadResults = auth.hasPermission('results.read_all') || auth.hasPermission('results.read_career');

  useEffect(() => {
    if (!accessToken || !context.careerId) {
      setDashboard(null);
      setAttentionItems([]);
      setAttentionError(null);
      setDashboardError(null);
      setState('ready');
      return;
    }

    const abortController = new AbortController();

    async function loadOverview() {
      if (!accessToken) {
        return;
      }

      setState('loading');
      setError(null);
      setAttentionError(null);
      setDashboardError(null);

      try {
        let nextAttentionItems: AcademicAttentionItemDto[] = [];
        let nextDashboard: CareerParticipationDashboardDto | null = null;
        let nextAttentionError: string | null = null;
        let nextDashboardError: string | null = null;

        if (context.academicCycleId) {
          const attentionPromise: Promise<AcademicAttentionDto> = canReadCatalog
            ? getCareerAttention(context.careerId, context.academicCycleId, {
                accessToken,
                onUnauthorized: auth.logout,
                signal: abortController.signal
              })
            : Promise.resolve({ items: [] });
          const dashboardPromise: Promise<CareerParticipationDashboardDto | null> = canReadResults
            ? getCareerParticipationDashboard(
                context.careerId,
                context.academicCycleId,
                accessToken,
                auth.logout,
                abortController.signal
              )
            : Promise.resolve(null);
          const [attentionResult, dashboardResult] = await Promise.allSettled([
            attentionPromise,
            dashboardPromise
          ] as const);

          if (attentionResult.status === 'fulfilled') {
            nextAttentionItems = attentionResult.value.items;
          } else {
            nextAttentionError = 'No pudimos cargar las alertas de configuración.';
          }

          if (dashboardResult.status === 'fulfilled') {
            nextDashboard = dashboardResult.value;
          } else {
            nextDashboardError = 'No pudimos cargar el seguimiento del ciclo.';
          }
        }

        setDashboard(nextDashboard);
        setAttentionItems(nextAttentionItems);
        setAttentionError(nextAttentionError);
        setDashboardError(nextDashboardError);
        setState('ready');
      } catch (loadError) {
        if (abortController.signal.aborted) {
          return;
        }

        setError('No fue posible cargar el resumen de la carrera.');
        setState('error');
      }
    }

    void loadOverview();
    return () => abortController.abort();
  }, [
    accessToken,
    auth.logout,
    canReadCatalog,
    canReadResults,
    context.academicCycleId,
    context.careerId
  ]);

  const sortedDashboardItems = useMemo(
    () => sortDashboardItems(dashboard?.items ?? [], dashboardSort),
    [dashboard?.items, dashboardSort]
  );

  const guard = useContextGuard();

  if (guard) {
    return guard;
  }

  return (
    <section className="app-content dashboard-page">
      <ContextHeader eyebrow="Contexto de carrera" title="Resumen" />
      {state === 'loading' ? <p aria-live="polite">Cargando resumen...</p> : null}
      {state === 'error' ? <p className="submit-error">{error}</p> : null}

      {state === 'ready' && !context.academicCycleId ? (
        <div className="empty-detail">
          <h3>Elegí un ciclo lectivo para ver el seguimiento.</h3>
          <p>El panel de participación necesita un ciclo para mostrar métricas comparables.</p>
        </div>
      ) : null}

      {state === 'ready' && context.academicCycleId && !canReadResults ? (
        <div className="empty-detail">
          <h3>Seguimiento de resultados no disponible para tu perfil.</h3>
          <p>Las métricas de participación requieren permisos de consulta de resultados.</p>
        </div>
      ) : null}

      {state === 'ready' && context.academicCycleId && canReadResults && dashboardError ? (
        <div className="empty-detail" role="alert">
          <h3>{dashboardError}</h3>
          <p>Intentá nuevamente o consultá al equipo técnico si el problema continúa.</p>
        </div>
      ) : null}

      {state === 'ready' && dashboard ? (
        <section className="participation-dashboard" aria-labelledby="participation-dashboard-title">
          <div className="participation-dashboard__header">
            <div>
              <p className="eyebrow">Seguimiento del ciclo</p>
              <h3 id="participation-dashboard-title">Participación académica</h3>
              <p>{dashboard.careerName} · {dashboard.academicCycleLabel}</p>
            </div>
          </div>

          <div className="dashboard-metrics dashboard-metrics--participation">
            <DashboardMetricCard
              label="Materias con respuestas"
              value={`${dashboard.subjectsWithResponses} de ${dashboard.totalSubjects}`}
              hint="Materias distintas con al menos una respuesta"
            />
            <DashboardMetricCard
              label="Participación promedio"
              value={dashboard.averageParticipationPercentage === null ? 'Sin datos' : formatParticipation(dashboard.averageParticipationPercentage)}
              hint="Promedio de encuestas a estudiantes con cantidad esperada"
            />
            <DashboardMetricCard
              label="Sesiones abiertas"
              value={dashboard.openSessionsCount}
              hint="Sesiones activas en este ciclo"
            />
            <DashboardMetricCard
              label="Baja participación"
              value={dashboard.lowParticipationAssignmentsCount}
              hint={`Menos de ${formatParticipation(dashboard.lowParticipationThresholdPercentage)}`}
            />
          </div>

          <div className="participation-list-header">
            <div>
              <p className="eyebrow">Participación por encuesta</p>
              <h3>{dashboard.studentSurveyAssignmentsCount} encuestas a estudiantes</h3>
            </div>
            <label>
              <span>Ordenar por</span>
              <select
                className="text-input"
                value={dashboardSort}
                onChange={(event) => setDashboardSort(event.target.value as DashboardSort)}
              >
                <option value="lowest">Menor participación</option>
                <option value="highest">Mayor participación</option>
                <option value="subject">Materia A-Z</option>
              </select>
            </label>
          </div>

          {sortedDashboardItems.length > 0 ? (
            <div className="participation-assignment-list">
              {sortedDashboardItems.map((item) => (
                <ParticipationAssignmentCard item={item} key={item.surveyAssignmentId} />
              ))}
            </div>
          ) : (
            <div className="attention-empty">
              <h4>No hay encuestas a estudiantes para este ciclo.</h4>
              <p>Cuando se creen asignaciones Student, aparecerán en este seguimiento.</p>
            </div>
          )}
        </section>
      ) : null}

      {state === 'ready' && context.academicCycleId && attentionError ? (
        <section className="attention-panel" aria-labelledby="attention-title">
          <div className="attention-panel__header">
            <div>
              <p className="eyebrow">Seguimiento académico</p>
              <h3 id="attention-title">Requiere atención</h3>
            </div>
          </div>
          <div className="attention-empty" role="alert">
            <h4>{attentionError}</h4>
            <p>Intentá nuevamente o revisá la conexión con la API.</p>
          </div>
        </section>
      ) : null}

      {state === 'ready' && context.academicCycleId && !attentionError ? (
        <section className="attention-panel" aria-labelledby="attention-title">
          <div className="attention-panel__header">
            <div>
              <p className="eyebrow">Seguimiento académico</p>
              <h3 id="attention-title">Requiere atención</h3>
            </div>
            <strong>{attentionItems.length}</strong>
          </div>
          {attentionItems.length > 0 ? (
            <div className="attention-list">
              {attentionItems.map((item) => (
                <article className={`attention-card attention-card--${item.severity}`} key={item.code}>
                  <div className="attention-card__icon" aria-hidden="true">
                    {item.severity === 'warning' ? '!' : 'i'}
                  </div>
                  <div>
                    <h4>{item.title}</h4>
                    <p>{item.description}</p>
                  </div>
                  <Link className="secondary-button" to={getAttentionActionRoute(item.actionCode)}>
                    {getAttentionActionLabel(item.actionCode)}
                  </Link>
                </article>
              ))}
            </div>
          ) : (
            <div className="attention-empty">
              <h4>Todo está listo para trabajar en este ciclo lectivo.</h4>
              <p>No detectamos asuntos de configuración pendientes para este contexto.</p>
            </div>
          )}
        </section>
      ) : null}
    </section>
  );
}

type DashboardSort = 'lowest' | 'highest' | 'subject';

function DashboardMetricCard({
  label,
  value,
  hint
}: {
  label: string;
  value: number | string;
  hint: string;
}) {
  return (
    <article className="dashboard-metric-card dashboard-metric-card--static">
      <span>{label}</span>
      <strong>{value}</strong>
      <small>{hint}</small>
    </article>
  );
}

function ParticipationAssignmentCard({ item }: { item: CareerParticipationDashboardItemDto }) {
  const progressWidth = item.participationPercentage === null
    ? 0
    : Math.min(100, Math.max(0, item.participationPercentage));

  return (
    <article className="participation-assignment-card">
      <header>
        <div>
          <p className="eyebrow">{item.subjectName}</p>
          <h4>{item.teacherName}</h4>
          <p>{item.surveyTitle} · v{item.surveyVersionNumber}</p>
        </div>
        {item.isLowParticipation ? (
          <span className="status-badge status-badge--warning">Participación baja</span>
        ) : null}
      </header>
      <div className="participation-assignment-card__progress">
        <div className="participation-assignment-card__main">
          <strong>{formatResponseCount(item)}</strong>
          <span>{formatParticipationText(item)}</span>
        </div>
        {item.participationPercentage === null ? (
          <p className="form-helper">Participación no disponible.</p>
        ) : (
          <div
            aria-label={`Participación: ${formatParticipation(item.participationPercentage)}`}
            aria-valuemax={100}
            aria-valuemin={0}
            aria-valuenow={Math.round(progressWidth)}
            className="participation-assignment-card__bar"
            role="progressbar"
          >
            <span style={{ width: `${progressWidth}%` }} />
          </div>
        )}
      </div>
      <Link className="secondary-button" to={`/app/results/assignments/${item.surveyAssignmentId}`}>
        {item.detailedResultsAvailable ? 'Ver resultados' : 'Ver seguimiento'}
      </Link>
    </article>
  );
}

function sortDashboardItems(
  items: CareerParticipationDashboardItemDto[],
  sort: DashboardSort
): CareerParticipationDashboardItemDto[] {
  return [...items].sort((left, right) => {
    if (sort === 'subject') {
      return compareDashboardItems(left, right);
    }

    const leftValue = left.participationPercentage;
    const rightValue = right.participationPercentage;

    if (leftValue === null && rightValue === null) {
      return compareDashboardItems(left, right);
    }

    if (leftValue === null) {
      return 1;
    }

    if (rightValue === null) {
      return -1;
    }

    const difference = sort === 'lowest'
      ? leftValue - rightValue
      : rightValue - leftValue;

    return difference === 0 ? compareDashboardItems(left, right) : difference;
  });
}

function compareDashboardItems(
  left: CareerParticipationDashboardItemDto,
  right: CareerParticipationDashboardItemDto
): number {
  return left.subjectName.localeCompare(right.subjectName)
    || left.teacherName.localeCompare(right.teacherName)
    || left.surveyTitle.localeCompare(right.surveyTitle)
    || left.surveyVersionNumber - right.surveyVersionNumber;
}

function formatResponseCount(item: CareerParticipationDashboardItemDto): string {
  return item.expectedRespondentCount === null
    ? `${item.responseCount} respuestas recibidas`
    : `${item.responseCount} de ${item.expectedRespondentCount} respuestas`;
}

function formatParticipationText(item: CareerParticipationDashboardItemDto): string {
  return item.participationPercentage === null
    ? 'Participación no disponible'
    : `${formatParticipation(item.participationPercentage)} de participación`;
}

function isSubjectEnrollmentImportFileAllowed(file: File): boolean {
  const fileName = file.name.toLowerCase();
  return fileName.endsWith('.csv') || fileName.endsWith('.xlsx');
}

function formatBytes(value: number): string {
  if (value < 1024) {
    return `${value} bytes`;
  }

  const kiloBytes = value / 1024;

  if (kiloBytes < 1024) {
    return `${kiloBytes.toFixed(1)} KB`;
  }

  return `${(kiloBytes / 1024).toFixed(1)} MB`;
}

function downloadBlob(blob: Blob, fileName: string) {
  const url = URL.createObjectURL(blob);
  const link = document.createElement('a');
  link.href = url;
  link.download = fileName;
  document.body.appendChild(link);
  link.click();
  link.remove();
  URL.revokeObjectURL(url);
}

function getDownloadFileName(contentDisposition: string | null, fallback: string): string {
  if (!contentDisposition) {
    return fallback;
  }

  const utf8Match = /filename\*=UTF-8''([^;]+)/i.exec(contentDisposition);

  if (utf8Match?.[1]) {
    return decodeURIComponent(utf8Match[1]);
  }

  const simpleMatch = /filename="?([^";]+)"?/i.exec(contentDisposition);
  return simpleMatch?.[1] ?? fallback;
}

function getImportStatusLabel(status: string): string {
  switch (status) {
    case 'Create':
      return 'Crear';
    case 'Update':
      return 'Actualizar';
    case 'Unchanged':
      return 'Sin cambios';
    case 'Error':
      return 'Error';
    default:
      return status;
  }
}

export function ContextSubjectsPage() {
  const auth = useAuth();
  const context = useAcademicContext();
  const toast = useToast();
  const [subjects, setSubjects] = useState<SubjectDto[]>([]);
  const [teachers, setTeachers] = useState<TeacherDto[]>([]);
  const [subjectAssignments, setSubjectAssignments] = useState<TeacherSubjectAssignmentDto[]>([]);
  const [subjectEnrollments, setSubjectEnrollments] = useState<SubjectEnrollmentDto[]>([]);
  const [state, setState] = useState<LoadState>('idle');
  const [error, setError] = useState<string | null>(null);
  const [yearFilter, setYearFilter] = useState('');
  const [createForm, setCreateForm] = useState<CreateSubjectRequest>({
    careerId: '',
    code: '',
    name: '',
    year: 1,
    period: 'Annual'
  });
  const [editForm, setEditForm] = useState<UpdateSubjectRequest>({
    name: '',
    year: 1,
    period: 'Annual'
  });
  const [assignmentForm, setAssignmentForm] = useState<CreateTeacherSubjectAssignmentRequest>({
    teacherId: '',
    subjectId: '',
    academicCycleId: '',
    teachingRole: ''
  });
  const [formError, setFormError] = useState<string | null>(null);
  const [showCreateModal, setShowCreateModal] = useState(false);
  const [editingSubject, setEditingSubject] = useState<SubjectDto | null>(null);
  const [deletingSubject, setDeletingSubject] = useState<SubjectDto | null>(null);
  const [assigningSubject, setAssigningSubject] = useState<SubjectDto | null>(null);
  const [enrollmentSubject, setEnrollmentSubject] = useState<SubjectDto | null>(null);
  const [enrollmentValue, setEnrollmentValue] = useState('');
  const [removingAssignment, setRemovingAssignment] = useState<TeacherSubjectAssignmentDto | null>(null);
  const [showImportModal, setShowImportModal] = useState(false);
  const [importStep, setImportStep] = useState<'prepare' | 'preview' | 'done'>('prepare');
  const [importFile, setImportFile] = useState<File | null>(null);
  const [importPreview, setImportPreview] = useState<SubjectEnrollmentImportPreviewDto | null>(null);
  const [importResult, setImportResult] = useState<{
    createdCount: number;
    updatedCount: number;
    unchangedCount: number;
    totalProcessed: number;
  } | null>(null);
  const [importError, setImportError] = useState<string | null>(null);
  const [confirmingImport, setConfirmingImport] = useState(false);
  const [busy, setBusy] = useState(false);
  const accessToken = auth.accessToken;
  const canReadCatalog = auth.hasPermission(READ_CATALOG) || auth.hasPermission(MANAGE_CATALOG);
  const canManageCatalog = auth.hasPermission(MANAGE_CATALOG);

  async function reloadSubjects() {
    if (!accessToken || !context.careerId) {
      return;
    }

    const [nextSubjects, nextAssignments, nextEnrollments] = await Promise.all([
      getSubjects(context.careerId, {
        accessToken,
        includeInactive: false,
        onUnauthorized: auth.logout
      }),
      getTeacherSubjectAssignments(
        {
          includeInactive: false,
          careerId: context.careerId,
          academicCycleId: context.academicCycleId || undefined
        },
        { accessToken, onUnauthorized: auth.logout }
      ),
      context.academicCycleId
        ? getSubjectEnrollments({
            accessToken,
            onUnauthorized: auth.logout,
            careerId: context.careerId,
            academicCycleId: context.academicCycleId
          })
        : Promise.resolve([] as SubjectEnrollmentDto[])
    ]);

    setSubjects(nextSubjects);
    setSubjectAssignments(nextAssignments);
    setSubjectEnrollments(nextEnrollments);
  }

  useEffect(() => {
    if (!accessToken || !context.careerId || !canReadCatalog) {
      setSubjects([]);
      setTeachers([]);
      setSubjectAssignments([]);
      setSubjectEnrollments([]);
      setState('ready');
      return;
    }

    const abortController = new AbortController();

    async function loadPage(signal: AbortSignal) {
      if (!accessToken) {
        return;
      }

      setState('loading');
      setError(null);

      try {
        const [nextSubjects, nextTeachers, nextAssignments, nextEnrollments] = await Promise.all([
          getSubjects(context.careerId, {
            accessToken,
            includeInactive: false,
            onUnauthorized: auth.logout,
            signal
          }),
          canManageCatalog
            ? getTeachers({
                accessToken,
                includeInactive: false,
                onUnauthorized: auth.logout,
                signal
              })
            : Promise.resolve([] as TeacherDto[]),
          getTeacherSubjectAssignments(
            {
              includeInactive: false,
              careerId: context.careerId,
              academicCycleId: context.academicCycleId || undefined
            },
            { accessToken, onUnauthorized: auth.logout, signal }
          ),
          context.academicCycleId
            ? getSubjectEnrollments({
                accessToken,
                onUnauthorized: auth.logout,
                careerId: context.careerId,
                academicCycleId: context.academicCycleId,
                signal
              })
            : Promise.resolve([] as SubjectEnrollmentDto[])
        ]);
        setSubjects(nextSubjects);
        setTeachers(nextTeachers);
        setSubjectAssignments(nextAssignments);
        setSubjectEnrollments(nextEnrollments);
        setState('ready');
      } catch (loadError) {
        if (signal.aborted) {
          return;
        }

        setError(getFriendlyCatalogError(loadError, 'No fue posible cargar materias.'));
        setState('error');
      }
    }

    void loadPage(abortController.signal);
    return () => abortController.abort();
  }, [accessToken, auth.logout, canManageCatalog, canReadCatalog, context.academicCycleId, context.careerId]);

  useEffect(() => {
    setYearFilter('');
    setCreateForm({ careerId: context.careerId, code: '', name: '', year: 1, period: 'Annual' });
    setEditingSubject(null);
    setDeletingSubject(null);
    setAssigningSubject(null);
    setEnrollmentSubject(null);
    setEnrollmentValue('');
    setRemovingAssignment(null);
    resetImportState();
  }, [context.academicCycleId, context.careerId]);

  const years = useMemo(
    () => Array.from(new Set(subjects.map((subject) => subject.year))).sort((left, right) => left - right),
    [subjects]
  );
  const filteredSubjects = yearFilter
    ? subjects.filter((subject) => subject.year === Number(yearFilter))
    : subjects;
  const groupedSubjects = groupSubjectsByYear(filteredSubjects);
  const assignmentsBySubject = useMemo(() => {
    const map = new Map<string, TeacherSubjectAssignmentDto[]>();
    subjectAssignments.forEach((assignment) => {
      map.set(assignment.subjectId, [...(map.get(assignment.subjectId) ?? []), assignment]);
    });
    return map;
  }, [subjectAssignments]);
  const enrollmentsBySubject = useMemo(() => {
    const map = new Map<string, SubjectEnrollmentDto>();
    subjectEnrollments.forEach((enrollment) => {
      map.set(enrollment.subjectId, enrollment);
    });
    return map;
  }, [subjectEnrollments]);
  const assignedTeacherIdsForForm = useMemo(() => {
    if (!assigningSubject || !assignmentForm.academicCycleId) {
      return new Set<string>();
    }

    return new Set(
      subjectAssignments
        .filter(
          (assignment) =>
            assignment.subjectId === assigningSubject.id &&
            assignment.academicCycleId === assignmentForm.academicCycleId &&
            assignment.isActive
        )
        .map((assignment) => assignment.teacherId)
    );
  }, [assignmentForm.academicCycleId, assigningSubject, subjectAssignments]);
  const availableTeachersForAssignment = useMemo(
    () => teachers.filter((teacher) => !assignedTeacherIdsForForm.has(teacher.id)),
    [assignedTeacherIdsForForm, teachers]
  );
  const guard = useContextGuard();

  if (guard) {
    return guard;
  }

  if (!canReadCatalog) {
    return <PermissionPanel message="No tenés permisos para consultar materias." />;
  }

  async function handleCreate(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    if (!accessToken || !context.careerId || busy) {
      return;
    }

    setBusy(true);
    setFormError(null);

    try {
      await createSubject(
        {
          ...createForm,
          careerId: context.careerId,
          code: createForm.code.trim(),
          name: createForm.name.trim()
        },
        { accessToken, onUnauthorized: auth.logout }
      );
      await reloadSubjects();
      setCreateForm({ careerId: context.careerId, code: '', name: '', year: 1, period: 'Annual' });
      setShowCreateModal(false);
      toast.success('Materia creada', 'La materia quedó disponible en la carrera seleccionada.');
    } catch (createError) {
      const message = getFriendlyCatalogError(createError, 'No fue posible crear la materia.');
      setFormError(message);
      toast.error('No se pudo crear la materia', message);
    } finally {
      setBusy(false);
    }
  }

  async function handleUpdate(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    if (!accessToken || !editingSubject || busy) {
      return;
    }

    setBusy(true);
    setFormError(null);

    try {
      await updateSubject(
        editingSubject.id,
        { ...editForm, name: editForm.name.trim() },
        { accessToken, onUnauthorized: auth.logout }
      );
      await reloadSubjects();
      setEditingSubject(null);
      toast.success('Materia actualizada', 'Los cambios se guardaron correctamente.');
    } catch (updateError) {
      const message = getFriendlyCatalogError(updateError, 'No fue posible actualizar la materia.');
      setFormError(message);
      toast.error('No se pudo actualizar la materia', message);
    } finally {
      setBusy(false);
    }
  }

  async function handleDeleteSubject() {
    if (!accessToken || !deletingSubject || busy) {
      return;
    }

    setBusy(true);

    try {
      await deactivateSubject(deletingSubject.id, { accessToken, onUnauthorized: auth.logout });
      await reloadSubjects();
      toast.success('Materia eliminada', 'La materia fue desactivada. Su historial académico se conserva.');
      setDeletingSubject(null);
    } catch (deleteError) {
      const message = getFriendlyCatalogError(deleteError, 'No fue posible eliminar la materia.');
      toast.error('No se pudo eliminar la materia', message);
    } finally {
      setBusy(false);
    }
  }

  async function handleAssignTeacher(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    if (!accessToken || !assigningSubject || busy) {
      return;
    }

    setBusy(true);
    setFormError(null);

    try {
      await createTeacherSubjectAssignment(
        {
          ...assignmentForm,
          subjectId: assigningSubject.id,
          teachingRole: assignmentForm.teachingRole.trim()
        },
        { accessToken, onUnauthorized: auth.logout }
      );
      await reloadSubjects();
      setAssigningSubject(null);
      toast.success(
        'Docente agregado',
        `El docente quedó agregado a ${assigningSubject.name}. La materia puede tener varios docentes en el mismo ciclo lectivo.`
      );
    } catch (assignmentError) {
      const message = getFriendlyCatalogError(assignmentError, 'No fue posible asignar el docente.');
      setFormError(message);
      toast.error('No se pudo asignar el docente', message);
    } finally {
      setBusy(false);
    }
  }

  async function handleRemoveTeacherAssignment() {
    if (!accessToken || !removingAssignment || busy) {
      return;
    }

    setBusy(true);

    try {
      await deactivateTeacherSubjectAssignment(removingAssignment.id, {
        accessToken,
        onUnauthorized: auth.logout
      });
      await reloadSubjects();
      setRemovingAssignment(null);
      toast.success(
        'Docente quitado de la materia',
        'Se desactivó solamente esta asignación. El docente, la materia y el historial se conservan.'
      );
    } catch (removeError) {
      const message = getFriendlyCatalogError(removeError, 'No fue posible quitar el docente de la materia.');
      toast.error('No se pudo quitar el docente', message);
    } finally {
      setBusy(false);
    }
  }

  function openEdit(subject: SubjectDto) {
    setFormError(null);
    setEditForm({ name: subject.name, year: subject.year, period: subject.period });
    setEditingSubject(subject);
  }

  function openAssignTeacher(subject: SubjectDto) {
    setFormError(null);
    setAssignmentForm({
      teacherId: '',
      subjectId: subject.id,
      academicCycleId: context.academicCycleId,
      teachingRole: ''
    });
    setAssigningSubject(subject);
  }

  function openEnrollmentModal(subject: SubjectDto) {
    const enrollment = enrollmentsBySubject.get(subject.id);
    setFormError(null);
    setEnrollmentSubject(subject);
    setEnrollmentValue(enrollment ? String(enrollment.enrolledStudentCount) : '');
  }

  async function handleSaveEnrollment(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    if (!accessToken || !enrollmentSubject || !context.academicCycleId || busy) {
      return;
    }

    const enrolledStudentCount = Number(enrollmentValue);

    if (!Number.isInteger(enrolledStudentCount) || enrolledStudentCount <= 0) {
      setFormError('Ingresá una cantidad de alumnos mayor a cero.');
      return;
    }

    setBusy(true);
    setFormError(null);

    try {
      await setSubjectEnrollment(
        enrollmentSubject.id,
        context.academicCycleId,
        enrolledStudentCount,
        { accessToken, onUnauthorized: auth.logout }
      );
      await reloadSubjects();
      setEnrollmentSubject(null);
      setEnrollmentValue('');
      toast.success('Matrícula guardada correctamente.');
    } catch (enrollmentError) {
      const message = getFriendlyCatalogError(enrollmentError, 'No fue posible guardar la matrícula.');
      setFormError(message);
      toast.error('No fue posible guardar la matrícula.', message);
    } finally {
      setBusy(false);
    }
  }

  function resetImportState() {
    setShowImportModal(false);
    setImportStep('prepare');
    setImportFile(null);
    setImportPreview(null);
    setImportResult(null);
    setImportError(null);
    setConfirmingImport(false);
  }

  function handleImportFile(file: File | null) {
    setImportPreview(null);
    setImportResult(null);
    setImportError(null);

    if (!file) {
      setImportFile(null);
      return;
    }

    if (!isSubjectEnrollmentImportFileAllowed(file)) {
      setImportFile(null);
      setImportError('Seleccioná un archivo CSV o Excel .xlsx.');
      return;
    }

    if (file.size > SUBJECT_ENROLLMENT_IMPORT_MAX_FILE_SIZE_BYTES) {
      setImportFile(null);
      setImportError('El archivo supera el tamaño máximo permitido de 5 MB.');
      return;
    }

    setImportFile(file);
  }

  async function handleDownloadEnrollmentTemplate(format: 'xlsx' | 'csv') {
    if (!accessToken || !context.careerId || !context.academicCycleId || busy) {
      return;
    }

    setBusy(true);
    setImportError(null);

    try {
      const response = await downloadSubjectEnrollmentImportTemplate(
        context.careerId,
        context.academicCycleId,
        format,
        { accessToken, onUnauthorized: auth.logout }
      );
      downloadBlob(response.blob, getDownloadFileName(response.contentDisposition, `matriculas.${format}`));
    } catch (downloadError) {
      const message = getFriendlyCatalogError(downloadError, 'No fue posible descargar la plantilla.');
      setImportError(message);
      toast.error('No se pudo descargar la plantilla', message);
    } finally {
      setBusy(false);
    }
  }

  async function handlePreviewImport() {
    if (!accessToken || !context.careerId || !context.academicCycleId || !importFile || busy) {
      return;
    }

    setBusy(true);
    setImportError(null);

    try {
      const preview = await previewSubjectEnrollmentImport(
        context.careerId,
        context.academicCycleId,
        importFile,
        { accessToken, onUnauthorized: auth.logout }
      );
      setImportPreview(preview);
      setImportStep('preview');
    } catch (previewError) {
      const message = getFriendlyCatalogError(previewError, 'No fue posible revisar el archivo.');
      setImportError(message);
      toast.error('No se pudo revisar el archivo', message);
    } finally {
      setBusy(false);
    }
  }

  async function handleCommitImport() {
    if (!accessToken || !context.careerId || !context.academicCycleId || !importFile || busy) {
      return;
    }

    setBusy(true);
    setImportError(null);

    try {
      const result = await importSubjectEnrollments(
        context.careerId,
        context.academicCycleId,
        importFile,
        { accessToken, onUnauthorized: auth.logout }
      );
      setImportResult(result);
      setImportStep('done');
      setConfirmingImport(false);
      await reloadSubjects();
      toast.success('Matrículas importadas correctamente.');
    } catch (commitError) {
      const message = getFriendlyCatalogError(commitError, 'No fue posible importar las matrículas.');
      setImportError(message);
      setImportStep('preview');
      setConfirmingImport(false);
      toast.error('No se pudieron importar las matrículas', message);
    } finally {
      setBusy(false);
    }
  }

  return (
    <section className="app-content academic-page">
      <ContextHeader eyebrow="Carrera seleccionada" title="Materias" />
      <div className="surveys-filters">
        <label>
          <span>Año de carrera</span>
          <select className="text-input" onChange={(event) => setYearFilter(event.target.value)} value={yearFilter}>
            <option value="">Todos</option>
            {years.map((year) => (
              <option key={year} value={year}>
                {year}° año
              </option>
            ))}
          </select>
        </label>
      </div>

      {canManageCatalog ? (
        <div className="crud-toolbar">
          <div>
            <strong>Materias de {context.selectedCareer?.name}</strong>
            <small>Creá, editá, eliminá o asigná docentes sin salir de la carrera.</small>
          </div>
          <div className="toolbar-actions">
            <button
              className="secondary-button"
              disabled={!context.academicCycleId}
              onClick={() => {
                setShowImportModal(true);
                setImportStep('prepare');
                setImportError(null);
              }}
              type="button"
            >
              Importar matrículas
            </button>
            <button
              className="primary-button"
              onClick={() => {
                setFormError(null);
                setCreateForm({ careerId: context.careerId, code: '', name: '', year: 1, period: 'Annual' });
                setShowCreateModal(true);
              }}
              type="button"
            >
              Nueva materia
            </button>
          </div>
        </div>
      ) : null}

      {!context.selectedAcademicCycle ? (
        <div className="inline-message">
          Seleccioná un ciclo lectivo para consultar la cantidad de alumnos inscriptos.
        </div>
      ) : null}

      <Modal
        description={`La materia quedará asociada a ${context.selectedCareer?.name ?? 'la carrera seleccionada'}.`}
        open={showCreateModal}
        onClose={() => {
          if (!busy) {
            setShowCreateModal(false);
            setFormError(null);
          }
        }}
        closeDisabled={busy}
        title="Nueva materia"
      >
        <form className="modal-form" onSubmit={handleCreate}>
          <label>
            <span>Código</span>
            <input autoFocus className="text-input" disabled={busy} required value={createForm.code} onChange={(event) => setCreateForm((current) => ({ ...current, code: event.target.value }))} />
          </label>
          <label>
            <span>Nombre</span>
            <input className="text-input" disabled={busy} required value={createForm.name} onChange={(event) => setCreateForm((current) => ({ ...current, name: event.target.value }))} />
          </label>
          <SubjectEditableFields disabled={busy} form={createForm} onChange={setCreateForm} />
          {formError ? <p className="submit-error" role="alert">{formError}</p> : null}
          <div className="modal-footer-actions">
            <button className="secondary-button" disabled={busy} onClick={() => { setShowCreateModal(false); setFormError(null); }} type="button">Cancelar</button>
            <button className="primary-button" disabled={busy} type="submit">{busy ? 'Creando...' : 'Crear materia'}</button>
          </div>
        </form>
      </Modal>

      <Modal
        description="Registrá la cantidad de alumnos inscriptos para esta materia y ciclo lectivo."
        open={enrollmentSubject !== null}
        onClose={() => {
          if (!busy) {
            setEnrollmentSubject(null);
            setEnrollmentValue('');
            setFormError(null);
          }
        }}
        closeDisabled={busy}
        title="Cantidad de alumnos inscriptos"
      >
        {enrollmentSubject ? (
          <form className="modal-form" onSubmit={handleSaveEnrollment}>
            <div className="enrollment-context">
              <div><span>Materia</span><strong>{enrollmentSubject.name}</strong></div>
              <div><span>Carrera</span><strong>{context.selectedCareer?.name}</strong></div>
              <div><span>Ciclo lectivo</span><strong>{context.selectedAcademicCycle ? formatAcademicCycle(context.selectedAcademicCycle) : 'Sin ciclo seleccionado'}</strong></div>
            </div>
            <label>
              <span>Alumnos inscriptos</span>
              <input
                autoFocus
                className="text-input"
                disabled={busy}
                min={1}
                onChange={(event) => setEnrollmentValue(event.target.value)}
                required
                step={1}
                type="number"
                value={enrollmentValue}
              />
              <small>
                Este valor se utilizará para calcular la participación y limitar la cantidad máxima de respuestas en nuevas encuestas de estudiantes.
              </small>
            </label>
            {enrollmentsBySubject.has(enrollmentSubject.id) ? (
              <p className="form-helper">
                Las encuestas ya creadas conservan la cantidad de alumnos registrada al momento de su creación.
              </p>
            ) : null}
            {formError ? <p className="submit-error" role="alert">{formError}</p> : null}
            <div className="modal-footer-actions">
              <button className="secondary-button" disabled={busy} onClick={() => { setEnrollmentSubject(null); setEnrollmentValue(''); setFormError(null); }} type="button">Cancelar</button>
              <button className="primary-button" disabled={busy || !context.academicCycleId} type="submit">{busy ? 'Guardando...' : 'Guardar'}</button>
            </div>
          </form>
        ) : null}
      </Modal>

      <SubjectEnrollmentImportModal
        busy={busy}
        file={importFile}
        importError={importError}
        open={showImportModal}
        preview={importPreview}
        result={importResult}
        selectedAcademicCycle={context.selectedAcademicCycle}
        selectedCareerName={context.selectedCareer?.name ?? 'Carrera seleccionada'}
        step={importStep}
        onClose={() => {
          if (!busy) {
            resetImportState();
          }
        }}
        onDownloadTemplate={(format) => void handleDownloadEnrollmentTemplate(format)}
        onFileChange={handleImportFile}
        onPreview={() => void handlePreviewImport()}
        onRemoveFile={() => handleImportFile(null)}
        onReturnToPrepare={() => {
          setImportStep('prepare');
          setImportPreview(null);
          setImportError(null);
        }}
        onConfirm={() => setConfirmingImport(true)}
      />

      <ConfirmDialog
        busy={busy}
        confirmLabel="Importar matrículas"
        message={
          importPreview
            ? `Se crearán ${importPreview.createRows} matrículas y se actualizarán ${importPreview.updateRows}. Las filas sin cambios se conservarán igual.`
            : ''
        }
        onCancel={() => setConfirmingImport(false)}
        onConfirm={() => void handleCommitImport()}
        open={confirmingImport}
        title="¿Importar estas matrículas?"
      />

      <Modal
        description={editingSubject ? `Código ${editingSubject.code}. El código no cambia para preservar referencias históricas.` : undefined}
        open={editingSubject !== null}
        onClose={() => {
          if (!busy) {
            setEditingSubject(null);
            setFormError(null);
          }
        }}
        closeDisabled={busy}
        title="Editar materia"
      >
        <form className="modal-form" onSubmit={handleUpdate}>
          <label>
            <span>Nombre</span>
            <input autoFocus className="text-input" disabled={busy} required value={editForm.name} onChange={(event) => setEditForm((current) => ({ ...current, name: event.target.value }))} />
          </label>
          <SubjectEditableFields disabled={busy} form={editForm} onChange={setEditForm} />
          {formError ? <p className="submit-error" role="alert">{formError}</p> : null}
          <div className="modal-footer-actions">
            <button className="secondary-button" disabled={busy} onClick={() => { setEditingSubject(null); setFormError(null); }} type="button">Cancelar</button>
            <button className="primary-button" disabled={busy} type="submit">{busy ? 'Guardando...' : 'Guardar cambios'}</button>
          </div>
        </form>
      </Modal>

      <Modal
        description={assigningSubject ? `Agregá un docente a ${assigningSubject.name}. Podés repetir esta acción para incorporar varios docentes en el mismo ciclo lectivo.` : undefined}
        open={assigningSubject !== null}
        onClose={() => {
          if (!busy) {
            setAssigningSubject(null);
            setFormError(null);
          }
        }}
        closeDisabled={busy}
        title="Agregar docente a materia"
      >
        <form className="modal-form" onSubmit={handleAssignTeacher}>
          <label>
            <span>Docente</span>
            <select autoFocus className="text-input" disabled={busy} required value={assignmentForm.teacherId} onChange={(event) => setAssignmentForm((current) => ({ ...current, teacherId: event.target.value }))}>
              <option value="">Seleccionar docente...</option>
              {[...availableTeachersForAssignment].sort((left, right) => `${left.lastName} ${left.firstName}`.localeCompare(`${right.lastName} ${right.firstName}`)).map((teacher) => (
                <option key={teacher.id} value={teacher.id}>{teacher.lastName}, {teacher.firstName}</option>
              ))}
            </select>
          </label>
          <AssignmentCycleAndRoleFields
            academicCycles={context.academicCycles}
            disabled={busy}
            form={assignmentForm}
            onChange={setAssignmentForm}
          />
          {teachers.length === 0 ? <p className="form-helper">Primero creá un docente desde el apartado Docentes.</p> : null}
          {teachers.length > 0 && assignmentForm.academicCycleId && availableTeachersForAssignment.length === 0 ? (
            <p className="form-helper">Todos los docentes activos ya están asignados a esta materia en el ciclo elegido.</p>
          ) : null}
          {formError ? <p className="submit-error" role="alert">{formError}</p> : null}
          <div className="modal-footer-actions">
            <button className="secondary-button" disabled={busy} onClick={() => { setAssigningSubject(null); setFormError(null); }} type="button">Cancelar</button>
            <button className="primary-button" disabled={busy || availableTeachersForAssignment.length === 0} type="submit">{busy ? 'Agregando...' : 'Agregar docente'}</button>
          </div>
        </form>
      </Modal>

      <ConfirmDialog
        busy={busy}
        confirmLabel="Eliminar materia"
        message={deletingSubject ? `Se desactivará “${deletingSubject.name}”. El historial y los resultados existentes se conservarán.` : ''}
        onCancel={() => setDeletingSubject(null)}
        onConfirm={() => void handleDeleteSubject()}
        open={deletingSubject !== null}
        title="¿Eliminar esta materia?"
        tone="danger"
      />

      <ConfirmDialog
        busy={busy}
        confirmLabel="Quitar docente"
        message={
          removingAssignment
            ? `Se quitará a ${removingAssignment.teacherFullName} de ${removingAssignment.subjectName} para ${removingAssignment.academicCycleYear} · ${formatPeriod(removingAssignment.academicCyclePeriod)}. Las demás asignaciones docentes de la materia no se modificarán.`
            : ''
        }
        onCancel={() => setRemovingAssignment(null)}
        onConfirm={() => void handleRemoveTeacherAssignment()}
        open={removingAssignment !== null}
        title="¿Quitar docente de esta materia?"
        tone="danger"
      />

      {state === 'loading' ? <p>Cargando materias...</p> : null}
      {state === 'error' ? <p className="submit-error">{error}</p> : null}
      {state === 'ready' && filteredSubjects.length === 0 ? (
        <div className="empty-detail">
          <h3>No hay materias para esta carrera</h3>
          <p>No hay materias activas para los filtros seleccionados.</p>
        </div>
      ) : null}
      <div className="assignment-card-list">
        {groupedSubjects.map(([year, yearSubjects]) => (
          <section className="structure-card" key={year}>
            <header>
              <div>
                <p className="eyebrow">{year}° año</p>
                <h3>{yearSubjects.length} materias</h3>
              </div>
            </header>
            <div className="context-entity-list">
              {yearSubjects.map((subject) => {
                const subjectTeacherAssignments = assignmentsBySubject.get(subject.id) ?? [];
                const enrollment = enrollmentsBySubject.get(subject.id);
                return (
                  <article className="context-entity-row context-entity-row--stacked" key={subject.id}>
                    <div className="context-entity-row__top">
                      <div className="context-entity-row__main">
                        <strong>{subject.name}</strong>
                        <small>{subject.code} · {formatPeriod(subject.period)}</small>
                      </div>
                      {canManageCatalog ? (
                        <div className="context-entity-actions" aria-label={`Acciones para ${subject.name}`}>
                          {context.selectedAcademicCycle ? (
                            <button className="secondary-button" onClick={() => openEnrollmentModal(subject)} type="button">
                              {enrollment ? 'Editar matrícula' : 'Cargar matrícula'}
                            </button>
                          ) : null}
                          <button className="secondary-button" onClick={() => openAssignTeacher(subject)} type="button">Agregar docente</button>
                          <button className="secondary-button" onClick={() => openEdit(subject)} type="button">Editar</button>
                          <button className="danger-button" onClick={() => setDeletingSubject(subject)} type="button">Eliminar</button>
                        </div>
                      ) : null}
                    </div>

                    <div className="subject-enrollment-summary">
                      <div>
                        <span>Cantidad de alumnos inscriptos</span>
                        {context.selectedAcademicCycle ? (
                          enrollment ? <strong>{enrollment.enrolledStudentCount}</strong> : <strong>Matrícula no cargada</strong>
                        ) : (
                          <strong>Requiere ciclo lectivo</strong>
                        )}
                      </div>
                    </div>

                    <div className="subject-teachers">
                      <div className="subject-teachers__heading">
                        <strong>Docentes asignados</strong>
                        <span>{subjectTeacherAssignments.length}</span>
                      </div>
                      {subjectTeacherAssignments.length === 0 ? (
                        <p className="subject-teachers__empty">Todavía no hay docentes asignados{context.selectedAcademicCycle ? ' en este ciclo lectivo' : ''}.</p>
                      ) : (
                        <div className="subject-teachers__list">
                          {subjectTeacherAssignments.map((assignment) => (
                            <div className="subject-teacher-chip" key={assignment.id}>
                              <div>
                                <strong>{assignment.teacherFullName}</strong>
                                <small>
                                  {assignment.teachingRole}
                                  {!context.selectedAcademicCycle
                                    ? ` · ${assignment.academicCycleYear} · ${formatPeriod(assignment.academicCyclePeriod)}`
                                    : ''}
                                </small>
                              </div>
                              {canManageCatalog ? (
                                <button
                                  aria-label={`Quitar a ${assignment.teacherFullName} de ${subject.name}`}
                                  className="text-danger-button"
                                  onClick={() => setRemovingAssignment(assignment)}
                                  type="button"
                                >
                                  Quitar
                                </button>
                              ) : null}
                            </div>
                          ))}
                        </div>
                      )}
                    </div>
                  </article>
                );
              })}
            </div>
          </section>
        ))}
      </div>
    </section>
  );
}

interface SubjectEnrollmentImportModalProps {
  busy: boolean;
  file: File | null;
  importError: string | null;
  open: boolean;
  preview: SubjectEnrollmentImportPreviewDto | null;
  result: {
    createdCount: number;
    updatedCount: number;
    unchangedCount: number;
    totalProcessed: number;
  } | null;
  selectedAcademicCycle: AcademicCycleDto | null;
  selectedCareerName: string;
  step: 'prepare' | 'preview' | 'done';
  onClose: () => void;
  onConfirm: () => void;
  onDownloadTemplate: (format: 'xlsx' | 'csv') => void;
  onFileChange: (file: File | null) => void;
  onPreview: () => void;
  onRemoveFile: () => void;
  onReturnToPrepare: () => void;
}

function SubjectEnrollmentImportModal({
  busy,
  file,
  importError,
  open,
  preview,
  result,
  selectedAcademicCycle,
  selectedCareerName,
  step,
  onClose,
  onConfirm,
  onDownloadTemplate,
  onFileChange,
  onPreview,
  onRemoveFile,
  onReturnToPrepare
}: SubjectEnrollmentImportModalProps) {
  const hasPreviewErrors = (preview?.errorRows ?? 0) > 0;

  return (
    <Modal
      closeDisabled={busy}
      description="Importá matrículas desde una plantilla CSV o Excel para la carrera y el ciclo seleccionados."
      onClose={onClose}
      open={open}
      title="Importar matrículas"
    >
      <div className="import-wizard">
        <ol className="import-steps" aria-label="Pasos de importación">
          <li className={step === 'prepare' ? 'active' : undefined}>Preparar archivo</li>
          <li className={step === 'preview' ? 'active' : undefined}>Revisar datos</li>
          <li className={step === 'done' ? 'active' : undefined}>Confirmar</li>
        </ol>

        {step === 'prepare' ? (
          <div className="import-step-panel">
            <div className="enrollment-context">
              <div><span>Carrera</span><strong>{selectedCareerName}</strong></div>
              <div><span>Ciclo lectivo</span><strong>{selectedAcademicCycle ? formatAcademicCycle(selectedAcademicCycle) : 'Sin ciclo seleccionado'}</strong></div>
            </div>
            <div className="import-template-actions">
              <button className="secondary-button" disabled={busy || !selectedAcademicCycle} onClick={() => onDownloadTemplate('xlsx')} type="button">
                Descargar plantilla Excel
              </button>
              <button className="secondary-button" disabled={busy || !selectedAcademicCycle} onClick={() => onDownloadTemplate('csv')} type="button">
                Descargar CSV
              </button>
            </div>
            <p className="form-helper">Completá únicamente la columna AlumnosInscriptos. El archivo puede tener hasta 5 MB y 2000 filas.</p>

            <label
              className="import-dropzone"
              htmlFor="subject-enrollment-import-file"
              onKeyDown={(event) => {
                if (event.key === 'Enter' || event.key === ' ') {
                  event.preventDefault();
                  document.getElementById('subject-enrollment-import-file')?.click();
                }
              }}
              tabIndex={0}
            >
              <strong>Seleccionar archivo</strong>
              <span>CSV o Excel .xlsx</span>
              <input
                accept=".csv,.xlsx"
                disabled={busy}
                id="subject-enrollment-import-file"
                onChange={(event) => onFileChange(event.target.files?.[0] ?? null)}
                type="file"
              />
            </label>

            {file ? (
              <div className="import-file-summary">
                <div>
                  <strong>{file.name}</strong>
                  <small>{formatBytes(file.size)}</small>
                </div>
                <button className="text-danger-button" disabled={busy} onClick={onRemoveFile} type="button">Quitar</button>
              </div>
            ) : null}

            {importError ? <p className="submit-error" role="alert">{importError}</p> : null}

            <div className="modal-footer-actions">
              <button className="secondary-button" disabled={busy} onClick={onClose} type="button">Cancelar</button>
              <button className="primary-button" disabled={busy || !file || !selectedAcademicCycle} onClick={onPreview} type="button">
                {busy ? 'Revisando...' : 'Revisar archivo'}
              </button>
            </div>
          </div>
        ) : null}

        {step === 'preview' && preview ? (
          <div className="import-step-panel">
            <div className="import-summary-grid">
              <MetricPill label="Filas" value={preview.totalRows} />
              <MetricPill label="Crear" value={preview.createRows} />
              <MetricPill label="Actualizar" value={preview.updateRows} />
              <MetricPill label="Sin cambios" value={preview.unchangedRows} />
              <MetricPill label="Errores" tone={preview.errorRows > 0 ? 'danger' : 'neutral'} value={preview.errorRows} />
            </div>

            {hasPreviewErrors ? (
              <p className="submit-error" role="alert">Corregí los errores en el archivo y volvé a cargarlo.</p>
            ) : null}
            {importError ? <p className="submit-error" role="alert">{importError}</p> : null}

            <div className="import-preview-table" role="region" aria-label="Vista previa de matrículas" tabIndex={0}>
              <table>
                <thead>
                  <tr>
                    <th>Fila</th>
                    <th>Código</th>
                    <th>Materia</th>
                    <th>Actual</th>
                    <th>Nuevo</th>
                    <th>Estado</th>
                  </tr>
                </thead>
                <tbody>
                  {preview.rows.map((row) => (
                    <tr key={`${row.rowNumber}-${row.subjectCode}`}>
                      <td>{row.rowNumber}</td>
                      <td>{row.subjectCode || '-'}</td>
                      <td>
                        <strong>{row.subjectName ?? row.providedSubjectName ?? '-'}</strong>
                        {row.errorMessage ? <small>{row.errorMessage}</small> : null}
                      </td>
                      <td>{row.currentEnrolledStudentCount ?? '-'}</td>
                      <td>{row.newEnrolledStudentCount ?? '-'}</td>
                      <td><span className={`import-status import-status--${row.status.toLowerCase()}`}>{getImportStatusLabel(row.status)}</span></td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>

            <div className="import-preview-cards">
              {preview.rows.map((row) => (
                <article className={`import-preview-card import-preview-card--${row.status.toLowerCase()}`} key={`${row.rowNumber}-${row.subjectCode}`}>
                  <div>
                    <strong>{row.subjectName ?? row.providedSubjectName ?? 'Materia no encontrada'}</strong>
                    <span>{row.subjectCode || 'Sin código'} · Fila {row.rowNumber}</span>
                  </div>
                  <span className={`import-status import-status--${row.status.toLowerCase()}`}>{getImportStatusLabel(row.status)}</span>
                  <dl>
                    <div><dt>Actual</dt><dd>{row.currentEnrolledStudentCount ?? '-'}</dd></div>
                    <div><dt>Nuevo</dt><dd>{row.newEnrolledStudentCount ?? '-'}</dd></div>
                  </dl>
                  {row.errorMessage ? <p>{row.errorMessage}</p> : null}
                </article>
              ))}
            </div>

            <div className="modal-footer-actions">
              <button className="secondary-button" disabled={busy} onClick={onReturnToPrepare} type="button">Volver</button>
              <button className="primary-button" disabled={busy || hasPreviewErrors || preview.validRows === 0} onClick={onConfirm} type="button">
                Importar matrículas
              </button>
            </div>
          </div>
        ) : null}

        {step === 'done' && result ? (
          <div className="import-step-panel">
            <div className="success-panel">
              <h3>Matrículas importadas correctamente.</h3>
              <p>Se procesaron {result.totalProcessed} filas: {result.createdCount} creadas, {result.updatedCount} actualizadas y {result.unchangedCount} sin cambios.</p>
            </div>
            <div className="modal-footer-actions">
              <button className="primary-button" onClick={onClose} type="button">Cerrar</button>
            </div>
          </div>
        ) : null}
      </div>
    </Modal>
  );
}

function MetricPill({ label, value, tone = 'neutral' }: { label: string; value: number; tone?: 'neutral' | 'danger' }) {
  return (
    <div className={`metric-pill metric-pill--${tone}`}>
      <span>{label}</span>
      <strong>{value}</strong>
    </div>
  );
}

export function ContextTeachersPage() {
  const auth = useAuth();
  const context = useAcademicContext();
  const toast = useToast();
  const [teachers, setTeachers] = useState<TeacherDto[]>([]);
  const [subjects, setSubjects] = useState<SubjectDto[]>([]);
  const [assignments, setAssignments] = useState<TeacherSubjectAssignmentDto[]>([]);
  const [state, setState] = useState<LoadState>('idle');
  const [error, setError] = useState<string | null>(null);
  const [teacherForm, setTeacherForm] = useState<CreateTeacherRequest>({ firstName: '', lastName: '', email: null });
  const [newTeacherAssignment, setNewTeacherAssignment] = useState<CreateTeacherSubjectAssignmentRequest>({
    teacherId: '',
    subjectId: '',
    academicCycleId: '',
    teachingRole: ''
  });
  const [assignmentForm, setAssignmentForm] = useState<CreateTeacherSubjectAssignmentRequest>({
    teacherId: '',
    subjectId: '',
    academicCycleId: '',
    teachingRole: ''
  });
  const [showCreateModal, setShowCreateModal] = useState(false);
  const [editingTeacher, setEditingTeacher] = useState<TeacherDto | null>(null);
  const [assigningTeacher, setAssigningTeacher] = useState<TeacherDto | null>(null);
  const [removingTeacher, setRemovingTeacher] = useState<TeacherDto | null>(null);
  const [formError, setFormError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  const accessToken = auth.accessToken;
  const canReadCatalog = auth.hasPermission(READ_CATALOG) || auth.hasPermission(MANAGE_CATALOG);
  const canManageCatalog = auth.hasPermission(MANAGE_CATALOG);

  async function reloadTeacherData() {
    if (!accessToken || !context.careerId) {
      return;
    }

    const [nextTeachers, nextAssignments] = await Promise.all([
      getCareerTeachers(context.careerId, {
        accessToken,
        academicCycleId: context.academicCycleId,
        includeInactive: false,
        onUnauthorized: auth.logout
      }),
      getTeacherSubjectAssignments(
        {
          includeInactive: false,
          careerId: context.careerId,
          academicCycleId: context.academicCycleId
        },
        { accessToken, onUnauthorized: auth.logout }
      )
    ]);
    setTeachers(nextTeachers);
    setAssignments(nextAssignments);
  }

  useEffect(() => {
    if (!accessToken || !context.careerId || !canReadCatalog) {
      setTeachers([]);
      setSubjects([]);
      setAssignments([]);
      setState('ready');
      return;
    }

    const abortController = new AbortController();

    async function loadPage() {
      if (!accessToken) {
        return;
      }

      setState('loading');
      setError(null);

      try {
        const [nextTeachers, nextAssignments, nextSubjects] = await Promise.all([
          getCareerTeachers(context.careerId, {
            accessToken,
            academicCycleId: context.academicCycleId,
            includeInactive: false,
            onUnauthorized: auth.logout,
            signal: abortController.signal
          }),
          getTeacherSubjectAssignments(
            {
              includeInactive: false,
              careerId: context.careerId,
              academicCycleId: context.academicCycleId
            },
            { accessToken, onUnauthorized: auth.logout, signal: abortController.signal }
          ),
          getSubjects(context.careerId, {
            accessToken,
            includeInactive: false,
            onUnauthorized: auth.logout,
            signal: abortController.signal
          })
        ]);
        setTeachers(nextTeachers);
        setAssignments(nextAssignments);
        setSubjects(nextSubjects);
        setState('ready');
      } catch (loadError) {
        if (abortController.signal.aborted) {
          return;
        }

        setError(getFriendlyCatalogError(loadError, 'No fue posible cargar docentes.'));
        setState('error');
      }
    }

    void loadPage();
    return () => abortController.abort();
  }, [accessToken, auth.logout, canReadCatalog, context.academicCycleId, context.careerId]);

  useEffect(() => {
    setShowCreateModal(false);
    setEditingTeacher(null);
    setAssigningTeacher(null);
    setRemovingTeacher(null);
  }, [context.academicCycleId, context.careerId]);

  const assignmentsByTeacher = useMemo(() => {
    const map = new Map<string, TeacherSubjectAssignmentDto[]>();
    assignments.forEach((assignment) => {
      map.set(assignment.teacherId, [...(map.get(assignment.teacherId) ?? []), assignment]);
    });
    return map;
  }, [assignments]);
  const guard = useContextGuard();

  if (guard) {
    return guard;
  }

  if (!canReadCatalog) {
    return <PermissionPanel message="No tenés permisos para consultar docentes." />;
  }

  function openCreateTeacher() {
    setFormError(null);
    setTeacherForm({ firstName: '', lastName: '', email: null });
    setNewTeacherAssignment({
      teacherId: '',
      subjectId: '',
      academicCycleId: context.academicCycleId,
      teachingRole: ''
    });
    setShowCreateModal(true);
  }

  function openEditTeacher(teacher: TeacherDto) {
    setFormError(null);
    setTeacherForm({ firstName: teacher.firstName, lastName: teacher.lastName, email: teacher.email });
    setEditingTeacher(teacher);
  }

  function openAssignTeacher(teacher: TeacherDto) {
    setFormError(null);
    setAssignmentForm({
      teacherId: teacher.id,
      subjectId: '',
      academicCycleId: context.academicCycleId,
      teachingRole: ''
    });
    setAssigningTeacher(teacher);
  }

  async function handleCreateTeacher(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    if (!accessToken || busy) {
      return;
    }

    setBusy(true);
    setFormError(null);

    try {
      const createdTeacher = await createTeacher(normalizeTeacherForm(teacherForm), {
        accessToken,
        onUnauthorized: auth.logout
      });

      try {
        await createTeacherSubjectAssignment(
          {
            ...newTeacherAssignment,
            teacherId: createdTeacher.id,
            teachingRole: newTeacherAssignment.teachingRole.trim()
          },
          { accessToken, onUnauthorized: auth.logout }
        );
      } catch (assignmentError) {
        const message = getFriendlyCatalogError(
          assignmentError,
          'El docente fue creado, pero no pudo asignarse a la materia.'
        );
        setFormError(message);
        toast.error('Docente creado sin asignación', `${message} Podés asignarlo desde Estructura académica.`);
        setShowCreateModal(false);
        return;
      }

      await reloadTeacherData();
      setShowCreateModal(false);
      toast.success('Docente creado', 'El docente quedó creado y asignado a la materia seleccionada.');
    } catch (createError) {
      const message = getFriendlyCatalogError(createError, 'No fue posible crear el docente.');
      setFormError(message);
      toast.error('No se pudo crear el docente', message);
    } finally {
      setBusy(false);
    }
  }

  async function handleUpdateTeacher(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    if (!accessToken || !editingTeacher || busy) {
      return;
    }

    setBusy(true);
    setFormError(null);

    try {
      await updateTeacher(
        editingTeacher.id,
        normalizeTeacherForm(teacherForm) as UpdateTeacherRequest,
        { accessToken, onUnauthorized: auth.logout }
      );
      await reloadTeacherData();
      setEditingTeacher(null);
      toast.success('Docente actualizado', 'Los datos del docente se guardaron correctamente.');
    } catch (updateError) {
      const message = getFriendlyCatalogError(updateError, 'No fue posible actualizar el docente.');
      setFormError(message);
      toast.error('No se pudo actualizar el docente', message);
    } finally {
      setBusy(false);
    }
  }

  async function handleAssignTeacher(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    if (!accessToken || !assigningTeacher || busy) {
      return;
    }

    setBusy(true);
    setFormError(null);

    try {
      await createTeacherSubjectAssignment(
        { ...assignmentForm, teacherId: assigningTeacher.id, teachingRole: assignmentForm.teachingRole.trim() },
        { accessToken, onUnauthorized: auth.logout }
      );
      await reloadTeacherData();
      setAssigningTeacher(null);
      toast.success('Materia asignada', 'El docente quedó vinculado a la materia y ciclo lectivo seleccionados.');
    } catch (assignmentError) {
      const message = getFriendlyCatalogError(assignmentError, 'No fue posible asignar la materia.');
      setFormError(message);
      toast.error('No se pudo asignar la materia', message);
    } finally {
      setBusy(false);
    }
  }

  async function handleRemoveTeacherFromCareer() {
    if (!accessToken || !removingTeacher || busy) {
      return;
    }

    const activeAssignments = (assignmentsByTeacher.get(removingTeacher.id) ?? []).filter((assignment) => assignment.isActive);
    if (activeAssignments.length === 0) {
      setRemovingTeacher(null);
      toast.info('Sin asignaciones activas', 'El docente ya no tiene materias activas en este contexto.');
      return;
    }

    setBusy(true);

    try {
      await Promise.all(
        activeAssignments.map((assignment) =>
          deactivateTeacherSubjectAssignment(assignment.id, { accessToken, onUnauthorized: auth.logout })
        )
      );
      await reloadTeacherData();
      setRemovingTeacher(null);
      toast.success(
        'Docente eliminado de la carrera',
        'Se desactivaron sus asignaciones en este contexto. El docente y el historial se conservan.'
      );
    } catch (removeError) {
      const message = getFriendlyCatalogError(removeError, 'No fue posible quitar el docente de la carrera.');
      toast.error('No se pudo eliminar el docente', message);
    } finally {
      setBusy(false);
    }
  }

  return (
    <section className="app-content academic-page">
      <ContextHeader eyebrow="Carrera seleccionada" title="Docentes" />

      {canManageCatalog ? (
        <div className="crud-toolbar">
          <div>
            <strong>Docentes de {context.selectedCareer?.name}</strong>
            <small>Creá docentes, actualizá sus datos y asignales materias por ciclo lectivo.</small>
          </div>
          <button className="primary-button" onClick={openCreateTeacher} type="button">Nuevo docente</button>
        </div>
      ) : null}

      <Modal
        description="Completá los datos personales y asigná al docente a una materia de esta carrera."
        open={showCreateModal}
        onClose={() => {
          if (!busy) {
            setShowCreateModal(false);
            setFormError(null);
          }
        }}
        closeDisabled={busy}
        size="large"
        title="Nuevo docente"
      >
        <form className="modal-form" onSubmit={handleCreateTeacher}>
          <TeacherEditableFields disabled={busy} form={teacherForm} onChange={setTeacherForm} />
          <div className="modal-section-divider">
            <strong>Asignación inicial</strong>
            <small>La asignación permite que el docente aparezca dentro de esta carrera.</small>
          </div>
          <label>
            <span>Materia</span>
            <select className="text-input" disabled={busy || subjects.length === 0} required value={newTeacherAssignment.subjectId} onChange={(event) => setNewTeacherAssignment((current) => ({ ...current, subjectId: event.target.value }))}>
              <option value="">Seleccionar materia...</option>
              {subjects.map((subject) => <option key={subject.id} value={subject.id}>{subject.name} · {subject.year}° año</option>)}
            </select>
          </label>
          <AssignmentCycleAndRoleFields academicCycles={context.academicCycles} disabled={busy} form={newTeacherAssignment} onChange={setNewTeacherAssignment} />
          {subjects.length === 0 ? <p className="form-helper">Primero creá una materia en esta carrera para poder incorporar docentes.</p> : null}
          {formError ? <p className="submit-error" role="alert">{formError}</p> : null}
          <div className="modal-footer-actions">
            <button className="secondary-button" disabled={busy} onClick={() => { setShowCreateModal(false); setFormError(null); }} type="button">Cancelar</button>
            <button className="primary-button" disabled={busy || subjects.length === 0} type="submit">{busy ? 'Creando...' : 'Crear y asignar docente'}</button>
          </div>
        </form>
      </Modal>

      <Modal
        description="Los cambios del docente se reflejan en todas las carreras donde esté asignado."
        open={editingTeacher !== null}
        onClose={() => {
          if (!busy) {
            setEditingTeacher(null);
            setFormError(null);
          }
        }}
        closeDisabled={busy}
        title="Editar docente"
      >
        <form className="modal-form" onSubmit={handleUpdateTeacher}>
          <TeacherEditableFields disabled={busy} form={teacherForm} onChange={setTeacherForm} />
          {formError ? <p className="submit-error" role="alert">{formError}</p> : null}
          <div className="modal-footer-actions">
            <button className="secondary-button" disabled={busy} onClick={() => { setEditingTeacher(null); setFormError(null); }} type="button">Cancelar</button>
            <button className="primary-button" disabled={busy} type="submit">{busy ? 'Guardando...' : 'Guardar cambios'}</button>
          </div>
        </form>
      </Modal>

      <Modal
        description={assigningTeacher ? `Asigná una nueva materia a ${assigningTeacher.firstName} ${assigningTeacher.lastName}.` : undefined}
        open={assigningTeacher !== null}
        onClose={() => {
          if (!busy) {
            setAssigningTeacher(null);
            setFormError(null);
          }
        }}
        closeDisabled={busy}
        title="Asignar a materia"
      >
        <form className="modal-form" onSubmit={handleAssignTeacher}>
          <label>
            <span>Materia</span>
            <select autoFocus className="text-input" disabled={busy} required value={assignmentForm.subjectId} onChange={(event) => setAssignmentForm((current) => ({ ...current, subjectId: event.target.value }))}>
              <option value="">Seleccionar materia...</option>
              {subjects.map((subject) => <option key={subject.id} value={subject.id}>{subject.name} · {subject.year}° año</option>)}
            </select>
          </label>
          <AssignmentCycleAndRoleFields academicCycles={context.academicCycles} disabled={busy} form={assignmentForm} onChange={setAssignmentForm} />
          {formError ? <p className="submit-error" role="alert">{formError}</p> : null}
          <div className="modal-footer-actions">
            <button className="secondary-button" disabled={busy} onClick={() => { setAssigningTeacher(null); setFormError(null); }} type="button">Cancelar</button>
            <button className="primary-button" disabled={busy || subjects.length === 0} type="submit">{busy ? 'Asignando...' : 'Asignar materia'}</button>
          </div>
        </form>
      </Modal>

      <ConfirmDialog
        busy={busy}
        confirmLabel="Eliminar de la carrera"
        message={removingTeacher ? (
          context.selectedAcademicCycle
            ? `Se desactivarán las materias de ${removingTeacher.firstName} ${removingTeacher.lastName} en ${formatAcademicCycle(context.selectedAcademicCycle)}. El docente seguirá existiendo y el historial se conservará.`
            : `Se desactivarán las asignaciones activas de ${removingTeacher.firstName} ${removingTeacher.lastName} en esta carrera. El docente seguirá existiendo y el historial se conservará.`
        ) : ''}
        onCancel={() => setRemovingTeacher(null)}
        onConfirm={() => void handleRemoveTeacherFromCareer()}
        open={removingTeacher !== null}
        title="¿Eliminar docente de esta carrera?"
        tone="danger"
      />

      {state === 'loading' ? <p>Cargando docentes...</p> : null}
      {state === 'error' ? <p className="submit-error">{error}</p> : null}
      {state === 'ready' && teachers.length === 0 ? (
        <div className="empty-detail">
          <h3>No hay docentes asignados</h3>
          <p>
            {context.selectedAcademicCycle
              ? 'No hay docentes asignados a esta carrera en el ciclo seleccionado.'
              : 'No hay docentes asignados a esta carrera.'}
          </p>
          {canManageCatalog ? <button className="primary-button" onClick={openCreateTeacher} type="button">Agregar primer docente</button> : null}
        </div>
      ) : null}
      <div className="assignment-card-list">
        {teachers.map((teacher) => {
          const teacherAssignments = assignmentsByTeacher.get(teacher.id) ?? [];
          return (
            <article className="survey-list-card" key={teacher.id}>
              <header>
                <div>
                  <p className="eyebrow">Docente</p>
                  <h3>{teacher.lastName}, {teacher.firstName}</h3>
                  <p>{teacher.email ?? 'Sin email registrado'}</p>
                </div>
                {canManageCatalog ? (
                  <div className="context-entity-actions">
                    <button className="secondary-button" onClick={() => openAssignTeacher(teacher)} type="button">Asignar a materia</button>
                    <button className="secondary-button" onClick={() => openEditTeacher(teacher)} type="button">Editar</button>
                    <button className="danger-button" onClick={() => setRemovingTeacher(teacher)} type="button">Eliminar</button>
                  </div>
                ) : null}
              </header>
              {teacherAssignments.length > 0 ? (
                <dl className="survey-card-meta">
                  {teacherAssignments.map((assignment) => (
                    <div key={assignment.id}>
                      <dt>{assignment.subjectName}</dt>
                      <dd>{assignment.teachingRole}</dd>
                    </div>
                  ))}
                </dl>
              ) : null}
            </article>
          );
        })}
      </div>
    </section>
  );
}

function SubjectEditableFields<T extends { year: number; period: SubjectPeriod }>({
  form,
  onChange,
  disabled = false
}: {
  form: T;
  onChange: (next: T) => void;
  disabled?: boolean;
}) {
  return (
    <div className="modal-form-grid">
      <label>
        <span>Año de carrera</span>
        <input
          className="text-input"
          disabled={disabled}
          min={1}
          max={10}
          required
          type="number"
          value={form.year}
          onChange={(event) => onChange({ ...form, year: Number(event.target.value) })}
        />
      </label>
      <label>
        <span>Período</span>
        <select
          className="text-input"
          disabled={disabled}
          value={form.period}
          onChange={(event) => onChange({ ...form, period: event.target.value as SubjectPeriod })}
        >
          {SUBJECT_PERIODS.map((period) => (
            <option key={period} value={period}>{formatPeriod(period)}</option>
          ))}
        </select>
      </label>
    </div>
  );
}

function TeacherEditableFields({
  form,
  onChange,
  disabled = false
}: {
  form: CreateTeacherRequest;
  onChange: (next: CreateTeacherRequest) => void;
  disabled?: boolean;
}) {
  return (
    <>
      <div className="modal-form-grid">
        <label>
          <span>Nombre</span>
          <input
            autoFocus
            className="text-input"
            disabled={disabled}
            required
            value={form.firstName}
            onChange={(event) => onChange({ ...form, firstName: event.target.value })}
          />
        </label>
        <label>
          <span>Apellido</span>
          <input
            className="text-input"
            disabled={disabled}
            required
            value={form.lastName}
            onChange={(event) => onChange({ ...form, lastName: event.target.value })}
          />
        </label>
      </div>
      <label>
        <span>Correo electrónico</span>
        <input
          className="text-input"
          disabled={disabled}
          inputMode="email"
          type="email"
          value={form.email ?? ''}
          onChange={(event) => onChange({ ...form, email: event.target.value || null })}
        />
        <small>Opcional. Se usa sólo como dato de contacto del docente.</small>
      </label>
    </>
  );
}

function AssignmentCycleAndRoleFields({
  academicCycles,
  form,
  onChange,
  disabled = false
}: {
  academicCycles: AcademicCycleDto[];
  form: CreateTeacherSubjectAssignmentRequest;
  onChange: (next: CreateTeacherSubjectAssignmentRequest) => void;
  disabled?: boolean;
}) {
  return (
    <div className="modal-form-grid">
      <label>
        <span>Ciclo lectivo</span>
        <select
          className="text-input"
          disabled={disabled}
          required
          value={form.academicCycleId}
          onChange={(event) => onChange({ ...form, academicCycleId: event.target.value })}
        >
          <option value="">Seleccionar ciclo...</option>
          {academicCycles.map((cycle) => (
            <option key={cycle.id} value={cycle.id}>{formatAcademicCycle(cycle)}</option>
          ))}
        </select>
      </label>
      <label>
        <span>Rol docente</span>
        <input
          className="text-input"
          disabled={disabled}
          placeholder="Ej.: Profesor titular"
          required
          value={form.teachingRole}
          onChange={(event) => onChange({ ...form, teachingRole: event.target.value })}
        />
      </label>
    </div>
  );
}

function normalizeTeacherForm(form: CreateTeacherRequest): CreateTeacherRequest {
  const email = form.email?.trim() ?? '';
  return {
    firstName: form.firstName.trim(),
    lastName: form.lastName.trim(),
    email: email || null
  };
}

export function ContextTeacherAssignmentsPage() {
  const auth = useAuth();
  const context = useAcademicContext();
  const [assignments, setAssignments] = useState<TeacherSubjectAssignmentDto[]>([]);
  const [state, setState] = useState<LoadState>('idle');
  const [error, setError] = useState<string | null>(null);
  const accessToken = auth.accessToken;
  const canReadCatalog = auth.hasPermission(READ_CATALOG) || auth.hasPermission(MANAGE_CATALOG);

  useEffect(() => {
    if (!accessToken || !context.careerId || !canReadCatalog) {
      setAssignments([]);
      setState('ready');
      return;
    }

    const abortController = new AbortController();

    setState('loading');
    setError(null);

    getTeacherSubjectAssignments(
      {
        includeInactive: false,
        careerId: context.careerId,
        academicCycleId: context.academicCycleId
      },
      { accessToken, onUnauthorized: auth.logout, signal: abortController.signal }
    )
      .then((nextAssignments) => {
        setAssignments(nextAssignments);
        setState('ready');
      })
      .catch((loadError) => {
        if (abortController.signal.aborted) {
          return;
        }

        setError(getFriendlyCatalogError(loadError, 'No fue posible cargar asignaciones docentes.'));
        setState('error');
      });

    return () => abortController.abort();
  }, [accessToken, auth.logout, canReadCatalog, context.academicCycleId, context.careerId]);

  const guard = useContextGuard(true);

  if (guard) {
    return guard;
  }

  if (!canReadCatalog) {
    return <PermissionPanel message="No tenés permisos para consultar asignaciones docentes." />;
  }

  return (
    <section className="app-content academic-page">
      <ContextHeader eyebrow="Carrera seleccionada" title="Asignaciones docentes" />
      {state === 'loading' ? <p>Cargando asignaciones docentes...</p> : null}
      {state === 'error' ? <p className="submit-error">{error}</p> : null}
      {state === 'ready' && assignments.length === 0 ? (
        <div className="empty-detail">
          <h3>No hay asignaciones docentes</h3>
          <p>No hay asignaciones docentes para esta carrera y ciclo lectivo.</p>
        </div>
      ) : null}
      <div className="assignment-card-list">
        {assignments.map((assignment) => (
          <article className="survey-list-card" key={assignment.id}>
            <header>
              <div>
                <p className="eyebrow">{assignment.subjectName}</p>
                <h3>{assignment.teacherFullName}</h3>
                <p>{assignment.teachingRole}</p>
              </div>
              <span className={`status-badge ${assignment.isActive ? 'status-badge--open' : 'status-badge--closed'}`}>
                {assignment.isActive ? 'Activa' : 'Inactiva'}
              </span>
            </header>
            <dl className="survey-card-meta">
              <div>
                <dt>Carrera</dt>
                <dd>{assignment.careerName}</dd>
              </div>
              <div>
                <dt>Ciclo</dt>
                <dd>{assignment.academicCycleYear} · {formatPeriod(assignment.academicCyclePeriod)}</dd>
              </div>
            </dl>
          </article>
        ))}
      </div>
    </section>
  );
}

function ContextHeader({ eyebrow, title }: { eyebrow: string; title: string }) {
  const context = useAcademicContext();

  return (
    <header className="surveys-header">
      <div>
        <p className="eyebrow">{eyebrow}</p>
        <h2>{title}</h2>
        <p>
          {context.selectedAcademicUnit?.name} · {context.selectedCareer?.name}
          {context.selectedAcademicCycle ? ` · ${formatAcademicCycle(context.selectedAcademicCycle)}` : ''}
        </p>
      </div>
    </header>
  );
}

function PermissionPanel({ message }: { message: string }) {
  return (
    <section className="app-content access-denied-panel">
      <p className="eyebrow">Sin acceso</p>
      <h2>{message}</h2>
      <p>El contexto académico no reemplaza los permisos asignados a tu usuario.</p>
    </section>
  );
}

function useContextGuard(requireCycle = false) {
  const context = useAcademicContext();

  if (!context.selectedAcademicUnit) {
    return (
      <section className="app-content empty-detail">
        <h3>Seleccioná una unidad académica.</h3>
        <p>Elegí una unidad académica en el panel lateral para comenzar.</p>
      </section>
    );
  }

  if (!context.selectedCareer) {
    return (
      <section className="app-content empty-detail">
        <h3>Seleccioná una carrera.</h3>
        <p>Elegí una carrera para ver sus materias, docentes, sesiones y resultados.</p>
      </section>
    );
  }

  if (requireCycle && !context.selectedAcademicCycle) {
    return (
      <section className="app-content empty-detail">
        <h3>Seleccioná un ciclo lectivo.</h3>
        <p>Elegí un ciclo lectivo para trabajar con información académica del período correcto.</p>
      </section>
    );
  }

  return null;
}

function groupSubjectsByYear(subjects: SubjectDto[]): Array<[number, SubjectDto[]]> {
  const grouped = new Map<number, SubjectDto[]>();

  subjects.forEach((subject) => {
    grouped.set(subject.year, [...(grouped.get(subject.year) ?? []), subject]);
  });

  return [...grouped.entries()]
    .sort(([left], [right]) => left - right)
    .map(([year, yearSubjects]) => [
      year,
      [...yearSubjects].sort((left, right) => left.name.localeCompare(right.name))
    ]);
}

function getAttentionActionRoute(actionCode: string): string {
  switch (actionCode) {
    case 'ReviewSubjectEnrollments':
    case 'AssignSubjectTeacher':
      return '/app/context/subjects';
    case 'ReviewSurveyAssignments':
    case 'ViewSurveyTemplates':
      return '/app/context/surveys';
    case 'ViewSurveyResults':
      return '/app/context/results';
    case 'ReviewAcademicCycle':
      return '/app/academic/cycles';
    default:
      return '/app/context';
  }
}

function getAttentionActionLabel(actionCode: string): string {
  switch (actionCode) {
    case 'ReviewSubjectEnrollments':
      return 'Revisar matrícula';
    case 'AssignSubjectTeacher':
      return 'Asignar docente';
    case 'ReviewSurveyAssignments':
      return 'Revisar asignaciones';
    case 'ViewSurveyTemplates':
      return 'Ver plantillas';
    case 'ViewSurveyResults':
      return 'Ver resultados';
    case 'ReviewAcademicCycle':
      return 'Revisar ciclo';
    default:
      return 'Revisar';
  }
}
