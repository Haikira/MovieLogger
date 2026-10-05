import { useState } from 'react';
import { Link } from 'react-router';
import { ArrowRight, Bookmark, Clapperboard, SearchX } from 'lucide-react';
import { errorMessage } from '../api/client';
import type { Movie, MovieSearchQuery } from '../api/types';
import { PageHeader } from '../components/layout/PageHeader';
import { MovieCell } from '../components/movies/MovieCell';
import tableStyles from '../components/movies/MovieTable.module.css';
import { ButtonLink, IconButton } from '../components/ui/Button';
import { FilterSelect, Pagination, SearchInput, Toolbar } from '../components/ui/Controls';
import { Alert, EmptyState, ErrorState, LoadingState } from '../components/ui/Feedback';
import { useMovieSearch, useWatchCounts, useWatchlistMovieIds, useWatchlistMutations } from '../hooks/queries';
import { useClampPage, useListParams } from '../hooks/useListParams';
import { formatGenres } from '../lib/format';
import styles from './SearchPage.module.css';

export const SEARCH_PAGE_SIZE = 8;

type SearchField = 'title' | 'director' | 'year';

const SEARCH_FIELDS: { value: SearchField; label: string }[] = [
  { value: 'title', label: 'Title' },
  { value: 'director', label: 'Director' },
  { value: 'year', label: 'Year' },
];

const PLACEHOLDERS: Record<SearchField, string> = {
  title: 'Search by title...',
  director: 'Search by director...',
  year: 'Search by year, e.g. 1979',
};

const FILTER_KEYS = ['by'] as const;

function parseField(value: string): SearchField {
  return value === 'director' || value === 'year' ? value : 'title';
}

/** The API's year range (MovieSearchQueryDto.Year is Range(1888, 2200)). */
function parseYear(value: string): number | null {
  if (!/^\d{4}$/.test(value)) {
    return null;
  }
  const year = Number(value);
  return year >= 1888 && year <= 2200 ? year : null;
}

