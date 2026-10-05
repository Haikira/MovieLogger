/**
 * Where the JWT lives between page loads. See client/README.md ("Authentication") for the
 * reasoning. In short:
 *
 * - "Remember me" checked: localStorage, so the session survives closing the browser (until the
 *   token's own expiry).
 * - Otherwise: sessionStorage, so the token is gone when the tab is closed.
 *
 * Only the token and its expiry are stored. The user profile is always re-fetched from
 * GET /api/users/me, so the server remains the source of identity.
 */

import { parseServerTimestamp } from '../lib/dates';

const STORAGE_KEY ='movielogger.session';

export interface StoredSession {
  token: string;
  /** UTC timestamp from the API. */
  expiresAt: string;
}

function storages(): Storage[] {
  const result: Storage[] = [];
  try {
    result.push(window.localStorage);
  } catch {
    // Storage can be unavailable (privacy modes); treat as empty.
  }
  try {
    result.push(window.sessionStorage);
  } catch {
    // As above.
  }
  return result;
}

export function saveSession(session: StoredSession, remember: boolean): void {
  clearSession();
  try {
    const storage = remember ? window.localStorage : window.sessionStorage;
    storage.setItem(STORAGE_KEY, JSON.stringify(session));
  } catch {
    // If storage is unavailable the session simply won't survive a reload.
  }
}

export function clearSession(): void {
  for (const storage of storages()) {
    try {
      storage.removeItem(STORAGE_KEY);
    } catch {
      // Ignore.
    }
  }
}

/** The stored session, or null if there is none or it has expired. Expired sessions are removed. */
export function getStoredSession(now: Date = new Date()): StoredSession | null {
  for (const storage of storages()) {
    let raw: string | null;
    try {
      raw = storage.getItem(STORAGE_KEY);
    } catch {
      raw = null;
    }
    if (!raw) {
      continue;
    }
    const session = parse(raw);
    if (!session || expiryTime(session) <= now.getTime()) {
      clearSession();
      return null;
    }
    return session;
  }
  return null;
}

export function getStoredToken(): string | null {
  return getStoredSession()?.token ?? null;
}

/** Milliseconds since epoch at which the session's token expires. */
export function expiryTime(session: StoredSession): number {
  return parseServerTimestamp(session.expiresAt).getTime();
}

function parse(raw: string): StoredSession | null {
  try {
    const value = JSON.parse(raw) as Partial<StoredSession>;
    return typeof value.token === 'string' && typeof value.expiresAt === 'string'
      ? { token: value.token, expiresAt: value.expiresAt }
      : null;
  } catch {
    return null;
  }
}
