import { screen, waitFor } from '@testing-library/react';
import { beforeEach, describe, expect, it } from 'vitest';
import { genres, movie, movieDetails } from '../test/fixtures';
import { json, noContent, validationProblem } from '../test/mockApi';
import { createApi, currentLocation, renderApp, signIn } from '../test/renderApp';

const alien = movie({ id: 1, title: 'Alien' });

describe('Edit Movie page', () => {
  beforeEach(() => signIn());

  it('starts from the saved details and saves the changes', async () => {
    const api = createApi()
      .on('GET', '/api/movies/1', movieDetails(alien))
      .on('PUT', '/api/movies/1', noContent());
    const { user } = renderApp('/movies/1/edit');

    const title = await screen.findByLabelText(/^Title/);
    expect(title).toHaveValue('Alien');
    expect(screen.getByLabelText(/^Release year/)).toHaveValue('1979');
    expect(screen.getByLabelText('Runtime (minutes)')).toHaveValue('117');
    expect(screen.getByLabelText('Director')).toHaveValue('Ridley Scott');
    // The summary names the genres once the genre list has loaded.
    await waitFor(() => expect(screen.getByRole('button', { name: /^Genres/ })).toHaveTextContent('Horror, Science Fiction'));

    await user.clear(title);
    await user.type(title, 'Alien: Director’s Cut');
    await user.clear(screen.getByLabelText('Runtime (minutes)'));
    await user.click(screen.getByRole('button', { name: /^Genres/ }));
    await user.click(screen.getByRole('checkbox', { name: 'Horror' }));
    await user.click(screen.getByRole('button', { name: 'Save Changes' }));

    await waitFor(() => expect(currentLocation()).toBe('/movies/1'));
    expect(api.calls('PUT', '/api/movies/1')[0].body).toEqual({
      title: 'Alien: Director’s Cut',
      releaseYear: 1979,
      runtimeMinutes: null,
      director: 'Ridley Scott',
      synopsis: 'The crew of the Nostromo answer a distress call.',
      posterImageUrl: null,
      genreIds: [genres[3].id],
    });
  });

  it('checks the fields before saving', async () => {
    const api = createApi().on('GET', '/api/movies/1', movieDetails(alien));
    const { user } = renderApp('/movies/1/edit');

    await user.clear(await screen.findByLabelText(/^Title/));
    await user.click(screen.getByRole('button', { name: 'Save Changes' }));

    expect(await screen.findByText('Enter the movie title.')).toBeInTheDocument();
    expect(api.calls('PUT', '/api/movies/1')).toHaveLength(0);
  });

  it('shows validation errors from the API on their fields', async () => {
    createApi()
      .on('GET', '/api/movies/1', movieDetails(alien))
      .on('PUT', '/api/movies/1', validationProblem({ Director: ['The Director field is too long.'] }));
    const { user } = renderApp('/movies/1/edit');

    await user.click(await screen.findByRole('button', { name: 'Save Changes' }));

    expect(await screen.findByLabelText('Director')).toHaveAccessibleDescription(/The Director field is too long\./);
    expect(currentLocation()).toBe('/movies/1/edit');
  });

  it('shows "not found" for a movie that does not exist', async () => {
    createApi().on('GET', '/api/movies/99', json(404));
    renderApp('/movies/99/edit');

    expect(await screen.findByText('Movie not found')).toBeInTheDocument();
  });
});
