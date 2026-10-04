import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
  type ReactNode
} from 'react';
import {
  getAcademicCycles,
  getAcademicUnits,
  getCareers
} from '../../api/academicCatalogApi';
import { useAuth } from '../../auth/AuthProvider';
import type { AcademicCycleDto, AcademicUnitDto, CareerDto } from '../../types/academicCatalog';

const STORAGE_KEY = 'academicSurvey.academicContext';

interface StoredAcademicContext {
  academicUnitId?: string;
  careerId?: string;
  academicCycleId?: string;
}

interface AcademicContextValue {
  academicUnits: AcademicUnitDto[];
  careers: CareerDto[];
  academicCycles: AcademicCycleDto[];
  academicUnitId: string;
  careerId: string;
  academicCycleId: string;
  selectedAcademicUnit: AcademicUnitDto | null;
  selectedCareer: CareerDto | null;
  selectedAcademicCycle: AcademicCycleDto | null;
  isLoading: boolean;
  error: string | null;
  setAcademicUnit: (academicUnitId: string) => void;
  setCareer: (careerId: string) => void;
  setAcademicCycle: (academicCycleId: string) => void;
  selectCareerContext: (academicUnitId: string, careerId: string) => void;
  clearContext: () => void;
  refreshContext: () => Promise<void>;
}

const AcademicContext = createContext<AcademicContextValue | null>(null);

