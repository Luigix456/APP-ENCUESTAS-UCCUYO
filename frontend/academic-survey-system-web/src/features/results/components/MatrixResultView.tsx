import type { SurveyMatrixResultsDto } from '../../../types/results';
import { HorizontalBar } from './HorizontalBar';

export function MatrixResultView({ matrix }: { matrix: SurveyMatrixResultsDto | null }) {
  if (!matrix) {
    return <p>No hay datos de matriz para esta pregunta.</p>;
  }

  if (matrix.rows.length === 0) {
    return <p>Sin filas de matriz.</p>;
  }

  return (
    <div className="matrix-result">
      {matrix.rows.map((row) => (
        <section className="matrix-result__row" key={row.rowId}>
          <header>
            <h4>{row.rowText}</h4>
            <span>{row.totalResponses} respuestas</span>
          </header>
          <div className="result-bars">
            {row.options.map((option) => (
              <HorizontalBar
                count={option.count}
                key={option.optionId}
                label={option.text}
                percentage={option.percentage}
              />
            ))}
          </div>
        </section>
      ))}
    </div>
  );
}