export function SearchPage() {
  const { page, search, searchText, setSearchText, filters, setFilter, setPage } = useListParams(FILTER_KEYS);
  const field = parseField(filters.by);
  const year = field === 'year' && search ? parseYear(search) : null;
  const invalidYear = field === 'year' && Boolean(search) && year === null;

  // The API filters title, director and year separately (combined with AND), so the search box
  // targets one field at a time. The design's "Release Date" sort isn't offered because the API
  // can't sort by it.
  const query: MovieSearchQuery = {
    title: field === 'title' ? search || undefined : undefined,
    director: field === 'director' ? search || undefined : undefined,
    year: year ?? undefined,
    page,
    pageSize: SEARCH_PAGE_SIZE,
  };
  const results = useMovieSearch(query, !invalidYear);
  useClampPage(page, results.data?.totalCount, SEARCH_PAGE_SIZE, setPage);

  const watchlistIds = useWatchlistMovieIds();
  const { counts } = useWatchCounts();
  const { add, remove } = useWatchlistMutations();
  const [pendingMovieId, setPendingMovieId] = useState<number | null>(null);
  const [actionError, setActionError] = useState<string | null>(null);

  async function toggleWatchlist(movie: Movie) {
    const onWatchlist = watchlistIds.has(movie.id);
    setPendingMovieId(movie.id);
    setActionError(null);
    try {
      await (onWatchlist ? remove : add).mutateAsync(movie.id);
    } catch (error) {
      setActionError(
        `Couldn't ${onWatchlist ? 'remove' : 'add'} ${movie.title} ${onWatchlist ? 'from' : 'to'} your watchlist. ${errorMessage(error)}`,
      );
    } finally {
      setPendingMovieId(null);
    }
  }

  const addMovieLink = `/movies/new${field === 'title' && search ? `?title=${encodeURIComponent(search)}` : ''}`;

  return (
    <>
      <PageHeader title="Search" subtitle="Search the Movie Logger catalogue by title, director or year." />

      <Toolbar
        end={
          <>
            <FilterSelect
              label="Search by:"
              value={field}
              onChange={(e) => setFilter('by', e.target.value === 'title' ? '' : e.target.value)}
              options={SEARCH_FIELDS}
            />
            <ButtonLink to="/movies/new" variant="secondary">
              + Add Movie
            </ButtonLink>
          </>
        }
      >
        <SearchInput
          label={`Search the catalogue by ${field}`}
          placeholder={PLACEHOLDERS[field]}
          value={searchText}
          onChange={(e) => setSearchText(e.target.value)}
          inputMode={field === 'year' ? 'numeric' : undefined}
          maxLength={200}
        />
      </Toolbar>

      <div className={tableStyles.section}>
        <div className="live-region" aria-live="assertive">{actionError && <Alert tone="error" className={tableStyles.notice}>{actionError}</Alert>}</div>

        {invalidYear ? (
          <div className={tableStyles.wrap}>
            <EmptyState
              icon={<SearchX size={22} />}
              title="Enter a four-digit year"
              description="Search by a release year between 1888 and 2200, for example 1979."
            />
          </div>
        ) : results.isPending ? (
          <div className={tableStyles.wrap}>
            <LoadingState label="Searching the catalogue…" />
          </div>
        ) : results.isError ? (
          <div className={tableStyles.wrap}>
            <ErrorState error={results.error} title="Search failed" onRetry={() => results.refetch()} />
          </div>
        ) : results.data.items.length === 0 ? (
          <div className={tableStyles.wrap}>
            <EmptyState
              icon={search ? <SearchX size={22} /> : <Clapperboard size={22} />}
              title={search ? `No movies found for “${search}”` : 'The catalogue is empty'}
              description={
                search ? "Check the spelling, or add the movie if it isn't in the catalogue yet." : 'Add the first movie to get started.'
              }
              actions={<ButtonLink to={addMovieLink}>Add it to the Catalogue</ButtonLink>}
            />
          </div>
        ) : (
          <>
            <div className={[tableStyles.wrap, results.isPlaceholderData && tableStyles.fetching].filter(Boolean).join(' ')}>
              <table className={tableStyles.table} aria-busy={results.isFetching || undefined}>
                <caption className="visually-hidden">Search results</caption>
                <thead>
                  <tr>
                    <th scope="col" className={tableStyles.colMovie}>Movie</th>
                    <th scope="col" className={tableStyles.colData}>Genre</th>
                    <th scope="col" className={tableStyles.colData}>Your History</th>
                    <th scope="col" className={tableStyles.colActions}>
                      <span className="visually-hidden">Actions</span>
                    </th>
                  </tr>
                </thead>
                <tbody>
                  {results.data.items.map((movie) => {
                    const watched = counts.get(movie.id) ?? 0;
                    const onWatchlist = watchlistIds.has(movie.id);
                    return (
                      <tr key={movie.id}>
                        <td>
                          <MovieCell movieId={movie.id} {...movie} backLabel="Back to search results" />
                        </td>
                        <td>{formatGenres(movie.genres) || <span className={tableStyles.muted}>—</span>}</td>
                        <td>
                          {watched > 0 ? (
                            <span className={tableStyles.muted}>Watched {watched}×</span>
                          ) : onWatchlist ? (
                            <span className={tableStyles.highlight}>On watchlist</span>
                          ) : (
                            <span className={tableStyles.muted}>Not watched</span>
                          )}
                        </td>
                        <td>
                          <div className={tableStyles.actions}>
                            <IconButton
                              label={onWatchlist ? `Remove ${movie.title} from watchlist` : `Add ${movie.title} to watchlist`}
                              aria-pressed={onWatchlist}
                              active={onWatchlist}
                              icon={<Bookmark size={16} aria-hidden="true" fill={onWatchlist ? 'currentColor' : 'none'} />}
                              onClick={() => toggleWatchlist(movie)}
                              disabled={pendingMovieId === movie.id}
                            />
                            <ButtonLink
                              to={`/log?movieId=${movie.id}`}
                              variant="secondary"
                              size="small"
                              aria-label={`Log ${movie.title}`}
                            >
                              Log
                            </ButtonLink>
                          </div>
                        </td>
                      </tr>
                    );
                  })}
                </tbody>
              </table>
            </div>
            <div className={tableStyles.footer}>
              <p className={styles.addPrompt}>
                Can&apos;t find the movie you&apos;re looking for?{' '}
                <Link to={addMovieLink}>
                  Add it to the catalogue <ArrowRight size={16} aria-hidden="true" />
                </Link>
              </p>
              <Pagination
                page={page}
                pageSize={SEARCH_PAGE_SIZE}
                totalCount={results.data.totalCount}
                noun={results.data.totalCount === 1 ? 'result' : 'results'}
                suffix={search ? `for “${search}”` : undefined}
                onPageChange={setPage}
              />
            </div>
          </>
        )}
      </div>
    </>
  );
}
