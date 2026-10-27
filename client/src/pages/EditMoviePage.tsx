import { useNavigate, useParams } from 'react-router';
import { useQueryClient } from '@tanstack/react-query';
import { Clapperboard, Lock } from 'lucide-react';
import { isApiError } from '../api/client';
import { moviesApi } from '../api/endpoints';
import { queryKeys } from '../api/queryKeys';
import type { Movie, UpdateMovieRequest } from '../api/types';
import { useCurrentUser } from '../auth/useAuth';
import { PageHeader } from '../components/layout/PageHeader';
import { MovieForm, type MovieFormAction } from '../components/movies/MovieForm';
import { ButtonLink } from '../components/ui/Button';
import { Card } from '../components/ui/Controls';
import { EmptyState, ErrorState, LoadingState } from '../components/ui/Feedback';
import { useMovieDetails } from '../hooks/queries';
import { useGoBack } from '../hooks/useBackLink';
import { toMovieFormValues } from '../lib/movieValidation';

const ACTIONS: MovieFormAction<'save'>[] = [{ intent: 'save', label: 'Save Changes', busyLabel: 'Saving…', primary: true }];

export function EditMoviePage() {
  const params = useParams();
  const movieId = Number(params.movieId);
  const validId = Number.isInteger(movieId) && movieId > 0;
  const details = useMovieDetails(validId ? movieId : null);
  const user = useCurrentUser();

  let content;
  if (!validId || (details.isError && isApiError(details.error, 404))) {
    content = (
      <Card>
        <EmptyState
          icon={<Clapperboard size={22} />}
          title="Movie not found"
          description="This movie isn't in the catalogue. It may have been removed."
          actions={<ButtonLink to="/search">Search the Catalogue</ButtonLink>}
        />
      </Card>
    );
  } else if (details.isPending) {
    content = (
      <Card>
        <LoadingState label="Loading movie…" />
      </Card>
    );
  } else if (details.isError) {
    content = (
      <Card>
        <ErrorState error={details.error} title="Couldn't load this movie" onRetry={() => details.refetch()} />
      </Card>
    );
  } else if (details.data.movie.createdByUserId !== user.id) {
    content = (
      <Card>
        <EmptyState
          icon={<Lock size={22} />}
          title="You can't edit this movie"
          description="Only the person who added a movie to the catalogue can change its details."
          actions={<ButtonLink to={`/movies/${details.data.movie.id}`}>Back to the Movie</ButtonLink>}
        />
      </Card>
    );
  } else {
    // Keyed by movie so the form starts from that movie's saved details.
    content = <EditMovieForm key={details.data.movie.id} movie={details.data.movie} />;
  }

  return (
    <>
      <PageHeader title="Edit Movie" subtitle="Correct or fill in this movie's details in the shared catalogue." />
      {content}
    </>
  );
}

function EditMovieForm({ movie }: { movie: Movie }) {
  const navigate = useNavigate();
  const goBack = useGoBack(`/movies/${movie.id}`);
  const queryClient = useQueryClient();

  async function handleSubmit(request: UpdateMovieRequest) {
    await moviesApi.update(movie.id, request);
    // Titles, posters and genres also appear in My Movies, the watchlist and the dashboard.
    await Promise.all([
      queryClient.invalidateQueries({ queryKey: queryKeys.movies }),
      queryClient.invalidateQueries({ queryKey: queryKeys.myMovies }),
      queryClient.invalidateQueries({ queryKey: queryKeys.watchlist }),
      queryClient.invalidateQueries({ queryKey: queryKeys.dashboard }),
    ]);
    navigate(`/movies/${movie.id}`, { replace: true });
  }

  return (
    <MovieForm
      initial={toMovieFormValues(movie)}
      actions={ACTIONS}
      onSubmit={handleSubmit}
      onCancel={goBack}
      note={{
        title: 'Shared catalogue',
        content: (
          <p>
            Changes apply to everyone who uses Movie Logger. Watch logs, ratings and watchlists aren&apos;t affected.
          </p>
        ),
      }}
    />
  );
}
