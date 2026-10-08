import { useEffect, useMemo, useState, type FormEvent } from 'react';
import { getAuditEntries } from '../../api/auditApi';
import { ApiClientError } from '../../api/apiClient';
import { useAuth } from '../../auth/AuthProvider';
import { PaginationControls } from '../../components/Pagination';
import { Modal } from '../../components/ui/Modal';
import type { AuditEntriesPageDto, AuditEntryDto } from '../../types/audit';
import { formatDateTime } from '../surveys/surveyUi';

type LoadState = 'loading' | 'ready' | 'error';

interface AuditFilterForm {
  search: string;
  module: string;
  action: string;
  from: string;
  to: string;
}

const PAGE_SIZE_OPTIONS = [10, 25, 50, 100];
const AUDIT_READ_PERMISSION = 'audit.read';

export function AuditPage() {
  const auth = useAuth();
  const accessToken = auth.accessToken;
  const [state, setState] = useState<LoadState>('loading');
  const [error, setError] = useState<string | null>(null);
  const [pageData, setPageData] = useState<AuditEntriesPageDto | null>(null);
  const [selectedEntry, setSelectedEntry] = useState<AuditEntryDto | null>(null);
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(25);
  const [reloadKey, setReloadKey] = useState(0);
  const [filters, setFilters] = useState<AuditFilterForm>({
    search: '',
    module: '',
    action: '',
    from: '',
    to: ''
  });

  const canReadAudit = auth.hasPermission(AUDIT_READ_PERMISSION);

  const queryFilters = useMemo(() => ({
    search: filters.search,
    module: filters.module,
    action: filters.action,
    fromUtc: toUtcBoundary(filters.from, 'start'),
    toUtc: toUtcBoundary(filters.to, 'end'),
    page,
    pageSize
  }), [filters, page, pageSize]);

  useEffect(() => {
    if (!accessToken || !canReadAudit) {
      setState('ready');
      return;
    }

    const abortController = new AbortController();
    setState('loading');
    setError(null);

    getAuditEntries(accessToken, auth.logout, queryFilters, abortController.signal)
      .then((result) => {
        setPageData(result);
        setState('ready');
      })
      .catch((loadError) => {
        if (abortController.signal.aborted) {
          return;
        }

        setError(getFriendlyAuditError(loadError));
        setState('error');
      });

    return () => {
      abortController.abort();
    };
  }, [accessToken, auth.logout, canReadAudit, queryFilters, reloadKey]);

  if (!canReadAudit) {
    return (
      <section className="app-content access-denied-panel">
        <p className="eyebrow">Sin acceso</p>
        <h2>No tenés permisos para consultar la auditoría.</h2>
        <p>Solicitá el permiso de auditoría a la administración del sistema.</p>
      </section>
    );
  }

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setPage(1);
    setReloadKey((current) => current + 1);
  }

  function handleReset() {
    setFilters({
      search: '',
      module: '',
      action: '',
      from: '',
      to: ''
    });
    setPage(1);
  }

  const entries = pageData?.items ?? [];
  const totalItems = pageData?.totalItems ?? 0;
  const totalPages = pageData?.totalPages ?? 1;
  const hasActiveFilters = Object.values(filters).some((value) => value.trim().length > 0);
  const firstItem = totalItems === 0 ? 0 : (pageData!.page - 1) * pageData!.pageSize + 1;
  const lastItem = pageData ? Math.min(pageData.page * pageData.pageSize, totalItems) : 0;

  return (
    <section className="app-content audit-page">
      <header className="surveys-header">
        <div>
          <p className="eyebrow">Administración</p>
          <h2>Auditoría</h2>
          <p>Consultá cambios administrativos: quién realizó la acción, cuándo y sobre qué módulo.</p>
        </div>
      </header>

      <form className="audit-filters" onSubmit={handleSubmit}>
        <label className="audit-filter-search">
          <span>Buscar</span>
          <input
            className="text-input"
            onChange={(event) => setFilters((current) => ({ ...current, search: event.target.value }))}
            placeholder="Usuario o descripción"
            type="search"
            value={filters.search}
          />
        </label>
        <label>
          <span>Módulo</span>
          <select
            className="text-input"
            onChange={(event) => { setFilters((current) => ({ ...current, module: event.target.value })); setPage(1); }}
            value={filters.module}
          >
            <option value="">Todos</option>
            <option value="identity">Identidad</option>
            <option value="academic">Estructura académica</option>
            <option value="surveys">Encuestas</option>
            <option value="sessions">Sesiones</option>
            <option value="reports">Informes</option>
          </select>
        </label>
        <label>
          <span>Acción</span>
          <input
            className="text-input"
            onChange={(event) => setFilters((current) => ({ ...current, action: event.target.value }))}
            placeholder="Ej: surveys.survey.published"
            type="text"
            value={filters.action}
          />
        </label>
        <label>
          <span>Desde</span>
          <input
            className="text-input"
            onChange={(event) => { setFilters((current) => ({ ...current, from: event.target.value })); setPage(1); }}
            type="date"
            value={filters.from}
          />
        </label>
        <label>
          <span>Hasta</span>
          <input
            className="text-input"
            onChange={(event) => { setFilters((current) => ({ ...current, to: event.target.value })); setPage(1); }}
            type="date"
            value={filters.to}
          />
        </label>
        <div className="audit-filter-actions">
          <button className="primary-button" type="submit">Buscar</button>
          <button className="secondary-button" onClick={handleReset} type="button">Limpiar</button>
        </div>
      </form>

      {state === 'loading' ? <p aria-live="polite">Cargando auditoría...</p> : null}

      {state === 'error' ? (
        <div className="audit-state-card audit-state-card--error" role="alert">
          <h3>No pudimos cargar la auditoría</h3>
          <p>{error ?? 'Revisá los filtros e intentá nuevamente.'}</p>
          <button className="secondary-button compact-button" onClick={() => setReloadKey((current) => current + 1)} type="button">
            Reintentar
          </button>
        </div>
      ) : null}

      {state === 'ready' && entries.length === 0 ? (
        <div className="audit-state-card">
          <h3>{hasActiveFilters ? 'No se encontraron registros con los filtros seleccionados.' : 'Todavía no hay registros de auditoría.'}</h3>
          <p>{hasActiveFilters ? 'Probá ajustar la búsqueda o limpiar los filtros.' : 'Cuando se registren acciones administrativas, aparecerán en esta lista.'}</p>
        </div>
      ) : null}

      {entries.length > 0 ? (
        <>
          <div className="audit-table-wrapper">
            <table className="audit-table">
              <thead>
                <tr>
                  <th>Fecha y hora</th>
                  <th>Usuario</th>
                  <th>Módulo</th>
                  <th>Acción</th>
                  <th>Descripción</th>
                  <th>Detalle</th>
                </tr>
              </thead>
              <tbody>
                {entries.map((entry) => (
                  <tr key={entry.id}>
                    <td>{formatDateTime(entry.occurredAtUtc)}</td>
                    <td>{entry.actorDisplayName ?? 'Sistema'}</td>
                    <td>{formatModule(entry.module)}</td>
                    <td>{formatAction(entry.action)}</td>
                    <td>{entry.description}</td>
                    <td>
                      <button className="secondary-button compact-button" onClick={() => setSelectedEntry(entry)} type="button">
                        Ver detalle
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          <div className="audit-card-list" role="list">
            {entries.map((entry) => (
              <article className="audit-card" key={entry.id} role="listitem">
                <header>
                  <span>{formatModule(entry.module)}</span>
                  <strong>{formatAction(entry.action)}</strong>
                </header>
                <p>{entry.description}</p>
                <dl>
                  <div><dt>Fecha</dt><dd>{formatDateTime(entry.occurredAtUtc)}</dd></div>
                  <div><dt>Usuario</dt><dd>{entry.actorDisplayName ?? 'Sistema'}</dd></div>
                </dl>
                <button className="secondary-button" onClick={() => setSelectedEntry(entry)} type="button">
                  Ver detalle
                </button>
              </article>
            ))}
          </div>

          <PaginationControls
            firstItem={firstItem}
            itemLabel="eventos"
            lastItem={lastItem}
            onPageChange={setPage}
            onPageSizeChange={(nextPageSize) => { setPageSize(nextPageSize); setPage(1); }}
            page={pageData?.page ?? page}
            pageSize={pageData?.pageSize ?? pageSize}
            pageSizeOptions={PAGE_SIZE_OPTIONS}
            totalItems={totalItems}
            totalPages={totalPages}
          />
        </>
      ) : null}

      <Modal
        description="Detalle seguro del evento registrado."
        onClose={() => setSelectedEntry(null)}
        open={selectedEntry !== null}
        title="Detalle de auditoría"
      >
        {selectedEntry ? <AuditEntryDetail entry={selectedEntry} /> : null}
      </Modal>
    </section>
  );
}

function AuditEntryDetail({ entry }: { entry: AuditEntryDto }) {
  return (
    <div className="audit-detail">
      <dl className="summary-list">
        <div><dt>Fecha y hora</dt><dd>{formatDateTime(entry.occurredAtUtc)}</dd></div>
        <div><dt>Usuario</dt><dd>{entry.actorDisplayName ?? 'Sistema'}</dd></div>
        <div><dt>Módulo</dt><dd>{formatModule(entry.module)}</dd></div>
        <div><dt>Acción</dt><dd>{formatAction(entry.action)}</dd></div>
        <div><dt>Entidad</dt><dd>{entry.entityType}{entry.entityId ? ` · ${entry.entityId}` : ''}</dd></div>
      </dl>
      <div>
        <h3>Descripción</h3>
        <p>{entry.description}</p>
      </div>
      <div>
        <h3>Metadata</h3>
        {entry.metadata ? <MetadataView value={entry.metadata} /> : <p className="muted-text">Sin metadata adicional.</p>}
      </div>
    </div>
  );
}

function MetadataView({ value }: { value: unknown }) {
  if (Array.isArray(value)) {
    return (
      <ul className="audit-metadata-list">
        {value.map((item, index) => (
          <li key={index}><MetadataView value={item} /></li>
        ))}
      </ul>
    );
  }

  if (value && typeof value === 'object') {
    return (
      <dl className="audit-metadata">
        {Object.entries(value as Record<string, unknown>).map(([key, item]) => (
          <div key={key}>
            <dt>{formatMetadataKey(key)}</dt>
            <dd><MetadataValue value={item} /></dd>
          </div>
        ))}
      </dl>
    );
  }

  return <MetadataValue value={value} />;
}

function MetadataValue({ value }: { value: unknown }) {
  if (value === null || value === undefined || value === '') {
    return <span className="muted-text">Sin dato</span>;
  }

  if (typeof value === 'boolean') {
    return <span>{value ? 'Sí' : 'No'}</span>;
  }

  if (Array.isArray(value) || typeof value === 'object') {
    return <MetadataView value={value} />;
  }

  return <span>{String(value)}</span>;
}

function toUtcBoundary(value: string, boundary: 'start' | 'end'): string | undefined {
  if (!value) {
    return undefined;
  }

  const suffix = boundary === 'start' ? 'T00:00:00.000' : 'T23:59:59.999';
  return new Date(`${value}${suffix}`).toISOString();
}

function formatModule(module: string): string {
  const labels: Record<string, string> = {
    identity: 'Identidad',
    academic: 'Estructura académica',
    academic_catalog: 'Catálogo académico',
    surveys: 'Encuestas',
    survey_templates: 'Plantillas',
    survey_assignments: 'Asignaciones',
    sessions: 'Sesiones',
    survey_sessions: 'Sesiones',
    reports: 'Informes'
  };

  return labels[module] ?? module;
}

function formatAction(action: string): string {
  return action
    .split('.')
    .slice(-1)[0]
    .replaceAll('_', ' ');
}

function formatMetadataKey(key: string): string {
  return key
    .replace(/([a-z])([A-Z])/g, '$1 $2')
    .replaceAll('_', ' ')
    .toLowerCase();
}

function getFriendlyAuditError(error: unknown): string {
  if (error instanceof ApiClientError) {
    if (error.status === 401) {
      return 'La sesión venció. Volvé a iniciar sesión.';
    }

    if (error.status === 403) {
      return 'No tenés permisos para consultar auditoría.';
    }

    if (error.status === 400) {
      return 'Revisá los filtros e intentá nuevamente.';
    }
  }

  return 'No fue posible cargar los eventos de auditoría.';
}
