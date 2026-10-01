import type { QuestionInputProps } from '../../types';

export function ShortTextQuestion({ value, onChange }: QuestionInputProps) {
  return (
    <input
      className="text-input"
      maxLength={500}
      onChange={(event) => onChange({ textValue: event.target.value })}
      type="text"
      value={value.textValue}
    />
  );
}
