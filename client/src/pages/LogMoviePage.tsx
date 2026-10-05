import { useState, type FormEvent, type ReactNode } from 'react';
import { Link, useLocation, useNavigate, useParams, useSearchParams } from 'react-router';
import { useQuery } from '@tanstack/react-query';
import { Clapperboard, Info } from 'lucide-react';
import { ApiError, errorMessage, isApiError } from '../api/client';
import { movieWatchesApi } from '../api/endpoints';
import { queryKeys } from '../api/queryKeys';
import type { Movie, MovieWatch, MovieWatchRequest, UserMovieHistory } from '../api/types';
import { PageHeader } from '../components/layout/PageHeader';
import { MoviePicker } from '../components/movies/MoviePicker';
import { Button, ButtonLink } from '../components/ui/Button';
import { Card, Poster, Stars } from '../components/ui/Controls';
import { ConfirmDialog, Dialog } from '../components/ui/Dialog';
import { TextAreaField, TextField } from '../components/ui/Field';
import { EmptyState, ErrorState, FormError, LoadingState } from '../components/ui/Feedback';
import { RatingInput } from '../components/ui/RatingInput';
import {
  useInvalidateWatchData,
  useMovieDetails,
  useWatchlistMovieIds,
  useWatchlistMutations,
} from '../hooks/queries';
import { useGoBack } from '../hooks/useBackLink';
import { formatCalendarDate, toCalendarDate, todayCalendarDate } from '../lib/dates';
import { formatMovieMeta } from '../lib/format';
import { NOTES_MAX_LENGTH, validateLog, type LogField, type LogFormValues } from '../lib/logValidation';
import styles from './LogMoviePage.module.css';

export function LogMoviePage() {
  const { watchId } = useParams();
  const location = useLocation();
  // Keyed by path so switching between "log" and "edit" starts from a fresh form.
  return watchId ? <EditLog key={location.pathname} watchId={Number(watchId)} /> : <CreateLog key={location.pathname} />;
}

// ---------------------------------------------------------------------------------------------
// Create
// ---------------------------------------------------------------------------------------------

function CreateLog() {
  const navigate = useNavigate();
  const [params] = useSearchParams();
  const fromWatchlist = params.get('from') === 'watchlist';
  const initialMovieId = Number(params.get('movieId'));
  const [movieId, setMovieId] = useState<number | null>(
    Number.isInteger(initialMovieId) && initialMovieId > 0 ? initialMovieId : null,
  );
  const [picking, setPicking] = useState(movieId === null);
  const [movieError, setMovieError] = useState<string | undefined>();
  const details = useMovieDetails(movieId);
  const invalidate = useInvalidateWatchData();
  const watchlistIds = useWatchlistMovieIds();
  const [saved, setSaved] = useState<Movie | null>(null);
  const goBack = useGoBack(fromWatchlist ? '/watchlist' : '/my-movies');

  const movie = details.data?.movie ?? null;
  const destinationAfterSave = (m: Movie) => (fromWatchlist ? '/watchlist' : `/movies/${m.id}`);

  async function handleSubmit(values: MovieWatchRequest) {
    if (!movie) {
      setMovieError('Choose the movie you watched.');
      setPicking(true);
      return;
    }
    await movieWatchesApi.create({ ...values, movieId: movie.id });
    await invalidate();
    if (watchlistIds.has(movie.id)) {
      // Logging doesn't remove the movie from the watchlist; ask instead of doing it silently.
      setSaved(movie);
    } else {
      navigate(destinationAfterSave(movie));
    }
  }

  let movieSlot: ReactNode;
  if (picking) {
    movieSlot = (
      <MoviePicker
        error={movieError}
        onCancel={movieId !== null ? () => setPicking(false) : undefined}
        onSelect={(selected) => {
          setMovieId(selected.id);
          setMovieError(undefined);
          setPicking(false);
        }}
      />
    );
  } else if (details.isPending) {
    movieSlot = <LoadingState label="Loading movie…" />;
  } else if (details.isError) {
    movieSlot = isApiError(details.error, 404) ? (
      <p className={styles.slotError}>
        That movie isn&apos;t in the catalogue.{' '}
        <Button variant="link" onClick={() => setPicking(true)}>
          Choose another movie
        </Button>
      </p>
    ) : (
      <ErrorState error={details.error} title="Couldn't load the movie" onRetry={() => details.refetch()} />
    );
  } else {
    movieSlot = <SelectedMovie movie={details.data.movie} onChange={() => setPicking(true)} />;
  }

  return (
    <>
      <PageHeader title="Log Movie" subtitle="Record that you watched a movie. Rating and notes are optional." />
      <div className={styles.layout}>
        <LogForm
          movieSlot={movieSlot}
          movieHint={
            <>
              Only movies already in the catalogue can be logged. Can&apos;t find it? <Link to="/movies/new">Add it first</Link>.
            </>
          }
          initial={{ dateWatched: todayCalendarDate(), rating: null, notes: '' }}
          submitLabel="Save Log"
          onSubmit={handleSubmit}
          onCancel={goBack}
        />
        <aside className={styles.side}>
          {movie && details.data && <HistoryPanel movie={movie} history={details.data.userHistory} />}
          <Card>
            <h2 className={styles.sideTitle}>
              <Info size={18} aria-hidden="true" className={styles.sideIcon} />
              After saving
            </h2>
            <p className={styles.sideText}>
              This log appears in My Movies and under Recently Watched on your Dashboard. You can edit or delete it later.
            </p>
          </Card>
        </aside>
      </div>

      {saved && (
        <RemoveFromWatchlistPrompt
          movie={saved}
          onDone={() => navigate(destinationAfterSave(saved))}
        />
      )}
    </>
  );
}

