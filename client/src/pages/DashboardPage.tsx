import { useState, type FormEvent } from 'react';
import { Link, useNavigate } from 'react-router';
import { Bookmark, Film, PieChart } from 'lucide-react';
import type { Dashboard, WatchlistItem } from '../api/types';
import { useCurrentUser } from '../auth/useAuth';
import { PageHeader } from '../components/layout/PageHeader';
import { ButtonLink } from '../components/ui/Button';
import { Card, Poster, SearchInput, Stars } from '../components/ui/Controls';
import { EmptyState, ErrorState, LoadingState } from '../components/ui/Feedback';
import { useDashboard, useMyMovies, useWatchlist } from '../hooks/queries';
import { useBackState } from '../hooks/useBackLink';
import { formatCalendarDate, formatMonthYear, isWithinLastDays } from '../lib/dates';
import { formatMovieMeta } from '../lib/format';
import styles from './DashboardPage.module.css';

const RECENT_COUNT = 5;
const UP_NEXT_COUNT = 3;

export function DashboardPage() {
  const user = useCurrentUser();
  const navigate = useNavigate();
  const [search, setSearch] = useState('');
  const dashboard = useDashboard();
  const watchlist = useWatchlist();

  function handleSearch(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const q = search.trim();
    navigate(q ? `/search?q=${encodeURIComponent(q)}` : '/search');
  }

  return (
    <>
      <PageHeader
        title="Dashboard"
        subtitle={`Welcome back, ${user.displayName}. Here's what you've been watching.`}
        actions={
          <>
            <form role="search" onSubmit={handleSearch} className={styles.search}>
              <SearchInput
                label="Search movies"
                placeholder="Search movies..."
                value={search}
                onChange={(e) => setSearch(e.target.value)}
                white
              />
            </form>
            <ButtonLink to="/log">+ Log Movie</ButtonLink>
          </>
        }
      />

      {dashboard.isPending ? (
        <Card>
          <LoadingState label="Loading your stats…" />
        </Card>
      ) : dashboard.isError ? (
        <Card>
          <ErrorState error={dashboard.error} title="Couldn't load your dashboard" onRetry={() => dashboard.refetch()} />
        </Card>
      ) : (
        <StatCards dashboard={dashboard.data} watchlist={watchlist.data} />
      )}

      <RecentlyWatched />

      <div className={styles.bottomRow}>
        <UpNext items={watchlist.data} isPending={watchlist.isPending} error={watchlist.error} onRetry={() => watchlist.refetch()} />
        <TopGenres dashboard={dashboard.data} />
      </div>
    </>
  );
}

function StatCards({ dashboard, watchlist }: { dashboard: Dashboard; watchlist?: WatchlistItem[] }) {
  const addedThisWeek = watchlist?.filter((item) => isWithinLastDays(item.dateAdded, 7)).length;
  const stats = [
    {
      label: 'Movies logged',
      value: dashboard.totalMoviesLogged,
      detail: `+${dashboard.moviesWatchedThisMonth} this month`,
    },
    { label: 'Watched this month', value: dashboard.moviesWatchedThisMonth, detail: formatMonthYear() },
    {
      label: 'Average rating',
      value: dashboard.averageRating === null ? '—' : dashboard.averageRating.toFixed(1),
      detail: dashboard.averageRating === null ? 'No ratings yet' : 'Out of 5 stars',
    },
    {
      label: 'On watchlist',
      value: dashboard.watchlistCount,
      detail: addedThisWeek === undefined ? '' : `${addedThisWeek} added this week`,
    },
  ];

  return (
    <ul className={styles.stats} aria-label="Your stats">
      {stats.map((stat) => (
        <li key={stat.label} className={styles.stat}>
          <span className={styles.statLabel}>{stat.label}</span>
          <span className={styles.statValue}>{stat.value}</span>
          <span className={styles.statDetail}>{stat.detail}</span>
        </li>
      ))}
    </ul>
  );
}

/**
 * The dashboard endpoint's recentlyWatched only carries movie ids, so this section uses the
 * My Movies endpoint (newest watched first), which includes titles, posters and last ratings.
 */
