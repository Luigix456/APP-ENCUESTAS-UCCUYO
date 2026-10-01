import type { SurveyRatingResultsDto } from '../../../types/results';
import { formatAverage } from '../resultsUi';
import { HorizontalBar } from './HorizontalBar';

export function RatingResultView({ rating }: { rating: SurveyRatingResultsDto | null }) {
  if (!rating) {
    return <p>No hay datos de escala para esta pregunta.</p>;
  }

  const denominator = rating.configuredMaximum ?? rating.maximumObserved;

  return (
    <div className="rating-result">
      <div className="rating-result__summary">
        <div>
          <span>Promedio</span>
          <strong>
            {formatAverage(rating.average)}
            {denominator ? ` / ${denominator}` : ''}
          </strong>
        </div>
        <div>
          <span>Respuestas</span>
          <strong>{rating.responseCount}</strong>
        </div>
        <div>
          <span>Mínimo observado</span>
          <strong>{rating.minimumObserved ?? 'Sin datos'}</strong>
        </div>
        <div>
          <span>Máximo observado</span>
          <strong>{rating.maximumObserved ?? 'Sin datos'}</strong>
        </div>
      </div>

      <div className="result-bars">
        {rating.distribution.map((item) => (
          <HorizontalBar
            count={item.count}
            key={item.value}
            label={String(item.value)}
            percentage={item.percentage}
          />
        ))}
      </div>
    </div>
  );
}
