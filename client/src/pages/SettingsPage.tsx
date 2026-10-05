import { useState, type FormEvent } from 'react';
import { useNavigate } from 'react-router';
import { ApiError, errorMessage, isApiError } from '../api/client';
import { usersApi } from '../api/endpoints';
import { useAuth, useCurrentUser } from '../auth/useAuth';
import { PageHeader } from '../components/layout/PageHeader';
import layoutStyles from '../components/layout/Layout.module.css';
import { Button } from '../components/ui/Button';
import { Card } from '../components/ui/Controls';
import { ConfirmDialog } from '../components/ui/Dialog';
import { TextField } from '../components/ui/Field';
import { Alert, FormError } from '../components/ui/Feedback';
import { useDashboard } from '../hooks/queries';
import { useFormState } from '../hooks/useFormState';
import { formatTimestampMonthYear } from '../lib/dates';
import { initialOf } from '../lib/format';
import {
  hasErrors,
  PASSWORD_HINT,
  validateDisplayName,
  validateEmail,
  validateNewPassword,
  validatePasswordConfirmation,
} from '../lib/validation';
import styles from './SettingsPage.module.css';

export function SettingsPage() {
  return (
    <>
      <PageHeader title="Settings" subtitle="Manage your profile, account and preferences." />
      <div className={styles.grid}>
        <ProfileCard />
        <PasswordCard />
      </div>
      <AccountCard />
    </>
  );
}

function ProfileCard() {
  const user = useCurrentUser();
  const { setUser } = useAuth();
  const dashboard = useDashboard();
  const form = useFormState({ displayName: user.displayName, email: user.email });
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [saved, setSaved] = useState(false);

  const { values } = form;
  const errors = { displayName: validateDisplayName(values.displayName), email: validateEmail(values.email) };
  const changed = values.displayName.trim() !== user.displayName || values.email.trim() !== user.email;
  const logged = dashboard.data?.totalMoviesLogged;

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    form.setSubmitted(true);
    setError(null);
    setSaved(false);
    if (hasErrors(errors)) {
      return;
    }
    setSaving(true);
    const request = { displayName: values.displayName.trim(), email: values.email.trim() };
    try {
      // The id comes from the authenticated /api/users/me response, never from user input.
      await usersApi.update(user.id, request);
      setUser({ ...user, ...request });
      form.reset(request);
      setSaved(true);
    } catch (err) {
      setError(
        err instanceof ApiError && err.status === 400 && Object.keys(err.fieldErrors).length
          ? (err.fieldError('email') ?? err.fieldError('displayName') ?? err.message)
          : errorMessage(err),
      );
    } finally {
      setSaving(false);
    }
  }

  return (
    <Card as="section" labelledBy="profile-heading">
      <h2 id="profile-heading" className={styles.cardTitle}>
        Profile
      </h2>
      <p className={styles.cardSubtitle}>How you appear in Movie Logger.</p>
      <form className={styles.form} onSubmit={handleSubmit} noValidate>
        {/* Photo upload isn't supported by the API yet, so the avatar is the display name's initial. */}
        <span className={`${layoutStyles.avatar} ${layoutStyles.avatarLarge}`} aria-hidden="true">
          {initialOf(values.displayName || user.displayName)}
        </span>
        <FormError error={error} />
        <div className="live-region" aria-live="polite">{saved && <Alert tone="success">Your profile has been updated.</Alert>}</div>
        <TextField
          label="Display name"
          autoComplete="nickname"
          maxLength={100}
          value={values.displayName}
          onChange={(e) => {
            form.setValue('displayName', e.target.value);
            setSaved(false);
          }}
          onBlur={() => form.touch('displayName')}
          error={form.shouldShow('displayName') ? errors.displayName : undefined}
        />
        <TextField
          label="Email address"
          type="email"
          autoComplete="email"
          value={values.email}
          onChange={(e) => {
            form.setValue('email', e.target.value);
            setSaved(false);
          }}
          onBlur={() => form.touch('email')}
          error={form.shouldShow('email') ? errors.email : undefined}
        />
        <p className={styles.meta}>
          {logged !== undefined && `${logged} ${logged === 1 ? 'movie' : 'movies'} logged · `}
          Member since {formatTimestampMonthYear(user.createdAt)}
        </p>
        <div className={styles.actions}>
          <Button type="submit" busy={saving} busyLabel="Saving…" disabled={!changed}>
            Save Changes
          </Button>
        </div>
      </form>
    </Card>
  );
}

const EMPTY_PASSWORDS = { currentPassword: '', newPassword: '', confirmNewPassword: '' };

