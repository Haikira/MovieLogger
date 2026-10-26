import { screen, waitFor } from '@testing-library/react';
import { beforeEach, describe, expect, it } from 'vitest';
import { movie } from '../test/fixtures';
import { json } from '../test/mockApi';
import { createApi, currentLocation, renderApp, signIn } from '../test/renderApp';

describe('Add Movie page', () => {
  beforeEach(() => signIn());

  it('adds the movie to the catalogue and opens it', async () => {
    const api = createApi().on('POST', '/api/movies', json(201, movie({ id: 42, title: 'Gattaca' })));
    const { user } = renderApp('/movies/new?title=Gattaca');

    expect(await screen.findByLabelText(/^Title/)).toHaveValue('Gattaca');
    await user.type(screen.getByLabelText(/^Release year/), '1997');
    await user.click(screen.getByRole('button', { name: /^Genres/ }));
    await user.click(screen.getByRole('checkbox', { name: 'Drama' }));
    await user.click(screen.getByRole('button', { name: 'Add to Catalogue' }));

    await waitFor(() => expect(currentLocation()).toBe('/movies/42'));
    expect(api.calls('POST', '/api/movies')[0].body).toEqual({
      title: 'Gattaca',
      releaseYear: 1997,
      runtimeMinutes: null,
      director: null,
      synopsis: null,
      posterImageUrl: null,
      genreIds: [7],
    });
  });

  it('can add the movie and go straight to logging it', async () => {
    createApi().on('POST', '/api/movies', json(201, movie({ id: 42, title: 'Gattaca' })));
    const { user } = renderApp('/movies/new?title=Gattaca');

    await user.type(await screen.findByLabelText(/^Release year/), '1997');
    await user.click(screen.getByRole('button', { name: /^Genres/ }));
    await user.click(screen.getByRole('checkbox', { name: 'Drama' }));
    await user.click(screen.getByRole('button', { name: 'Add & Log Movie' }));

    await waitFor(() => expect(currentLocation()).toBe('/log?movieId=42'));
  });
});
