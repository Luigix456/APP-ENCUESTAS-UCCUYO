import { useEffect, useState, type FormEvent } from 'react';
import { Link, useLocation, useNavigate, useParams } from 'react-router-dom';
import {
  activateSurvey,
  addSurveyMatrixRow,
  addSurveyQuestion,
  addSurveyQuestionOption,
  addSurveySection,
  archiveSurvey,
  deactivateSurvey,
  getSurvey,
  getOrCreateEditableSurveyVersion,
  publishSurvey,
  setSurveyMatrixRowActive,
  setSurveyQuestionActive,
  setSurveyQuestionOptionActive,
  setSurveySectionActive,
  sortSurveyDetail,
  updateSurvey,
  updateSurveyMatrixRow,
  updateSurveyQuestion,
  updateSurveyQuestionOption,
  updateSurveySection
} from '../../api/surveysApi';
import { useAuth } from '../../auth/AuthProvider';
import { ConfirmDialog } from '../../components/ui/Modal';
import { useToast } from '../../components/ui/ToastProvider';
import type {
  CreateSurveyMatrixRowRequest,
  CreateSurveyQuestionOptionRequest,
  CreateSurveyQuestionRequest,
  CreateSurveySectionRequest,
  SurveyDetailDto,
  SurveyMatrixRowDto,
  SurveyQuestionDto,
  SurveyQuestionOptionDto,
  SurveyQuestionType,
  SurveySectionDto,
  SurveyTarget
} from '../../types/surveys';
import { SURVEY_QUESTION_TYPES, SURVEY_TARGETS } from '../../types/surveys';
import {
  ActivityBadge,
  canQuestionHaveMatrixRows,
  canQuestionHaveOptions,
  formatQuestionType,
  formatSurveyTarget,
  getFriendlySurveyError,
  getNextOrder,
  getNextQuestionOrder,
  getNextSectionOrder,
  MANAGE_SURVEY_TEMPLATES_PERMISSION,
  PermissionDeniedPanel,
  SurveyVersionBadge,
  SurveyStatusBadge,
  trimmedOrNull
} from './surveyUi';

type LoadState = 'loading' | 'ready' | 'error';

interface SurveyFormState {
  title: string;
  description: string;
  target: SurveyTarget;
  isAnonymous: boolean;
}

interface SectionFormState {
  title: string;
  description: string;
  order: string;
}

interface QuestionFormState {
  text: string;
  type: SurveyQuestionType;
  isRequired: boolean;
  allowsComment: boolean;
  allowsOtherOption: boolean;
  order: string;
  ratingMin: string;
  ratingMax: string;
}

interface OptionFormState {
  text: string;
  value: string;
  order: string;
}

interface MatrixRowFormState {
  text: string;
  order: string;
}

