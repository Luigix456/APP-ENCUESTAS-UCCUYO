import type { SurveyChoiceResultsDto } from '../../../types/results';
import { HorizontalBar } from './HorizontalBar';

export function ChoiceResultView({ choice }: { choice: SurveyChoiceResultsDto | null }) {
  if (!choice) {
    return <p>No hay datos de opciones para esta pregunta.</p>;
  }

  return (
    <div className="result-subsection">
      <div className="result-bars">
        {choice.options.map((option) => (
          <HorizontalBar
            count={option.count}
            key={option.optionId}
            label={option.text}
            percentage={option.percentage}
          />
        ))}
        {choice.other ? (
          <HorizontalBar count={choice.other.count} label="Otro" percentage={choice.other.percentage} />
        ) : null}
      </div>

      {choice.other ? (
        <section className="text-response-section">
          <h4>Respuestas en Otro</h4>
          {choice.other.values.length > 0 ? (
            <div className="text-response-list">
              {choice.other.values.map((value, index) => (
                <blockquote className="text-response" key={`${value}-${index}`}>
                  {value}
                </blockquote>
              ))}
            </div>
          ) : (
            <p>Sin respuestas en Otro.</p>
          )}
        </section>
      ) : null}
    </div>
  );
}
