import { createContext, useContext } from 'react';
import type { LoginRequest, RegisterRequest, User } from '../api/types';

export type AuthStatus = 'loading' | 'authenticated' | 'unauthenticated' | 'error';

export interface AuthContextValue {
  status: AuthStatus;
  /** The signed-in user, as returned by the API. Never supplied by the user. */
  user: User | null;
  /** True after the user was signed out because their token expired or was rejected. */
  sessionExpired: boolean;
  login: (request: LoginRequest, remember: boolean) => Promise<void>;
  register: (request: RegisterRequest) => Promise<void>;
  logout: () => void;
  /** Replaces the cached user after a profile update. */
  setUser: (user: User) => void;
  /** Retries restoring the session after the API couldn't be reached. */
  retry: () => void;
}

export const AuthContext = createContext<AuthContextValue | null>(null);

export function useAuth(): AuthContextValue {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
}

/** The signed-in user. Only use inside routes protected by RequireAuth. */
export function useCurrentUser(): User {
  const { user } = useAuth();
  if (!user) {
    throw new Error('useCurrentUser called without a signed-in user');
  }
  return user;
}
