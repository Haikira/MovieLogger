import { useCallback } from 'react';
import { useLocation, useNavigate } from 'react-router';

export interface BackLink {
  to: string;
  label: string;
}

interface BackState {
  back?: BackLink;
}

/** Router state to attach to a link so the destination can offer "Back to …" this page. */
export function useBackState(label: string): BackState {
  const location = useLocation();
  return { back: { to: `${location.pathname}${location.search}`, label } };
}

/**
 * For Cancel buttons: go back to the previous page in the app, or to `fallback` when this page
 * was opened directly (no in-app history to return to).
 */
export function useGoBack(fallback: string): () => void {
  const navigate = useNavigate();
  const location = useLocation();
  return useCallback(() => {
    if (location.key !== 'default') {
      navigate(-1);
    } else {
      navigate(fallback);
    }
  }, [navigate, location.key, fallback]);
}

/** The "Back to …" link passed by the previous page, or a fallback. */
export function useBackLink(fallback: BackLink): BackLink {
  const location = useLocation();
  const back = (location.state as BackState | null)?.back;
  return back && back.to.startsWith('/') && !back.to.startsWith('//') ? back : fallback;
}
