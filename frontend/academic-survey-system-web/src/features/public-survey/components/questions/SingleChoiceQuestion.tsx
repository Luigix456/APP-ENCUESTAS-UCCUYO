import type { QuestionInputProps } from '../../types';

export function SingleChoiceQuestion({ question, value, onChange }: QuestionInputProps) {
  const name = `question-${question.id}`;

  return (
    <div className="option-list">
      {question.options.map((option) => (
        <label className="choice-control" key={option.id}>
          <input
            checked={!value.otherSelected && value.optionIds[0] === option.id}
            name={name}
            onChange={() =>
              onChange({
                optionIds: [option.id],
                otherSelected: false,
                otherText: ''
              })
            }
            type="radio"
            value={option.id}
          />
          <span>{option.text}</span>
        </label>
      ))}

      {question.allowsOtherOption ? (
        <div className="choice-control choice-control--stacked">
          <label>
            <input
              checked={value.otherSelected}
              name={name}
              onChange={() =>
                onChange({
                  optionIds: [],
                  otherSelected: true
                })
              }
              type="radio"
              value="other"
            />
            <span>Otro</span>
          </label>
          <input
            aria-label="Especificar otra respuesta"
            className="text-input"
            disabled={!value.otherSelected}
            onChange={(event) =>
              onChange({
                otherSelected: true,
                optionIds: [],
                otherText: event.target.value
              })
            }
            placeholder="Especificar"
            type="text"
            value={value.otherText}
          />
        </div>
      ) : null}
    </div>
  );
}
