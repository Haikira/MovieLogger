import { useEffect, useId, useRef, type KeyboardEvent, type ReactNode } from 'react';
import { createPortal } from 'react-dom';
import { X } from 'lucide-react';
import { Button } from './Button';
import { FormError } from './Feedback';
import styles from './Dialog.module.css';

const FOCUSABLE =
  'a[href], button:not([disabled]), input:not([disabled]), select:not([disabled]), textarea:not([disabled]), [tabindex]:not([tabindex="-1"])';

interface DialogProps {
  title: string;
  description?: ReactNode;
  children?: ReactNode;
  footer: ReactNode;
  onClose: () => void;
  /** Prevents closing (Escape, backdrop, close button) while an action is in progress. */
  busy?: boolean;
  /** "alertdialog" for confirmations that interrupt the user. */
  role?: 'dialog' | 'alertdialog';
  /** Selector for the element to focus when the dialog opens; defaults to the first focusable. */
  initialFocus?: string;
}

/**
 * Modal dialog: traps focus while open, closes on Escape, and returns focus to whatever opened
 * it. Render it conditionally (`{open && <Dialog … />}`).
 */
export function Dialog({ title, description, children, footer, onClose, busy, role = 'dialog', initialFocus }: DialogProps) {
  const titleId = useId();
  const descriptionId = useId();
  const panelRef = useRef<HTMLDivElement>(null);
  const onCloseRef = useRef(onClose);
  const busyRef = useRef(busy);

  useEffect(() => {
    onCloseRef.current = onClose;
    busyRef.current = busy;
  });

  useEffect(() => {
    const previouslyFocused = document.activeElement as HTMLElement | null;
    const panel = panelRef.current;
    const target =
      (initialFocus && panel?.querySelector<HTMLElement>(initialFocus)) ||
      panel?.querySelector<HTMLElement>(FOCUSABLE) ||
      panel;
    target?.focus();

    const originalOverflow = document.body.style.overflow;
    document.body.style.overflow = 'hidden';

    return () => {
      document.body.style.overflow = originalOverflow;
      previouslyFocused?.focus?.();
    };
    // Focus handling runs once per opening.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  function handleKeyDown(event: KeyboardEvent<HTMLDivElement>) {
    if (event.key === 'Escape') {
      event.stopPropagation();
      if (!busyRef.current) {
        onCloseRef.current();
      }
      return;
    }
    if (event.key !== 'Tab' || !panelRef.current) {
      return;
    }
    const focusable = Array.from(panelRef.current.querySelectorAll<HTMLElement>(FOCUSABLE));
    if (focusable.length === 0) {
      event.preventDefault();
      return;
    }
    const first = focusable[0];
    const last = focusable[focusable.length - 1];
    if (event.shiftKey && document.activeElement === first) {
      event.preventDefault();
      last.focus();
    } else if (!event.shiftKey && document.activeElement === last) {
      event.preventDefault();
      first.focus();
    }
  }

  return createPortal(
    <div
      className={styles.backdrop}
      onMouseDown={(event) => {
        if (event.target === event.currentTarget && !busy) {
          onClose();
        }
      }}
    >
      <div
        ref={panelRef}
        role={role}
        aria-modal="true"
        aria-labelledby={titleId}
        aria-describedby={description ? descriptionId : undefined}
        tabIndex={-1}
        className={styles.panel}
        onKeyDown={handleKeyDown}
      >
        <div className={styles.header}>
          <h2 id={titleId} className={styles.title}>
            {title}
          </h2>
          <button type="button" className={styles.close} onClick={onClose} disabled={busy} aria-label="Close dialog">
            <X size={18} aria-hidden="true" />
          </button>
        </div>
        {description && (
          <div id={descriptionId} className={styles.description}>
            {description}
          </div>
        )}
        {children && <div className={styles.body}>{children}</div>}
        <div className={styles.footer}>{footer}</div>
      </div>
    </div>,
    document.body,
  );
}

interface ConfirmDialogProps {
  title: string;
  description: ReactNode;
  confirmLabel: string;
  busyLabel?: string;
  cancelLabel?: string;
  destructive?: boolean;
  busy?: boolean;
  error?: string | null;
  onConfirm: () => void;
  onCancel: () => void;
  children?: ReactNode;
}

export function ConfirmDialog({
  title,
  description,
  confirmLabel,
  busyLabel,
  cancelLabel = 'Cancel',
  destructive,
  busy,
  error,
  onConfirm,
  onCancel,
  children,
}: ConfirmDialogProps) {
  return (
    <Dialog
      role="alertdialog"
      title={title}
      description={description}
      onClose={onCancel}
      busy={busy}
      // Destructive actions start on the safe choice.
      initialFocus={destructive ? '[data-dialog-cancel]' : '[data-dialog-confirm]'}
      footer={
        <>
          <Button variant="secondary" onClick={onCancel} disabled={busy} data-dialog-cancel="">
            {cancelLabel}
          </Button>
          <Button
            variant={destructive ? 'dangerSolid' : 'primary'}
            onClick={onConfirm}
            busy={busy}
            busyLabel={busyLabel}
            data-dialog-confirm=""
          >
            {confirmLabel}
          </Button>
        </>
      }
    >
      {children}
      <FormError error={error} />
    </Dialog>
  );
}