export function SurveyEditorPage() {
  const { surveyId } = useParams();
  const location = useLocation();
  const navigate = useNavigate();
  const auth = useAuth();
  const toast = useToast();
  const [survey, setSurvey] = useState<SurveyDetailDto | null>(null);
  const [loadState, setLoadState] = useState<LoadState>('loading');
  const [pageError, setPageError] = useState<string | null>(null);
  const [actionError, setActionError] = useState<string | null>(null);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);
  const [surveyForm, setSurveyForm] = useState<SurveyFormState | null>(null);
  const [sectionForm, setSectionForm] = useState<SectionFormState>(createEmptySectionForm(1));
  const [activeAction, setActiveAction] = useState<string | null>(null);
  const [confirmAction, setConfirmAction] = useState<'publish' | 'archive' | 'toggle' | null>(null);

  const accessToken = auth.accessToken;
  const canManageTemplates = auth.hasPermission(MANAGE_SURVEY_TEMPLATES_PERMISSION);
  const isDraft = survey?.status === 'Draft';
  const routeMessage = (location.state as { editableVersionMessage?: string } | null)?.editableVersionMessage ?? null;

  useEffect(() => {
    if (routeMessage) {
      setSuccessMessage(routeMessage);
    }
  }, [routeMessage]);

  useEffect(() => { if (successMessage) toast.success(successMessage); }, [successMessage, toast]);
  useEffect(() => { if (actionError) toast.error('No se pudo completar la acción', actionError); }, [actionError, toast]);

  useEffect(() => {
    if (!canManageTemplates || !accessToken || !surveyId) {
      setLoadState('ready');
      return;
    }

    void loadSurvey(accessToken, surveyId);
  }, [accessToken, canManageTemplates, surveyId]);

  if (!canManageTemplates) {
    return <PermissionDeniedPanel />;
  }

  async function loadSurvey(token: string, id = surveyId) {
    if (!id) {
      return;
    }

    setLoadState('loading');
    setPageError(null);

    try {
      const nextSurvey = sortSurveyDetail(await getSurvey(id, token, auth.logout));

      if (nextSurvey.status !== 'Draft') {
        const editableVersion = await getOrCreateEditableSurveyVersion(id, token, auth.logout);
        const message = editableVersion.createdNewVersion
          ? `Se creó una versión editable v${editableVersion.versionNumber}.`
          : `Se abrió la versión editable v${editableVersion.versionNumber}.`;

        if (editableVersion.survey.id === id && editableVersion.survey.status !== 'Draft') {
          const readonlySurvey = sortSurveyDetail(editableVersion.survey);
          setSurvey(readonlySurvey);
          setSurveyForm(createSurveyForm(readonlySurvey));
          setSectionForm(createEmptySectionForm(getNextSectionOrder(readonlySurvey)));
          setSuccessMessage(message);
          setLoadState('ready');
          return;
        }

        navigate(`/app/surveys/${editableVersion.survey.id}/edit`, {
          replace: true,
          state: { editableVersionMessage: message }
        });
        return;
      }

      setSurvey(nextSurvey);
      setSurveyForm(createSurveyForm(nextSurvey));
      setSectionForm(createEmptySectionForm(getNextSectionOrder(nextSurvey)));
      setLoadState('ready');
    } catch (error) {
      setPageError(getFriendlySurveyError(error, 'No fue posible cargar la plantilla de encuesta.'));
      setLoadState('error');
    }
  }

  async function reloadSurvey() {
    if (!accessToken || !survey?.id) {
      return;
    }

    const nextSurvey = sortSurveyDetail(await getSurvey(survey.id, accessToken, auth.logout));
    setSurvey(nextSurvey);
    setSurveyForm(createSurveyForm(nextSurvey));
    setSectionForm(createEmptySectionForm(getNextSectionOrder(nextSurvey)));
  }

  async function handleSaveSurvey(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    if (!accessToken || !survey || !surveyForm) {
      return;
    }

    if (!surveyForm.title.trim()) {
      setActionError('Ingresá el título de la encuesta.');
      return;
    }

    setActiveAction('save-survey');
    setActionError(null);
    setSuccessMessage(null);

    try {
      await updateSurvey(
        survey.id,
        {
          title: surveyForm.title.trim(),
          description: trimmedOrNull(surveyForm.description),
          target: surveyForm.target,
          isAnonymous: surveyForm.isAnonymous
        },
        accessToken,
        auth.logout
      );
      await reloadSurvey();
      setSuccessMessage('Encuesta actualizada.');
    } catch (error) {
      setActionError(getFriendlySurveyError(error, 'No fue posible guardar la encuesta.'));
    } finally {
      setActiveAction(null);
    }
  }

  async function handleAddSection(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    if (!accessToken || !survey) {
      return;
    }

    const request = buildSectionRequest(sectionForm);
    const validationError = validateSectionRequest(request);

    if (validationError) {
      setActionError(validationError);
      return;
    }

    setActiveAction('add-section');
    setActionError(null);
    setSuccessMessage(null);

    try {
      const nextSurvey = sortSurveyDetail(await addSurveySection(survey.id, request, accessToken, auth.logout));
      setSurvey(nextSurvey);
      setSurveyForm(createSurveyForm(nextSurvey));
      setSectionForm(createEmptySectionForm(getNextSectionOrder(nextSurvey)));
      setSuccessMessage('Sección agregada.');
    } catch (error) {
      setActionError(getFriendlySurveyError(error, 'No fue posible agregar la sección.'));
    } finally {
      setActiveAction(null);
    }
  }

  async function handlePublish() {
    if (!accessToken || !survey) {
      return;
    }

    await runCommand('publish', 'Encuesta publicada.', 'No fue posible publicar la encuesta.', () =>
      publishSurvey(survey.id, accessToken, auth.logout)
    );
  }

  async function handleArchive() {
    if (!accessToken || !survey) {
      return;
    }

    await runCommand('archive', 'Encuesta archivada.', 'No fue posible archivar la encuesta.', () =>
      archiveSurvey(survey.id, accessToken, auth.logout)
    );
  }

  async function handleToggleSurveyActive() {
    if (!accessToken || !survey) {
      return;
    }

    await runCommand(
      'toggle-survey',
      survey.isActive ? 'Encuesta desactivada.' : 'Encuesta activada.',
      survey.isActive ? 'No fue posible desactivar la encuesta.' : 'No fue posible activar la encuesta.',
      () =>
        survey.isActive
          ? deactivateSurvey(survey.id, accessToken, auth.logout)
          : activateSurvey(survey.id, accessToken, auth.logout)
    );
  }

  async function runCommand(
    action: string,
    success: string,
    failure: string,
    command: () => Promise<unknown>
  ) {
    setActiveAction(action);
    setActionError(null);
    setSuccessMessage(null);

    try {
      await command();
      await reloadSurvey();
      setSuccessMessage(success);
    } catch (error) {
      setActionError(getFriendlySurveyError(error, failure));
    } finally {
      setActiveAction(null);
    }
  }

  if (loadState === 'loading') {
    return (
      <section className="app-content">
        <p>Cargando encuesta...</p>
      </section>
    );
  }

  if (loadState === 'error' || !survey || !surveyForm) {
    return (
      <section className="app-content" role="alert">
        <p className="eyebrow">Editor</p>
        <h2>No pudimos cargar la encuesta</h2>
        <p>{pageError ?? 'La plantilla solicitada no está disponible.'}</p>
      </section>
    );
  }

  return (
    <section className="app-content survey-editor-page">
      <header className="survey-editor-header">
        <div>
          <p className="eyebrow">Editor de plantilla</p>
          <h2>{survey.title}</h2>
          <p>{isDraft ? 'Editá la estructura antes de publicar.' : 'La estructura publicada o archivada es de sólo lectura.'}</p>
        </div>
        <div className="badge-group">
          <SurveyVersionBadge survey={survey} />
          <SurveyStatusBadge status={survey.status} />
          <ActivityBadge isActive={survey.isActive} />
        </div>
      </header>

      <div className="survey-editor-toolbar">
        <Link className="secondary-link-button" to="/app/surveys">
          Volver al listado
        </Link>
        <Link className="secondary-link-button" to={`/app/surveys/${survey.id}/preview`}>
          Vista previa
        </Link>
        {isDraft ? (
          <button
            className="primary-button"
            disabled={activeAction !== null}
            onClick={() => setConfirmAction('publish')}
            type="button"
          >
            {activeAction === 'publish' ? 'Publicando...' : 'Publicar'}
          </button>
        ) : null}
        {survey.status === 'Published' ? (
          <button
            className="danger-button"
            disabled={activeAction !== null}
            onClick={() => setConfirmAction('archive')}
            type="button"
          >
            {activeAction === 'archive' ? 'Archivando...' : 'Archivar'}
          </button>
        ) : null}
        <button
          className="secondary-button"
          disabled={activeAction !== null}
          onClick={() => setConfirmAction('toggle')}
          type="button"
        >
          {survey.isActive ? 'Desactivar' : 'Activar'}
        </button>
      </div>

      {!isDraft ? (
        <div className="inline-message">
          Esta encuesta ya no está en borrador. Podés consultar su estructura, pero no modificar secciones,
          preguntas, opciones ni filas de matriz.
        </div>
      ) : null}

      <form className="survey-admin-form" noValidate onSubmit={handleSaveSurvey}>
        <header>
          <h3>Datos generales</h3>
        </header>
        <label>
          <span>Título</span>
          <input
            className="text-input"
            disabled={!isDraft || activeAction !== null}
            maxLength={200}
            onChange={(event) => setSurveyForm((current) => current && { ...current, title: event.target.value })}
            required
            type="text"
            value={surveyForm.title}
          />
        </label>
        <label>
          <span>Descripción</span>
          <textarea
            className="text-area"
            disabled={!isDraft || activeAction !== null}
            maxLength={1000}
            onChange={(event) =>
              setSurveyForm((current) => current && { ...current, description: event.target.value })
            }
            rows={3}
            value={surveyForm.description}
          />
        </label>
        <label>
          <span>Audiencia</span>
          <select
            className="text-input"
            disabled={!isDraft || activeAction !== null}
            onChange={(event) =>
              setSurveyForm((current) =>
                current ? { ...current, target: event.target.value as SurveyTarget } : current
              )
            }
            value={surveyForm.target}
          >
            {SURVEY_TARGETS.map((target) => (
              <option key={target} value={target}>
                {formatSurveyTarget(target)}
              </option>
            ))}
          </select>
        </label>
        <label className="checkbox-field">
          <input
            checked={surveyForm.isAnonymous}
            disabled={!isDraft || activeAction !== null}
            onChange={(event) =>
              setSurveyForm((current) => current && { ...current, isAnonymous: event.target.checked })
            }
            type="checkbox"
          />
          <span>Respuestas anónimas</span>
        </label>
        {isDraft ? (
          <button className="primary-button" disabled={activeAction !== null} type="submit">
            {activeAction === 'save-survey' ? 'Guardando...' : 'Guardar datos generales'}
          </button>
        ) : null}
      </form>

      {isDraft ? (
        <form className="survey-admin-form" noValidate onSubmit={handleAddSection}>
          <header>
            <h3>Agregar sección</h3>
          </header>
          <SectionFields form={sectionForm} onChange={setSectionForm} />
          <button className="primary-button" disabled={activeAction !== null} type="submit">
            {activeAction === 'add-section' ? 'Agregando...' : 'Agregar sección'}
          </button>
        </form>
      ) : null}

      <div className="survey-structure">
        <h3>Estructura</h3>
        {survey.sections.length === 0 ? (
          <div className="empty-detail">
            <h3>Sin secciones</h3>
            <p>Agregá al menos una sección para comenzar a cargar preguntas.</p>
          </div>
        ) : (
          survey.sections.map((section) => (
            <SectionEditor
              accessToken={accessToken}
              authLogout={auth.logout}
              disabled={activeAction !== null}
              editable={isDraft}
              key={section.id}
              onError={setActionError}
              onRefresh={reloadSurvey}
              onSetSurvey={(nextSurvey) => {
                setSurvey(sortSurveyDetail(nextSurvey));
                setSurveyForm(createSurveyForm(nextSurvey));
              }}
              onSuccess={setSuccessMessage}
              section={section}
              surveyId={survey.id}
            />
          ))
        )}
      </div>
      <ConfirmDialog busy={activeAction === 'publish'} confirmLabel="Publicar encuesta" message="La estructura quedará bloqueada y esta versión podrá utilizarse en asignaciones y sesiones." onCancel={() => setConfirmAction(null)} onConfirm={() => { void handlePublish().finally(() => setConfirmAction(null)); }} open={confirmAction === 'publish'} title="¿Publicar esta encuesta?" />
      <ConfirmDialog busy={activeAction === 'archive'} confirmLabel="Archivar encuesta" message="La versión dejará de utilizarse para nuevas operaciones, pero su información histórica se conservará." onCancel={() => setConfirmAction(null)} onConfirm={() => { void handleArchive().finally(() => setConfirmAction(null)); }} open={confirmAction === 'archive'} title="¿Archivar esta encuesta?" tone="danger" />
      <ConfirmDialog busy={activeAction === 'toggle-survey'} confirmLabel={survey.isActive ? 'Desactivar' : 'Activar'} message={survey.isActive ? 'La encuesta dejará de estar disponible para nuevas operaciones.' : 'La encuesta volverá a estar disponible para nuevas operaciones.'} onCancel={() => setConfirmAction(null)} onConfirm={() => { void handleToggleSurveyActive().finally(() => setConfirmAction(null)); }} open={confirmAction === 'toggle'} title={survey.isActive ? '¿Desactivar encuesta?' : '¿Activar encuesta?'} tone={survey.isActive ? 'danger' : 'primary'} />
    </section>
  );
}

