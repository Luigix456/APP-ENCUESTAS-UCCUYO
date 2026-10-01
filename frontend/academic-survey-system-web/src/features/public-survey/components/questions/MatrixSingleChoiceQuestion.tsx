import type { QuestionInputProps } from '../../types';

export function MatrixSingleChoiceQuestion({ question, value, onChange }: QuestionInputProps) {
  return (
    <div className="matrix-question">
      <div className="matrix-question__header" aria-hidden="true">
        <span />
        {question.options.map((option) => (
          <span key={option.id}>{option.text}</span>
        ))}
      </div>

      {question.matrixRows.map((row) => (
        <fieldset className="matrix-question__row" key={row.id}>
          <legend>{row.text}</legend>
          <div className="matrix-question__options">
            {question.options.map((option) => (
              <label key={option.id}>
                <input
                  checked={value.matrixAnswers[row.id] === option.id}
                  name={`question-${question.id}-row-${row.id}`}
                  onChange={() =>
                    onChange({
                      matrixAnswers: {
                        ...value.matrixAnswers,
                        [row.id]: option.id
                      }
                    })
                  }
                  type="radio"
                  value={option.id}
                />
                <span>{option.text}</span>
              </label>
            ))}
          </div>
        </fieldset>
      ))}
    </div>
  );
}
