import type {
  Dashboard,
  Genre,
  Movie,
  MovieDetails,
  MovieWatch,
  MyMovie,
  PagedResult,
  User,
  WatchlistItem,
} from '../api/types';

export const testUser: User = {
  id: 7,
  displayName: 'Campbell',
  email: 'campbell@example.com',
  createdAt: '2025-03-12T10:00:00',
};

export const genres: Genre[] = [
  { id: 7, name: 'Drama' },
  { id: 10, name: 'Horror' },
  { id: 14, name: 'Romance' },
  { id: 15, name: 'Science Fiction' },
];

export function movie(overrides: Partial<Movie> = {}): Movie {
  return {
    id: 1,
    title: 'Alien',
    releaseYear: 1979,
    runtimeMinutes: 117,
    director: 'Ridley Scott',
    synopsis: 'The crew of the Nostromo answer a distress call.',
    posterImageUrl: null,
    createdAt: '2025-03-12T09:30:00',
    createdByUserId: testUser.id,
    genres: [genres[1], genres[3]],
    ...overrides,
  };
}

export function watch(overrides: Partial<MovieWatch> = {}): MovieWatch {
  return {
    id: 100,
    userId: testUser.id,
    movieId: 1,
    dateWatched: '2025-06-14T00:00:00',
    rating: 5,
    notes: 'Still the best haunted-house movie set in space.',
    createdAt: '2025-06-14T21:00:00',
    updatedAt: null,
    ...overrides,
  };
}

export function movieDetails(m: Movie = movie(), logs: MovieWatch[] = []): MovieDetails {
  const sorted = [...logs].sort((a, b) => b.dateWatched.localeCompare(a.dateWatched));
  return {
    movie: m,
    userHistory: {
      timesWatched: logs.length,
      lastWatchedAt: sorted[0]?.dateWatched ?? null,
      lastRating: sorted[0]?.rating ?? null,
      logs: sorted,
    },
  };
}

export function myMovie(overrides: Partial<MyMovie> = {}): MyMovie {
  return {
    movieId: 1,
    title: 'Alien',
    releaseYear: 1979,
    director: 'Ridley Scott',
    runtimeMinutes: 117,
    posterImageUrl: null,
    lastWatchedAt: '2025-06-14T00:00:00',
    lastRating: 5,
    timesWatched: 3,
    ...overrides,
  };
}

export function watchlistItem(overrides: Partial<WatchlistItem> = {}): WatchlistItem {
  return {
    id: 1,
    movieId: 20,
    title: 'Past Lives',
    releaseYear: 2023,
    director: 'Celine Song',
    runtimeMinutes: 106,
    posterImageUrl: null,
    genres: [genres[0], genres[2]],
    dateAdded: '2026-09-21T10:00:00',
    ...overrides,
  };
}

export function paged<T>(items: T[], page = 1, pageSize = 8, totalCount = items.length): PagedResult<T> {
  return { items, page, pageSize, totalCount };
}

export const emptyDashboard: Dashboard = {
  totalMoviesLogged: 0,
  moviesWatchedThisMonth: 0,
  averageRating: null,
  watchlistCount: 0,
  recentlyWatched: [],
  topGenres: [],
};
