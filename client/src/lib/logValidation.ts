import { isFutureCalendarDate, isValidCalendarDate } from './dates';

export const NOTES_MAX_LENGTH = 500;

export interface LogFormValues {
  /** "YYYY-MM-DD" */
  dateWatched: string;
  rating: number | null;
  notes: string;
}

export type LogField = 'dateWatched' | 'rating' | 'notes';

/** Mirrors the API's rules for a watch log (NotInFuture date, Range(1, 5) rating, 500-char notes). */
export function validateLog(values: LogFormValues, now: Date = new Date()): Partial<Record<LogField, string>> {
  const errors: Partial<Record<LogField, string>> = {};
  if (!values.dateWatched) {
    errors.dateWatched = 'Enter the date you watched it.';
  } else if (!isValidCalendarDate(values.dateWatched)) {
    errors.dateWatched = 'Enter a valid date.';
  } else if (isFutureCalendarDate(values.dateWatched, now)) {
    errors.dateWatched = "The date watched can't be in the future.";
  }
  if (values.rating !== null && (!Number.isInteger(values.rating) || values.rating < 1 || values.rating > 5)) {
    errors.rating = 'Rating must be between 1 and 5 stars.';
  }
  if (values.notes.length > NOTES_MAX_LENGTH) {
    errors.notes = `Notes must be ${NOTES_MAX_LENGTH} characters or fewer.`;
  }
  return errors;
}
