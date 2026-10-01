import type { PublicSurveyQuestionDto } from '../../../types/publicSurvey';
import type { QuestionAnswerDraft } from '../types';
import { LongTextQuestion } from './questions/LongTextQuestion';
import { MatrixSingleChoiceQuestion } from './questions/MatrixSingleChoiceQuestion';
import { MultipleChoiceQuestion } from './questions/MultipleChoiceQuestion';
import { RatingScaleQuestion } from './questions/RatingScaleQuestion';
import { ShortTextQuestion } from './questions/ShortTextQuestion';
import { SingleChoiceQuestion } from './questions/SingleChoiceQuestion';

interface QuestionRendererProps {
  question: PublicSurveyQuestionDto;
  value: QuestionAnswerDraft;
  error?: string;
  onChange: (nextValue: Partial<QuestionAnswerDraft>) => void;
}

export function QuestionRenderer({ question, value, error, onChange }: QuestionRendererProps) {
  const errorId = `question-${question.id}-error`;
  const descriptionId = `question-${question.id}-description`;

  return (
    <fieldset
      aria-describedby={error ? errorId : descriptionId}
      aria-invalid={Boolean(error)}
      className="question"
      id={`question-${question.id}`}
      tabIndex={-1}
    >
      <legend>
        <span>{question.text}</span>
        {question.isRequired ? <strong>Obligatoria</strong> : null}
      </legend>

      <p className="question__hint" id={descriptionId}>
        {getQuestionHint(question)}
      </p>

      {renderQuestionInput(question, value, error, onChange)}

      {question.allowsComment ? (
        <label className="comment-field">
          <span>Comentario opcional</span>
          <textarea
            className="text-area"
            maxLength={1000}
            onChange={(event) => onChange({ comment: event.target.value })}
            rows={3}
            value={value.comment}
          />
        </label>
      ) : null}

      {error ? (
        <p className="field-error" id={errorId} role="alert">
          {error}
        </p>
      ) : null}
    </fieldset>
  );
}

function renderQuestionInput(
  question: PublicSurveyQuestionDto,
  value: QuestionAnswerDraft,
  error: string | undefined,
  onChange: (nextValue: Partial<QuestionAnswerDraft>) => void
) {
  const commonProps = { question, value, error, onChange };

  switch (question.type) {
    case 'SingleChoice':
      return <SingleChoiceQuestion {...commonProps} />;
    case 'MultipleChoice':
      return <MultipleChoiceQuestion {...commonProps} />;
    case 'ShortText':
      return <ShortTextQuestion {...commonProps} />;
    case 'LongText':
      return <LongTextQuestion {...commonProps} />;
    case 'RatingScale':
      return <RatingScaleQuestion {...commonProps} />;
    case 'MatrixSingleChoice':
      return <MatrixSingleChoiceQuestion {...commonProps} />;
  }
}

function getQuestionHint(question: PublicSurveyQuestionDto): string {
  switch (question.type) {
    case 'SingleChoice':
      return 'Seleccioná una opción.';
    case 'MultipleChoice':
      return 'Podés seleccionar una o más opciones.';
    case 'ShortText':
      return 'Respondé con una frase breve.';
    case 'LongText':
      return 'Respondé con el detalle que consideres necesario.';
    case 'RatingScale':
      return 'Seleccioná un valor de la escala.';
    case 'MatrixSingleChoice':
      return 'Seleccioná una opción por fila.';
  }
}