function SectionEditor({
  accessToken,
  authLogout,
  disabled,
  editable,
  onError,
  onRefresh,
  onSetSurvey,
  onSuccess,
  section,
  surveyId
}: {
  accessToken: string | null;
  authLogout: () => void;
  disabled: boolean;
  editable: boolean;
  onError: (message: string | null) => void;
  onRefresh: () => Promise<void>;
  onSetSurvey: (survey: SurveyDetailDto) => void;
  onSuccess: (message: string | null) => void;
  section: SurveySectionDto;
  surveyId: string;
}) {
  const [form, setForm] = useState<SectionFormState>(() => createSectionForm(section));
  const [questionForm, setQuestionForm] = useState<QuestionFormState>(() =>
    createEmptyQuestionForm(getNextQuestionOrder(section.questions))
  );
  const [isBusy, setIsBusy] = useState(false);

  useEffect(() => {
    setForm(createSectionForm(section));
    setQuestionForm(createEmptyQuestionForm(getNextQuestionOrder(section.questions)));
  }, [section]);

  async function handleSave(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    if (!accessToken) {
      return;
    }

    const request = buildSectionRequest(form);
    const validationError = validateSectionRequest(request);

    if (validationError) {
      onError(validationError);
      return;
    }

    await runLocalAction('Sección actualizada.', 'No fue posible actualizar la sección.', async () => {
      await updateSurveySection(surveyId, section.id, request, accessToken, authLogout);
      await onRefresh();
    });
  }

  async function handleToggleSection() {
    if (!accessToken) {
      return;
    }

    await runLocalAction(
      section.isActive ? 'Sección desactivada.' : 'Sección activada.',
      section.isActive ? 'No fue posible desactivar la sección.' : 'No fue posible activar la sección.',
      async () => {
        await setSurveySectionActive(surveyId, section.id, !section.isActive, accessToken, authLogout);
        await onRefresh();
      }
    );
  }

  async function handleAddQuestion(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    if (!accessToken) {
      return;
    }

    const request = buildQuestionRequest(questionForm);
    const validationError = validateQuestionRequest(request);

    if (validationError) {
      onError(validationError);
      return;
    }

    await runLocalAction('Pregunta agregada.', 'No fue posible agregar la pregunta.', async () => {
      const nextSurvey = await addSurveyQuestion(surveyId, section.id, request, accessToken, authLogout);
      onSetSurvey(nextSurvey);
      setQuestionForm(createEmptyQuestionForm(getNextQuestionOrder(section.questions) + 1));
    });
  }

  async function runLocalAction(success: string, failure: string, action: () => Promise<void>) {
    setIsBusy(true);
    onError(null);
    onSuccess(null);

    try {
      await action();
      onSuccess(success);
    } catch (error) {
      onError(getFriendlySurveyError(error, failure));
    } finally {
      setIsBusy(false);
    }
  }

  return (
    <article className="structure-card">
      <header>
        <div>
          <p className="eyebrow">Sección {section.order}</p>
          <h3>{section.title}</h3>
          <p>{section.description || 'Sin descripción'}</p>
        </div>
        <ActivityBadge isActive={section.isActive} />
      </header>

      {editable ? (
        <form className="nested-form" noValidate onSubmit={handleSave}>
          <SectionFields form={form} onChange={setForm} />
          <div className="form-actions">
            <button className="secondary-button" disabled={disabled || isBusy} type="submit">
              Guardar sección
            </button>
            <button
              className="secondary-button"
              disabled={disabled || isBusy}
              onClick={() => void handleToggleSection()}
              type="button"
            >
              {section.isActive ? 'Desactivar sección' : 'Activar sección'}
            </button>
          </div>
        </form>
      ) : null}

      <div className="structure-list">
        {section.questions.length === 0 ? (
          <p className="inline-message">Esta sección todavía no tiene preguntas.</p>
        ) : (
          section.questions.map((question) => (
            <QuestionEditor
              accessToken={accessToken}
              authLogout={authLogout}
              disabled={disabled || isBusy}
              editable={editable}
              key={question.id}
              onError={onError}
              onRefresh={onRefresh}
              onSetSurvey={onSetSurvey}
              onSuccess={onSuccess}
              question={question}
              sectionId={section.id}
              surveyId={surveyId}
            />
          ))
        )}
      </div>

      {editable ? (
        <form className="nested-form" noValidate onSubmit={handleAddQuestion}>
          <h4>Agregar pregunta</h4>
          <QuestionFields form={questionForm} onChange={setQuestionForm} />
          <button className="primary-button" disabled={disabled || isBusy} type="submit">
            Agregar pregunta
          </button>
        </form>
      ) : null}
    </article>
  );
}

