import type { ReactNode } from 'react';
import { CircleAlert, CircleCheck, Info } from 'lucide-react';
import { errorMessage } from '../../api/client';
import { Button } from './Button';
import styles from './Feedback.module.css';

export type AlertTone = 'error' | 'info' | 'success';

const ICONS: Record<AlertTone, ReactNode> = {
  error: <CircleAlert size={18} aria-hidden="true" />,
  info: <Info size={18} aria-hidden="true" />,
  success: <CircleCheck size={18} aria-hidden="true" />,
};

interface AlertProps {
  tone?: AlertTone;
  children: ReactNode;
  actions?: ReactNode;
  className?: string;
}

/**
 * Inline message. Errors are announced assertively (role="alert"); info and success messages
 * politely (role="status").
 */
export function Alert({ tone = 'info', children, actions, className }: AlertProps) {
  return (
    <div
      role={tone === 'error' ? 'alert' : 'status'}
      className={[styles.alert, tone !== 'info' && styles[tone], className].filter(Boolean).join(' ')}
    >
      {ICONS[tone]}
      <div className={styles.alertBody}>
        <div>{children}</div>
        {actions && <div className={styles.alertActions}>{actions}</div>}
      </div>
    </div>
  );
}

/** An always-present live region, so messages added later are reliably announced. */
export function FormError({ error }: { error?: string | null }) {
  return (
    <div className="live-region" aria-live="assertive" aria-atomic="true">
      {error && (
        <div className={`${styles.alert} ${styles.error}`}>
          {ICONS.error}
          <div className={styles.alertBody}>{error}</div>
        </div>
      )}
    </div>
  );
}

export function Spinner() {
  return <span className={styles.spinner} aria-hidden="true" />;
}

export function LoadingState({ label = 'Loading…' }: { label?: string }) {
  return (
    <div className={styles.state} role="status">
      <Spinner />
      <span>{label}</span>
    </div>
  );
}

interface ErrorStateProps {
  error: unknown;
  title?: string;
  onRetry?: () => void;
}

export function ErrorState({ error, title = "Something didn't load", onRetry }: ErrorStateProps) {
  return (
    <div className={styles.state} role="alert">
      <div className={styles.stateIcon}>
        <CircleAlert size={22} aria-hidden="true" />
      </div>
      <p className={styles.stateTitle}>{title}</p>
      <p className={styles.stateDescription}>{errorMessage(error)}</p>
      {onRetry && (
        <div className={styles.stateActions}>
          <Button variant="secondary" size="small" onClick={onRetry}>
            Try Again
          </Button>
        </div>
      )}
    </div>
  );
}

interface EmptyStateProps {
  icon: ReactNode;
  title: string;
  description?: ReactNode;
  actions?: ReactNode;
}

export function EmptyState({ icon, title, description, actions }: EmptyStateProps) {
  return (
    <div className={styles.state}>
      <div className={styles.stateIcon} aria-hidden="true">
        {icon}
      </div>
      <p className={styles.stateTitle}>{title}</p>
      {description && <p className={styles.stateDescription}>{description}</p>}
      {actions && <div className={styles.stateActions}>{actions}</div>}
    </div>
  );
}
