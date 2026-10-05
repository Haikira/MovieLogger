import { Link } from 'react-router';
import { useBackState } from '../../hooks/useBackLink';
import { formatMovieMeta } from '../../lib/format';
import { Poster } from '../ui/Controls';
import styles from './MovieTable.module.css';

interface MovieCellProps {
  movieId: number;
  /** Label for the details page's back link, e.g. "Back to search results". */
  backLabel: string;
  title: string;
  releaseYear: number;
  director?: string | null;
  runtimeMinutes?: number | null;
  posterImageUrl?: string | null;
}

/** Thumbnail, linked title and "year · director · runtime" line used in every movie table. */
export function MovieCell({
  movieId,
  backLabel,
  title,
  releaseYear,
  director,
  runtimeMinutes,
  posterImageUrl,
}: MovieCellProps) {
  const backState = useBackState(backLabel);
  return (
    <div className={styles.movieCell}>
      <Poster url={posterImageUrl} title={title} size="thumb" />
      <div>
        <Link to={`/movies/${movieId}`} state={backState} className={styles.movieTitle}>
          {title}
        </Link>
        <div className={styles.movieMeta}>{formatMovieMeta({ releaseYear, director, runtimeMinutes })}</div>
      </div>
    </div>
  );
}
