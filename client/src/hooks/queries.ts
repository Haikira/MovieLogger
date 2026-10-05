import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useCallback, useMemo } from 'react';
import { isApiError } from '../api/client';
import { dashboardApi, genresApi, movieWatchesApi, moviesApi, watchlistApi } from '../api/endpoints';
import { queryKeys } from '../api/queryKeys';
import type { MovieSearchQuery, MyMoviesQuery } from '../api/types';

export function useDashboard() {
  return useQuery({ queryKey: queryKeys.dashboard, queryFn: dashboardApi.get });
}

export function useGenres() {
  // The genre list is seeded reference data; it doesn't change while the app is open.
  return useQuery({ queryKey: queryKeys.genres, queryFn: genresApi.list, staleTime: Infinity });
}

export function useWatchlist() {
  return useQuery({ queryKey: queryKeys.watchlist, queryFn: watchlistApi.list });
}

/** The ids of movies on the user's watchlist (empty until the watchlist has loaded). */
export function useWatchlistMovieIds(): Set<number> {
  const { data } = useWatchlist();
  return useMemo(() => new Set((data ?? []).map((item) => item.movieId)), [data]);
}

/** How many times the user has logged each movie, from the full list of their logs. */
export function useWatchCounts() {
  const query = useQuery({ queryKey: queryKeys.movieWatches, queryFn: movieWatchesApi.listMine });
  const counts = useMemo(() => {
    const map = new Map<number, number>();
    for (const watch of query.data ?? []) {
      map.set(watch.movieId, (map.get(watch.movieId) ?? 0) + 1);
    }
    return map;
  }, [query.data]);
  return { ...query, counts };
}

export function useMyMovies(query: MyMoviesQuery) {
  return useQuery({
    queryKey: queryKeys.myMoviesPage(query),
    queryFn: () => movieWatchesApi.myMovies(query),
    placeholderData: keepPreviousData,
  });
}

export function useMovieSearch(query: MovieSearchQuery, enabled = true) {
  return useQuery({
    queryKey: queryKeys.movieSearch(query),
    queryFn: () => moviesApi.search(query),
    placeholderData: keepPreviousData,
    enabled,
  });
}

export function useMovieDetails(movieId: number | null) {
  return useQuery({
    queryKey: queryKeys.movieDetails(movieId ?? 0),
    queryFn: () => moviesApi.details(movieId as number),
    enabled: movieId !== null,
  });
}

/** Refreshes everything derived from the user's watch logs after one is created, edited or deleted. */
export function useInvalidateWatchData() {
  const queryClient = useQueryClient();
  return useCallback(
    () =>
      Promise.all([
        queryClient.invalidateQueries({ queryKey: queryKeys.dashboard }),
        queryClient.invalidateQueries({ queryKey: queryKeys.myMovies }),
        queryClient.invalidateQueries({ queryKey: queryKeys.movieWatches }),
        queryClient.invalidateQueries({ queryKey: queryKeys.movies }),
      ]),
    [queryClient],
  );
}

/**
 * Add/remove watchlist mutations. The API answers 409 when the movie is already on the watchlist
 * and 404 when it is already off it; both mean the watchlist is already in the state the user
 * asked for, so they are treated as success and the cached watchlist is refreshed.
 */
export function useWatchlistMutations() {
  const queryClient = useQueryClient();

  const refresh = useCallback(
    () =>
      Promise.all([
        queryClient.invalidateQueries({ queryKey: queryKeys.watchlist }),
        queryClient.invalidateQueries({ queryKey: queryKeys.dashboard }),
      ]),
    [queryClient],
  );

  const add = useMutation({
    mutationFn: async (movieId: number) => {
      try {
        await watchlistApi.add(movieId);
      } catch (error) {
        if (!isApiError(error, 409)) {
          throw error;
        }
      }
    },
    onSettled: refresh,
  });

  const remove = useMutation({
    mutationFn: async (movieId: number) => {
      try {
        await watchlistApi.remove(movieId);
      } catch (error) {
        if (!isApiError(error, 404)) {
          throw error;
        }
      }
    },
    onSettled: refresh,
  });

  return { add, remove };
}
