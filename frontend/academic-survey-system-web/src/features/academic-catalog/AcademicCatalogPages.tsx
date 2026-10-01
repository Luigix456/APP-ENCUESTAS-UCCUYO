import { useEffect, useMemo, useState, type FormEvent } from 'react';
import {
  activateAcademicUnit,
  activateAcademicCycle,
  activateCareer,
  activateSubject,
  activateTeacher,
  activateTeacherSubjectAssignment,
  createAcademicUnit,
  createAcademicCycle,
  createCareer,
  createSubject,
  createTeacher,
  createTeacherSubjectAssignment,
  deactivateAcademicUnit,
  deactivateAcademicCycle,
  deactivateCareer,
  deactivateSubject,
  deactivateTeacher,
  deactivateTeacherSubjectAssignment,
  getAcademicUnits,
  getAcademicCycles,
  getCareers,
  getSubjects,
  getTeachers,
  getTeacherSubjectAssignments,
  updateAcademicUnit,
  updateAcademicCycle,
  updateCareer,
  updateSubject,
  updateTeacher,
  updateTeacherSubjectAssignment
} from '../../api/academicCatalogApi';
import { useAuth } from '../../auth/AuthProvider';
import type {
  AcademicCycleDto,
  AcademicCyclePeriod,
  AcademicUnitDto,
  CareerDto,
  CareerType,
  CreateAcademicUnitRequest,
  CreateAcademicCycleRequest,
  CreateCareerRequest,
  CreateSubjectRequest,
  CreateTeacherRequest,
  CreateTeacherSubjectAssignmentRequest,
  SubjectDto,
  SubjectPeriod,
  TeacherDto,
  TeacherSubjectAssignmentDto,
  UpdateAcademicUnitRequest,
  UpdateAcademicCycleRequest,
  UpdateCareerRequest,
  UpdateSubjectRequest,
  UpdateTeacherRequest,
  UpdateTeacherSubjectAssignmentRequest
} from '../../types/academicCatalog';
import {
  ACADEMIC_CYCLE_PERIODS,
  CAREER_TYPES,
  SUBJECT_PERIODS
} from '../../types/academicCatalog';
import { ActivityBadge } from '../surveys/surveyUi';
import {
  AcademicCatalogPermissionPanel,
  formatAcademicCycle,
  formatCareerType,
  formatDateOnly,
  formatPeriod,
  getFriendlyCatalogError,
  MANAGE_ACADEMIC_CATALOG_PERMISSION,
  nullIfBlank
} from './academicCatalogUi';

type LoadState = 'loading' | 'ready' | 'error';
type DependentLoadState = 'idle' | 'loading' | 'ready' | 'error';

interface CatalogRuntime {
  accessToken: string;
  onUnauthorized: () => void;
}

interface BasePageState {
  includeInactive: boolean;
  loadState: LoadState;
  pageError: string | null;
  actionError: string | null;
  successMessage: string | null;
  activeActionId: string | null;
}

const basePageState: BasePageState = {
  includeInactive: true,
  loadState: 'loading',
  pageError: null,
  actionError: null,
  successMessage: null,
  activeActionId: null
};

export function AcademicUnitsCatalogPage() {
  const auth = useAuth();
  const runtime = useCatalogRuntime(auth.accessToken, auth.logout);
  const canManageCatalog = auth.hasPermission(MANAGE_ACADEMIC_CATALOG_PERMISSION);
  const [page, setPage] = useState<BasePageState>(basePageState);
  const [academicUnits, setAcademicUnits] = useState<AcademicUnitDto[]>([]);
  const [createForm, setCreateForm] = useState<CreateAcademicUnitRequest>({ code: '', name: '' });
  const [editingId, setEditingId] = useState<string | null>(null);
  const [editForm, setEditForm] = useState<UpdateAcademicUnitRequest>({ name: '' });

  useEffect(() => {
    if (!runtime || !canManageCatalog) {
      setPage((current) => ({ ...current, loadState: 'ready' }));
      return;
    }

    const abortController = new AbortController();
    void loadAcademicUnits(runtime, page.includeInactive, abortController.signal);
    return () => abortController.abort();
  }, [canManageCatalog, page.includeInactive, runtime]);

  if (!canManageCatalog) {
    return <AcademicCatalogPermissionPanel />;
  }

  async function loadAcademicUnits(
    nextRuntime = runtime,
    includeInactive = page.includeInactive,
    signal?: AbortSignal
  ) {
    if (!nextRuntime) {
      return;
    }

    setPage((current) => ({ ...current, loadState: 'loading', pageError: null }));

    try {
      setAcademicUnits(await getAcademicUnits({ ...nextRuntime, includeInactive, signal }));
      setPage((current) => ({ ...current, loadState: 'ready' }));
    } catch (error) {
      if (signal?.aborted) {
        return;
      }

      setPage((current) => ({
        ...current,
        loadState: 'error',
        pageError: getFriendlyCatalogError(error, 'No fue posible cargar unidades académicas.')
      }));
    }
  }

  async function handleCreate(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    if (!runtime) {
      return;
    }

    await runPageAction(setPage, 'Unidad académica creada.', 'No fue posible crear la unidad académica.', async () => {
      await createAcademicUnit(
        { code: createForm.code.trim(), name: createForm.name.trim() },
        runtime
      );
      setCreateForm({ code: '', name: '' });
      await loadAcademicUnits(runtime);
    });
  }

  async function handleUpdate(event: FormEvent<HTMLFormElement>, academicUnit: AcademicUnitDto) {
    event.preventDefault();

    if (!runtime) {
      return;
    }

    await runPageAction(setPage, 'Unidad académica actualizada.', 'No fue posible actualizar la unidad académica.', async () => {
      await updateAcademicUnit(academicUnit.id, { name: editForm.name.trim() }, runtime);
      setEditingId(null);
      await loadAcademicUnits(runtime);
    });
  }

  async function handleToggle(academicUnit: AcademicUnitDto) {
    if (!runtime) {
      return;
    }

    if (academicUnit.isActive) {
      const confirmed = window.confirm(
        'Al desactivar esta unidad académica dejará de estar disponible para nuevas carreras. Los datos históricos se conservarán.'
      );

      if (!confirmed) {
        return;
      }
    }

    await runPageAction(
      setPage,
      academicUnit.isActive ? 'Unidad académica desactivada.' : 'Unidad académica reactivada.',
      academicUnit.isActive
        ? 'No fue posible desactivar la unidad académica.'
        : 'No fue posible reactivar la unidad académica.',
      async () => {
        setPage((current) => ({ ...current, activeActionId: academicUnit.id }));
        await (academicUnit.isActive
          ? deactivateAcademicUnit(academicUnit.id, runtime)
          : activateAcademicUnit(academicUnit.id, runtime));
        await loadAcademicUnits(runtime);
      }
    );
  }

  return (
    <CatalogPageShell
      description="Administrá facultades, departamentos o sedes que agrupan carreras."
      includeInactive={page.includeInactive}
      onIncludeInactiveChange={(includeInactive) => setPage((current) => ({ ...current, includeInactive }))}
      page={page}
      title="Unidades académicas"
    >
      <form className="survey-admin-form" onSubmit={handleCreate}>
        <h3>Nueva unidad académica</h3>
        <label>
          <span>Código</span>
          <input className="text-input" onChange={(event) => setCreateForm((current) => ({ ...current, code: event.target.value }))} required type="text" value={createForm.code} />
        </label>
        <label>
          <span>Nombre</span>
          <input className="text-input" onChange={(event) => setCreateForm((current) => ({ ...current, name: event.target.value }))} required type="text" value={createForm.name} />
        </label>
        <button className="primary-button" type="submit">
          Crear
        </button>
      </form>

      <div className="assignment-card-list">
        {academicUnits.map((academicUnit) => (
          <article className="survey-list-card" key={academicUnit.id}>
            <header>
              <div>
                <p className="eyebrow">{academicUnit.code}</p>
                <h3>{academicUnit.name}</h3>
              </div>
              <ActivityBadge isActive={academicUnit.isActive} />
            </header>
            {editingId === academicUnit.id ? (
              <form className="nested-form" onSubmit={(event) => void handleUpdate(event, academicUnit)}>
                <p className="inline-message">Código: {academicUnit.code}</p>
                <label>
                  <span>Nombre</span>
                  <input className="text-input" onChange={(event) => setEditForm({ name: event.target.value })} required type="text" value={editForm.name} />
                </label>
                <ActionButtons onCancel={() => setEditingId(null)} submitText="Guardar" />
              </form>
            ) : (
              <CatalogActions
                activeActionId={page.activeActionId}
                isActive={academicUnit.isActive}
                itemId={academicUnit.id}
                onEdit={() => {
                  setEditingId(academicUnit.id);
                  setEditForm({ name: academicUnit.name });
                }}
                onToggle={() => void handleToggle(academicUnit)}
              />
            )}
          </article>
        ))}
      </div>
    </CatalogPageShell>
  );
}

