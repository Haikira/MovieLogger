import { screen, waitFor, within } from '@testing-library/react';
import { beforeEach, describe, expect, it } from 'vitest';
import type { RecordedRequest } from '../test/mockApi';
import { json } from '../test/mockApi';
import { movie, paged, watch, watchlistItem } from '../test/fixtures';
import { createApi, currentLocation, renderApp, signIn } from '../test/renderApp';

function searchResults(request: RecordedRequest) {
  const page = Number(request.query.get('page'));
  const title = request.query.get('title') ?? '';
  const items = [movie({ id: 1, title: `${title || 'Any'} result p${page}` })];
  return json(200, paged(items, page, 8, 20));
}

describe('Search page', () => {
  beforeEach(() => signIn());

  it('debounces typing into a single search request', async () => {
    const api = createApi().on('GET', '/api/movies', searchResults);
    const { user } = renderApp('/search');
    await screen.findByText('Any result p1');

    await user.type(screen.getByRole('searchbox', { name: /Search the catalogue/ }), 'alien');

    expect(await screen.findByText('alien result p1')).toBeInTheDocument();
    const titles = api.calls('GET', '/api/movies').map((r) => r.query.get('title'));
    // One request for the initial catalogue page and one for the finished word; none per keystroke.
    expect(titles).toEqual([null, 'alien']);
    expect(currentLocation()).toBe('/search?q=alien');
  });

  it('pages through results and goes back to page 1 when the search changes', async () => {
    const api = createApi().on('GET', '/api/movies', searchResults);
    const { user } = renderApp('/search?q=alien');
    await screen.findByText('alien result p1');
    expect(screen.getByText('Showing 1–8 of 20 results for “alien”')).toBeInTheDocument();

    await user.click(screen.getByRole('button', { name: 'Next Page' }));
    expect(await screen.findByText('alien result p2')).toBeInTheDocument();
    expect(currentLocation()).toBe('/search?q=alien&page=2');

    await user.type(screen.getByRole('searchbox'), 's');

    expect(await screen.findByText('aliens result p1')).toBeInTheDocument();
    expect(api.requests.at(-1)?.query.get('page')).toBe('1');
    expect(currentLocation()).toBe('/search?q=aliens');
  });

  it('searches by director or year using the matching API filter', async () => {
    const api = createApi().on('GET', '/api/movies', searchResults);
    const { user } = renderApp('/search');
    await screen.findByText('Any result p1');

    await user.selectOptions(screen.getByLabelText('Search by:'), 'year');
    await user.type(screen.getByRole('searchbox'), '19');
    expect(await screen.findByText('Enter a four-digit year')).toBeInTheDocument();

    await user.type(screen.getByRole('searchbox'), '79');
    await waitFor(() => expect(api.requests.at(-1)?.query.get('year')).toBe('1979'));
    expect(api.requests.at(-1)?.query.has('title')).toBe(false);
  });

  it("does not offer the design's release-date sort, which the API can't do", async () => {
    createApi().on('GET', '/api/movies', searchResults);
    renderApp('/search');
    await screen.findByText('Any result p1');

    expect(screen.queryByText(/Release Date/)).not.toBeInTheDocument();
  });

  it('shows each movie’s history and toggles the watchlist', async () => {
    const api = createApi()
      .on('GET', '/api/movies', paged([movie({ id: 1, title: 'Alien' }), movie({ id: 2, title: 'Alien: Romulus' }), movie({ id: 3, title: 'Alien Nation' })]))
      .on('GET', '/api/moviewatches', [watch({ id: 1, movieId: 1 }), watch({ id: 2, movieId: 1 }), watch({ id: 3, movieId: 1 })])
      .on('GET', '/api/watchlist', [watchlistItem({ movieId: 2, title: 'Alien: Romulus' })])
      .on('POST', '/api/watchlist/3', json(201, watchlistItem({ movieId: 3 })));
    const { user } = renderApp('/search');

    const rows = await screen.findAllByRole('row');
    expect(within(rows[1]).getByText('Watched 3×')).toBeInTheDocument();
    expect(await within(rows[2]).findByText('On watchlist')).toBeInTheDocument();
    expect(within(rows[3]).getByText('Not watched')).toBeInTheDocument();
    expect(within(rows[2]).getByRole('button', { name: 'Remove Alien: Romulus from watchlist' })).toHaveAttribute('aria-pressed', 'true');

    await user.click(within(rows[3]).getByRole('button', { name: 'Add Alien Nation to watchlist' }));

    await waitFor(() => expect(api.calls('POST', '/api/watchlist/3')).toHaveLength(1));
    // The watchlist is refetched so the row reflects the server's state.
    await waitFor(() => expect(api.calls('GET', '/api/watchlist').length).toBeGreaterThan(1));
  });

  it('treats "already on watchlist" (409) as success', async () => {
    createApi()
      .on('GET', '/api/movies', paged([movie({ id: 3, title: 'Alien Nation' })]))
      .on('POST', '/api/watchlist/3', json(409, { message: 'This movie is already on your watchlist.' }));
    const { user } = renderApp('/search');

    await user.click(await screen.findByRole('button', { name: 'Add Alien Nation to watchlist' }));

    await waitFor(() => expect(screen.getByRole('button', { name: 'Add Alien Nation to watchlist' })).toBeEnabled());
    expect(screen.queryByRole('alert')).not.toBeInTheDocument();
  });

  it('keeps the "add it to the catalogue" route, carrying the search over', async () => {
    createApi().on('GET', '/api/movies', paged([]));
    const { user } = renderApp('/search?q=Past%20Lives');

    await user.click(await screen.findByRole('link', { name: 'Add it to the Catalogue' }));

    expect(await screen.findByRole('heading', { name: 'Add Movie' })).toBeInTheDocument();
    expect(screen.getByLabelText(/^Title/)).toHaveValue('Past Lives');
  });
});
