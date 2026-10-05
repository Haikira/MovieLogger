import { useId, type InputHTMLAttributes, type ReactNode, type TextareaHTMLAttributes } from 'react';
import { CircleAlert } from 'lucide-react';
import styles from './Field.module.css';

interface FieldChromeProps {
  label: ReactNode;
  /** Shown under the control; replaced visually by the error when there is one. */
  hint?: ReactNode;
  error?: string;
  /** Adds " *" to the label; the control also gets aria-required. */
  required?: boolean;
  className?: string;
}

interface FieldIds {
  controlId: string;
  hintId: string;
  errorId: string;
}

function useFieldIds(id?: string): FieldIds {
  const generated = useId();
  const controlId = id ?? generated;
  return { controlId, hintId: `${controlId}-hint`, errorId: `${controlId}-error` };
}

function describedBy(ids: FieldIds, hint: ReactNode, error?: string): string | undefined {
  return [error && ids.errorId, hint && ids.hintId].filter(Boolean).join(' ') || undefined;
}

function FieldChrome({
  ids,
  label,
  hint,
  error,
  required,
  className,
  children,
}: FieldChromeProps & { ids: FieldIds; children: ReactNode }) {
  return (
    <div className={[styles.field, className].filter(Boolean).join(' ')}>
      <label htmlFor={ids.controlId} className={styles.label}>
        {label}
        {required && <span aria-hidden="true"> *</span>}
      </label>
      {children}
      {error && (
        <p id={ids.errorId} className={styles.error}>
          <CircleAlert size={14} aria-hidden="true" />
          {error}
        </p>
      )}
      {hint && (
        <p id={ids.hintId} className={styles.hint}>
          {hint}
        </p>
      )}
    </div>
  );
}

export type TextFieldProps = FieldChromeProps & Omit<InputHTMLAttributes<HTMLInputElement>, 'required'>;

export function TextField({ label, hint, error, required, className, id, ...inputProps }: TextFieldProps) {
  const ids = useFieldIds(id);
  return (
    <FieldChrome ids={ids} label={label} hint={hint} error={error} required={required} className={className}>
      <input
        id={ids.controlId}
        className={styles.control}
        aria-invalid={error ? true : undefined}
        aria-required={required || undefined}
        aria-describedby={describedBy(ids, hint, error)}
        {...inputProps}
      />
    </FieldChrome>
  );
}

export type TextAreaFieldProps = FieldChromeProps & Omit<TextareaHTMLAttributes<HTMLTextAreaElement>, 'required'>;

export function TextAreaField({ label, hint, error, required, className, id, ...textareaProps }: TextAreaFieldProps) {
  const ids = useFieldIds(id);
  return (
    <FieldChrome ids={ids} label={label} hint={hint} error={error} required={required} className={className}>
      <textarea
        id={ids.controlId}
        className={styles.control}
        aria-invalid={error ? true : undefined}
        aria-required={required || undefined}
        aria-describedby={describedBy(ids, hint, error)}
        {...textareaProps}
      />
    </FieldChrome>
  );
}
