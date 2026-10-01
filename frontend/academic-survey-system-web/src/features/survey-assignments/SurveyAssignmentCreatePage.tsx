import { useEffect, useMemo, useState, type FormEvent } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  getAcademicCycles,
  getCareers,
  getSubjects,
  getTeacherSubjectAssignments
} from '../../api/academicCatalogApi';
import { createSurveyAssignment } from '../../api/surveyAssignmentsApi';
import { getSurveys } from '../../api/surveysApi';
import { useAuth } from '../../auth/AuthProvider';
import type {
  AcademicCycleDto,
  CareerDto,
  SubjectDto,
  TeacherSubjectAssignmentDto
} from '../../types/academicCatalog';
import type { CreateSurveyAssignmentRequest } from '../../types/surveyAssignments';
import type { SurveySummaryDto } from '../../types/surveys';
import {
  formatAcademicCycle,
  formatSubject,
  formatSurveyOption,
  formatTeacherSubjectAssignment,
  getFriendlyAssignmentError,
  MANAGE_SURVEY_ASSIGNMENTS_PERMISSION,
  READ_ACADEMIC_CATALOG_PERMISSION,
  SurveyAssignmentPermissionPanel
} from './surveyAssignmentUi';
import { useAcademicContext } from '../academic-context/AcademicContextProvider';

type LoadState = 'loading' | 'ready' | 'error';
type DependentLoadState = 'idle' | 'loading' | 'ready' | 'error';

interface AssignmentFormState {
  surveyId: string;
  careerId: string;
  subjectId: string;
  academicCycleId: string;
  teacherSubjectAssignmentId: string;
}

const initialForm: AssignmentFormState = {
  surveyId: '',
  careerId: '',
  subjectId: '',
  academicCycleId: '',
  teacherSubjectAssignmentId: ''
};

