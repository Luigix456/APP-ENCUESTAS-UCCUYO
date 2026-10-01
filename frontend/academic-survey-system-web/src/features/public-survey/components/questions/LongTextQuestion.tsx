import type { QuestionInputProps } from '../../types';

export function LongTextQuestion({ value, onChange }: QuestionInputProps) {
  return (
    <textarea
      className="text-area"
      maxLength={2000}
      onChange={(event) => onChange({ textValue: event.target.value })}
      rows={5}
      value={value.textValue}
    />
  );
}
