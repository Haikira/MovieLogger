import { getStoredToken } from '../auth/tokenStorage';

const API_BASE_URL = (import.meta.env.VITE_API_BASE_URL ?? '').replace(/\/$/, '');

/** Validation messages keyed by camelCase field name (e.g. "password", "dateWatched"). */
export type ApiFieldErrors = Record<string, string[]>;

export class ApiError extends Error {
  readonly status: number;
  readonly fieldErrors: ApiFieldErrors;

  constructor(status: number, message: string, fieldErrors: ApiFieldErrors = {}) {
    super(message);
    this.name = 'ApiError';
    this.status = status;
    this.fieldErrors = fieldErrors;
  }

  /** First validation message for a field, if the API reported one. */
  fieldError(field: string): string | undefined {
    return this.fieldErrors[field]?.[0];
  }
}

/** Status used when the request never got a response (network down, API not running). */
export const NETWORK_ERROR_STATUS = 0;

let unauthorizedHandler: ((rejectedToken: string) => void) | null = null;

/**
 * Registers what happens when an authenticated request is rejected with 401 (expired or revoked
 * token). The auth context uses this to sign the user out.
 */
export function setUnauthorizedHandler(handler: ((rejectedToken: string) => void) | null): void {
  unauthorizedHandler = handler;
}

type QueryValue = string | number | boolean | null | undefined;

export interface RequestOptions {
  method?: 'GET' | 'POST' | 'PUT' | 'DELETE';
  body?: unknown;
  query?: Record<string, QueryValue>;
  /** Send the stored bearer token. Defaults to true. */
  auth?: boolean;
  signal?: AbortSignal;
}

export async function apiRequest<T>(path: string, options: RequestOptions = {}): Promise<T> {
  const { method = 'GET', body, query, auth = true, signal } = options;

  const headers: Record<string, string> = { Accept: 'application/json' };
  if (body !== undefined) {
    headers['Content-Type'] = 'application/json';
  }
  const token = auth ? getStoredToken() : null;
  if (token) {
    headers.Authorization = `Bearer ${token}`;
  }

  let response: Response;
  try {
    response = await fetch(`${API_BASE_URL}${path}${buildQueryString(query)}`, {
      method,
      headers,
      body: body === undefined ? undefined : JSON.stringify(body),
      signal,
    });
  } catch (error) {
    if (error instanceof DOMException && error.name === 'AbortError') {
      throw error;
    }
    throw new ApiError(
      NETWORK_ERROR_STATUS,
      "Can't reach the Movie Logger server. Check your connection and try again.",
    );
  }

  if (response.status === 401 && token) {
    unauthorizedHandler?.(token);
  }

  if (!response.ok) {
    throw await toApiError(response);
  }

  if (response.status === 204) {
    return undefined as T;
  }
  const text = await response.text();
  return (text ? JSON.parse(text) : undefined) as T;
}

export function buildQueryString(query?: Record<string, QueryValue>): string {
  if (!query) {
    return '';
  }
  const params = new URLSearchParams();
  for (const [key, value] of Object.entries(query)) {
    if (value !== undefined && value !== null && value !== '') {
      params.set(key, String(value));
    }
  }
  const qs = params.toString();
  return qs ? `?${qs}` : '';
}

interface ErrorBody {
  message?: string;
  title?: string;
  errors?: Record<string, string[]>;
}

async function toApiError(response: Response): Promise<ApiError> {
  let body: ErrorBody | undefined;
  try {
    const text = await response.text();
    body = text ? (JSON.parse(text) as ErrorBody) : undefined;
  } catch {
    body = undefined;
  }

  const fieldErrors = normaliseFieldErrors(body?.errors);
  return new ApiError(response.status, messageFor(response.status, body, fieldErrors), fieldErrors);
}

/**
 * ASP.NET Core reports validation errors keyed by property name ("Password") or JSON path
 * ("$.dateWatched"); normalise both to the camelCase names the frontend uses.
 */
function normaliseFieldErrors(errors?: Record<string, string[]>): ApiFieldErrors {
  const result: ApiFieldErrors = {};
  if (!errors) {
    return result;
  }
  for (const [key, messages] of Object.entries(errors)) {
    const name = key.replace(/^\$\./, '');
    const field = name ? name.charAt(0).toLowerCase() + name.slice(1) : '';
    result[field] = [...(result[field] ?? []), ...messages];
  }
  return result;
}

function messageFor(status: number, body: ErrorBody | undefined, fieldErrors: ApiFieldErrors): string {
  if (body?.message) {
    return body.message;
  }
  switch (status) {
    case 400: {
      const first = Object.values(fieldErrors)[0]?.[0];
      return first ?? 'Please check the details you entered and try again.';
    }
    case 401:
      return 'Your session has expired. Please log in again.';
    case 403:
      return "You don't have permission to do that.";
    case 404:
      return "We couldn't find what you were looking for. It may have been deleted.";
    case 409:
      return 'That conflicts with something that already exists.';
    default:
      return status >= 500
        ? 'Something went wrong on our side. Please try again in a moment.'
        : 'Something went wrong. Please try again.';
  }
}

/** A user-facing message for any error thrown while talking to the API. */
export function errorMessage(error: unknown): string {
  if (error instanceof ApiError) {
    return error.message;
  }
  return 'Something went wrong. Please try again.';
}

export function isApiError(error: unknown, status?: number): error is ApiError {
  return error instanceof ApiError && (status === undefined || error.status === status);
}