function QuestionEditor({
  accessToken,
  authLogout,
  disabled,
  editable,
  onError,
  onRefresh,
  onSetSurvey,
  onSuccess,
  question,
  sectionId,
  surveyId
}: {
  accessToken: string | null;
  authLogout: () => void;
  disabled: boolean;
  editable: boolean;
  onError: (message: string | null) => void;
  onRefresh: () => Promise<void>;
  onSetSurvey: (survey: SurveyDetailDto) => void;
  onSuccess: (message: string | null) => void;
  question: SurveyQuestionDto;
  sectionId: string;
  surveyId: string;
}) {
  const [form, setForm] = useState<QuestionFormState>(() => createQuestionForm(question));
  const [optionForm, setOptionForm] = useState<OptionFormState>(() =>
    createEmptyOptionForm(getNextOrder(question.options))
  );
  const [matrixRowForm, setMatrixRowForm] = useState<MatrixRowFormState>(() =>
    createEmptyMatrixRowForm(getNextOrder(question.matrixRows))
  );
  const [isBusy, setIsBusy] = useState(false);
  const hasPendingTypeChange = form.type !== question.type;
  const hasPreviousTypeConfigurationWarning =
    hasPendingTypeChange &&
    ((question.options.length > 0 && !supportsOptionsForType(form.type)) ||
      (question.matrixRows.length > 0 && !supportsMatrixRowsForType(form.type)));

  useEffect(() => {
    setForm(createQuestionForm(question));
    setOptionForm(createEmptyOptionForm(getNextOrder(question.options)));
    setMatrixRowForm(createEmptyMatrixRowForm(getNextOrder(question.matrixRows)));
  }, [question]);

  async function handleSave(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    if (!accessToken) {
      return;
    }

    const request = buildQuestionRequest(form);
    const validationError = validateQuestionRequest(request);

    if (validationError) {
      onError(validationError);
      return;
    }

    await runLocalAction('Pregunta actualizada.', 'No fue posible actualizar la pregunta.', async () => {
      await updateSurveyQuestion(surveyId, sectionId, question.id, request, accessToken, authLogout);
      await onRefresh();
    });
  }

  async function handleToggleQuestion() {
    if (!accessToken) {
      return;
    }

    await runLocalAction(
      question.isActive ? 'Pregunta desactivada.' : 'Pregunta activada.',
      question.isActive ? 'No fue posible desactivar la pregunta.' : 'No fue posible activar la pregunta.',
      async () => {
        await setSurveyQuestionActive(surveyId, sectionId, question.id, !question.isActive, accessToken, authLogout);
        await onRefresh();
      }
    );
  }

  async function handleAddOption(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    if (!accessToken) {
      return;
    }

    const request = buildOptionRequest(optionForm);
    const validationError = validateOptionRequest(request);

    if (validationError) {
      onError(validationError);
      return;
    }

    await runLocalAction('Opción agregada.', 'No fue posible agregar la opción.', async () => {
      const nextSurvey = await addSurveyQuestionOption(
        surveyId,
        sectionId,
        question.id,
        request,
        accessToken,
        authLogout
      );
      onSetSurvey(nextSurvey);
    });
  }

  async function handleAddMatrixRow(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    if (!accessToken) {
      return;
    }

    const request = buildMatrixRowRequest(matrixRowForm);
    const validationError = validateMatrixRowRequest(request);

    if (validationError) {
      onError(validationError);
      return;
    }

    await runLocalAction('Fila de matriz agregada.', 'No fue posible agregar la fila.', async () => {
      const nextSurvey = await addSurveyMatrixRow(
        surveyId,
        sectionId,
        question.id,
        request,
        accessToken,
        authLogout
      );
      onSetSurvey(nextSurvey);
    });
  }

  async function runLocalAction(success: string, failure: string, action: () => Promise<void>) {
    setIsBusy(true);
    onError(null);
    onSuccess(null);

    try {
      await action();
      onSuccess(success);
    } catch (error) {
      onError(getFriendlySurveyError(error, failure));
    } finally {
      setIsBusy(false);
    }
  }

  return (
    <article className="question-editor-card">
      <header>
        <div>
          <p className="eyebrow">
            Pregunta {question.order} · {formatQuestionType(question.type)}
          </p>
          <h4>{question.text}</h4>
        </div>
        <ActivityBadge isActive={question.isActive} />
      </header>

      {editable ? (
        <form className="nested-form" noValidate onSubmit={handleSave}>
          <QuestionFields form={form} onChange={setForm} />
          <div className="form-actions">
            <button className="secondary-button" disabled={disabled || isBusy} type="submit">
              Guardar pregunta
            </button>
            <button
              className="secondary-button"
              disabled={disabled || isBusy}
              onClick={() => void handleToggleQuestion()}
              type="button"
            >
              {question.isActive ? 'Desactivar pregunta' : 'Activar pregunta'}
            </button>
          </div>
        </form>
      ) : null}

      {hasPendingTypeChange ? (
        <p className="inline-message">
          Guardá la pregunta para aplicar el nuevo tipo y configurar sus opciones.
        </p>
      ) : null}

      {hasPreviousTypeConfigurationWarning ? (
        <p className="inline-message">
          Esta pregunta ya posee configuración del tipo anterior. Al cambiar de tipo dejará de mostrarse en la
          encuesta, pero no se eliminará automáticamente.
        </p>
      ) : null}

      {!hasPendingTypeChange && canQuestionHaveOptions(question) ? (
        <div className="nested-block">
          <h5>Opciones</h5>
          {question.options.length === 0 ? <p className="inline-message">Sin opciones cargadas.</p> : null}
          {question.options.map((option) => (
            <OptionEditor
              accessToken={accessToken}
              authLogout={authLogout}
              disabled={disabled || isBusy}
              editable={editable}
              key={option.id}
              onError={onError}
              onRefresh={onRefresh}
              onSetSurvey={onSetSurvey}
              onSuccess={onSuccess}
              option={option}
              questionId={question.id}
              sectionId={sectionId}
              surveyId={surveyId}
            />
          ))}
          {editable ? (
            <form className="compact-form" noValidate onSubmit={handleAddOption}>
              <OptionFields form={optionForm} onChange={setOptionForm} />
              <button className="secondary-button" disabled={disabled || isBusy} type="submit">
                Agregar opción
              </button>
            </form>
          ) : null}
        </div>
      ) : null}

      {!hasPendingTypeChange && canQuestionHaveMatrixRows(question) ? (
        <div className="nested-block">
          <h5>Filas de matriz</h5>
          {question.matrixRows.length === 0 ? <p className="inline-message">Sin filas cargadas.</p> : null}
          {question.matrixRows.map((row) => (
            <MatrixRowEditor
              accessToken={accessToken}
              authLogout={authLogout}
              disabled={disabled || isBusy}
              editable={editable}
              key={row.id}
              onError={onError}
              onRefresh={onRefresh}
              onSetSurvey={onSetSurvey}
              onSuccess={onSuccess}
              questionId={question.id}
              row={row}
              sectionId={sectionId}
              surveyId={surveyId}
            />
          ))}
          {editable ? (
            <form className="compact-form" noValidate onSubmit={handleAddMatrixRow}>
              <MatrixRowFields form={matrixRowForm} onChange={setMatrixRowForm} />
              <button className="secondary-button" disabled={disabled || isBusy} type="submit">
                Agregar fila
              </button>
            </form>
          ) : null}
        </div>
      ) : null}
    </article>
  );
}

