import { useEffect, useMemo, useState, type ReactNode } from 'react';
import { Link, NavLink, Navigate, Outlet, useLocation, useNavigate, useParams, useSearchParams } from 'react-router-dom';
import {
  getAcademicCycles,
  getAcademicUnits,
  getAcademicUnit,
  getCareers,
  getCareer
} from '../../api/academicCatalogApi';
import { useAuth } from '../../auth/AuthProvider';
import { PaginationControls, usePagination } from '../../components/Pagination';
import type { AcademicCycleDto, AcademicUnitDto, CareerDto, CareerType } from '../../types/academicCatalog';
import { formatAcademicCycle, formatCareerType, getFriendlyCatalogError } from '../academic-catalog/academicCatalogUi';
import { useAcademicContext } from './AcademicContextProvider';

type LoadState = 'loading' | 'ready' | 'error';

const READ_CATALOG = 'academic.catalog.read';
const MANAGE_CATALOG = 'academic.catalog.manage';

function formatActiveCareerCount(count: number): string {
  return count === 1 ? '1 carrera activa' : `${count} carreras activas`;
}

export function AcademicUnitPage() {
  const auth = useAuth();
  const { unitId } = useParams();
  const [unit, setUnit] = useState<AcademicUnitDto | null>(null);
  const [careers, setCareers] = useState<CareerDto[]>([]);
  const [state, setState] = useState<LoadState>('loading');
  const [error, setError] = useState<string | null>(null);
  const [search, setSearch] = useState('');
  const [typeFilter, setTypeFilter] = useState<CareerType | ''>('');
  const [reloadKey, setReloadKey] = useState(0);
  const accessToken = auth.accessToken;
  const canReadCatalog = auth.hasPermission(READ_CATALOG) || auth.hasPermission(MANAGE_CATALOG);

  useEffect(() => {
    if (!accessToken || !unitId || !canReadCatalog) {
      setState('ready');
      return;
    }

    const controller = new AbortController();
    let cancelled = false;
    setState('loading');
    setError(null);

    Promise.all([
      getAcademicUnit(unitId, { accessToken, onUnauthorized: auth.logout }),
      getCareers({
        accessToken,
        academicUnitId: unitId,
        includeInactive: false,
        onUnauthorized: auth.logout,
        signal: controller.signal
      })
    ])
      .then(([nextUnit, nextCareers]) => {
        if (cancelled) {
          return;
        }

        setUnit(nextUnit);
        setCareers(nextCareers);
        setState('ready');
      })
      .catch((loadError) => {
        if (cancelled || controller.signal.aborted) return;
        setError(getFriendlyCatalogError(loadError, 'No fue posible cargar la unidad académica.'));
        setState('error');
      });

    return () => {
      cancelled = true;
      controller.abort();
    };
  }, [accessToken, auth.logout, canReadCatalog, reloadKey, unitId]);

  const filteredCareers = useMemo(() => {
    const normalizedSearch = search.trim().toLowerCase();

    return careers
      .filter((career) => !typeFilter || career.type === typeFilter)
      .filter((career) =>
        !normalizedSearch
        || career.name.toLowerCase().includes(normalizedSearch)
        || career.code.toLowerCase().includes(normalizedSearch))
      .sort((left, right) => left.name.localeCompare(right.name));
  }, [careers, search, typeFilter]);
  const pagination = usePagination(filteredCareers, 6);

  if (!canReadCatalog) {
    return (
      <section className="app-content access-denied-panel">
        <p className="eyebrow">Sin acceso</p>
        <h2>No tenés permisos para consultar unidades académicas.</h2>
      </section>
    );
  }

  if (state === 'loading') {
    return <WorkspaceLoading message="Cargando unidad académica..." />;
  }

  if (state === 'error' || !unit) {
    return (
      <section className="app-content empty-detail state-card" role="alert">
        <h3>No pudimos abrir la unidad académica.</h3>
        <p>{error}</p>
        <div className="state-card__actions">
          <button className="secondary-button" onClick={() => setReloadKey((current) => current + 1)} type="button">
            Reintentar
          </button>
          <Link className="secondary-button" to="/app/academic/units">Volver a unidades académicas</Link>
        </div>
      </section>
    );
  }

  return (
    <section className="app-content academic-workspace">
      <Link className="back-link" to="/app/academic/units">← Volver a unidades académicas</Link>
      <header className="workspace-hero">
        <div>
          <p className="eyebrow">Unidad académica</p>
          <h2>{unit.name}</h2>
          <p>Seleccioná una carrera para administrar su información académica.</p>
        </div>
        <strong>{formatActiveCareerCount(careers.filter((career) => career.isActive).length)}</strong>
      </header>

      <div className="career-browser">
        <label>
          <span>Buscar carrera</span>
          <input
            className="text-input"
            onChange={(event) => setSearch(event.target.value)}
            placeholder="Buscar carrera por nombre"
            type="search"
            value={search}
          />
        </label>
        <label>
          <span>Tipo</span>
          <select
            className="text-input"
            onChange={(event) => setTypeFilter(event.target.value as CareerType | '')}
            value={typeFilter}
          >
            <option value="">Todos</option>
            <option value="Undergraduate">Grado</option>
            <option value="Postgraduate">Posgrado</option>
            <option value="Course">Curso</option>
            <option value="Other">Otro</option>
          </select>
        </label>
      </div>

      {pagination.items.length > 0 ? (
        <div className="career-card-grid">
          {pagination.items.map((career) => (
            <article className="career-entry-card" key={career.id}>
              <div>
                <h3>{career.name}</h3>
                <span>{formatCareerType(career.type)}</span>
              </div>
              <Link className="primary-link-button" to={`/app/academic/units/${unit.id}/careers/${career.id}`}>
                Ingresar
              </Link>
            </article>
          ))}
        </div>
      ) : (
        <div className="empty-detail">
          <h3>No hay carreras con esos filtros.</h3>
          <p>Probá limpiar la búsqueda o seleccionar otro tipo.</p>
        </div>
      )}

      <PaginationControls
        firstItem={pagination.firstItem}
        itemLabel="carreras"
        lastItem={pagination.lastItem}
        onPageChange={pagination.setPage}
        onPageSizeChange={pagination.setPageSize}
        page={pagination.page}
        pageSize={pagination.pageSize}
        pageSizeOptions={[6, 9, 12]}
        showSummary={false}
        totalItems={pagination.totalItems}
        totalPages={pagination.totalPages}
      />
    </section>
  );
}

