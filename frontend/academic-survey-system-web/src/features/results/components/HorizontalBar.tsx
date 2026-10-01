import { formatPercent } from '../resultsUi';

interface HorizontalBarProps {
  label: string;
  count: number;
  percentage: number;
}

export function HorizontalBar({ count, label, percentage }: HorizontalBarProps) {
  const width = `${Math.max(0, Math.min(100, percentage))}%`;

  return (
    <div className="horizontal-bar">
      <div className="horizontal-bar__header">
        <span>{label}</span>
        <strong>{formatPercent(percentage)}%</strong>
      </div>
      <div className="horizontal-bar__track" aria-hidden="true">
        <div className="horizontal-bar__fill" style={{ width }} />
      </div>
      <small>{count} respuestas</small>
    </div>
  );
}
