import type { SurveyQuestionResultsDto } from '../../../types/results';
import { formatQuestionType } from '../resultsUi';
import { ChoiceResultView } from './ChoiceResultView';
import { MatrixResultView } from './MatrixResultView';
import { RatingResultView } from './RatingResultView';
import { TextResultView } from './TextResultView';

export function QuestionResultCard({ question }: { question: SurveyQuestionResultsDto }) {
  return (
    <article className="question-result-card">
      <header>
        <div>
          <p className="eyebrow">{formatQuestionType(question.type)}</p>
          <h3>{question.text}</h3>
        </div>
        <span className="status-badge">{question.responseCount} respuestas</span>
      </header>

      {renderQuestionResult(question)}

      {question.comments.length > 0 ? (
        <section className="comments-section">
          <h4>Comentarios adicionales</h4>
          <div className="text-response-list">
            {question.comments.map((comment) => (
              <blockquote className="text-response" key={comment.answerId}>
                {comment.comment}
              </blockquote>
            ))}
          </div>
        </section>
      ) : null}
    </article>
  );
}

function renderQuestionResult(question: SurveyQuestionResultsDto) {
  switch (question.type) {
    case 'SingleChoice':
    case 'MultipleChoice':
      return <ChoiceResultView choice={question.choice} />;
    case 'ShortText':
      return <TextResultView textValues={question.textValues} />;
    case 'LongText':
      return <TextResultView title="Respuestas" textValues={question.textValues} />;
    case 'RatingScale':
      return <RatingResultView rating={question.rating} />;
    case 'MatrixSingleChoice':
      return <MatrixResultView matrix={question.matrix} />;
    default:
      return <p>Tipo de pregunta no reconocido.</p>;
  }
}
