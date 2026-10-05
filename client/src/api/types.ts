/**
 * TypeScript mirrors of the API's DTOs (server/src/MovieLogger.Service/Dtos). ASP.NET Core
 * serialises property names in camelCase and DateTime values as ISO strings without a time zone
 * (see lib/dates.ts for how those are interpreted).
 */

export interface User {
  id: number;
  displayName: string;
  email: string;
  createdAt: string;
}

export interface AuthResponse {
  token: string;
  expiresAt: string;
  user: User;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  displayName: string;
  email: string;
  password: string;
  confirmPassword: string;
}

export interface UpdateUserRequest {
  displayName: string;
  email: string;
}

export interface ChangePasswordRequest {
  currentPassword: string;
  newPassword: string;
  confirmNewPassword: string;
}

export interface Genre {
  id: number;
  name: string;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface Movie {
  id: number;
  title: string;
  releaseYear: number;
  runtimeMinutes: number | null;
  director: string | null;
  synopsis: string | null;
  posterImageUrl: string | null;
  createdAt: string;
  createdByUserId: number | null;
  genres: Genre[];
}

export interface CreateMovieRequest {
  title: string;
  releaseYear: number;
  runtimeMinutes: number | null;
  director: string | null;
  synopsis: string | null;
  posterImageUrl: string | null;
  genreIds: number[];
}

export interface MovieSearchQuery {
  title?: string;
  director?: string;
  year?: number;
  page: number;
  pageSize: number;
}

export interface MovieWatch {
  id: number;
  userId: number;
  movieId: number;
  /** Calendar date, e.g. "2026-09-28T00:00:00". */
  dateWatched: string;
  rating: number | null;
  notes: string | null;
  createdAt: string;
  updatedAt: string | null;
}

export interface UserMovieHistory {
  timesWatched: number;
  lastWatchedAt: string | null;
  lastRating: number | null;
  logs: MovieWatch[];
}

export interface MovieDetails {
  movie: Movie;
  userHistory: UserMovieHistory | null;
}

export interface MovieWatchRequest {
  /** "YYYY-MM-DD" */
  dateWatched: string;
  rating: number | null;
  notes: string | null;
}

export interface CreateMovieWatchRequest extends MovieWatchRequest {
  movieId: number;
}

export type MyMoviesSort =
  | 'DateWatchedDesc'
  | 'DateWatchedAsc'
  | 'TitleAsc'
  | 'TitleDesc'
  | 'RatingDesc'
  | 'RatingAsc';

export interface MyMoviesQuery {
  search?: string;
  genreId?: number;
  rating?: number;
  sort: MyMoviesSort;
  page: number;
  pageSize: number;
}

export interface MyMovie {
  movieId: number;
  title: string;
  releaseYear: number;
  director: string | null;
  runtimeMinutes: number | null;
  posterImageUrl: string | null;
  /** Calendar date of the most recent watch. */
  lastWatchedAt: string;
  lastRating: number | null;
  timesWatched: number;
}

export interface WatchlistItem {
  id: number;
  movieId: number;
  title: string;
  releaseYear: number;
  director: string | null;
  runtimeMinutes: number | null;
  posterImageUrl: string | null;
  genres: Genre[];
  /** UTC timestamp. */
  dateAdded: string;
}

export interface GenreCount {
  genreId: number;
  genreName: string;
  count: number;
}

export interface Dashboard {
  totalMoviesLogged: number;
  moviesWatchedThisMonth: number;
  averageRating: number | null;
  watchlistCount: number;
  recentlyWatched: MovieWatch[];
  topGenres: GenreCount[];
}