export function AcademicContextProvider({ children }: { children: ReactNode }) {
  const auth = useAuth();
  const [storedIds, setStoredIds] = useState<StoredAcademicContext>(() => readStoredContext());
  const [academicUnits, setAcademicUnits] = useState<AcademicUnitDto[]>([]);
  const [careers, setCareers] = useState<CareerDto[]>([]);
  const [academicCycles, setAcademicCycles] = useState<AcademicCycleDto[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const accessToken = auth.accessToken;
  const canReadCatalog = auth.hasPermission('academic.catalog.read') || auth.hasPermission('academic.catalog.manage');

  const persistContext = useCallback((nextIds: StoredAcademicContext) => {
    const normalized = normalizeStoredContext(nextIds);
    setStoredIds((current) => (isSameStoredContext(current, normalized) ? current : normalized));

    if (!normalized.academicUnitId && !normalized.careerId && !normalized.academicCycleId) {
      sessionStorage.removeItem(STORAGE_KEY);
      return;
    }

    sessionStorage.setItem(STORAGE_KEY, JSON.stringify(normalized));
  }, []);

  useEffect(() => {
    if (auth.isLoading) {
      return;
    }

    if (!auth.isAuthenticated || !canReadCatalog) {
      sessionStorage.removeItem(STORAGE_KEY);
      setStoredIds({});
      setAcademicUnits([]);
      setCareers([]);
      setAcademicCycles([]);
      setError(null);
      setIsLoading(false);
    }
  }, [auth.isAuthenticated, auth.isLoading, canReadCatalog]);

  const loadContext = useCallback(
    async (signal?: AbortSignal) => {
      if (!accessToken || !canReadCatalog) {
        setAcademicUnits([]);
        setCareers([]);
        setAcademicCycles([]);
        setIsLoading(false);
        setError(null);
        return;
      }

      setIsLoading(true);
      setError(null);

      try {
        const [units, cycles] = await Promise.all([
          getAcademicUnits({ accessToken, onUnauthorized: auth.logout, includeInactive: false, signal }),
          getAcademicCycles({ accessToken, onUnauthorized: auth.logout, includeInactive: false, signal })
        ]);

        const orderedCycles = [...cycles].sort(compareAcademicCycles);
        const activeAcademicUnitId = units.some((unit) => unit.id === storedIds.academicUnitId)
          ? storedIds.academicUnitId
          : '';

        let nextCareers: CareerDto[] = [];
        let activeCareerId = '';

        if (activeAcademicUnitId) {
          nextCareers = await getCareers({
            accessToken,
            academicUnitId: activeAcademicUnitId,
            includeInactive: false,
            onUnauthorized: auth.logout,
            signal
          });
          activeCareerId = nextCareers.some((career) => career.id === storedIds.careerId)
            ? storedIds.careerId ?? ''
            : '';
        }

        const activeAcademicCycleId = orderedCycles.some((cycle) => cycle.id === storedIds.academicCycleId)
          ? storedIds.academicCycleId
          : '';

        setAcademicUnits(units);
        setCareers(nextCareers);
        setAcademicCycles(orderedCycles);
        persistContext({
          academicUnitId: activeAcademicUnitId || undefined,
          careerId: activeCareerId || undefined,
          academicCycleId: activeAcademicCycleId || undefined
        });
      } catch (loadError) {
        if (signal?.aborted) {
          return;
        }

        setError('No fue posible cargar el contexto académico.');
      } finally {
        if (!signal?.aborted) {
          setIsLoading(false);
        }
      }
    },
    [
      accessToken,
      auth.logout,
      canReadCatalog,
      persistContext,
      storedIds.academicCycleId,
      storedIds.academicUnitId,
      storedIds.careerId
    ]
  );

  useEffect(() => {
    const abortController = new AbortController();
    void loadContext(abortController.signal);
    return () => abortController.abort();
  }, [loadContext]);

  const selectedAcademicUnit = useMemo(
    () => academicUnits.find((unit) => unit.id === storedIds.academicUnitId) ?? null,
    [academicUnits, storedIds.academicUnitId]
  );
  const selectedCareer = useMemo(
    () => careers.find((career) => career.id === storedIds.careerId) ?? null,
    [careers, storedIds.careerId]
  );
  const selectedAcademicCycle = useMemo(
    () => academicCycles.find((cycle) => cycle.id === storedIds.academicCycleId) ?? null,
    [academicCycles, storedIds.academicCycleId]
  );

  const value = useMemo<AcademicContextValue>(
    () => ({
      academicUnits,
      careers,
      academicCycles,
      academicUnitId: storedIds.academicUnitId ?? '',
      careerId: storedIds.careerId ?? '',
      academicCycleId: storedIds.academicCycleId ?? '',
      selectedAcademicUnit,
      selectedCareer,
      selectedAcademicCycle,
      isLoading,
      error,
      setAcademicUnit: (academicUnitId) => {
        persistContext({
          academicUnitId: academicUnitId || undefined,
          academicCycleId: storedIds.academicCycleId
        });
      },
      setCareer: (careerId) => {
        persistContext({
          ...storedIds,
          careerId: careerId || undefined
        });
      },
      setAcademicCycle: (academicCycleId) => {
        persistContext({
          ...storedIds,
          academicCycleId: academicCycleId || undefined
        });
      },
      selectCareerContext: (academicUnitId, careerId) => {
        persistContext({
          academicUnitId: academicUnitId || undefined,
          careerId: careerId || undefined,
          academicCycleId: storedIds.academicCycleId
        });
      },
      clearContext: () => persistContext({}),
      refreshContext: () => loadContext()
    }),
    [
      academicCycles,
      academicUnits,
      careers,
      error,
      isLoading,
      loadContext,
      persistContext,
      selectedAcademicCycle,
      selectedAcademicUnit,
      selectedCareer,
      storedIds
    ]
  );

  return <AcademicContext.Provider value={value}>{children}</AcademicContext.Provider>;
}

export function useAcademicContext(): AcademicContextValue {
  const context = useContext(AcademicContext);

  if (!context) {
    throw new Error('useAcademicContext debe usarse dentro de AcademicContextProvider.');
  }

  return context;
}

function readStoredContext(): StoredAcademicContext {
  try {
    const raw = sessionStorage.getItem(STORAGE_KEY);

    if (!raw) {
      return {};
    }

    return normalizeStoredContext(JSON.parse(raw) as StoredAcademicContext);
  } catch {
    sessionStorage.removeItem(STORAGE_KEY);
    return {};
  }
}

function normalizeStoredContext(value: StoredAcademicContext): StoredAcademicContext {
  return {
    academicUnitId: typeof value.academicUnitId === 'string' ? value.academicUnitId : undefined,
    careerId: typeof value.careerId === 'string' ? value.careerId : undefined,
    academicCycleId: typeof value.academicCycleId === 'string' ? value.academicCycleId : undefined
  };
}

function isSameStoredContext(left: StoredAcademicContext, right: StoredAcademicContext): boolean {
  return (
    (left.academicUnitId ?? '') === (right.academicUnitId ?? '') &&
    (left.careerId ?? '') === (right.careerId ?? '') &&
    (left.academicCycleId ?? '') === (right.academicCycleId ?? '')
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
