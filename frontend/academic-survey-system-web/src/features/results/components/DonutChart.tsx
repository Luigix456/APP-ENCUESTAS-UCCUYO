import { formatPercent } from '../resultsUi';

interface DonutChartEntry {
  count: number;
  label: string;
  percentage: number;
}

const DONUT_COLORS = ['#214f68', '#2f806d', '#9d6c1f', '#6a5a9e', '#9d2636', '#526071'];

export function DonutChart({ entries, title }: { entries: DonutChartEntry[]; title: string }) {
  const visibleEntries = entries.filter((entry) => entry.percentage > 0);
  let offset = 0;

  return (
    <div className="donut-chart">
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
      <div className="donut-chart__legend">
        {entries.map((entry, index) => (
          <div className="donut-chart__legend-item" key={`${entry.label}-${index}`}>
            <span
              aria-hidden="true"
              style={{ backgroundColor: DONUT_COLORS[index % DONUT_COLORS.length] }}
            />
            <p>
              <strong>{entry.label}</strong>
              <small>
                {entry.count} respuestas · {formatPercent(entry.percentage)}%
              </small>
            </p>
          </div>
        ))}
      </div>
    </div>
  );
}