export function CareersCatalogPage() {
  const auth = useAuth();
  const runtime = useCatalogRuntime(auth.accessToken, auth.logout);
  const canManageCatalog = auth.hasPermission(MANAGE_ACADEMIC_CATALOG_PERMISSION);
  const [page, setPage] = useState<BasePageState>(basePageState);
  const [academicUnits, setAcademicUnits] = useState<AcademicUnitDto[]>([]);
  const [careers, setCareers] = useState<CareerDto[]>([]);
  const [createForm, setCreateForm] = useState<CreateCareerRequest>({
    academicUnitId: '',
    code: '',
    name: '',
    type: 'Undergraduate'
  });
  const [editingId, setEditingId] = useState<string | null>(null);
  const [editForm, setEditForm] = useState<UpdateCareerRequest>({ name: '', type: 'Undergraduate' });

  useEffect(() => {
    if (!runtime || !canManageCatalog) {
      setPage((current) => ({ ...current, loadState: 'ready' }));
      return;
    }

    const abortController = new AbortController();
    void loadCareers(runtime, page.includeInactive, abortController.signal);
    return () => abortController.abort();
  }, [canManageCatalog, page.includeInactive, runtime]);

  if (!canManageCatalog) {
    return <AcademicCatalogPermissionPanel />;
  }

  async function loadCareers(
    nextRuntime = runtime,
    includeInactive = page.includeInactive,
    signal?: AbortSignal
  ) {
    if (!nextRuntime) {
      return;
    }

    setPage((current) => ({ ...current, loadState: 'loading', pageError: null }));

    try {
      const [nextAcademicUnits, nextCareers] = await Promise.all([
        getAcademicUnits({ ...nextRuntime, includeInactive: false, signal }),
        getCareers({ ...nextRuntime, includeInactive, signal })
      ]);
      setAcademicUnits(nextAcademicUnits);
      setCareers(nextCareers);
      setPage((current) => ({ ...current, loadState: 'ready' }));
    } catch (error) {
      if (signal?.aborted) {
        return;
      }

      setPage((current) => ({
        ...current,
        loadState: 'error',
        pageError: getFriendlyCatalogError(error, 'No fue posible cargar carreras.')
      }));
    }
  }

  async function handleCreate(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    if (!runtime) {
      return;
    }

    await runPageAction(setPage, 'Nueva carrera creada.', 'No fue posible crear la carrera.', async () => {
      await createCareer(
        { ...createForm, code: createForm.code.trim(), name: createForm.name.trim() },
        runtime
      );
      setCreateForm({ academicUnitId: '', code: '', name: '', type: 'Undergraduate' });
      await loadCareers(runtime);
    });
  }

  async function handleUpdate(event: FormEvent<HTMLFormElement>, career: CareerDto) {
    event.preventDefault();

    if (!runtime) {
      return;
    }

    await runPageAction(setPage, 'Carrera actualizada.', 'No fue posible actualizar la carrera.', async () => {
      await updateCareer(career.id, { ...editForm, name: editForm.name.trim() }, runtime);
      setEditingId(null);
      await loadCareers(runtime);
    });
  }

  async function handleToggle(career: CareerDto) {
    if (!runtime) {
      return;
    }

    if (career.isActive) {
      const confirmed = window.confirm(
        'Al desactivar esta carrera dejará de estar disponible para nuevas operaciones académicas. Los datos históricos se conservarán.'
      );

      if (!confirmed) {
        return;
      }
    }

    await runPageAction(
      setPage,
      career.isActive ? 'Carrera desactivada.' : 'Carrera reactivada.',
      career.isActive ? 'No fue posible desactivar la carrera.' : 'No fue posible reactivar la carrera.',
      async () => {
        setPage((current) => ({ ...current, activeActionId: career.id }));
        await (career.isActive ? deactivateCareer(career.id, runtime) : activateCareer(career.id, runtime));
        await loadCareers(runtime);
      }
    );
  }

  return (
    <CatalogPageShell
      description="Administrá carreras, cursos o trayectos académicos."
      includeInactive={page.includeInactive}
      onIncludeInactiveChange={(includeInactive) => setPage((current) => ({ ...current, includeInactive }))}
      page={page}
      title="Carreras"
    >
      <form className="survey-admin-form" onSubmit={handleCreate}>
        <h3>Nueva carrera</h3>
        <label>
          <span>Unidad académica</span>
          <select
            className="text-input"
            onChange={(event) => setCreateForm((current) => ({ ...current, academicUnitId: event.target.value }))}
            required
            value={createForm.academicUnitId}
          >
            <option value="">Seleccionar...</option>
            {academicUnits.map((unit) => (
              <option key={unit.id} value={unit.id}>
                {unit.code} · {unit.name}
              </option>
            ))}
          </select>
        </label>
        <label>
          <span>Código</span>
          <input
            className="text-input"
            onChange={(event) => setCreateForm((current) => ({ ...current, code: event.target.value }))}
            required
            type="text"
            value={createForm.code}
          />
        </label>
        <label>
          <span>Nombre</span>
          <input
            className="text-input"
            onChange={(event) => setCreateForm((current) => ({ ...current, name: event.target.value }))}
            required
            type="text"
            value={createForm.name}
          />
        </label>
        <CareerTypeSelect
          value={createForm.type}
          onChange={(type) => setCreateForm((current) => ({ ...current, type }))}
        />
        <button className="primary-button" type="submit">
          Crear
        </button>
      </form>

      <div className="assignment-card-list">
        {careers.map((career) => (
          <article className="survey-list-card" key={career.id}>
            <header>
              <div>
                <p className="eyebrow">{career.code}</p>
                <h3>{career.name}</h3>
                <p>{formatCareerType(career.type)} · {career.academicUnitName ?? 'Sin unidad académica'}</p>
              </div>
              <ActivityBadge isActive={career.isActive} />
            </header>
            {editingId === career.id ? (
              <form className="nested-form" onSubmit={(event) => void handleUpdate(event, career)}>
                <p className="inline-message">Código: {career.code}</p>
                <p className="inline-message">Unidad académica: {career.academicUnitName ?? 'Sin unidad académica'}</p>
                <label>
                  <span>Nombre</span>
                  <input
                    className="text-input"
                    onChange={(event) => setEditForm((current) => ({ ...current, name: event.target.value }))}
                    required
                    type="text"
                    value={editForm.name}
                  />
                </label>
                <CareerTypeSelect value={editForm.type} onChange={(type) => setEditForm((current) => ({ ...current, type }))} />
                <ActionButtons onCancel={() => setEditingId(null)} submitText="Guardar" />
              </form>
            ) : (
              <CatalogActions
                activeActionId={page.activeActionId}
                isActive={career.isActive}
                itemId={career.id}
                onEdit={() => {
                  setEditingId(career.id);
                  setEditForm({ name: career.name, type: career.type });
                }}
                onToggle={() => void handleToggle(career)}
              />
            )}
          </article>
        ))}
      </div>
    </CatalogPageShell>
  );
}

