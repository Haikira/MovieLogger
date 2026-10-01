import type { CreateMovieRequest } from '../api/types';
import { isValidHttpUrl } from './validation';

export interface MovieFormValues {
  title: string;
  releaseYear: string;
  runtimeMinutes: string;
  director: string;
  genreIds: number[];
  synopsis: string;
  posterImageUrl: string;
}

export type MovieField = keyof MovieFormValues;

const WHOLE_NUMBER = /^\d+$/;

/** Mirrors CreateMovieDto's rules, plus the design's "at least one genre" requirement. */
export function validateMovie(values: MovieFormValues): Partial<Record<MovieField, string>> {
  const errors: Partial<Record<MovieField, string>> = {};
  const title = values.title.trim();
  if (!title) {
    errors.title = 'Enter the movie title.';
  } else if (title.length > 200) {
    errors.title = 'Title must be 200 characters or fewer.';
  }

  const year = values.releaseYear.trim();
  if (!year) {
    errors.releaseYear = 'Enter the release year.';
  } else if (!WHOLE_NUMBER.test(year) || Number(year) < 1888 || Number(year) > 2200) {
    errors.releaseYear = 'Enter a year between 1888 and 2200.';
  }

  const runtime = values.runtimeMinutes.trim();
  if (runtime && (!WHOLE_NUMBER.test(runtime) || Number(runtime) < 1 || Number(runtime) > 1000)) {
    errors.runtimeMinutes = 'Runtime must be a whole number of minutes between 1 and 1000.';
  }

  if (values.director.trim().length > 200) {
    errors.director = 'Director must be 200 characters or fewer.';
  }
  if (values.genreIds.length === 0) {
    errors.genreIds = 'Choose at least one genre.';
  }
  if (values.synopsis.trim().length > 2000) {
    errors.synopsis = 'Synopsis must be 2000 characters or fewer.';
  }

  const poster = values.posterImageUrl.trim();
  if (poster && !isValidHttpUrl(poster)) {
    errors.posterImageUrl = 'Enter a full web address starting with https://';
  } else if (poster.length > 2000) {
    errors.posterImageUrl = 'Poster URL must be 2000 characters or fewer.';
  }
  return errors;
}

export function toCreateMovieRequest(values: MovieFormValues): CreateMovieRequest {
  const optional = (value: string) => value.trim() || null;
  return {
    title: values.title.trim(),
    releaseYear: Number(values.releaseYear.trim()),
    runtimeMinutes: values.runtimeMinutes.trim() ? Number(values.runtimeMinutes.trim()) : null,
    director: optional(values.director),
    synopsis: optional(values.synopsis),
    posterImageUrl: optional(values.posterImageUrl),
    genreIds: values.genreIds,
  };
}
