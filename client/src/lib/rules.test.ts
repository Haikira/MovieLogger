import { describe, expect, it } from 'vitest';
import { sectionForPath } from '../components/layout/navigation';
import { genres, watchlistItem } from '../test/fixtures';
import { validateLog } from './logValidation';
import { validateMovie } from './movieValidation';
import { validateNewPassword, validatePasswordConfirmation } from './validation';
import { filterWatchlist } from './watchlist';

describe('password rules (from the Figma)', () => {
  it('requires at least 8 characters including a number', () => {
    expect(validateNewPassword('short1')).toMatch(/at least 8 characters/);
    expect(validateNewPassword('longenough')).toMatch(/number/);
    expect(validateNewPassword('longenough1')).toBeUndefined();
  });

  it('requires the confirmation to match', () => {
    expect(validatePasswordConfirmation('password1', 'password2')).toBe("Passwords don't match.");
    expect(validatePasswordConfirmation('password1', 'password1')).toBeUndefined();
  });
});

describe('validateLog', () => {
  const now = new Date(2026, 8, 30, 12);

  it('accepts a log for today with no rating or notes', () => {
    expect(validateLog({ dateWatched: '2026-09-30', rating: null, notes: '' }, now)).toEqual({});
  });

  it('rejects future dates, bad ratings and long notes', () => {
    const errors = validateLog({ dateWatched: '2026-10-01', rating: 6, notes: 'x'.repeat(501) }, now);
    expect(errors.dateWatched).toMatch(/future/);
    expect(errors.rating).toMatch(/1 and 5/);
    expect(errors.notes).toMatch(/500/);
  });

  it('requires a date', () => {
    expect(validateLog({ dateWatched: '', rating: null, notes: '' }, now).dateWatched).toBeDefined();
  });
});

describe('validateMovie', () => {
  const valid = {
    title: 'Past Lives',
    releaseYear: '2023',
    runtimeMinutes: '106',
    director: 'Celine Song',
    genreIds: [7],
    synopsis: '',
    posterImageUrl: '',
  };

  it('accepts a complete movie', () => {
    expect(validateMovie(valid)).toEqual({});
  });

  it('requires at least one genre', () => {
    expect(validateMovie({ ...valid, genreIds: [] }).genreIds).toBe('Choose at least one genre.');
  });

  it('checks the year, runtime and poster URL against the API rules', () => {
    const errors = validateMovie({ ...valid, releaseYear: '1700', runtimeMinutes: '0', posterImageUrl: 'not a url' });
    expect(errors.releaseYear).toBeDefined();
    expect(errors.runtimeMinutes).toBeDefined();
    expect(errors.posterImageUrl).toBeDefined();
  });
});

describe('filterWatchlist', () => {
  const items = [
    watchlistItem({ movieId: 1, title: 'Past Lives', director: 'Celine Song', genres: [genres[0]], dateAdded: '2026-09-21T10:00:00' }),
    watchlistItem({ movieId: 2, title: 'Alien: Romulus', director: 'Fede Álvarez', genres: [genres[1]], dateAdded: '2026-09-14T10:00:00' }),
    watchlistItem({ movieId: 3, title: 'Heat', director: 'Michael Mann', genres: [genres[0]], dateAdded: '2026-07-27T10:00:00' }),
  ];

  it('sorts by date added, newest first, by default', () => {
    expect(filterWatchlist(items, { search: '', sort: 'added-desc' }).map((i) => i.movieId)).toEqual([1, 2, 3]);
    expect(filterWatchlist(items, { search: '', sort: 'added-asc' }).map((i) => i.movieId)).toEqual([3, 2, 1]);
  });

  it('searches titles and directors case-insensitively', () => {
    expect(filterWatchlist(items, { search: 'ALIEN', sort: 'added-desc' }).map((i) => i.movieId)).toEqual([2]);
    expect(filterWatchlist(items, { search: 'mann', sort: 'added-desc' }).map((i) => i.movieId)).toEqual([3]);
  });

  it('filters by genre', () => {
    expect(filterWatchlist(items, { search: '', genreId: 7, sort: 'title-asc' }).map((i) => i.movieId)).toEqual([3, 1]);
  });
});

describe('sidebar sections', () => {
  it.each([
    ['/dashboard', 'dashboard'],
    ['/my-movies', 'my-movies'],
    ['/log', 'my-movies'],
    ['/logs/4/edit', 'my-movies'],
    ['/search', 'search'],
    ['/movies/12', 'search'],
    ['/movies/new', 'search'],
    ['/watchlist', 'watchlist'],
    ['/settings', 'settings'],
  ])('%s highlights %s', (path, section) => {
    expect(sectionForPath(path)).toBe(section);
  });
});
