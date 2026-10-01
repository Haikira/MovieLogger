import { useState, type InputHTMLAttributes, type ReactNode, type SelectHTMLAttributes } from 'react';
import { ChevronDown, ImageIcon, Search, Star } from 'lucide-react';
import { Button } from './Button';
import styles from './Controls.module.css';

export interface SearchInputProps extends Omit<InputHTMLAttributes<HTMLInputElement>, 'type'> {
  /** Accessible name for the search box. */
  label: string;
  white?: boolean;
}

export function SearchInput({ label, white, className, ...rest }: SearchInputProps) {
  return (
    <div className={[styles.search, className].filter(Boolean).join(' ')}>
      <Search size={18} className={styles.searchIcon} aria-hidden="true" />
      <input
        type="search"
        aria-label={label}
        className={[styles.searchInput, white && styles.searchWhite].filter(Boolean).join(' ')}
        {...rest}
      />
    </div>
  );
}

export interface FilterOption {
  value: string;
  label: string;
}

export interface FilterSelectProps extends Omit<SelectHTMLAttributes<HTMLSelectElement>, 'children'> {
  /** Visible prefix, e.g. "Genre:"; also the select's accessible name. */
  label: string;
  options: FilterOption[];
}

/** The design's "Genre: All Genres ⌄" control, built on a native select for accessibility. */
export function FilterSelect({ label, options, className, ...rest }: FilterSelectProps) {
  return (
    <label className={[styles.filter, className].filter(Boolean).join(' ')}>
      <span className={styles.filterLabel}>{label}</span>
      <select className={styles.filterSelect} {...rest}>
        {options.map((option) => (
          <option key={option.value} value={option.value}>
            {option.label}
          </option>
        ))}
      </select>
      <ChevronDown size={18} className={styles.filterChevron} aria-hidden="true" />
    </label>
  );
}

interface PaginationProps {
  page: number;
  pageSize: number;
  totalCount: number;
  /** Completes "Showing 1–8 of 184 …", e.g. "watched movies". */
  noun: string;
  suffix?: string;
  onPageChange: (page: number) => void;
}

export function Pagination({ page, pageSize, totalCount, noun, suffix, onPageChange }: PaginationProps) {
  const pageCount = Math.max(1, Math.ceil(totalCount / pageSize));
  const first = totalCount === 0 ? 0 : (page - 1) * pageSize + 1;
  const last = Math.min(page * pageSize, totalCount);

  return (
    <nav className={styles.pagination} aria-label="Pagination">
      <p className={styles.paginationSummary} aria-live="polite">
        Showing {first}–{last} of {totalCount} {noun}
        {suffix ? ` ${suffix}` : ''}
      </p>
      <div className={styles.paginationButtons}>
        <Button variant="secondary" disabled={page <= 1} onClick={() => onPageChange(page - 1)}>
          Previous
        </Button>
        <Button disabled={page >= pageCount} onClick={() => onPageChange(page + 1)}>
          Next Page
        </Button>
      </div>
    </nav>
  );
}

interface StarsProps {
  rating: number | null | undefined;
  size?: number;
  /** Text shown when there is no rating. */
  emptyText?: string;
}

/** Read-only 1–5 star rating with a text alternative. */
export function Stars({ rating, size = 16, emptyText = 'No rating' }: StarsProps) {
  if (!rating) {
    return <span className={styles.noRating}>{emptyText}</span>;
  }
  return (
    <span className={styles.stars} role="img" aria-label={`${rating} out of 5 stars`}>
      {[1, 2, 3, 4, 5].map((n) => (
        <Star
          key={n}
          size={size}
          aria-hidden="true"
          fill={n <= rating ? 'currentColor' : 'none'}
          className={n <= rating ? undefined : styles.starEmpty}
        />
      ))}
    </span>
  );
}

type PosterSize = 'small' | 'thumb' | 'card' | 'large';

const POSTER_CLASS: Record<PosterSize, string> = {
  small: styles.posterSmall,
  thumb: styles.posterThumb,
  card: styles.posterCard,
  large: styles.posterLarge,
};

const ICON_SIZE: Record<PosterSize, number> = { small: 12, thumb: 14, card: 32, large: 32 };

interface PosterProps {
  url: string | null | undefined;
  title: string;
  size: PosterSize;
  className?: string;
}

/** A movie poster, falling back to the design's placeholder when there is no image or it fails to load. */
export function Poster({ url, title, size, className }: PosterProps) {
  const [failedUrl, setFailedUrl] = useState<string | null>(null);
  const showImage = url && failedUrl !== url;
  return (
    <div className={[styles.poster, POSTER_CLASS[size], className].filter(Boolean).join(' ')}>
      {showImage ? (
        <img src={url} alt={`Poster for ${title}`} loading="lazy" onError={() => setFailedUrl(url)} />
      ) : (
        <ImageIcon size={ICON_SIZE[size]} aria-hidden="true" />
      )}
    </div>
  );
}

interface CardProps {
  children: ReactNode;
  padded?: boolean;
  className?: string;
  as?: 'section' | 'div' | 'aside';
  labelledBy?: string;
}

export function Card({ children, padded = true, className, as: Tag = 'div', labelledBy }: CardProps) {
  return (
    <Tag
      className={[styles.card, padded && styles.cardPadded, className].filter(Boolean).join(' ')}
      aria-labelledby={labelledBy}
    >
      {children}
    </Tag>
  );
}

export function Toolbar({ children, end }: { children?: ReactNode; end?: ReactNode }) {
  return (
    <div className={styles.toolbar}>
      {children}
      {end && <div className={styles.toolbarEnd}>{end}</div>}
    </div>
  );
}
