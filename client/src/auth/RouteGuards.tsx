import type { ReactNode } from 'react';
import { Navigate, useLocation } from 'react-router';
import { FullPageMessage } from '../components/ui/FullPageMessage';
import { Button } from '../components/ui/Button';
import { useAuth } from './useAuth';

export interface RedirectState {
  from?: string;
}

function SessionGate({ children }: { children: ReactNode }) {
  const { status, retry, logout } = useAuth();
  if (status === 'loading') {
    return <FullPageMessage busy title="Loading Movie Logger…" />;
  }
  if (status === 'error') {
    return (
      <FullPageMessage
        title="Can't reach Movie Logger"
        description="We couldn't check your session because the server didn't respond. Check your connection and try again."
      >
        <Button onClick={retry}>Try Again</Button>
        <Button variant="secondary" onClick={logout}>
          Log Out
        </Button>
      </FullPageMessage>
    );
  }
  return <>{children}</>;
}

/** Only renders its children for a signed-in user; otherwise redirects to the login page. */
export function RequireAuth({ children }: { children: ReactNode }) {
  const { status } = useAuth();
  const location = useLocation();

  if (status === 'unauthenticated') {
    const state: RedirectState = { from: `${location.pathname}${location.search}` };
    return <Navigate to="/login" replace state={state} />;
  }
  return <SessionGate>{children}</SessionGate>;
}

/** For the login/register pages: a signed-in user is sent on to the app instead. */
export function PublicOnly({ children }: { children: ReactNode }) {
  const { status } = useAuth();
  const location = useLocation();

  if (status === 'authenticated') {
    const from = (location.state as RedirectState | null)?.from;
    // Only follow same-app paths ("//host" would be a protocol-relative URL to another site).
    const target = from && from.startsWith('/') && !from.startsWith('//') ? from : '/dashboard';
    return <Navigate to={target} replace />;
  }
  return <SessionGate>{children}</SessionGate>;
}
