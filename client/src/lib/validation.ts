/**
 * Client-side validation rules. These give immediate feedback; the API still validates every
 * request and its errors are shown as well.
 */

export const PASSWORD_HINT = 'At least 8 characters, including a number.';

// Deliberately simple: the API does the authoritative [EmailAddress] check.
const EMAIL = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

export function validateEmail(value: string): string | undefined {
  if (!value.trim()) {
    return 'Enter your email address.';
  }
  if (!EMAIL.test(value.trim())) {
    return 'Enter a valid email address, like you@example.com.';
  }
  if (value.trim().length > 256) {
    return 'Email address must be 256 characters or fewer.';
  }
  return undefined;
}

/** The Figma password rule: at least 8 characters including a number (stricter than the API's minimum). */
export function validateNewPassword(value: string): string | undefined {
  if (!value) {
    return 'Enter a password.';
  }
  if (value.length < 8) {
    return 'Password must be at least 8 characters.';
  }
  if (!/\d/.test(value)) {
    return 'Password must include at least one number.';
  }
  if (value.length > 100) {
    return 'Password must be 100 characters or fewer.';
  }
  return undefined;
}

export function validatePasswordConfirmation(password: string, confirmation: string): string | undefined {
  if (!confirmation) {
    return 'Confirm your password.';
  }
  return password === confirmation ? undefined : "Passwords don't match.";
}

export function validateDisplayName(value: string): string | undefined {
  if (!value.trim()) {
    return 'Enter a display name.';
  }
  if (value.trim().length > 100) {
    return 'Display name must be 100 characters or fewer.';
  }
  return undefined;
}

export function isValidHttpUrl(value: string): boolean {
  try {
    const url = new URL(value);
    return url.protocol === 'http:' || url.protocol === 'https:';
  } catch {
    return false;
  }
}

export type FieldErrors<T extends string> = Partial<Record<T, string>>;

export function hasErrors<T extends string>(errors: FieldErrors<T>): boolean {
  return Object.values(errors).some(Boolean);
}