export function AcademicUnitsPage() {
  const auth = useAuth();
  const [units, setUnits] = useState<AcademicUnitDto[]>([]);
  const [careerCountsByUnitId, setCareerCountsByUnitId] = useState<Record<string, number>>({});
  const [state, setState] = useState<LoadState>('loading');
  const [error, setError] = useState<string | null>(null);
  const [reloadKey, setReloadKey] = useState(0);
  const accessToken = auth.accessToken;
  const canReadCatalog = auth.hasPermission(READ_CATALOG) || auth.hasPermission(MANAGE_CATALOG);

  useEffect(() => {
    if (!accessToken || !canReadCatalog) {
      setState('ready');
      return;
    }

    const controller = new AbortController();
    setState('loading');
    setError(null);

    Promise.all([
      getAcademicUnits({
        accessToken,
        includeInactive: false,
        onUnauthorized: auth.logout,
        signal: controller.signal
      }),
      getCareers({
        accessToken,
        includeInactive: false,
        onUnauthorized: auth.logout,
        signal: controller.signal
      })
    ])
      .then(([nextUnits, nextCareers]) => {
        const nextCounts = nextCareers.reduce<Record<string, number>>((counts, career) => {
          counts[career.academicUnitId] = (counts[career.academicUnitId] ?? 0) + 1;
          return counts;
        }, {});

        setUnits([...nextUnits].sort((left, right) => left.name.localeCompare(right.name)));
        setCareerCountsByUnitId(nextCounts);
        setState('ready');
      })
      .catch((loadError) => {
        if (controller.signal.aborted) return;
        setError(getFriendlyCatalogError(loadError, 'No fue posible cargar las unidades académicas.'));
        setState('error');
      });

    return () => controller.abort();
  }, [accessToken, auth.logout, canReadCatalog, reloadKey]);

  if (!canReadCatalog) {
    return (
      <section className="app-content access-denied-panel">
        <p className="eyebrow">Sin acceso</p>
        <h2>No tenés permisos para consultar unidades académicas.</h2>
      </section>
    );
  }

  if (state === 'loading') {
    return <WorkspaceLoading message="Cargando unidades académicas..." />;
  }

  if (state === 'error') {
    return (
      <section className="app-content empty-detail state-card" role="alert">
        <h3>No pudimos cargar las unidades académicas.</h3>
        <p>{error}</p>
        <button className="secondary-button" onClick={() => setReloadKey((current) => current + 1)} type="button">
          Reintentar
        </button>
      </section>
    );
  }

  return (
    <section className="app-content academic-workspace">
      <header className="workspace-hero">
        <div>
          <h2>Unidades académicas</h2>
          <p>Seleccioná una unidad para acceder a sus carreras.</p>
        </div>
      </header>

      {units.length > 0 ? (
        <div className="academic-unit-grid">
          {units.map((unit) => (
            <Link className="academic-unit-card" key={unit.id} to={`/app/academic/units/${unit.id}`}>
              <h3>{unit.name}</h3>
              <strong>{formatActiveCareerCount(careerCountsByUnitId[unit.id] ?? 0)}</strong>
            </Link>
          ))}
        </div>
      ) : (
        <div className="empty-detail">
          <h3>No hay unidades académicas activas.</h3>
          <p>Cuando exista una unidad activa, aparecerá en este listado.</p>
        </div>
      )}
    </section>
  );
}

