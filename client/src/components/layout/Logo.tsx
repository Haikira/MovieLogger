import { Projector } from 'lucide-react';
import styles from './Layout.module.css';

export function Logo({ large }: { large?: boolean }) {
  return (
    <span className={[styles.logo, large && styles.logoLarge].filter(Boolean).join(' ')}>
      <Projector size={large ? 40 : 30} strokeWidth={2} aria-hidden="true" />
      <span>Movie Logger</span>
    </span>
  );
}
