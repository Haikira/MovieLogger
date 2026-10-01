import type { ComponentType } from 'react';
import { Link, useLocation } from 'react-router';
import { Bookmark, Film, LayoutDashboard, Search, Settings, type LucideProps } from 'lucide-react';
import { useCurrentUser } from '../../auth/useAuth';
import { useDashboard } from '../../hooks/queries';
import { initialOf } from '../../lib/format';
import { Logo } from './Logo';
import { sectionForPath, type Section } from './navigation';
import styles from './Layout.module.css';

interface NavItem {
  section: Section;
  to: string;
  label: string;
  icon: ComponentType<LucideProps>;
}

const NAV_ITEMS: NavItem[] = [
  { section: 'dashboard', to: '/dashboard', label: 'Dashboard', icon: LayoutDashboard },
  { section: 'my-movies', to: '/my-movies', label: 'My Movies', icon: Film },
  { section: 'search', to: '/search', label: 'Search', icon: Search },
  { section: 'watchlist', to: '/watchlist', label: 'Watchlist', icon: Bookmark },
  { section: 'settings', to: '/settings', label: 'Settings', icon: Settings },
];

export function Sidebar() {
  const { pathname } = useLocation();
  const user = useCurrentUser();
  const dashboard = useDashboard();
  const active = sectionForPath(pathname);
  const logged = dashboard.data?.totalMoviesLogged;

  return (
    <aside className={styles.sidebar}>
      <Link to="/dashboard" className={styles.sidebarLogo} aria-label="Movie Logger dashboard">
        <Logo />
      </Link>
      <nav className={styles.nav} aria-label="Main">
        <ul>
          {NAV_ITEMS.map(({ section, to, label, icon: Icon }) => {
            const isActive = section === active;
            return (
              <li key={section}>
                <Link
                  to={to}
                  className={[styles.navLink, isActive && styles.navLinkActive].filter(Boolean).join(' ')}
                  // "page" when on the item itself; "true" when on a page within its section.
                  aria-current={isActive ? (pathname === to ? 'page' : 'true') : undefined}
                >
                  <Icon size={22} aria-hidden="true" />
                  {label}
                </Link>
              </li>
            );
          })}
        </ul>
      </nav>
      <Link to="/settings" className={styles.userCard} aria-label={`Account settings for ${user.displayName}`}>
        <span className={styles.avatar} aria-hidden="true">
          {initialOf(user.displayName)}
        </span>
        <span className={styles.userText}>
          <span className={styles.userName}>
            {user.displayName}
          </span>
          {logged !== undefined && (
            <span className={styles.userMeta}>
              {logged} {logged === 1 ? 'Movie' : 'Movies'} Logged
            </span>
          )}
        </span>
      </Link>
    </aside>
  );
}