export function SurveyAssignmentCreatePage({ contextual = false }: { contextual?: boolean }) {
  const auth = useAuth();
  const academicContext = useAcademicContext();
  const navigate = useNavigate();
  const [surveys, setSurveys] = useState<SurveySummaryDto[]>([]);
  const [careers, setCareers] = useState<CareerDto[]>([]);
  const [subjects, setSubjects] = useState<SubjectDto[]>([]);
  const [cycles, setCycles] = useState<AcademicCycleDto[]>([]);
  const [teacherAssignments, setTeacherAssignments] = useState<TeacherSubjectAssignmentDto[]>([]);
  const [form, setForm] = useState<AssignmentFormState>(() => ({
    ...initialForm,
    careerId: contextual ? academicContext.careerId : '',
    academicCycleId: contextual ? academicContext.academicCycleId : ''
  }));
  const [loadState, setLoadState] = useState<LoadState>('loading');
  const [subjectsState, setSubjectsState] = useState<DependentLoadState>('idle');
  const [teacherAssignmentsState, setTeacherAssignmentsState] = useState<DependentLoadState>('idle');
  const [pageError, setPageError] = useState<string | null>(null);
  const [subjectsError, setSubjectsError] = useState<string | null>(null);
  const [teacherAssignmentsError, setTeacherAssignmentsError] = useState<string | null>(null);
  const [formError, setFormError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const accessToken = auth.accessToken;
  const canManageAssignments = auth.hasPermission(MANAGE_SURVEY_ASSIGNMENTS_PERMISSION);
  const canReadCatalog = auth.hasPermission(READ_ACADEMIC_CATALOG_PERMISSION);

  useEffect(() => {
    if (!canManageAssignments || !canReadCatalog || !accessToken) {
      setLoadState('ready');
      return;
    }

    setLoadState('loading');
    setPageError(null);

    Promise.all([
      getSurveys({ includeInactive: false, status: 'Published', target: '' }, accessToken, auth.logout),
      contextual && academicContext.selectedCareer
        ? Promise.resolve([academicContext.selectedCareer])
        : getCareers({ accessToken, onUnauthorized: auth.logout }),
      getAcademicCycles({ accessToken, onUnauthorized: auth.logout })
    ])
      .then(([nextSurveys, nextCareers, nextCycles]) => {
        setSurveys(nextSurveys.filter((survey) => survey.status === 'Published' && survey.isActive));
        setCareers(nextCareers);
        setCycles(nextCycles);
        setLoadState('ready');
      })
      .catch((error) => {
        setPageError(getFriendlyAssignmentError(error, 'No fue posible cargar los datos para crear la asignación.'));
        setLoadState('error');
      });
  }, [
    accessToken,
    academicContext.selectedCareer,
    auth.logout,
    canManageAssignments,
    canReadCatalog,
    contextual
  ]);

  useEffect(() => {
    if (!contextual) {
      return;
    }

    setForm((current) => ({
      ...current,
      careerId: academicContext.careerId,
      academicCycleId: academicContext.academicCycleId,
      subjectId: current.careerId === academicContext.careerId ? current.subjectId : '',
      teacherSubjectAssignmentId:
        current.careerId === academicContext.careerId &&
        current.academicCycleId === academicContext.academicCycleId
          ? current.teacherSubjectAssignmentId
          : ''
    }));
  }, [academicContext.academicCycleId, academicContext.careerId, contextual]);

  useEffect(() => {
    if (!accessToken || !form.careerId || !canManageAssignments || !canReadCatalog) {
      setSubjects([]);
      setSubjectsState('idle');
      return;
    }

    const abortController = new AbortController();

    setSubjects([]);
    setSubjectsState('loading');
    setSubjectsError(null);

    getSubjects(form.careerId, {
      accessToken,
      onUnauthorized: auth.logout,
      signal: abortController.signal
    })
      .then((nextSubjects) => {
        setSubjects(nextSubjects);
        setSubjectsState('ready');
      })
      .catch((error) => {
        if (abortController.signal.aborted) {
          return;
        }

        setSubjectsError(getFriendlyAssignmentError(error, 'No fue posible cargar las materias.'));
        setSubjectsState('error');
      });

    return () => {
      abortController.abort();
    };
  }, [accessToken, auth.logout, canManageAssignments, canReadCatalog, form.careerId]);

  useEffect(() => {
    if (
      !accessToken ||
      !form.subjectId ||
      !form.academicCycleId ||
      !canManageAssignments ||
      !canReadCatalog
    ) {
      setTeacherAssignments([]);
      setTeacherAssignmentsState('idle');
      return;
    }

    const abortController = new AbortController();

    setTeacherAssignments([]);
    setTeacherAssignmentsState('loading');
    setTeacherAssignmentsError(null);

    getTeacherSubjectAssignments(
        {
          includeInactive: false,
          careerId: form.careerId,
          subjectId: form.subjectId,
          academicCycleId: form.academicCycleId
      },
      {
        accessToken,
        onUnauthorized: auth.logout,
        signal: abortController.signal
      }
    )
      .then((nextAssignments) => {
        setTeacherAssignments(nextAssignments);
        setTeacherAssignmentsState('ready');
      })
      .catch((error) => {
        if (abortController.signal.aborted) {
          return;
        }

        setTeacherAssignmentsError(
          getFriendlyAssignmentError(error, 'No fue posible cargar las asignaciones docente-materia.')
        );
        setTeacherAssignmentsState('error');
      });

    return () => {
      abortController.abort();
    };
  }, [
    accessToken,
    auth.logout,
    canManageAssignments,
    canReadCatalog,
    form.academicCycleId,
    form.subjectId
  ]);

  const selectedCareer = useMemo(
    () => careers.find((career) => career.id === form.careerId) ?? null,
    [careers, form.careerId]
  );

  if (!canManageAssignments) {
    return <SurveyAssignmentPermissionPanel />;
  }

  if (!canReadCatalog) {
    return <SurveyAssignmentPermissionPanel missingCatalog />;
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    if (!accessToken) {
      return;
    }

    const validationError = validateForm(form);

    if (validationError) {
      setFormError(validationError);
      return;
    }

    setIsSubmitting(true);
    setFormError(null);

    try {
      const createdAssignment = await createSurveyAssignment(buildRequest(form), accessToken, auth.logout);
      navigate(contextual ? '/app/context/surveys' : '/app/survey-assignments', {
        replace: true,
        state: { createdAssignmentId: createdAssignment.id }
      });
    } catch (error) {
      setFormError(getFriendlyAssignmentError(error, 'No fue posible crear la asignación.'));
    } finally {
      setIsSubmitting(false);
    }
  }

  function handleCareerChange(careerId: string) {
    setForm((current) => ({
      ...current,
      careerId,
      subjectId: '',
      teacherSubjectAssignmentId: ''
    }));
    setTeacherAssignments([]);
    setTeacherAssignmentsState('idle');
    setFormError(null);
  }

  function handleSubjectChange(subjectId: string) {
    setForm((current) => ({
      ...current,
      subjectId,
      teacherSubjectAssignmentId: ''
    }));
    setFormError(null);
  }

  function handleAcademicCycleChange(academicCycleId: string) {
    setForm((current) => ({
      ...current,
      academicCycleId,
      teacherSubjectAssignmentId: ''
    }));
    setFormError(null);
  }

  if (loadState === 'loading') {
    return (
      <section className="app-content">
        <p>Cargando datos académicos...</p>
      </section>
    );
  }

  if (contextual && !academicContext.selectedCareer) {
    return (
      <section className="app-content empty-detail">
        <h3>Seleccioná una carrera.</h3>
        <p>Elegí una carrera en el panel lateral para crear una asignación contextual.</p>
      </section>
    );
  }

  if (contextual && !academicContext.selectedAcademicCycle) {
    return (
      <section className="app-content empty-detail">
        <h3>Seleccioná un ciclo lectivo.</h3>
        <p>Elegí un ciclo lectivo para crear la asignación de encuesta.</p>
      </section>
    );
  }

  if (loadState === 'error') {
    return (
      <section className="app-content" role="alert">
        <p className="eyebrow">Nueva asignación</p>
        <h2>No pudimos cargar el formulario</h2>
        <p>{pageError}</p>
      </section>
    );
  }

  return (
    <section className="app-content assignments-page">
      <header className="surveys-header">
        <div>
          <p className="eyebrow">{contextual ? 'Carrera seleccionada' : 'Nueva asignación'}</p>
          <h2>Asignar encuesta publicada</h2>
          <p>
            {contextual
              ? `La asignación se creará para ${academicContext.selectedCareer?.name}.`
              : 'Seleccioná una plantilla activa y el contexto académico donde estará disponible.'}
          </p>
        </div>
      </header>

      <form className="survey-admin-form assignment-form" noValidate onSubmit={handleSubmit}>
        <label>
          <span>Encuesta</span>
          <select
            aria-describedby="survey-help"
            className="text-input"
            onChange={(event) => {
              setForm((current) => ({ ...current, surveyId: event.target.value }));
              setFormError(null);
            }}
            required
            value={form.surveyId}
          >
            <option value="">Seleccionar...</option>
            {surveys.map((survey) => (
              <option key={survey.id} value={survey.id}>
                {formatSurveyOption(survey)}
              </option>
            ))}
          </select>
          <small id="survey-help">
            {surveys.length === 0
              ? 'No hay encuestas publicadas y activas disponibles.'
              : 'Sólo se muestran plantillas publicadas y activas.'}
          </small>
        </label>

        <label>
          <span>Carrera</span>
          <select
            aria-describedby="career-help"
            className="text-input"
            disabled={contextual}
            onChange={(event) => handleCareerChange(event.target.value)}
            required
            value={form.careerId}
          >
            <option value="">Seleccionar...</option>
            {careers.map((career) => (
              <option key={career.id} value={career.id}>
                {career.name}
              </option>
            ))}
          </select>
          <small id="career-help">
            {careers.length === 0 ? 'No hay carreras activas disponibles.' : 'Seleccioná la carrera del contexto.'}
          </small>
        </label>

        <label>
          <span>Materia</span>
          <select
            aria-describedby="subject-help"
            className="text-input"
            disabled={!form.careerId || subjectsState === 'loading'}
            onChange={(event) => handleSubjectChange(event.target.value)}
            required
            value={form.subjectId}
          >
            <option value="">Seleccionar...</option>
            {subjects.map((subject) => (
              <option key={subject.id} value={subject.id}>
                {formatSubject(subject)}
              </option>
            ))}
          </select>
          <small id="subject-help">{getSubjectHelpText(form.careerId, subjectsState, subjects, subjectsError)}</small>
        </label>

        <label>
          <span>Ciclo lectivo</span>
          <select
            aria-describedby="cycle-help"
            className="text-input"
            disabled={contextual}
            onChange={(event) => handleAcademicCycleChange(event.target.value)}
            required
            value={form.academicCycleId}
          >
            <option value="">Seleccionar...</option>
            {cycles.map((cycle) => (
              <option key={cycle.id} value={cycle.id}>
                {formatAcademicCycle(cycle)}
              </option>
            ))}
          </select>
          <small id="cycle-help">
            {cycles.length === 0 ? 'No hay ciclos lectivos activos disponibles.' : 'Seleccioná el ciclo lectivo.'}
          </small>
        </label>

        <label className="assignment-form__wide">
          <span>Docente / asignación docente-materia</span>
          <select
            aria-describedby="teacher-assignment-help"
            className="text-input"
            disabled={!form.subjectId || !form.academicCycleId || teacherAssignmentsState === 'loading'}
            onChange={(event) => {
              setForm((current) => ({ ...current, teacherSubjectAssignmentId: event.target.value }));
              setFormError(null);
            }}
            required
            value={form.teacherSubjectAssignmentId}
          >
            <option value="">Seleccionar...</option>
            {teacherAssignments.map((assignment) => (
              <option key={assignment.id} value={assignment.id}>
                {formatTeacherSubjectAssignment(assignment)}
              </option>
            ))}
          </select>
          <small id="teacher-assignment-help">
            {getTeacherAssignmentHelpText(
              selectedCareer?.name ?? null,
              form.subjectId,
              form.academicCycleId,
              teacherAssignmentsState,
              teacherAssignments,
              teacherAssignmentsError
            )}
          </small>
        </label>

        {formError ? (
          <p className="submit-error assignment-form__wide" role="alert">
            {formError}
          </p>
        ) : null}

        <div className="form-actions assignment-form__wide">
          <button className="primary-button" disabled={isSubmitting} type="submit">
            {isSubmitting ? 'Creando...' : 'Crear asignación'}
          </button>
          <button
            className="secondary-button"
            onClick={() => navigate(contextual ? '/app/context/surveys' : '/app/survey-assignments')}
            type="button"
          >
            Cancelar
          </button>
        </div>
      </form>
    </section>
  );
}

function buildRequest(form: AssignmentFormState): CreateSurveyAssignmentRequest {
  return {
    surveyId: form.surveyId,
    careerId: form.careerId,
    subjectId: form.subjectId,
    academicCycleId: form.academicCycleId,
    teacherSubjectAssignmentId: form.teacherSubjectAssignmentId
  };
}

function validateForm(form: AssignmentFormState): string | null {
  if (!form.surveyId) {
    return 'Seleccioná una encuesta publicada.';
  }

  if (!form.careerId) {
    return 'Seleccioná una carrera.';
  }

  if (!form.subjectId) {
    return 'Seleccioná una materia.';
  }

  if (!form.academicCycleId) {
    return 'Seleccioná un ciclo lectivo.';
  }

  if (!form.teacherSubjectAssignmentId) {
    return 'Seleccioná una asignación docente-materia.';
  }

  return null;
}

function getSubjectHelpText(
  careerId: string,
  state: DependentLoadState,
  subjects: SubjectDto[],
  error: string | null
): string {
  if (!careerId) {
    return 'Seleccioná una carrera para cargar sus materias.';
  }

  if (state === 'loading') {
    return 'Cargando materias...';
  }

  if (state === 'error') {
    return error ?? 'No fue posible cargar las materias.';
  }

  if (state === 'ready' && subjects.length === 0) {
    return 'No hay materias activas para esta carrera.';
  }

  return 'Las materias se filtran por la carrera seleccionada.';
}

function getTeacherAssignmentHelpText(
  careerName: string | null,
  subjectId: string,
  academicCycleId: string,
  state: DependentLoadState,
  assignments: TeacherSubjectAssignmentDto[],
  error: string | null
): string {
  if (!subjectId || !academicCycleId) {
    return 'Seleccioná materia y ciclo lectivo para cargar docentes asignados.';
  }

  if (state === 'loading') {
    return 'Cargando docentes asignados...';
  }

  if (state === 'error') {
    return error ?? 'No fue posible cargar docentes asignados.';
  }

  if (state === 'ready' && assignments.length === 0) {
    return 'No hay docentes asignados a esta materia y ciclo lectivo.';
  }

  return careerName
    ? `Asignaciones docentes activas para ${careerName}.`
    : 'Asignaciones docentes activas para la materia y ciclo lectivo seleccionados.';
}