function OptionEditor({
  accessToken,
  authLogout,
  disabled,
  editable,
  onError,
  onRefresh,
  onSetSurvey,
  onSuccess,
  option,
  questionId,
  sectionId,
  surveyId
}: {
  accessToken: string | null;
  authLogout: () => void;
  disabled: boolean;
  editable: boolean;
  onError: (message: string | null) => void;
  onRefresh: () => Promise<void>;
  onSetSurvey: (survey: SurveyDetailDto) => void;
  onSuccess: (message: string | null) => void;
  option: SurveyQuestionOptionDto;
  questionId: string;
  sectionId: string;
  surveyId: string;
}) {
  const [form, setForm] = useState<OptionFormState>(() => createOptionForm(option));
  const [isBusy, setIsBusy] = useState(false);

  useEffect(() => setForm(createOptionForm(option)), [option]);

  if (!editable) {
    return <ReadOnlyLine isActive={option.isActive} order={option.order} text={option.text} />;
  }

  async function handleSave(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    if (!accessToken) {
      return;
    }

    const request = buildOptionRequest(form);
    const validationError = validateOptionRequest(request);

    if (validationError) {
      onError(validationError);
      return;
    }

    await runOptionAction('Opción actualizada.', 'No fue posible actualizar la opción.', async () => {
      const nextSurvey = await updateSurveyQuestionOption(
        surveyId,
        sectionId,
        questionId,
        option.id,
        request,
        accessToken,
        authLogout
      );
      if (nextSurvey) {
        onSetSurvey(nextSurvey);
      } else {
        await onRefresh();
      }
    });
  }

  async function handleToggle() {
    if (!accessToken) {
      return;
    }

    await runOptionAction(
      option.isActive ? 'Opción desactivada.' : 'Opción activada.',
      option.isActive ? 'No fue posible desactivar la opción.' : 'No fue posible activar la opción.',
      async () => {
        await setSurveyQuestionOptionActive(
          surveyId,
          sectionId,
          questionId,
          option.id,
          !option.isActive,
          accessToken,
          authLogout
        );
        await onRefresh();
      }
    );
  }

  async function runOptionAction(success: string, failure: string, action: () => Promise<void>) {
    setIsBusy(true);
    onError(null);
    onSuccess(null);

    try {
      await action();
      onSuccess(success);
    } catch (error) {
      onError(getFriendlySurveyError(error, failure));
    } finally {
      setIsBusy(false);
    }
  }

  return (
    <form className="compact-form" noValidate onSubmit={handleSave}>
      <OptionFields form={form} onChange={setForm} />
      <button className="secondary-button" disabled={disabled || isBusy} type="submit">
        Guardar
      </button>
      <button className="secondary-button" disabled={disabled || isBusy} onClick={() => void handleToggle()} type="button">
        {option.isActive ? 'Desactivar' : 'Activar'}
      </button>
    </form>
  );
}

