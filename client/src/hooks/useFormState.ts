import { useCallback, useState } from 'react';

/**
 * Minimal form state: values, which fields the user has left (touched), and whether a submit
 * was attempted. Validation is a pure function of the values; an error is shown once its field
 * has been touched or a submit attempted, and then updates as the user types.
 */
export function useFormState<T extends object>(initial: T) {
  const [values, setValues] = useState<T>(initial);
  const [touched, setTouched] = useState<Partial<Record<keyof T, boolean>>>({});
  const [submitted, setSubmitted] = useState(false);

  const setValue = useCallback(<K extends keyof T>(key: K, value: T[K]) => {
    setValues((current) => ({ ...current, [key]: value }));
  }, []);

  const touch = useCallback((key: keyof T) => {
    setTouched((current) => (current[key] ? current : { ...current, [key]: true }));
  }, []);

  const shouldShow = useCallback((key: keyof T) => submitted || Boolean(touched[key]), [submitted, touched]);

  const reset = useCallback((next: T) => {
    setValues(next);
    setTouched({});
    setSubmitted(false);
  }, []);

  return { values, setValue, setValues, touch, shouldShow, submitted, setSubmitted, reset };
}
