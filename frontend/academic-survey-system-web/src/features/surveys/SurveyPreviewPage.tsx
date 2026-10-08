import { useEffect, useMemo, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { getSurvey, sortSurveyDetail } from '../../api/surveysApi';
import { useAuth } from '../../auth/AuthProvider';
import type {
  PublicSurveyQuestionDto,
  PublicSurveySectionDto
} from '../../types/publicSurvey';
import type {
  SurveyDetailDto,
  SurveyMatrixRowDto,
  SurveyQuestionDto,
  SurveyQuestionOptionDto,
  SurveySectionDto
} from '../../types/surveys';
import { QuestionRenderer } from '../public-survey/components/QuestionRenderer';
import {
  createEmptyAnswer,
  type AnswerState,
  type QuestionAnswerDraft
} from '../public-survey/types';
import {
  ActivityBadge,
  formatSurveyTarget,
  getFriendlySurveyError,
  MANAGE_SURVEY_TEMPLATES_PERMISSION,
  READ_SURVEY_TEMPLATES_PERMISSION,
  PermissionDeniedPanel,
  SurveyVersionBadge,
  SurveyStatusBadge
} from './surveyUi';

type LoadState = 'loading' | 'ready' | 'error';

export function SurveyPreviewPage() {
  const { surveyId } = useParams();
  const auth = useAuth();
  const [survey, setSurvey] = useState<SurveyDetailDto | null>(null);
  const [answers, setAnswers] = useState<AnswerState>({});
  const [loadState, setLoadState] = useState<LoadState>('loading');
  const [pageError, setPageError] = useState<string | null>(null);

  const accessToken = auth.accessToken;
  const canManageTemplates = auth.hasPermission(MANAGE_SURVEY_TEMPLATES_PERMISSION);
  const canReadTemplates = canManageTemplates || auth.hasPermission(READ_SURVEY_TEMPLATES_PERMISSION);
  const previewSections = useMemo(() => (survey ? toPreviewSections(survey.sections) : []), [survey]);

  useEffect(() => {
    if (!canReadTemplates || !accessToken || !surveyId) {
      setLoadState('ready');
      return;
    }

    setLoadState('loading');
    getSurvey(surveyId, accessToken, auth.logout)
      .then((nextSurvey) => {
        const sortedSurvey = sortSurveyDetail(nextSurvey);
        const nextPreviewSections = toPreviewSections(sortedSurvey.sections);

        setSurvey(sortedSurvey);
        setAnswers(createPreviewAnswers(nextPreviewSections));
        setLoadState('ready');
      })
      .catch((error) => {
        setPageError(getFriendlySurveyError(error, 'No fue posible cargar la vista previa.'));
        setLoadState('error');
      });
  }, [accessToken, auth.logout, canReadTemplates, surveyId]);

  if (!canReadTemplates) {
    return <PermissionDeniedPanel mode="read" />;
  }

  function updatePreviewAnswer(questionId: string, nextValue: Partial<QuestionAnswerDraft>) {
    setAnswers((current) => ({
      ...current,
      [questionId]: {
        ...(current[questionId] ?? createEmptyAnswer()),
        ...nextValue
      }
    }));
  }

  function resetPreviewAnswers() {
    setAnswers(createPreviewAnswers(previewSections));
  }

  if (loadState === 'loading') {
    return (
      <section className="app-content">
        <p>Cargando vista previa...</p>
      </section>
    );
  }

  if (loadState === 'error' || !survey) {
    return (
      <section className="app-content" role="alert">
        <p className="eyebrow">Vista previa</p>
        <h2>No pudimos cargar la encuesta</h2>
        <p>{pageError ?? 'La plantilla solicitada no está disponible.'}</p>
      </section>
    );
  }

  return (
    <section className="app-content survey-preview-page">
      <header className="survey-preview-header">
        <div>
          <p className="eyebrow">Vista previa administrativa</p>
          <h2>{survey.title}</h2>
          <p>{survey.description || 'Sin descripción'}</p>
        </div>
        <div className="badge-group">
          <SurveyVersionBadge survey={survey} />
          <SurveyStatusBadge status={survey.status} />
          <ActivityBadge isActive={survey.isActive} />
        </div>
      </header>

      <div className="inline-message" role="status">
        Vista previa: las respuestas ingresadas aquí no se guardarán.
      </div>

      <dl className="survey-card-meta">
        <div>
          <dt>Audiencia</dt>
          <dd>{formatSurveyTarget(survey.target)}</dd>
        </div>
        <div>
          <dt>Anonimato</dt>
          <dd>{survey.isAnonymous ? 'Anónima' : 'Identificada'}</dd>
        </div>
      </dl>

      <div className="survey-preview-content">
        {previewSections.length === 0 ? (
          <div className="empty-detail">
            <h3>La encuesta no tiene secciones activas para mostrar</h3>
            <p>La vista previa sólo incluye secciones, preguntas, opciones y filas activas.</p>
          </div>
        ) : (
          previewSections.map((section) => (
            <section className="survey-section" key={section.id}>
              <header>
                <h2>
                  {section.order}. {section.title}
                </h2>
                <p>{section.description || 'Sin descripción'}</p>
              </header>

              {section.questions.length === 0 ? (
                <p className="inline-message">Esta sección no tiene preguntas activas.</p>
              ) : (
                section.questions.map((question) => (
                  <QuestionRenderer
                    key={question.id}
                    onChange={(nextValue) => updatePreviewAnswer(question.id, nextValue)}
                    question={question}
                    value={answers[question.id] ?? createEmptyAnswer()}
                  />
                ))
              )}
            </section>
          ))
        )}
      </div>

      <div className="form-actions">
        <button className="secondary-button" onClick={resetPreviewAnswers} type="button">
          Restablecer respuestas de vista previa
        </button>
        {canManageTemplates ? (
          <Link className="secondary-link-button" to={`/app/surveys/${survey.id}/edit`}>
            Volver al editor
          </Link>
        ) : null}
        <Link className="secondary-link-button" to="/app/surveys">
          Volver al listado
        </Link>
      </div>
    </section>
  );
}

function toPreviewSections(sections: SurveySectionDto[]): PublicSurveySectionDto[] {
  return sections
    .filter((section) => section.isActive)
    .map((section) => ({
      id: section.id,
      title: section.title,
      description: section.description,
      order: section.order,
      questions: section.questions
        .filter((question) => question.isActive)
        .map(toPreviewQuestion)
        .sort(compareByOrder)
    }))
    .sort(compareByOrder);
}

function toPreviewQuestion(question: SurveyQuestionDto): PublicSurveyQuestionDto {
  return {
    id: question.id,
    text: question.text,
    type: question.type,
    isRequired: question.isRequired,
    allowsComment: question.allowsComment,
    allowsOtherOption: question.allowsOtherOption,
    order: question.order,
    options: question.options.filter((option) => option.isActive).map(toPreviewOption).sort(compareByOrder),
    matrixRows: question.matrixRows.filter((row) => row.isActive).map(toPreviewMatrixRow).sort(compareByOrder),
    ratingMin: question.ratingMin,
    ratingMax: question.ratingMax
  };
}

function toPreviewOption(option: SurveyQuestionOptionDto) {
  return {
    id: option.id,
    text: option.text,
    value: option.value,
    order: option.order
  };
}

function toPreviewMatrixRow(row: SurveyMatrixRowDto) {
  return {
    id: row.id,
    text: row.text,
    order: row.order
  };
}

function createPreviewAnswers(sections: PublicSurveySectionDto[]): AnswerState {
  return sections.reduce<AnswerState>((state, section) => {
    section.questions.forEach((question) => {
      state[question.id] = createEmptyAnswer();
    });

    return state;
  }, {});
}

function compareByOrder<T extends { order: number }>(left: T, right: T): number {
  return left.order - right.order;
}
