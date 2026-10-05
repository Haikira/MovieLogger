import { useId, useState } from 'react';
import { Star } from 'lucide-react';
import { Button } from './Button';
import styles from './RatingInput.module.css';

interface RatingInputProps {
  value: number | null;
  onChange: (value: number | null) => void;
  legend?: string;
  error?: string;
}

/**
 * Optional 1–5 star rating. Built from native radio buttons, so it works with the keyboard
 * (arrow keys) and screen readers; the stars are the radios' visible labels.
 */
export function RatingInput({ value, onChange, legend = 'Rating (optional)', error }: RatingInputProps) {
  const name = useId();
  const hintId = `${name}-hint`;
  const errorId = `${name}-error`;
  const [hovered, setHovered] = useState<number | null>(null);
  const shown = hovered ?? value ?? 0;

  return (
    <fieldset className={styles.fieldset} aria-describedby={[error && errorId, hintId].filter(Boolean).join(' ')}>
      <legend className={styles.legend}>{legend}</legend>
      <div className={styles.row}>
        <div className={styles.stars} onMouseLeave={() => setHovered(null)}>
          {[1, 2, 3, 4, 5].map((n) => (
            <label key={n} className={styles.star} onMouseEnter={() => setHovered(n)}>
              <input
                type="radio"
                name={name}
                value={n}
                checked={value === n}
                onChange={() => onChange(n)}
                className="visually-hidden"
              />
              <Star
                size={30}
                aria-hidden="true"
                fill={n <= shown ? 'currentColor' : 'none'}
                strokeWidth={n <= shown ? 1.5 : 1.25}
                className={n <= shown ? styles.filled : styles.empty}
              />
              <span className="visually-hidden">
                {n} {n === 1 ? 'star' : 'stars'}
              </span>
            </label>
          ))}
        </div>
        <span className={styles.value} aria-hidden="true">
          {value ? `${value} / 5` : ''}
        </span>
        {value !== null && (
          <Button variant="link" onClick={() => onChange(null)} aria-label="Clear rating">
            Clear
          </Button>
        )}
      </div>
      {error && (
        <p id={errorId} className={styles.error}>
          {error}
        </p>
      )}
      <p id={hintId} className={styles.hint}>
        Select a star to rate. Leave empty to skip.
      </p>
    </fieldset>
  );
}
