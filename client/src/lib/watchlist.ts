import type { WatchlistItem } from '../api/types';
import { parseServerTimestamp } from './dates';

export type WatchlistSort = 'added-desc' | 'added-asc' | 'title-asc';

export const WATCHLIST_SORT_OPTIONS: { value: WatchlistSort; label: string }[] = [
  { value: 'added-desc', label: 'Date Added' },
  { value: 'added-asc', label: 'Oldest Added' },
  { value: 'title-asc', label: 'Title A–Z' },
];

export function parseWatchlistSort(value: string): WatchlistSort {
  return WATCHLIST_SORT_OPTIONS.some((o) => o.value === value) ? (value as WatchlistSort) : 'added-desc';
}

interface WatchlistFilter {
  search: string;
  genreId?: number;
  sort: WatchlistSort;
}

/**
 * The watchlist endpoint returns the whole (personal, typically small) list, so search, genre
 * filtering and sorting happen in the browser.
 */
export function filterWatchlist(items: WatchlistItem[], { search, genreId, sort }: WatchlistFilter): WatchlistItem[] {
  const needle = search.trim().toLocaleLowerCase();
  const filtered = items.filter(
    (item) =>
      (!needle ||
        item.title.toLocaleLowerCase().includes(needle) ||
        (item.director ?? '').toLocaleLowerCase().includes(needle)) &&
      (!genreId || item.genres.some((g) => g.id === genreId)),
  );

  const added = (item: WatchlistItem) => parseServerTimestamp(item.dateAdded).getTime();
  return [...filtered].sort((a, b) => {
    switch (sort) {
      case 'added-asc':
        return added(a) - added(b);
      case 'title-asc':
        return a.title.localeCompare(b.title);
      default:
        return added(b) - added(a);
    }
  });
}

export function paginate<T>(items: T[], page: number, pageSize: number): T[] {
  return items.slice((page - 1) * pageSize, page * pageSize);
}
