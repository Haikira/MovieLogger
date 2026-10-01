import { describe, expect, it } from 'vitest';
import { clearSession, getStoredSession, getStoredToken, saveSession } from './tokenStorage';

const KEY = 'movielogger.session';
const future = '2099-01-01T00:00:00';

describe('tokenStorage', () => {
  it('keeps a "remember me" session in localStorage', () => {
    saveSession({ token: 'abc', expiresAt: future }, true);

    expect(window.localStorage.getItem(KEY)).not.toBeNull();
    expect(window.sessionStorage.getItem(KEY)).toBeNull();
    expect(getStoredToken()).toBe('abc');
  });

  it('keeps other sessions in sessionStorage only', () => {
    saveSession({ token: 'abc', expiresAt: future }, false);

    expect(window.localStorage.getItem(KEY)).toBeNull();
    expect(window.sessionStorage.getItem(KEY)).not.toBeNull();
    expect(getStoredToken()).toBe('abc');
  });

  it('replaces a previous session wherever it was stored', () => {
    saveSession({ token: 'old', expiresAt: future }, true);
    saveSession({ token: 'new', expiresAt: future }, false);

    expect(window.localStorage.getItem(KEY)).toBeNull();
    expect(getStoredToken()).toBe('new');
  });

  it('discards an expired session (API timestamps are UTC without a Z)', () => {
    saveSession({ token: 'abc', expiresAt: '2026-09-30T10:00:00' }, true);

    expect(getStoredSession(new Date('2026-09-30T10:00:01Z'))).toBeNull();
    expect(window.localStorage.getItem(KEY)).toBeNull();
  });

  it('ignores corrupt stored data', () => {
    window.localStorage.setItem(KEY, '{not json');

    expect(getStoredToken()).toBeNull();
  });

  it('clears both storages on logout', () => {
    saveSession({ token: 'abc', expiresAt: future }, true);
    clearSession();

    expect(getStoredToken()).toBeNull();
  });
});
