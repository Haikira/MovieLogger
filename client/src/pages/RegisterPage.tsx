import { useState, type FormEvent } from 'react';
import { Link } from 'react-router';
import { ApiError, errorMessage, isApiError } from '../api/client';
import { useAuth } from '../auth/useAuth';
import { AuthLayout } from '../components/layout/AuthLayout';
import { Button } from '../components/ui/Button';
import { TextField } from '../components/ui/Field';
import { FormError } from '../components/ui/Feedback';
import { useFormState } from '../hooks/useFormState';
import {
  hasErrors,
  PASSWORD_HINT,
  validateDisplayName,
  validateEmail,
  validateNewPassword,
  validatePasswordConfirmation,
  type FieldErrors,
} from '../lib/validation';
import styles from './AuthPages.module.css';

type Field = 'displayName' | 'email' | 'password' | 'confirmPassword';

export function RegisterPage() {
  const { register } = useAuth();
  const form = useFormState({ displayName: '', email: '', password: '', confirmPassword: '' });
  const [submitting, setSubmitting] = useState(false);
  const [formError, setFormError] = useState<string | null>(null);
  const [serverErrors, setServerErrors] = useState<FieldErrors<Field>>({});

  const { values } = form;
  const errors: FieldErrors<Field> = {
    displayName: validateDisplayName(values.displayName),
    email: validateEmail(values.email),
    password: validateNewPassword(values.password),
    confirmPassword: validatePasswordConfirmation(values.password, values.confirmPassword),
  };
  const visible = (field: Field) => (form.shouldShow(field) ? errors[field] : undefined) ?? serverErrors[field];

  function change(field: Field, value: string) {
    form.setValue(field, value);
    setServerErrors((current) => ({ ...current, [field]: undefined }));
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    form.setSubmitted(true);
    setFormError(null);
    if (hasErrors(errors)) {
      return;
    }
    setSubmitting(true);
    try {
      await register({
        displayName: values.displayName.trim(),
        email: values.email.trim(),
        password: values.password,
        confirmPassword: values.confirmPassword,
      });
    } catch (error) {
      setSubmitting(false);
      if (isApiError(error, 409)) {
        setServerErrors({ email: 'An account with this email already exists. Try logging in instead.' });
      } else if (error instanceof ApiError && error.status === 400 && Object.keys(error.fieldErrors).length) {
        setServerErrors({
          displayName: error.fieldError('displayName'),
          email: error.fieldError('email'),
          password: error.fieldError('password'),
          confirmPassword: error.fieldError('confirmPassword'),
        });
        setFormError('Please fix the highlighted fields.');
      } else {
        setFormError(errorMessage(error));
      }
    }
  }

  return (
    <AuthLayout
      title="Create your account"
      subtitle="Start logging the movies you watch."
      footer={
        <>
          Already have an account?<Link to="/login">Log in</Link>
        </>
      }
    >
      <form className={styles.form} onSubmit={handleSubmit} noValidate>
        <FormError error={formError} />
        <TextField
          label="Display name"
          autoComplete="nickname"
          placeholder="e.g. Campbell"
          maxLength={100}
          value={values.displayName}
          onChange={(e) => change('displayName', e.target.value)}
          onBlur={() => form.touch('displayName')}
          error={visible('displayName')}
        />
        <TextField
          label="Email address"
          type="email"
          autoComplete="email"
          placeholder="you@example.com"
          value={values.email}
          onChange={(e) => change('email', e.target.value)}
          onBlur={() => form.touch('email')}
          error={visible('email')}
        />
        <TextField
          label="Password"
          type="password"
          autoComplete="new-password"
          placeholder="Create a password"
          hint={PASSWORD_HINT}
          value={values.password}
          onChange={(e) => change('password', e.target.value)}
          onBlur={() => form.touch('password')}
          error={visible('password')}
        />
        <TextField
          label="Confirm password"
          type="password"
          autoComplete="new-password"
          placeholder="Re-enter your password"
          value={values.confirmPassword}
          onChange={(e) => change('confirmPassword', e.target.value)}
          onBlur={() => form.touch('confirmPassword')}
          error={visible('confirmPassword')}
        />
        <Button type="submit" block className={styles.submit} busy={submitting} busyLabel="Creating account…">
          Create Account
        </Button>
      </form>
    </AuthLayout>
  );
}
