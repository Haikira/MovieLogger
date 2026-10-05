import { useState, type FormEvent } from 'react';
import { Link } from 'react-router';
import { errorMessage, isApiError } from '../api/client';
import { useAuth } from '../auth/useAuth';
import { AuthLayout } from '../components/layout/AuthLayout';
import { Button } from '../components/ui/Button';
import { TextField } from '../components/ui/Field';
import { Alert, FormError } from '../components/ui/Feedback';
import { useFormState } from '../hooks/useFormState';
import { hasErrors, validateEmail, type FieldErrors } from '../lib/validation';
import styles from './AuthPages.module.css';

type Field = 'email' | 'password';

// The design's "Forgot password?" link is intentionally omitted: the API has no password reset yet.
export function LoginPage() {
  const { login, sessionExpired } = useAuth();
  const form = useFormState({ email: '', password: '', remember: false });
  const [submitting, setSubmitting] = useState(false);
  const [formError, setFormError] = useState<string | null>(null);

  const errors: FieldErrors<Field> = {
    email: validateEmail(form.values.email),
    password: form.values.password ? undefined : 'Enter your password.',
  };
  const visible = (field: Field) => (form.shouldShow(field) ? errors[field] : undefined);

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    form.setSubmitted(true);
    setFormError(null);
    if (hasErrors(errors)) {
      return;
    }
    setSubmitting(true);
    try {
      await login({ email: form.values.email.trim(), password: form.values.password }, form.values.remember);
      // RouteGuards redirect the now signed-in user to where they were going.
    } catch (error) {
      setFormError(isApiError(error, 401) ? 'Invalid email or password.' : errorMessage(error));
      setSubmitting(false);
    }
  }

  return (
    <AuthLayout
      title="Welcome back"
      subtitle="Log in to keep track of everything you've watched."
      footer={
        <>
          Don&apos;t have an account?<Link to="/register">Sign up</Link>
        </>
      }
    >
      <form className={styles.form} onSubmit={handleSubmit} noValidate>
        {sessionExpired && <Alert>Your session has ended. Please log in again.</Alert>}
        <FormError error={formError} />
        <TextField
          label="Email address"
          type="email"
          autoComplete="email"
          placeholder="you@example.com"
          value={form.values.email}
          onChange={(e) => form.setValue('email', e.target.value)}
          onBlur={() => form.touch('email')}
          error={visible('email')}
        />
        <TextField
          label="Password"
          type="password"
          autoComplete="current-password"
          placeholder="Enter your password"
          value={form.values.password}
          onChange={(e) => form.setValue('password', e.target.value)}
          onBlur={() => form.touch('password')}
          error={visible('password')}
        />
        <div className={styles.row}>
          <label className={styles.checkbox}>
            <input
              type="checkbox"
              checked={form.values.remember}
              onChange={(e) => form.setValue('remember', e.target.checked)}
            />
            Remember me
          </label>
        </div>
        <Button type="submit" block className={styles.submit} busy={submitting} busyLabel="Logging in…">
          Log In
        </Button>
      </form>
    </AuthLayout>
  );
}