export function SubjectsCatalogPage() {
  const auth = useAuth();
  const runtime = useCatalogRuntime(auth.accessToken, auth.logout);
  const canManageCatalog = auth.hasPermission(MANAGE_ACADEMIC_CATALOG_PERMISSION);
  const [page, setPage] = useState<BasePageState>(basePageState);
  const [careers, setCareers] = useState<CareerDto[]>([]);
  const [subjects, setSubjects] = useState<SubjectDto[]>([]);
  const [filterCareerId, setFilterCareerId] = useState('');
  const [createForm, setCreateForm] = useState<CreateSubjectRequest>({
    careerId: '',
    code: '',
    name: '',
    year: 1,
    period: 'Annual'
  });
  const [editingId, setEditingId] = useState<string | null>(null);
  const [editForm, setEditForm] = useState<UpdateSubjectRequest>({
    name: '',
    year: 1,
    period: 'Annual'
  });

  useEffect(() => {
    if (!runtime || !canManageCatalog) {
      setPage((current) => ({ ...current, loadState: 'ready' }));
      return;
    }

    const abortController = new AbortController();
    void loadSubjects(runtime, page.includeInactive, filterCareerId, abortController.signal);
    return () => abortController.abort();
  }, [canManageCatalog, filterCareerId, page.includeInactive, runtime]);

  if (!canManageCatalog) {
    return <AcademicCatalogPermissionPanel />;
  }

  async function loadSubjects(
    nextRuntime = runtime,
    includeInactive = page.includeInactive,
    careerId = filterCareerId,
    signal?: AbortSignal
  ) {
    if (!nextRuntime) {
      return;
    }

    setPage((current) => ({ ...current, loadState: 'loading', pageError: null }));

    try {
      const [nextCareers, nextSubjects] = await Promise.all([
        getCareers({ ...nextRuntime, includeInactive, signal }),
        getSubjects(careerId, { ...nextRuntime, includeInactive, signal })
      ]);

      setCareers(nextCareers);
      setSubjects(nextSubjects);
      setPage((current) => ({ ...current, loadState: 'ready' }));
    } catch (error) {
      if (signal?.aborted) {
        return;
      }

      setPage((current) => ({
        ...current,
        loadState: 'error',
        pageError: getFriendlyCatalogError(error, 'No fue posible cargar materias.')
      }));
    }
  }

  async function handleCreate(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    if (!runtime) {
      return;
    }

    await runPageAction(setPage, 'Materia creada.', 'No fue posible crear la materia.', async () => {
      await createSubject(
        {
          ...createForm,
          code: createForm.code.trim(),
          name: createForm.name.trim()
        },
        runtime
      );
      setCreateForm({ careerId: '', code: '', name: '', year: 1, period: 'Annual' });
      await loadSubjects(runtime);
    });
  }

  async function handleUpdate(event: FormEvent<HTMLFormElement>, subject: SubjectDto) {
    event.preventDefault();

    if (!runtime) {
      return;
    }

    await runPageAction(setPage, 'Materia actualizada.', 'No fue posible actualizar la materia.', async () => {
      await updateSubject(subject.id, { ...editForm, name: editForm.name.trim() }, runtime);
      setEditingId(null);
      await loadSubjects(runtime);
    });
  }

  async function handleToggle(subject: SubjectDto) {
    if (!runtime) {
      return;
    }

    await runPageAction(
      setPage,
      subject.isActive ? 'Materia desactivada.' : 'Materia reactivada.',
      subject.isActive ? 'No fue posible desactivar la materia.' : 'No fue posible reactivar la materia.',
      async () => {
        setPage((current) => ({ ...current, activeActionId: subject.id }));
        await (subject.isActive ? deactivateSubject(subject.id, runtime) : activateSubject(subject.id, runtime));
        await loadSubjects(runtime);
      }
    );
  }

  return (
    <CatalogPageShell
      description="Administrá materias vinculadas a carreras activas."
      includeInactive={page.includeInactive}
      onIncludeInactiveChange={(includeInactive) => {
        if (!includeInactive && filterCareerId) {
          const selectedCareer = careers.find((career) => career.id === filterCareerId);

          if (selectedCareer && !selectedCareer.isActive) {
            setFilterCareerId('');
          }
        }

        setPage((current) => ({ ...current, includeInactive }));
      }}
      page={page}
      title="Materias"
    >
      <div className="surveys-filters">
        <label>
          <span>Filtrar por carrera</span>
          <select className="text-input" onChange={(event) => setFilterCareerId(event.target.value)} value={filterCareerId}>
            <option value="">Todas</option>
            {careers.map((career) => (
              <option key={career.id} value={career.id}>
                {career.name}
              </option>
            ))}
          </select>
        </label>
      </div>

      <form className="survey-admin-form" onSubmit={handleCreate}>
        <h3>Nueva materia</h3>
        <label>
          <span>Carrera</span>
          <select
            className="text-input"
            onChange={(event) => setCreateForm((current) => ({ ...current, careerId: event.target.value }))}
            required
            value={createForm.careerId}
          >
            <option value="">Seleccionar...</option>
            {careers.filter((career) => career.isActive).map((career) => (
              <option key={career.id} value={career.id}>
                {career.name}
              </option>
            ))}
          </select>
        </label>
        <label>
          <span>Código</span>
          <input className="text-input" onChange={(event) => setCreateForm((current) => ({ ...current, code: event.target.value }))} required type="text" value={createForm.code} />
        </label>
        <SubjectMutableFields form={createForm} onChange={setCreateForm} />
        <button className="primary-button" type="submit">
          Crear
        </button>
      </form>

      <div className="assignment-card-list">
        {subjects.map((subject) => (
          <article className="survey-list-card" key={subject.id}>
            <header>
              <div>
                <p className="eyebrow">{subject.careerName}</p>
                <h3>{subject.name}</h3>
                <p>
                  {subject.code} · {subject.year}° · {formatPeriod(subject.period)}
                </p>
              </div>
              <ActivityBadge isActive={subject.isActive} />
            </header>
            {editingId === subject.id ? (
              <form className="nested-form" onSubmit={(event) => void handleUpdate(event, subject)}>
                <p className="inline-message">
                  Carrera: {subject.careerName} · Código: {subject.code}
                </p>
                <SubjectMutableFields form={editForm} onChange={setEditForm} />
                <ActionButtons onCancel={() => setEditingId(null)} submitText="Guardar" />
              </form>
            ) : (
              <CatalogActions
                activeActionId={page.activeActionId}
                isActive={subject.isActive}
                itemId={subject.id}
                onEdit={() => {
                  setEditingId(subject.id);
                  setEditForm({ name: subject.name, year: subject.year, period: subject.period });
                }}
                onToggle={() => void handleToggle(subject)}
              />
            )}
          </article>
        ))}
      </div>
    </CatalogPageShell>
  );
}

