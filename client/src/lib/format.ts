import type { Genre } from '../api/types';

interface MovieMetaSource {
  releaseYear: number;
  director?: string | null;
  runtimeMinutes?: number | null;
}

/** "1979 · Ridley Scott · 117 min", skipping whatever the catalogue doesn't have. */
export function formatMovieMeta(movie: MovieMetaSource): string {
  const parts: string[] = [String(movie.releaseYear)];
  if (movie.director) {
    parts.push(movie.director);
  }
  if (movie.runtimeMinutes) {
    parts.push(`${movie.runtimeMinutes} min`);
  }
  return parts.join(' · ');
}

export function formatGenres(genres: Genre[]): string {
  return genres.map((g) => g.name).join(', ');
}

export function pluralise(count: number, singular: string, plural = `${singular}s`): string {
  return `${count} ${count === 1 ? singular : plural}`;
}

export function initialOf(name: string): string {
  return name.trim().charAt(0).toUpperCase() || '?';
}
