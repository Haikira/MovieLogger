import { apiRequest } from './client';
import type {
  AuthResponse,
  ChangePasswordRequest,
  CreateMovieRequest,
  CreateMovieWatchRequest,
  Dashboard,
  Genre,
  LoginRequest,
  Movie,
  MovieDetails,
  MovieSearchQuery,
  MovieWatch,
  MovieWatchRequest,
  MyMovie,
  MyMoviesQuery,
  PagedResult,
  RegisterRequest,
  UpdateUserRequest,
  User,
  WatchlistItem,
} from './types';

/** The API rejects page sizes above this (Range(1, 100) on the query DTOs). */
export const MAX_PAGE_SIZE = 100;

function clampPageSize(pageSize: number): number {
  return Math.min(Math.max(1, pageSize), MAX_PAGE_SIZE);
}

// Every call to the API goes through one of these functions; pages never call fetch directly.

export const authApi = {
  login: (request: LoginRequest) =>
    apiRequest<AuthResponse>('/api/auth/login', { method: 'POST', body: request, auth: false }),
  register: (request: RegisterRequest) =>
    apiRequest<AuthResponse>('/api/auth/register', { method: 'POST', body: request, auth: false }),
};

export const usersApi = {
  me: (signal?: AbortSignal) => apiRequest<User>('/api/users/me', { signal }),
  // The id is always the signed-in user's own id (from /api/users/me); the API also rejects any
  // other id with 403.
  update: (id: number, request: UpdateUserRequest) =>
    apiRequest<void>(`/api/users/${id}`, { method: 'PUT', body: request }),
  remove: (id: number) => apiRequest<void>(`/api/users/${id}`, { method: 'DELETE' }),
  changePassword: (request: ChangePasswordRequest) =>
    apiRequest<void>('/api/users/change-password', { method: 'POST', body: request }),
};

export const genresApi = {
  list: () => apiRequest<Genre[]>('/api/genres'),
};

export const moviesApi = {
  search: (query: MovieSearchQuery) =>
    apiRequest<PagedResult<Movie>>('/api/movies', {
      query: { ...query, pageSize: clampPageSize(query.pageSize) },
    }),
  details: (id: number) => apiRequest<MovieDetails>(`/api/movies/${id}`),
  create: (request: CreateMovieRequest) =>
    apiRequest<Movie>('/api/movies', { method: 'POST', body: request }),
};

export const movieWatchesApi = {
  listMine: () => apiRequest<MovieWatch[]>('/api/moviewatches'),
  get: (id: number) => apiRequest<MovieWatch>(`/api/moviewatches/${id}`),
  myMovies: (query: MyMoviesQuery) =>
    apiRequest<PagedResult<MyMovie>>('/api/moviewatches/my-movies', {
      query: { ...query, pageSize: clampPageSize(query.pageSize) },
    }),
  create: (request: CreateMovieWatchRequest) =>
    apiRequest<MovieWatch>('/api/moviewatches', { method: 'POST', body: request }),
  update: (id: number, request: MovieWatchRequest) =>
    apiRequest<void>(`/api/moviewatches/${id}`, { method: 'PUT', body: request }),
  remove: (id: number) => apiRequest<void>(`/api/moviewatches/${id}`, { method: 'DELETE' }),
};

export const watchlistApi = {
  list: () => apiRequest<WatchlistItem[]>('/api/watchlist'),
  add: (movieId: number) => apiRequest<WatchlistItem>(`/api/watchlist/${movieId}`, { method: 'POST' }),
  remove: (movieId: number) => apiRequest<void>(`/api/watchlist/${movieId}`, { method: 'DELETE' }),
};

export const dashboardApi = {
  get: () => apiRequest<Dashboard>('/api/dashboard'),
};