export function AcademicCyclesCatalogPage() {
  const auth = useAuth();
  const runtime = useCatalogRuntime(auth.accessToken, auth.logout);
  const canManageCatalog = auth.hasPermission(MANAGE_ACADEMIC_CATALOG_PERMISSION);
  const [page, setPage] = useState<BasePageState>(basePageState);
  const [cycles, setCycles] = useState<AcademicCycleDto[]>([]);
  const [createForm, setCreateForm] = useState<CreateAcademicCycleRequest>({
    year: new Date().getFullYear(),
    period: 'Annual',
    startDate: '',
    endDate: ''
  });
  const [editingId, setEditingId] = useState<string | null>(null);
  const [editForm, setEditForm] = useState<UpdateAcademicCycleRequest>({
    period: 'Annual',
    startDate: '',
    endDate: ''
  });

  useEffect(() => {
    if (!runtime || !canManageCatalog) {
      setPage((current) => ({ ...current, loadState: 'ready' }));
      return;
    }

    const abortController = new AbortController();
    void loadCycles(runtime, page.includeInactive, abortController.signal);
    return () => abortController.abort();
  }, [canManageCatalog, page.includeInactive, runtime]);

  if (!canManageCatalog) {
    return <AcademicCatalogPermissionPanel />;
  }

  async function loadCycles(
    nextRuntime = runtime,
    includeInactive = page.includeInactive,
    signal?: AbortSignal
  ) {
    if (!nextRuntime) {
      return;
    }

    setPage((current) => ({ ...current, loadState: 'loading', pageError: null }));

    try {
      setCycles(await getAcademicCycles({ ...nextRuntime, includeInactive, signal }));
      setPage((current) => ({ ...current, loadState: 'ready' }));
    } catch (error) {
      if (signal?.aborted) {
        return;
      }

      setPage((current) => ({
        ...current,
        loadState: 'error',
        pageError: getFriendlyCatalogError(error, 'No fue posible cargar ciclos lectivos.')
      }));
    }
  }

  async function handleCreate(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    if (!runtime || !validateDateRange(createForm.startDate, createForm.endDate, setPage)) {
      return;
    }

    await runPageAction(setPage, 'Ciclo lectivo creado.', 'No fue posible crear el ciclo lectivo.', async () => {
      await createAcademicCycle(createForm, runtime);
      setCreateForm({ year: new Date().getFullYear(), period: 'Annual', startDate: '', endDate: '' });
      await loadCycles(runtime);
    });
  }

  async function handleUpdate(event: FormEvent<HTMLFormElement>, cycle: AcademicCycleDto) {
    event.preventDefault();

    if (!runtime || !validateDateRange(editForm.startDate, editForm.endDate, setPage)) {
      return;
    }

    await runPageAction(setPage, 'Ciclo lectivo actualizado.', 'No fue posible actualizar el ciclo lectivo.', async () => {
      await updateAcademicCycle(cycle.id, editForm, runtime);
      setEditingId(null);
      await loadCycles(runtime);
    });
  }

  async function handleToggle(cycle: AcademicCycleDto) {
    if (!runtime) {
      return;
    }

    await runPageAction(
      setPage,
      cycle.isActive ? 'Ciclo lectivo desactivado.' : 'Ciclo lectivo reactivado.',
      cycle.isActive ? 'No fue posible desactivar el ciclo lectivo.' : 'No fue posible reactivar el ciclo lectivo.',
      async () => {
        setPage((current) => ({ ...current, activeActionId: cycle.id }));
        await (cycle.isActive ? deactivateAcademicCycle(cycle.id, runtime) : activateAcademicCycle(cycle.id, runtime));
        await loadCycles(runtime);
      }
    );
  }

  return (
    <CatalogPageShell
      description="Administrá ciclos lectivos y sus fechas de vigencia."
      includeInactive={page.includeInactive}
      onIncludeInactiveChange={(includeInactive) => setPage((current) => ({ ...current, includeInactive }))}
      page={page}
      title="Ciclos lectivos"
    >
      <form className="survey-admin-form" onSubmit={handleCreate}>
        <h3>Nuevo ciclo</h3>
        <label>
          <span>Año</span>
          <input className="text-input" max={2100} min={2000} onChange={(event) => setCreateForm((current) => ({ ...current, year: Number(event.target.value) }))} required type="number" value={createForm.year} />
        </label>
        <CycleMutableFields form={createForm} onChange={setCreateForm} />
        <button className="primary-button" type="submit">
          Crear
        </button>
      </form>

      <div className="assignment-card-list">
        {cycles.map((cycle) => (
          <article className="survey-list-card" key={cycle.id}>
            <header>
              <div>
                <p className="eyebrow">{cycle.year}</p>
                <h3>{formatPeriod(cycle.period)}</h3>
                <p>
                  {formatDateOnly(cycle.startDate)} · {formatDateOnly(cycle.endDate)}
                </p>
              </div>
              <ActivityBadge isActive={cycle.isActive} />
            </header>
            {editingId === cycle.id ? (
              <form className="nested-form" onSubmit={(event) => void handleUpdate(event, cycle)}>
                <p className="inline-message">Año: {cycle.year}</p>
                <CycleMutableFields form={editForm} onChange={setEditForm} />
                <ActionButtons onCancel={() => setEditingId(null)} submitText="Guardar" />
              </form>
            ) : (
              <CatalogActions
                activeActionId={page.activeActionId}
                isActive={cycle.isActive}
                itemId={cycle.id}
                onEdit={() => {
                  setEditingId(cycle.id);
                  setEditForm({ period: cycle.period, startDate: cycle.startDate, endDate: cycle.endDate });
                }}
                onToggle={() => void handleToggle(cycle)}
              />
            )}
          </article>
        ))}
      </div>
    </CatalogPageShell>
  );
}

