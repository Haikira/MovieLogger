export type Section = 'dashboard' | 'my-movies' | 'search' | 'watchlist' | 'settings';

/**
 * Which sidebar item a route belongs to, following the Figma: logging/editing a watch sits under
 * My Movies; movie details and Add Movie sit under Search.
 */
export function sectionForPath(pathname: string): Section | null {
  if (pathname.startsWith('/dashboard')) return 'dashboard';
  if (pathname.startsWith('/my-movies') || pathname.startsWith('/log')) return 'my-movies';
  if (pathname.startsWith('/search') || pathname.startsWith('/movies')) return 'search';
  if (pathname.startsWith('/watchlist')) return 'watchlist';
  if (pathname.startsWith('/settings')) return 'settings';
  return null;
}