export function CareerWorkspaceRedirect() {
  const location = useLocation();
  return <Navigate replace to={`${location.pathname}/overview${location.search}`} />;
}

export function CareerWorkspacePage({ children }: { children?: ReactNode }) {
  const auth = useAuth();
  const academicContext = useAcademicContext();
  const { unitId, careerId } = useParams();
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();
  const location = useLocation();
  const [state, setState] = useState<LoadState>('loading');
  const [error, setError] = useState<string | null>(null);
  const [unit, setUnit] = useState<AcademicUnitDto | null>(null);
  const [career, setCareer] = useState<CareerDto | null>(null);
  const [cycles, setCycles] = useState<AcademicCycleDto[]>([]);
  const [reloadKey, setReloadKey] = useState(0);
  const accessToken = auth.accessToken;
  const canReadCatalog = auth.hasPermission(READ_CATALOG) || auth.hasPermission(MANAGE_CATALOG);
  const requestedCycleId = searchParams.get('cycle') ?? '';
  const searchText = searchParams.toString();

  useEffect(() => {
    if (!accessToken || !unitId || !careerId || !canReadCatalog) {
      setState('ready');
      return;
    }

    if (unit?.id === unitId && career?.id === careerId && cycles.length > 0) {
      const selectedCycle = cycles.find((cycle) => cycle.id === requestedCycleId) ?? cycles[0];

      if (selectedCycle.id !== requestedCycleId) {
        const nextParams = new URLSearchParams(searchText);
        nextParams.set('cycle', selectedCycle.id);
        navigate(`${location.pathname}?${nextParams.toString()}`, { replace: true });
      }

      academicContext.setRouteContext(unit, career, cycles, selectedCycle.id);
      setState('ready');
      return;
    }

    const controller = new AbortController();
    let cancelled = false;
    setState('loading');
    setError(null);

    Promise.all([
      getAcademicUnit(unitId, { accessToken, onUnauthorized: auth.logout }),
      getCareer(careerId, { accessToken, onUnauthorized: auth.logout }),
      getAcademicCycles({ accessToken, includeInactive: false, onUnauthorized: auth.logout, signal: controller.signal })
    ])
      .then(([nextUnit, nextCareer, nextCycles]) => {
        if (cancelled) {
          return;
        }

        if (nextCareer.academicUnitId !== nextUnit.id) {
          setUnit(nextUnit);
          setCareer(null);
          setCycles([]);
          setError('Carrera no encontrada en esta unidad académica.');
          setState('error');
          return;
        }

        const orderedCycles = [...nextCycles].sort(compareAcademicCycles);
        const selectedCycle = orderedCycles.find((cycle) => cycle.id === requestedCycleId)
          ?? orderedCycles[0]
          ?? null;

        setUnit(nextUnit);
        setCareer(nextCareer);
        setCycles(orderedCycles);

        if (selectedCycle && selectedCycle.id !== requestedCycleId) {
          const nextParams = new URLSearchParams(searchText);
          nextParams.set('cycle', selectedCycle.id);
          navigate(`${location.pathname}?${nextParams.toString()}`, { replace: true });
        }

        academicContext.setRouteContext(nextUnit, nextCareer, orderedCycles, selectedCycle?.id ?? '');
        setState('ready');
      })
      .catch((loadError) => {
        if (cancelled || controller.signal.aborted) return;
        setError(getFriendlyCatalogError(loadError, 'No fue posible cargar la carrera.'));
        setState('error');
      });

    return () => {
      cancelled = true;
      controller.abort();
    };
  }, [
    accessToken,
    auth.logout,
    canReadCatalog,
    careerId,
    location.pathname,
    navigate,
    reloadKey,
    requestedCycleId,
    searchText,
    unitId
  ]);

  const selectedCycleId = searchParams.get('cycle') ?? academicContext.academicCycleId;
  const selectedCycle = cycles.find((cycle) => cycle.id === selectedCycleId) ?? null;

  function handleCycleChange(cycleId: string) {
    const nextParams = new URLSearchParams(searchParams);
    nextParams.set('cycle', cycleId);
    academicContext.setAcademicCycle(cycleId);
    navigate(`${location.pathname}?${nextParams.toString()}`, { replace: true });
  }

  if (!canReadCatalog) {
    return (
      <section className="app-content access-denied-panel">
        <p className="eyebrow">Sin acceso</p>
        <h2>No tenés permisos para consultar esta carrera.</h2>
      </section>
    );
  }

  if (state === 'loading') {
    return <WorkspaceLoading message="Cargando carrera..." />;
  }

  if (state === 'error' || !unit || !career) {
    return (
      <section className="app-content empty-detail state-card" role="alert">
        <h3>{error ?? 'Carrera no encontrada en esta unidad académica.'}</h3>
        <div className="state-card__actions">
          <button className="secondary-button" onClick={() => setReloadKey((current) => current + 1)} type="button">
            Reintentar
          </button>
          <Link className="secondary-button" to={unitId ? `/app/academic/units/${unitId}` : '/app/academic/units'}>
            Volver a carreras
          </Link>
        </div>
      </section>
    );
  }

  return (
    <section className="career-workspace-shell">
      <nav className="workspace-breadcrumb" aria-label="Migas de pan">
        <Link to="/app/academic/units">Unidades académicas</Link>
        <span>/</span>
        <Link to={`/app/academic/units/${unit.id}`}>{unit.name}</Link>
        <span>/</span>
        <span>{career.name}</span>
      </nav>

      <header className="workspace-hero workspace-hero--career">
        <div>
          <Link className="back-link" to={`/app/academic/units/${unit.id}`}>← Volver a carreras</Link>
          <p className="eyebrow">{unit.name}</p>
          <h2>{career.name}</h2>
          <p>Gestioná la actividad académica y las encuestas de esta carrera.</p>
          <span className="status-badge">{formatCareerType(career.type)}</span>
        </div>
        <label className="workspace-cycle-select">
          <span>Ciclo lectivo</span>
          <select
            className="text-input"
            onChange={(event) => handleCycleChange(event.target.value)}
            value={selectedCycle?.id ?? ''}
          >
            {cycles.map((cycle) => (
              <option key={cycle.id} value={cycle.id}>
                {formatAcademicCycle(cycle)}
              </option>
            ))}
          </select>
        </label>
      </header>

      <CareerLocalNavigation unitId={unit.id} careerId={career.id} cycleId={selectedCycle?.id ?? ''} />
      {children ?? <Outlet />}
    </section>
  );
}

