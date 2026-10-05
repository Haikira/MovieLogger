import { screen, waitFor, within } from '@testing-library/react';
import { beforeEach, describe, expect, it } from 'vitest';
import { movie, movieDetails, watch } from '../test/fixtures';
import { json, noContent } from '../test/mockApi';
import { createApi, renderApp, signIn } from '../test/renderApp';

const alien = movie({ id: 1, title: 'Alien' });
const logs = [
  watch({ id: 1, dateWatched: '2025-06-14T00:00:00', rating: 5, notes: 'Still the best.' }),
  watch({ id: 2, dateWatched: '2021-02-02T00:00:00', rating: null, notes: null }),
];

describe('Movie details page', () => {
  beforeEach(() => signIn());

  it('shows the movie, the user’s stats and every log', async () => {
    createApi().on('GET', '/api/movies/1', movieDetails(alien, logs));
    renderApp('/movies/1');

    expect(await screen.findByRole('heading', { level: 1, name: 'Alien' })).toBeInTheDocument();
    expect(screen.getByText('1979 · 117 min · Directed by Ridley Scott')).toBeInTheDocument();
    expect(within(screen.getByRole('list', { name: 'Genres' })).getAllByRole('listitem')).toHaveLength(2);
    expect(screen.getByText('Added to the catalogue by you on 12 Mar 2025')).toBeInTheDocument();

    const history = screen.getByRole('region', { name: 'Your History' });
    const items = within(history).getAllByRole('listitem');
    expect(items[0]).toHaveTextContent('14 Jun 2025');
    expect(items[1]).toHaveTextContent('No rating');
    expect(items[1]).toHaveTextContent('No notes');
    expect(within(items[0]).getByRole('link', { name: 'Edit log from 14 Jun 2025' })).toHaveAttribute('href', '/logs/1/edit');
  });

  it('asks for confirmation before deleting a log, then refreshes the history', async () => {
    let current = logs;
    const api = createApi()
      .on('GET', '/api/movies/1', () => json(200, movieDetails(alien, current)))
      .on('DELETE', '/api/moviewatches/2', () => {
        current = logs.slice(0, 1);
        return noContent();
      });
    const { user } = renderApp('/movies/1');

    await user.click(await screen.findByRole('button', { name: 'Delete log from 02 Feb 2021' }));
    const dialog = screen.getByRole('alertdialog', { name: 'Delete this log?' });
    // Destructive dialogs start on the safe option.
    expect(within(dialog).getByRole('button', { name: 'Cancel' })).toHaveFocus();

    await user.click(within(dialog).getByRole('button', { name: 'Cancel' }));
    expect(api.calls('DELETE', '/api/moviewatches/2')).toHaveLength(0);

    await user.click(screen.getByRole('button', { name: 'Delete log from 02 Feb 2021' }));
    await user.click(screen.getByRole('button', { name: 'Delete Log' }));

    await waitFor(() => expect(screen.queryByRole('alertdialog')).not.toBeInTheDocument());
    expect(api.calls('DELETE', '/api/moviewatches/2')).toHaveLength(1);
    expect(await screen.findByText('Deleted your log from 02 Feb 2021.')).toBeInTheDocument();
    await waitFor(() => expect(screen.queryByText('02 Feb 2021')).not.toBeInTheDocument());
  });

  it('closes the dialog with Escape', async () => {
    createApi().on('GET', '/api/movies/1', movieDetails(alien, logs));
    const { user } = renderApp('/movies/1');

    const trigger = await screen.findByRole('button', { name: 'Delete log from 14 Jun 2025' });
    await user.click(trigger);
    await user.keyboard('{Escape}');

    expect(screen.queryByRole('alertdialog')).not.toBeInTheDocument();
    expect(trigger).toHaveFocus();
  });

  it('adds to and removes from the watchlist', async () => {
    let onWatchlist = false;
    const api = createApi()
      .on('GET', '/api/movies/1', movieDetails(alien))
      .on('GET', '/api/watchlist', () => json(200, onWatchlist ? [{ ...alien, movieId: 1, id: 9, genres: [], dateAdded: '2026-09-01T00:00:00' }] : []))
      .on('POST', '/api/watchlist/1', () => {
        onWatchlist = true;
        return json(201, {});
      })
      .on('DELETE', '/api/watchlist/1', () => {
        onWatchlist = false;
        return noContent();
      });
    const { user } = renderApp('/movies/1');

    await user.click(await screen.findByRole('button', { name: 'Add to Watchlist' }));
    const remove = await screen.findByRole('button', { name: 'Remove from Watchlist' });
    expect(remove).toHaveAttribute('aria-pressed', 'true');

    await user.click(remove);
    expect(await screen.findByRole('button', { name: 'Add to Watchlist' })).toBeInTheDocument();
    expect(api.calls('DELETE', '/api/watchlist/1')).toHaveLength(1);
  });

  it('shows a not-found state for an unknown movie', async () => {
    createApi().on('GET', '/api/movies/404', json(404));
    renderApp('/movies/404');

    expect(await screen.findByText('Movie not found')).toBeInTheDocument();
  });
});
