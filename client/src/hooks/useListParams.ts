import { useCallback, useEffect, useRef, useState } from 'react';
import { useSearchParams } from 'react-router';
import { useDebouncedValue } from './useDebouncedValue';

/**
 * List-page state (search text, filters, page) kept in the URL query string, so the back button
 * and "Back to search results" links return to the same page of results.
 *
 * - Changing any filter or the (debounced) search text resets to page 1.
 * - The search box updates immediately; the URL, and so the API request, only once typing pauses.
 */
export function useListParams<F extends string>(filterKeys: readonly F[]) {
  const [params, setParams] = useSearchParams();

  const page = Math.max(1, Number.parseInt(params.get('page') ?? '1', 10) || 1);
  const committedSearch = params.get('q') ?? '';
  const [searchText, setSearchText] = useState(committedSearch);
  const debouncedSearch = useDebouncedValue(searchText.trim());

  const committedRef = useRef(committedSearch);
  useEffect(() => {
    committedRef.current = committedSearch;
  });

  // Push the debounced search text into the URL when it changes (and only then, so a stale
  // debounced value can't overwrite a URL change from back/forward navigation), resetting
  // pagination.
  useEffect(() => {
    if (debouncedSearch === committedRef.current) {
      return;
    }
    setParams(
      (current) => {
        const next = new URLSearchParams(current);
        if (debouncedSearch) {
          next.set('q', debouncedSearch);
        } else {
          next.delete('q');
        }
        next.delete('page');
        return next;
      },
      { replace: true },
    );
  }, [debouncedSearch, setParams]);

  // Keep the box in sync when the URL changes from elsewhere (back/forward navigation).
  const [lastCommitted, setLastCommitted] = useState(committedSearch);
  if (committedSearch !== lastCommitted) {
    setLastCommitted(committedSearch);
    if (committedSearch !== debouncedSearch) {
      setSearchText(committedSearch);
    }
  }

  const filters = Object.fromEntries(filterKeys.map((key) => [key, params.get(key) ?? ''])) as Record<F, string>;

  const setFilter = useCallback(
    (key: F, value: string) => {
      setParams((current) => {
        const next = new URLSearchParams(current);
        if (value) {
          next.set(key, value);
        } else {
          next.delete(key);
        }
        next.delete('page');
        return next;
      });
    },
    [setParams],
  );

  const setPage = useCallback(
    (nextPage: number, options?: { replace?: boolean }) => {
      setParams(
        (current) => {
          const next = new URLSearchParams(current);
          if (nextPage > 1) {
            next.set('page', String(nextPage));
          } else {
            next.delete('page');
          }
          return next;
        },
        { replace: options?.replace },
      );
    },
    [setParams],
  );

  /** Clears the search text and every filter in one URL update, keeping other params. */
  const resetFilters = useCallback(() => {
    setSearchText('');
    setParams((current) => {
      const next = new URLSearchParams(current);
      for (const key of [...filterKeys, 'q', 'page']) {
        next.delete(key);
      }
      return next;
    });
    // filterKeys is a module-level constant in every caller.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [setParams]);

  return { page, search: committedSearch, searchText, setSearchText, filters, setFilter, setPage, resetFilters };
}

/**
 * If the current page is past the end of the results (e.g. the last item on the last page was
 * removed, or the list shrank elsewhere), step back to the last page that has results.
 */
export function useClampPage(
  page: number,
  totalCount: number | undefined,
  pageSize: number,
  setPage: (page: number, options?: { replace?: boolean }) => void,
) {
  useEffect(() => {
    if (totalCount === undefined) {
      return;
    }
    const lastPage = Math.max(1, Math.ceil(totalCount / pageSize));
    if (page > lastPage) {
      setPage(lastPage, { replace: true });
    }
  }, [page, totalCount, pageSize, setPage]);
}