function CareerLocalNavigation({
  unitId,
  careerId,
  cycleId
}: {
  unitId: string;
  careerId: string;
  cycleId: string;
}) {
  const auth = useAuth();
  const basePath = `/app/academic/units/${unitId}/careers/${careerId}`;
  const query = cycleId ? `?cycle=${encodeURIComponent(cycleId)}` : '';
  const items = [
    { label: 'Resumen', to: `${basePath}/overview${query}`, visible: auth.hasPermission(READ_CATALOG) || auth.hasPermission(MANAGE_CATALOG) },
    { label: 'Materias', to: `${basePath}/subjects${query}`, visible: auth.hasPermission(READ_CATALOG) || auth.hasPermission(MANAGE_CATALOG) },
    { label: 'Docentes', to: `${basePath}/teachers${query}`, visible: auth.hasPermission(READ_CATALOG) || auth.hasPermission(MANAGE_CATALOG) },
    { label: 'Encuestas asignadas', to: `${basePath}/surveys${query}`, visible: auth.hasPermission('surveys.templates.read') || auth.hasPermission('surveys.templates.manage') },
    { label: 'Sesiones', to: `${basePath}/sessions${query}`, visible: auth.hasPermission('surveys.sessions.manage') },
    { label: 'Resultados', to: `${basePath}/results${query}`, visible: auth.hasPermission('results.read_all') || auth.hasPermission('results.read_career') },
    { label: 'Histórico', to: `${basePath}/history${query}`, visible: auth.hasPermission('results.read_all') || auth.hasPermission('results.read_career') }
  ];

  return (
    <nav className="career-local-nav" aria-label="Navegación de carrera">
      {items.filter((item) => item.visible).map((item) => (
        <NavLink
          key={item.to}
          to={item.to}
        >
          {item.label}
        </NavLink>
      ))}
    </nav>
  );
}

function WorkspaceLoading({ message }: { message: string }) {
  return (
    <section className="app-content state-card" aria-busy="true" aria-live="polite">
      <div className="loading-dot" aria-hidden="true" />
      <p>{message}</p>
    </section>
  );
}

function compareAcademicCycles(left: AcademicCycleDto, right: AcademicCycleDto): number {
  const yearDifference = right.year - left.year;

  if (yearDifference !== 0) {
    return yearDifference;
  }

  return getPeriodOrder(left.period) - getPeriodOrder(right.period);
}

function getPeriodOrder(period: AcademicCycleDto['period']): number {
  switch (period) {
    case 'Annual':
      return 0;
    case 'FirstSemester':
      return 1;
    case 'SecondSemester':
      return 2;
  }
}
