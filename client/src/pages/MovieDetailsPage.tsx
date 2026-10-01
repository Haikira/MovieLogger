import { useEffect, useRef, useState } from 'react';
import { Link, useLocation, useParams } from 'react-router';
import { ArrowLeft, Clapperboard, History } from 'lucide-react';
import { errorMessage, isApiError } from '../api/client';
import { movieWatchesApi } from '../api/endpoints';
import type { Movie, MovieWatch, UserMovieHistory } from '../api/types';
import { useCurrentUser } from '../auth/useAuth';
import { Button, ButtonLink } from '../components/ui/Button';
import { Card, Poster, Stars } from '../components/ui/Controls';
import { ConfirmDialog } from '../components/ui/Dialog';
import { Alert, EmptyState, ErrorState, LoadingState } from '../components/ui/Feedback';
import { useInvalidateWatchData, useMovieDetails, useWatchlistMovieIds, useWatchlistMutations } from '../hooks/queries';
import { useBackLink, useBackState } from '../hooks/useBackLink';
import { usePageTitle } from '../hooks/usePageTitle';
import { formatCalendarDate, formatTimestampDate } from '../lib/dates';
import styles from './MovieDetailsPage.module.css';

export function MovieDetailsPage() {
  const params = useParams();
  const movieId = Number(params.movieId);
  const validId = Number.isInteger(movieId) && movieId > 0;
  const details = useMovieDetails(validId ? movieId : null);
  const back = useBackLink({ to: '/search', label: 'Back to search' });

  let content;
  if (!validId || (details.isError && isApiError(details.error, 404))) {
    content = (
      <Card>
        <EmptyState
          icon={<Clapperboard size={22} />}
          title="Movie not found"
          description="This movie isn't in the catalogue. It may have been removed."
          actions={<ButtonLink to="/search">Search the Catalogue</ButtonLink>}
        />
      </Card>
    );
  } else if (details.isPending) {
    content = (
      <Card>
        <LoadingState label="Loading movie…" />
      </Card>
    );
  } else if (details.isError) {
    content = (
      <Card>
        <ErrorState error={details.error} title="Couldn't load this movie" onRetry={() => details.refetch()} />
      </Card>
    );
  } else {
    content = <MovieDetailsContent movie={details.data.movie} history={details.data.userHistory} />;
  }

  return (
    <>
      <Link to={back.to} className={styles.back}>
        <ArrowLeft size={16} aria-hidden="true" />
        {back.label}
      </Link>
      {content}
    </>
  );
}

function MovieDetailsContent({ movie, history }: { movie: Movie; history: UserMovieHistory | null }) {
  usePageTitle(movie.title);
  const user = useCurrentUser();
  const location = useLocation();
  const headingRef = useRef<HTMLHeadingElement>(null);
  const historyHeadingRef = useRef<HTMLHeadingElement>(null);
  const logBackState = useBackState(`Back to ${movie.title}`);

  const watchlistIds = useWatchlistMovieIds();
  const { add, remove } = useWatchlistMutations();
  const onWatchlist = watchlistIds.has(movie.id);
  const [watchlistError, setWatchlistError] = useState<string | null>(null);

  // Arriving from "Edit" on My Movies (#history) lands on the history; otherwise on the title.
  useEffect(() => {
    if (location.hash === '#history' && historyHeadingRef.current) {
      historyHeadingRef.current.scrollIntoView?.({ block: 'start' });
      historyHeadingRef.current.focus({ preventScroll: true });
    } else {
      headingRef.current?.focus({ preventScroll: true });
    }
  }, [location.hash]);

  async function toggleWatchlist() {
    setWatchlistError(null);
    try {
      await (onWatchlist ? remove : add).mutateAsync(movie.id);
    } catch (error) {
      setWatchlistError(errorMessage(error));
    }
  }

  const metaParts = [String(movie.releaseYear)];
  if (movie.runtimeMinutes) metaParts.push(`${movie.runtimeMinutes} min`);
  if (movie.director) metaParts.push(`Directed by ${movie.director}`);

  const logs = history?.logs ?? [];
  const watchlistBusy = add.isPending || remove.isPending;

  return (
    <>
      <Card className={styles.hero} as="section" labelledBy="movie-title">
        <Poster url={movie.posterImageUrl} title={movie.title} size="large" />
        <div className={styles.info}>
          <h1 id="movie-title" ref={headingRef} tabIndex={-1} className={styles.title}>
            {movie.title}
          </h1>
          <p className={styles.meta}>{metaParts.join(' · ')}</p>

          {movie.genres.length > 0 && (
            <ul className={styles.genres} aria-label="Genres">
              {movie.genres.map((genre) => (
                <li key={genre.id} className={styles.genre}>
                  {genre.name}
                </li>
              ))}
            </ul>
          )}

          {movie.synopsis && (
            <div className={styles.synopsis}>
              <h2 className={styles.synopsisHeading}>Synopsis</h2>
              <p>{movie.synopsis}</p>
            </div>
          )}

          <dl className={styles.stats}>
            <div>
              <dt>Times watched</dt>
              <dd>{history?.timesWatched ?? 0}</dd>
            </div>
            <div>
              <dt>Last watched</dt>
              <dd>{history?.lastWatchedAt ? formatCalendarDate(history.lastWatchedAt) : 'Not yet'}</dd>
            </div>
            <div>
              <dt>Your last rating</dt>
              <dd>
                <Stars rating={history?.lastRating} size={18} emptyText={history?.timesWatched ? 'No rating' : '—'} />
              </dd>
            </div>
          </dl>

          <div className={styles.actions}>
            <ButtonLink to={`/log?movieId=${movie.id}`} state={logBackState}>
              Log Movie
            </ButtonLink>
            <Button
              variant="secondary"
              onClick={toggleWatchlist}
              aria-pressed={onWatchlist}
              busy={watchlistBusy}
              busyLabel={onWatchlist ? 'Removing…' : 'Adding…'}
            >
              {onWatchlist ? 'Remove from Watchlist' : 'Add to Watchlist'}
            </Button>
          </div>
          <div className="live-region" aria-live="assertive">{watchlistError && <Alert tone="error">{watchlistError}</Alert>}</div>

          <p className={styles.attribution}>
            Added to the catalogue
            {movie.createdByUserId === user.id ? ' by you' : ''} on {formatTimestampDate(movie.createdAt)}
          </p>
        </div>
      </Card>

      <Card as="section" padded={false} className={styles.history} labelledBy="history-heading">
        <div className={styles.historyHeader}>
          <h2 id="history" ref={historyHeadingRef} tabIndex={-1} className={styles.historyTitle}>
            <span id="history-heading">Your History</span>
          </h2>
          <p className={styles.historySubtitle}>Every time you&apos;ve logged this movie, newest first.</p>
        </div>
        {logs.length === 0 ? (
          <EmptyState
            icon={<History size={22} />}
            title="You haven't logged this movie yet"
            description="Log it when you watch it and it will show up here."
            actions={
              <ButtonLink to={`/log?movieId=${movie.id}`} state={logBackState}>
                Log Movie
              </ButtonLink>
            }
          />
        ) : (
          <HistoryList movie={movie} logs={logs} />
        )}
      </Card>
    </>
  );
}

