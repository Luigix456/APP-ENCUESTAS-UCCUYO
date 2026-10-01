import { useEffect, useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import { getResultAssignments } from '../../api/resultsApi';
import { useAuth } from '../../auth/AuthProvider';
import { PaginationControls, usePagination } from '../../components/Pagination';
import type { ResultsAssignmentFilters, SurveyAssignmentResultListItemDto } from '../../types/results';
import { useAcademicContext } from '../academic-context/AcademicContextProvider';
import {
  formatAcademicCycleParts,
  formatDateTimeOrEmpty,
  getFriendlyResultsError,
  hasResultsPermission,
  READ_CAREER_RESULTS_PERMISSION,
  ResultsPermissionPanel
} from './resultsUi';

type LoadState = 'loading' | 'ready' | 'error';

interface FilterOption {
  id: string;
  label: string;
}

const initialFilters: ResultsAssignmentFilters = {
  surveyId: '',
  careerId: '',
  subjectId: '',
  academicCycleId: '',
  teacherId: ''
};

export function ResultsPage({ contextual = false }: { contextual?: boolean }) {
  const auth = useAuth();
  const academicContext = useAcademicContext();
  const accessToken = auth.accessToken;
  const canReadResults = hasResultsPermission(auth.hasPermission);
  const [filters, setFilters] = useState<ResultsAssignmentFilters>(initialFilters);
  const [assignments, setAssignments] = useState<SurveyAssignmentResultListItemDto[]>([]);
  const [filterSource, setFilterSource] = useState<SurveyAssignmentResultListItemDto[]>([]);
  const [loadState, setLoadState] = useState<LoadState>('loading');
  const [pageError, setPageError] = useState<string | null>(null);

  useEffect(() => {
    if (!contextual) {
      return;
    }

    setFilters(initialFilters);
    setAssignments([]);
    setFilterSource([]);
  }, [academicContext.academicCycleId, academicContext.careerId, contextual]);

  useEffect(() => {
    if (
      !canReadResults ||
      !accessToken ||
      (contextual && (!academicContext.careerId || !academicContext.academicCycleId))
    ) {
      setLoadState('ready');
      setAssignments([]);
      return;
    }

    const controller = new AbortController();

    setLoadState('loading');
    setPageError(null);

    const effectiveFilters = buildEffectiveFilters(
      filters,
      contextual,
      academicContext.careerId,
      academicContext.academicCycleId
    );

    getResultAssignments(accessToken, auth.logout, effectiveFilters, controller.signal)
      .then((items) => {
        setAssignments(items);
        setLoadState('ready');

        if (isEmptySecondaryFilter(filters)) {
          setFilterSource(items);
        }
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
  }, [
    accessToken,
    academicContext.academicCycleId,
    academicContext.careerId,
    auth.logout,
    canReadResults,
    contextual,
    filters
  ]);

  const options = useMemo(() => buildFilterOptions(filterSource), [filterSource]);
  const resultsPagination = usePagination(assignments, 8);
  const hasAnyAuthorizedAssignment = filterSource.length > 0;
  const isReadCareerOnly =
    auth.hasPermission(READ_CAREER_RESULTS_PERMISSION) &&
    !auth.hasPermission('results.read_all');

  if (!canReadResults) {
    return <ResultsPermissionPanel />;
  }

  if (contextual && !academicContext.selectedCareer) {
    return (
      <section className="app-content empty-detail">
        <h3>Seleccioná una carrera.</h3>
        <p>Elegí una carrera en el panel lateral para consultar resultados.</p>
      </section>
    );
  }

  if (contextual && !academicContext.selectedAcademicCycle) {
    return (
      <section className="app-content empty-detail">
        <h3>Seleccioná un ciclo lectivo.</h3>
        <p>Elegí un ciclo lectivo para consultar resultados de encuesta.</p>
      </section>
    );
  }

  return (
    <section className="app-content results-page">
      <header className="results-header">
        <div>
          <p className="eyebrow">{contextual ? 'Carrera seleccionada' : 'Resultados'}</p>
          <h2>Resultados</h2>
          <p>
            {contextual
              ? 'Consultá resultados agregados para la carrera y ciclo lectivo seleccionados.'
              : 'Consultá resultados agregados por contexto académico autorizado.'}
          </p>
        </div>
        <button
          className="secondary-button"
          disabled={!accessToken || loadState === 'loading'}
          onClick={() => setFilters({ ...filters })}
          type="button"
        >
          Actualizar
        </button>
      </header>

      <div className="surveys-filters" aria-label="Filtros de resultados">
        <FilterSelect
          label="Encuesta"
          onChange={(value) => setFilters((current) => ({ ...current, surveyId: value }))}
          options={options.surveys}
          value={filters.surveyId}
        />
        {!contextual ? (
        <FilterSelect
          label="Carrera"
          onChange={(value) =>
            setFilters((current) => ({
              ...current,
              careerId: value,
              subjectId: ''
            }))
          }
          options={options.careers}
          value={filters.careerId}
        />
        ) : null}
        <FilterSelect
          label="Materia"
          onChange={(value) => setFilters((current) => ({ ...current, subjectId: value }))}
          options={options.subjects.filter((subject) => {
            const careerId = contextual ? academicContext.careerId : filters.careerId;
            return !careerId || subject.parentId === careerId;
          })}
          value={filters.subjectId}
        />
        {!contextual ? (
        <FilterSelect
          label="Ciclo"
          onChange={(value) => setFilters((current) => ({ ...current, academicCycleId: value }))}
          options={options.cycles}
          value={filters.academicCycleId}
        />
        ) : null}
        <FilterSelect
          label="Docente"
          onChange={(value) => setFilters((current) => ({ ...current, teacherId: value }))}
          options={options.teachers}
          value={filters.teacherId}
        />
        <button
          className="secondary-button"
          onClick={() => setFilters(contextual ? { ...initialFilters, careerId: '', academicCycleId: '' } : initialFilters)}
          type="button"
        >
          Limpiar filtros
        </button>
      </div>

      {loadState === 'loading' ? <p aria-live="polite">Cargando resultados...</p> : null}

      {loadState === 'error' ? (
        <div className="empty-detail" role="alert">
          <h3>No pudimos cargar los resultados</h3>
          <p>{pageError}</p>
        </div>
      ) : null}

      {loadState === 'ready' && assignments.length === 0 ? (
        <div className="empty-detail">
          <h3>No hay resultados disponibles</h3>
          <p>
            {hasAnyAuthorizedAssignment
              ? 'No hay resultados que coincidan con los filtros seleccionados.'
              : isReadCareerOnly
                ? 'No hay resultados disponibles para tus carreras asignadas.'
                : contextual
                  ? 'No hay resultados disponibles para esta carrera y ciclo lectivo.'
                  : 'No hay resultados disponibles para consultar.'}
          </p>
        </div>
      ) : null}

      {loadState === 'ready' && assignments.length > 0 ? (
        <>
          <PaginationControls
            firstItem={resultsPagination.firstItem}
            itemLabel="resultados"
            lastItem={resultsPagination.lastItem}
            onPageChange={resultsPagination.setPage}
            onPageSizeChange={resultsPagination.setPageSize}
            page={resultsPagination.page}
            pageSize={resultsPagination.pageSize}
            totalItems={resultsPagination.totalItems}
            totalPages={resultsPagination.totalPages}
          />
          <div className="result-card-list" role="list">
            {resultsPagination.items.map((assignment) => (
              <ResultAssignmentCard assignment={assignment} key={assignment.surveyAssignmentId} />
            ))}
          </div>
        </>
      ) : null}
    </section>
  );
}

function ResultAssignmentCard({ assignment }: { assignment: SurveyAssignmentResultListItemDto }) {
  return (
    <article className="result-card" role="listitem">
      <header>
        <div>
          <p className="eyebrow">{assignment.surveyTitle}</p>
          <h3>{assignment.subjectName}</h3>
          <p>
            {assignment.careerName} · {formatAcademicCycleParts(
              assignment.academicCycleYear,
              assignment.academicCyclePeriod
            )}
          </p>
        </div>
        <div className="badge-group">
          {assignment.isActive ? (
            <span className="status-badge status-badge--open">Activa</span>
          ) : (
            <span className="status-badge status-badge--closed">Inactiva</span>
          )}
          {assignment.totalResponses === 0 ? <span className="status-badge">Sin respuestas</span> : null}
        </div>
      </header>

      <dl className="survey-card-meta">
        <div>
          <dt>Docente</dt>
          <dd>{assignment.teacherFullName}</dd>
        </div>
        <div>
          <dt>Rol docente</dt>
          <dd>{assignment.teachingRole}</dd>
        </div>
        <div>
          <dt>Respuestas</dt>
          <dd>{assignment.totalResponses}</dd>
        </div>
        <div>
          <dt>Sesiones</dt>
          <dd>{assignment.totalSessions}</dd>
        </div>
        <div>
          <dt>Primera respuesta</dt>
          <dd>{formatDateTimeOrEmpty(assignment.firstSubmittedAtUtc)}</dd>
        </div>
        <div>
          <dt>Última respuesta</dt>
          <dd>{formatDateTimeOrEmpty(assignment.lastSubmittedAtUtc)}</dd>
        </div>
      </dl>

      <div className="survey-card-actions">
        <Link
          className="primary-link-button"
          to={`/app/results/assignments/${encodeURIComponent(assignment.surveyAssignmentId)}`}
        >
          Ver resultados
        </Link>
      </div>
    </article>
  );
}

function FilterSelect({
  label,
  onChange,
  options,
  value
}: {
  label: string;
  onChange: (value: string) => void;
  options: FilterOption[];
  value: string;
}) {
  return (
    <label>
      <span>{label}</span>
      <select className="text-input" onChange={(event) => onChange(event.target.value)} value={value}>
        <option value="">Todos</option>
        {options.map((option) => (
          <option key={option.id} value={option.id}>
            {option.label}
          </option>
        ))}
      </select>
    </label>
  );
}

function buildFilterOptions(items: SurveyAssignmentResultListItemDto[]) {
  return {
    surveys: uniqueOptions(items, (item) => ({ id: item.surveyId, label: item.surveyTitle })),
    careers: uniqueOptions(items, (item) => ({ id: item.careerId, label: item.careerName })),
    subjects: uniqueOptionsWithParent(items, (item) => ({
      id: item.subjectId,
      label: item.subjectName,
      parentId: item.careerId
    })),
    cycles: uniqueOptions(items, (item) => ({
      id: item.academicCycleId,
      label: formatAcademicCycleParts(item.academicCycleYear, item.academicCyclePeriod)
    })),
    teachers: uniqueOptions(items, (item) => ({ id: item.teacherId, label: item.teacherFullName }))
  };
}

function uniqueOptions(
  items: SurveyAssignmentResultListItemDto[],
  selector: (item: SurveyAssignmentResultListItemDto) => FilterOption
): FilterOption[] {
  const options = new Map<string, FilterOption>();

  items.forEach((item) => {
    const option = selector(item);
    options.set(option.id, option);
  });

  return [...options.values()].sort((left, right) => left.label.localeCompare(right.label));
}

function uniqueOptionsWithParent(
  items: SurveyAssignmentResultListItemDto[],
  selector: (item: SurveyAssignmentResultListItemDto) => FilterOption & { parentId: string }
): Array<FilterOption & { parentId: string }> {
  const options = new Map<string, FilterOption & { parentId: string }>();

  items.forEach((item) => {
    const option = selector(item);
    options.set(option.id, option);
  });

  return [...options.values()].sort((left, right) => left.label.localeCompare(right.label));
}

function isEmptySecondaryFilter(filters: ResultsAssignmentFilters): boolean {
  return !filters.surveyId && !filters.subjectId && !filters.teacherId;
}

function buildEffectiveFilters(
  filters: ResultsAssignmentFilters,
  contextual: boolean,
  careerId: string,
  academicCycleId: string
): ResultsAssignmentFilters {
  if (!contextual) {
    return filters;
  }

  return {
    ...filters,
    careerId,
    academicCycleId
  };
}

function isAbortError(error: unknown): boolean {
  return error instanceof DOMException && error.name === 'AbortError';
}
