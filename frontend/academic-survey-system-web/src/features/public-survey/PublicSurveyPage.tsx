import { useEffect, useMemo, useState } from 'react';
import { ApiError, getPublicSurvey, submitSurveyResponse } from '../../api/publicSurveyApi';
import type {
  PublicSurveyMatrixRowDto,
  PublicSurveyQuestionDto,
  PublicSurveyQuestionOptionDto,
  PublicSurveySectionDto,
  PublicSurveySessionDto,
  SubmitSurveyResponseRequest
} from '../../types/publicSurvey';
import { QuestionRenderer } from './components/QuestionRenderer';
import { InstitutionBrand } from '../../components/InstitutionBrand';
import { ConfirmDialog } from '../../components/ui/Modal';
import {
  type AnswerState,
  type QuestionAnswerDraft,
  type ValidationErrors,
  createEmptyAnswer,
  toSubmitAnswer,
  validateQuestion
} from './types';

interface PublicSurveyPageProps {
  accessCode: string;
}

interface PageError {
  title: string;
  message: string;
}

type LoadState =
  | { status: 'loading' }
  | { status: 'ready'; survey: PublicSurveySessionDto }
  | { status: 'error'; error: PageError };

export function PublicSurveyPage({ accessCode }: PublicSurveyPageProps) {
  const [loadState, setLoadState] = useState<LoadState>({ status: 'loading' });
  const [answers, setAnswers] = useState<AnswerState>({});
  const [validationErrors, setValidationErrors] = useState<ValidationErrors>({});
  const [submitError, setSubmitError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [isSubmitted, setIsSubmitted] = useState(false);
  const [confirmSubmitOpen, setConfirmSubmitOpen] = useState(false);

  useEffect(() => {
    let isCurrent = true;

    setLoadState({ status: 'loading' });
    setSubmitError(null);
    setValidationErrors({});
    setIsSubmitted(false);

    getPublicSurvey(accessCode)
      .then((survey) => {
        if (!isCurrent) {
          return;
        }

        const orderedSurvey = orderSurvey(survey);
        setAnswers(createInitialAnswers(orderedSurvey));
        setLoadState({ status: 'ready', survey: orderedSurvey });
      })
      .catch((error: unknown) => {
        if (!isCurrent) {
          return;
        }

        setLoadState({ status: 'error', error: getLoadError(error) });
      });

    return () => {
      isCurrent = false;
    };
  }, [accessCode]);

  const questions = useMemo(() => {
    if (loadState.status !== 'ready') {
      return [];
    }

    return loadState.survey.sections.flatMap((section) => section.questions);
  }, [loadState]);

  if (loadState.status === 'loading') {
    return (
      <main className="survey-shell">
        <section className="status-panel" aria-live="polite">
          <p>Cargando encuesta...</p>
        </section>
      </main>
    );
  }

  if (loadState.status === 'error') {
    return (
      <main className="survey-shell">
        <section className="status-panel" role="alert">
          <p className="eyebrow">Encuesta pública</p>
          <h1>{loadState.error.title}</h1>
          <p>{loadState.error.message}</p>
        </section>
      </main>
    );
  }

  if (isSubmitted) {
    return (
      <main className="survey-shell">
        <section className="status-panel" aria-live="polite">
          <p className="eyebrow">Encuesta pública</p>
          <h1>Respuesta enviada correctamente</h1>
          <p>Gracias por completar la encuesta.</p>
        </section>
      </main>
    );
  }

  const survey = loadState.survey;

  function updateAnswer(questionId: string, nextValue: Partial<QuestionAnswerDraft>) {
    setAnswers((current) => ({
      ...current,
      [questionId]: {
        ...(current[questionId] ?? createEmptyAnswer()),
        ...nextValue
      }
    }));

    if (validationErrors[questionId]) {
      setValidationErrors((current) => {
        const nextErrors = { ...current };
        delete nextErrors[questionId];
        return nextErrors;
      });
    }
  }

  function handleSubmit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();

    if (isSubmitting) {
      return;
    }

    setSubmitError(null);

    const nextErrors = validateSurvey(questions, answers);
    setValidationErrors(nextErrors);

    if (Object.keys(nextErrors).length > 0) {
      focusFirstInvalidQuestion(nextErrors);
      return;
    }

    setConfirmSubmitOpen(true);
  }

  async function confirmSubmit() {
    if (isSubmitting) {
      return;
    }

    const request = buildSubmitRequest(questions, answers);
    setIsSubmitting(true);
    setSubmitError(null);

    try {
      await submitSurveyResponse(accessCode, request);
      setConfirmSubmitOpen(false);
      setIsSubmitted(true);
    } catch (error) {
      setConfirmSubmitOpen(false);
      setSubmitError(getSubmitError(error));
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <main className="survey-shell">
      <article className="survey-layout">
        <header className="survey-header">
          <InstitutionBrand compact subtitle="Sistema de Encuestas Académicas" />
          <div className="survey-header__title">
            <p className="eyebrow">Encuesta académica</p>
            <h1>{survey.surveyTitle}</h1>
          </div>
          {survey.surveyDescription ? <p>{survey.surveyDescription}</p> : null}
          <dl className="survey-meta">
            <div>
              <dt>Carrera</dt>
              <dd>{survey.careerName}</dd>
            </div>
            <div>
              <dt>Asignatura</dt>
              <dd>{survey.subjectName}</dd>
            </div>
            <div>
              <dt>Docente</dt>
              <dd>{survey.teacherFullName}</dd>
            </div>
            <div>
              <dt>Ciclo</dt>
              <dd>
                {survey.academicCycleYear} - {formatPeriod(survey.academicCyclePeriod)}
              </dd>
            </div>
          </dl>
        </header>

        <form className="survey-form" noValidate onSubmit={handleSubmit}>
          {survey.sections.map((section) => (
            <section className="survey-section" key={section.id}>
              <header>
                <h2>{section.title}</h2>
                {section.description ? <p>{section.description}</p> : null}
              </header>

              {section.questions.map((question) => (
                <QuestionRenderer
                  error={validationErrors[question.id]}
                  key={question.id}
                  onChange={(nextValue) => updateAnswer(question.id, nextValue)}
                  question={question}
                  value={answers[question.id] ?? createEmptyAnswer()}
                />
              ))}
            </section>
          ))}

          {submitError ? (
            <div className="submit-error" role="alert">
              {submitError}
            </div>
          ) : null}

          <div className="submit-bar">
            <button className="primary-button" disabled={isSubmitting} type="submit">
              {isSubmitting ? 'Enviando...' : 'Enviar respuesta'}
            </button>
          </div>
        </form>
      </article>
      <ConfirmDialog
        busy={isSubmitting}
        confirmLabel="Sí, enviar encuesta"
        message="Revisá tus respuestas antes de continuar. Una vez enviada la encuesta, no podrás modificarlas."
        onCancel={() => setConfirmSubmitOpen(false)}
        onConfirm={() => void confirmSubmit()}
        open={confirmSubmitOpen}
        title="¿Enviar la encuesta?"
      />
    </main>
  );
}

function orderSurvey(survey: PublicSurveySessionDto): PublicSurveySessionDto {
  return {
    ...survey,
    sections: [...survey.sections]
      .sort(byOrder)
      .map((section) => ({
        ...section,
        questions: [...section.questions]
          .sort(byOrder)
          .map((question) => ({
            ...question,
            options: [...question.options].sort(byOrder),
            matrixRows: [...question.matrixRows].sort(byOrder)
          }))
      }))
  };
}

function byOrder<T extends { order: number }>(left: T, right: T): number {
  return left.order - right.order;
}

function createInitialAnswers(survey: PublicSurveySessionDto): AnswerState {
  return survey.sections.reduce<AnswerState>((state, section) => {
    section.questions.forEach((question) => {
      state[question.id] = createEmptyAnswer();
    });

    return state;
  }, {});
}

function validateSurvey(
  questions: PublicSurveyQuestionDto[],
  answers: AnswerState
): ValidationErrors {
  return questions.reduce<ValidationErrors>((errors, question) => {
    const error = validateQuestion(question, answers[question.id] ?? createEmptyAnswer());

    if (error) {
      errors[question.id] = error;
    }

    return errors;
  }, {});
}

function buildSubmitRequest(
  questions: PublicSurveyQuestionDto[],
  answers: AnswerState
): SubmitSurveyResponseRequest {
  return {
    answers: questions
      .map((question) => toSubmitAnswer(question, answers[question.id] ?? createEmptyAnswer()))
      .filter((answer): answer is NonNullable<typeof answer> => answer !== null)
  };
}

function focusFirstInvalidQuestion(errors: ValidationErrors) {
  const firstQuestionId = Object.keys(errors)[0];

  if (!firstQuestionId) {
    return;
  }

  window.requestAnimationFrame(() => {
    document.getElementById(`question-${firstQuestionId}`)?.focus();
  });
}

function getLoadError(error: unknown): PageError {
  if (error instanceof ApiError) {
    if (error.status === 404) {
      return {
        title: 'Encuesta no encontrada',
        message: 'No encontramos una encuesta disponible para el código indicado.'
      };
    }

    if (error.status === 409 || error.status === 410 || isAvailabilityError(error)) {
      return {
        title: 'Encuesta no disponible',
        message: 'La sesión está cerrada, vencida o no se encuentra disponible en este momento.'
      };
    }

    return {
      title: 'No pudimos cargar la encuesta',
      message: 'No fue posible cargar la encuesta. Intentá nuevamente.'
    };
  }

  return {
    title: 'Error de conexión',
    message: 'No se pudo conectar con el servicio. Intentá nuevamente en unos minutos.'
  };
}

function getSubmitError(error: unknown): string {
  if (error instanceof ApiError) {
    if (isResponseLimitError(error)) {
      return 'La encuesta alcanzó la cantidad máxima de respuestas prevista.';
    }

    if (error.status === 409 || error.status === 410 || isAvailabilityError(error)) {
      return 'La sesión de esta encuesta ya finalizó y no es posible enviar respuestas.';
    }

    return 'No fue posible enviar la respuesta. Intentá nuevamente.';
  }

  return 'No fue posible enviar la respuesta. Intentá nuevamente.';
}

function isResponseLimitError(error: ApiError): boolean {
  const code = error.code?.toLowerCase() ?? '';
  const messages = [
    error.message,
    error.detail,
    ...error.details
  ]
    .filter((message): message is string => Boolean(message))
    .map((message) => message.toLowerCase());

  return (
    code === 'survey.responselimitreached' ||
    messages.some((message) =>
      message.includes('cantidad máxima de respuestas') ||
      message.includes('maximum') && message.includes('responses')
    )
  );
}

function isAvailabilityError(error: ApiError): boolean {
  const code = error.code?.toLowerCase() ?? '';
  const messages = [
    error.message,
    error.detail,
    ...error.details
  ]
    .filter((message): message is string => Boolean(message))
    .map((message) => message.toLowerCase());

  return (
    code === 'surveysession.notavailable' ||
    code.includes('closed') ||
    code.includes('expired') ||
    code.includes('notavailable') ||
    messages.some(
      (message) =>
        message.includes('survey session is not available') ||
        message.includes('not available') ||
        message.includes('cerrada') ||
        message.includes('vencida') ||
        message.includes('no disponible')
    )
  );
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