function MatrixRowEditor({
  accessToken,
  authLogout,
  disabled,
  editable,
  onError,
  onRefresh,
  onSetSurvey,
  onSuccess,
  questionId,
  row,
  sectionId,
  surveyId
}: {
  accessToken: string | null;
  authLogout: () => void;
  disabled: boolean;
  editable: boolean;
  onError: (message: string | null) => void;
  onRefresh: () => Promise<void>;
  onSetSurvey: (survey: SurveyDetailDto) => void;
  onSuccess: (message: string | null) => void;
  questionId: string;
  row: SurveyMatrixRowDto;
  sectionId: string;
  surveyId: string;
}) {
  const [form, setForm] = useState<MatrixRowFormState>(() => createMatrixRowForm(row));
  const [isBusy, setIsBusy] = useState(false);

  useEffect(() => setForm(createMatrixRowForm(row)), [row]);

  if (!editable) {
    return <ReadOnlyLine isActive={row.isActive} order={row.order} text={row.text} />;
  }

  async function handleSave(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    if (!accessToken) {
      return;
    }

    const request = buildMatrixRowRequest(form);
    const validationError = validateMatrixRowRequest(request);

    if (validationError) {
      onError(validationError);
      return;
    }

    await runRowAction('Fila actualizada.', 'No fue posible actualizar la fila.', async () => {
      const nextSurvey = await updateSurveyMatrixRow(
        surveyId,
        sectionId,
        questionId,
        row.id,
        request,
        accessToken,
        authLogout
      );
      if (nextSurvey) {
        onSetSurvey(nextSurvey);
      } else {
        await onRefresh();
      }
    });
  }

  async function handleToggle() {
    if (!accessToken) {
      return;
    }

    await runRowAction(
      row.isActive ? 'Fila desactivada.' : 'Fila activada.',
      row.isActive ? 'No fue posible desactivar la fila.' : 'No fue posible activar la fila.',
      async () => {
        await setSurveyMatrixRowActive(
          surveyId,
          sectionId,
          questionId,
          row.id,
          !row.isActive,
          accessToken,
          authLogout
        );
        await onRefresh();
      }
    );
  }

  async function runRowAction(success: string, failure: string, action: () => Promise<void>) {
    setIsBusy(true);
    onError(null);
    onSuccess(null);

    try {
      await action();
      onSuccess(success);
    } catch (error) {
      onError(getFriendlySurveyError(error, failure));
    } finally {
      setIsBusy(false);
    }
  }

  return (
    <form className="compact-form" noValidate onSubmit={handleSave}>
      <MatrixRowFields form={form} onChange={setForm} />
      <button className="secondary-button" disabled={disabled || isBusy} type="submit">
        Guardar
      </button>
      <button className="secondary-button" disabled={disabled || isBusy} onClick={() => void handleToggle()} type="button">
        {row.isActive ? 'Desactivar' : 'Activar'}
      </button>
    </form>
  );
}

