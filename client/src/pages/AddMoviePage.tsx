import { useNavigate, useSearchParams } from 'react-router';
import { useQueryClient } from '@tanstack/react-query';
import { moviesApi } from '../api/endpoints';
import { queryKeys } from '../api/queryKeys';
import type { CreateMovieRequest } from '../api/types';
import { PageHeader } from '../components/layout/PageHeader';
import { MovieForm, type MovieFormAction } from '../components/movies/MovieForm';
import { useGoBack } from '../hooks/useBackLink';
import styles from './AddMoviePage.module.css';

type Intent = 'add' | 'add-and-log';

const ACTIONS: MovieFormAction<Intent>[] = [
  { intent: 'add-and-log', label: 'Add & Log Movie', busyLabel: 'Adding…' },
  { intent: 'add', label: 'Add to Catalogue', busyLabel: 'Adding…', primary: true },
];

export function AddMoviePage() {
  const navigate = useNavigate();
  const goBack = useGoBack('/search');
  const [params] = useSearchParams();
  const queryClient = useQueryClient();

  async function handleSubmit(request: CreateMovieRequest, intent: Intent) {
    const movie = await moviesApi.create(request);
    await queryClient.invalidateQueries({ queryKey: queryKeys.movies });
    navigate(intent === 'add-and-log' ? `/log?movieId=${movie.id}` : `/movies/${movie.id}`, { replace: true });
  }

  return (
    <>
      <PageHeader title="Add Movie" subtitle="Add an existing movie to the Movie Logger catalogue so it can be searched and logged." />
      <MovieForm
        initial={{
          title: params.get('title') ?? '',
          releaseYear: '',
          runtimeMinutes: '',
          director: '',
          genreIds: [],
          synopsis: '',
          posterImageUrl: '',
        }}
        actions={ACTIONS}
        onSubmit={handleSubmit}
        onCancel={goBack}
        note={{
          title: 'What happens next',
          content: (
            <ol className={styles.steps}>
              <li>The movie is added to the shared catalogue.</li>
              <li>Anyone can find it in Search.</li>
              <li>Log it with a date, rating and notes whenever you watch it.</li>
            </ol>
          ),
        }}
      />
    </>
  );
}
