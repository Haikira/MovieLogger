import { screen, waitFor, within } from '@testing-library/react';
import { beforeEach, describe, expect, it } from 'vitest';
import type { WatchlistItem } from '../api/types';
import { genres, watchlistItem } from '../test/fixtures';
import { json, noContent } from '../test/mockApi';
import { createApi, currentLocation, renderApp, signIn } from '../test/renderApp';

function items(count: number): WatchlistItem[] {
  // Newest first: item 1 was added most recently.
  return Array.from({ length: count }, (_, i) =>
    watchlistItem({
      id: i + 1,
      movieId: i + 1,
      title: `Movie ${i + 1}`,
      genres: [i % 2 === 0 ? genres[0] : genres[1]],
      dateAdded: new Date(Date.UTC(2026, 8, 30 - i, 10)).toISOString().replace('Z', ''),
    }),
  );
}

describe('Watchlist page', () => {
  beforeEach(() => signIn());

  it('lists movies with genre and date added, newest first, 8 per page', async () => {
    createApi().on('GET', '/api/watchlist', items(10));
    const { user } = renderApp('/watchlist');

    const rows = await screen.findAllByRole('row');
    expect(rows).toHaveLength(9);
    expect(within(rows[1]).getByText('Movie 1')).toBeInTheDocument();
    expect(within(rows[1]).getByText('Drama')).toBeInTheDocument();
    expect(within(rows[1]).getByText('Added 30 Sep 2026')).toBeInTheDocument();
    expect(screen.getByText('Showing 1–8 of 10 movies')).toBeInTheDocument();

    await user.click(screen.getByRole('button', { name: 'Next Page' }));
    expect(await screen.findByText('Showing 9–10 of 10 movies')).toBeInTheDocument();
  });

  it('searches, filters by genre and re-sorts in the browser, resetting to page 1', async () => {
    const api = createApi().on('GET', '/api/watchlist', items(10));
    const { user } = renderApp('/watchlist?page=2');
    await screen.findByText('Showing 9–10 of 10 movies');

    await user.selectOptions(screen.getByLabelText('Genre:'), 'Horror');
    expect(await screen.findByText('Showing 1–5 of 5 movies')).toBeInTheDocument();
    expect(currentLocation()).toBe('/watchlist?genre=10');

    await user.selectOptions(screen.getByLabelText('Sort:'), 'Oldest Added');
    expect(within(screen.getAllByRole('row')[1]).getByText('Movie 10')).toBeInTheDocument();

    await user.type(screen.getByRole('searchbox', { name: 'Search watchlist' }), 'movie 4');
    expect(await screen.findByText('Showing 1–1 of 1 movie')).toBeInTheDocument();
    expect(api.calls('GET', '/api/watchlist')).toHaveLength(1);
  });

  it('removes a movie, offering undo', async () => {
    let current = items(2);
    const api = createApi()
      .on('GET', '/api/watchlist', () => json(200, current))
      .on('DELETE', '/api/watchlist/1', () => {
        current = current.filter((i) => i.movieId !== 1);
        return noContent();
      })
      .on('POST', '/api/watchlist/1', json(201, items(1)[0]));
    const { user } = renderApp('/watchlist');

    await user.click(await screen.findByRole('button', { name: 'Remove Movie 1 from watchlist' }));

    expect(await screen.findByText('Removed Movie 1 from your watchlist.')).toBeInTheDocument();
    await waitFor(() => expect(screen.queryByText('Movie 1')).not.toBeInTheDocument());

    await user.click(screen.getByRole('button', { name: 'Undo' }));
    await waitFor(() => expect(api.calls('POST', '/api/watchlist/1')).toHaveLength(1));
  });

  it('treats removing an already-removed movie (404) as done and refreshes', async () => {
    let current = items(1);
    const api = createApi()
      .on('GET', '/api/watchlist', () => json(200, current))
      .on('DELETE', '/api/watchlist/1', () => {
        current = [];
        return json(404);
      });
    const { user } = renderApp('/watchlist');

    await user.click(await screen.findByRole('button', { name: 'Remove Movie 1 from watchlist' }));

    expect(await screen.findByText('Your watchlist is empty')).toBeInTheDocument();
    expect(screen.queryByRole('alert')).not.toBeInTheDocument();
    expect(api.calls('GET', '/api/watchlist')).toHaveLength(2);
  });

  it('steps back a page when the last item on the last page is removed', async () => {
    let current = items(9);
    createApi()
      .on('GET', '/api/watchlist', () => json(200, current))
      .on('DELETE', '/api/watchlist/9', () => {
        current = current.filter((i) => i.movieId !== 9);
        return noContent();
      });
    const { user } = renderApp('/watchlist?page=2');

    await user.click(await screen.findByRole('button', { name: 'Remove Movie 9 from watchlist' }));

    expect(await screen.findByText('Showing 1–8 of 8 movies')).toBeInTheDocument();
    expect(currentLocation()).toBe('/watchlist');
  });

  it('logs a movie from the watchlist', async () => {
    createApi().on('GET', '/api/watchlist', items(1));
    renderApp('/watchlist');

    expect(await screen.findByRole('link', { name: 'Log Movie 1' })).toHaveAttribute('href', '/log?movieId=1&from=watchlist');
  });
});