function HistoryList({ movie, logs }: { movie: Movie; logs: MovieWatch[] }) {
  const invalidate = useInvalidateWatchData();
  const editBackState = useBackState(`Back to ${movie.title}`);
  const [deleting, setDeleting] = useState<MovieWatch | null>(null);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [notice, setNotice] = useState<string | null>(null);

  async function confirmDelete() {
    if (!deleting) return;
    setBusy(true);
    setError(null);
    try {
      await movieWatchesApi.remove(deleting.id);
    } catch (err) {
      // 404: already deleted (e.g. in another tab) - the refresh below brings the list up to date.
      if (!isApiError(err, 404)) {
        setError(errorMessage(err));
        setBusy(false);
        return;
      }
    }
    await invalidate();
    setNotice(`Deleted your log from ${formatCalendarDate(deleting.dateWatched)}.`);
    setBusy(false);
    setDeleting(null);
  }

  return (
    <>
      <div aria-live="polite" className={`live-region ${styles.historyNotice}`}>
        {notice && <Alert tone="success">{notice}</Alert>}
      </div>
      <ul className={styles.logs}>
        {logs.map((log) => {
          const date = formatCalendarDate(log.dateWatched);
          return (
            <li key={log.id} className={styles.log}>
              <span className={styles.logDate}>{date}</span>
              <span className={styles.logRating}>
                <Stars rating={log.rating} />
              </span>
              <span className={log.notes ? styles.logNotes : styles.logNotesEmpty}>{log.notes || 'No notes'}</span>
              <span className={styles.logActions}>
                <Link
                  to={`/logs/${log.id}/edit`}
                  state={editBackState}
                  className={styles.logAction}
                  aria-label={`Edit log from ${date}`}
                >
                  Edit
                </Link>
                <button
                  type="button"
                  className={`${styles.logAction} ${styles.logDelete}`}
                  onClick={() => {
                    setNotice(null);
                    setError(null);
                    setDeleting(log);
                  }}
                  aria-label={`Delete log from ${date}`}
                >
                  Delete
                </button>
              </span>
            </li>
          );
        })}
      </ul>

      {deleting && (
        <ConfirmDialog
          destructive
          title="Delete this log?"
          description={`Your log of ${movie.title} from ${formatCalendarDate(deleting.dateWatched)} will be permanently deleted.`}
          confirmLabel="Delete Log"
          busyLabel="Deleting…"
          busy={busy}
          error={error}
          onConfirm={confirmDelete}
          onCancel={() => setDeleting(null)}
        />
      )}
    </>
  );
}
