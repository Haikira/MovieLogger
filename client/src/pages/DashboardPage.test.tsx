import { screen, within } from '@testing-library/react';
import { beforeEach, describe, expect, it } from 'vitest';
import { myMovie, paged, watchlistItem } from '../test/fixtures';
import { json } from '../test/mockApi';
import { createApi, currentLocation, renderApp, signIn } from '../test/renderApp';

describe('Dashboard page', () => {
  beforeEach(() => signIn());

  it('shows stats from the dashboard API, recent movies, up next and top genres', async () => {
    const api = createApi()
      .on('GET', '/api/dashboard', {
        totalMoviesLogged: 184,
        moviesWatchedThisMonth: 6,
        averageRating: 4.125,
        watchlistCount: 23,
        recentlyWatched: [],
        topGenres: [
          { genreId: 7, genreName: 'Drama', count: 58 },
          { genreId: 15, genreName: 'Sci-Fi', count: 41 },
        ],
      })
      .on('GET', '/api/moviewatches/my-movies', paged([myMovie({ title: 'Dune: Part Two', movieId: 5, lastWatchedAt: '2026-09-26T00:00:00' })]))
      .on('GET', '/api/watchlist', [watchlistItem({ title: 'Past Lives', movieId: 20 })]);
    renderApp('/dashboard');

    const stats = await screen.findByRole('list', { name: 'Your stats' });
    expect(within(stats).getByText('184')).toBeInTheDocument();
    expect(within(stats).getByText('4.1')).toBeInTheDocument();
    expect(within(stats).getByText('23')).toBeInTheDocument();
    expect(within(screen.getByRole('complementary')).getByText('184 Movies Logged')).toBeInTheDocument();

    expect(await screen.findByText('Dune: Part Two')).toBeInTheDocument();
    expect(screen.getByText('Watched 26 Sep 2026')).toBeInTheDocument();
    const recentQuery = api.calls('GET', '/api/moviewatches/my-movies')[0].query;
    expect(recentQuery.get('sort')).toBe('DateWatchedDesc');
    expect(recentQuery.get('pageSize')).toBe('5');

    expect(await screen.findByRole('link', { name: 'Log Past Lives' })).toHaveAttribute('href', '/log?movieId=20&from=watchlist');
    expect(screen.getByText('2023 · Celine Song · 106 min')).toBeInTheDocument();
    expect(screen.getByText('Sci-Fi')).toBeInTheDocument();
  });

  it('shows empty states for a new user', async () => {
    createApi().on('GET', '/api/moviewatches/my-movies', paged([]));
    renderApp('/dashboard');

    expect(await screen.findByText('Nothing logged yet')).toBeInTheDocument();
    expect(await screen.findByText('Your watchlist is empty')).toBeInTheDocument();
    expect(screen.getByText('No ratings yet')).toBeInTheDocument();
  });

  it('shows an error state when the dashboard fails to load', async () => {
    createApi()
      .on('GET', '/api/dashboard', json(500))
      .on('GET', '/api/moviewatches/my-movies', paged([]));
    renderApp('/dashboard');

    expect(await screen.findByText("Couldn't load your dashboard")).toBeInTheDocument();
  });

  it('sends the header search to the Search page', async () => {
    createApi().on('GET', '/api/moviewatches/my-movies', paged([])).on('GET', '/api/movies', paged([]));
    const { user } = renderApp('/dashboard');

    await user.type(await screen.findByRole('searchbox', { name: 'Search movies' }), 'alien{Enter}');

    expect(await screen.findByRole('heading', { name: 'Search' })).toBeInTheDocument();
    expect(currentLocation()).toBe('/search?q=alien');
  });
});