function RemoveFromWatchlistPrompt({ movie, onDone }: { movie: Movie; onDone: () => void }) {
  const { remove } = useWatchlistMutations();
  const [error, setError] = useState<string | null>(null);

  async function handleRemove() {
    setError(null);
    try {
      await remove.mutateAsync(movie.id);
      onDone();
    } catch (err) {
      setError(errorMessage(err));
    }
  }

  return (
    <Dialog
      title="Log saved"
      description={`${movie.title} is still on your watchlist. Remove it now that you've watched it?`}
      onClose={onDone}
      busy={remove.isPending}
      footer={
        <>
          <Button variant="secondary" onClick={onDone} disabled={remove.isPending}>
            Keep on Watchlist
          </Button>
          <Button onClick={handleRemove} busy={remove.isPending} busyLabel="Removing…">
            Remove from Watchlist
          </Button>
        </>
      }
    >
      <FormError error={error} />
    </Dialog>
  );
}

// ---------------------------------------------------------------------------------------------
// Edit
// ---------------------------------------------------------------------------------------------

function EditLog({ watchId }: { watchId: number }) {
  const navigate = useNavigate();
  const validId = Number.isInteger(watchId) && watchId > 0;
  const watch = useQuery({
    queryKey: queryKeys.movieWatch(watchId),
    queryFn: () => movieWatchesApi.get(watchId),
    enabled: validId,
  });
  const details = useMovieDetails(watch.data?.movieId ?? null);
  const invalidate = useInvalidateWatchData();
  const [confirmingDelete, setConfirmingDelete] = useState(false);
  const [deleteBusy, setDeleteBusy] = useState(false);
  const [deleteError, setDeleteError] = useState<string | null>(null);

  const movieDetailsPath = watch.data ? `/movies/${watch.data.movieId}` : '/my-movies';
  const goBack = useGoBack(movieDetailsPath);

  let body: ReactNode;
  if (!validId || (watch.isError && isApiError(watch.error, 404))) {
    body = (
      <Card>
        <EmptyState
          icon={<Clapperboard size={22} />}
          title="Log not found"
          description="This log doesn't exist or has been deleted."
          actions={<ButtonLink to="/my-movies">Go to My Movies</ButtonLink>}
        />
      </Card>
    );
  } else if (watch.isError) {
    body = (
      <Card>
        <ErrorState error={watch.error} title="Couldn't load this log" onRetry={() => watch.refetch()} />
      </Card>
    );
  } else if (watch.isPending || details.isPending) {
    body = (
      <Card>
        <LoadingState label="Loading log…" />
      </Card>
    );
  } else if (details.isError) {
    body = (
      <Card>
        <ErrorState error={details.error} title="Couldn't load the movie" onRetry={() => details.refetch()} />
      </Card>
    );
  } else {
    const current: MovieWatch = watch.data;
    const movie = details.data.movie;

    const handleSubmit = async (values: MovieWatchRequest) => {
      await movieWatchesApi.update(current.id, values);
      await invalidate();
      navigate(movieDetailsPath);
    };

    const handleDelete = async () => {
      setDeleteBusy(true);
      setDeleteError(null);
      try {
        await movieWatchesApi.remove(current.id);
      } catch (error) {
        if (!isApiError(error, 404)) {
          setDeleteError(errorMessage(error));
          setDeleteBusy(false);
          return;
        }
      }
      await invalidate();
      navigate(movieDetailsPath, { replace: true });
    };

    body = (
      <div className={styles.layout}>
        <LogForm
          movieSlot={<SelectedMovie movie={movie} />}
          initial={{
            dateWatched: toCalendarDate(current.dateWatched),
            rating: current.rating,
            notes: current.notes ?? '',
          }}
          submitLabel="Save Changes"
          onSubmit={handleSubmit}
          onCancel={goBack}
          extraActions={
            <Button variant="danger" onClick={() => setConfirmingDelete(true)}>
              Delete Log
            </Button>
          }
          notFoundMessage="This log no longer exists. It may have been deleted."
        />
        <aside className={styles.side}>
          <HistoryPanel movie={movie} history={details.data.userHistory} />
        </aside>
        {confirmingDelete && (
          <ConfirmDialog
            destructive
            title="Delete this log?"
            description={`Your log of ${movie.title} from ${formatCalendarDate(current.dateWatched)} will be permanently deleted.`}
            confirmLabel="Delete Log"
            busyLabel="Deleting…"
            busy={deleteBusy}
            error={deleteError}
            onConfirm={handleDelete}
            onCancel={() => setConfirmingDelete(false)}
          />
        )}
      </div>
    );
  }

  return (
    <>
      <PageHeader title="Edit Log" subtitle="Change the date, rating or notes for this viewing." />
      {body}
    </>
  );
}

