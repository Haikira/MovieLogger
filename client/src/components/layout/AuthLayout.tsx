import type { ReactNode } from 'react';
import { usePageTitle } from '../../hooks/usePageTitle';
import { Logo } from './Logo';
import styles from './Layout.module.css';

interface AuthLayoutProps {
  title: string;
  subtitle: string;
  children: ReactNode;
  footer: ReactNode;
}

/** The centred-card layout used by the login and register pages. */
export function AuthLayout({ title, subtitle, children, footer }: AuthLayoutProps) {
  usePageTitle(title);
  return (
    <main className={styles.authPage}>
      <Logo large />
      <div className={styles.authCard}>
        <h1 className={styles.authTitle}>{title}</h1>
        <p className={styles.authSubtitle}>{subtitle}</p>
        {children}
      </div>
      <p className={styles.authFooter}>{footer}</p>
    </main>
  );
}