export function TeachersCatalogPage() {
  const auth = useAuth();
  const runtime = useCatalogRuntime(auth.accessToken, auth.logout);
  const canManageCatalog = auth.hasPermission(MANAGE_ACADEMIC_CATALOG_PERMISSION);
  const [page, setPage] = useState<BasePageState>(basePageState);
  const [teachers, setTeachers] = useState<TeacherDto[]>([]);
  const [createForm, setCreateForm] = useState<CreateTeacherRequest>({
    firstName: '',
    lastName: '',
    email: null
  });
  const [editingId, setEditingId] = useState<string | null>(null);
  const [editForm, setEditForm] = useState<UpdateTeacherRequest>({
    firstName: '',
    lastName: '',
    email: null
  });

  useEffect(() => {
    if (!runtime || !canManageCatalog) {
      setPage((current) => ({ ...current, loadState: 'ready' }));
      return;
    }

    const abortController = new AbortController();
    void loadTeachers(runtime, page.includeInactive, abortController.signal);
    return () => abortController.abort();
  }, [canManageCatalog, page.includeInactive, runtime]);

  if (!canManageCatalog) {
    return <AcademicCatalogPermissionPanel />;
  }

  async function loadTeachers(
    nextRuntime = runtime,
    includeInactive = page.includeInactive,
    signal?: AbortSignal
  ) {
    if (!nextRuntime) {
      return;
    }

    setPage((current) => ({ ...current, loadState: 'loading', pageError: null }));

    try {
      setTeachers(await getTeachers({ ...nextRuntime, includeInactive, signal }));
      setPage((current) => ({ ...current, loadState: 'ready' }));
    } catch (error) {
      if (signal?.aborted) {
        return;
      }

      setPage((current) => ({
        ...current,
        loadState: 'error',
        pageError: getFriendlyCatalogError(error, 'No fue posible cargar docentes.')
      }));
    }
  }

  async function handleCreate(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    if (!runtime) {
      return;
    }

    await runPageAction(setPage, 'Docente creado.', 'No fue posible crear el docente.', async () => {
      await createTeacher(normalizeTeacherRequest(createForm), runtime);
      setCreateForm({ firstName: '', lastName: '', email: null });
      await loadTeachers(runtime);
    });
  }

  async function handleUpdate(event: FormEvent<HTMLFormElement>, teacher: TeacherDto) {
    event.preventDefault();

    if (!runtime) {
      return;
    }

    await runPageAction(setPage, 'Docente actualizado.', 'No fue posible actualizar el docente.', async () => {
      await updateTeacher(teacher.id, normalizeTeacherRequest(editForm), runtime);
      setEditingId(null);
      await loadTeachers(runtime);
    });
  }

  async function handleToggle(teacher: TeacherDto) {
    if (!runtime) {
      return;
    }

    await runPageAction(
      setPage,
      teacher.isActive ? 'Docente desactivado.' : 'Docente reactivado.',
      teacher.isActive ? 'No fue posible desactivar el docente.' : 'No fue posible reactivar el docente.',
      async () => {
        setPage((current) => ({ ...current, activeActionId: teacher.id }));
        await (teacher.isActive ? deactivateTeacher(teacher.id, runtime) : activateTeacher(teacher.id, runtime));
        await loadTeachers(runtime);
      }
    );
  }

  return (
    <CatalogPageShell
      description="Administrá docentes del catálogo académico."
      includeInactive={page.includeInactive}
      onIncludeInactiveChange={(includeInactive) => setPage((current) => ({ ...current, includeInactive }))}
      page={page}
      title="Docentes"
    >
      <form className="survey-admin-form" onSubmit={handleCreate}>
        <h3>Nuevo docente</h3>
        <TeacherFields form={createForm} onChange={setCreateForm} />
        <button className="primary-button" type="submit">
          Crear
        </button>
      </form>

      <div className="assignment-card-list">
        {teachers.map((teacher) => (
          <article className="survey-list-card" key={teacher.id}>
            <header>
              <div>
                <p className="eyebrow">{teacher.lastName}</p>
                <h3>
                  {teacher.lastName}, {teacher.firstName}
                </h3>
                <p>{teacher.email ?? 'Sin email'}</p>
              </div>
              <ActivityBadge isActive={teacher.isActive} />
            </header>
            {editingId === teacher.id ? (
              <form className="nested-form" onSubmit={(event) => void handleUpdate(event, teacher)}>
                <TeacherFields form={editForm} onChange={setEditForm} />
                <ActionButtons onCancel={() => setEditingId(null)} submitText="Guardar" />
              </form>
            ) : (
              <CatalogActions
                activeActionId={page.activeActionId}
                isActive={teacher.isActive}
                itemId={teacher.id}
                onEdit={() => {
                  setEditingId(teacher.id);
                  setEditForm({
                    firstName: teacher.firstName,
                    lastName: teacher.lastName,
                    email: teacher.email
                  });
                }}
                onToggle={() => void handleToggle(teacher)}
              />
            )}
          </article>
        ))}
      </div>
    </CatalogPageShell>
  );
}

