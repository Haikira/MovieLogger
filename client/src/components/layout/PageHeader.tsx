import { useEffect, useRef, type ReactNode } from 'react';
import { usePageTitle } from '../../hooks/usePageTitle';
import styles from './Layout.module.css';

interface PageHeaderProps {
  title: string;
  subtitle?: ReactNode;
  actions?: ReactNode;
}

export function PageHeader({ title, subtitle, actions }: PageHeaderProps) {
  usePageTitle(title);
  const headingRef = useRef<HTMLHeadingElement>(null);

  // Move focus to the new page's heading after client-side navigation, so screen reader and
  // keyboard users start at the top of the new content rather than on the link they clicked.
  useEffect(() => {
    headingRef.current?.focus({ preventScroll: true });
  }, []);

  return (
    <header className={styles.pageHeader}>
      <div>
        <h1 ref={headingRef} tabIndex={-1} className={styles.pageTitle}>
          {title}
        </h1>
        {subtitle && <p className={styles.pageSubtitle}>{subtitle}</p>}
      </div>
      {actions && <div className={styles.pageActions}>{actions}</div>}
    </header>
  );
}
