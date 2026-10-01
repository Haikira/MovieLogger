import { screen, waitFor, within } from '@testing-library/react';
import { beforeEach, describe, expect, it } from 'vitest';
import { json, type RecordedRequest } from '../test/mockApi';
import { myMovie, paged } from '../test/fixtures';
import { createApi, currentLocation, renderApp, signIn } from '../test/renderApp';

function myMoviesPage(total: number) {
  return (request: RecordedRequest) => {
    const page = Number(request.query.get('page'));
    const pageSize = Number(request.query.get('pageSize'));
    const first = (page - 1) * pageSize;
    const items = Array.from({ length: Math.max(0, Math.min(pageSize, total - first)) }, (_, i) =>
      myMovie({ movieId: first + i + 1, title: `Movie ${first + i + 1}` }),
    );
    return json(200, paged(items, page, pageSize, total));
  };
}

describe('My Movies page', () => {
  beforeEach(() => signIn());

  it('shows one row per watched movie with its latest date and rating', async () => {
    createApi().on('GET', '/api/moviewatches/my-movies', paged([myMovie({ lastWatchedAt: '2026-09-01T00:00:00', lastRating: 4 })]));
    renderApp('/my-movies');

    const row = (await screen.findAllByRole('row'))[1];
    expect(within(row).getByRole('link', { name: 'Alien' })).toHaveAttribute('href', '/movies/1');
    expect(within(row).getByText('1979 · Ridley Scott · 117 min')).toBeInTheDocument();
    expect(within(row).getByText('01 Sep 2026')).toBeInTheDocument();
    expect(within(row).getByRole('img', { name: '4 out of 5 stars' })).toBeInTheDocument();
  });

  it("edits via the movie's history and has no direct delete (a row isn't a single log)", async () => {
    createApi().on('GET', '/api/moviewatches/my-movies', paged([myMovie()]));
    renderApp('/my-movies');

    const edit = await screen.findByRole('link', { name: 'Edit logs for Alien' });
    expect(edit).toHaveAttribute('href', '/movies/1#history');
    expect(screen.queryByRole('button', { name: /delete/i })).not.toBeInTheDocument();
  });

  it('sends filters and sorting to the API and resets to page 1 when they change', async () => {
    const api = createApi().on('GET', '/api/moviewatches/my-movies', myMoviesPage(20));
    const { user } = renderApp('/my-movies');
    await screen.findByText('Movie 1');

    await user.click(screen.getByRole('button', { name: 'Next Page' }));
    await screen.findByText('Movie 9');
    expect(currentLocation()).toBe('/my-movies?page=2');

    await user.selectOptions(screen.getByLabelText('Rating:'), '5');
    await waitFor(() => expect(currentLocation()).toBe('/my-movies?rating=5'));
    await user.selectOptions(screen.getByLabelText('Genre:'), 'Horror');
    await user.selectOptions(screen.getByLabelText('Sort:'), 'Title A–Z');

    await waitFor(() =>
      expect(Object.fromEntries(api.requests.at(-1)!.query)).toEqual({
        genreId: '10',
        rating: '5',
        sort: 'TitleAsc',
        page: '1',
        pageSize: '8',
      }),
    );
  });

  it('debounces the search box', async () => {
    const api = createApi().on('GET', '/api/moviewatches/my-movies', myMoviesPage(3));
    const { user } = renderApp('/my-movies');
    await screen.findByText('Movie 1');

    await user.type(screen.getByRole('searchbox', { name: 'Search watched movies' }), 'dune');

    await waitFor(() => expect(api.requests.at(-1)?.query.get('search')).toBe('dune'));
    const searches = api.calls('GET', '/api/moviewatches/my-movies').map((r) => r.query.get('search'));
    expect(searches).toEqual([null, 'dune']);
  });

  it('steps back to the last page when the current page no longer has results', async () => {
    createApi().on('GET', '/api/moviewatches/my-movies', myMoviesPage(9));
    renderApp('/my-movies?page=3');

    expect(await screen.findByText('Movie 9')).toBeInTheDocument();
    expect(currentLocation()).toBe('/my-movies?page=2');
  });

  it('distinguishes "nothing logged" from "no matches"', async () => {
    createApi().on('GET', '/api/moviewatches/my-movies', paged([]));
    const { user, unmount } = renderApp('/my-movies');

    expect(await screen.findByText("You haven't logged any movies yet")).toBeInTheDocument();
    unmount();

    renderApp('/my-movies?rating=1');
    expect(await screen.findByText('No movies match your filters')).toBeInTheDocument();
    await user.click(screen.getByRole('button', { name: 'Clear Filters' }));
    await waitFor(() => expect(currentLocation()).toBe('/my-movies'));
  });

  it('shows an error with a retry when the API fails', async () => {
    createApi().on('GET', '/api/moviewatches/my-movies', json(500));
    renderApp('/my-movies');

    expect(await screen.findByText("Couldn't load your movies")).toBeInTheDocument();
    expect(screen.getByText('Something went wrong on our side. Please try again in a moment.')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Try Again' })).toBeInTheDocument();
  });
});
