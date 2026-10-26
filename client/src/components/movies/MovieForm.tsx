import { useState, type FormEvent, type ReactNode } from 'react';
import { ApiError, errorMessage } from '../../api/client';
import type { CreateMovieRequest } from '../../api/types';
import { useGenres } from '../../hooks/queries';
import { useFormState } from '../../hooks/useFormState';
import { toCreateMovieRequest, validateMovie, type MovieField, type MovieFormValues } from '../../lib/movieValidation';
import { isValidHttpUrl } from '../../lib/validation';
import { Button } from '../ui/Button';
import { Card, Poster } from '../ui/Controls';
import { TextAreaField, TextField } from '../ui/Field';
import { FormError } from '../ui/Feedback';
import { GenreMultiSelect } from '../ui/GenreMultiSelect';
import styles from './MovieForm.module.css';

export interface MovieFormAction<Intent extends string> {
  intent: Intent;
  label: string;
  busyLabel: string;
  /** The primary action is the form's submit button (Enter submits it). */
  primary?: boolean;
}

interface MovieFormProps<Intent extends string> {
  initial: MovieFormValues;
  /** Shown left to right after Cancel; put the primary action last. */
  actions: MovieFormAction<Intent>[];
  /** Saves the movie. Throwing shows the error on the form; field errors from a 400 go on their fields. */
  onSubmit: (request: CreateMovieRequest, intent: Intent) => Promise<void>;
  onCancel: () => void;
  /** The side card below the poster preview. */
  note: { title: string; content: ReactNode };
}

/** The movie details form shared by Add Movie and Edit Movie, with a live poster preview. */
export function MovieForm<Intent extends string>({ initial, actions, onSubmit, onCancel, note }: MovieFormProps<Intent>) {
  const genres = useGenres();
  const form = useFormState<MovieFormValues>(initial);
  const [submitting, setSubmitting] = useState<Intent | null>(null);
  const [formError, setFormError] = useState<string | null>(null);
  const [serverErrors, setServerErrors] = useState<Partial<Record<MovieField, string>>>({});

  const { values } = form;
  const errors = validateMovie(values);
  const shown = (field: MovieField) => (form.shouldShow(field) ? errors[field] : undefined) ?? serverErrors[field];

  function change<K extends MovieField>(field: K, value: MovieFormValues[K]) {
    form.setValue(field, value);
    setServerErrors((current) => ({ ...current, [field]: undefined }));
  }

  async function submit(intent: Intent) {
    form.setSubmitted(true);
    setFormError(null);
    if (Object.values(errors).some(Boolean)) {
      setFormError('Please fix the highlighted fields.');
      return;
    }
    setSubmitting(intent);
    try {
      // On success the caller navigates away, so the busy state is left as it is.
      await onSubmit(toCreateMovieRequest(values), intent);
    } catch (error) {
      setSubmitting(null);
      if (error instanceof ApiError && error.status === 400) {
        const fieldError = (name: string) => error.fieldError(name);
        setServerErrors({
          title: fieldError('title'),
          releaseYear: fieldError('releaseYear'),
          runtimeMinutes: fieldError('runtimeMinutes'),
          director: fieldError('director'),
          synopsis: fieldError('synopsis'),
          posterImageUrl: fieldError('posterImageUrl'),
          genreIds: fieldError('genreIds') ?? (/genre/i.test(error.message) ? error.message : undefined),
        });
      }
      setFormError(errorMessage(error));
    }
  }

  const primary = actions.find((action) => action.primary);

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (primary) {
      void submit(primary.intent);
    }
  }

  const posterUrl = values.posterImageUrl.trim();

  return (
    <div className={styles.layout}>
      <Card className={styles.formCard}>
        <h2 className={styles.cardTitle}>Movie details</h2>
        <p className={styles.cardSubtitle}>Fields marked * are required.</p>
        <form className={styles.form} onSubmit={handleSubmit} noValidate>
          <FormError error={formError} />
          <TextField
            label="Title"
            required
            maxLength={200}
            value={values.title}
            onChange={(e) => change('title', e.target.value)}
            onBlur={() => form.touch('title')}
            error={shown('title')}
          />
          <div className={styles.pair}>
            <TextField
              label="Release year"
              required
              inputMode="numeric"
              placeholder="e.g. 2023"
              value={values.releaseYear}
              onChange={(e) => change('releaseYear', e.target.value)}
              onBlur={() => form.touch('releaseYear')}
              error={shown('releaseYear')}
            />
            <TextField
              label="Runtime (minutes)"
              inputMode="numeric"
              placeholder="e.g. 106"
              value={values.runtimeMinutes}
              onChange={(e) => change('runtimeMinutes', e.target.value)}
              onBlur={() => form.touch('runtimeMinutes')}
              error={shown('runtimeMinutes')}
            />
          </div>
          <div className={styles.pair}>
            <TextField
              label="Director"
              maxLength={200}
              value={values.director}
              onChange={(e) => change('director', e.target.value)}
              onBlur={() => form.touch('director')}
              error={shown('director')}
            />
            <GenreMultiSelect
              label="Genres"
              required
              genres={genres.data ?? []}
              disabled={!genres.data}
              selected={values.genreIds}
              onChange={(ids) => change('genreIds', ids)}
              onClose={() => form.touch('genreIds')}
              error={shown('genreIds') ?? (genres.isError ? `Couldn't load genres. ${errorMessage(genres.error)}` : undefined)}
            />
          </div>
          <TextAreaField
            label="Synopsis"
            placeholder="Short, spoiler-free summary of the movie..."
            maxLength={2000}
            value={values.synopsis}
            onChange={(e) => change('synopsis', e.target.value)}
            onBlur={() => form.touch('synopsis')}
            error={shown('synopsis')}
          />
          <TextField
            label="Poster image URL"
            type="url"
            placeholder="https://..."
            value={values.posterImageUrl}
            onChange={(e) => change('posterImageUrl', e.target.value)}
            onBlur={() => form.touch('posterImageUrl')}
            hint="Optional. A placeholder poster is shown if left blank."
            error={shown('posterImageUrl')}
          />
          <div className={styles.actions}>
            <Button variant="secondary" onClick={onCancel} className={styles.cancel}>
              Cancel
            </Button>
            {actions.map((action) => (
              <Button
                key={action.intent}
                type={action.primary ? 'submit' : 'button'}
                variant={action.primary ? undefined : 'secondary'}
                onClick={action.primary ? undefined : () => void submit(action.intent)}
                busy={submitting === action.intent}
                busyLabel={action.busyLabel}
                disabled={submitting !== null}
              >
                {action.label}
              </Button>
            ))}
          </div>
        </form>
      </Card>

      <aside className={styles.side}>
        <Card>
          <h2 className={styles.sideTitle}>Poster preview</h2>
          <div className={styles.preview}>
            <Poster url={isValidHttpUrl(posterUrl) ? posterUrl : null} title={values.title.trim() || 'the new movie'} size="card" />
          </div>
        </Card>
        <Card>
          <h2 className={styles.sideTitle}>{note.title}</h2>
          <div className={styles.note}>{note.content}</div>
        </Card>
      </aside>
    </div>
  );
}