export function TeacherAssignmentsCatalogPage() {
  const auth = useAuth();
  const runtime = useCatalogRuntime(auth.accessToken, auth.logout);
  const canManageCatalog = auth.hasPermission(MANAGE_ACADEMIC_CATALOG_PERMISSION);
  const [page, setPage] = useState<BasePageState>(basePageState);
  const [careers, setCareers] = useState<CareerDto[]>([]);
  const [createSubjects, setCreateSubjects] = useState<SubjectDto[]>([]);
  const [filterSubjects, setFilterSubjects] = useState<SubjectDto[]>([]);
  const [cycles, setCycles] = useState<AcademicCycleDto[]>([]);
  const [filterCycles, setFilterCycles] = useState<AcademicCycleDto[]>([]);
  const [teachers, setTeachers] = useState<TeacherDto[]>([]);
  const [filterTeachers, setFilterTeachers] = useState<TeacherDto[]>([]);
  const [assignments, setAssignments] = useState<TeacherSubjectAssignmentDto[]>([]);
  const [createCareerId, setCreateCareerId] = useState('');
  const [subjectsState, setSubjectsState] = useState<DependentLoadState>('idle');
  const [createForm, setCreateForm] = useState<CreateTeacherSubjectAssignmentRequest>({
    teacherId: '',
    subjectId: '',
    academicCycleId: '',
    teachingRole: ''
  });
  const [filters, setFilters] = useState({ teacherId: '', subjectId: '', academicCycleId: '' });
  const [editingId, setEditingId] = useState<string | null>(null);
  const [editForm, setEditForm] = useState<UpdateTeacherSubjectAssignmentRequest>({ teachingRole: '' });

  useEffect(() => {
    if (!runtime || !canManageCatalog) {
      setPage((current) => ({ ...current, loadState: 'ready' }));
      return;
    }

    const abortController = new AbortController();
    void loadTeacherAssignments(runtime, page.includeInactive, filters, abortController.signal);
    return () => abortController.abort();
  }, [canManageCatalog, filters, page.includeInactive, runtime]);

  useEffect(() => {
    if (!runtime || !createCareerId || !canManageCatalog) {
      setCreateSubjects([]);
      setSubjectsState('idle');
      return;
    }

    const abortController = new AbortController();
    setCreateSubjects([]);
    setSubjectsState('loading');
    setPage((current) => ({ ...current, actionError: null }));

    getSubjects(createCareerId, { ...runtime, includeInactive: false, signal: abortController.signal })
      .then((nextSubjects) => {
        setCreateSubjects(nextSubjects);
        setSubjectsState('ready');
      })
      .catch((error) => {
        if (!abortController.signal.aborted) {
          setPage((current) => ({
            ...current,
            actionError: getFriendlyCatalogError(error, 'No fue posible cargar materias.')
          }));
          setSubjectsState('error');
        }
      });

    return () => abortController.abort();
  }, [canManageCatalog, createCareerId, runtime]);

  if (!canManageCatalog) {
    return <AcademicCatalogPermissionPanel />;
  }

  async function loadTeacherAssignments(
    nextRuntime = runtime,
    includeInactive = page.includeInactive,
    nextFilters = filters,
    signal?: AbortSignal
  ) {
    if (!nextRuntime) {
      return;
    }

    setPage((current) => ({ ...current, loadState: 'loading', pageError: null }));

    try {
      const [nextCareers, allCycles, allTeachers, allSubjects, nextAssignments] = await Promise.all([
        getCareers({ ...nextRuntime, includeInactive: false, signal }),
        getAcademicCycles({ ...nextRuntime, includeInactive: true, signal }),
        getTeachers({ ...nextRuntime, includeInactive: true, signal }),
        getSubjects('', { ...nextRuntime, includeInactive: true, signal }),
        getTeacherSubjectAssignments(
          { includeInactive, ...nextFilters },
          { ...nextRuntime, signal }
        )
      ]);

      setCareers(nextCareers);
      setCycles(allCycles.filter((cycle) => cycle.isActive));
      setFilterCycles(allCycles);
      setTeachers(allTeachers.filter((teacher) => teacher.isActive));
      setFilterTeachers(allTeachers);
      setFilterSubjects(allSubjects);
      setAssignments(nextAssignments);
      setPage((current) => ({ ...current, loadState: 'ready' }));
    } catch (error) {
      if (signal?.aborted) {
        return;
      }

      setPage((current) => ({
        ...current,
        loadState: 'error',
        pageError: getFriendlyCatalogError(error, 'No fue posible cargar asignaciones docentes.')
      }));
    }
  }

  async function handleCreate(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    if (!runtime) {
      return;
    }

    await runPageAction(setPage, 'Asignación docente creada.', 'No fue posible crear la asignación docente.', async () => {
      await createTeacherSubjectAssignment(
        { ...createForm, teachingRole: createForm.teachingRole.trim() },
        runtime
      );
      setCreateCareerId('');
      setCreateForm({ teacherId: '', subjectId: '', academicCycleId: '', teachingRole: '' });
      await loadTeacherAssignments(runtime);
    });
  }

  async function handleUpdate(event: FormEvent<HTMLFormElement>, assignment: TeacherSubjectAssignmentDto) {
    event.preventDefault();

    if (!runtime) {
      return;
    }

    await runPageAction(setPage, 'Asignación docente actualizada.', 'No fue posible actualizar la asignación docente.', async () => {
      await updateTeacherSubjectAssignment(
        assignment.id,
        { teachingRole: editForm.teachingRole.trim() },
        runtime
      );
      setEditingId(null);
      await loadTeacherAssignments(runtime);
    });
  }

  async function handleToggle(assignment: TeacherSubjectAssignmentDto) {
    if (!runtime) {
      return;
    }

    await runPageAction(
      setPage,
      assignment.isActive ? 'Asignación docente desactivada.' : 'Asignación docente reactivada.',
      assignment.isActive
        ? 'No fue posible desactivar la asignación docente.'
        : 'No fue posible reactivar la asignación docente.',
      async () => {
        setPage((current) => ({ ...current, activeActionId: assignment.id }));
        await (assignment.isActive
          ? deactivateTeacherSubjectAssignment(assignment.id, runtime)
          : activateTeacherSubjectAssignment(assignment.id, runtime));
        await loadTeacherAssignments(runtime);
      }
    );
  }

  return (
    <CatalogPageShell
      description="Administrá la relación entre docente, materia, ciclo lectivo y rol."
      includeInactive={page.includeInactive}
      onIncludeInactiveChange={(includeInactive) => setPage((current) => ({ ...current, includeInactive }))}
      page={page}
      title="Asignaciones docentes"
    >
      <div className="surveys-filters">
        <label>
          <span>Docente</span>
          <select className="text-input" onChange={(event) => setFilters((current) => ({ ...current, teacherId: event.target.value }))} value={filters.teacherId}>
            <option value="">Todos</option>
            {filterTeachers.map((teacher) => (
              <option key={teacher.id} value={teacher.id}>
                {teacher.lastName}, {teacher.firstName}
              </option>
            ))}
          </select>
        </label>
        <label>
          <span>Materia</span>
          <select className="text-input" onChange={(event) => setFilters((current) => ({ ...current, subjectId: event.target.value }))} value={filters.subjectId}>
            <option value="">Todas</option>
            {filterSubjects.map((subject) => (
              <option key={subject.id} value={subject.id}>
                {subject.careerName} · {subject.name}
              </option>
            ))}
          </select>
        </label>
        <label>
          <span>Ciclo</span>
          <select className="text-input" onChange={(event) => setFilters((current) => ({ ...current, academicCycleId: event.target.value }))} value={filters.academicCycleId}>
            <option value="">Todos</option>
            {filterCycles.map((cycle) => (
              <option key={cycle.id} value={cycle.id}>
                {formatAcademicCycle(cycle)}
              </option>
            ))}
          </select>
        </label>
      </div>

      <form className="survey-admin-form" onSubmit={handleCreate}>
        <h3>Nueva asignación docente</h3>
        <label>
          <span>Carrera</span>
          <select
            className="text-input"
            onChange={(event) => {
              setCreateCareerId(event.target.value);
              setCreateForm((current) => ({ ...current, subjectId: '' }));
            }}
            required
            value={createCareerId}
          >
            <option value="">Seleccionar...</option>
            {careers.map((career) => (
              <option key={career.id} value={career.id}>
                {career.name}
              </option>
            ))}
          </select>
        </label>
        <label>
          <span>Materia</span>
          <select
            className="text-input"
            disabled={!createCareerId || subjectsState === 'loading'}
            onChange={(event) => setCreateForm((current) => ({ ...current, subjectId: event.target.value }))}
            required
            value={createForm.subjectId}
          >
            <option value="">Seleccionar...</option>
            {createSubjects.map((subject) => (
              <option key={subject.id} value={subject.id}>
                {subject.name}
              </option>
            ))}
          </select>
          <small>
            {getDependentSubjectsHelpText(createCareerId, subjectsState, createSubjects)}
          </small>
        </label>
        <label>
          <span>Ciclo</span>
          <select className="text-input" onChange={(event) => setCreateForm((current) => ({ ...current, academicCycleId: event.target.value }))} required value={createForm.academicCycleId}>
            <option value="">Seleccionar...</option>
            {cycles.map((cycle) => (
              <option key={cycle.id} value={cycle.id}>
                {formatAcademicCycle(cycle)}
              </option>
            ))}
          </select>
        </label>
        <label>
          <span>Docente</span>
          <select className="text-input" onChange={(event) => setCreateForm((current) => ({ ...current, teacherId: event.target.value }))} required value={createForm.teacherId}>
            <option value="">Seleccionar...</option>
            {teachers.map((teacher) => (
              <option key={teacher.id} value={teacher.id}>
                {teacher.lastName}, {teacher.firstName}
              </option>
            ))}
          </select>
        </label>
        <TeachingRoleField form={createForm} onChange={setCreateForm} />
        <button className="primary-button" type="submit">
          Crear
        </button>
      </form>

      <div className="assignment-card-list">
        {assignments.map((assignment) => (
          <article className="survey-list-card" key={assignment.id}>
            <header>
              <div>
                <p className="eyebrow">{assignment.careerName}</p>
                <h3>{assignment.teacherFullName}</h3>
                <p>
                  {assignment.subjectName} · {assignment.academicCycleYear} · {formatPeriod(assignment.academicCyclePeriod)}
                </p>
                <p>Rol: {assignment.teachingRole}</p>
              </div>
              <ActivityBadge isActive={assignment.isActive} />
            </header>
            {editingId === assignment.id ? (
              <form className="nested-form" onSubmit={(event) => void handleUpdate(event, assignment)}>
                <p className="inline-message">
                  Docente: {assignment.teacherFullName} · Materia: {assignment.subjectName} · Ciclo: {assignment.academicCycleYear} {formatPeriod(assignment.academicCyclePeriod)}
                </p>
                <TeachingRoleField form={editForm} onChange={setEditForm} />
                <ActionButtons onCancel={() => setEditingId(null)} submitText="Guardar" />
              </form>
            ) : (
              <CatalogActions
                activeActionId={page.activeActionId}
                isActive={assignment.isActive}
                itemId={assignment.id}
                onEdit={() => {
                  setEditingId(assignment.id);
                  setEditForm({ teachingRole: assignment.teachingRole });
                }}
                onToggle={() => void handleToggle(assignment)}
              />
            )}
          </article>
        ))}
      </div>
    </CatalogPageShell>
  );
}

