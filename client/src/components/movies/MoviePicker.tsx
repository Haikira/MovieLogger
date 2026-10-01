import { useId, useState } from 'react';
import { Link } from 'react-router';
import type { Movie } from '../../api/types';
import { useMovieSearch } from '../../hooks/queries';
import { useDebouncedValue } from '../../hooks/useDebouncedValue';
import { formatMovieMeta } from '../../lib/format';
import { Button } from '../ui/Button';
import { Poster, SearchInput } from '../ui/Controls';
import { ErrorState, Spinner } from '../ui/Feedback';
import styles from './MoviePicker.module.css';

const RESULT_LIMIT = 6;

interface MoviePickerProps {
  onSelect: (movie: Movie) => void;
  onCancel?: () => void;
  error?: string;
}

/**
 * Finds a catalogue movie by title. Results are plain buttons (rather than a custom combobox) so
 * they work predictably with keyboards and screen readers.
 */
export function MoviePicker({ onSelect, onCancel, error }: MoviePickerProps) {
  const [text, setText] = useState('');
  const term = useDebouncedValue(text.trim());
  const statusId = useId();
  const results = useMovieSearch({ title: term, page: 1, pageSize: RESULT_LIMIT }, term.length > 0);

  let status = '';
  if (term && results.data) {
    status =
      results.data.totalCount === 0
        ? `No movies found for “${term}”.`
        : results.data.totalCount > RESULT_LIMIT
          ? `Showing the first ${RESULT_LIMIT} of ${results.data.totalCount} matches. Keep typing to narrow them down.`
          : `${results.data.totalCount} ${results.data.totalCount === 1 ? 'match' : 'matches'}.`;
  }

  return (
    <div className={styles.picker}>
      <div className={styles.searchRow}>
        <SearchInput
          label="Search the catalogue for a movie to log"
          placeholder="Search by title..."
          value={text}
          onChange={(e) => setText(e.target.value)}
          aria-describedby={statusId}
          aria-invalid={error ? true : undefined}
          className={styles.search}
          white
          autoFocus
        />
        {onCancel && (
          <Button variant="link" onClick={onCancel}>
            Cancel
          </Button>
        )}
      </div>
      {error && <p className={styles.error}>{error}</p>}

      <p id={statusId} className={styles.status} aria-live="polite">
        {status}
      </p>

      {term && results.isFetching && !results.data && (
        <div className={styles.loading}>
          <Spinner /> Searching…
        </div>
      )}
      {results.isError && <ErrorState error={results.error} title="Search failed" onRetry={() => results.refetch()} />}

      {term && results.data && results.data.items.length > 0 && (
        <ul className={styles.results}>
          {results.data.items.map((movie) => (
            <li key={movie.id}>
              <button type="button" className={styles.result} onClick={() => onSelect(movie)}>
                <Poster url={movie.posterImageUrl} title={movie.title} size="small" />
                <span>
                  <span className={styles.resultTitle}>{movie.title}</span>
                  <span className={styles.resultMeta}>{formatMovieMeta(movie)}</span>
                </span>
              </button>
            </li>
          ))}
        </ul>
      )}

      {term && results.data?.totalCount === 0 && (
        <p className={styles.status}>
          Not in the catalogue yet? <Link to={`/movies/new?title=${encodeURIComponent(term)}`}>Add it first</Link>.
        </p>
      )}
    </div>
  );
}
