import { useEffect, useId, useRef, useState } from 'react';
import { Check, ChevronDown, CircleAlert } from 'lucide-react';
import type { Genre } from '../../api/types';
import fieldStyles from './Field.module.css';
import styles from './GenreMultiSelect.module.css';

interface GenreMultiSelectProps {
  label: string;
  genres: Genre[];
  selected: number[];
  onChange: (selected: number[]) => void;
  onClose?: () => void;
  required?: boolean;
  error?: string;
  disabled?: boolean;
}

/**
 * The design's "Drama, Romance ⌄" dropdown: a disclosure button that opens a list of checkboxes.
 * Escape or clicking outside closes it.
 */
export function GenreMultiSelect({ label, genres, selected, onChange, onClose, required, error, disabled }: GenreMultiSelectProps) {
  const id = useId();
  const [open, setOpen] = useState(false);
  const containerRef = useRef<HTMLDivElement>(null);
  const buttonRef = useRef<HTMLButtonElement>(null);
  const onCloseRef = useRef(onClose);
  useEffect(() => {
    onCloseRef.current = onClose;
  });

  useEffect(() => {
    if (!open) {
      return;
    }
    function handlePointer(event: MouseEvent) {
      if (!containerRef.current?.contains(event.target as Node)) {
        setOpen(false);
        onCloseRef.current?.();
      }
    }
    document.addEventListener('mousedown', handlePointer);
    return () => document.removeEventListener('mousedown', handlePointer);
  }, [open]);

  function close() {
    setOpen(false);
    onClose?.();
    buttonRef.current?.focus();
  }

  function toggle(genreId: number) {
    onChange(selected.includes(genreId) ? selected.filter((g) => g !== genreId) : [...selected, genreId]);
  }

  const selectedNames = genres.filter((g) => selected.includes(g.id)).map((g) => g.name);
  const summary = selectedNames.length ? selectedNames.join(', ') : 'Select genres';
  const describedBy = error ? `${id}-error` : undefined;

  return (
    <div className={fieldStyles.field} ref={containerRef}>
      <label htmlFor={`${id}-button`} className={fieldStyles.label}>
        {label}
        {required && <span aria-hidden="true"> *</span>}
      </label>
      <div className={styles.wrapper}>
        <button
          ref={buttonRef}
          id={`${id}-button`}
          type="button"
          className={`${fieldStyles.control} ${styles.button}`}
          aria-expanded={open}
          aria-controls={`${id}-list`}
          aria-invalid={error ? true : undefined}
          aria-describedby={describedBy}
          disabled={disabled}
          onClick={() => (open ? close() : setOpen(true))}
        >
          <span className={selectedNames.length ? styles.value : styles.placeholder}>{summary}</span>
          <ChevronDown size={18} aria-hidden="true" className={styles.chevron} />
        </button>
        {open && (
          <fieldset
            id={`${id}-list`}
            className={styles.panel}
            onKeyDown={(event) => {
              if (event.key === 'Escape') {
                event.stopPropagation();
                close();
              }
            }}
          >
            <legend className="visually-hidden">{label}</legend>
            {genres.map((genre) => {
              const checked = selected.includes(genre.id);
              return (
                <label key={genre.id} className={styles.option}>
                  <input
                    type="checkbox"
                    checked={checked}
                    onChange={() => toggle(genre.id)}
                    className="visually-hidden"
                  />
                  <span className={checked ? `${styles.box} ${styles.boxChecked}` : styles.box} aria-hidden="true">
                    {checked && <Check size={12} strokeWidth={3} />}
                  </span>
                  {genre.name}
                </label>
              );
            })}
          </fieldset>
        )}
      </div>
      {error && (
        <p id={`${id}-error`} className={fieldStyles.error}>
          <CircleAlert size={14} aria-hidden="true" />
          {error}
        </p>
      )}
    </div>
  );
}