function CatalogPageShell({
  children,
  description,
  includeInactive,
  onIncludeInactiveChange,
  page,
  title
}: {
  children: React.ReactNode;
  description: string;
  includeInactive: boolean;
  onIncludeInactiveChange: (includeInactive: boolean) => void;
  page: BasePageState;
  title: string;
}) {
  return (
    <section className="app-content academic-page">
      <header className="surveys-header">
        <div>
          <p className="eyebrow">Catálogo académico</p>
          <h2>{title}</h2>
          <p>{description}</p>
        </div>
        <label className="checkbox-field">
          <input checked={includeInactive} onChange={(event) => onIncludeInactiveChange(event.target.checked)} type="checkbox" />
          <span>Incluir inactivos</span>
        </label>
      </header>

      {page.successMessage ? <div className="success-message" role="status">{page.successMessage}</div> : null}
      {page.actionError ? <p className="submit-error" role="alert">{page.actionError}</p> : null}
      {page.loadState === 'loading' ? <p aria-live="polite">Cargando...</p> : null}
      {page.loadState === 'error' ? (
        <div className="empty-detail" role="alert">
          <h3>No pudimos cargar este módulo</h3>
          <p>{page.pageError}</p>
        </div>
      ) : null}
      {children}
    </section>
  );
}