function SectionFields({
  form,
  onChange
}: {
  form: SectionFormState;
  onChange: (nextForm: SectionFormState) => void;
}) {
  return (
    <>
      <label>
        <span>Título</span>
        <input
          className="text-input"
          maxLength={200}
          onChange={(event) => onChange({ ...form, title: event.target.value })}
          required
          type="text"
          value={form.title}
        />
      </label>
      <label>
        <span>Descripción</span>
        <textarea
          className="text-area"
          maxLength={1000}
          onChange={(event) => onChange({ ...form, description: event.target.value })}
          rows={2}
          value={form.description}
        />
      </label>
      <label>
        <span>Orden</span>
        <input
          className="text-input"
          min={1}
          onChange={(event) => onChange({ ...form, order: event.target.value })}
          required
          type="number"
          value={form.order}
        />
      </label>
    </>
  );
}

function QuestionFields({
  form,
  onChange
}: {
  form: QuestionFormState;
  onChange: (nextForm: QuestionFormState) => void;
}) {
  const supportsOther = form.type === 'SingleChoice' || form.type === 'MultipleChoice';

  function handleTypeChange(type: SurveyQuestionType) {
    onChange({
      ...form,
      type,
      allowsOtherOption: supportsOtherForType(type) ? form.allowsOtherOption : false,
      ratingMin: type === 'RatingScale' ? form.ratingMin || '1' : '',
      ratingMax: type === 'RatingScale' ? form.ratingMax || '5' : ''
    });
  }

  return (
    <>
      <label>
        <span>Texto</span>
        <textarea
          className="text-area"
          maxLength={1000}
          onChange={(event) => onChange({ ...form, text: event.target.value })}
          required
          rows={2}
          value={form.text}
        />
      </label>
      <label>
        <span>Tipo</span>
        <select
          className="text-input"
          onChange={(event) => handleTypeChange(event.target.value as SurveyQuestionType)}
          value={form.type}
        >
          {SURVEY_QUESTION_TYPES.map((type) => (
            <option key={type} value={type}>
              {formatQuestionType(type)}
            </option>
          ))}
        </select>
      </label>
      <label>
        <span>Orden</span>
        <input
          className="text-input"
          min={1}
          onChange={(event) => onChange({ ...form, order: event.target.value })}
          required
          type="number"
          value={form.order}
        />
      </label>
      {form.type === 'RatingScale' ? (
        <div className="compact-form">
          <label>
            <span>Mínimo</span>
            <input
              className="text-input"
              min={0}
              onChange={(event) => onChange({ ...form, ratingMin: event.target.value })}
              required
              type="number"
              value={form.ratingMin}
            />
          </label>
          <label>
            <span>Máximo</span>
            <input
              className="text-input"
              min={1}
              onChange={(event) => onChange({ ...form, ratingMax: event.target.value })}
              required
              type="number"
              value={form.ratingMax}
            />
          </label>
        </div>
      ) : null}
      <div className="toggle-row">
        <label className="checkbox-field">
          <input
            checked={form.isRequired}
            onChange={(event) => onChange({ ...form, isRequired: event.target.checked })}
            type="checkbox"
          />
          <span>Obligatoria</span>
        </label>
        <label className="checkbox-field">
          <input
            checked={form.allowsComment}
            onChange={(event) => onChange({ ...form, allowsComment: event.target.checked })}
            type="checkbox"
          />
          <span>Permite comentario</span>
        </label>
        <label className="checkbox-field">
          <input
            checked={form.allowsOtherOption}
            disabled={!supportsOther}
            onChange={(event) => onChange({ ...form, allowsOtherOption: event.target.checked })}
            type="checkbox"
          />
          <span>Permite opción Otra</span>
        </label>
      </div>
    </>
  );
}

function OptionFields({
  form,
  onChange
}: {
  form: OptionFormState;
  onChange: (nextForm: OptionFormState) => void;
}) {
  return (
    <>
      <label>
        <span>Texto</span>
        <input
          className="text-input"
          maxLength={500}
          onChange={(event) => onChange({ ...form, text: event.target.value })}
          required
          type="text"
          value={form.text}
        />
      </label>
      <label>
        <span>Valor</span>
        <input
          className="text-input"
          maxLength={200}
          onChange={(event) => onChange({ ...form, value: event.target.value })}
          required
          type="text"
          value={form.value}
        />
      </label>
      <label>
        <span>Orden</span>
        <input
          className="text-input"
          min={1}
          onChange={(event) => onChange({ ...form, order: event.target.value })}
          required
          type="number"
          value={form.order}
        />
      </label>
    </>
  );
}

