import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { json, MockApi, noContent, validationProblem } from '../test/mockApi';
import { signIn, TEST_TOKEN } from '../test/renderApp';
import { ApiError, apiRequest, errorMessage, NETWORK_ERROR_STATUS, setUnauthorizedHandler } from './client';
import { movieWatchesApi, moviesApi } from './endpoints';

describe('apiRequest', () => {
  let api: MockApi;

  beforeEach(() => {
    api = new MockApi().install();
  });

  afterEach(() => {
    setUnauthorizedHandler(null);
  });

  it('sends the stored token as a bearer token', async () => {
    signIn();
    api.on('GET', '/api/dashboard', {});

    await apiRequest('/api/dashboard');

    expect(api.requests[0].headers.Authorization).toBe(`Bearer ${TEST_TOKEN}`);
  });

  it("doesn't send a token for anonymous endpoints", async () => {
    signIn();
    api.on('POST', '/api/auth/login', { token: 't', expiresAt: '2030-01-01T00:00:00', user: {} });

    await apiRequest('/api/auth/login', { method: 'POST', body: {}, auth: false });

    expect(api.requests[0].headers.Authorization).toBeUndefined();
  });

  it('normalises ASP.NET validation errors to camelCase field names', async () => {
    api.on(
      'POST',
      '/api/moviewatches',
      validationProblem({ DateWatched: ['DateWatched cannot be in the future.'], '$.rating': ['Bad rating.'] }),
    );

    const error = await apiRequest('/api/moviewatches', { method: 'POST', body: {} }).catch((e: unknown) => e);

    expect(error).toBeInstanceOf(ApiError);
    const apiError = error as ApiError;
    expect(apiError.status).toBe(400);
    expect(apiError.fieldError('dateWatched')).toBe('DateWatched cannot be in the future.');
    expect(apiError.fieldError('rating')).toBe('Bad rating.');
    expect(apiError.message).toBe('DateWatched cannot be in the future.');
  });

  it("uses the API's own message for conflicts", async () => {
    api.on('POST', '/api/watchlist/5', json(409, { message: 'This movie is already on your watchlist.' }));

    await expect(apiRequest('/api/watchlist/5', { method: 'POST' })).rejects.toMatchObject({
      status: 409,
      message: 'This movie is already on your watchlist.',
    });
  });

  it.each([
    [403, "You don't have permission to do that."],
    [404, "We couldn't find what you were looking for. It may have been deleted."],
    [500, 'Something went wrong on our side. Please try again in a moment.'],
    [503, 'Something went wrong on our side. Please try again in a moment.'],
  ])('gives a readable message for %i responses', async (status, message) => {
    api.on('GET', '/api/thing', json(status));

    const error = await apiRequest('/api/thing').catch((e: unknown) => e);

    expect(errorMessage(error)).toBe(message);
  });

  it('reports network failures without a status', async () => {
    vi.stubGlobal('fetch', vi.fn().mockRejectedValue(new TypeError('Failed to fetch')));

    await expect(apiRequest('/api/dashboard')).rejects.toMatchObject({ status: NETWORK_ERROR_STATUS });
  });

  it('calls the unauthorized handler with the rejected token when an authenticated call gets 401', async () => {
    signIn();
    const handler = vi.fn();
    setUnauthorizedHandler(handler);
    api.on('GET', '/api/dashboard', json(401));

    await expect(apiRequest('/api/dashboard')).rejects.toMatchObject({ status: 401 });
    expect(handler).toHaveBeenCalledWith(TEST_TOKEN);
  });

  it("doesn't treat a failed login (401 without a token) as an expired session", async () => {
    const handler = vi.fn();
    setUnauthorizedHandler(handler);
    api.on('POST', '/api/auth/login', json(401, { message: 'Invalid email or password.' }));

    await expect(apiRequest('/api/auth/login', { method: 'POST', body: {}, auth: false })).rejects.toMatchObject({
      message: 'Invalid email or password.',
    });
    expect(handler).not.toHaveBeenCalled();
  });

  it('returns undefined for 204 responses', async () => {
    api.on('DELETE', '/api/watchlist/5', noContent());

    await expect(apiRequest('/api/watchlist/5', { method: 'DELETE' })).resolves.toBeUndefined();
  });
});

describe('endpoints', () => {
  it('never asks for more than the maximum page size and omits empty filters', async () => {
    const api = new MockApi()
      .on('GET', '/api/movies', { items: [], totalCount: 0, page: 1, pageSize: 100 })
      .on('GET', '/api/moviewatches/my-movies', { items: [], totalCount: 0, page: 1, pageSize: 100 })
      .install();

    await moviesApi.search({ title: '', page: 1, pageSize: 500 });
    await movieWatchesApi.myMovies({ sort: 'TitleAsc', page: 2, pageSize: 1000, search: 'alien' });

    expect(api.requests[0].query.get('pageSize')).toBe('100');
    expect(api.requests[0].query.has('title')).toBe(false);
    expect(Object.fromEntries(api.requests[1].query)).toEqual({ sort: 'TitleAsc', page: '2', pageSize: '100', search: 'alien' });
  });
});