function CatalogActions({
  activeActionId,
  isActive,
  itemId,
  onEdit,
  onToggle
}: {
  activeActionId: string | null;
  isActive: boolean;
  itemId: string;
  onEdit: () => void;
  onToggle: () => void;
}) {
  return (
    <div className="survey-card-actions">
      <button className="secondary-button" onClick={onEdit} type="button">
        Editar
      </button>
      <button
        className={isActive ? 'danger-button' : 'secondary-button'}
        disabled={activeActionId === itemId}
        onClick={onToggle}
        type="button"
      >
        {activeActionId === itemId ? 'Procesando...' : isActive ? 'Desactivar' : 'Reactivar'}
      </button>
    </div>
  );
}

function ActionButtons({ onCancel, submitText }: { onCancel: () => void; submitText: string }) {
  return (
    <div className="form-actions">
      <button className="secondary-button" type="submit">
        {submitText}
      </button>
      <button className="secondary-button" onClick={onCancel} type="button">
        Cancelar
      </button>
    </div>
  );
}

function CareerTypeSelect({ onChange, value }: { onChange: (value: CareerType) => void; value: CareerType }) {
  return (
    <label>
      <span>Tipo</span>
      <select className="text-input" onChange={(event) => onChange(event.target.value as CareerType)} value={value}>
        {CAREER_TYPES.map((type) => (
          <option key={type} value={type}>
            {formatCareerType(type)}
          </option>
        ))}
      </select>
    </label>
  );
}

function SubjectMutableFields<T extends UpdateSubjectRequest>({
  form,
  onChange
}: {
  form: T;
  onChange: (form: T) => void;
}) {
  return (
    <>
      <label>
        <span>Nombre</span>
        <input className="text-input" onChange={(event) => onChange({ ...form, name: event.target.value })} required type="text" value={form.name} />
      </label>
      <label>
        <span>Año</span>
        <input className="text-input" max={10} min={1} onChange={(event) => onChange({ ...form, year: Number(event.target.value) })} required type="number" value={form.year} />
      </label>
      <label>
        <span>Período</span>
        <select className="text-input" onChange={(event) => onChange({ ...form, period: event.target.value as SubjectPeriod })} value={form.period}>
          {SUBJECT_PERIODS.map((period) => (
            <option key={period} value={period}>
              {formatPeriod(period)}
            </option>
          ))}
        </select>
      </label>
    </>
  );
}

function CycleMutableFields<T extends UpdateAcademicCycleRequest>({
  form,
  onChange
}: {
  form: T;
  onChange: (form: T) => void;
}) {
  return (
    <>
      <label>
        <span>Período</span>
        <select className="text-input" onChange={(event) => onChange({ ...form, period: event.target.value as AcademicCyclePeriod })} value={form.period}>
          {ACADEMIC_CYCLE_PERIODS.map((period) => (
            <option key={period} value={period}>
              {formatPeriod(period)}
            </option>
          ))}
        </select>
      </label>
      <label>
        <span>Fecha inicio</span>
        <input className="text-input" onChange={(event) => onChange({ ...form, startDate: event.target.value })} required type="date" value={form.startDate} />
      </label>
      <label>
        <span>Fecha fin</span>
        <input className="text-input" onChange={(event) => onChange({ ...form, endDate: event.target.value })} required type="date" value={form.endDate} />
      </label>
    </>
  );
}

function TeacherFields<T extends CreateTeacherRequest>({
  form,
  onChange
}: {
  form: T;
  onChange: (form: T) => void;
}) {
  return (
    <>
      <label>
        <span>Nombre</span>
        <input className="text-input" onChange={(event) => onChange({ ...form, firstName: event.target.value })} required type="text" value={form.firstName} />
      </label>
      <label>
        <span>Apellido</span>
        <input className="text-input" onChange={(event) => onChange({ ...form, lastName: event.target.value })} required type="text" value={form.lastName} />
      </label>
      <label>
        <span>Email</span>
        <input className="text-input" onChange={(event) => onChange({ ...form, email: event.target.value })} type="email" value={form.email ?? ''} />
      </label>
    </>
  );
}

function TeachingRoleField<T extends UpdateTeacherSubjectAssignmentRequest>({
  form,
  onChange
}: {
  form: T;
  onChange: (form: T) => void;
}) {
  return (
    <label>
      <span>Rol docente</span>
      <input className="text-input" onChange={(event) => onChange({ ...form, teachingRole: event.target.value })} required type="text" value={form.teachingRole} />
    </label>
  );
}

function useCatalogRuntime(accessToken: string | null, logout: () => void): CatalogRuntime | null {
  return useMemo(
    () => (accessToken ? { accessToken, onUnauthorized: logout } : null),
    [accessToken, logout]
  );
}

async function runPageAction(
  setPage: React.Dispatch<React.SetStateAction<BasePageState>>,
  success: string,
  fallback: string,
  action: () => Promise<void>
) {
  setPage((current) => ({ ...current, actionError: null, successMessage: null }));

  try {
    await action();
    setPage((current) => ({ ...current, activeActionId: null, successMessage: success }));
  } catch (error) {
    setPage((current) => ({
      ...current,
      activeActionId: null,
      actionError: getFriendlyCatalogError(error, fallback)
    }));
  }
}

function normalizeTeacherRequest<T extends CreateTeacherRequest>(form: T): T {
  return {
    ...form,
    firstName: form.firstName.trim(),
    lastName: form.lastName.trim(),
    email: nullIfBlank(form.email ?? '')
  };
}

function validateDateRange(
  startDate: string,
  endDate: string,
  setPage: React.Dispatch<React.SetStateAction<BasePageState>>
): boolean {
  if (!startDate || !endDate) {
    setPage((current) => ({ ...current, actionError: 'Ingresá fecha de inicio y finalización.' }));
    return false;
  }

  if (startDate > endDate) {
    setPage((current) => ({
      ...current,
      actionError: 'La fecha de inicio debe ser anterior o igual a la fecha de finalización.'
    }));
    return false;
  }

  return true;
}

function getDependentSubjectsHelpText(
  careerId: string,
  state: DependentLoadState,
  subjects: SubjectDto[]
): string {
  if (!careerId) {
    return 'Seleccioná una carrera para cargar sus materias.';
  }

  if (state === 'loading') {
    return 'Cargando materias...';
  }

  if (state === 'error') {
    return 'No fue posible cargar las materias de la carrera seleccionada.';
  }

  if (state === 'ready' && subjects.length === 0) {
    return 'No hay materias activas para esta carrera.';
  }

  return 'Seleccioná una materia activa de la carrera elegida.';
}
