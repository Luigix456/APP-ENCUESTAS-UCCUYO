interface DonutChartEntry {
  count: number;
  label: string;
  percentage: number;
}

const DONUT_COLORS = ['#214f68', '#2f806d', '#9d6c1f', '#6a5a9e', '#9d2636', '#526071'];

export function DonutChart({ entries, title }: { entries: DonutChartEntry[]; title: string }) {
  const visibleEntries = entries.filter((entry) => entry.percentage > 0);
  const totalResponses = entries.reduce((total, entry) => total + entry.count, 0);
  let offset = 0;

  return (
    <figure className="donut-chart">
      <div className="donut-chart__visual">
        <svg aria-label={title} className="donut-chart__svg" role="img" viewBox="0 0 42 42">
          <circle
            className="donut-chart__track"
            cx="21"
            cy="21"
            fill="transparent"
            r="15.9155"
            strokeWidth="5"
          />
          {visibleEntries.length > 0 ? (
            visibleEntries.map((entry, index) => {
              const currentOffset = offset;
              offset += entry.percentage;

              return (
                <circle
                  className="donut-chart__slice"
                  cx="21"
                  cy="21"
                  fill="transparent"
                  key={`${entry.label}-${index}`}
                  pathLength="100"
                  r="15.9155"
                  stroke={DONUT_COLORS[index % DONUT_COLORS.length]}
                  strokeDasharray={`${entry.percentage} ${100 - entry.percentage}`}
                  strokeDashoffset={-currentOffset}
                  strokeWidth="5"
                />
              );
            })
          ) : (
            <circle
              className="donut-chart__empty"
              cx="21"
              cy="21"
              fill="transparent"
              r="15.9155"
              strokeWidth="5"
            />
          )}
        </svg>

        <div aria-hidden="true" className="donut-chart__center">
          <strong>{totalResponses}</strong>
          <span>{totalResponses === 1 ? 'respuesta' : 'respuestas'}</span>
        </div>
      </div>
      <figcaption>Distribución de respuestas</figcaption>
    </figure>
  );
}
