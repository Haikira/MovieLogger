import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, useLocation } from 'react-router';
import { AppProviders } from '../App';
import { AppRoutes } from '../AppRoutes';
import { saveSession } from '../auth/tokenStorage';
import { createQueryClient } from '../lib/queryClient';
import { emptyDashboard, genres, testUser } from './fixtures';
import { MockApi } from './mockApi';

export const TEST_TOKEN = 'test-token';

/** Stores a valid, unexpired session as if the user had logged in earlier. */
export function signIn(remember = true) {
  const expiresAt = new Date(Date.now() + 60 * 60 * 1000).toISOString().replace('Z', '');
  saveSession({ token: TEST_TOKEN, expiresAt }, remember);
}

/**
 * The API calls every authenticated page makes (session restore, sidebar stats, genres). Tests
 * override whichever they care about.
 */
export function createApi(): MockApi {
  return new MockApi()
    .on('GET', '/api/users/me', testUser)
    .on('GET', '/api/dashboard', emptyDashboard)
    .on('GET', '/api/genres', genres)
    .on('GET', '/api/watchlist', [])
    .on('GET', '/api/moviewatches', [])
    .install();
}

function LocationProbe() {
  const location = useLocation();
  return (
    <div data-testid="location" hidden>
      {`${location.pathname}${location.search}${location.hash}`}
    </div>
  );
}

export function currentLocation(): string {
  return screen.getByTestId('location').textContent ?? '';
}

export function renderApp(route: string) {
  const queryClient = createQueryClient();
  queryClient.setDefaultOptions({ queries: { ...queryClient.getDefaultOptions().queries, retry: false } });
  const user = userEvent.setup();
  const result = render(
    <MemoryRouter initialEntries={[route]}>
      <AppProviders queryClient={queryClient}>
        <AppRoutes />
        <LocationProbe />
      </AppProviders>
    </MemoryRouter>,
  );
  return { user, queryClient, ...result };
}
