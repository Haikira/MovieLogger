import type { ButtonHTMLAttributes, ReactNode } from 'react';
import { Link, type LinkProps } from 'react-router';
import styles from './Button.module.css';

export type ButtonVariant = 'primary' | 'secondary' | 'danger' | 'dangerSolid' | 'link';

interface StyleProps {
  variant?: ButtonVariant;
  size?: 'md' | 'small';
  block?: boolean;
}

function classes({ variant = 'primary', size = 'md', block }: StyleProps, extra?: string): string {
  return [styles.button, styles[variant], size === 'small' && styles.small, block && styles.block, extra]
    .filter(Boolean)
    .join(' ');
}

export interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement>, StyleProps {
  /** Shows a busy label and disables the button while an action is in progress. */
  busy?: boolean;
  busyLabel?: string;
}

export function Button({ variant, size, block, busy, busyLabel, className, children, disabled, type, ...rest }: ButtonProps) {
  return (
    <button
      type={type ?? 'button'}
      className={classes({ variant, size, block }, className)}
      disabled={disabled || busy}
      aria-busy={busy || undefined}
      {...rest}
    >
      {busy && busyLabel ? busyLabel : children}
    </button>
  );
}

export function ButtonLink({ variant, size, block, className, ...rest }: LinkProps & StyleProps) {
  return <Link className={classes({ variant, size, block }, className)} {...rest} />;
}

export interface IconButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  /** Accessible name; icon-only buttons must have one. */
  label: string;
  icon: ReactNode;
  active?: boolean;
}

export function IconButton({ label, icon, active, className, type, ...rest }: IconButtonProps) {
  return (
    <button
      type={type ?? 'button'}
      aria-label={label}
      title={label}
      className={[styles.iconButton, active && styles.iconButtonActive, className].filter(Boolean).join(' ')}
      {...rest}
    >
      {icon}
    </button>
  );
}

export function IconButtonLink({
  label,
  icon,
  className,
  ...rest
}: LinkProps & { label: string; icon: ReactNode }) {
  return (
    <Link aria-label={label} title={label} className={[styles.iconButton, className].filter(Boolean).join(' ')} {...rest}>
      {icon}
    </Link>
  );
}
