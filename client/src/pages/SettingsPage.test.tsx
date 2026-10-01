import { screen, waitFor, within } from '@testing-library/react';
import { beforeEach, describe, expect, it } from 'vitest';
import { getStoredToken } from '../auth/tokenStorage';
import { testUser } from '../test/fixtures';
import { json, noContent } from '../test/mockApi';
import { createApi, currentLocation, renderApp, signIn } from '../test/renderApp';

describe('Settings page', () => {
  beforeEach(() => signIn());

  it('updates the profile for the signed-in user only', async () => {
    const api = createApi().on('PUT', `/api/users/${testUser.id}`, noContent());
    const { user } = renderApp('/settings');

    const name = await screen.findByLabelText('Display name');
    await user.clear(name);
    await user.type(name, 'Cam');
    await user.click(screen.getByRole('button', { name: 'Save Changes' }));

    expect(await screen.findByText('Your profile has been updated.')).toBeInTheDocument();
    expect(api.calls('PUT', `/api/users/${testUser.id}`)[0].body).toEqual({ displayName: 'Cam', email: testUser.email });
    // The sidebar picks up the new name without a reload.
    expect(within(screen.getByRole('complementary')).getByText('Cam')).toBeInTheDocument();
  });

  it('shows an incorrect current password against that field', async () => {
    createApi().on('POST', '/api/users/change-password', json(400, { message: 'Current password is incorrect.' }));
    const { user } = renderApp('/settings');

    await user.type(await screen.findByLabelText('Current password'), 'wrong-password');
    await user.type(screen.getByLabelText('New password'), 'newpassword1');
    await user.type(screen.getByLabelText('Confirm new password'), 'newpassword1');
    await user.click(screen.getByRole('button', { name: 'Update Password' }));

    expect(await screen.findByText('Current password is incorrect.')).toBeInTheDocument();
    expect(screen.getByLabelText('Current password')).toHaveAttribute('aria-invalid', 'true');
  });

  it('applies the design password rule to a new password', async () => {
    const api = createApi();
    const { user } = renderApp('/settings');

    await user.type(await screen.findByLabelText('Current password'), 'whatever1');
    await user.type(screen.getByLabelText('New password'), 'short');
    await user.click(screen.getByRole('button', { name: 'Update Password' }));

    expect(screen.getByText('Password must be at least 8 characters.')).toBeInTheDocument();
    expect(api.calls('POST', '/api/users/change-password')).toHaveLength(0);
  });

  it('does not offer photo upload (not supported by the API)', async () => {
    createApi();
    renderApp('/settings');

    await screen.findByRole('heading', { name: 'Profile' });
    expect(screen.queryByText(/change photo/i)).not.toBeInTheDocument();
  });

  it('logs out', async () => {
    createApi();
    const { user } = renderApp('/settings');

    await user.click(await screen.findByRole('button', { name: 'Log Out' }));

    expect(await screen.findByRole('heading', { name: 'Welcome back' })).toBeInTheDocument();
    expect(getStoredToken()).toBeNull();
  });

  it('only deletes the account after confirmation, then signs out', async () => {
    const api = createApi().on('DELETE', `/api/users/${testUser.id}`, noContent());
    const { user } = renderApp('/settings');

    await user.click(await screen.findByRole('button', { name: 'Delete Account' }));
    let dialog = screen.getByRole('alertdialog', { name: 'Delete your account?' });
    await user.click(within(dialog).getByRole('button', { name: 'Cancel' }));
    expect(screen.queryByRole('alertdialog')).not.toBeInTheDocument();
    expect(api.calls('DELETE', `/api/users/${testUser.id}`)).toHaveLength(0);

    await user.click(screen.getByRole('button', { name: 'Delete Account' }));
    dialog = screen.getByRole('alertdialog', { name: 'Delete your account?' });
    await user.click(within(dialog).getByRole('button', { name: 'Delete My Account' }));

    expect(await screen.findByRole('heading', { name: 'Welcome back' })).toBeInTheDocument();
    expect(currentLocation()).toBe('/login');
    expect(api.calls('DELETE', `/api/users/${testUser.id}`)).toHaveLength(1);
    expect(getStoredToken()).toBeNull();
  });

  it('keeps the user signed in and explains when deletion fails', async () => {
    createApi().on('DELETE', `/api/users/${testUser.id}`, json(500));
    const { user } = renderApp('/settings');

    await user.click(await screen.findByRole('button', { name: 'Delete Account' }));
    await user.click(screen.getByRole('button', { name: 'Delete My Account' }));

    expect(await screen.findByText('Something went wrong on our side. Please try again in a moment.')).toBeInTheDocument();
    await waitFor(() => expect(screen.getByRole('button', { name: 'Delete My Account' })).toBeEnabled());
    expect(getStoredToken()).not.toBeNull();
  });
});
