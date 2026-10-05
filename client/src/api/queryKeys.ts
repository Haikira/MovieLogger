import type { MovieSearchQuery, MyMoviesQuery } from './types';

/** TanStack Query cache keys, kept in one place so invalidation stays consistent. */
export const queryKeys = {
  dashboard: ['dashboard'] as const,
  genres: ['genres'] as const,
  watchlist: ['watchlist'] as const,
  movieWatches: ['movieWatches'] as const,
  movieWatch: (id: number) => ['movieWatches', id] as const,
  myMovies: ['myMovies'] as const,
  myMoviesPage: (query: MyMoviesQuery) => ['myMovies', query] as const,
  movies: ['movies'] as const,
  movieSearch: (query: MovieSearchQuery) => ['movies', 'search', query] as const,
  movieDetails: (id: number) => ['movies', 'details', id] as const,
};
