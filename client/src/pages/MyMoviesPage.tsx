import { Film, Pencil, SearchX } from 'lucide-react';
import type { MyMoviesSort } from '../api/types';
import { PageHeader } from '../components/layout/PageHeader';
import { MovieCell } from '../components/movies/MovieCell';
import tableStyles from '../components/movies/MovieTable.module.css';
import { ButtonLink, Button, IconButtonLink } from '../components/ui/Button';
import { FilterSelect, Pagination, SearchInput, Stars, Toolbar } from '../components/ui/Controls';
import { EmptyState, ErrorState, LoadingState } from '../components/ui/Feedback';
import { useGenres, useMyMovies } from '../hooks/queries';
import { useClampPage, useListParams } from '../hooks/useListParams';
import { formatCalendarDate } from '../lib/dates';

export const MY_MOVIES_PAGE_SIZE = 8;

const SORT_OPTIONS: { value: MyMoviesSort; label: string; description: string }[] = [
  { value: 'DateWatchedDesc', label: 'Newest Watched', description: 'sorted by newest first' },
  { value: 'DateWatchedAsc', label: 'Oldest Watched', description: 'sorted by oldest first' },
  { value: 'TitleAsc', label: 'Title A–Z', description: 'sorted by title' },
  { value: 'TitleDesc', label: 'Title Z–A', description: 'sorted by title, Z to A' },
  { value: 'RatingDesc', label: 'Highest Rated', description: 'sorted by your highest rated' },
  { value: 'RatingAsc', label: 'Lowest Rated', description: 'sorted by your lowest rated' },
];

const RATING_OPTIONS = [
  { value: '', label: 'All Ratings' },
  ...[5, 4, 3, 2, 1].map((n) => ({ value: String(n), label: `${n} ${n === 1 ? 'star' : 'stars'}` })),
];

const FILTER_KEYS = ['genre', 'rating', 'sort'] as const;

function parseSort(value: string): MyMoviesSort {
  return SORT_OPTIONS.some((o) => o.value === value) ? (value as MyMoviesSort) : 'DateWatchedDesc';
}

function parseRating(value: string): number | undefined {
  const n = Number(value);
  return Number.isInteger(n) && n >= 1 && n <= 5 ? n : undefined;
}

export function MyMoviesPage() {
  const { page, search, searchText, setSearchText, filters, setFilter, setPage, resetFilters } = useListParams(FILTER_KEYS);
  const genres = useGenres();

  const sort = parseSort(filters.sort);
  const rating = parseRating(filters.rating);
  const genreId = Number(filters.genre) > 0 ? Number(filters.genre) : undefined;

  const query = { search: search || undefined, genreId, rating, sort, page, pageSize: MY_MOVIES_PAGE_SIZE };
  const myMovies = useMyMovies(query);
  useClampPage(page, myMovies.data?.totalCount, MY_MOVIES_PAGE_SIZE, setPage);

  const hasFilters = Boolean(search || genreId || rating);
  const sortDescription = SORT_OPTIONS.find((o) => o.value === sort)?.description;

  return (
    <>
      <PageHeader title="My Movies" subtitle={`All movies you have watched, ${sortDescription}.`} />

      <Toolbar
        end={
          <>
            <FilterSelect
              label="Genre:"
              value={genreId ? String(genreId) : ''}
              onChange={(e) => setFilter('genre', e.target.value)}
              options={[
                { value: '', label: 'All Genres' },
                ...(genres.data ?? []).map((g) => ({ value: String(g.id), label: g.name })),
              ]}
            />
            <FilterSelect
              label="Sort:"
              value={sort}
              onChange={(e) => setFilter('sort', e.target.value === 'DateWatchedDesc' ? '' : e.target.value)}
              options={SORT_OPTIONS}
            />
            <FilterSelect
              label="Rating:"
              value={rating ? String(rating) : ''}
              onChange={(e) => setFilter('rating', e.target.value)}
              options={RATING_OPTIONS}
            />
          </>
        }
      >
        <SearchInput
          label="Search watched movies"
          placeholder="Search watched movies..."
          value={searchText}
          onChange={(e) => setSearchText(e.target.value)}
          maxLength={200}
        />
      </Toolbar>

      <div className={tableStyles.section}>
        {myMovies.isPending ? (
          <div className={tableStyles.wrap}>
            <LoadingState label="Loading your movies…" />
          </div>
        ) : myMovies.isError ? (
          <div className={tableStyles.wrap}>
            <ErrorState error={myMovies.error} title="Couldn't load your movies" onRetry={() => myMovies.refetch()} />
          </div>
        ) : myMovies.data.items.length === 0 ? (
          <div className={tableStyles.wrap}>
            {hasFilters ? (
              <EmptyState
                icon={<SearchX size={22} />}
                title="No movies match your filters"
                description="Try a different search, genre or rating."
                actions={
                  <Button variant="secondary" onClick={resetFilters}>
                    Clear Filters
                  </Button>
                }
              />
            ) : (
              <EmptyState
                icon={<Film size={22} />}
                title="You haven't logged any movies yet"
                description="Log a movie you've watched and it will appear here."
                actions={
                  <>
                    <ButtonLink to="/log">Log Movie</ButtonLink>
                    <ButtonLink to="/search" variant="secondary">
                      Search the Catalogue
                    </ButtonLink>
                  </>
                }
              />
            )}
          </div>
        ) : (
          <>
            <div className={[tableStyles.wrap, myMovies.isPlaceholderData && tableStyles.fetching].filter(Boolean).join(' ')}>
              <table className={tableStyles.table} aria-busy={myMovies.isFetching || undefined}>
                <caption className="visually-hidden">Movies you have watched</caption>
                <thead>
                  <tr>
                    <th scope="col" className={tableStyles.colMovie}>Movie</th>
                    <th scope="col" className={tableStyles.colData}>Date Watched</th>
                    <th scope="col" className={tableStyles.colData}>Your Rating</th>
                    <th scope="col" className={tableStyles.colActions}>
                      <span className="visually-hidden">Actions</span>
                    </th>
                  </tr>
                </thead>
                <tbody>
                  {myMovies.data.items.map((movie) => (
                    <tr key={movie.movieId}>
                      <td>
                        <MovieCell {...movie} backLabel="Back to My Movies" />
                      </td>
                      <td>{formatCalendarDate(movie.lastWatchedAt)}</td>
                      <td>
                        <Stars rating={movie.lastRating} />
                      </td>
                      <td>
                        <div className={tableStyles.actions}>
                          {/* A row is a whole movie, not one log, so Edit opens the movie's history
                              where each log can be edited or deleted. */}
                          <IconButtonLink
                            to={`/movies/${movie.movieId}#history`}
                            label={`Edit logs for ${movie.title}`}
                            icon={<Pencil size={16} aria-hidden="true" />}
                          />
                        </div>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
            <div className={tableStyles.footer}>
              <Pagination
                page={page}
                pageSize={MY_MOVIES_PAGE_SIZE}
                totalCount={myMovies.data.totalCount}
                noun="watched movies"
                onPageChange={setPage}
              />
            </div>
          </>
        )}
      </div>
    </>
  );
}
