import type { QuestionInputProps } from '../../types';

export function RatingScaleQuestion({ question, value, onChange }: QuestionInputProps) {
  if (
    question.ratingMin === null ||
    question.ratingMax === null ||
    question.ratingMin > question.ratingMax
  ) {
    return <p className="inline-message">Esta pregunta no tiene una escala configurada.</p>;
  }

  const ratingMin = question.ratingMin;
  const ratingMax = question.ratingMax;
  const values = Array.from(
    { length: ratingMax - ratingMin + 1 },
    (_, index) => ratingMin + index
  );

  return (
    <div className="rating-scale">
      {values.map((rating) => (
        <label className="rating-option" key={rating}>
          <input
            checked={value.numericValue === rating}
            name={`question-${question.id}`}
            onChange={() => onChange({ numericValue: rating })}
            type="radio"
            value={rating}
          />
          <span>{rating}</span>
        </label>
      ))}
    </div>
  );
}
