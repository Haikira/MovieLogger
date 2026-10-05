import { Outlet } from 'react-router';
import { Sidebar } from './Sidebar';
import styles from './Layout.module.css';

export function AppLayout() {
  return (
    <div className={styles.shell}>
      <a href="#main-content" className={styles.skipLink}>
        Skip to main content
      </a>
      <Sidebar />
      <main id="main-content" className={styles.main} tabIndex={-1}>
        <Outlet />
      </main>
    </div>
  );
}
