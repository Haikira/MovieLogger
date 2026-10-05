import { useCallback, useEffect, useMemo, useState, type ReactNode } from 'react';
import { useQueryClient } from '@tanstack/react-query';
import { isApiError, setUnauthorizedHandler } from '../api/client';
import { authApi, usersApi } from '../api/endpoints';
import type { AuthResponse, LoginRequest, RegisterRequest, User } from '../api/types';
import { AuthContext, type AuthContextValue, type AuthStatus } from './useAuth';
import { clearSession, expiryTime, getStoredSession, getStoredToken, saveSession } from './tokenStorage';

// setTimeout can't wait longer than ~24.8 days.
const MAX_TIMEOUT_MS = 2_147_483_647;

export function AuthProvider({ children }: { children: ReactNode }) {
  const queryClient = useQueryClient();
  const [status, setStatus] = useState<AuthStatus>(() => (getStoredSession() ? 'loading' : 'unauthenticated'));
  const [user, setUserState] = useState<User | null>(null);
  const [sessionExpired, setSessionExpired] = useState(false);
  const [restoreAttempt, setRestoreAttempt] = useState(0);

  const endSession = useCallback(
    (expired: boolean) => {
      clearSession();
      queryClient.clear();
      setUserState(null);
      setSessionExpired(expired);
      setStatus('unauthenticated');
    },
    [queryClient],
  );

  // Restore an existing session on load by asking the API who the token belongs to.
  useEffect(() => {
    if (!getStoredSession()) {
      return;
    }
    const controller = new AbortController();
    usersApi
      .me(controller.signal)
      .then((me) => {
        setUserState(me);
        setStatus('authenticated');
      })
      .catch((error: unknown) => {
        if (controller.signal.aborted) {
          return;
        }
        if (isApiError(error, 401) || isApiError(error, 404)) {
          // Token rejected, or the account no longer exists.
          endSession(true);
        } else {
          setStatus('error');
        }
      });
    return () => controller.abort();
  }, [endSession, restoreAttempt]);

  // Any authenticated request rejected with 401 signs the user out, unless it was made with an
  // older token than the one now stored (e.g. a request still in flight from before a re-login).
  useEffect(() => {
    setUnauthorizedHandler((rejectedToken) => {
      const current = getStoredToken();
      if (current === null || current === rejectedToken) {
        endSession(true);
      }
    });
    return () => setUnauthorizedHandler(null);
  }, [endSession]);

  // Sign out when the token expires rather than waiting for the next request to fail.
  useEffect(() => {
    if (status !== 'authenticated') {
      return;
    }
    const session = getStoredSession();
    const delay = session ? Math.min(Math.max(expiryTime(session) - Date.now(), 0), MAX_TIMEOUT_MS) : 0;
    const timer = window.setTimeout(() => endSession(true), delay);
    return () => window.clearTimeout(timer);
  }, [status, endSession]);

  const startSession = useCallback(
    (response: AuthResponse, remember: boolean) => {
      queryClient.clear();
      saveSession({ token: response.token, expiresAt: response.expiresAt }, remember);
      setUserState(response.user);
      setSessionExpired(false);
      setStatus('authenticated');
    },
    [queryClient],
  );

  const login = useCallback(
    async (request: LoginRequest, remember: boolean) => {
      startSession(await authApi.login(request), remember);
    },
    [startSession],
  );

  const register = useCallback(
    async (request: RegisterRequest) => {
      // No "remember me" on the register form, so the new session lasts until the tab closes.
      startSession(await authApi.register(request), false);
    },
    [startSession],
  );

  const logout = useCallback(() => endSession(false), [endSession]);

  const retry = useCallback(() => {
    if (getStoredSession()) {
      setStatus('loading');
      setRestoreAttempt((n) => n + 1);
    } else {
      setStatus('unauthenticated');
    }
  }, []);

  const value = useMemo<AuthContextValue>(
    () => ({ status, user, sessionExpired, login, register, logout, setUser: setUserState, retry }),
    [status, user, sessionExpired, login, register, logout, retry],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}