function PasswordCard() {
  const form = useFormState(EMPTY_PASSWORDS);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [currentPasswordError, setCurrentPasswordError] = useState<string | undefined>();
  const [saved, setSaved] = useState(false);

  const { values } = form;
  const errors = {
    currentPassword: values.currentPassword ? undefined : 'Enter your current password.',
    newPassword: validateNewPassword(values.newPassword),
    confirmNewPassword: validatePasswordConfirmation(values.newPassword, values.confirmNewPassword),
  };
  const shown = (field: keyof typeof errors) => (form.shouldShow(field) ? errors[field] : undefined);

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    form.setSubmitted(true);
    setError(null);
    setSaved(false);
    if (hasErrors(errors)) {
      return;
    }
    setSaving(true);
    try {
      await usersApi.changePassword(values);
      form.reset(EMPTY_PASSWORDS);
      setSaved(true);
    } catch (err) {
      if (isApiError(err, 400) && /current password/i.test(err.message)) {
        setCurrentPasswordError(err.message);
      } else {
        setError(errorMessage(err));
      }
    } finally {
      setSaving(false);
    }
  }

  return (
    <Card as="section" labelledBy="password-heading">
      <h2 id="password-heading" className={styles.cardTitle}>
        Password
      </h2>
      <p className={styles.cardSubtitle}>Change the password you use to log in.</p>
      <form className={styles.form} onSubmit={handleSubmit} noValidate>
        <FormError error={error} />
        <div className="live-region" aria-live="polite">{saved && <Alert tone="success">Your password has been changed.</Alert>}</div>
        <TextField
          label="Current password"
          type="password"
          autoComplete="current-password"
          value={values.currentPassword}
          onChange={(e) => {
            form.setValue('currentPassword', e.target.value);
            setCurrentPasswordError(undefined);
            setSaved(false);
          }}
          onBlur={() => form.touch('currentPassword')}
          error={shown('currentPassword') ?? currentPasswordError}
        />
        <TextField
          label="New password"
          type="password"
          autoComplete="new-password"
          hint={PASSWORD_HINT}
          value={values.newPassword}
          onChange={(e) => {
            form.setValue('newPassword', e.target.value);
            setSaved(false);
          }}
          onBlur={() => form.touch('newPassword')}
          error={shown('newPassword')}
        />
        <TextField
          label="Confirm new password"
          type="password"
          autoComplete="new-password"
          value={values.confirmNewPassword}
          onChange={(e) => {
            form.setValue('confirmNewPassword', e.target.value);
            setSaved(false);
          }}
          onBlur={() => form.touch('confirmNewPassword')}
          error={shown('confirmNewPassword')}
        />
        <div className={styles.actions}>
          <Button type="submit" busy={saving} busyLabel="Updating…">
            Update Password
          </Button>
        </div>
      </form>
    </Card>
  );
}

function AccountCard() {
  const user = useCurrentUser();
  const { logout } = useAuth();
  const navigate = useNavigate();
  const [confirming, setConfirming] = useState(false);
  const [deleting, setDeleting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  function handleLogout() {
    logout();
    navigate('/login', { replace: true });
  }

  async function handleDelete() {
    setDeleting(true);
    setError(null);
    try {
      await usersApi.remove(user.id);
    } catch (err) {
      // 404: the account is already gone, which is the outcome the user asked for.
      if (!isApiError(err, 404)) {
        setError(errorMessage(err));
        setDeleting(false);
        return;
      }
    }
    logout();
    navigate('/login', { replace: true });
  }

  return (
    <Card as="section" labelledBy="account-heading" className={styles.account}>
      <h2 id="account-heading" className={styles.cardTitle}>
        Account
      </h2>
      <p className={styles.accountText}>
        Log out of Movie Logger on this device, or permanently delete your account and all of your logs.
      </p>
      <div className={styles.accountActions}>
        <Button variant="secondary" onClick={handleLogout}>
          Log Out
        </Button>
        <Button variant="danger" onClick={() => setConfirming(true)}>
          Delete Account
        </Button>
      </div>
      {confirming && (
        <ConfirmDialog
          destructive
          title="Delete your account?"
          description="This permanently deletes your account, your watch logs, your watchlist and your lists. It can't be undone."
          confirmLabel="Delete My Account"
          busyLabel="Deleting…"
          busy={deleting}
          error={error}
          onConfirm={handleDelete}
          onCancel={() => {
            setConfirming(false);
            setError(null);
          }}
        />
      )}
    </Card>
  );
}
