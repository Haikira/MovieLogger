import { screen, waitFor, within } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { testUser } from '../test/fixtures';
import { json, validationProblem } from '../test/mockApi';
import { createApi, currentLocation, renderApp, signIn, TEST_TOKEN } from '../test/renderApp';
import { getStoredToken } from './tokenStorage';

const authResponse = {
  token: 'fresh-token',
  expiresAt: new Date(Date.now() + 3_600_000).toISOString().replace('Z', ''),
  user: testUser,
};

describe('protected routes', () => {
  it('redirects a signed-out user to the login page, then back where they were going', async () => {
    const api = createApi()
      .on('POST', '/api/auth/login', authResponse)
      .on('GET', '/api/moviewatches/my-movies', { items: [], totalCount: 0, page: 1, pageSize: 8 });
    const { user } = renderApp('/my-movies?rating=5');

    expect(await screen.findByRole('heading', { name: 'Welcome back' })).toBeInTheDocument();
    expect(currentLocation()).toBe('/login');
    expect(api.calls('GET', '/api/users/me')).toHaveLength(0);

    await user.type(screen.getByLabelText('Email address'), 'campbell@example.com');
    await user.type(screen.getByLabelText('Password'), 'Correct-Horse-9');
    await user.click(screen.getByRole('button', { name: 'Log In' }));

    expect(await screen.findByRole('heading', { name: 'My Movies' })).toBeInTheDocument();
    expect(currentLocation()).toBe('/my-movies?rating=5');
    expect(api.calls('POST', '/api/auth/login')[0].body).toEqual({
      email: 'campbell@example.com',
      password: 'Correct-Horse-9',
    });
  });

  it('restores an existing session from the stored token via /api/users/me', async () => {
    signIn();
    const api = createApi();
    renderApp('/dashboard');

    expect(await screen.findByText(/Welcome back, Campbell/)).toBeInTheDocument();
    expect(api.calls('GET', '/api/users/me')[0].headers.Authorization).toBe(`Bearer ${TEST_TOKEN}`);
    expect(within(screen.getByRole('complementary')).getByText('Campbell')).toBeInTheDocument();
  });

  it('signs the user out when the stored token is rejected', async () => {
    signIn();
    createApi().on('GET', '/api/users/me', json(401));
    renderApp('/dashboard');

    expect(await screen.findByText('Your session has ended. Please log in again.')).toBeInTheDocument();
    expect(currentLocation()).toBe('/login');
    expect(getStoredToken()).toBeNull();
  });

  it('signs the user out when a request is rejected mid-session (expired token)', async () => {
    signIn();
    createApi().on('GET', '/api/dashboard', json(401));
    renderApp('/dashboard');

    expect(await screen.findByText('Your session has ended. Please log in again.')).toBeInTheDocument();
    expect(getStoredToken()).toBeNull();
  });

  it('offers a retry instead of logging out when the API is unreachable', async () => {
    signIn();
    createApi().on('GET', '/api/users/me', json(503));
    renderApp('/dashboard');

    expect(await screen.findByRole('heading', { name: "Can't reach Movie Logger" })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Try Again' })).toBeInTheDocument();
    expect(getStoredToken()).toBe(TEST_TOKEN);
  });

  it('sends a signed-in user away from the login page', async () => {
    signIn();
    createApi();
    renderApp('/login');

    expect(await screen.findByRole('heading', { name: 'Dashboard' })).toBeInTheDocument();
  });
});

describe('login', () => {
  it('shows an error and stores nothing for invalid credentials', async () => {
    createApi().on('POST', '/api/auth/login', json(401, { message: 'Invalid email or password.' }));
    const { user } = renderApp('/login');

    await user.type(await screen.findByLabelText('Email address'), 'campbell@example.com');
    await user.type(screen.getByLabelText('Password'), 'wrong-password');
    await user.click(screen.getByRole('button', { name: 'Log In' }));

    expect(await screen.findByText('Invalid email or password.')).toBeInTheDocument();
    expect(getStoredToken()).toBeNull();
    expect(currentLocation()).toBe('/login');
  });

  it('validates the fields before calling the API', async () => {
    const api = createApi();
    const { user } = renderApp('/login');

    await user.type(await screen.findByLabelText('Email address'), 'not-an-email');
    await user.click(screen.getByRole('button', { name: 'Log In' }));

    expect(screen.getByText(/Enter a valid email address/)).toBeInTheDocument();
    expect(screen.getByText('Enter your password.')).toBeInTheDocument();
    expect(screen.getByLabelText('Email address')).toHaveAttribute('aria-invalid', 'true');
    expect(api.calls('POST', '/api/auth/login')).toHaveLength(0);
  });

  it('keeps the session across browser restarts only with "Remember me"', async () => {
    createApi().on('POST', '/api/auth/login', authResponse);
    const { user } = renderApp('/login');

    await user.type(await screen.findByLabelText('Email address'), 'campbell@example.com');
    await user.type(screen.getByLabelText('Password'), 'Correct-Horse-9');
    await user.click(screen.getByLabelText('Remember me'));
    await user.click(screen.getByRole('button', { name: 'Log In' }));

    await screen.findByRole('heading', { name: 'Dashboard' });
    expect(window.localStorage.getItem('movielogger.session')).toContain('fresh-token');
    expect(window.sessionStorage.getItem('movielogger.session')).toBeNull();
  });

  it('does not offer the unsupported forgot-password flow', async () => {
    createApi();
    renderApp('/login');

    await screen.findByRole('heading', { name: 'Welcome back' });
    expect(screen.queryByText(/forgot/i)).not.toBeInTheDocument();
  });
});

describe('register', () => {
  async function fillForm(user: ReturnType<typeof renderApp>['user'], password: string, confirm = password) {
    await user.type(await screen.findByLabelText('Display name'), 'Campbell');
    await user.type(screen.getByLabelText('Email address'), 'campbell@example.com');
    await user.type(screen.getByLabelText('Password'), password);
    await user.type(screen.getByLabelText('Confirm password'), confirm);
  }

  it('enforces the design password rule (8+ characters with a number) immediately', async () => {
    const api = createApi();
    const { user } = renderApp('/register');

    await user.type(await screen.findByLabelText('Password'), 'longpassword');
    await user.tab();

    expect(screen.getByText('Password must include at least one number.')).toBeInTheDocument();
    await user.type(screen.getByLabelText('Password'), '1');
    expect(screen.queryByText('Password must include at least one number.')).not.toBeInTheDocument();
    expect(api.calls('POST', '/api/auth/register')).toHaveLength(0);
  });

  it("reports passwords that don't match", async () => {
    const api = createApi();
    const { user } = renderApp('/register');

    await fillForm(user, 'password123', 'password124');
    await user.click(screen.getByRole('button', { name: 'Create Account' }));

    expect(screen.getByText("Passwords don't match.")).toBeInTheDocument();
    expect(api.calls('POST', '/api/auth/register')).toHaveLength(0);
  });

  it('shows a duplicate email (409) against the email field', async () => {
    createApi().on('POST', '/api/auth/register', json(409, { message: 'A user with this email already exists.' }));
    const { user } = renderApp('/register');

    await fillForm(user, 'password123');
    await user.click(screen.getByRole('button', { name: 'Create Account' }));

    expect(await screen.findByText(/An account with this email already exists/)).toBeInTheDocument();
    expect(screen.getByLabelText('Email address')).toHaveAttribute('aria-invalid', 'true');
  });

  it('shows server validation errors next to the right field', async () => {
    createApi().on('POST', '/api/auth/register', validationProblem({ Email: ['The Email field is not a valid e-mail address.'] }));
    const { user } = renderApp('/register');

    await fillForm(user, 'password123');
    await user.click(screen.getByRole('button', { name: 'Create Account' }));

    expect(await screen.findByText('The Email field is not a valid e-mail address.')).toBeInTheDocument();
  });

  it('signs the new user in', async () => {
    const api = createApi().on('POST', '/api/auth/register', authResponse);
    const { user } = renderApp('/register');

    await fillForm(user, 'password123');
    await user.click(screen.getByRole('button', { name: 'Create Account' }));

    expect(await screen.findByRole('heading', { name: 'Dashboard' })).toBeInTheDocument();
    await waitFor(() => expect(getStoredToken()).toBe('fresh-token'));
    expect(api.calls('POST', '/api/auth/register')[0].body).toMatchObject({ displayName: 'Campbell', confirmPassword: 'password123' });
  });
});
