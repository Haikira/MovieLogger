import { useState } from 'react';
import { Bookmark, SearchX, Trash } from 'lucide-react';
import { errorMessage } from '../api/client';
import type { WatchlistItem } from '../api/types';
import { PageHeader } from '../components/layout/PageHeader';
import { MovieCell } from '../components/movies/MovieCell';
import tableStyles from '../components/movies/MovieTable.module.css';
import { Button, ButtonLink, IconButton } from '../components/ui/Button';
import { FilterSelect, Pagination, SearchInput, Toolbar } from '../components/ui/Controls';
import { Alert, EmptyState, ErrorState, LoadingState } from '../components/ui/Feedback';
import { useGenres, useWatchlist, useWatchlistMutations } from '../hooks/queries';
import { useClampPage, useListParams } from '../hooks/useListParams';
import { formatTimestampDate } from '../lib/dates';
import { formatGenres } from '../lib/format';
import { filterWatchlist, paginate, parseWatchlistSort, WATCHLIST_SORT_OPTIONS } from '../lib/watchlist';

export const WATCHLIST_PAGE_SIZE = 8;

const FILTER_KEYS = ['genre', 'sort'] as const;

type Notice = { tone: 'success' | 'error'; text: string; undo?: WatchlistItem };

export function WatchlistPage() {
  const { page, search, searchText, setSearchText, filters, setFilter, setPage, resetFilters } = useListParams(FILTER_KEYS);
  const watchlist = useWatchlist();
  const genres = useGenres();
  const { add, remove } = useWatchlistMutations();
  const [notice, setNotice] = useState<Notice | null>(null);
  const [pendingMovieId, setPendingMovieId] = useState<number | null>(null);

  const sort = parseWatchlistSort(filters.sort);
  const genreId = Number(filters.genre) > 0 ? Number(filters.genre) : undefined;
  const filtered = filterWatchlist(watchlist.data ?? [], { search, genreId, sort });
  const visible = paginate(filtered, page, WATCHLIST_PAGE_SIZE);
  useClampPage(page, watchlist.data ? filtered.length : undefined, WATCHLIST_PAGE_SIZE, setPage);

  const hasFilters = Boolean(search || genreId);

  async function handleRemove(item: WatchlistItem) {
    setPendingMovieId(item.movieId);
    setNotice(null);
    try {
      await remove.mutateAsync(item.movieId);
      setNotice({ tone: 'success', text: `Removed ${item.title} from your watchlist.`, undo: item });
    } catch (error) {
      setNotice({ tone: 'error', text: `Couldn't remove ${item.title}. ${errorMessage(error)}` });
    } finally {
      setPendingMovieId(null);
    }
  }

  async function handleUndo(item: WatchlistItem) {
    setNotice(null);
    try {
      await add.mutateAsync(item.movieId);
      setNotice({ tone: 'success', text: `Added ${item.title} back to your watchlist.` });
    } catch (error) {
      setNotice({ tone: 'error', text: `Couldn't add ${item.title} back. ${errorMessage(error)}` });
    }
  }

  return (
    <>
      <PageHeader title="Watchlist" subtitle="Movies you plan to watch. Log one once you've seen it." />

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
              onChange={(e) => setFilter('sort', e.target.value === 'added-desc' ? '' : e.target.value)}
              options={WATCHLIST_SORT_OPTIONS}
            />
          </>
        }
      >
        <SearchInput
          label="Search watchlist"
          placeholder="Search watchlist..."
          value={searchText}
          onChange={(e) => setSearchText(e.target.value)}
        />
      </Toolbar>

      <div className={tableStyles.section}>
        <div className="live-region" aria-live="polite">
          {notice && (
            <Alert
              tone={notice.tone}
              className={tableStyles.notice}
              actions={
                notice.undo && (
                  <Button variant="link" onClick={() => handleUndo(notice.undo!)}>
                    Undo
                  </Button>
                )
              }
            >
              {notice.text}
            </Alert>
          )}
        </div>

        {watchlist.isPending ? (
          <div className={tableStyles.wrap}>
            <LoadingState label="Loading your watchlist…" />
          </div>
        ) : watchlist.isError ? (
          <div className={tableStyles.wrap}>
            <ErrorState error={watchlist.error} title="Couldn't load your watchlist" onRetry={() => watchlist.refetch()} />
          </div>
        ) : filtered.length === 0 ? (
          <div className={tableStyles.wrap}>
            {hasFilters ? (
              <EmptyState
                icon={<SearchX size={22} />}
                title="Nothing on your watchlist matches"
                description="Try a different search or genre."
                actions={
                  <Button variant="secondary" onClick={resetFilters}>
                    Clear Filters
                  </Button>
                }
              />
            ) : (
              <EmptyState
                icon={<Bookmark size={22} />}
                title="Your watchlist is empty"
                description="Find a movie in the catalogue and bookmark it to watch later."
                actions={<ButtonLink to="/search">Search the Catalogue</ButtonLink>}
              />
            )}
          </div>
        ) : (
          <>
            <div className={tableStyles.wrap}>
              <table className={tableStyles.table}>
                <caption className="visually-hidden">Movies on your watchlist</caption>
                <thead>
                  <tr>
                    <th scope="col" className={tableStyles.colMovie}>Movie</th>
                    <th scope="col" className={tableStyles.colData}>Genre</th>
                    <th scope="col" className={tableStyles.colData}>Date Added</th>
                    <th scope="col" className={tableStyles.colActions}>
                      <span className="visually-hidden">Actions</span>
                    </th>
                  </tr>
                </thead>
                <tbody>
                  {visible.map((item) => (
                    <tr key={item.movieId}>
                      <td>
                        <MovieCell {...item} backLabel="Back to watchlist" />
                      </td>
                      <td>{formatGenres(item.genres) || <span className={tableStyles.muted}>—</span>}</td>
                      <td className={tableStyles.muted}>Added {formatTimestampDate(item.dateAdded)}</td>
                      <td>
                        <div className={tableStyles.actions}>
                          <IconButton
                            label={`Remove ${item.title} from watchlist`}
                            icon={<Trash size={16} aria-hidden="true" />}
                            onClick={() => handleRemove(item)}
                            disabled={pendingMovieId === item.movieId}
                          />
                          <ButtonLink
                            to={`/log?movieId=${item.movieId}&from=watchlist`}
                            variant="secondary"
                            size="small"
                            aria-label={`Log ${item.title}`}
                          >
                            Log
                          </ButtonLink>
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
                pageSize={WATCHLIST_PAGE_SIZE}
                totalCount={filtered.length}
                noun={filtered.length === 1 ? 'movie' : 'movies'}
                onPageChange={setPage}
              />
            </div>
          </>
        )}
      </div>
    </>
  );
}