// ---------------------------------------------------------------------------------------------
// Shared pieces
// ---------------------------------------------------------------------------------------------

interface LogFormProps {
  movieSlot: ReactNode;
  movieHint?: ReactNode;
  initial: LogFormValues;
  submitLabel: string;
  onSubmit: (values: MovieWatchRequest) => Promise<void>;
  onCancel: () => void;
  extraActions?: ReactNode;
  /** Shown if the API answers 404 (e.g. the log was deleted in another tab). */
  notFoundMessage?: string;
}

function LogForm({
  movieSlot,
  movieHint,
  initial,
  submitLabel,
  onSubmit,
  onCancel,
  extraActions,
  notFoundMessage,
}: LogFormProps) {
  const [values, setValues] = useState(initial);
  const [submitted, setSubmitted] = useState(false);
  const [submitting, setSubmitting] = useState(false);
  const [formError, setFormError] = useState<string | null>(null);
  const [serverErrors, setServerErrors] = useState<Partial<Record<LogField, string>>>({});
  const today = todayCalendarDate();

  const errors = validateLog(values);
  const shown = (field: LogField) => (submitted ? errors[field] : undefined) ?? serverErrors[field];

  function update<K extends LogField>(field: K, value: LogFormValues[K]) {
    setValues((current) => ({ ...current, [field]: value }));
    setServerErrors((current) => ({ ...current, [field]: undefined }));
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setSubmitted(true);
    setFormError(null);
    if (Object.values(errors).some(Boolean)) {
      return;
    }
    setSubmitting(true);
    try {
      const notes = values.notes.trim();
      await onSubmit({ dateWatched: values.dateWatched, rating: values.rating, notes: notes || null });
    } catch (error) {
      if (error instanceof ApiError && error.status === 400 && Object.keys(error.fieldErrors).length) {
        setServerErrors({
          dateWatched: error.fieldError('dateWatched'),
          rating: error.fieldError('rating'),
          notes: error.fieldError('notes'),
        });
        setFormError('Please fix the highlighted fields.');
      } else if (isApiError(error, 404) && notFoundMessage) {
        setFormError(notFoundMessage);
      } else {
        setFormError(errorMessage(error));
      }
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <Card className={styles.formCard}>
      <form className={styles.form} onSubmit={handleSubmit} noValidate>
        <FormError error={formError} />
        <div className={styles.movieField}>
          <span className={styles.label} id="log-movie-label">
            Movie <span aria-hidden="true">*</span>
          </span>
          <div role="group" aria-labelledby="log-movie-label">
            {movieSlot}
          </div>
          {movieHint && <p className={styles.hint}>{movieHint}</p>}
        </div>

        <TextField
          label="Date watched"
          required
          type="date"
          max={today}
          value={values.dateWatched}
          onChange={(e) => update('dateWatched', e.target.value)}
          hint="Defaults to today. Future dates aren't allowed."
          error={shown('dateWatched')}
        />

        <RatingInput value={values.rating} onChange={(rating) => update('rating', rating)} error={shown('rating')} />

        <TextAreaField
          label="Notes (optional)"
          placeholder="What did you think? Anything worth remembering?"
          value={values.notes}
          maxLength={NOTES_MAX_LENGTH}
          onChange={(e) => update('notes', e.target.value)}
          hint={`${values.notes.length} / ${NOTES_MAX_LENGTH} characters`}
          error={shown('notes')}
        />

        <div className={styles.actions}>
          {extraActions && <div className={styles.extraActions}>{extraActions}</div>}
          <Button variant="secondary" onClick={onCancel}>
            Cancel
          </Button>
          <Button type="submit" busy={submitting} busyLabel="Saving…">
            {submitLabel}
          </Button>
        </div>
      </form>
    </Card>
  );
}

function SelectedMovie({ movie, onChange }: { movie: Movie; onChange?: () => void }) {
  return (
    <div className={styles.selected}>
      <Poster url={movie.posterImageUrl} title={movie.title} size="thumb" />
      <div className={styles.selectedText}>
        <span className={styles.selectedTitle}>{movie.title}</span>
        <span className={styles.selectedMeta}>{formatMovieMeta(movie)}</span>
      </div>
      {onChange && (
        <Button variant="link" onClick={onChange} aria-label={`Change movie (currently ${movie.title})`}>
          Change
        </Button>
      )}
    </div>
  );
}

function HistoryPanel({ movie, history }: { movie: Movie; history: UserMovieHistory | null }) {
  return (
    <Card>
      <h2 className={styles.sideTitle}>Your history with {movie.title}</h2>
      <dl className={styles.historyList}>
        <div>
          <dt>Times watched</dt>
          <dd>{history?.timesWatched ?? 0}</dd>
        </div>
        <div>
          <dt>Last watched</dt>
          <dd>{history?.lastWatchedAt ? formatCalendarDate(history.lastWatchedAt) : 'Not yet'}</dd>
        </div>
        <div>
          <dt>Last rating</dt>
          <dd>
            <Stars rating={history?.lastRating} emptyText="—" />
          </dd>
        </div>
      </dl>
      <Link to={`/movies/${movie.id}`} className={styles.sideLink}>
        View movie details
      </Link>
    </Card>
  );
}
