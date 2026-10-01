import type { SurveyTextResultsDto } from '../../../types/results';

export function TextResultView({ title = 'Respuestas abiertas', textValues }: {
  title?: string;
  textValues: SurveyTextResultsDto | null;
}) {
  return (
    <section className="text-response-section">
      <h4>{title}</h4>
      {textValues && textValues.values.length > 0 ? (
        <div className="text-response-list">
          {textValues.values.map((value, index) => (
            <blockquote className="text-response" key={`${value}-${index}`}>
              {value}
            </blockquote>
          ))}
        </div>
      ) : (
        <p>Sin respuestas de texto.</p>
      )}
    </section>
  );
}
