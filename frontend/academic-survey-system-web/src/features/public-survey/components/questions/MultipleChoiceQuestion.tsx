import type { QuestionInputProps } from '../../types';

export function MultipleChoiceQuestion({ question, value, onChange }: QuestionInputProps) {
  function toggleOption(optionId: string, checked: boolean) {
    onChange({
      optionIds: checked
        ? [...value.optionIds, optionId]
        : value.optionIds.filter((selectedId) => selectedId !== optionId)
    });
  }

  return (
    <div className="option-list">
      {question.options.map((option) => (
        <label className="choice-control" key={option.id}>
          <input
            checked={value.optionIds.includes(option.id)}
            onChange={(event) => toggleOption(option.id, event.target.checked)}
            type="checkbox"
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
              onChange={(event) =>
                onChange({
                  otherSelected: event.target.checked,
                  otherText: event.target.checked ? value.otherText : ''
                })
              }
              type="checkbox"
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
