import { fireEvent, screen, waitFor } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { movie, movieDetails, paged, watch, watchlistItem } from '../test/fixtures';
import { json, noContent, validationProblem } from '../test/mockApi';
import { createApi, currentLocation, renderApp, signIn } from '../test/renderApp';

const alien = movie({ id: 1, title: 'Alien' });

describe('Log Movie page', () => {
  beforeEach(() => {
    // Only Date is faked, so timers (debounce, user-event) run normally.
    vi.useFakeTimers({ toFake: ['Date'] });
    vi.setSystemTime(new Date(2026, 8, 28, 15, 0));
    signIn();
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  it('defaults the date to today and shows the movie and its history', async () => {
    createApi().on('GET', '/api/movies/1', movieDetails(alien, [watch({ dateWatched: '2025-06-14T00:00:00', rating: 5 })]));
    renderApp('/log?movieId=1');

    expect(await screen.findByText('1979 · Ridley Scott · 117 min')).toBeInTheDocument();
    const date = screen.getByLabelText(/^Date watched/);
    expect(date).toHaveValue('2026-09-28');
    expect(date).toHaveAttribute('max', '2026-09-28');
    expect(await screen.findByText('Your history with Alien')).toBeInTheDocument();
    expect(screen.getByText('14 Jun 2025')).toBeInTheDocument();
  });

  it('rejects a future date without calling the API', async () => {
    const api = createApi().on('GET', '/api/movies/1', movieDetails(alien));
    const { user } = renderApp('/log?movieId=1');
    await screen.findByText('1979 · Ridley Scott · 117 min');

    fireEvent.change(screen.getByLabelText(/^Date watched/), { target: { value: '2026-09-29' } });
    await user.click(screen.getByRole('button', { name: 'Save Log' }));

    expect(screen.getByText("The date watched can't be in the future.")).toBeInTheDocument();
    expect(api.calls('POST', '/api/moviewatches')).toHaveLength(0);
  });

  it('saves a calendar date, an optional clearable rating and notes', async () => {
    const api = createApi()
      .on('GET', '/api/movies/1', movieDetails(alien))
      .on('POST', '/api/moviewatches', json(201, watch()));
    const { user } = renderApp('/log?movieId=1');
    await screen.findByText('1979 · Ridley Scott · 117 min');

    await user.click(screen.getByLabelText('4 stars'));
    expect(screen.getByText('4 / 5')).toBeInTheDocument();
    await user.click(screen.getByRole('button', { name: 'Clear rating' }));
    expect(screen.queryByText('4 / 5')).not.toBeInTheDocument();
    await user.click(screen.getByLabelText('3 stars'));

    await user.type(screen.getByLabelText('Notes (optional)'), 'Tense.');
    expect(screen.getByText('6 / 500 characters')).toBeInTheDocument();
    await user.click(screen.getByRole('button', { name: 'Save Log' }));

    await waitFor(() => expect(currentLocation()).toBe('/movies/1'));
    expect(api.calls('POST', '/api/moviewatches')[0].body).toEqual({
      movieId: 1,
      dateWatched: '2026-09-28',
      rating: 3,
      notes: 'Tense.',
    });
  });

  it('shows API validation errors against the field', async () => {
    createApi()
      .on('GET', '/api/movies/1', movieDetails(alien))
      .on('POST', '/api/moviewatches', validationProblem({ DateWatched: ['DateWatched cannot be in the future.'] }));
    const { user } = renderApp('/log?movieId=1');
    await screen.findByText('1979 · Ridley Scott · 117 min');

    await user.click(screen.getByRole('button', { name: 'Save Log' }));

    expect(await screen.findByText('DateWatched cannot be in the future.')).toBeInTheDocument();
    expect(screen.getByLabelText(/^Date watched/)).toHaveAttribute('aria-invalid', 'true');
    expect(screen.getByText('Please fix the highlighted fields.')).toBeInTheDocument();
  });

  it('lets the user pick a movie when none was chosen', async () => {
    const api = createApi()
      .on('GET', '/api/movies', paged([alien]))
      .on('GET', '/api/movies/1', movieDetails(alien));
    const { user } = renderApp('/log');

    await user.type(await screen.findByRole('searchbox', { name: /movie to log/ }), 'ali');
    await user.click(await screen.findByRole('button', { name: /Alien/ }));

    expect(await screen.findByRole('button', { name: 'Change movie (currently Alien)' })).toBeInTheDocument();
    expect(api.calls('GET', '/api/movies')[0].query.get('title')).toBe('ali');
  });

  it('offers (but does not force) removal from the watchlist after logging a watchlisted movie', async () => {
    const api = createApi()
      .on('GET', '/api/movies/1', movieDetails(alien))
      .on('GET', '/api/watchlist', [watchlistItem({ movieId: 1, title: 'Alien' })])
      .on('POST', '/api/moviewatches', json(201, watch()))
      .on('DELETE', '/api/watchlist/1', noContent());
    const { user } = renderApp('/log?movieId=1&from=watchlist');
    await screen.findByText('1979 · Ridley Scott · 117 min');

    await user.click(screen.getByRole('button', { name: 'Save Log' }));

    const dialog = await screen.findByRole('dialog', { name: 'Log saved' });
    expect(dialog).toHaveTextContent('Alien is still on your watchlist');
    expect(api.calls('DELETE', '/api/watchlist/1')).toHaveLength(0);

    await user.click(screen.getByRole('button', { name: 'Remove from Watchlist' }));

    await waitFor(() => expect(currentLocation()).toBe('/watchlist'));
    expect(api.calls('DELETE', '/api/watchlist/1')).toHaveLength(1);
  });

  it('keeps the movie on the watchlist if the user chooses to', async () => {
    const api = createApi()
      .on('GET', '/api/movies/1', movieDetails(alien))
      .on('GET', '/api/watchlist', [watchlistItem({ movieId: 1, title: 'Alien' })])
      .on('POST', '/api/moviewatches', json(201, watch()));
    const { user } = renderApp('/log?movieId=1');
    await screen.findByText('1979 · Ridley Scott · 117 min');

    await user.click(screen.getByRole('button', { name: 'Save Log' }));
    await user.click(await screen.findByRole('button', { name: 'Keep on Watchlist' }));

    await waitFor(() => expect(currentLocation()).toBe('/movies/1'));
    expect(api.calls('DELETE', '/api/watchlist/1')).toHaveLength(0);
  });
});

describe('Edit Log page', () => {
  beforeEach(() => signIn());

  it('edits an existing log without shifting its date', async () => {
    const existing = watch({ id: 55, movieId: 1, dateWatched: '2023-10-31T00:00:00', rating: 4, notes: 'Halloween rewatch.' });
    const api = createApi()
      .on('GET', '/api/moviewatches/55', existing)
      .on('GET', '/api/movies/1', movieDetails(alien, [existing]))
      .on('PUT', '/api/moviewatches/55', noContent());
    const { user } = renderApp('/logs/55/edit');

    const date = await screen.findByLabelText(/^Date watched/);
    expect(date).toHaveValue('2023-10-31');
    expect(screen.getByLabelText('Notes (optional)')).toHaveValue('Halloween rewatch.');
    expect(screen.queryByRole('button', { name: /Change movie/ })).not.toBeInTheDocument();

    await user.click(screen.getByRole('button', { name: 'Clear rating' }));
    await user.click(screen.getByRole('button', { name: 'Save Changes' }));

    await waitFor(() => expect(currentLocation()).toBe('/movies/1'));
    expect(api.calls('PUT', '/api/moviewatches/55')[0].body).toEqual({
      dateWatched: '2023-10-31',
      rating: null,
      notes: 'Halloween rewatch.',
    });
  });

  it('shows "not found" for a log that does not exist or is not the user\'s', async () => {
    createApi().on('GET', '/api/moviewatches/99', json(404));
    renderApp('/logs/99/edit');

    expect(await screen.findByText('Log not found')).toBeInTheDocument();
  });
});