function RecentlyWatched() {
  const recent = useMyMovies({ sort: 'DateWatchedDesc', page: 1, pageSize: RECENT_COUNT });
  const backState = useBackState('Back to dashboard');

  return (
    <section className={styles.section} aria-labelledby="recent-heading">
      <div className={styles.sectionHeader}>
        <h2 id="recent-heading" className={styles.sectionTitle}>
          Recently Watched
        </h2>
        <Link to="/my-movies" className={styles.sectionLink}>
          View all in My Movies
        </Link>
      </div>
      {recent.isPending ? (
        <Card>
          <LoadingState label="Loading recently watched…" />
        </Card>
      ) : recent.isError ? (
        <Card>
          <ErrorState error={recent.error} onRetry={() => recent.refetch()} />
        </Card>
      ) : recent.data.items.length === 0 ? (
        <Card>
          <EmptyState
            icon={<Film size={22} />}
            title="Nothing logged yet"
            description="Movies you log will show up here."
            actions={<ButtonLink to="/log">Log Movie</ButtonLink>}
          />
        </Card>
      ) : (
        <ul className={styles.posters}>
          {recent.data.items.map((movie) => (
            <li key={movie.movieId} className={styles.posterCard}>
              <Link to={`/movies/${movie.movieId}`} state={backState} className={styles.posterLink}>
                <Poster url={movie.posterImageUrl} title={movie.title} size="card" />
                <span className={styles.posterTitle}>{movie.title}</span>
              </Link>
              <span className={styles.posterMeta}>Watched {formatCalendarDate(movie.lastWatchedAt)}</span>
              <Stars rating={movie.lastRating} />
            </li>
          ))}
        </ul>
      )}
    </section>
  );
}

interface UpNextProps {
  items?: WatchlistItem[];
  isPending: boolean;
  error: unknown;
  onRetry: () => void;
}

function UpNext({ items, isPending, error, onRetry }: UpNextProps) {
  const backState = useBackState('Back to dashboard');
  return (
    <Card as="section" labelledBy="up-next-heading">
      <div className={styles.cardHeader}>
        <h2 id="up-next-heading" className={styles.cardTitle}>
          Up next from your watchlist
        </h2>
        <Link to="/watchlist" className={styles.sectionLink}>
          View watchlist
        </Link>
      </div>
      {isPending ? (
        <LoadingState label="Loading watchlist…" />
      ) : error ? (
        <ErrorState error={error} onRetry={onRetry} />
      ) : !items || items.length === 0 ? (
        <EmptyState
          icon={<Bookmark size={22} />}
          title="Your watchlist is empty"
          description="Bookmark movies in Search to plan what to watch next."
        />
      ) : (
        <ul className={styles.upNext}>
          {items.slice(0, UP_NEXT_COUNT).map((item) => (
            <li key={item.movieId} className={styles.upNextItem}>
              <Poster url={item.posterImageUrl} title={item.title} size="small" />
              <div className={styles.upNextText}>
                <Link to={`/movies/${item.movieId}`} state={backState} className={styles.upNextTitle}>
                  {item.title}
                </Link>
                <span className={styles.upNextMeta}>{formatMovieMeta(item)}</span>
              </div>
              <ButtonLink
                to={`/log?movieId=${item.movieId}&from=watchlist`}
                variant="secondary"
                size="small"
                aria-label={`Log ${item.title}`}
              >
                Log
              </ButtonLink>
            </li>
          ))}
        </ul>
      )}
    </Card>
  );
}

function TopGenres({ dashboard }: { dashboard?: Dashboard }) {
  const genres = dashboard?.topGenres ?? [];
  const max = Math.max(1, ...genres.map((g) => g.count));
  return (
    <Card as="section" labelledBy="genres-heading">
      <div className={styles.cardHeader}>
        <h2 id="genres-heading" className={styles.cardTitle}>
          Your top genres
        </h2>
        <span className={styles.cardMeta}>All time</span>
      </div>
      {!dashboard ? null : genres.length === 0 ? (
        <EmptyState
          icon={<PieChart size={22} />}
          title="No genres yet"
          description="Your most-watched genres appear once you log movies that have genres."
        />
      ) : (
        <ul className={styles.genres}>
          {genres.map((genre) => (
            <li key={genre.genreId} className={styles.genre}>
              <span className={styles.genreName}>{genre.genreName}</span>
              <span className={styles.genreTrack} aria-hidden="true">
                <span className={styles.genreBar} style={{ width: `${(genre.count / max) * 100}%` }} />
              </span>
              <span className={styles.genreCount}>
                {genre.count}
                <span className="visually-hidden"> {genre.count === 1 ? 'log' : 'logs'}</span>
              </span>
            </li>
          ))}
        </ul>
      )}
    </Card>
  );
}
