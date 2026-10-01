import type { ReactNode } from 'react';
import { Logo } from '../layout/Logo';
import { Spinner } from './Feedback';
import feedback from './Feedback.module.css';

interface FullPageMessageProps {
  title: string;
  description?: string;
  busy?: boolean;
  children?: ReactNode;
}

export function FullPageMessage({ title, description, busy, children }: FullPageMessageProps) {
  return (
    <main className={feedback.fullPage}>
      <div className={feedback.state} role={busy ? 'status' : undefined}>
        <Logo />
        {busy && <Spinner />}
        <h1 className={feedback.stateTitle}>{title}</h1>
        {description && <p className={feedback.stateDescription}>{description}</p>}
        {children && <div className={feedback.stateActions}>{children}</div>}
      </div>
    </main>
  );
}