function MatrixRowFields({
  form,
  onChange
}: {
  form: MatrixRowFormState;
  onChange: (nextForm: MatrixRowFormState) => void;
}) {
  return (
    <>
      <label>
        <span>Texto</span>
        <input
          className="text-input"
          maxLength={500}
          onChange={(event) => onChange({ ...form, text: event.target.value })}
          required
          type="text"
          value={form.text}
        />
      </label>
      <label>
        <span>Orden</span>
        <input
          className="text-input"
          min={1}
          onChange={(event) => onChange({ ...form, order: event.target.value })}
          required
          type="number"
          value={form.order}
        />
      </label>
    </>
  );
}

function ReadOnlyLine({ isActive, order, text }: { isActive: boolean; order: number; text: string }) {
  return (
    <div className="readonly-line">
      <span>{order}. {text}</span>
      <ActivityBadge isActive={isActive} />
    </div>
  );
}

function createSurveyForm(survey: SurveyDetailDto): SurveyFormState {
  return {
    title: survey.title,
    description: survey.description ?? '',
    target: survey.target,
    isAnonymous: survey.isAnonymous
  };
}

function createEmptySectionForm(order: number): SectionFormState {
  return {
    title: '',
    description: '',
    order: String(order)
  };
}

function createSectionForm(section: SurveySectionDto): SectionFormState {
  return {
    title: section.title,
    description: section.description ?? '',
    order: String(section.order)
  };
}

function createEmptyQuestionForm(order: number): QuestionFormState {
  return {
    text: '',
    type: 'SingleChoice',
    isRequired: true,
    allowsComment: false,
    allowsOtherOption: false,
    order: String(order),
    ratingMin: '',
    ratingMax: ''
  };
}

function createQuestionForm(question: SurveyQuestionDto): QuestionFormState {
  return {
    text: question.text,
    type: question.type,
    isRequired: question.isRequired,
    allowsComment: question.allowsComment,
    allowsOtherOption: question.allowsOtherOption,
    order: String(question.order),
    ratingMin: question.ratingMin === null ? '' : String(question.ratingMin),
    ratingMax: question.ratingMax === null ? '' : String(question.ratingMax)
  };
}

function createEmptyOptionForm(order: number): OptionFormState {
  return {
    text: '',
    value: '',
    order: String(order)
  };
}

function createOptionForm(option: SurveyQuestionOptionDto): OptionFormState {
  return {
    text: option.text,
    value: option.value,
    order: String(option.order)
  };
}

function createEmptyMatrixRowForm(order: number): MatrixRowFormState {
  return {
    text: '',
    order: String(order)
  };
}

function createMatrixRowForm(row: SurveyMatrixRowDto): MatrixRowFormState {
  return {
    text: row.text,
    order: String(row.order)
  };
}

function buildSectionRequest(form: SectionFormState): CreateSurveySectionRequest {
  return {
    title: form.title.trim(),
    description: trimmedOrNull(form.description),
    order: Number(form.order)
  };
}

function buildQuestionRequest(form: QuestionFormState): CreateSurveyQuestionRequest {
  const isRating = form.type === 'RatingScale';

  return {
    text: form.text.trim(),
    type: form.type,
    isRequired: form.isRequired,
    allowsComment: form.allowsComment,
    allowsOtherOption: supportsOtherForType(form.type) ? form.allowsOtherOption : false,
    order: Number(form.order),
    ratingMin: isRating ? Number(form.ratingMin) : null,
    ratingMax: isRating ? Number(form.ratingMax) : null
  };
}

function buildOptionRequest(form: OptionFormState): CreateSurveyQuestionOptionRequest {
  return {
    text: form.text.trim(),
    value: form.value.trim(),
    order: Number(form.order)
  };
}

function buildMatrixRowRequest(form: MatrixRowFormState): CreateSurveyMatrixRowRequest {
  return {
    text: form.text.trim(),
    order: Number(form.order)
  };
}

function validateSectionRequest(request: CreateSurveySectionRequest): string | null {
  if (!request.title) {
    return 'Ingresá el título de la sección.';
  }

  return validateOrder(request.order);
}

function validateQuestionRequest(request: CreateSurveyQuestionRequest): string | null {
  if (!request.text) {
    return 'Ingresá el texto de la pregunta.';
  }

  const orderError = validateOrder(request.order);

  if (orderError) {
    return orderError;
  }

  if (request.type === 'RatingScale') {
    if (request.ratingMin === null || request.ratingMax === null || request.ratingMin >= request.ratingMax) {
      return 'Configurá una escala de valoración válida.';
    }
  }

  return null;
}

function validateOptionRequest(request: CreateSurveyQuestionOptionRequest): string | null {
  if (!request.text) {
    return 'Ingresá el texto de la opción.';
  }

  if (!request.value) {
    return 'Ingresá el valor de la opción.';
  }

  return validateOrder(request.order);
}

function validateMatrixRowRequest(request: CreateSurveyMatrixRowRequest): string | null {
  if (!request.text) {
    return 'Ingresá el texto de la fila.';
  }

  return validateOrder(request.order);
}

function validateOrder(order: number): string | null {
  if (!Number.isInteger(order) || order <= 0) {
    return 'Indicá un orden mayor a cero.';
  }

  return null;
}

function supportsOtherForType(type: SurveyQuestionType): boolean {
  return type === 'SingleChoice' || type === 'MultipleChoice';
}

function supportsOptionsForType(type: SurveyQuestionType): boolean {
  return type === 'SingleChoice' || type === 'MultipleChoice' || type === 'MatrixSingleChoice';
}

function supportsMatrixRowsForType(type: SurveyQuestionType): boolean {
  return type === 'MatrixSingleChoice';
}
