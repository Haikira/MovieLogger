import { useState, type FormEvent } from 'react';
import { useNavigate, useSearchParams } from 'react-router';
import { useQueryClient } from '@tanstack/react-query';
import { ApiError, errorMessage } from '../api/client';
import { moviesApi } from '../api/endpoints';
import { queryKeys } from '../api/queryKeys';
import { PageHeader } from '../components/layout/PageHeader';
import { Button } from '../components/ui/Button';
import { Card, Poster } from '../components/ui/Controls';
import { TextAreaField, TextField } from '../components/ui/Field';
import { FormError } from '../components/ui/Feedback';
import { GenreMultiSelect } from '../components/ui/GenreMultiSelect';
import { useGenres } from '../hooks/queries';
import { useGoBack } from '../hooks/useBackLink';
import { useFormState } from '../hooks/useFormState';
import { toCreateMovieRequest, validateMovie, type MovieField, type MovieFormValues } from '../lib/movieValidation';
import { isValidHttpUrl } from '../lib/validation';
import styles from './AddMoviePage.module.css';

type Intent = 'add' | 'add-and-log';

export function AddMoviePage() {
  const navigate = useNavigate();
  const goBack = useGoBack('/search');
  const [params] = useSearchParams();
  const queryClient = useQueryClient();
  const genres = useGenres();
  const form = useFormState<MovieFormValues>({
    title: params.get('title') ?? '',
    releaseYear: '',
    runtimeMinutes: '',
    director: '',
    genreIds: [],
    synopsis: '',
    posterImageUrl: '',
  });
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
      const movie = await moviesApi.create(toCreateMovieRequest(values));
      await queryClient.invalidateQueries({ queryKey: queryKeys.movies });
      navigate(intent === 'add-and-log' ? `/log?movieId=${movie.id}` : `/movies/${movie.id}`, { replace: true });
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

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    void submit('add');
  }

  const posterUrl = values.posterImageUrl.trim();

  return (
    <>
      <PageHeader title="Add Movie" subtitle="Add an existing movie to the Movie Logger catalogue so it can be searched and logged." />
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
              <Button variant="secondary" onClick={goBack} className={styles.cancel}>
                Cancel
              </Button>
              <Button
                variant="secondary"
                onClick={() => void submit('add-and-log')}
                busy={submitting === 'add-and-log'}
                busyLabel="Adding…"
                disabled={submitting !== null}
              >
                Add &amp; Log Movie
              </Button>
              <Button type="submit" busy={submitting === 'add'} busyLabel="Adding…" disabled={submitting !== null}>
                Add to Catalogue
              </Button>
            </div>
          </form>
        </Card>

        <aside className={styles.side}>
          <Card>
            <h2 className={styles.sideTitle}>Poster preview</h2>
            <div className={styles.preview}>
              <Poster
                url={isValidHttpUrl(posterUrl) ? posterUrl : null}
                title={values.title.trim() || 'the new movie'}
                size="card"
              />
            </div>
          </Card>
          <Card>
            <h2 className={styles.sideTitle}>What happens next</h2>
            <ol className={styles.steps}>
              <li>The movie is added to the shared catalogue.</li>
              <li>Anyone can find it in Search.</li>
              <li>Log it with a date, rating and notes whenever you watch it.</li>
            </ol>
          </Card>
        </aside>
      </div>
    </>
  );
}
